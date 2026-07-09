import { useState, useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import type { User } from './types';
import Login from './pages/Login';
import Navbar from './components/Navbar';
import Dashboard from './pages/Dashboard';
import ConnectAccounts from './pages/ConnectAccounts';
import TimesheetReview from './pages/TimesheetReview';
import Analytics from './pages/Analytics';
import ChatAssistant from './pages/ChatAssistant';

const STORAGE_KEY = 'ai-timesheet-user';

export default function App() {
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    const cached = sessionStorage.getItem(STORAGE_KEY);
    if (cached) setUser(JSON.parse(cached));
  }, []);

  const handleLogin = (u: User) => {
    setUser(u);
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(u));
  };

  const handleLogout = () => {
    setUser(null);
    sessionStorage.removeItem(STORAGE_KEY);
  };

  if (!user) {
    return <Login onLogin={handleLogin} />;
  }

  return (
    <div className="app-shell">
      <Navbar user={user} onLogout={handleLogout} />
      <div className="main">
        <Routes>
          <Route path="/" element={<Dashboard user={user} />} />
          <Route path="/connect" element={<ConnectAccounts user={user} />} />
          <Route path="/timesheet/:id" element={<TimesheetReview user={user} />} />
          <Route path="/analytics" element={<Analytics user={user} />} />
          <Route path="/chat" element={<ChatAssistant user={user} />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </div>
    </div>
  );
}
