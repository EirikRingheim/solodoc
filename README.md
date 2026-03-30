# Solodoc

Quality and project management system for construction workers, machine operators, and farmers.

## Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js 18+ LTS](https://nodejs.org/) (for Playwright E2E tests)
- [Claude Code](https://code.claude.com/) (development agent)
- [Git](https://git-scm.com/)

### 1. Clone and configure
```bash
git clone <your-repo-url>
cd solodoc
cp .env.example .env
# Edit .env with your values (or keep defaults for local dev)
```

### 2. Start infrastructure
```bash
docker-compose up -d
```

This starts:
| Service | URL | Purpose |
|---------|-----|---------|
| PostgreSQL | localhost:5432 | Database |
| MinIO | localhost:9000 | File storage (S3-compatible) |
| MinIO Console | localhost:9001 | File storage web UI |
| SEQ | localhost:8081 | Log viewer |
| MailHog | localhost:8025 | Email testing (catches all emails) |

### 3. Run the application
```bash
# Apply database migrations
dotnet ef database update -p src/Infrastructure -s src/Api

# Run the API (backend)
dotnet run --project src/Api

# In a separate terminal — run the Client (frontend)
dotnet run --project src/Client
```

### 4. Development with Claude Code
```bash
cd ~/projects/solodoc
claude
```
Claude Code reads `CLAUDE.md` automatically and understands the project conventions.

## Project Structure
```
solodoc/
├── CLAUDE.md                    # Project conventions (Claude Code reads this)
├── .claude/settings.json        # Claude Code permissions
├── docker-compose.yml           # Local infrastructure
├── .env.example                 # Environment variable template
├── .gitignore                   # Git ignore rules
├── docs/
│   └── modules/                 # Detailed module specifications
│       ├── auth-and-onboarding.md
│       ├── dashboard-and-homescreen.md
│       ├── template-builder.md
│       ├── digital-signature.md
│       ├── audit-trail-and-export.md
│       ├── task-groups.md
│       ├── full-text-search.md
│       ├── chemical-register.md
│       ├── reporting-analytics.md
│       ├── multi-language.md
│       ├── gdpr-data-retention.md
│       ├── tutorials-help-system.md
│       └── data-migration.md
├── src/
│   ├── Api/                     # ASP.NET Core Minimal API endpoints
│   ├── Application/             # Business logic and service interfaces
│   ├── Shared/                  # DTOs and validators (shared with Client)
│   ├── Domain/                  # Entities, value objects, enums
│   ├── Infrastructure/          # EF Core, external services, file storage
│   ├── Client/                  # Blazor WASM PWA frontend
│   └── Worker/                  # Quartz.NET background jobs
└── tests/
    ├── UnitTests/
    ├── IntegrationTests/
    ├── E2ETests/                # Playwright
    └── ComponentTests/          # bUnit
```

## Environments

| Environment | Domain | Purpose |
|-------------|--------|---------|
| Development | localhost | Your machine, Docker Compose |
| Staging | test.solodoc.app | Test environment, mirrors production |
| Production | solodoc.app | Real users |
| Landing page | solodoc.no | Marketing site (separate repo) |

## Tech Stack
.NET 10 · Blazor WASM (PWA) · PostgreSQL · MinIO (S3) · MudBlazor · 
EF Core · FluentValidation · QuestPDF · Serilog + SEQ · Quartz.NET · 
DeepL API · Anthropic API · Tesseract OCR

## License
Proprietary — All rights reserved.
