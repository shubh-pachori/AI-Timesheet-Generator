import { useState } from 'react';
import { AuthApi } from '../api/timesheetApi';
import type { User } from '../types';

/**
 * Step 1: Login.
 * In production this hands off to MSAL (@azure/msal-react) for Microsoft
 * Entra ID / Office 365 SSO. For the hackathon demo, a lightweight email
 * form calls /api/auth/login directly, which upserts the employee record.
 */
export default function Login({ onLogin }: { onLogin: (user: User) => void }) {
  const [email, setEmail] = useState('');
  const [fullName, setFullName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !fullName) return;
    setLoading(true);
    setError('');
    try {
      const user = await AuthApi.login(email, fullName);
      onLogin(user);
    } catch {
      setError('Could not reach the API. Is the backend running on :5080?');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-screen">
      <div className="login-card">
        <div className="login-mark">T</div>
        <h1>AI Timesheet Generator</h1>
        <p>Sign in to auto-generate this week's timesheet from your commits, tickets and meetings.</p>
        <form onSubmit={submit}>
          <div className="field">
            <label>Full name</label>
            <input value={fullName} onChange={e => setFullName(e.target.value)} placeholder="Priya Sharma" />
          </div>
          <div className="field">
            <label>Work email (Office 365)</label>
            <input type="email" value={email} onChange={e => setEmail(e.target.value)} placeholder="priya@company.com" />
          </div>
          {error && <p style={{ color: 'var(--warn)', fontSize: 12.5 }}>{error}</p>}
          <button className="btn btn-primary" style={{ width: '100%' }} disabled={loading}>
            {loading ? 'Signing in…' : 'Continue with Microsoft'}
          </button>
        </form>
      </div>
    </div>
  );
}
