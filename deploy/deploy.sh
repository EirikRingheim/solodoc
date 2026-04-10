#!/bin/bash
set -e

# =============================================================================
# Solodoc Production Deployment Script
# =============================================================================
# Usage:
#   1. Copy .env.production.template to .env.production and fill in values
#   2. SSH into your server
#   3. Clone the repo
#   4. Run: ./deploy/deploy.sh
#
# Prerequisites:
#   - Docker and Docker Compose installed
#   - Domain DNS pointing to this server
#   - Port 80 and 443 open in firewall
# =============================================================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$PROJECT_DIR/.env.production"

echo "=== Solodoc Production Deployment ==="
echo ""

# Check for .env.production
if [ ! -f "$ENV_FILE" ]; then
    echo "ERROR: .env.production not found!"
    echo ""
    echo "Create it from the template:"
    echo "  cp deploy/.env.production.template .env.production"
    echo "  nano .env.production"
    echo ""
    exit 1
fi

# Load env vars for validation
source "$ENV_FILE"

# Validate required vars
REQUIRED_VARS=(DOMAIN POSTGRES_PASSWORD JWT_SECRET MINIO_SECRET_KEY)
for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var}" ] || [[ "${!var}" == *"CHANGE_ME"* ]] || [[ "${!var}" == *"GENERATE"* ]]; then
        echo "ERROR: $var is not set or still has placeholder value"
        exit 1
    fi
done

echo "Domain: $DOMAIN"
echo "Database: $POSTGRES_DB"
echo ""

# Generate secrets if they look like placeholders
if [ ${#JWT_SECRET} -lt 32 ]; then
    echo "WARNING: JWT_SECRET should be at least 32 characters long"
    echo "Generate one with: openssl rand -base64 64"
    exit 1
fi

cd "$PROJECT_DIR"

echo "=== Pulling latest changes ==="
git pull origin main 2>/dev/null || echo "Not a git repo or no remote, skipping pull"

echo ""
echo "=== Building Docker images ==="
docker compose -f docker-compose.production.yml --env-file .env.production build

echo ""
echo "=== Starting services ==="
docker compose -f docker-compose.production.yml --env-file .env.production up -d

echo ""
echo "=== Waiting for database to be ready ==="
sleep 5

echo ""
echo "=== Running database migrations ==="
docker compose -f docker-compose.production.yml --env-file .env.production exec api \
    dotnet Solodoc.Api.dll --migrate 2>/dev/null || \
    echo "Auto-migration not configured. Run migrations manually if needed."

echo ""
echo "=== Checking service health ==="
sleep 3

# Check API health
API_HEALTH=$(docker compose -f docker-compose.production.yml exec -T web curl -s http://api:5078/health 2>/dev/null || echo "")
if echo "$API_HEALTH" | grep -q "healthy"; then
    echo "  API: healthy"
else
    echo "  API: starting up (check logs with: docker compose -f docker-compose.production.yml logs api)"
fi

# Check web
WEB_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost" 2>/dev/null || echo "000")
if [ "$WEB_STATUS" = "200" ] || [ "$WEB_STATUS" = "308" ]; then
    echo "  Web: running"
else
    echo "  Web: starting up (Caddy needs to obtain TLS certificate)"
fi

echo ""
echo "=== Deployment complete! ==="
echo ""
echo "  App:  https://$DOMAIN"
echo "  API:  https://$DOMAIN/api"
echo ""
echo "  View logs:     docker compose -f docker-compose.production.yml logs -f"
echo "  Stop:          docker compose -f docker-compose.production.yml down"
echo "  Update:        git pull && ./deploy/deploy.sh"
echo ""
echo "  Default login:"
echo "    admin@solodoc.dev / Admin1234! (only if seed data ran)"
echo ""
