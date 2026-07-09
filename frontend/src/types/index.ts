export interface User {
  id: string;
  fullName: string;
  email: string;
  role: string;
}

export interface ConnectionStatus {
  provider: string;
  isConnected: boolean;
  connectedAt: string | null;
}

export interface TimesheetEntry {
  id: string;
  date: string;
  description: string;
  hours: number;
  devHours: number;
  meetingHours: number;
  reviewHours: number;
  isEdited: boolean;
}

export interface Timesheet {
  id: string;
  userId: string;
  weekStartDate: string;
  weekEndDate: string;
  status: 'Draft' | 'Generated' | 'Submitted' | 'Approved' | 'Rejected';
  weeklySummary: string | null;
  entries: TimesheetEntry[];
}

export interface Activity {
  id: string;
  source: string;
  title: string;
  status: string | null;
  activityDate: string;
  estimatedHours: number | null;
}
