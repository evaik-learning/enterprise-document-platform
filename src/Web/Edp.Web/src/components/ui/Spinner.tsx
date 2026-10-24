import { cx } from '../../utils/cx';

export function Spinner({ size = 'md', label }: { size?: 'sm' | 'md' | 'lg'; label?: string }) {
  const dimension = size === 'sm' ? 'size-4' : size === 'lg' ? 'size-8' : 'size-5';
  return (
    <span role={label ? 'status' : undefined} className="inline-flex items-center gap-2">
      <svg aria-hidden="true" className={cx('animate-spin', dimension)} viewBox="0 0 24 24" fill="none">
        <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" className="opacity-25" />
        <path d="M22 12a10 10 0 0 0-10-10" stroke="currentColor" strokeWidth="4" strokeLinecap="round" />
      </svg>
      {label && <span>{label}</span>}
    </span>
  );
}
