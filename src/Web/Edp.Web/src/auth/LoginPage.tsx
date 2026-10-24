import { Navigate, useSearchParams } from 'react-router';
import { appConfig } from '../app/config';
import { Button } from '../components/ui/Button';
import { FullPageLoading } from '../components/feedback/LoadingSkeleton';
import { redirectToLogin, safeReturnUrl } from './authService';
import { useAuth } from './useAuth';

/** Sign-in is handled entirely by the Gateway (Microsoft Entra ID). No credentials are collected here. */
export function LoginPage() {
  const { status } = useAuth();
  const [params] = useSearchParams();
  const returnUrl = safeReturnUrl(params.get('returnUrl'));

  if (status === 'loading') return <FullPageLoading />;
  if (status === 'authenticated') return <Navigate to={returnUrl} replace />;

  return (
    <main className="grid min-h-screen grid-cols-1 bg-slate-50 text-slate-950 lg:grid-cols-[1fr_480px]">
      <section className="flex min-h-[40vh] items-end bg-[linear-gradient(135deg,#0f172a,#14545d_55%,#f5b841)] px-8 py-10 text-white lg:min-h-screen">
        <div className="max-w-2xl">
          <p className="mb-4 text-sm font-semibold uppercase tracking-wide text-teal-100">{appConfig.appName}</p>
          <h1 className="text-4xl font-semibold leading-tight lg:text-6xl">Secure document operations for every workflow.</h1>
        </div>
      </section>
      <section className="flex items-center justify-center px-6 py-10">
        <div className="w-full max-w-sm rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 className="text-xl font-semibold">Sign in</h2>
          <p className="mt-2 text-sm leading-6 text-slate-600">Use your organization account to access templates, documents, workflows, approvals, signatures and audit activity.</p>
          <Button variant="primary" className="mt-6 w-full" onClick={() => redirectToLogin(returnUrl)}>Sign in with Microsoft Entra ID</Button>
        </div>
      </section>
    </main>
  );
}
