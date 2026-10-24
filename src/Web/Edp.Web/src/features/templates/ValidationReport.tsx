import type { ValidationResult } from '../../api/contracts';
import { Alert } from '../../components/feedback/Feedback';
import { formatDateTime } from '../../utils/format';

/** Template validation outcome: errors block activation; warnings are advisory. Severity is shown as text, not colour only. */
export function ValidationReport({ result }: { result: ValidationResult }) {
  return (
    <div className="space-y-2">
      <Alert tone={result.isValid ? 'success' : 'error'} title={result.isValid ? 'Validation passed' : 'Validation failed'}>
        {result.errorCount} error{result.errorCount === 1 ? '' : 's'}, {result.warningCount} warning{result.warningCount === 1 ? '' : 's'} · {formatDateTime(result.validatedAt)}
      </Alert>
      {result.errors.length > 0 && (
        <ul aria-label="Validation errors" className="space-y-1 text-sm">
          {result.errors.map((issue, index) => <li key={`${issue.code}-${index}`} className="rounded-lg border border-red-200 bg-red-50 p-2 text-red-950"><strong>Error</strong> {issue.code}: {issue.message}{issue.location && <span className="text-red-900"> ({issue.location})</span>}</li>)}
        </ul>
      )}
      {result.warnings.length > 0 && (
        <ul aria-label="Validation warnings" className="space-y-1 text-sm">
          {result.warnings.map((issue, index) => <li key={`${issue.code}-${index}`} className="rounded-lg border border-amber-200 bg-amber-50 p-2 text-amber-950"><strong>Warning</strong> {issue.code}: {issue.message}{issue.location && <span className="text-amber-900"> ({issue.location})</span>}</li>)}
        </ul>
      )}
    </div>
  );
}
