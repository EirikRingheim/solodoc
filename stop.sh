#!/bin/bash

cd "$(dirname "$0")"

echo "=== Solodoc Stop ==="

# Kill tracked PIDs
if [ -f /tmp/solodoc-api.pid ]; then
    kill "$(cat /tmp/solodoc-api.pid)" 2>/dev/null && echo "Stopped API" || true
    rm -f /tmp/solodoc-api.pid
fi

if [ -f /tmp/solodoc-client.pid ]; then
    kill "$(cat /tmp/solodoc-client.pid)" 2>/dev/null && echo "Stopped Client" || true
    rm -f /tmp/solodoc-client.pid
fi

# Kill any remaining dotnet processes for this project
pkill -f "dotnet.*Solodoc" 2>/dev/null || true
pkill -f "dotnet run --project src/" 2>/dev/null || true

echo "Done. Docker services are still running."
echo "Run 'docker-compose down' to stop those too."
