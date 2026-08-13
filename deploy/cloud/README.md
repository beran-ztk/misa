# Resona Cloud

The compose stack runs PostgreSQL, the Resona ASP.NET Core API, and the Python/Essentia analysis API. Both APIs are bound to localhost so a reverse proxy can provide public HTTPS without exposing the container ports directly.

1. Copy `.env.example` to `.env` and replace the database password.
2. Run `docker compose up -d --build` from this directory.
3. Verify both services:
   - `curl http://127.0.0.1:5080/health`
   - `curl http://127.0.0.1:5081/health`
4. Put Caddy, nginx, or another TLS reverse proxy in front of ports 5080 and 5081. Separate subdomains such as `api.example.com` and `analyzer.example.com` keep the proxy configuration simple.

The schema is created idempotently at API startup. PostgreSQL data lives in the `postgres-data` volume. Back up that volume or use `pg_dump` before upgrades.

The analyzer intentionally runs one analysis at a time by default because TensorFlow inference is memory intensive. It returns MAEST genre scores, BPM, EBU R128 loudness/dynamics, and the MIREX emotional-character clusters used by Resona. Increase `MAX_CONCURRENT_ANALYSES` only after checking memory usage on the server.

`MUSIC_API_KEY` is optional infrastructure preparation. The current desktop client does not send this header yet, so leave it empty while the analyzer is reachable only through a trusted proxy or restricted network. Do not expose the analysis endpoint openly to the internet without adding client authentication or equivalent nginx access control.
