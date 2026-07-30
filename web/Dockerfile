# Multi-stage build for the Angular SPA: Node builds the static bundle, nginx serves it (and reverse-
# proxies /api to the API container). Build context is ./web (see docker-compose.yml).
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# Production build applies the fileReplacement so API_URL becomes the relative "/api".
RUN npm run build

FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/web/browser /usr/share/nginx/html
EXPOSE 80
