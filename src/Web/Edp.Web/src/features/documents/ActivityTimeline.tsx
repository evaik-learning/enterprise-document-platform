import type { TimelineEvent } from '../../api/contracts';
import { Badge } from '../../components/ui/Badge';
import { EmptyState } from '../../components/feedback/Feedback';
import { Timeline, type TimelineItem } from '../../components/timeline/Timeline';

/** Unified activity feed (document, workflow and signature events composed by the Gateway). */
export function ActivityTimeline({ events }: { events: TimelineEvent[] }) {
  if (events.length === 0) return <EmptyState title="No activity yet" description="Events appear here as the document is generated, routed and signed." />;
  const items: TimelineItem[] = events.map((event) => ({
    id: event.id,
    title: event.title,
    description: event.description,
    timestamp: event.timestamp,
    state: event.status === 'failed' ? 'failed' : 'completed',
    meta: <span className="ml-2"><Badge>{event.source}</Badge></span>
  }));
  return <Timeline label="Document activity" items={items} />;
}
