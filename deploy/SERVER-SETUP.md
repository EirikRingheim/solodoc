# Server Setup Guide

## 1. Get a VPS

Recommended: **Hetzner Cloud** (cheapest EU servers, GDPR-compliant)
- CX22: 2 vCPU, 4GB RAM, 40GB SSD — ~€4.35/month
- Location: Helsinki or Falkenstein (EU)

Alternatives: DigitalOcean, Vultr, Linode

## 2. Initial Server Setup

SSH into your new server:

```bash
ssh root@YOUR_SERVER_IP
```

Install Docker:

```bash
# Ubuntu 22.04/24.04
curl -fsSL https://get.docker.com | sh
```

Create a deploy user (optional but recommended):

```bash
adduser solodoc
usermod -aG docker solodoc
su - solodoc
```

## 3. Point Your Domain

In your domain registrar's DNS settings, add:

```
Type: A
Name: app (or @ for root domain)
Value: YOUR_SERVER_IP
TTL: 300
```

Wait for DNS propagation (usually 5-15 minutes).

## 4. Deploy Solodoc

```bash
# Clone the repo
git clone https://github.com/YOUR_USERNAME/solodoc.git
cd solodoc

# Create production env file
cp deploy/.env.production.template .env.production

# Generate secrets
echo "JWT_SECRET=$(openssl rand -base64 64 | tr -d '\n')" 
echo "POSTGRES_PASSWORD=$(openssl rand -base64 32 | tr -d '\n')"
echo "MINIO_SECRET_KEY=$(openssl rand -base64 32 | tr -d '\n')"

# Edit .env.production with your values
nano .env.production

# Deploy!
./deploy/deploy.sh
```

## 5. Verify

Visit `https://your-domain.no` — you should see the Solodoc login page.

Caddy automatically obtains and renews HTTPS certificates from Let's Encrypt.

## 6. First Login

If this is a fresh database, seed data runs in Development only.
To create the first admin user in production, use the API directly:

```bash
curl -X POST https://your-domain.no/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@yourcompany.no","password":"YourSecurePassword1","fullName":"Admin"}'
```

Then create a tenant through the app's onboarding flow.

## Useful Commands

```bash
# View logs
docker compose -f docker-compose.production.yml logs -f

# View specific service logs
docker compose -f docker-compose.production.yml logs -f api

# Restart a service
docker compose -f docker-compose.production.yml restart api

# Stop everything
docker compose -f docker-compose.production.yml down

# Update and redeploy
git pull && ./deploy/deploy.sh

# Database backup
docker compose -f docker-compose.production.yml exec postgres \
  pg_dump -U solodoc solodoc > backup_$(date +%Y%m%d).sql

# Restore database
cat backup.sql | docker compose -f docker-compose.production.yml exec -T postgres \
  psql -U solodoc solodoc
```

## Monitoring

SEQ (structured logging) runs internally on port 5341.
To access it from your machine, SSH tunnel:

```bash
ssh -L 8081:localhost:5341 root@YOUR_SERVER_IP
# Then open http://localhost:8081
```

## Firewall

Only ports 80 and 443 need to be open:

```bash
ufw allow 22    # SSH
ufw allow 80    # HTTP (Caddy redirects to HTTPS)
ufw allow 443   # HTTPS
ufw enable
```
