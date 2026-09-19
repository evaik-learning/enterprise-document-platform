# Digital Signature Service

The Digital Signature service manages signing requests, signers, signature fields, lifecycle transitions, audit actions, and integration with Workflow.

## Local Configuration

The API reads these settings:

- `ConnectionStrings:SigningDb`: SQL Server database for signing data and the transactional outbox.
- `ConnectionStrings:ServiceBus`: optional Azure Service Bus connection string. When empty, the shared null publisher is used.
- `ConnectionStrings:Storage`: Blob Storage connection string. Development can use `UseDevelopmentStorage=true` with Azurite.
- `ServiceBus:SigningTopic`: topic used for signing integration events; defaults to `signing-events`.
- `DigitalSignature:Provider`: provider name; the local implementation is `LocalDemo`.

Apply migrations with:

```powershell
dotnet ef database update --project src/Services/DigitalSignature/Edp.DigitalSignature.Infrastructure/Edp.DigitalSignature.Infrastructure.csproj --startup-project src/Services/DigitalSignature/Edp.DigitalSignature.Api/Edp.DigitalSignature.Api.csproj
```

## Messaging

Signing lifecycle changes are written to `OutboxMessages` in the same EF save operation as the aggregate change. `OutboxBackgroundService` publishes pending `EventEnvelope` messages to `signing-events`. Workflow consumes `SigningRequestCompletedEvent` and advances a configured `signing-completed` transition.

## Storage

Original and signed document paths are organization-scoped and kept separate:

```text
organizations/{organizationId}/documents/{documentId}/versions/{versionId}/original.pdf
organizations/{organizationId}/documents/{documentId}/versions/{versionId}/signed.pdf
```

SHA-256 hashes are calculated during storage operations. Signed documents must never overwrite the original blob.

## Local Provider

`LocalDemo` is an in-memory provider intended for development and automated tests. It is not durable across process restarts and must be replaced by a provider adapter for production signing transactions. Activation retrieves and verifies the organization-scoped original blob before creating the provider envelope. Sign and cancel operations use deterministic provider transaction keys, and completion downloads the provider output, stores it as a separate signed blob, and publishes its SHA-256 hash.

## Endpoints

The `SigningRequestsController` exposes authenticated endpoints for creating, activating, listing, retrieving, signing, declining, cancelling, resending invitations, and reading audit history.
