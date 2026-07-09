import { apiClient } from './client';
import type { Timesheet, ConnectionStatus, Activity, User } from '../types';

export const AuthApi = {
  login: (email: string, fullName: string) =>
    apiClient.post<User>('/auth/login', { email, fullName }).then(r => r.data),
};

export const IntegrationApi = {
  connect: (userId: string, provider: string, accessToken: string) =>
    apiClient.post('/integrations/connect', { userId, provider, accessToken }).then(r => r.data),
  status: (userId: string) =>
    apiClient.get<ConnectionStatus[]>(`/integrations/status/${userId}`).then(r => r.data),
  disconnect: (userId: string, provider: string) =>
    apiClient.delete(`/integrations/${userId}/${provider}`).then(r => r.data),
};

export const TimesheetApi = {
  generate: (userId: string, weekStartDate: string) =>
    apiClient.post<Timesheet>('/timesheets/generate', { userId, weekStartDate }).then(r => r.data),
  getForUser: (userId: string) =>
    apiClient.get<Timesheet[]>(`/timesheets/user/${userId}`).then(r => r.data),
  getById: (id: string) =>
    apiClient.get<Timesheet>(`/timesheets/${id}`).then(r => r.data),
  updateEntry: (timesheetId: string, entryId: string, hours: number, description: string) =>
    apiClient.put(`/timesheets/${timesheetId}/entries/${entryId}`, { hours, description }).then(r => r.data),
  submit: (id: string) =>
    apiClient.post(`/timesheets/${id}/submit`).then(r => r.data),
};

export const ApprovalApi = {
  getPending: (managerId: string) =>
    apiClient.get<Timesheet[]>(`/approvals/pending/${managerId}`).then(r => r.data),
  decide: (timesheetId: string, approve: boolean, comments?: string) =>
    apiClient.post(`/approvals/${timesheetId}/decision`, { approve, comments }).then(r => r.data),
};

export const ActivityApi = {
  getForUser: (userId: string) =>
    apiClient.get<Activity[]>(`/activities/user/${userId}`).then(r => r.data),
};

export const AnalyticsApi = {
  getTeamAnalytics: (managerId: string) =>
    apiClient.get(`/analytics/team/${managerId}`).then(r => r.data),
};

export const ChatApi = {
  ask: (userId: string, question: string) =>
    apiClient.post<{ answer: string }>('/chat/ask', { userId, question }).then(r => r.data),
};
