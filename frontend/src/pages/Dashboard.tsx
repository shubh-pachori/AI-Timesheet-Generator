import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { TimesheetApi } from '../api/timesheetApi';
import type { Timesheet, User } from '../types';

/** Returns the Monday of the current week as an ISO date string. */
function currentWeekStart(): string {
  const now = new Date();
  const day = now.getDay();
  const diff = now.getDate() - day + (day === 0 ? -6 : 1);
  const monday = new Date(now.setDate(diff));
  return monday.toISOString().slice(0, 10);
}

export default function Dashboard({ user }: { user: User }) {
  const [timesheets, setTimesheets] = useState<Timesheet[]>([]);
  const [loading, setLoading] = useState(true);
  const [generating, setGenerating] = useState(false);
  const navigate = useNavigate();

  const load = async () => {
    setLoading(true);
    const data = await TimesheetApi.getForUser(user.id);
    setTimesheets(data);
    setLoading(false);
  };

  useEffect(() => { load(); }, [user.id]);

  const generate = async () => {
    setGenerating(true);
    try {
      const sheet = await TimesheetApi.generate(user.id, currentWeekStart());
      await load();
      navigate(`/timesheet/${sheet.id}`);
    } finally {
      setGenerating(false);
    }
  };

  return (
    <>
      <div className="page-header">
        <span className="eyebrow">Step 3–4 · Fetch + AI Processing</span>
        <h1>Welcome back, {user.fullName.split(' ')[0]}</h1>
        <p>Generate this week's timesheet from your commits, tickets and meetings in one click.</p>
      </div>

      <div className="card" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <strong style={{ fontSize: 15 }}>Week of {currentWeekStart()}</strong>
          <p style={{ margin: '4px 0 0', color: 'var(--text-secondary)', fontSize: 13.5 }}>
            Pulls from every connected tool — Git, Jira, Azure DevOps, Outlook.
          </p>
        </div>
        <button className="btn btn-accent" onClick={generate} disabled={generating}>
          {generating ? 'Generating with AI…' : 'Generate this week\u2019s timesheet'}
        </button>
      </div>

      <div className="card">
        <strong style={{ fontSize: 15 }}>Your timesheets</strong>
        {loading ? (
          <p style={{ color: 'var(--text-secondary)', fontSize: 13.5 }}>Loading…</p>
        ) : timesheets.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)', fontSize: 13.5 }}>
            No timesheets yet — generate your first one above.
          </p>
        ) : (
          <table className="ts-table" style={{ marginTop: 12 }}>
            <thead>
              <tr>
                <th>Week</th>
                <th>Status</th>
                <th>Total hours</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {timesheets.map(t => (
                <tr key={t.id}>
                  <td>{t.weekStartDate} → {t.weekEndDate}</td>
                  <td><span className={`status-pill status-${t.status}`}>{t.status}</span></td>
                  <td>{t.entries.reduce((s, e) => s + e.hours, 0)}h</td>
                  <td><button className="btn btn-outline btn-sm" onClick={() => navigate(`/timesheet/${t.id}`)}>Open</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </>
  );
}
