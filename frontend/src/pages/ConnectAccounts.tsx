import { useEffect, useState } from 'react';
import { IntegrationApi } from '../api/timesheetApi';
import type { ConnectionStatus, User } from '../types';

const PROVIDER_META: Record<string, { label: string; sub: string }> = {
  GitHub: { label: 'GitHub', sub: 'Commits, pull requests, code reviews' },
  AzureDevOps: { label: 'Azure DevOps', sub: 'Work items, boards, pull requests' },
  Jira: { label: 'Jira', sub: 'Issues, sprints, ticket status' },
  OutlookCalendar: { label: 'Outlook Calendar', sub: 'Meetings & events' },
  TeamsCalendar: { label: 'Teams Calendar', sub: 'Calls & meetings (optional)' },
};

/**
 * Step 2: Connect Accounts.
 * In production, "Connect" opens each provider's OAuth consent screen
 * (GitHub OAuth App, Azure DevOps PAT/OAuth, Jira OAuth 2.0, Microsoft Graph).
 * For the hackathon demo, connecting stores a placeholder token so the
 * generate step has something to fetch against (falls back to mock data
 * automatically if the token isn't a real one).
 */
export default function ConnectAccounts({ user }: { user: User }) {
  const [statuses, setStatuses] = useState<ConnectionStatus[]>([]);
  const [busy, setBusy] = useState<string | null>(null);

  const load = async () => setStatuses(await IntegrationApi.status(user.id));
  useEffect(() => { load(); }, [user.id]);

  const toggle = async (provider: string, isConnected: boolean) => {
    setBusy(provider);
    try {
      if (isConnected) {
        await IntegrationApi.disconnect(user.id, provider);
      } else {
        await IntegrationApi.connect(user.id, provider, `demo-token-${provider.toLowerCase()}`);
      }
      await load();
    } finally {
      setBusy(null);
    }
  };

  return (
    <>
      <div className="page-header">
        <span className="eyebrow">Step 2 · Connect Accounts</span>
        <h1>Connect your work tools</h1>
        <p>The AI engine only reads activity from tools you explicitly connect.</p>
      </div>

      <div className="card">
        {Object.entries(PROVIDER_META).map(([key, meta]) => {
          const status = statuses.find(s => s.provider === key);
          const isConnected = status?.isConnected ?? false;
          return (
            <div className="provider-tile" key={key}>
              <div>
                <div className="name">
                  <span className={`dot ${isConnected ? 'dot-on' : 'dot-off'}`} />
                  {meta.label}
                </div>
                <div className="sub">{meta.sub}</div>
              </div>
              <button
                className={isConnected ? 'btn btn-outline btn-sm' : 'btn btn-primary btn-sm'}
                disabled={busy === key}
                onClick={() => toggle(key, isConnected)}
              >
                {busy === key ? '…' : isConnected ? 'Disconnect' : 'Connect'}
              </button>
            </div>
          );
        })}
      </div>
    </>
  );
}
