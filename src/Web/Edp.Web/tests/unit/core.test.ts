import { describe, expect, it } from 'vitest';
import { hasPermission, Permissions } from '../../src/auth/permissions';
import { isLocalReturnUrl, safeReturnUrl } from '../../src/auth/authService';
import { humanize, isProcessingStatus, toUxStatus } from '../../src/utils/status';
import { formatBytes, formatCount, shortId } from '../../src/utils/format';
import { notificationRoute } from '../../src/utils/notificationRoutes';
import { toPaged } from '../../src/api/contracts';
import { pageWindow } from '../../src/components/tables/Pagination';
import { createQueryKeys } from '../../src/api/queryKeys';
import { visibleNavigation } from '../../src/components/navigation/navigation';

describe('permission model', () => {
  it('requires every permission by default and any when asked', () => {
    const granted = [Permissions.TemplateRead, Permissions.DocumentRead];
    expect(hasPermission(granted, Permissions.TemplateRead)).toBe(true);
    expect(hasPermission(granted, [Permissions.TemplateRead, Permissions.TemplateWrite])).toBe(false);
    expect(hasPermission(granted, [Permissions.TemplateRead, Permissions.TemplateWrite], 'any')).toBe(true);
    expect(hasPermission(granted, undefined)).toBe(true);
    expect(hasPermission([], [])).toBe(true);
  });

  it('shows only the navigation a user may use', () => {
    const can = (required: Parameters<typeof hasPermission>[1], mode?: 'all' | 'any') => hasPermission([Permissions.TemplateRead, Permissions.DocumentRead], required, mode);
    const labels = visibleNavigation(can).flatMap((group) => group.items.map((item) => item.label));
    expect(labels).toEqual(['Dashboard', 'Templates', 'Documents']);
  });

  it('shows administration only to organization admins', () => {
    const can = (required: Parameters<typeof hasPermission>[1], mode?: 'all' | 'any') => hasPermission(Object.values(Permissions), required, mode);
    const groups = visibleNavigation(can);
    expect(groups.at(-1)?.heading).toBe('Administration');
    expect(groups.at(-1)?.items.map((item) => item.label)).toEqual(['Organization', 'Users', 'Roles', 'Settings']);
  });
});

describe('return url safety', () => {
  it.each(['/documents', '/documents?page=2'])('accepts local path %s', (value) => expect(isLocalReturnUrl(value)).toBe(true));
  it.each(['https://evil.example', '//evil.example', '/\\evil', '', null, undefined])('rejects %s', (value) => expect(isLocalReturnUrl(value)).toBe(false));
  it('falls back for unsafe or login destinations', () => {
    expect(safeReturnUrl('https://evil.example')).toBe('/');
    expect(safeReturnUrl('/login?returnUrl=%2F')).toBe('/');
    expect(safeReturnUrl('/templates')).toBe('/templates');
  });
});

describe('status mapping', () => {
  it.each([
    ['Draft', 'draft'], ['Active', 'active'], ['Published', 'active'], ['Generating', 'processing'], ['InProgress', 'processing'],
    ['AwaitingApproval', 'pending'], ['PendingSignature', 'pending'], ['PartiallySigned', 'pending'], ['Approved', 'approved'],
    ['Rejected', 'rejected'], ['Declined', 'rejected'], ['Signed', 'signed'], ['Completed', 'completed'], ['Failed', 'failed'],
    ['Cancelled', 'cancelled'], ['Expired', 'expired'], ['Archived', 'inactive'], ['SomethingNew', 'draft']
  ])('maps %s to %s', (raw, expected) => expect(toUxStatus(raw)).toBe(expected));

  it('humanizes pascal case and detects processing', () => {
    expect(humanize('PartiallySigned')).toBe('Partially signed');
    expect(humanize('awaiting_approval')).toBe('Awaiting approval');
    expect(isProcessingStatus('Generating')).toBe(true);
    expect(isProcessingStatus('Completed')).toBe(false);
  });
});

describe('formatting', () => {
  it('formats sizes, counts and ids', () => {
    expect(formatBytes(512)).toBe('512 B');
    expect(formatBytes(2048)).toBe('2.0 KB');
    expect(formatBytes(5 * 1024 * 1024)).toBe('5.0 MB');
    expect(formatCount(null)).toBe('—');
    expect(formatCount(1284)).toBe((1284).toLocaleString());
    expect(shortId('abcdef123456')).toBe('abcdef12');
  });
});

describe('notification navigation', () => {
  const base = { notificationType: 'WorkflowCompleted' };
  it('resolves resource references to routes', () => {
    expect(notificationRoute({ ...base, resourceType: 'Workflow', resourceId: 'w1' })).toBe('/workflows/instances/w1');
    expect(notificationRoute({ ...base, resourceType: 'Document', resourceId: 'd1' })).toBe('/documents/d1');
    expect(notificationRoute({ ...base, resourceType: 'Signature', resourceId: 's1' })).toBe('/signatures/s1');
    expect(notificationRoute({ ...base, resourceType: 'Template', resourceId: 't1' })).toBe('/templates/t1');
  });
  it('sends approval notifications to the inbox and unknown ones to the center', () => {
    expect(notificationRoute({ notificationType: 'ApprovalAssigned', resourceType: 'Workflow', resourceId: 'w1' })).toBe('/approvals');
    expect(notificationRoute({ ...base, resourceType: 'Other', resourceId: 'x' })).toBe('/notifications');
    expect(notificationRoute({ ...base })).toBe('/notifications');
  });
});

describe('pagination helpers', () => {
  it('normalizes paged envelopes and bare arrays', () => {
    expect(toPaged([1, 2], 1, 2)).toMatchObject({ items: [1, 2], hasNextPage: true });
    expect(toPaged({ items: [1], totalCount: 51, page: 2, pageSize: 25 }, 2, 25)).toMatchObject({ totalPages: 3, hasNextPage: true });
    expect(toPaged({ items: [1], totalCount: 51, page: 3, pageSize: 25 }, 3, 25).hasNextPage).toBe(false);
    expect(toPaged({ items: [], hasNextPage: false }, 1, 25).hasNextPage).toBe(false);
    expect(toPaged(undefined, 1, 25).items).toEqual([]);
  });

  it('builds a compact page window', () => {
    expect(pageWindow(1, 5)).toEqual([1, 2, 3, 4, 5]);
    expect(pageWindow(6, 52)).toEqual([1, 'gap', 5, 6, 7, 'gap', 52]);
    expect(pageWindow(1, 52)).toEqual([1, 2, 'gap', 52]);
  });
});

describe('tenant-scoped cache keys', () => {
  it('prefixes every key with the organization so tenants never share cache entries', () => {
    const a = createQueryKeys('org-a');
    const b = createQueryKeys('org-b');
    expect(a.documents.list({ page: 1 })).not.toEqual(b.documents.list({ page: 1 }));
    const all = [a.dashboard, a.templates.all, a.documents.detail('1'), a.workflows.list, a.approvals.all, a.signatures.all, a.notifications.unread, a.audit.all, a.organization.mine];
    for (const key of all) expect(key[0]).toBe('org-a');
    expect(createQueryKeys(null).scope).toBe('no-org');
  });
});
