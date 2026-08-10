import asyncio
import json
import os
import sys
import tempfile
from pathlib import Path

from fastapi import FastAPI, File, Header, HTTPException, UploadFile
from fastapi.responses import JSONResponse


BASE_DIRECTORY = Path("/app")
SCRIPT_PATH = BASE_DIRECTORY / "analyze.py"
MODEL_DIRECTORY = BASE_DIRECTORY / "models" / "Essentia" / "DiscogsMAEST"
REQUIRED_MODEL_PATH = MODEL_DIRECTORY / "discogs-maest-30s-pw-519l-2.pb"
SUPPORTED_EXTENSIONS = {".flac", ".m4a", ".mp3", ".wav"}
UPLOAD_CHUNK_SIZE = 1024 * 1024
API_KEY = os.environ.get("MUSIC_API_KEY", "").strip()
MAX_UPLOAD_BYTES = int(os.environ.get("MAX_UPLOAD_BYTES", str(1024 * 1024 * 1024)))
ANALYSIS_TIMEOUT_SECONDS = int(os.environ.get("ANALYSIS_TIMEOUT_SECONDS", "1800"))
MAX_CONCURRENT_ANALYSES = max(1, int(os.environ.get("MAX_CONCURRENT_ANALYSES", "1")))
ANALYSIS_SLOTS = asyncio.Semaphore(MAX_CONCURRENT_ANALYSES)

app = FastAPI(title="Resona Analysis API", docs_url=None, redoc_url=None)


@app.get("/health")
def health():
    if not SCRIPT_PATH.is_file() or not REQUIRED_MODEL_PATH.is_file():
        raise HTTPException(status_code=503, detail="Analyzer files are incomplete")

    return {
        "status": "ok",
        "service": "music-analysis",
        "maxConcurrentAnalyses": MAX_CONCURRENT_ANALYSES,
    }


@app.post("/analyze")
async def analyze(
    file: UploadFile = File(...),
    x_api_key: str | None = Header(default=None),
):
    if API_KEY and x_api_key != API_KEY:
        raise HTTPException(status_code=401, detail="Invalid API key")

    suffix = Path(file.filename or "").suffix.lower()
    if suffix not in SUPPORTED_EXTENSIONS:
        raise HTTPException(status_code=415, detail="Unsupported audio format")

    temporary_path: Path | None = None
    try:
        with tempfile.NamedTemporaryFile(delete=False, suffix=suffix, dir="/tmp") as temporary_file:
            temporary_path = Path(temporary_file.name)
            uploaded_bytes = 0
            while chunk := await file.read(UPLOAD_CHUNK_SIZE):
                uploaded_bytes += len(chunk)
                if uploaded_bytes > MAX_UPLOAD_BYTES:
                    raise HTTPException(status_code=413, detail="Audio file is too large")
                temporary_file.write(chunk)

        if uploaded_bytes == 0:
            raise HTTPException(status_code=400, detail="Audio file is empty")

        async with ANALYSIS_SLOTS:
            payload, return_code, stderr = await run_analyzer(temporary_path)

        if return_code != 0 or payload.get("success") is not True:
            error_payload = dict(payload)
            error_payload.setdefault("success", False)
            error_payload.setdefault("error", "Analysis failed")
            if stderr:
                error_payload["serverDetails"] = stderr[-4000:]
            return JSONResponse(status_code=500, content=error_payload)

        return payload
    finally:
        await file.close()
        if temporary_path is not None:
            temporary_path.unlink(missing_ok=True)


async def run_analyzer(track_path: Path) -> tuple[dict, int, str]:
    process = await asyncio.create_subprocess_exec(
        sys.executable,
        str(SCRIPT_PATH),
        str(track_path),
        "--model-directory",
        str(MODEL_DIRECTORY),
        stdout=asyncio.subprocess.PIPE,
        stderr=asyncio.subprocess.PIPE,
        cwd=BASE_DIRECTORY,
    )

    try:
        stdout, stderr = await asyncio.wait_for(
            process.communicate(),
            timeout=ANALYSIS_TIMEOUT_SECONDS,
        )
    except TimeoutError:
        process.kill()
        await process.wait()
        raise HTTPException(status_code=504, detail="Analysis timed out")
    except asyncio.CancelledError:
        process.kill()
        await process.wait()
        raise

    stdout_text = stdout.decode("utf-8", errors="replace").strip()
    stderr_text = stderr.decode("utf-8", errors="replace").strip()
    try:
        payload = json.loads(stdout_text)
    except json.JSONDecodeError:
        raise HTTPException(
            status_code=500,
            detail={
                "error": "Analyzer returned invalid JSON",
                "stderr": stderr_text[-4000:],
            },
        )

    if not isinstance(payload, dict):
        raise HTTPException(status_code=500, detail="Analyzer returned an invalid response")
    return payload, process.returncode or 0, stderr_text
