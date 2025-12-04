# Build stage
FROM node:18-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies with legacy peer deps to resolve conflicts
RUN npm install --legacy-peer-deps

# Copy source code
COPY . .

# Build Angular app (gera dist/MBA.Modulo4)
RUN npm run build

# Production stage
FROM nginx:alpine

# Copy built app from build stage (ajustado para nome real do output)
COPY --from=build /app/dist/MBA.Modulo4 /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Expose port
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"] 