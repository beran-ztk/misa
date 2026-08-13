# Resona Cloud

The compose stack runs PostgreSQL and the Resona ASP.NET Core API. The API is bound to localhost so a reverse proxy can provide public HTTPS.

1. Copy `.env.example` to `.env` and replace the database password.
2. Run `docker compose up -d --build` from this directory.
3. Verify `curl http://127.0.0.1:5080/health`.
4. Put Caddy, nginx, or another TLS reverse proxy in front of port 5080.

The schema is created idempotently at API startup. PostgreSQL data lives in the `postgres-data` volume. Back up that volume or use `pg_dump` before upgrades.
