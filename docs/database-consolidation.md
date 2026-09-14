# Edp database consolidation

The services now use `Edp.Persistence.EdpDbContext` and the single `EdpDb` connection string. The migration assembly is `Edp.Persistence`, with one migration history for the platform database.

## Apply migrations

Run this from the repository root after the Azure SQL database and credentials have been provisioned:

```powershell
.\scripts\database\Apply-EdpMigrations.ps1 -ConnectionString $env:EDP_DB_CONNECTION
```

Generate an idempotent SQL artifact for approval instead of applying it directly:

```powershell
.\scripts\database\Apply-EdpMigrations.ps1 -ConnectionString $env:EDP_DB_CONNECTION -ScriptOnly
```

Production API startup does not apply migrations. Migration execution belongs in the deployment job, before the API rollout, so multiple service instances cannot race to change the schema.

## Existing database data migration

The repository contains separate historical databases (`DocumentDb`, `TemplateDb`, `WorkflowDb`, `OrganizationDb`, `IdentityDb`, and `AuditDb`) and no connection credentials were supplied for them. Do not run a blind cross-database copy.

Before cutover:

1. Take backups of each source database.
2. Provision `edp-platform-db` and apply `InitialUnifiedModel`.
3. Copy each source table into its matching target table. Copy Template and Workflow outboxes into `template.OutboxMessages` and `workflow.OutboxMessages` respectively.
4. Validate row counts, primary keys, unique indexes, timestamps, and representative records.
5. Freeze writes, run a final delta copy, switch all services to `EdpDb`, and run smoke tests.
6. Retain the source databases read-only until rollback validation is complete.

The data copy is intentionally not executed by the application startup or this code change because it is an irreversible environment operation and requires explicit Azure SQL access.
