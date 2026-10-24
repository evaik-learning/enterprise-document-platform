import { describe, expect, it } from 'vitest';
import type { Placeholder, SigningRequestDetail, WorkflowState, WorkflowTransition } from '../../src/api/contracts';
import { validateFile } from '../../src/components/files/FileUploader';
import { buildGenerationData, initialValues } from '../../src/features/documents/PlaceholderValueFields';
import { validatePlaceholderForm } from '../../src/features/placeholders/PlaceholderFormDialog';
import { signatureProgressItems } from '../../src/features/signatures/SignatureProgress';
import { isWaitingForEarlierSigner } from '../../src/features/signatures/SignatureRequestPage';
import { validateSigningForm } from '../../src/features/signatures/SendForSignatureDialog';
import { validateTemplateDetails } from '../../src/features/templates/CreateTemplatePage';
import { orderStates } from '../../src/features/workflows/WorkflowStateVisualizer';

const placeholder = (overrides: Partial<Placeholder>): Placeholder => ({ id: overrides.name ?? 'p', name: 'Field', dataType: 'String', isRequired: true, occurrences: 1, ...overrides });

describe('document generation payload', () => {
  it('omits blank optional values and reports blank required ones', () => {
    const placeholders = [placeholder({ name: 'CustomerName' }), placeholder({ name: 'Notes', isRequired: false })];
    const { data, errors } = buildGenerationData(placeholders, { CustomerName: '  ', Notes: '' });
    expect(errors).toEqual({ CustomerName: 'CustomerName is required.' });
    expect(data).toEqual({});
  });

  it('converts numbers and keeps booleans, trimming text', () => {
    const placeholders = [placeholder({ name: 'Amount', dataType: 'Currency' }), placeholder({ name: 'Count', dataType: 'Integer' }), placeholder({ name: 'Paid', dataType: 'Boolean' }), placeholder({ name: 'Customer' })];
    const { data, errors } = buildGenerationData(placeholders, { Amount: '1250.50', Count: '3', Paid: false, Customer: ' Acme ' });
    expect(errors).toEqual({});
    expect(data).toEqual({ Amount: 1250.5, Count: 3, Paid: false, Customer: 'Acme' });
  });

  it('validates numbers, integers, emails and urls', () => {
    const placeholders = [placeholder({ name: 'Amount', dataType: 'Decimal' }), placeholder({ name: 'Count', dataType: 'Integer' }), placeholder({ name: 'Mail', dataType: 'Email' }), placeholder({ name: 'Site', dataType: 'Url' })];
    const { errors } = buildGenerationData(placeholders, { Amount: 'abc', Count: '2.5', Mail: 'nope', Site: 'ftp://x' });
    expect(Object.keys(errors).sort()).toEqual(['Amount', 'Count', 'Mail', 'Site']);
  });

  it('starts from defaults and typed empties', () => {
    expect(initialValues([placeholder({ name: 'A', defaultValue: 'x' }), placeholder({ name: 'B', dataType: 'Boolean', defaultValue: 'true' }), placeholder({ name: 'C' })])).toEqual({ A: 'x', B: true, C: '' });
  });
});

describe('form validation', () => {
  it('validates placeholder definitions like the backend rules', () => {
    expect(validatePlaceholderForm({ name: '', displayName: '', defaultValue: '', format: '', description: '' }, true).name).toMatch(/required/);
    expect(validatePlaceholderForm({ name: '1bad', displayName: '', defaultValue: '', format: '', description: '' }, true).name).toMatch(/letters/);
    expect(validatePlaceholderForm({ name: 'Good_Name1', displayName: '', defaultValue: '', format: '', description: '' }, true)).toEqual({});
    expect(validatePlaceholderForm({ name: '', displayName: 'x'.repeat(201), defaultValue: '', format: '', description: '' }, false).displayName).toMatch(/200/);
  });

  it('validates template details', () => {
    expect(Object.keys(validateTemplateDetails({ name: '', code: '', description: '' }))).toEqual(['name', 'code']);
    expect(validateTemplateDetails({ name: 'NDA', code: 'bad code', description: '' }).code).toMatch(/letters/);
    expect(validateTemplateDetails({ name: 'NDA', code: 'NDA-1', description: '' })).toEqual({});
  });

  it('validates signature preparation', () => {
    const valid = { title: 'Sign', expiresOn: '', signers: [{ displayName: 'A', email: 'a@x.com' }] };
    expect(validateSigningForm(valid)).toEqual([]);
    expect(validateSigningForm({ ...valid, signers: [] })).toContain('Add at least one signer.');
    expect(validateSigningForm({ ...valid, signers: [{ displayName: '', email: 'bad' }] })).toHaveLength(2);
    expect(validateSigningForm({ ...valid, signers: [{ displayName: 'A', email: 'a@x.com' }, { displayName: 'B', email: 'A@x.com' }] })).toContain('Signer 2: duplicate email address.');
    expect(validateSigningForm({ ...valid, expiresOn: '2020-01-01' }, new Date('2026-01-01'))).toContain('The expiry date must be in the future.');
  });

  it('validates uploaded files', () => {
    const file = (name: string, size: number) => new File([new Uint8Array(size)], name);
    expect(validateFile(file('a.docx', 10), '.docx', 100)).toBeNull();
    expect(validateFile(file('a.pdf', 10), '.docx', 100)).toMatch(/Only \.docx/);
    expect(validateFile(file('a.docx', 0), '.docx', 100)).toMatch(/empty/);
    expect(validateFile(file('a.docx', 200), '.docx', 100)).toMatch(/maximum/);
  });
});

describe('workflow and signature presentation', () => {
  const state = (id: string, name: string, stateType: string): WorkflowState => ({ id, workflowVersionId: 'v', name, stateType });
  const transition = (id: string, from: string, to: string, order = 0): WorkflowTransition => ({ id, workflowVersionId: 'v', fromStateId: from, toStateId: to, order });

  it('orders states from the start state for display only', () => {
    const states = [state('c', 'Completed', 'End'), state('a', 'Review', 'Task'), state('s', 'Start', 'Start'), state('x', 'Orphan', 'Task')];
    const ordered = orderStates(states, [transition('1', 's', 'a'), transition('2', 'a', 'c')]);
    expect(ordered.map((item) => item.name)).toEqual(['Start', 'Review', 'Completed', 'Orphan']);
  });

  const request = (overrides: Partial<SigningRequestDetail>): SigningRequestDetail => ({
    signingRequestId: 'r', workflowInstanceId: 'w', documentId: 'd', title: 'T', status: 'InProgress', signingMode: 'Sequential', createdAt: '2026-01-01T00:00:00Z', activatedAt: '2026-01-01T01:00:00Z',
    signers: [
      { signerId: 's1', email: 'a@x.com', displayName: 'Ann', role: 'Signer', status: 'Signed', signingOrder: 1, signedAt: '2026-01-01T02:00:00Z' },
      { signerId: 's2', email: 'b@x.com', displayName: 'Bob', role: 'Signer', status: 'Invited', signingOrder: 2 }
    ],
    signatureFields: [], auditTrail: [], ...overrides
  });

  it('derives the signature progress from backend signer states', () => {
    const items = signatureProgressItems(request({}));
    expect(items.map((item) => [item.title, item.state])).toEqual([
      ['Request created', 'completed'], ['Sent to Ann', 'completed'], ['Ann signed', 'completed'], ['Sent to Bob', 'completed'], ['Awaiting Bob', 'current'], ['Completed', 'upcoming']
    ]);
  });

  it('marks declined requests as failed', () => {
    const items = signatureProgressItems(request({ status: 'Declined', signers: [{ signerId: 's1', email: 'a@x.com', displayName: 'Ann', role: 'Signer', status: 'Declined', signingOrder: 1, declinedAt: '2026-01-01T02:00:00Z', declineReason: 'No' }] }));
    expect(items.at(-1)).toMatchObject({ title: 'Declined', state: 'failed' });
    expect(items.some((item) => item.state === 'failed' && item.title === 'Ann declined')).toBe(true);
  });

  it('flags signers who must wait for earlier signers (display hint only)', () => {
    const sequential = request({ signers: [{ signerId: 's1', email: 'a@x.com', displayName: 'Ann', role: 'Signer', status: 'Invited', signingOrder: 1 }, { signerId: 's2', email: 'b@x.com', displayName: 'Bob', role: 'Signer', status: 'Invited', signingOrder: 2 }] });
    expect(isWaitingForEarlierSigner(sequential, sequential.signers[1]!)).toBe(true);
    expect(isWaitingForEarlierSigner(sequential, sequential.signers[0]!)).toBe(false);
    expect(isWaitingForEarlierSigner({ ...sequential, signingMode: 'Parallel' }, sequential.signers[1]!)).toBe(false);
  });
});
