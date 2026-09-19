import { Navigate, createBrowserRouter } from 'react-router';
import { AppLayout } from '../layouts/AppLayout';
import { LoginPage } from '../features/authentication/LoginPage';
import { ProtectedRoute } from './ProtectedRoute';
import { AuditPage } from '../features/audit/AuditPage';
import { ApprovalsPage } from '../features/approvals/ApprovalsPage';
import { CreateDocumentPage, DocumentDetailPage, DocumentsPage } from '../features/documents/DocumentsPage';
import { OrganizationsPage } from '../features/organizations/OrganizationsPage';
import { TemplateDetailPage, TemplatesPage } from '../features/templates/TemplatesPage';
import { WorkflowsPage } from '../features/workflows/WorkflowsPage';
import { WorkflowInstancePage } from '../features/workflows/WorkflowInstancePage';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { NotificationsPage } from '../features/notifications/NotificationsPage';
import { SigningPage } from '../features/signing/SigningPage';

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />
  },
  {
    path: '/',
    element: (
      <ProtectedRoute>
        <AppLayout />
      </ProtectedRoute>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'organizations', element: <OrganizationsPage /> },
      { path: 'templates', element: <TemplatesPage /> },
      { path: 'templates/:templateId', element: <TemplateDetailPage /> },
      { path: 'documents', element: <DocumentsPage /> },
      { path: 'documents/new', element: <CreateDocumentPage /> },
      { path: 'documents/:documentId', element: <DocumentDetailPage /> },
      { path: 'workflows', element: <WorkflowsPage /> },
      { path: 'workflow-instances/:instanceId', element: <WorkflowInstancePage /> },
      { path: 'approvals', element: <ApprovalsPage /> },
      { path: 'signing', element: <SigningPage /> },
      { path: 'notifications', element: <NotificationsPage /> },
      { path: 'audit', element: <AuditPage /> }
    ]
  }
]);
