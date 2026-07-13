import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5080/api';

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' }
});

// Axios request interceptor to attach JWT token
apiClient.interceptors.request.use(config => {
  const cachedUserStr = sessionStorage.getItem('ai-timesheet-user');
  if (cachedUserStr) {
    try {
      const cached = JSON.parse(cachedUserStr);
      if (cached && cached.token) {
        config.headers.Authorization = `Bearer ${cached.token}`;
      }
    } catch (e) {
      console.error(e);
    }
  }
  return config;
}, error => {
  return Promise.reject(error);
});
