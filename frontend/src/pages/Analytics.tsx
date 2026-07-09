import { useEffect, useState } from 'react';
import { ApprovalApi, AnalyticsApi } from '../api/timesheetApi';
import HourChart from '../components/HourChart';
import type { Timesheet, User } from '../types';

/** Step 7–8: Manager dashboard — pending approvals + team productivity graphs. */
export default function Analytics({ user }: { user: User }) {
  const [pending, setPending] = useState<Timesheet[]>([]);
  const [analytics, setAnalytics] = useState<any>(null);

  const load = async () => {
    const [p, a] = await Promise.all([
      ApprovalApi.getPending(user.id).catch(() => []),
      AnalyticsApi.getTeamAnalytics(user.id).catch(() => null),
    ]);
    setPending(p);
    setAnalytics(a);
  };

  useEffect(() => { load(); }, [user.id]);

  const decide = async (timesheetId: string, approve: boolean) => {
    await ApprovalApi.decide(timesheetId, approve);
    await load();
  };

  const weeklyLabels = analytics?.weeklyHours?.map((w: any) => w.week) ?? [];
  const weeklyData = analytics?.weeklyHours?.map((w: any) => w.totalHours) ?? [];

  return (
    <>
      <div className="page-header">
        <span className="eyebrow">Step 7–8 · Manager Dashboard</span>
        <h1>Team analytics &amp; approvals</h1>
        <p>Review submitted timesheets and track team productivity.</p>
      </div>

      <div className="card">
        <strong style={{ fontSize: 15 }}>Pending approvals</strong>
        {pending.length === 0 ? (
          <p style={{ color: 'var(--text-secondary)', fontSize: 13.5, marginTop: 8 }}>Nothing waiting on you right now.</p>
        ) : (
          <table className="ts-table" style={{ marginTop: 12 }}>
            <thead>
              <tr><th>Week</th><th>Total hours</th><th></th></tr>
            </thead>
            <tbody>
              {pending.map(t => (
                <tr key={t.id}>
                  <td>{t.weekStartDate} → {t.weekEndDate}</td>
                  <td>{t.entries.reduce((s, e) => s + e.hours, 0)}h</td>
                  <td style={{ display: 'flex', gap: 8 }}>
                    <button className="btn btn-accent btn-sm" onClick={() => decide(t.id, true)}>Approve</button>
                    <button className="btn btn-outline btn-sm" onClick={() => decide(t.id, false)}>Reject</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {weeklyLabels.length > 0 && (
        <div className="card">
          <strong style={{ fontSize: 15 }}>Weekly hours across team</strong>
          <div style={{ marginTop: 12 }}>
            <HourChart labels={weeklyLabels} data={weeklyData} label="Total hours" />
          </div>
        </div>
      )}
    </>
  );
}
