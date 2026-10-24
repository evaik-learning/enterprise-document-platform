import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

afterEach(() => {
  cleanup();
});

// jsdom does not implement <dialog> modality or scrollIntoView; the Dialog component falls back gracefully.
if (!('crypto' in globalThis) || !globalThis.crypto.randomUUID) {
  Object.defineProperty(globalThis, 'crypto', { value: { randomUUID: () => `${Math.random().toString(16).slice(2)}-test` } });
}
