import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { TimesheetApi } from '../api/timesheetApi';
import TimesheetTable from '../components/TimesheetTable';
import type { Timesheet, User } from '../types';

/** Step 5–6: Employee reviews AI-generated entries, edits if needed, then submits. */
export default function TimesheetReview({ user }: { user: User }) {
  const { id } = useParams();
  const navigate = useNavigate();
  const [sheet, setSheet] = useState<Timesheet | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const load = async () => {
    if (!id) return;
    setSheet(await TimesheetApi.getById(id));
  };

  useEffect(() => { load(); }, [id]);

  if (!sheet) return <p style={{ color: 'var(--text-secondary)' }}>Loading timesheet…</p>;

  const editable = sheet.status === 'Generated' || sheet.status === 'Draft';

  const saveEntry = async (entryId: string, hours: number, description: string) => {
    await TimesheetApi.updateEntry(sheet.id, entryId, hours, description);
    await load();
  };

  const submit = async () => {
    setSubmitting(true);
    try {
      await TimesheetApi.submit(sheet.id);
      await load();
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <div className="page-header">
        <span className="eyebrow">Step 5–6 · Review &amp; Submit</span>
        <h1>Week of {sheet.weekStartDate}</h1>
        <p>
          <span className={`status-pill status-${sheet.status}`}>{sheet.status}</span>
        </p>
      </div>

      {sheet.weeklySummary && (
        <div className="card" style={{ background: 'var(--accent-soft)', border: '1px solid var(--accent)' }}>
          <strong style={{ fontSize: 13, color: 'var(--accent-ink)' }}>AI Weekly Summary</strong>
          <p style={{ margin: '6px 0 0', fontSize: 14 }}>{sheet.weeklySummary}</p>
        </div>
      )}

      <div className="card">
        <TimesheetTable entries={sheet.entries} editable={editable} onSave={saveEntry} />
      </div>

      {editable && (
        <div style={{ marginTop: 16, display: 'flex', gap: 10 }}>
          <button className="btn btn-accent" onClick={submit} disabled={submitting}>
            {submitting ? 'Submitting…' : 'Submit for approval'}
          </button>
          <button className="btn btn-outline" onClick={() => navigate('/')}>Back to dashboard</button>
        </div>
      )}
    </>
  );
}
