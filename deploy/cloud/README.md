# Resona Cloud

The compose stack runs PostgreSQL, the Resona ASP.NET Core API, and the Python/Essentia analysis API. Both APIs are bound to localhost so a reverse proxy can provide public HTTPS without exposing the container ports directly.

1. Copy `.env.example` to `.env` and replace the database password.
2. Run `docker compose up -d --build` from this directory.
3. Verify both services:
   - `curl http://127.0.0.1:5080/health`
   - `curl http://127.0.0.1:5081/health`
4. Put Caddy, nginx, or another TLS reverse proxy in front of ports 5080 and 5081. Separate subdomains such as `api.example.com` and `analyzer.example.com` keep the proxy configuration simple.

For nginx, the API virtual host must allow the same 25 MB request size as the ASP.NET Core server. Add this inside the `server` block for the Resona API and reload nginx:

```nginx
client_max_body_size 25m;
```

Without this setting nginx uses its much smaller default limit and full-library synchronization returns HTTP 413 (`Request Entity Too Large`).

## Public library API

The public read endpoints require no device credentials. List responses are ordered deterministically and use `offset`/`limit` pagination. The default limit is 50 and the maximum is 100.

```text
GET /api/v1/public/profiles?search=&offset=0&limit=50
GET /api/v1/public/profiles/{userId}
GET /api/v1/public/profiles/{userId}/image
GET /api/v1/public/profiles/{userId}/tracks?search=&offset=0&limit=50
```

After deploying, these commands provide a quick production check:

```bash
curl -sS 'https://api.resona-music.de/api/v1/public/profiles?limit=10'
curl -sS 'https://api.resona-music.de/api/v1/public/profiles/USER_ID'
curl -sS 'https://api.resona-music.de/api/v1/public/profiles/USER_ID/tracks?limit=10'
curl -fS 'https://api.resona-music.de/api/v1/public/profiles/USER_ID/image' -o profile.jpg
```

Profile search matches username and bio. Track search matches edited title, original title, and channel name. Missing profiles return HTTP 404; invalid pagination returns HTTP 400.

The schema is created idempotently at API startup. PostgreSQL data lives in the `postgres-data` volume. Back up that volume or use `pg_dump` before upgrades.

The analyzer intentionally runs one analysis at a time by default because TensorFlow inference is memory intensive. It returns MAEST genre scores, BPM, EBU R128 loudness/dynamics, and the MIREX emotional-character clusters used by Resona. Increase `MAX_CONCURRENT_ANALYSES` only after checking memory usage on the server.

`MUSIC_API_KEY` is optional infrastructure preparation. The current desktop client does not send this header yet, so leave it empty while the analyzer is reachable only through a trusted proxy or restricted network. Do not expose the analysis endpoint openly to the internet without adding client authentication or equivalent nginx access control.
