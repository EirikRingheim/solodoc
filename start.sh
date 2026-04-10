#!/bin/bash
set -e

cd "$(dirname "$0")"

echo "=== Solodoc Start ==="

# 1. Kill existing dotnet processes
echo "Stopping existing dotnet processes..."
pkill -f "dotnet.*Solodoc" 2>/dev/null || true
pkill -f "dotnet run --project src/" 2>/dev/null || true
sleep 1

# 2. Ensure Docker services are running
echo "Starting Docker services..."
docker-compose up -d
echo "Waiting for PostgreSQL..."
until docker exec solodoc-postgres pg_isready -U solodoc -q 2>/dev/null; do
    sleep 1
done
echo "PostgreSQL is ready."

# 3. Build and apply pending migrations
echo "Building..."
rm -rf tests/*/bin tests/*/obj 2>/dev/null
dotnet build src/Api --nologo -q 2>/dev/null
dotnet build src/Client --nologo -q 2>/dev/null
echo "Applying database migrations..."
dotnet ef database update -p src/Infrastructure -s src/Api 2>/dev/null || echo "Migration warning (may be OK)"

# 4. Start API in background
echo "Starting API..."
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Api > /tmp/solodoc-api.log 2>&1 &
API_PID=$!
echo "API PID: $API_PID"

# Wait for API to be ready
echo "Waiting for API..."
for i in $(seq 1 30); do
    if curl -s http://localhost:5078/health > /dev/null 2>&1; then
        break
    fi
    sleep 1
done

if ! curl -s http://localhost:5078/health > /dev/null 2>&1; then
    echo "ERROR: API failed to start. Check /tmp/solodoc-api.log"
    exit 1
fi
echo "API is ready."

# 5. Start Client in background
echo "Starting Client..."
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Client > /tmp/solodoc-client.log 2>&1 &
CLIENT_PID=$!
echo "Client PID: $CLIENT_PID"

# Wait for Client to be ready
echo "Waiting for Client..."
for i in $(seq 1 30); do
    if curl -s http://localhost:5063 > /dev/null 2>&1; then
        break
    fi
    sleep 1
done

# Save PIDs for stop.sh
echo "$API_PID" > /tmp/solodoc-api.pid
echo "$CLIENT_PID" > /tmp/solodoc-client.pid

echo ""
echo "=== Solodoc is running ==="
echo ""
echo "  Client:   http://localhost:5063"
echo "  API:      http://localhost:5078"
echo "  API docs: http://localhost:5078/scalar"
echo "  Mailpit:  http://localhost:8025"
echo "  SEQ logs: http://localhost:8081"
echo "  MinIO:    http://localhost:9001"
echo ""
echo "  Login:    admin@solodoc.dev / Admin1234!"
echo ""
echo "  Logs:     /tmp/solodoc-api.log"
echo "            /tmp/solodoc-client.log"
echo ""
echo "  Stop:     ./stop.sh"
echo ""
