import { Link, isRouteErrorResponse, useRouteError } from 'react-router';
import { ErrorState } from '../components/feedback/Feedback';

export function NotFoundPage() {
  return (
    <div className="mx-auto max-w-lg rounded-xl border border-slate-200 bg-white p-8 text-center">
      <h1 className="text-xl font-semibold">Page not found</h1>
      <p className="mt-2 text-sm text-slate-600">The page you are looking for does not exist or has moved.</p>
      <Link to="/" className="mt-4 inline-block font-medium text-teal-900 underline">Go to the dashboard</Link>
    </div>
  );
}

/** Last-resort boundary for render errors; never shows stack traces. */
export function RouteError() {
  const error = useRouteError();
  if (isRouteErrorResponse(error) && error.status === 404) return <NotFoundPage />;
  return (
    <div className="mx-auto mt-16 max-w-lg space-y-4 px-4">
      <ErrorState error={error} title="This page failed to load" onRetry={() => window.location.reload()} />
      <Link to="/" className="inline-block font-medium text-teal-900 underline">Go to the dashboard</Link>
    </div>
  );
}
