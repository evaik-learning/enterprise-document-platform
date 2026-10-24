import { Navigate, createBrowserRouter, useParams, type RouteObject } from 'react-router';
import { isFeatureEnabled } from './featureFlags';
import { NotFoundPage, RouteError } from './RouteErrors';
import { LoginPage } from '../auth/LoginPage';
import { OrganizationSelectPage } from '../auth/OrganizationSelectPage';
import { Permissions } from '../auth/permissions';
import { ProtectedRoute } from '../auth/ProtectedRoute';
import { FullPageLoading } from '../components/feedback/LoadingSkeleton';
import { AppShell } from '../components/layout/AppShell';

/** Feature areas are code-split: each page loads on first navigation. */
const page = <T extends Record<string, unknown>>(loader: () => Promise<T>, name: keyof T): RouteObject['lazy'] =>
  async () => ({ Component: (await loader())[name] as React.ComponentType });

const RedirectWorkflowInstance = () => {
  const { instanceId = '' } = useParams();
  return <Navigate to={`/workflows/instances/${instanceId}`} replace />;
};

const signatureRoutes: RouteObject[] = isFeatureEnabled('signature.enabled')
  ? [{
      element: <ProtectedRoute permission={Permissions.SignatureRead} />,
      children: [
        { path: 'signatures', lazy: page(() => import('../features/signatures/SignaturesPage'), 'SignaturesPage') },
        { path: 'signatures/:signingRequestId', lazy: page(() => import('../features/signatures/SignatureRequestPage'), 'SignatureRequestPage') }
      ]
    }]
  : [];

export const routes: RouteObject[] = [
  { path: '/login', element: <LoginPage />, errorElement: <RouteError /> },
  {
    element: <ProtectedRoute requireOrganization={false} />,
    errorElement: <RouteError />,
    children: [{ path: '/select-organization', element: <OrganizationSelectPage /> }]
  },
  {
    path: '/',
    element: <ProtectedRoute />,
    errorElement: <RouteError />,
    hydrateFallbackElement: <FullPageLoading />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, lazy: page(() => import('../features/dashboard/DashboardPage'), 'DashboardPage') },
          {
            element: <ProtectedRoute permission={Permissions.TemplateRead} />,
            children: [
              { path: 'templates', lazy: page(() => import('../features/templates/TemplatesPage'), 'TemplatesPage') },
              { path: 'templates/:templateId', lazy: page(() => import('../features/templates/TemplateDetailPage'), 'TemplateDetailPage') }
            ]
          },
          {
            element: <ProtectedRoute permission={Permissions.TemplateWrite} />,
            children: [{ path: 'templates/new', lazy: page(() => import('../features/templates/CreateTemplatePage'), 'CreateTemplatePage') }]
          },
          {
            element: <ProtectedRoute permission={Permissions.DocumentRead} />,
            children: [
              { path: 'documents', lazy: page(() => import('../features/documents/DocumentsPage'), 'DocumentsPage') },
              { path: 'documents/:documentId', lazy: page(() => import('../features/documents/DocumentWorkspacePage'), 'DocumentWorkspacePage') }
            ]
          },
          {
            element: <ProtectedRoute permission={Permissions.DocumentGenerate} />,
            children: [{ path: 'documents/new', lazy: page(() => import('../features/documents/GenerateDocumentPage'), 'GenerateDocumentPage') }]
          },
          {
            element: <ProtectedRoute permission={Permissions.WorkflowRead} />,
            children: [
              { path: 'workflows', lazy: page(() => import('../features/workflows/WorkflowsPage'), 'WorkflowsPage') },
              { path: 'workflows/definitions/:workflowId', lazy: page(() => import('../features/workflows/WorkflowDefinitionPage'), 'WorkflowDefinitionPage') },
              { path: 'workflows/instances/:instanceId', lazy: page(() => import('../features/workflows/WorkflowInstancePage'), 'WorkflowInstancePage') },
              { path: 'workflow-instances/:instanceId', element: <RedirectWorkflowInstance /> }
            ]
          },
          {
            element: <ProtectedRoute permission={Permissions.WorkflowApprove} />,
            children: [
              { path: 'approvals', lazy: page(() => import('../features/approvals/ApprovalsPage'), 'ApprovalsPage') },
              { path: 'approvals/:taskId', lazy: page(() => import('../features/approvals/ApprovalDetailPage'), 'ApprovalDetailPage') }
            ]
          },
          ...signatureRoutes,
          { path: 'signing', element: <Navigate to="/signatures" replace /> },
          {
            element: <ProtectedRoute permission={Permissions.NotificationRead} />,
            children: [{ path: 'notifications', lazy: page(() => import('../features/notifications/NotificationsPage'), 'NotificationsPage') }]
          },
          {
            element: <ProtectedRoute permission={Permissions.AuditRead} />,
            children: [{ path: 'audit', lazy: page(() => import('../features/audit/AuditPage'), 'AuditPage') }]
          },
          {
            path: 'admin',
            element: <ProtectedRoute permission={Permissions.OrganizationAdmin} />,
            children: [
              { index: true, element: <Navigate to="organization" replace /> },
              { path: 'organization', lazy: page(() => import('../features/organization/AdminPages'), 'OrganizationAdminPage') },
              { path: 'users', lazy: page(() => import('../features/organization/AdminPages'), 'UsersAdminPage') },
              { path: 'roles', lazy: page(() => import('../features/organization/AdminPages'), 'RolesAdminPage') },
              { path: 'settings', lazy: page(() => import('../features/organization/AdminPages'), 'SettingsAdminPage') }
            ]
          },
          { path: 'organizations', element: <Navigate to="/admin/organization" replace /> },
          { path: 'profile', lazy: page(() => import('../features/profile/ProfilePage'), 'ProfilePage') },
          { path: '*', element: <NotFoundPage /> }
        ]
      }
    ]
  }
];

export const router = createBrowserRouter(routes);
