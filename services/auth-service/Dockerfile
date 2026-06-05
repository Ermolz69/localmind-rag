FROM node:24-alpine AS builder

WORKDIR /app

# Enable pnpm
RUN corepack enable && corepack prepare pnpm@latest --activate

# Copy package config
COPY package.json pnpm-lock.yaml* ./

# Install dependencies (frozen lockfile might fail if workspace depends on other things, but here it's isolated)
# We will use pnpm install directly since we separated it.
RUN pnpm install --frozen-lockfile || pnpm install

# Copy source code
COPY . .

# Build NestJS app
RUN pnpm run build

# Production image
FROM node:24-alpine

WORKDIR /app

# Copy production dependencies and built app
COPY --from=builder /app/node_modules ./node_modules
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/package.json ./

EXPOSE 3001
ENV NODE_ENV=production

ENTRYPOINT ["node", "dist/main"]
