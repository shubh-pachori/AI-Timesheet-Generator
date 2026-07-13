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
      <div className="user-chip" style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <img 
            src="/avatar_priya.jpg" 
            alt={user.fullName} 
            style={{ 
              width: '40px', 
              height: '40px', 
              borderRadius: '50%', 
              border: '2px solid var(--accent-primary)',
              objectFit: 'cover'
            }} 
            onError={(e) => {
              // fallback if image not found or fails to load
              e.currentTarget.style.display = 'none';
            }}
          />
          <div>
            <strong style={{ display: 'block', color: 'var(--text-primary)', fontSize: '14px', fontWeight: 600 }}>
              {user.fullName}
            </strong>
            <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>
              {user.role}
            </span>
          </div>
        </div>
        <div style={{ fontSize: '12.5px', color: 'var(--text-muted)', wordBreak: 'break-all' }}>
          {user.email}
        </div>
        <div>
          <button className="btn btn-outline btn-sm" style={{ width: '100%' }} onClick={onLogout}>
            Sign out
          </button>
        </div>
      </div>
    </aside>
  );
}
