// Production build (Docker/nginx): the SPA and API share an origin — nginx reverse-proxies /api to the
// API container, so the browser calls a relative path and there is no CORS in production.
export const API_URL = '/api';
