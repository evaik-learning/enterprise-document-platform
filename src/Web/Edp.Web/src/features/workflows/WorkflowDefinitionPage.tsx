import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { STATE_TYPES, type WorkflowTransition } from '../../api/contracts';
import { toUserMessage } from '../../api/errors';
import { useWorkflowDefinitionMutations, useWorkflowGraph, useWorkflows, useWorkflowVersions } from '../../api/hooks/useWorkflows';
import { Permissions } from '../../auth/permissions';
import { PermissionGate } from '../../auth/PermissionGate';
import { Alert, EmptyState } from '../../components/feedback/Feedback';
import { QueryBoundary } from '../../components/feedback/QueryBoundary';
import { useToast } from '../../components/feedback/Toast';
import { SelectField, TextField } from '../../components/forms/Fields';
import { Badge, StatusBadge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card, PageHeader } from '../../components/ui/Card';
import { humanize } from '../../utils/status';
import { orderStates, WorkflowStateVisualizer } from './WorkflowStateVisualizer';

const CAN_DESIGN = [Permissions.WorkflowExecute, Permissions.OrganizationAdmin] as const;
const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** Workflow definition designer: versions, states, transitions, validation and publishing. */
export function WorkflowDefinitionPage() {
  const { workflowId = '' } = useParams();
  const workflows = useWorkflows();
  const versions = useWorkflowVersions(workflowId);
  const mutations = useWorkflowDefinitionMutations();
  const toast = useToast();
  const [selectedVersionId, setSelectedVersionId] = useState('');
  const [stateName, setStateName] = useState('');
  const [stateType, setStateType] = useState<string>('Task');
  const [approvers, setApprovers] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [message, setMessage] = useState<{ tone: 'error' | 'success' | 'warning'; text: string; items?: string[] } | null>(null);

  const workflow = workflows.data?.find((candidate) => candidate.id === workflowId);
  const versionList = [...(versions.data ?? [])].sort((a, b) => b.version - a.version);
  const version = versionList.find((candidate) => candidate.id === selectedVersionId) ?? versionList[0];
  const graph = useWorkflowGraph(version?.id);
  const editable = !!version && !version.isPublished;

  const run = async (action: () => Promise<unknown>, success?: string) => {
    setMessage(null);
    try { await action(); if (success) toast.success(success); } catch (cause) { setMessage({ tone: 'error', text: toUserMessage(cause) }); }
  };

  const nameOfState = (id: string) => graph.data?.states.find((state) => state.id === id)?.name ?? id.slice(0, 8);

  return (
    <QueryBoundary query={versions} loadingLabel="Loading workflow">
      {() => (
        <>
          <p className="mb-2"><Link to="/workflows" className="text-sm font-medium text-teal-800 underline">Back to workflows</Link></p>
          <PageHeader eyebrow={workflow?.code ?? 'Workflow'} title={workflow?.name ?? 'Workflow definition'} description="Add states and transitions, validate the version and publish it to make it startable."
            actions={<>{workflow && <StatusBadge status={workflow.status} />}<PermissionGate permission={CAN_DESIGN} mode="any"><Button onClick={() => void run(() => mutations.createVersion.mutateAsync(workflowId), 'New draft version created.')} loading={mutations.createVersion.isPending}>New version</Button></PermissionGate></>} />

          {message && <div className="mb-4"><Alert tone={message.tone} onClose={() => setMessage(null)}>{message.text}{message.items && <ul className="mt-1 list-disc pl-5">{message.items.map((item) => <li key={item}>{item}</li>)}</ul>}</Alert></div>}

          {versionList.length === 0 ? (
            <EmptyState title="No versions yet" description="Create a version to start designing the workflow." />
          ) : (
            <div className="grid gap-6 lg:grid-cols-2">
              <Card title="Version" actions={version && <Badge tone={version.isPublished ? 'success' : 'neutral'}>{version.isPublished ? 'Published' : 'Draft'}</Badge>}>
                <SelectField label="Version" hideLabel value={version?.id ?? ''} onChange={(event) => setSelectedVersionId(event.target.value)}>
                  {versionList.map((candidate) => <option key={candidate.id} value={candidate.id}>v{candidate.version}{candidate.isPublished ? ' (published)' : ''}</option>)}
                </SelectField>
                <div className="mt-4">
                  {graph.isPending ? <p className="text-sm text-slate-600">Loading states…</p> : graph.data && <WorkflowStateVisualizer states={graph.data.states} transitions={graph.data.transitions} />}
                </div>
                <PermissionGate permission={CAN_DESIGN} mode="any">
                  <div className="mt-4 flex gap-2">
                    <Button loading={mutations.validate.isPending} onClick={() => version && void run(async () => {
                      const result = await mutations.validate.mutateAsync(version.id);
                      setMessage(result.isValid ? { tone: 'success', text: 'The version is valid.' } : { tone: 'warning', text: 'The version has validation problems:', items: result.errors });
                    })}>Validate</Button>
                    <Button variant="primary" disabled={!editable} loading={mutations.publish.isPending} onClick={() => version && void run(() => mutations.publish.mutateAsync({ id: workflowId, version: version.version }), `Version ${version.version} published.`)}>Publish</Button>
                  </div>
                </PermissionGate>
              </Card>

              <Card title="Transitions">
                {graph.data && graph.data.transitions.length === 0 ? <p className="text-sm text-slate-600">No transitions defined.</p> : (
                  <ul className="space-y-2 text-sm">
                    {graph.data?.transitions.map((transition: WorkflowTransition) => <li key={transition.id} className="rounded-lg border border-slate-200 p-2">{nameOfState(transition.fromStateId)} <span aria-label="to">→</span> {nameOfState(transition.toStateId)}</li>)}
                  </ul>
                )}
              </Card>

              <PermissionGate permission={CAN_DESIGN} mode="any">
                <Card title="Add state">
                  {!editable && <p className="mb-3 text-sm text-slate-600">Published versions are read-only. Create a new version to make changes.</p>}
                  <form className="space-y-3" onSubmit={(event) => {
                    event.preventDefault();
                    if (!version || !stateName.trim()) return;
                    const approverUserIds = approvers.split(/[\s,;]+/).filter(Boolean);
                    if (approverUserIds.some((id) => !GUID.test(id))) { setMessage({ tone: 'error', text: 'Approver user IDs must be valid GUIDs separated by commas.' }); return; }
                    void run(async () => { await mutations.addState.mutateAsync({ versionId: version.id, name: stateName.trim(), stateType, approverUserIds: approverUserIds.length > 0 ? approverUserIds : undefined }); setStateName(''); setApprovers(''); }, 'State added.');
                  }}>
                    <TextField label="State name" required value={stateName} disabled={!editable} onChange={(event) => setStateName(event.target.value)} maxLength={200} />
                    <SelectField label="State type" value={stateType} disabled={!editable} onChange={(event) => setStateType(event.target.value)}>
                      {STATE_TYPES.map((type) => <option key={type} value={type}>{humanize(type)}</option>)}
                    </SelectField>
                    {stateType.includes('Approval') && <TextField label="Approver user IDs" value={approvers} disabled={!editable} onChange={(event) => setApprovers(event.target.value)} help="Comma-separated user IDs (GUIDs) who can approve this step." />}
                    <Button type="submit" variant="primary" disabled={!editable || !stateName.trim()} loading={mutations.addState.isPending}>Add state</Button>
                  </form>
                </Card>
                <Card title="Add transition">
                  <form className="space-y-3" onSubmit={(event) => { event.preventDefault(); if (!version || !from || !to) return; void run(async () => { await mutations.addTransition.mutateAsync({ versionId: version.id, fromStateId: from, toStateId: to, order: graph.data?.transitions.length ?? 0 }); setFrom(''); setTo(''); }, 'Transition added.'); }}>
                    <SelectField label="From state" value={from} disabled={!editable} onChange={(event) => setFrom(event.target.value)}>
                      <option value="">Select state</option>
                      {graph.data && orderStates(graph.data.states, graph.data.transitions).map((state) => <option key={state.id} value={state.id}>{state.name}</option>)}
                    </SelectField>
                    <SelectField label="To state" value={to} disabled={!editable} onChange={(event) => setTo(event.target.value)}>
                      <option value="">Select state</option>
                      {graph.data && orderStates(graph.data.states, graph.data.transitions).map((state) => <option key={state.id} value={state.id}>{state.name}</option>)}
                    </SelectField>
                    <Button type="submit" variant="primary" disabled={!editable || !from || !to || from === to} loading={mutations.addTransition.isPending}>Add transition</Button>
                  </form>
                </Card>
              </PermissionGate>
            </div>
          )}
        </>
      )}
    </QueryBoundary>
  );
}
