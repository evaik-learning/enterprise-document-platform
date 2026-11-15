# CI/CD pipelines (Azure DevOps)

| File | Purpose |
| --- | --- |
| `ci.yml` | Pull requests and feature branches: build, test, lint, audit. Never deploys. |
| `cd.yml` | `develop` -> Dev. `main` -> Dev -> (approval) -> Prod. Manual run supports skipping an environment and **rollback** by image tag. |
| `templates/build-and-test.yml` | .NET build/test/NuGet audit; web lint/typecheck/test/build/npm audit |
| `templates/deploy-environment.yml` | One environment: Bicep deploy -> build & push 11 images -> DB migration, rollout, health wait, smoke test |
| `variables/common.yml` | Tool versions shared by all pipelines |
| `variables/dev.yml`, `variables/prod.yml` | Service connection, resource group, region, bicep param file, variable group (`edp-dev` / `edp-prod`) |

Setup (service connections, variable groups, Environments with approvals): [guide 02, section 4](../docs/guides/02-Azure-Deployment-Guide.md).

The heavy lifting lives in scripts you can also run by hand: `scripts/deploy/build-push.sh`, `scripts/deploy/release.sh`, `scripts/database/Apply-EdpMigrations.ps1`, `scripts/Smoke-Test.ps1`.
