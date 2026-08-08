import { Navigate, createBrowserRouter } from 'react-router';
import { AppLayout } from '../layouts/AppLayout';
import { LoginPage } from '../features/authentication/LoginPage';
import { ProtectedRoute } from './ProtectedRoute';
import { AuditPage } from '../features/audit/AuditPage';
import { ApprovalsPage } from '../features/approvals/ApprovalsPage';
import { DocumentsPage } from '../features/documents/DocumentsPage';
import { OrganizationsPage } from '../features/organizations/OrganizationsPage';
import { TemplatesPage } from '../features/templates/TemplatesPage';
import { WorkflowsPage } from '../features/workflows/WorkflowsPage';

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
      { index: true, element: <Navigate to="/documents" replace /> },
      { path: 'organizations', element: <OrganizationsPage /> },
      { path: 'templates', element: <TemplatesPage /> },
      { path: 'documents', element: <DocumentsPage /> },
      { path: 'workflows', element: <WorkflowsPage /> },
      { path: 'approvals', element: <ApprovalsPage /> },
      { path: 'audit', element: <AuditPage /> }
    ]
  }
]);
