import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

from fastapi import FastAPI, File, Header, HTTPException, UploadFile
from fastapi.responses import JSONResponse


BASE_DIR = Path("/root/music")
SCRIPT_PATH = BASE_DIR / "Scripts" / "analyze.py"
MODEL_DIRECTORY = BASE_DIR / "Models" / "Essentia" / "DiscogsMAEST"
API_KEY = os.environ.get("MUSIC_API_KEY", "nu1in128hd12812fnn891hf2891nf12h8f19")

app = FastAPI(title="Music Analysis API")


@app.get("/health")
def health():
    return {
        "success": True,
        "service": "music-analysis",
        "scriptExists": SCRIPT_PATH.exists(),
        "modelDirectoryExists": MODEL_DIRECTORY.exists(),
    }


@app.post("/analyze")
async def analyze(
    file: UploadFile = File(...),
    x_api_key: str | None = Header(default=None),
    include_experimental: bool = True,
    top: int = 20,
):
    if API_KEY and x_api_key != API_KEY:
        raise HTTPException(status_code=401, detail="Invalid API Key")

    suffix = Path(file.filename or "upload").suffix or ".audio"

    with tempfile.NamedTemporaryFile(delete=False, suffix=suffix, dir="/tmp") as tmp:
        tmp_path = Path(tmp.name)
        content = await file.read()
        tmp.write(content)

    command = [
        sys.executable,
        str(SCRIPT_PATH),
        str(tmp_path),
        "--top",
        str(top),
        "--model-directory",
        str(MODEL_DIRECTORY),
    ]

    if include_experimental:
        command.append("--include-experimental")

    try:
        result = subprocess.run(
            command,
            cwd=str(BASE_DIR),
            capture_output=True,
            text=True,
            timeout=600,
        )

        stdout = result.stdout.strip()
        stderr = result.stderr.strip()

        try:
            payload = json.loads(stdout)
        except json.JSONDecodeError:
            raise HTTPException(
                status_code=500,
                detail={
                    "error": "Analyzer did not return valid JSON",
                    "stdout": stdout,
                    "stderr": stderr,
                    "returncode": result.returncode,
                },
            )

        if result.returncode != 0 or payload.get("success") is False:
            return JSONResponse(
                status_code=500,
                content={
                    **payload,
                    "stderr": stderr,
                    "returncode": result.returncode,
                },
            )

        return payload

    finally:
        try:
            tmp_path.unlink(missing_ok=True)
        except Exception:
            pass
