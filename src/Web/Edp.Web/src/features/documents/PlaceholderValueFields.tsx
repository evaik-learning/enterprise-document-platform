import type { Placeholder } from '../../api/contracts';
import { CheckboxField, TextField } from '../../components/forms/Fields';

export type FieldValues = Record<string, string | boolean>;

const NUMERIC = new Set(['integer', 'decimal', 'currency']);

export function initialValues(placeholders: Placeholder[]): FieldValues {
  return Object.fromEntries(placeholders.map((placeholder) => [placeholder.name, placeholder.dataType.toLowerCase() === 'boolean' ? placeholder.defaultValue?.toLowerCase() === 'true' : (placeholder.defaultValue ?? '')]));
}

/**
 * Converts form values into the generation payload and reports client-side problems.
 * Blank optional values are omitted (the Document service rejects empty strings); the server remains authoritative.
 */
export function buildGenerationData(placeholders: Placeholder[], values: FieldValues): { data: Record<string, unknown>; errors: Record<string, string> } {
  const data: Record<string, unknown> = {};
  const errors: Record<string, string> = {};
  for (const placeholder of placeholders) {
    const type = placeholder.dataType.toLowerCase();
    const label = placeholder.displayName || placeholder.name;
    const raw = values[placeholder.name];

    if (type === 'boolean') {
      data[placeholder.name] = Boolean(raw);
      continue;
    }

    const text = typeof raw === 'string' ? raw.trim() : '';
    if (text === '') {
      if (placeholder.isRequired) errors[placeholder.name] = `${label} is required.`;
      continue;
    }

    if (NUMERIC.has(type)) {
      const number = Number(text);
      if (!Number.isFinite(number)) { errors[placeholder.name] = `${label} must be a number.`; continue; }
      if (type === 'integer' && !Number.isInteger(number)) { errors[placeholder.name] = `${label} must be a whole number.`; continue; }
      data[placeholder.name] = number;
    } else if (type === 'email' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(text)) {
      errors[placeholder.name] = `${label} must be a valid email address.`;
    } else if (type === 'url' && !/^https?:\/\/\S+$/i.test(text)) {
      errors[placeholder.name] = `${label} must be a valid http(s) URL.`;
    } else {
      data[placeholder.name] = text;
    }
  }
  return { data, errors };
}

function inputType(dataType: string) {
  switch (dataType.toLowerCase()) {
    case 'integer':
    case 'decimal':
    case 'currency': return 'number';
    case 'date': return 'date';
    case 'datetime': return 'datetime-local';
    case 'email': return 'email';
    case 'phone': return 'tel';
    case 'url': return 'url';
    default: return 'text';
  }
}

interface PlaceholderValueFieldsProps {
  placeholders: Placeholder[];
  values: FieldValues;
  errors: Record<string, string>;
  onChange: (name: string, value: string | boolean) => void;
  disabled?: boolean;
}

export function PlaceholderValueFields({ placeholders, values, errors, onChange, disabled }: PlaceholderValueFieldsProps) {
  if (placeholders.length === 0) return <p className="text-sm text-slate-600">This template version has no placeholders.</p>;
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {placeholders.map((placeholder) => {
        const label = placeholder.displayName || placeholder.name;
        const help = placeholder.description || placeholder.format || undefined;
        if (placeholder.dataType.toLowerCase() === 'boolean') {
          return <CheckboxField key={placeholder.id} label={label} help={help} disabled={disabled} checked={Boolean(values[placeholder.name])} onChange={(event) => onChange(placeholder.name, event.target.checked)} />;
        }
        return (
          <TextField
            key={placeholder.id}
            label={label}
            help={help}
            required={placeholder.isRequired}
            disabled={disabled}
            type={inputType(placeholder.dataType)}
            step={placeholder.dataType.toLowerCase() === 'integer' ? 1 : placeholder.dataType.toLowerCase() === 'decimal' || placeholder.dataType.toLowerCase() === 'currency' ? 'any' : undefined}
            value={String(values[placeholder.name] ?? '')}
            onChange={(event) => onChange(placeholder.name, event.target.value)}
            error={errors[placeholder.name]}
          />
        );
      })}
    </div>
  );
}
