import { NavLink } from 'react-router-dom';
import type { User } from '../types';

export default function Navbar({ user, onLogout }: { user: User; onLogout: () => void }) {
  const initials = user.fullName.split(' ').map(p => p[0]).join('').slice(0, 2).toUpperCase();

  return (
    <aside className="sidebar">
      <div className="brand">
        Timesheet<small>AI Generator</small>
      </div>
      <nav>
        <NavLink to="/" end className={({ isActive }) => (isActive ? 'active' : '')}>Dashboard</NavLink>
        <NavLink to="/connect" className={({ isActive }) => (isActive ? 'active' : '')}>Connect Accounts</NavLink>
        <NavLink to="/analytics" className={({ isActive }) => (isActive ? 'active' : '')}>Analytics</NavLink>
        <NavLink to="/chat" className={({ isActive }) => (isActive ? 'active' : '')}>AI Chat</NavLink>
      </nav>
      <div className="user-chip">
        <strong>{initials} · {user.fullName}</strong>
        {user.role} · {user.email}
        <div style={{ marginTop: 10 }}>
          <button className="btn btn-outline btn-sm" style={{ color: '#e6e2d6', borderColor: '#3a4252' }} onClick={onLogout}>
            Sign out
          </button>
        </div>
      </div>
    </aside>
  );
}
