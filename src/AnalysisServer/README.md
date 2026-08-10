# Resona Analysis Server

GPU-capable FastAPI service for the desktop application's `POST /analyze` request.
The analyzer always returns the MAEST output and all available additional model
outputs; no experimental command-line or query parameter is required.

## Local build and smoke test

Run these commands from the repository root:

```powershell
docker build -f src/AnalysisServer/Dockerfile -t resona-analysis:gpu .
docker run --rm -p 8000:8000 resona-analysis:gpu
```

The service can run without a GPU for a functional smoke test. Verify it with:

```powershell
Invoke-RestMethod http://localhost:8000/health
curl.exe -X POST -F "file=@C:\path\track.m4a" http://localhost:8000/analyze
```

## GPU server

The Linux host needs a working NVIDIA driver, Docker Engine, Docker Compose, and
the NVIDIA Container Toolkit. Copy the repository or an exported image to the
server. From `src/AnalysisServer`, create `.env` from `.env.example`, then run:

```bash
docker compose -f compose.yaml up -d --build
docker compose -f compose.yaml ps
curl http://127.0.0.1:8000/health
```

The Compose definition assigns every visible GPU to the container. Keep
`MAX_CONCURRENT_ANALYSES=1` initially: additional simultaneous TensorFlow jobs
can consume GPU memory without increasing throughput. Increase it only after a
measured test on the target GPU.

To transfer a prebuilt image instead of the source tree:

```powershell
docker save resona-analysis:gpu -o src\AnalysisServer\resona-analysis-gpu.tar
```

On the Linux server:

```bash
docker load -i resona-analysis-gpu.tar
docker compose -f compose.yaml up -d --no-build
```

Port `8000` must be allowed by the server firewall or cloud firewall for the
desktop computer. The optional `MUSIC_API_KEY` is intentionally empty because
the current desktop client does not send `X-Api-Key`; do not expose the service
widely on the public internet without adding authentication or restricting the
source IP in the firewall.
