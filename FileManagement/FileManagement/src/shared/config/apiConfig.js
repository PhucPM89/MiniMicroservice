const rawApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim() || 'http://localhost:5201'

export const API_BASE_URL = rawApiBaseUrl.replace(/\/+$/, '')
