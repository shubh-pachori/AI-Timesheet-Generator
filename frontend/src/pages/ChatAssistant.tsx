import { useState } from 'react';
import { ChatApi } from '../api/timesheetApi';
import type { User } from '../types';

interface Message { role: 'user' | 'ai'; text: string; }

/** AI Chat: "What did I work on last Thursday?" */
export default function ChatAssistant({ user }: { user: User }) {
  const [messages, setMessages] = useState<Message[]>([
    { role: 'ai', text: `Hi ${user.fullName.split(' ')[0]}, ask me anything about your logged work — e.g. "What did I work on last Thursday?"` }
  ]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);

  const send = async () => {
    if (!input.trim()) return;
    const question = input.trim();
    setMessages(m => [...m, { role: 'user', text: question }]);
    setInput('');
    setLoading(true);
    try {
      const res = await ChatApi.ask(user.id, question);
      setMessages(m => [...m, { role: 'ai', text: res.answer }]);
    } catch {
      setMessages(m => [...m, { role: 'ai', text: 'Sorry, I could not reach the AI service.' }]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <div className="page-header">
        <span className="eyebrow">AI Chat</span>
        <h1>Ask about your work history</h1>
        <p>Grounded in your connected commits, tickets and meetings.</p>
      </div>

      <div className="card">
        <div className="chat-window">
          {messages.map((m, i) => (
            <div key={i} className={`chat-bubble ${m.role === 'user' ? 'chat-user' : 'chat-ai'}`}>{m.text}</div>
          ))}
          {loading && <div className="chat-bubble chat-ai">Thinking…</div>}
        </div>
        <div className="chat-input-row">
          <input
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && send()}
            placeholder="What did I work on last Thursday?"
          />
          <button className="btn btn-accent" onClick={send} disabled={loading}>Send</button>
        </div>
      </div>
    </>
  );
}
