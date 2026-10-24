# Phase 10 --- Unified Web Application, UX/UI & End-to-End Platform Integration

**Project:** Enterprise Document Automation Platform\
**Phase:** 10\
**Status:** Detailed Implementation Requirement\
**Primary Goal:** Build the complete React web application and connect
the user experience to the API Gateway and all backend services
delivered in Phases 4--9.

------------------------------------------------------------------------

## 1. Phase 10 Objective

Phase 10 converts the backend platform into a complete enterprise
application.

The implementation must provide:

-   Production-ready React + TypeScript web application.
-   Authentication and session handling.
-   Organization/tenant context.
-   Permission-aware navigation and actions.
-   Dashboard.
-   Template management.
-   User-managed placeholder management.
-   Document generation and management.
-   Workflow definition/instance UI.
-   Approval inbox and approval actions.
-   Digital signature preparation and signing experience.
-   Notification center.
-   Audit trail and activity timeline.
-   Document/workflow/signature workspaces.
-   File upload, preview and download.
-   Search, filtering, sorting and server-side pagination.
-   Async operation handling.
-   Real-time/near-real-time status refresh where supported.
-   Consistent loading, validation, error, empty and success states.
-   Accessibility and responsive design.
-   Automated UI/API/E2E tests.
-   CI/CD integration.

The browser must communicate with the **API Gateway**, not directly with
internal microservices.

------------------------------------------------------------------------

# 2. Phase 10 Scope

## 2.1 Frontend

Implement:

``` text
Application Shell
Authentication
Organization Context
Dashboard
Templates
Placeholder Management
Documents
Document Generation
Workflows
Approvals
Digital Signatures
Notifications
Audit
Administration
User Profile
Search
File Preview/Download
Activity Timeline
```

## 2.2 Backend Integration

Integrate the UI through the Gateway with:

``` text
Identity Service
Organization Service
Template Service
Document Service
Workflow Service
Signature Service
Notification Service
Audit Service
Storage
Service Bus / Async Processing
```

## 2.3 Quality

Implement:

-   Unit tests.
-   Component tests.
-   API integration tests.
-   End-to-end tests.
-   Accessibility checks.
-   Security checks.
-   Performance validation.

------------------------------------------------------------------------

# 3. Existing Architecture

``` text
                         ┌─────────────────────┐
                         │      React Web       │
                         │                     │
                         │ Dashboard           │
                         │ Templates           │
                         │ Documents           │
                         │ Workflows           │
                         │ Approvals           │
                         │ Signatures          │
                         │ Notifications       │
                         │ Audit               │
                         └──────────┬──────────┘
                                    │
                                  HTTPS
                                    │
                                    v
                         ┌─────────────────────┐
                         │     API Gateway     │
                         │                     │
                         │ Auth                │
                         │ Routing             │
                         │ Authorization       │
                         │ Tenant Context      │
                         │ Aggregation         │
                         │ Correlation         │
                         └──────────┬──────────┘
                                    │
          ┌─────────────────────────┼─────────────────────────┐
          │                         │                         │
          v                         v                         v
 ┌────────────────┐       ┌────────────────┐       ┌────────────────┐
 │ Template       │       │ Document       │       │ Workflow       │
 │ Service        │       │ Service        │       │ Service        │
 └────────────────┘       └────────────────┘       └────────────────┘
          │                         │                         │
          └─────────────────────────┼─────────────────────────┘
                                    │
                    ┌───────────────┼────────────────┐
                    v               v                v
             ┌────────────┐  ┌────────────┐  ┌────────────┐
             │ Signature  │  │Notification│  │   Audit    │
             │ Service    │  │ Service    │  │  Service   │
             └────────────┘  └────────────┘  └────────────┘
                                    │
                                    v
                              Service Bus
                                    │
                                    v
                           Workers / Functions
                                    │
                   ┌────────────────┼────────────────┐
                   v                v                v
              Azure SQL        Blob Storage       Cosmos DB
```

------------------------------------------------------------------------

# 4. Core Architecture Rules

## 4.1 Browser → Gateway Only

Never expose internal service URLs to the browser.

Correct:

``` text
React
  ↓
Gateway
  ↓
Service
```

Incorrect:

``` text
React
  ├── Template Service
  ├── Document Service
  ├── Workflow Service
  └── Signature Service
```

## 4.2 Backend Is the Source of Truth

The backend owns:

-   Authorization.
-   Tenant isolation.
-   Workflow state.
-   Document state.
-   Signature state.
-   Business validation.
-   Audit records.

The frontend must not recreate business state-transition logic.

------------------------------------------------------------------------

# 5. Frontend Project Structure

Recommended:

``` text
src/Web/
├── src/
│   ├── app/
│   │   ├── App.tsx
│   │   ├── router.tsx
│   │   ├── providers.tsx
│   │   └── config.ts
│   │
│   ├── api/
│   │   ├── httpClient.ts
│   │   ├── gatewayClient.ts
│   │   ├── contracts/
│   │   ├── endpoints/
│   │   └── hooks/
│   │
│   ├── auth/
│   │   ├── AuthProvider.tsx
│   │   ├── ProtectedRoute.tsx
│   │   ├── authService.ts
│   │   └── permissions.ts
│   │
│   ├── components/
│   │   ├── layout/
│   │   ├── navigation/
│   │   ├── forms/
│   │   ├── tables/
│   │   ├── dialogs/
│   │   ├── files/
│   │   ├── timeline/
│   │   └── feedback/
│   │
│   ├── features/
│   │   ├── dashboard/
│   │   ├── templates/
│   │   ├── placeholders/
│   │   ├── documents/
│   │   ├── workflows/
│   │   ├── approvals/
│   │   ├── signatures/
│   │   ├── notifications/
│   │   ├── audit/
│   │   ├── organization/
│   │   └── profile/
│   │
│   ├── hooks/
│   ├── state/
│   ├── routes/
│   ├── types/
│   ├── utils/
│   └── main.tsx
│
├── tests/
├── public/
└── package.json
```

Use feature-oriented organization. Shared UI components must not contain
business-specific service logic.

------------------------------------------------------------------------

# 6. Application Shell

## 6.1 Desktop

``` text
+------------------------------------------------------------------+
| Logo | Organization | Search             | Notifications | User |
+-------------+----------------------------------------------------+
|             |                                                    |
| Dashboard   |                                                    |
| Templates   |              Application Content                 |
| Documents   |                                                    |
| Workflows   |                                                    |
| Approvals   |                                                    |
| Signatures  |                                                    |
| Notifications|                                                   |
| Audit       |                                                    |
| Admin       |                                                    |
|             |                                                    |
+-------------+----------------------------------------------------+
```

## 6.2 Mobile

``` text
+---------------------------------------+
| Menu | Logo | Bell | Profile          |
+---------------------------------------+
|                                       |
|             Page Content              |
|                                       |
+---------------------------------------+
```

------------------------------------------------------------------------

# 7. Navigation

Main navigation:

``` text
Dashboard

Templates
Documents
Workflows
Approvals
Signatures
Notifications
Audit

Administration
  ├── Organization
  ├── Users
  ├── Roles
  └── Settings
```

Navigation visibility is permission-aware.

Important:

> Hiding a button or route is not security. Backend authorization
> remains mandatory.

------------------------------------------------------------------------

# 8. Authentication Flow

## 8.1 Login

``` text
User
  ↓
Login
  ↓
Identity Provider
  ↓
Authentication
  ↓
Application Session
  ↓
GET /api/v1/me
  ↓
User + Organization + Permissions
  ↓
Dashboard
```

Use the identity architecture from Phase 4.

Do not implement username/password storage inside React.

------------------------------------------------------------------------

# 9. Session Expiration

When Gateway returns:

``` http
401 Unauthorized
```

the client must:

1.  Attempt the supported session/token renewal flow.
2.  If successful, retry the request where safe.
3.  If renewal fails, clear session state.
4.  Redirect to login.
5.  Preserve the intended route where appropriate.

Never log access or refresh tokens.

------------------------------------------------------------------------

# 10. Organization/Tenant Context

After authentication:

``` http
GET /api/v1/me
```

Example:

``` json
{
  "userId": "user-001",
  "name": "John Doe",
  "organizationId": "org-001",
  "roles": [
    "DocumentManager"
  ],
  "permissions": [
    "template.read",
    "template.write",
    "document.read",
    "document.generate"
  ]
}
```

If users can belong to multiple organizations:

``` text
Organization Selector
---------------------
Acme Ltd
Contoso Ltd
Example Corp
```

Changing organization must:

-   Update tenant context.
-   Refresh permissions.
-   Clear tenant-scoped cache.
-   Reload dashboard.
-   Reload current resource data.
-   Prevent cross-tenant data leakage.

------------------------------------------------------------------------

# 11. Permission Model

Define frontend constants:

``` typescript
export const Permissions = {
  TemplateRead: "template.read",
  TemplateWrite: "template.write",
  DocumentRead: "document.read",
  DocumentGenerate: "document.generate",
  WorkflowRead: "workflow.read",
  WorkflowExecute: "workflow.execute",
  WorkflowApprove: "workflow.approve",
  SignatureRead: "signature.read",
  SignatureExecute: "signature.execute",
  NotificationRead: "notification.read",
  AuditRead: "audit.read",
  OrganizationAdmin: "organization.admin"
} as const;
```

Reusable component:

``` tsx
<PermissionGate permission="template.write">
    <CreateTemplateButton />
</PermissionGate>
```

Backend authorization must still validate every protected operation.

------------------------------------------------------------------------

# 12. Global Search

Search:

``` text
Templates
Documents
Workflows
Signatures
Users - only where authorized
```

Example UX:

``` text
+------------------------------------------------+
| Search documents, templates, workflows...      |
+------------------------------------------------+
                  ↓
Invoice
------------------------------------
Templates       3 results
Documents      12 results
Workflows       4 results
------------------------------------
```

Gateway:

``` http
GET /api/v1/search?q={query}&type={type}
```

Results must respect tenant and permission boundaries.

------------------------------------------------------------------------

# 13. Dashboard UX

Dashboard widgets:

``` text
+----------------+ +----------------+ +----------------+
| Templates      | | Documents      | | Pending Tasks  |
| 42             | | 1,284          | | 17             |
+----------------+ +----------------+ +----------------+

+----------------+ +----------------+ +----------------+
| Awaiting Sign  | | Completed      | | Notifications  |
| 8              | | 964            | | 5 unread       |
+----------------+ +----------------+ +----------------+
```

Additional sections:

-   Recent documents.
-   Pending approvals.
-   Pending signatures.
-   Recent activity.
-   Recent notifications.

------------------------------------------------------------------------

# 14. Dashboard Backend Integration

Prefer a Gateway aggregation endpoint:

``` http
GET /api/v1/dashboard
```

Gateway:

``` text
GET /dashboard
     ↓
Gateway
     ├── Document Service
     ├── Workflow Service
     ├── Signature Service
     └── Notification Service
```

The Gateway should aggregate data without owning business rules.

------------------------------------------------------------------------

# 15. Template Management

## 15.1 Template List

Columns:

``` text
Name
Type
Current Version
Status
Owner
Updated Date
Actions
```

Filters:

``` text
Search
Status
Type
Owner
Date Range
```

Actions:

``` text
View
Edit
Upload Version
Validate
Activate
Deactivate
Generate Document
```

------------------------------------------------------------------------

# 16. Template Creation Flow

``` text
Templates
   ↓
Create Template
   ↓
Basic Details
   ↓
Upload File
   ↓
Detect Placeholders
   ↓
Review Placeholders
   ↓
Validate
   ↓
Save Draft
   ↓
Activate
```

Backend integration:

``` text
POST /api/v1/templates
POST /api/v1/templates/{id}/versions
GET  /api/v1/templates/{id}/placeholders
POST /api/v1/templates/{id}/validate
POST /api/v1/templates/{id}/activate
```

Exact routes must follow the Gateway's actual API contract.

------------------------------------------------------------------------

# 17. Placeholder Management

Phase 5 requires full user-managed Placeholder CRUD.

Screen:

``` text
Placeholder Definitions

+-------------------------------------------------------------+
| Search | Type | Status             | + New Placeholder      |
+-------------------------------------------------------------+

| Name          | Type | Required | Default | Status          |
| CustomerName  | Text | Yes      |         | Active          |
| InvoiceDate   | Date | Yes      |         | Active          |
| Address       | Text | No       |         | Active          |
+-------------------------------------------------------------+
```

Create/edit form:

``` text
Name
Display Name
Data Type
Required
Default Value
Validation Rules
Description
Status
```

Validation:

``` text
Client validation
       ↓
Gateway
       ↓
Template Service
       ↓
Domain validation
```

Server validation is authoritative.

------------------------------------------------------------------------

# 18. Document Management

Document list:

``` text
Document
Template
Version
Status
Workflow Status
Signature Status
Created By
Created Date
Updated Date
Actions
```

Actions:

``` text
Open
Preview
Download
Start Workflow
View Timeline
Send for Signature
Cancel
```

Only valid actions for the current backend state and user permissions
should be displayed.

------------------------------------------------------------------------

# 19. Document Generation UX

``` text
Select Template
       ↓
Select Version
       ↓
Enter Placeholder Values
       ↓
Validate
       ↓
Generate
       ↓
Processing
       ↓
Generated
       ↓
Preview / Download / Start Workflow
```

For asynchronous generation:

``` text
POST Generate
       ↓
202 Accepted
       ↓
Processing
       ↓
Poll or receive event
       ↓
Completed
```

Never keep the browser request open indefinitely for long-running
generation.

------------------------------------------------------------------------

# 20. Document Detail Workspace

Primary tabs:

``` text
Overview
Preview
Workflow
Signatures
Activity
Audit
```

Layout:

``` text
+---------------------------------------------------------------+
| Document Name                     Status: Completed           |
+--------------------------+------------------------------------+
|                          | Overview                           |
|                          | Workflow                           |
|       Preview            | Signatures                         |
|                          | Activity                           |
|                          | Audit                              |
+--------------------------+------------------------------------+
| Download | Workflow | Signature | More                     |
+---------------------------------------------------------------+
```

This is a central Phase 10 screen.

------------------------------------------------------------------------

# 21. Unified Document Timeline

Timeline:

``` text
Document Created
      ↓
Template Version Selected
      ↓
Document Generated
      ↓
Workflow Started
      ↓
Review Completed
      ↓
Approval Completed
      ↓
Signature Requested
      ↓
Signature Completed
      ↓
Document Completed
```

Recommended Gateway endpoint:

``` http
GET /api/v1/documents/{documentId}/timeline
```

Gateway can aggregate:

``` text
Document Service
Workflow Service
Signature Service
Audit Service
```

The browser should not orchestrate four internal services itself.

------------------------------------------------------------------------

# 22. Workflow UX

Phase 7 implemented a real workflow/state-machine engine. Phase 10 must
represent that state machine faithfully.

## 22.1 Workflow Definition

For initial UI implementation:

``` text
Workflow Name
Description
Trigger
Steps
```

Example:

``` text
1. Review
2. Approval
3. Signature
4. Completion
```

Step configuration:

``` text
Step Name
Step Type
Assignee
Role
Required
Order
Timeout
Escalation
```

------------------------------------------------------------------------

# 23. Workflow State Visualization

Example:

``` text
Draft
  |
  v
Review
  |
  v
Approval
  |
  v
Signature
  |
  v
Completed
```

Current state must be retrieved from Workflow Service.

Do not calculate state transitions in React.

------------------------------------------------------------------------

# 24. Workflow Instance Detail

``` text
Workflow: Contract Approval

Status: Awaiting Approval

Timeline
----------------------------------
✓ Started
✓ Review completed
● Approval pending
○ Signature
○ Completed
----------------------------------

Current Task
Assigned To: Finance Manager

[Approve] [Reject] [Request Changes]
```

Gateway:

``` http
GET  /api/v1/workflows/{id}
POST /api/v1/workflows/{id}/actions
```

Action request should use an idempotency key when supported.

------------------------------------------------------------------------

# 25. Approval Inbox

Screen:

``` text
My Pending Approvals

| Document | Workflow | Requester | Due | Action |
| Contract A | Approval | User A | 20 Sep | Review |
```

Approval detail:

``` text
Document Preview
        ↓
Workflow Context
        ↓
Comments
        ↓
History
        ↓
Approve / Reject / Request Changes
```

Backend:

``` text
React
  ↓
Gateway
  ↓
Workflow Service
  ↓
State Machine
  ↓
Service Bus
  ├── Notification
  ├── Audit
  └── Next Workflow Step
```

------------------------------------------------------------------------

# 26. Approval Action Flow

``` text
Open Approval
      ↓
Load workflow
      ↓
Load document
      ↓
Load available actions
      ↓
Review
      ↓
Approve / Reject / Request Changes
      ↓
POST action
      ↓
Backend validates current state + permission
      ↓
Workflow transition
      ↓
Persist state
      ↓
Publish event
      ↓
UI refresh
```

Do not optimistically change approval state before backend confirmation.

------------------------------------------------------------------------

# 27. Digital Signature UX

## 27.1 Signature Preparation

``` text
Document
   ↓
Start Signature
   ↓
Select Signers
   ↓
Configure Signing Order
   ↓
Required / Optional
   ↓
Review
   ↓
Send for Signature
```

Signer:

``` text
Name
Email
Role
Signing Order
Required
```

------------------------------------------------------------------------

# 28. Signer Experience

``` text
Signature Request
--------------------------------
Document
Sender
Expiry
Required Actions
--------------------------------

[Open Document]

Signature Fields

Name
Date
Signature

[Sign]
```

The backend provides the secure signing session.

Do not expose permanent signing credentials or secrets in the browser.

------------------------------------------------------------------------

# 29. Signature Status

``` text
✓ Request Created
✓ Sent to Signer 1
✓ Signer 1 Completed
● Awaiting Signer 2
○ Completed
```

Backend is the source of truth.

------------------------------------------------------------------------

# 30. Notification Center

Header:

``` text
🔔 5
```

Dropdown:

``` text
Notifications
--------------------------------
Approval required
Contract A requires review

Signature completed
Contract B was signed

Document generated
Invoice 2026 generated

[View All]
```

API:

``` http
GET  /api/v1/notifications
POST /api/v1/notifications/{id}/read
POST /api/v1/notifications/read-all
```

------------------------------------------------------------------------

# 31. Notification-to-Resource Navigation

Notification payload:

``` json
{
  "id": "notification-1",
  "type": "ApprovalRequired",
  "resourceType": "Workflow",
  "resourceId": "workflow-123"
}
```

Flow:

``` text
Notification
   ↓
Resolve resource
   ↓
Open /workflows/workflow-123
```

Authorization must still be checked by the backend.

------------------------------------------------------------------------

# 32. Notification Preferences

User can configure:

``` text
Email Notifications       ON
In-App Notifications      ON
Workflow Notifications    ON
Signature Notifications   ON
Audit Notifications       OFF
```

Persist through Notification Service.

------------------------------------------------------------------------

# 33. Audit UX

Audit is different from normal activity.

## Activity

Human-readable:

``` text
10:42 Document generated
10:44 Workflow started
10:50 Review completed
11:03 Signature requested
11:20 Signed by John
```

## Audit

Detailed:

``` text
Timestamp
Actor
Action
Resource Type
Resource ID
Source
Correlation ID
Metadata
```

Audit records are immutable from the UI.

------------------------------------------------------------------------

# 34. Gateway API Surface

Recommended logical routes:

``` text
/api/v1/me

/api/v1/organizations/*
/api/v1/templates/*
/api/v1/placeholders/*
/api/v1/documents/*
/api/v1/workflows/*
/api/v1/approvals/*
/api/v1/signatures/*
/api/v1/notifications/*
/api/v1/audit/*
/api/v1/search/*
/api/v1/dashboard/*
```

Internal service URLs must never be required by React.

------------------------------------------------------------------------

# 35. Gateway Responsibilities

Gateway owns:

-   Authentication handling.
-   Authorization enforcement/propagation.
-   Tenant context.
-   Routing.
-   API versioning.
-   Correlation IDs.
-   Rate limiting where applicable.
-   Request validation.
-   Response composition.
-   Cross-service aggregation where useful.

Gateway must remain thin and must not become the business workflow
engine.

------------------------------------------------------------------------

# 36. Gateway-to-Service Mapping

  UI Feature      Gateway                      Service
  --------------- ---------------------------- -------------------------
  User Context    `/me`                        Identity / Organization
  Organization    `/organizations`             Organization
  Templates       `/templates`                 Template
  Placeholders    `/placeholders`              Template
  Documents       `/documents`                 Document
  Generation      `/documents/generate`        Document
  Workflow        `/workflows`                 Workflow
  Approvals       `/approvals`                 Workflow
  Signatures      `/signatures`                Signature
  Notifications   `/notifications`             Notification
  Audit           `/audit`                     Audit
  Files           `/files`                     Storage / Document
  Timeline        `/documents/{id}/timeline`   Aggregated services

------------------------------------------------------------------------

# 37. API Client Architecture

Create one centralized HTTP client.

``` typescript
export const gatewayClient = {
    get,
    post,
    put,
    patch,
    delete
};
```

Feature APIs:

``` typescript
templateApi
documentApi
workflowApi
signatureApi
notificationApi
auditApi
organizationApi
```

React components consume hooks:

``` typescript
const {
    data,
    isLoading,
    error
} = useDocuments(filters);
```

Do not place raw HTTP calls inside JSX/components.

------------------------------------------------------------------------

# 38. API Contract Management

Use OpenAPI-generated or centrally maintained TypeScript contracts where
practical.

Example:

``` typescript
type DocumentStatus =
    | "Draft"
    | "Processing"
    | "Completed"
    | "Failed"
    | "Cancelled";
```

Avoid duplicating domain validation/business rules in the frontend.

------------------------------------------------------------------------

# 39. Server State Management

Use a query/cache library such as TanStack Query.

Server state:

``` text
Templates
Documents
Workflow Instances
Approvals
Signatures
Notifications
Audit
```

Client state:

``` text
Modal state
UI preferences
Navigation
Temporary form state
Selected filters
```

Do not copy complete backend entities into global state unnecessarily.

------------------------------------------------------------------------

# 40. Cache Invalidation

Examples:

### Approve Workflow

``` text
Approve
  ↓
Invalidate workflow
Invalidate approval inbox
Invalidate document timeline
Invalidate notifications
Refresh dashboard counts
```

### Create Template

``` text
Create
  ↓
Invalidate template list
Refresh template detail
```

Do not use full-page reloads for normal mutations.

------------------------------------------------------------------------

# 41. Async Backend Integration

For long-running actions:

``` text
React
  ↓
Gateway
  ↓
Service
  ↓
202 Accepted
  ↓
Service Bus
  ↓
Worker / Function
  ↓
Database / Blob
  ↓
Completed State
  ↓
UI Refresh
```

Possible UI mechanisms:

1.  Polling.
2.  SignalR/realtime events if implemented.
3.  Explicit refresh.
4.  Query invalidation.

The UI must recover correctly even if a realtime event is missed.

------------------------------------------------------------------------

# 42. Real-Time Flow

For supported realtime features:

``` text
Service
  ↓
Event
  ↓
Realtime Infrastructure
  ↓
React
  ↓
Invalidate Query
  ↓
GET Latest State
  ↓
Render
```

Do not trust event payloads as the only source of truth.

Use events to trigger refresh.

------------------------------------------------------------------------

# 43. File Upload

UX:

``` text
+--------------------------------------+
| Drag and drop file here              |
|                                      |
|             or                       |
|                                      |
|             [Browse]                  |
+--------------------------------------+
```

Show:

``` text
File Name
File Size
Upload Progress
Validation Status
```

For large files, use the backend-approved secure upload mechanism.

Never expose storage account keys.

------------------------------------------------------------------------

# 44. File Download

Download must go through an authorized backend flow:

``` text
GET /api/v1/documents/{id}/download
```

or a short-lived secure download URL returned by the backend.

Never construct permanent Blob Storage URLs in React.

------------------------------------------------------------------------

# 45. Error Contract

Recommended Gateway response:

``` json
{
  "code": "DOCUMENT_NOT_FOUND",
  "message": "Document was not found.",
  "correlationId": "abc-123",
  "details": []
}
```

UI handling:

  HTTP   UI Behavior
  ------ --------------------------------
  400    Field/business validation
  401    Renew or login
  403    Permission message
  404    Not found
  409    Concurrency/business conflict
  422    Domain validation
  429    Retry/backoff
  500    Generic error + correlation ID
  503    Service unavailable + retry

Never show backend stack traces.

------------------------------------------------------------------------

# 46. Loading States

Every async feature must support:

``` text
Initial Loading
Refreshing
Submitting
Processing
Success
Error
Empty
```

Use skeletons for larger screens.

Buttons must be disabled during critical submissions to prevent
duplicate actions.

------------------------------------------------------------------------

# 47. Empty States

Example:

``` text
No templates found.

Create your first template.

[Create Template]
```

Approval inbox:

``` text
No pending approvals.

You are all caught up.
```

Empty states should provide a relevant next action where the user has
permission.

------------------------------------------------------------------------

# 48. Form Standards

All forms must implement:

-   Required validation.
-   Type validation.
-   Length validation.
-   Domain validation errors.
-   Server validation.
-   Unsaved-change protection where needed.
-   Disabled submit while processing.
-   Success feedback.
-   Recoverable errors.

------------------------------------------------------------------------

# 49. Concurrency Handling

If backend returns:

``` http
409 Conflict
```

show:

``` text
This record was changed by another user.

[Reload Latest]
```

Do not silently overwrite changes.

------------------------------------------------------------------------

# 50. URL State

Where appropriate, list filters should be represented in URL query
parameters.

Example:

``` text
/documents?status=Completed&page=1&pageSize=25
```

Benefits:

-   Refresh-safe.
-   Bookmarkable.
-   Navigable.
-   Shareable where permitted.

Never place secrets or sensitive document content in URLs.

------------------------------------------------------------------------

# 51. Pagination

Use server-side pagination.

Example:

``` http
GET /api/v1/documents?page=1&pageSize=25
```

Response:

``` json
{
  "items": [],
  "page": 1,
  "pageSize": 25,
  "totalCount": 1284,
  "totalPages": 52
}
```

Do not load thousands of records into the browser for pagination.

------------------------------------------------------------------------

# 52. Responsive Design

## Desktop

-   Sidebar.
-   Data tables.
-   Multi-column workspaces.

## Tablet

-   Collapsible sidebar.
-   Responsive tables.
-   Stacked forms.

## Mobile

-   Drawer navigation.
-   Card lists.
-   Stacked forms.
-   Sticky primary action where useful.

Approval and signing flows must remain usable on mobile.

------------------------------------------------------------------------

# 53. Accessibility

Target WCAG 2.1 AA-compatible implementation.

Requirements:

-   Keyboard navigation.
-   Focus management.
-   Accessible labels.
-   Semantic HTML.
-   Dialog focus trapping.
-   Screen-reader-friendly status.
-   Accessible errors.
-   Sufficient contrast.
-   No color-only status indicators.

------------------------------------------------------------------------

# 54. Design System

Reusable components:

``` text
Button
Input
Select
DatePicker
Checkbox
Radio
Textarea
Dialog
Drawer
Tabs
Table
Pagination
Badge
StatusBadge
Toast
Alert
Tooltip
Dropdown
FileUploader
Timeline
EmptyState
ErrorState
LoadingSkeleton
ConfirmDialog
PermissionGate
```

Standard states:

``` text
Draft
Active
Inactive
Processing
Pending
Approved
Rejected
Signed
Completed
Failed
Cancelled
Expired
```

------------------------------------------------------------------------

# 55. UX Status Model

Status must include text and/or icons.

Example:

``` text
[Draft]      Draft
[Pending]    Pending
[Approved]   Approved
[Rejected]   Rejected
[Completed]  Completed
```

Never communicate status by color alone.

------------------------------------------------------------------------

# 56. Complete Flow A --- Template → Document

``` text
Login
  ↓
Dashboard
  ↓
Templates
  ↓
Create Template
  ↓
Enter Metadata
  ↓
Upload File
  ↓
Detect Placeholders
  ↓
Review / Manage Placeholders
  ↓
Validate
  ↓
Save Draft
  ↓
Activate
  ↓
Generate Document
  ↓
Enter Values
  ↓
Validate
  ↓
Generate
  ↓
Document Workspace
  ↓
Preview / Download / Start Workflow
```

Backend:

``` text
React
 ↓
Gateway
 ↓
Template Service
 ↓
Storage / Database

React
 ↓
Gateway
 ↓
Document Service
 ↓
Template / Storage / Database
```

------------------------------------------------------------------------

# 57. Complete Flow B --- Document → Workflow

``` text
Document Workspace
  ↓
Start Workflow
  ↓
Select Workflow
  ↓
Configure/Confirm
  ↓
Submit
  ↓
Workflow Instance Created
  ↓
Review
  ↓
Approval
  ↓
Signature
  ↓
Completed
```

Backend:

``` text
Document Service
  ↓
Workflow Service
  ↓
State Machine
  ↓
Service Bus
  ├── Notification
  ├── Audit
  └── Signature
```

------------------------------------------------------------------------

# 58. Complete Flow C --- Approval

``` text
Notification
  ↓
Approval Inbox
  ↓
Open Approval
  ↓
Document Preview
  ↓
Review
  ↓
Approve / Reject / Request Changes
  ↓
Gateway
  ↓
Workflow Service
  ↓
State Transition
  ↓
Persist
  ↓
Publish Event
  ↓
Refresh UI
```

------------------------------------------------------------------------

# 59. Complete Flow D --- Signature

``` text
Workflow reaches Signature
  ↓
Signature Request Created
  ↓
Notification
  ↓
Signer Opens Request
  ↓
Review Document
  ↓
Sign
  ↓
Signature Service
  ↓
Workflow Advances
  ↓
Audit
  ↓
Notification
  ↓
UI Displays Completed
```

------------------------------------------------------------------------

# 60. Complete Flow E --- Audit

``` text
Document
  ↓
Activity
  ↓
View Audit
  ↓
Gateway
  ↓
Audit Service
  ↓
Immutable Audit Records
```

------------------------------------------------------------------------

# 61. Document Workspace Contract

Recommended aggregated Gateway contract:

``` json
{
  "document": {},
  "workflow": {},
  "signature": {},
  "permissions": {},
  "timeline": [],
  "availableActions": []
}
```

Example:

``` json
{
  "availableActions": [
    "DOWNLOAD",
    "START_WORKFLOW",
    "VIEW_AUDIT"
  ]
}
```

This lets the backend communicate which actions are currently valid
without duplicating workflow rules in React.

------------------------------------------------------------------------

# 62. Backend-Driven Actions

Example:

``` text
Backend:
Status = AwaitingApproval
Permission = workflow.approve

UI:
[Approve]
[Reject]
[Request Changes]
```

After transition:

``` text
Backend:
Status = AwaitingSignature

UI:
[View Signature]
```

The UI refreshes from the backend and changes available actions
accordingly.

------------------------------------------------------------------------

# 63. Security Requirements

Never expose in frontend:

``` text
Database credentials
Service Bus connection strings
Storage account keys
Client secrets
Permanent SAS tokens
Internal service credentials
Signing secrets
```

Do not log:

``` text
Access tokens
Refresh tokens
Signature secrets
Sensitive document contents
Unnecessary personal data
```

------------------------------------------------------------------------

# 64. Correlation ID

Every request should carry or receive a correlation ID.

Example:

``` http
X-Correlation-Id: 8b7f1234
```

Propagation:

``` text
React
 ↓
Gateway
 ↓
Service
 ↓
Service Bus
 ↓
Worker
```

Failure UI:

``` text
Something went wrong.

Reference: 8b7f1234
```

This enables end-to-end troubleshooting.

------------------------------------------------------------------------

# 65. Observability

Frontend telemetry may capture:

``` text
Route
API latency
Failed API call
Correlation ID
Application version
Browser/application information
```

Do not send:

``` text
Access tokens
Document contents
Signature secrets
Sensitive payloads
```

Backend remains responsible for distributed tracing and server-side
telemetry.

------------------------------------------------------------------------

# 66. Performance Requirements

Implement:

-   Route-level lazy loading.
-   Server-side pagination.
-   Debounced search.
-   Query caching.
-   Request deduplication.
-   Large-table virtualization where necessary.
-   Async document generation.
-   Optimized file preview.
-   Avoid unnecessary global state.

Suggested lazy-loaded feature areas:

``` text
Dashboard
Templates
Documents
Workflows
Approvals
Signatures
Administration
```

------------------------------------------------------------------------

# 67. Frontend Environment Configuration

Example:

``` text
VITE_GATEWAY_BASE_URL
VITE_APP_NAME
VITE_APP_VERSION
VITE_ENVIRONMENT
```

Anything bundled into React is public.

Never place secrets in frontend environment variables.

------------------------------------------------------------------------

# 68. Local Development

``` text
React
localhost:<frontend>
        |
        v
Gateway
localhost:<gateway>
        |
        v
Internal Services
```

React communicates only with Gateway.

Gateway must provide the required local CORS configuration.

------------------------------------------------------------------------

# 69. Docker

Recommended frontend image:

``` text
Build Stage
  ↓
Node
  ↓
npm ci
  ↓
npm run build
  ↓
Production Static Server
```

Example:

``` text
React Build
   ↓
Nginx / approved static hosting
```

The production image should contain only the required static
application/runtime assets.

------------------------------------------------------------------------

# 70. Deployment Architecture

``` text
User
  ↓
Frontend Hosting
  ↓
API Gateway
  ├── Identity
  ├── Organization
  ├── Template
  ├── Document
  ├── Workflow
  ├── Signature
  ├── Notification
  └── Audit
```

Backend services should remain private/internal wherever the
architecture supports it.

------------------------------------------------------------------------

# 71. CI/CD

Recommended:

``` text
Install
 ↓
Lint
 ↓
Type Check
 ↓
Unit Tests
 ↓
Build
 ↓
Component Tests
 ↓
Security Scan
 ↓
Package
 ↓
Deploy DEV
 ↓
Integration Tests
 ↓
Deploy TST
 ↓
E2E Tests
 ↓
UAT Approval
 ↓
Deploy PROD
```

Use the project's existing Azure DevOps strategy where applicable.

------------------------------------------------------------------------

# 72. API Compatibility

React uses:

``` text
/api/v1/*
```

Never couple React directly to internal service API versions.

For breaking changes:

``` text
Add Gateway compatibility
        ↓
Update React
        ↓
Test E2E
        ↓
Deploy
        ↓
Remove deprecated contract later
```

------------------------------------------------------------------------

# 73. Feature Flags

Potential flags:

``` text
signature.enabled
realtime.enabled
advanced.audit.search
multi.organization
```

Feature flags control availability only.

They must never replace backend authorization.

------------------------------------------------------------------------

# 74. Testing Strategy

## 74.1 Unit

Test:

-   Validation.
-   Permission mapping.
-   Status mapping.
-   Utility functions.
-   API transformation.

## 74.2 Component

Test:

-   Forms.
-   Tables.
-   Dialogs.
-   Timeline.
-   PermissionGate.
-   Loading/error/empty states.

## 74.3 API Integration

Test:

``` text
React API Client
  ↓
Gateway
  ↓
Service
```

## 74.4 E2E

Critical scenarios:

1.  Login.
2.  Create template.
3.  Upload template.
4.  Manage placeholders.
5.  Validate template.
6.  Activate template.
7.  Generate document.
8.  Start workflow.
9.  Approve.
10. Send for signature.
11. Complete signature.
12. Verify workflow completed.
13. Verify notification.
14. Verify audit.
15. Download document.

------------------------------------------------------------------------

# 75. Critical E2E Acceptance Journey

The complete integrated journey must work with real backend state:

``` text
1. Login
2. Open Dashboard
3. Create Template
4. Upload Template
5. Define Placeholders
6. Validate
7. Activate
8. Generate Document
9. Open Document Workspace
10. Start Workflow
11. Complete Review
12. Approve
13. Create Signature Request
14. Sign
15. Verify Workflow Completed
16. Verify Notification
17. Open Timeline
18. Verify Audit
19. Download Final Document
```

The test is not successful if the UI is merely mocked.

Every important state must be persisted by the actual backend services.

------------------------------------------------------------------------

# 76. Implementation Task Breakdown

## Task 1 --- React Foundation

-   Verify React/TypeScript setup.
-   Configure routing.
-   Configure providers.
-   Configure linting.
-   Configure formatting.
-   Configure test framework.
-   Configure environment management.

## Task 2 --- Design System

-   Build reusable components.
-   Build layout.
-   Build navigation.
-   Build form components.
-   Build tables.
-   Build status components.
-   Build dialogs.
-   Build loading/error/empty states.

## Task 3 --- Authentication

-   Integrate identity.
-   Protected routes.
-   User context.
-   Session expiration.
-   Logout.

## Task 4 --- Organization

-   Load organization.
-   Organization selector if supported.
-   Tenant-aware caching.
-   Permission context.

## Task 5 --- Gateway Client

-   HTTP client.
-   Authentication.
-   Correlation ID.
-   Error handling.
-   API contracts.
-   Retry strategy where appropriate.

## Task 6 --- Dashboard

-   Summary.
-   Pending approvals.
-   Pending signatures.
-   Recent documents.
-   Notifications.

## Task 7 --- Templates

-   List.
-   Search.
-   Filter.
-   Create.
-   Edit.
-   Version.
-   Validate.
-   Activate/deactivate.

## Task 8 --- Placeholders

-   List.
-   Create.
-   Edit.
-   Delete/deactivate.
-   Validation.
-   Search/filter.

## Task 9 --- Documents

-   List.
-   Generate.
-   Detail.
-   Preview.
-   Download.
-   Timeline.

## Task 10 --- Workflow

-   Definitions.
-   Instances.
-   State display.
-   Actions.
-   Timeline.

## Task 11 --- Approvals

-   Inbox.
-   Detail.
-   Approve.
-   Reject.
-   Request changes.

## Task 12 --- Signatures

-   Prepare request.
-   Manage signers.
-   Send.
-   Sign.
-   Status.

## Task 13 --- Notifications

-   Unread count.
-   List.
-   Read.
-   Read all.
-   Preferences.
-   Resource navigation.

## Task 14 --- Audit

-   Resource audit.
-   Audit search.
-   Filters.
-   Timeline.

## Task 15 --- Cross-Service Workspace

-   Document workspace.
-   Workflow workspace.
-   Signature workspace.
-   Unified timeline.
-   Available actions.

## Task 16 --- Async/Realtime

-   Polling fallback.
-   Event-driven refresh.
-   SignalR/realtime if implemented.
-   Query invalidation.

## Task 17 --- Security

-   Tenant isolation.
-   Permission gates.
-   Secure downloads.
-   Secure signing.
-   No secrets.
-   Secure error handling.

## Task 18 --- Testing

-   Unit.
-   Component.
-   Integration.
-   E2E.
-   Accessibility.

## Task 19 --- Deployment

-   Docker.
-   Azure DevOps pipeline.
-   Environment configuration.
-   DEV/TST/UAT/PROD.

------------------------------------------------------------------------

# 77. Definition of Done

## UI

-   [ ] Application shell implemented.
-   [ ] Authentication implemented.
-   [ ] Organization context implemented.
-   [ ] Dashboard implemented.
-   [ ] Templates implemented.
-   [ ] Placeholder CRUD implemented.
-   [ ] Documents implemented.
-   [ ] Document generation implemented.
-   [ ] Workflow UI implemented.
-   [ ] Approval UI implemented.
-   [ ] Signature UI implemented.
-   [ ] Notifications implemented.
-   [ ] Audit implemented.
-   [ ] Unified timeline implemented.
-   [ ] Responsive design implemented.
-   [ ] Accessibility implemented.

## Gateway

-   [ ] React communicates only with Gateway.
-   [ ] API versioning implemented.
-   [ ] Authentication handled.
-   [ ] Tenant context handled.
-   [ ] Authorization enforced.
-   [ ] Correlation IDs implemented.
-   [ ] Error contract implemented.
-   [ ] Required aggregation endpoints implemented.

## Services

-   [ ] Identity integrated.
-   [ ] Organization integrated.
-   [ ] Template integrated.
-   [ ] Document integrated.
-   [ ] Workflow integrated.
-   [ ] Signature integrated.
-   [ ] Notification integrated.
-   [ ] Audit integrated.
-   [ ] Storage integrated.
-   [ ] Async processing integrated.

## Quality

-   [ ] Unit tests pass.
-   [ ] Component tests pass.
-   [ ] Integration tests pass.
-   [ ] E2E tests pass.
-   [ ] Accessibility checks pass.
-   [ ] Security checks pass.
-   [ ] Docker build passes.
-   [ ] CI/CD passes.
-   [ ] Critical end-to-end journey passes.

------------------------------------------------------------------------

# 78. Recommended Implementation Order

``` text
1. React foundation
       ↓
2. Design system
       ↓
3. Authentication
       ↓
4. Organization + permissions
       ↓
5. Gateway client
       ↓
6. Dashboard
       ↓
7. Templates + placeholders
       ↓
8. Documents
       ↓
9. Workflow + approvals
       ↓
10. Signatures
       ↓
11. Notifications
       ↓
12. Audit + timeline
       ↓
13. Cross-service workspaces
       ↓
14. Async/realtime refresh
       ↓
15. Security + accessibility
       ↓
16. Automated testing
       ↓
17. Docker + CI/CD
       ↓
18. Complete E2E validation
```

------------------------------------------------------------------------

# 79. Final Target User Journey

The completed Phase 10 platform must allow:

``` text
                    ┌───────────────┐
                    │     Login     │
                    └───────┬───────┘
                            ↓
                    ┌───────────────┐
                    │   Dashboard   │
                    └───────┬───────┘
                            ↓
                    ┌───────────────┐
                    │    Template   │
                    │    Management │
                    └───────┬───────┘
                            ↓
                    ┌───────────────┐
                    │  Placeholder  │
                    │   Management  │
                    └───────┬───────┘
                            ↓
                    ┌───────────────┐
                    │    Document   │
                    │   Generation  │
                    └───────┬───────┘
                            ↓
                    ┌───────────────┐
                    │    Workflow   │
                    └───────┬───────┘
                            ↓
                    ┌───────────────┐
                    │ Review /      │
                    │ Approval      │
                    └───────┬───────┘
                            ↓
                    ┌───────────────┐
                    │   Signature   │
                    └───────┬───────┘
                            ↓
                    ┌───────────────┐
                    │   Completed   │
                    └───────┬───────┘
                            ↓
             ┌──────────────┼──────────────┐
             ↓              ↓              ↓
        Notification     Timeline        Audit
             │              │              │
             └──────────────┼──────────────┘
                            ↓
                    Final Document
```

------------------------------------------------------------------------

# 80. Phase 10 Architectural Outcome

After Phase 10, the platform must be a cohesive end-to-end enterprise
product rather than a collection of independent backend services.

The complete architecture is:

``` text
                         USER
                          │
                          ▼
                ┌───────────────────┐
                │ React Web App     │
                │                   │
                │ UX/UI             │
                │ Forms             │
                │ Workspaces        │
                │ Dashboards        │
                └─────────┬─────────┘
                          │
                    HTTPS / JSON
                          │
                          ▼
                ┌───────────────────┐
                │   API Gateway     │
                │                   │
                │ Auth              │
                │ Tenant Context    │
                │ Authorization     │
                │ Routing           │
                │ Aggregation       │
                │ Correlation       │
                └─────────┬─────────┘
                          │
          ┌───────────────┼────────────────┐
          │               │                │
          ▼               ▼                ▼
     Template        Document          Workflow
      Service         Service           Service
          │               │                │
          └───────────────┼────────────────┘
                          │
             ┌────────────┼────────────┐
             ▼            ▼            ▼
        Signature    Notification     Audit
         Service       Service       Service
             │            │            │
             └────────────┼────────────┘
                          ▼
                    Azure Service Bus
                          │
                          ▼
                  Workers / Functions
                          │
             ┌────────────┼────────────┐
             ▼            ▼            ▼
         Azure SQL    Blob Storage   Cosmos DB
```

**Phase 10 is complete only when the React UI, Gateway, and Phases 4--9
backend services operate as one integrated application with real
persisted state, secure authorization, asynchronous processing,
observability, and automated end-to-end validation.**
