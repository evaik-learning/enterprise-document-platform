# Scripts

| Script | Purpose |
| --- | --- |
| `local/Start-Local.ps1`, `local/Stop-Local.ps1` | Run/stop the whole platform natively (no Docker). Logs in `.local/logs`. |
| `Smoke-Test.ps1 -BaseUrl <url>` | End-to-end API smoke test (local with demo sign-in; health + auth checks on Azure) |
| `database/Apply-EdpMigrations.ps1` | Apply (or script with `-ScriptOnly`) the EF Core migrations to `EdpDb` |
| `deploy/build-push.sh` | Build one image and push it to the environment's ACR |
| `deploy/release.sh` | Point all apps at an image tag, restart, wait for health (also used for rollback) |
| `build.ps1`, `test.ps1` | Solution build / test helpers |

See [guide 01](../docs/guides/01-Local-Setup-and-Run.md) and [guide 02](../docs/guides/02-Azure-Deployment-Guide.md).
