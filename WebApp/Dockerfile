# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies (use npm install instead of npm ci for flexibility)
RUN npm install --legacy-peer-deps

# Copy source code
COPY . .

# Build the Nuxt app
RUN npm run build

# Production stage
FROM node:22-alpine
WORKDIR /app

# Copy only the build output
COPY --from=build /app/.output ./.output

# Set environment variables
ENV HOST=0.0.0.0
ENV PORT=3000
ENV NODE_ENV=production

EXPOSE 3000

# Start the Nuxt server
CMD ["node", ".output/server/index.mjs"]