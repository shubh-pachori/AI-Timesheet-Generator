import { useState } from 'react';
import type { TimesheetEntry } from '../types';

interface Props {
  entries: TimesheetEntry[];
  editable: boolean;
  onSave: (entryId: string, hours: number, description: string) => Promise<void>;
}

const dayLabel = (iso: string) =>
  new Date(iso).toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' });

export default function TimesheetTable({ entries, editable, onSave }: Props) {
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draftHours, setDraftHours] = useState<number>(0);
  const [draftDesc, setDraftDesc] = useState<string>('');
  const [saving, setSaving] = useState(false);

  const startEdit = (e: TimesheetEntry) => {
    setEditingId(e.id);
    setDraftHours(e.hours);
    setDraftDesc(e.description);
  };

  const save = async (id: string) => {
    setSaving(true);
    await onSave(id, draftHours, draftDesc);
    setSaving(false);
    setEditingId(null);
  };

  const totalHours = entries.reduce((sum, e) => sum + e.hours, 0);

  return (
    <table className="ts-table">
      <thead>
        <tr>
          <th style={{ width: '18%' }}>Date</th>
          <th>AI-generated activity</th>
          <th style={{ width: '10%' }}>Hours</th>
          {editable && <th style={{ width: '10%' }}></th>}
        </tr>
      </thead>
      <tbody>
        {entries.map(e => (
          <tr key={e.id}>
            <td>{dayLabel(e.date)}</td>
            <td>
              {editingId === e.id ? (
                <textarea className="desc-edit" value={draftDesc} onChange={ev => setDraftDesc(ev.target.value)} />
              ) : (
                <>
                  {e.description}
                  {e.isEdited && <span style={{ marginLeft: 8, fontSize: 11, color: 'var(--text-secondary)' }}>(edited)</span>}
                </>
              )}
            </td>
            <td>
              {editingId === e.id ? (
                <input
                  className="hours-input"
                  type="number"
                  step="0.5"
                  value={draftHours}
                  onChange={ev => setDraftHours(parseFloat(ev.target.value) || 0)}
                />
              ) : (
                <span className="hours-badge">{e.hours}h</span>
              )}
            </td>
            {editable && (
              <td>
                {editingId === e.id ? (
                  <button className="btn btn-accent btn-sm" disabled={saving} onClick={() => save(e.id)}>
                    {saving ? '…' : 'Save'}
                  </button>
                ) : (
                  <button className="btn btn-outline btn-sm" onClick={() => startEdit(e)}>Edit</button>
                )}
              </td>
            )}
          </tr>
        ))}
        <tr>
          <td></td>
          <td style={{ fontWeight: 600 }}>Total</td>
          <td style={{ fontWeight: 600 }}>{totalHours}h</td>
          {editable && <td></td>}
        </tr>
      </tbody>
    </table>
  );
}
