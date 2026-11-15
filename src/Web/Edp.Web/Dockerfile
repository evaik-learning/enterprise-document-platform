# Production web image: static React build served by nginx, reverse-proxying /api and /bff to the Gateway.
# Build from src/Web/Edp.Web:  docker build -t edp-web .
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
ARG VITE_APP_NAME="Enterprise Document Platform"
ARG VITE_APP_VERSION=0.0.0
ARG VITE_ENVIRONMENT=production
ARG VITE_FEATURE_SIGNATURE=true
ARG VITE_FEATURE_REALTIME=false
ARG VITE_FEATURE_ADVANCED_AUDIT=true
ARG VITE_FEATURE_MULTI_ORGANIZATION=true
# Public values only: everything under VITE_ is bundled into the browser code.
ENV VITE_APP_NAME=$VITE_APP_NAME VITE_APP_VERSION=$VITE_APP_VERSION VITE_ENVIRONMENT=$VITE_ENVIRONMENT \
    VITE_FEATURE_SIGNATURE=$VITE_FEATURE_SIGNATURE VITE_FEATURE_REALTIME=$VITE_FEATURE_REALTIME \
    VITE_FEATURE_ADVANCED_AUDIT=$VITE_FEATURE_ADVANCED_AUDIT VITE_FEATURE_MULTI_ORGANIZATION=$VITE_FEATURE_MULTI_ORGANIZATION
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.27-alpine
# nginx's entrypoint renders templates with the container's environment variables.
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
ENV GATEWAY_URL=http://gateway:8080
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s CMD wget -q -O /dev/null http://127.0.0.1:8080/healthz || exit 1
