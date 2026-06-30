FROM node:22-alpine AS deps
WORKDIR /app

COPY Companion.Web/package.json Companion.Web/package-lock.json* ./
RUN npm ci

FROM node:22-alpine AS build
WORKDIR /app

ARG NEXT_PUBLIC_API_BASE_URL=http://localhost:8080
ARG NEXT_PUBLIC_OAUTH_REDIRECT_ORIGIN=
ENV NEXT_PUBLIC_API_BASE_URL=${NEXT_PUBLIC_API_BASE_URL}
ENV NEXT_PUBLIC_OAUTH_REDIRECT_ORIGIN=${NEXT_PUBLIC_OAUTH_REDIRECT_ORIGIN}

COPY --from=deps /app/node_modules ./node_modules
COPY Companion.Web ./
RUN npm run build

FROM node:22-alpine AS final
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public

EXPOSE 3000
CMD ["node", "server.js"]
