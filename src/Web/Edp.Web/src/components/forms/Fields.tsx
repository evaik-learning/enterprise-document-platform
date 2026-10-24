import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react';
import { cx } from '../../utils/cx';

export const controlClass = (invalid?: boolean) =>
  cx(
    'block w-full rounded-lg border bg-white px-3 py-2 text-sm text-slate-900 shadow-sm placeholder:text-slate-400 focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-teal-700 disabled:cursor-not-allowed disabled:bg-slate-100',
    invalid ? 'border-red-600' : 'border-slate-300'
  );

interface ShellProps {
  label: string;
  help?: string;
  error?: string;
  required?: boolean;
  /** Visually hides the label while keeping it for assistive technology. */
  hideLabel?: boolean;
  className?: string;
}

interface Ids { inputId: string; helpId: string; errorId: string }

function useIds(id?: string): Ids {
  const generated = useId();
  const inputId = id ?? generated;
  return { inputId, helpId: `${inputId}-help`, errorId: `${inputId}-error` };
}

const describedBy = (ids: Ids, shell: ShellProps) => [shell.help ? ids.helpId : undefined, shell.error ? ids.errorId : undefined].filter(Boolean).join(' ') || undefined;

function Shell({ ids, shell, children }: { ids: Ids; shell: ShellProps; children: ReactNode }) {
  return (
    <div className={shell.className}>
      <label htmlFor={ids.inputId} className={cx('mb-1 block text-sm font-medium text-slate-800', shell.hideLabel && 'sr-only')}>
        {shell.label}
        {shell.required && <span aria-hidden="true" className="ml-0.5 text-red-700">*</span>}
      </label>
      {children}
      {shell.help && <p id={ids.helpId} className="mt-1 text-xs text-slate-600">{shell.help}</p>}
      {shell.error && <p id={ids.errorId} className="mt-1 text-xs font-medium text-red-800">{shell.error}</p>}
    </div>
  );
}

type TextFieldProps = ShellProps & Omit<InputHTMLAttributes<HTMLInputElement>, 'className'>;

export function TextField({ label, help, error, hideLabel, className, id, ...input }: TextFieldProps) {
  const ids = useIds(id);
  const shell = { label, help, error, hideLabel, className, required: input.required };
  return (
    <Shell ids={ids} shell={shell}>
      <input {...input} id={ids.inputId} aria-invalid={error ? true : undefined} aria-describedby={describedBy(ids, shell)} className={controlClass(!!error)} />
    </Shell>
  );
}

/** Date input (native picker keeps it keyboard- and screen-reader-accessible). */
export function DateField(props: Omit<TextFieldProps, 'type'>) {
  return <TextField {...props} type="date" />;
}

type TextareaFieldProps = ShellProps & Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'className'>;

export function TextareaField({ label, help, error, hideLabel, className, id, ...input }: TextareaFieldProps) {
  const ids = useIds(id);
  const shell = { label, help, error, hideLabel, className, required: input.required };
  return (
    <Shell ids={ids} shell={shell}>
      <textarea rows={3} {...input} id={ids.inputId} aria-invalid={error ? true : undefined} aria-describedby={describedBy(ids, shell)} className={controlClass(!!error)} />
    </Shell>
  );
}

type SelectFieldProps = ShellProps & Omit<SelectHTMLAttributes<HTMLSelectElement>, 'className'>;

export function SelectField({ label, help, error, hideLabel, className, id, children, ...input }: SelectFieldProps) {
  const ids = useIds(id);
  const shell = { label, help, error, hideLabel, className, required: input.required };
  return (
    <Shell ids={ids} shell={shell}>
      <select {...input} id={ids.inputId} aria-invalid={error ? true : undefined} aria-describedby={describedBy(ids, shell)} className={controlClass(!!error)}>{children}</select>
    </Shell>
  );
}

export function CheckboxField({ label, help, id, className, ...input }: { label: string; help?: string; className?: string } & Omit<InputHTMLAttributes<HTMLInputElement>, 'type' | 'className'>) {
  const ids = useIds(id);
  return (
    <div className={className}>
      <div className="flex items-start gap-2">
        <input {...input} id={ids.inputId} type="checkbox" aria-describedby={help ? ids.helpId : undefined} className="mt-0.5 size-4 rounded border-slate-400 text-teal-700 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-teal-700" />
        <label htmlFor={ids.inputId} className="text-sm font-medium text-slate-800">{label}</label>
      </div>
      {help && <p id={ids.helpId} className="ml-6 mt-0.5 text-xs text-slate-600">{help}</p>}
    </div>
  );
}

export function RadioGroup<T extends string>({ legend, name, value, options, onChange }: { legend: string; name: string; value: T; options: Array<{ value: T; label: string; help?: string }>; onChange: (value: T) => void }) {
  return (
    <fieldset>
      <legend className="mb-1 text-sm font-medium text-slate-800">{legend}</legend>
      <div className="space-y-2">
        {options.map((option) => (
          <label key={option.value} className="flex cursor-pointer items-start gap-2 text-sm">
            <input type="radio" name={name} value={option.value} checked={value === option.value} onChange={() => onChange(option.value)} className="mt-0.5 size-4 text-teal-700" />
            <span><span className="font-medium text-slate-800">{option.label}</span>{option.help && <span className="block text-xs text-slate-600">{option.help}</span>}</span>
          </label>
        ))}
      </div>
    </fieldset>
  );
}

/** Summarizes submit-time problems; focus moves here so screen readers announce it. */
export function FormErrorSummary({ errors }: { errors: string[] }) {
  if (errors.length === 0) return null;
  return (
    <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-950">
      <p className="font-semibold">Please fix the following:</p>
      <ul className="mt-1 list-disc pl-5">{errors.map((message) => <li key={message}>{message}</li>)}</ul>
    </div>
  );
}
