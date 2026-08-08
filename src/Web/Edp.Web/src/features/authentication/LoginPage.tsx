import { useAuthentication } from '../../hooks/useAuthentication';

export function LoginPage() {
  const { login, isLoading } = useAuthentication();

  return (
    <main className="grid min-h-screen grid-cols-1 bg-slate-50 text-slate-950 lg:grid-cols-[1fr_480px]">
      <section className="flex min-h-[45vh] items-end bg-[linear-gradient(135deg,#0f172a,#14545d_55%,#f5b841)] px-8 py-10 text-white lg:min-h-screen">
        <div className="max-w-2xl">
          <p className="mb-4 text-sm font-semibold uppercase tracking-wide text-teal-100">
            Enterprise Document Platform
          </p>
          <h1 className="text-4xl font-semibold leading-tight lg:text-6xl">
            Secure document operations for every workflow.
          </h1>
        </div>
      </section>

      <section className="flex items-center justify-center px-6 py-10">
        <div className="w-full max-w-sm rounded border border-slate-200 bg-white p-6 shadow-sm">
          <h2 className="text-xl font-semibold">Sign in</h2>
          <p className="mt-2 text-sm leading-6 text-slate-600">
            Use your organization account to access documents, templates, workflows, approvals, and audit activity.
          </p>

          <button
            className="mt-6 w-full rounded bg-slate-900 px-4 py-3 text-sm font-semibold text-white hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-400"
            disabled={isLoading}
            type="button"
            onClick={login}
          >
            Login with Microsoft Entra ID
          </button>
        </div>
      </section>
    </main>
  );
}
