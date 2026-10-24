import type { WorkflowHistoryEntry, WorkflowState, WorkflowTransition } from '../../api/contracts';
import { Timeline, type TimelineItem } from '../../components/timeline/Timeline';
import { humanize } from '../../utils/status';

interface Props {
  states: WorkflowState[];
  transitions: WorkflowTransition[];
  currentStateId?: string;
  instanceStatus?: string;
  history?: WorkflowHistoryEntry[];
}

const TERMINAL_FAILURE = new Set(['Rejected', 'Cancelled', 'Failed']);

/** Display order only (breadth-first from the start state); the Workflow service owns every state transition. */
export function orderStates(states: WorkflowState[], transitions: WorkflowTransition[]): WorkflowState[] {
  const byId = new Map(states.map((state) => [state.id, state]));
  const start = states.find((state) => state.stateType === 'Start') ?? states[0];
  const ordered: WorkflowState[] = [];
  const seen = new Set<string>();
  const queue = start ? [start.id] : [];
  while (queue.length > 0) {
    const id = queue.shift()!;
    if (seen.has(id)) continue;
    seen.add(id);
    const state = byId.get(id);
    if (state) ordered.push(state);
    transitions.filter((transition) => transition.fromStateId === id).sort((a, b) => a.order - b.order).forEach((transition) => queue.push(transition.toStateId));
  }
  return [...ordered, ...states.filter((state) => !seen.has(state.id))];
}

export function WorkflowStateVisualizer({ states, transitions, currentStateId, instanceStatus, history = [] }: Props) {
  const visited = new Set(history.map((entry) => entry.stateId).filter(Boolean));
  const completedInstance = instanceStatus === 'Completed';
  const failed = !!instanceStatus && TERMINAL_FAILURE.has(instanceStatus);
  const items: TimelineItem[] = orderStates(states, transitions).map((state) => {
    const isCurrent = state.id === currentStateId;
    const itemState: TimelineItem['state'] = isCurrent
      ? (completedInstance ? 'completed' : failed ? 'failed' : 'current')
      : visited.has(state.id) || completedInstance ? 'completed' : 'upcoming';
    return { id: state.id, title: state.name, description: humanize(String(state.stateType)), state: itemState };
  });
  if (items.length === 0) return <p className="text-sm text-slate-600">The workflow definition has no states.</p>;
  return <Timeline label="Workflow progress" items={items} />;
}
