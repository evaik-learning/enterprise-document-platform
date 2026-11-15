# 03 - Client demo script

A 25-30 minute walkthrough that shows the whole product: secure sign-in, templates, document generation, approval workflow, e-signatures, notifications, audit trail and administration. It works on the local stack ([01](01-Local-Setup-and-Run.md)) and on Azure ([02](02-Azure-Deployment-Guide.md)).

## Before the meeting (10 minutes)

- Start the stack (`docker compose up --build -d`) and run `.\scripts\Smoke-Test.ps1 -BaseUrl http://localhost:8080`. Use the Docker option: it includes the Service Bus emulator, so notifications and audit events flow.
- Open three browser profiles/windows (or one normal and two private windows) at http://localhost:8080 so you can be **Alex**, **Priya** and **Sam** at once. On Azure, use three real Entra users with the matching app roles.
- Have `docs/test-data/sample-template.docx` ready on the desktop.
- Prepare the organization once (so the demo starts clean and fast):
  1. As **Alex**: create organization *"Contoso Legal"*.
  2. *Administration -> Users*: add Priya (`00000000-0000-0000-0000-0000000000a2`) and Sam (`...a3`) as members (Priya as *Administrator* so she sees Approvals).
  3. Sign in as Priya and Sam once and select *Contoso Legal*.

Cast: **Alex Admin** (owner), **Priya Approver**, **Sam Signer**.

## The story

> "Contoso Legal issues contracts. They need consistent templates, controlled generation, a manager's approval before anything goes out, a legally traceable signature, and a full audit trail - without e-mailing Word files around."

### 1. Sign-in and tenancy (2 min) - *Alex*

- Open the site -> sign-in page. Say: in production this is Microsoft Entra ID single sign-on; the app never sees passwords; tokens stay on the server (BFF pattern, secure cookie). Locally you click a demo persona.
- Select the organization. Point out the organization name and the user menu: every screen and API call is scoped to one organization (multi-tenant isolation).
- **Dashboard**: pending approvals, recent documents, signature status, notifications - one aggregated call from the Gateway.
- Show **global search** (top bar) and the notification bell.

### 2. Templates (5 min) - *Alex*

- *Templates -> New template* (a 5-step wizard: Details, Upload file, Placeholders, Validate, Activate). Name it "Mutual NDA" and upload `sample-template.docx`.
- Show the detected **placeholders**, run **validation**, then **Activate template**.
- Open the template: version history; **Upload new version** creates a new version that must be validated and activated before use (nothing is overwritten).
- Talking points: file checks on upload, permission-controlled actions (buttons are hidden/disabled when you lack the permission).

### 3. Generate a document (4 min) - *Alex*

- *Documents -> New document*: pick the active template, fill the placeholder values (the form is generated from the template), generate.
- Open the **document workspace**: details, versions, preview, activity timeline.
- Say: generation, storage (Azure Blob) and the event published to the platform are decoupled services.
- Be honest if asked: document merging is a placeholder in this build; the production generator plugs into the same flow.

### 4. Workflow and approval (5 min) - *Alex then Priya*

- *Workflows*: **before the meeting**, create a workflow (*New workflow*), add states and transitions, **Validate** and **Publish** a version (keep it to 2-3 states). In the demo, open it and show the designer.
- From the document workspace click **Start workflow**, choose the published workflow.
- Switch to **Priya**: bell shows a new notification -> *Approvals* -> open the task. Show the document context, then **Approve** (or **Reject** with a reason to show the other path; "Request changes" is recorded as a rejection with a prefix).
- Back as **Alex**: the workflow instance page shows the timeline with who did what and when; the document status changed.

### 5. Digital signature (5 min) - *Alex then Sam*

- From the document workspace click **Send for signature**: add **Sam** as signer (name *Sam Signer*, email `sam.signer@local.test`), choose *Sequential* or *Parallel* order, review and **Send for signature**.
- Switch to **Sam**: *Signatures* -> open the request -> review -> **Sign**. Show the status moving to *Completed* for Alex.
- Mention: decline with reason, **Resend** invitation, cancel; signing events are written to the audit trail.

### 6. Notifications, audit and compliance (3 min) - *Alex*

- *Notifications*: unread/all, mark read, preferences (channels and event types).
- *Audit*: filter by entity, action, user and date; follow one **correlation id** from upload to signature. Say who is allowed to see this (Auditor / admin permission).
- Mention retention, tamper-evident append-only logging and export as the next step if the client needs it.

### 7. Administration (2 min) - *Alex*

- *Administration -> Organization / Users / Roles / Settings*: members and roles, capabilities, settings. A user without the permission does not see these pages.

### 8. Architecture and operations close (3 min)

Show the diagram in [02](02-Azure-Deployment-Guide.md): React SPA -> Gateway -> 9 services, SQL, Blob, Service Bus, Key Vault, Entra ID. Then:

- Dev and Prod from the same code; configuration per environment lives in `infra/*.bicepparam` and `pipelines/variables/*.yml`.
- CI/CD: build, test, security audit, image build, DB migration, rollout, smoke test; **approval gate before Prod**; one-click **rollback**.
- Observability: Application Insights + Log Analytics; health endpoints; correlation ids end to end.
- Security: Entra ID, no secrets in code (Key Vault references, managed identity for ACR), HTTPS only, per-organization data isolation, rate limiting and security headers at the Gateway.

## Likely questions

| Question | Answer |
|---|---|
| Can we use our own identity provider? | Entra ID is built in (supports guests and federation). Other OIDC providers need Gateway configuration changes. |
| How is data isolated between customers? | Every record carries the organization id; services filter by the organization in the token. |
| Does it scale? | Stateless containers behind App Service autoscale; SQL and Service Bus scale by tier. |
| Real-time updates? | Currently polling (30-60 s). Real-time push (SignalR) is on the roadmap. |
| Is the e-signature legally binding (eIDAS/ESIGN)? | The platform records signer identity, time and audit evidence. Qualified/advanced signature providers can be plugged in; this needs a legal review per jurisdiction. |
| What is not finished? | See *Known limitations* in [01](01-Local-Setup-and-Run.md#known-limitations-be-upfront-with-stakeholders). |

## If something goes wrong during the demo

| Symptom | Quick recovery |
|---|---|
| Blank page / 502 | `docker compose ps`; `docker compose restart gateway web`. |
| No notification yet | Wait ~10 s (polling); confirm Service Bus emulator is running (`docker compose logs servicebus-emulator`). |
| Approvals menu missing for Priya | She is not an Administrator in the organization; add her on *Administration -> Users*. |
| Action button disabled | The persona lacks the permission - that is the permission model working; switch to Alex. |
