# Docker

Container assets for the Enterprise Document Platform. The full local stack is defined in the root `docker-compose.yml` (see [guide 01](../docs/guides/01-Local-Setup-and-Run.md)).

| File | Purpose |
| --- | --- |
| `Dockerfile.service` | One parametrised image for the Gateway and every service. Build args: `PROJECT` (path under `src/`), `ASSEMBLY` (dll name). Build from the repository root. |
| `Dockerfile.migrator` | One-shot job that applies the EF Core migrations to `EdpDb` (reads `EdpDb_Connection`). |
| `servicebus/Config.json` | Topics/subscriptions for the local Service Bus emulator. Keep in sync with `serviceBusTopics` in `infra/main.bicep`. |
| `../src/Web/Edp.Web/Dockerfile` | Web image: React build served by nginx, proxies `/api` and `/bff` to `GATEWAY_URL`. |

```powershell
docker build -f docker/Dockerfile.service --build-arg PROJECT=Gateway/Edp.Gateway/Edp.Gateway.csproj --build-arg ASSEMBLY=Edp.Gateway -t edp-gateway .
docker build -t edp-web src/Web/Edp.Web
```

The compose defaults (SQL `sa` password, JWT key) are for local use only. `appsettings.Development.json` files are excluded from images by `.dockerignore`.
