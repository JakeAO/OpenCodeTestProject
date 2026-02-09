#!/bin/bash
set -e

# Initialize Nakama module with RPC functions and hooks

echo "Initializing OCTP Nakama server..."

# Create data directory if needed
mkdir -p data/nakama

# Ensure Docker daemon is running before starting containers
if ! docker info >/dev/null 2>&1; then
	echo "Docker does not appear to be running. Attempting to start Docker Desktop..."
	open -a Docker >/dev/null 2>&1 || true

	# Wait for Docker to become ready (max 60s)
	for i in {1..30}; do
		if docker info >/dev/null 2>&1; then
			break
		fi
		sleep 2
	done

	if ! docker info >/dev/null 2>&1; then
		echo "Docker failed to start. Please start Docker and retry."
		exit 1
	fi
fi

# Start services
echo "Starting Docker containers..."
docker-compose up -d --force-recreate

# Wait for services to be ready
echo "Waiting for services to be ready..."
sleep 10

# Check health
echo "Checking Nakama health..."
curl -f http://localhost:7351/ || exit 1

echo "✓ Nakama server started successfully"
echo ""
echo "Access Nakama Admin Console at: http://localhost:7352"
echo "Default credentials: admin@example.com / password"
echo ""
echo "API endpoints:"
echo "  gRPC: localhost:7350"
echo "  HTTP: localhost:7351"
