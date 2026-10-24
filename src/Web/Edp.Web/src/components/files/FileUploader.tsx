import { useRef, useState, type DragEvent } from 'react';
import { toUserMessage } from '../../api/errors';
import { cx } from '../../utils/cx';
import { formatBytes } from '../../utils/format';
import { Alert } from '../feedback/Feedback';
import { Button } from '../ui/Button';

interface FileUploaderProps {
  /** e.g. `.docx` */
  accept?: string;
  maxSizeBytes?: number;
  label?: string;
  disabled?: boolean;
  /** Performs the upload through a backend-approved endpoint (never a storage account directly). */
  onUpload: (file: File, onProgress: (percent: number) => void, signal: AbortSignal) => Promise<unknown>;
  onUploaded?: (file: File) => void;
}

type UploadState = { kind: 'idle' } | { kind: 'invalid'; message: string } | { kind: 'uploading'; file: File; percent: number } | { kind: 'done'; file: File } | { kind: 'error'; file: File; message: string };

export function validateFile(file: File, accept?: string, maxSizeBytes?: number): string | null {
  if (accept) {
    const allowed = accept.split(',').map((entry) => entry.trim().toLowerCase());
    const name = file.name.toLowerCase();
    if (!allowed.some((entry) => (entry.startsWith('.') ? name.endsWith(entry) : file.type === entry))) return `Only ${accept} files are supported.`;
  }
  if (file.size === 0) return 'The selected file is empty.';
  if (maxSizeBytes && file.size > maxSizeBytes) return `The file is ${formatBytes(file.size)}; the maximum is ${formatBytes(maxSizeBytes)}.`;
  return null;
}

export function FileUploader({ accept, maxSizeBytes, label = 'Upload file', disabled, onUpload, onUploaded }: FileUploaderProps) {
  const [state, setState] = useState<UploadState>({ kind: 'idle' });
  const [dragging, setDragging] = useState(false);
  const input = useRef<HTMLInputElement>(null);
  const abort = useRef<AbortController | null>(null);

  const start = async (file: File) => {
    const problem = validateFile(file, accept, maxSizeBytes);
    if (problem) { setState({ kind: 'invalid', message: problem }); return; }
    abort.current = new AbortController();
    setState({ kind: 'uploading', file, percent: 0 });
    try {
      await onUpload(file, (percent) => setState({ kind: 'uploading', file, percent }), abort.current.signal);
      setState({ kind: 'done', file });
      onUploaded?.(file);
    } catch (cause) {
      if (cause instanceof DOMException && cause.name === 'AbortError') setState({ kind: 'idle' });
      else setState({ kind: 'error', file, message: toUserMessage(cause, 'The upload failed.') });
    }
  };

  const onDrop = (event: DragEvent) => {
    event.preventDefault();
    setDragging(false);
    const file = event.dataTransfer.files[0];
    if (file && !disabled && state.kind !== 'uploading') void start(file);
  };

  const uploading = state.kind === 'uploading';

  return (
    <div>
      <div
        onDragOver={(event) => { event.preventDefault(); setDragging(true); }}
        onDragLeave={() => setDragging(false)}
        onDrop={onDrop}
        className={cx('rounded-xl border-2 border-dashed p-6 text-center', dragging ? 'border-teal-700 bg-teal-50' : 'border-slate-300 bg-white')}
      >
        <p className="text-sm text-slate-700">Drag and drop a file here</p>
        <p className="my-1 text-xs text-slate-500">or</p>
        <input
          ref={input}
          type="file"
          accept={accept}
          disabled={disabled || uploading}
          className="sr-only"
          aria-label={label}
          onChange={(event) => { const file = event.target.files?.[0]; if (file) void start(file); event.target.value = ''; }}
        />
        <Button size="sm" disabled={disabled || uploading} onClick={() => input.current?.click()}>Browse</Button>
        {(accept || maxSizeBytes) && <p className="mt-2 text-xs text-slate-500">{[accept && `Accepted: ${accept}`, maxSizeBytes && `max ${formatBytes(maxSizeBytes)}`].filter(Boolean).join(' · ')}</p>}
      </div>

      <div className="mt-3" aria-live="polite">
        {state.kind === 'invalid' && <Alert tone="error">{state.message}</Alert>}
        {state.kind === 'error' && <Alert tone="error" title={state.file.name}>{state.message}</Alert>}
        {state.kind === 'done' && <Alert tone="success" title={state.file.name}>Uploaded ({formatBytes(state.file.size)}).</Alert>}
        {uploading && (
          <div className="rounded-lg border border-slate-200 bg-white p-3 text-sm">
            <div className="flex items-center justify-between gap-2">
              <span className="truncate font-medium">{state.file.name} <span className="font-normal text-slate-500">({formatBytes(state.file.size)})</span></span>
              <Button size="sm" variant="ghost" onClick={() => abort.current?.abort()}>Cancel</Button>
            </div>
            <div role="progressbar" aria-label={`Uploading ${state.file.name}`} aria-valuemin={0} aria-valuemax={100} aria-valuenow={state.percent} className="mt-2 h-2 overflow-hidden rounded bg-slate-200">
              <div className="h-full bg-teal-700 transition-all" style={{ width: `${state.percent}%` }} />
            </div>
            <p className="mt-1 text-xs text-slate-600">{state.percent}%</p>
          </div>
        )}
      </div>
    </div>
  );
}
