# CLAUDE.md — Solodoc

## Agent Behavior
- Work autonomously. Make reasonable decisions based on CLAUDE.md conventions.
- Do NOT ask for confirmation unless the situation is genuinely ambiguous or high-risk.
- Prefer making a decision and explaining it over stopping to ask.
- When generating UI, always design mobile-first — desktop is the secondary target.
- **No stock emojis anywhere in the UI** — use professional, consistent line icons (Lucide).
- Always read the relevant module spec in `/docs/modules/` before implementing a feature.

## Overview
Solodoc is a multi-tenant quality and project management system built for
construction workers, machine operators, and farmers in Norway. It handles:

- **Projects (Prosjekt)** — large, admin-created with full lifecycle: checklists, documentation, competence requirements, task groups
- **Jobs (Oppdrag)** — lightweight, employee-created for quick service calls, repairs, customer visits (companies and private individuals)
- **Hours & time tracking** — clock-in/out, hours per project/job, overtime, export for payroll
- **Deviations (Avvik)** — three-status lifecycle (Åpen → Under behandling → Lukket), corrective actions, photo evidence
- **Checklists & schemas** — unified template builder, OK/Irrelevant buttons, data entry fields, photo capture, signatures
- **Procedures (Prosedyrer)** — block-based document builder for written work procedures
- **Chemical register** — SDS search via SDS Manager database, AI summaries, barcode scanning, GHS pictograms
- **Machine park** — equipment registry, maintenance logs, certifications, usage tracking
- **Contacts** — customers (companies and private individuals), subcontractors, suppliers, inspectors
- **HMS** — SJA, risk assessments, incident reports, safety rounds (scheduled recurring), HMS meetings
- **Employee management** — CV/profile, certifications with OCR expiry extraction, expiry notifications (90/30/0 days)
- **Task Groups** — reusable bundles of checklists, equipment, procedures, chemicals for common work types
- **Worksite check-in** — QR code scanning at site entrance, GPS verification, personnel registry
- **Announcements** — rich text message wall with urgency levels, acknowledgment tracking
- **Reporting & analytics** — pre-built reports with heatmaps, export to PDF/Excel

The primary users are field workers on mobile devices, often with limited connectivity.
The system must work offline and sync when back online.

## Detailed Module Specs
Detailed specifications for each module are in `/docs/modules/`. Always read the
relevant spec before implementing:

- `auth-and-onboarding.md` — BankID registration, user model, subcontractor access, worksite check-in
- `dashboard-and-homescreen.md` — role-adaptive dashboards, announcements, navigation
- `template-builder.md` — checklists, schemas, procedures, versioning, base templates
- `digital-signature.md` — stored signatures, auto-apply with dispute, guest signatures
- `audit-trail-and-export.md` — change history, snapshots, documentation export system
- `task-groups.md` — reusable resource bundles, auto-built crew
- `full-text-search.md` — PostgreSQL tsvector, contextual search, result grouping
- `chemical-register.md` — SDS management, barcode scanning, AI summaries
- `reporting-analytics.md` — pre-built reports, heatmaps, scheduled safety activities
- `multi-language.md` — DeepL translation, 6 languages, AI SDS summaries via Anthropic
- `gdpr-data-retention.md` — consent model, anonymization, data subject rights
- `tutorials-help-system.md` — walkthroughs, contextual help, AI chatbot, feedback
- `data-migration.md` — migration wizard, competitor export guides, import pipeline

## Development Tooling
- This project is developed using **Claude Code** (Anthropic's CLI agent)
- Claude Code reads this CLAUDE.md file to understand project conventions
- The `settings.json` file in `.claude/settings.json` configures Claude Code permissions
- Always keep CLAUDE.md up to date when architectural decisions change
- When a feature is built and tested, generate tutorial content alongside it

## Tech Stack
- .NET 10, ASP.NET Core Minimal APIs
- Entity Framework Core 10 with PostgreSQL (+ pg_trgm extension for fuzzy search)
- PostgreSQL full-text search via tsvector (Norwegian language configuration)
- Use Dependency Injection pattern
- FluentValidation for request validation
- Scalar for OpenAPI documentation
- QuestPDF for server-side PDF generation
- ClosedXML for Excel export
- PdfPig for PDF reading/extraction
- Tesseract (via TesseractSharp or CLI) for OCR on certifications and scanned documents
- xUnit + FluentAssertions for testing
- **Blazor WASM as PWA** — installable, offline-capable, mobile-first
- Workbox (or Blazor PWA tooling) for service worker / caching strategy
- **MinIO** (S3-compatible object storage) for all file/photo storage
- AWSSDK.S3 client library — same code works against MinIO (dev) and AWS S3 (production)
- Serilog + SEQ for structured logging
- Quartz.NET for background jobs
- **DeepL API** for content translation (6 languages)
- **Anthropic API** for AI-generated SDS summaries and help chatbot
- Shepherd.js (via JS interop) for interactive walkthroughs
- ASP.NET Core Identity with JWT Bearer tokens + WebAuthn/FIDO2 passkeys

## Why PWA over Native
- Single codebase for mobile and desktop
- Installable on Android/iOS home screens without app stores
- Service worker enables offline caching and background sync
- Shared Blazor component library between web and mobile experience
- Lower development and maintenance cost than native apps
- If native is needed later, MAUI Blazor Hybrid can wrap the same components

## Project Structure
- `src/Api/` — Endpoints, middleware, DI configuration
- `src/Application/` — Interfaces, services, business logic, import base classes
- `src/Shared/` — DTOs and validators used by both Blazor and API
- `src/Domain/` — Entities, value objects, enums, domain events
- `src/Infrastructure/` — EF Core, external services (DeepL, Anthropic, BankID/BRREG), file storage, OCR
- `src/Client/` — Blazor WASM PWA front-end
- `src/Worker/` — Background service host using Quartz.NET
- `src/Worker/Jobs/` — Quartz Job implementations (only triggers, no business logic)
- `tests/UnitTests/` — Domain and application layer tests
- `tests/IntegrationTests/` — API and database tests
- `tests/E2ETests/` — Playwright end-to-end browser tests
- `tests/ComponentTests/` — bUnit Blazor component tests
- `docs/modules/` — Detailed module specifications

## Database Conventions
- PostgreSQL via Npgsql
- All entities have: Id (Guid), CreatedAt, UpdatedAt, IsDeleted (soft delete), DeletedAt, DeletedBy
- Use snake_case for table and column names (EF Core NamingConvention)
- Migrations must never contain data changes, only schema changes
- Full-text search: tsvector columns on searchable entities, Norwegian language config
- Fuzzy search: pg_trgm extension for typo tolerance
- Audit trail: `AuditEvent` table for event logs, `AuditSnapshot` table for document snapshots
- Translation: `Translation` table (EntityType, EntityId, FieldName, LanguageCode, TranslatedText)

## Authentication & Identity
- ASP.NET Core Identity with JWT Bearer tokens
- Refresh token rotation (15-min access token, 7-day refresh token)
- WebAuthn/FIDO2 passkeys for biometric login (Face ID, fingerprint) — optional
- All endpoints require [Authorize] unless explicitly marked [AllowAnonymous]
- Tenant resolution via JWT claim "tenantId"
- Tokens cached locally for offline identity verification
- On reconnect, refresh token attempt; if expired, prompt re-login
- **Never lose offline work** — queued data persists through re-login
- BankID (via ID-porten/Signicat) for initial tenant registration ONLY, not daily login
- BRREG API + Altinn authorization for company role verification

### Identity Model
- A Person exists independently of any tenant (owns: profile, CV, certifications)
- Tenant membership is separate — roles are per-tenant
- Subcontractor access is per-project, not per-tenant
- Users can exist without any tenant (profile-only state)
- Email is the unique global identifier (one account per email)
- Password: minimum 8 characters, at least 1 uppercase, at least 1 number

## Multi-Tenancy
- Strategy: shared schema with TenantId column on all tenant-scoped entities
- TenantId must be applied as a global query filter in EF Core
- NEVER query tenant-scoped data without the global filter active
- Super-admin role can bypass tenant filters
- Tenant accent color derived from uploaded company logo
- Subscription entity with tier, user limits, feature flags (pricing model TBD)

## GPS & Location
- GPS is a **tenant-level opt-in** feature (disabled by default)
- When admin enables GPS, each employee must individually accept or decline
- Declining GPS does not block any functionality — fields are simply empty
- GPS captured ONLY at moment of action (check-in, clock-in, deviation) — no continuous tracking
- Store as `Latitude` (double) / `Longitude` (double) / `LocationAccuracy` (double, meters)
- GPS consent status stored on tenant membership record, withdrawable anytime

## Offline & Sync Strategy

### Principles
- Core field tasks must work without connectivity
- Data created offline stored in IndexedDB via local sync queue
- Conflict resolution: last-write-wins except deviations/checklists (field-first rule)
- Service worker caches app shell, static assets, recent entity data, chemical register summaries

### What Works Offline
- Clock in/out, checklist completion, deviation reporting, photo capture
- View synced projects, contacts, machines, chemical register (summaries + pictograms)
- SJA forms, worksite check-in (without GPS verification)
- Contextual help and walkthroughs (cached content)

### What Requires Connectivity
- Login / token refresh, admin operations, PDF export, data import
- AI chatbot, template translation, SDS search, real-time dashboard data

### Sync Queue
- JSON operations in IndexedDB: entity type, action, payload, timestamp, GPS
- Replayed in order on reconnect, exponential backoff on failure
- UI indicator: "Sist synkronisert: [time]" + "X elementer venter på synkronisering"

## Internationalisation
- All user-facing strings use IStringLocalizer — never hardcode UI text
- Default: Norwegian Bokmål (nb-NO)
- Supported day one: nb-NO, en, pl, lt, ro, es
- Admin enables languages per tenant (only enabled languages shown to users)
- UI strings: professionally translated (not AI)
- Dynamic content: DeepL API translation with domain glossary
- SDS AI summaries: generated natively per language via Anthropic API
- GHS H/P-statements: official EU translations
- All machine-translated content shows "Automatisk oversatt" indicator

## DateTime Conventions
- Store moments as `DateTimeOffset` (C#) / `timestamptz` (PostgreSQL), always UTC
- Store business dates as `DateOnly` (C#) / `date` (PostgreSQL)
- Never use `DateTime` — it is ambiguous
- Never use `DateTime.Now` — always `DateTimeOffset.UtcNow`
- API sends/receives UTC. Client converts via `ITimeZoneService`
- Timezone fallback: session → User.TimeZoneId → Tenant.DefaultTimeZoneId → "Europe/Oslo"
- IANA format only ("Europe/Oslo", not "Central European Standard Time")

## Commands
- Build: `dotnet build`
- Test: `dotnet test`
- Run API: `dotnet run --project src/Api`
- Run Client: `dotnet run --project src/Client`
- Add Migration: `dotnet ef migrations add <n> -p src/Infrastructure -s src/Api`
- Update Database: `dotnet ef database update -p src/Infrastructure -s src/Api`
- Format: `dotnet format`

## Architecture Rules
- Domain layer has ZERO external dependencies
- Application layer defines interfaces, Infrastructure implements them
- All database access through EF Core DbContext (no repository pattern)
- Service classes injected via DI for all business logic
- API layer is thin — endpoint definitions only
- File storage behind `IFileStorageService` (MinIO dev, S3 production)
- Translation behind `ITranslationService` (DeepL implementation)
- Chemical data behind `IChemicalDataService` (PDF extraction now, paid API later)
- Search behind `ISearchService` (PostgreSQL tsvector now, Elasticsearch upgrade path)

## Roles & Permissions
- Roles are per-tenant — a user can have different roles in different tenants
- System roles: super-admin (Solodoc staff), tenant-admin, project-leader, field-worker, subcontractor
- Subcontractor role is project-scoped (not tenant-scoped)
- Permissions follow pattern: [module].[action]
  - `projects.create`, `projects.edit`, `projects.delete`, `projects.view`
  - `jobs.create`, `jobs.edit`, `jobs.close`, `jobs.view`
  - `hours.register`, `hours.approve`, `hours.export`
  - `deviations.report`, `deviations.close`, `deviations.assign`
  - `checklists.create-template`, `checklists.complete`, `checklists.approve`
  - `chemicals.edit`, `chemicals.view`
  - `machines.register`, `machines.edit`, `machines.view`
  - `hms.sja-create`, `hms.incident-report`, `hms.safety-round`
  - `employees.manage`, `employees.view`
  - `employees.self-edit` — implicit for all users
  - `templates.create`, `templates.edit` — configurable per role by admin
- ASP.NET Core policy-based authorization — never check role strings directly
- Admin configures project-leader permissions via settings menu
- Super-admin bypasses all filters and permission checks

## Digital Signatures
- Workers draw signature once, stored as PNG, reused via tap-to-confirm
- Multi-participant documents (SJA): auto-applied, disputeable anytime, no timeout
- Guest signatures: on-screen drawing + typed name, no authentication required
- SHA-256 hash per signed document for tamper detection
- See `docs/modules/digital-signature.md` for full spec

## Blazor WASM / PWA Rules
- Component library: MudBlazor
- All API calls through typed HttpClient services
- **No stock emojis** — Lucide icons only
- Light mode + dark mode (system default, user-overridable)
- Mobile: bottom nav bar, 360px minimum width, 48px touch targets
- Desktop: left sidebar navigation
- Context switcher (top bar): [Logo] Company → Project/Job with accent color
- Interactive walkthroughs via Shepherd.js

### Reusable Components (src/Client/Components/Shared/)
- PhotoCapture, SignaturePad, GpsIndicator, SyncStatusBar
- ConfirmDeleteDialog, StatusBadge, OfflineBanner
- ContextSwitcher, AnnouncementWall, HelpButton
- ChatbotWidget, FeedbackWidget, BarcodeScanner

## File & Photo Storage (MinIO / S3)
- `IFileStorageService` wrapping AWSSDK.S3
- Dev: MinIO Docker Compose localhost:9000. Production: AWS S3 or self-hosted MinIO
- Bucket: `solodoc-{env}`, key: `{tenantId}/{entityType}/{entityId}/`
- Client-side compression: max 1920px, 80% JPEG. Server thumbnails: 200px, 400px
- Presigned URLs for download. Max 25MB. HEIC → JPEG server-side
- Offline: photos in IndexedDB, uploaded on sync

## Audit Trail
- Full snapshots for documents, event logs for admin changes
- Reopened documents: original preserved, edit marked with who/when
- Soft deletes visible in audit trail, restorable
- Active data: 12 months hot. Older: cold storage with "Henter arkiverte data..." UX
- See `docs/modules/audit-trail-and-export.md`

## GDPR & Data Retention
- DPA accepted at tenant registration. GPS: two-layer consent
- Deletion = anonymization for historical documents ("Anonymisert bruker")
- 5-year retention after employee leaves, then automatic anonymization
- 90-day grace after subscription cancellation
- All sub-processors EU/EEA based
- See `docs/modules/gdpr-data-retention.md`

## Notification System
- In-app bell with unread count + optional email per tenant
- Announcement wall: rich text, urgency levels, acknowledgment tracking
- Push notifications: designed for future, not yet implemented

## Background Jobs (Quartz.NET)
- Certification expiry check (daily 06:00)
- SDS revision check (weekly Monday 07:00)
- Translation queue (continuous)
- Sync queue cleanup (hourly)
- Overdue deviation reminders (daily 08:00)
- Scheduled activity check (daily 09:00) — missed safety rounds/HMS meetings
- Data anonymization (monthly) — 5-year enforcement
- Audit archival (monthly) — move old data to cold storage
- System health check (every 5 minutes)
- Hours summary digest (weekly, configurable)

## Logging
- Serilog + SEQ. Always ILogger<T>, never Serilog directly
- Structured templates. LogEnrichmentMiddleware: TenantId, UserId, RequestId, GPS
- Never log: passwords, tokens, personal data, fødselsnummer
- Client errors captured via Blazor error boundary, batched to API

## Code Conventions

### Naming
- Commands: `Create[Entity]Command`, `Update[Entity]Command`
- Queries: `Get[Entity]Query`, `List[Entities]Query`
- DTOs: `[Entity]Dto`, `Create[Entity]Request`

### Patterns We Use
- Primary constructors for DI
- Records for DTOs and commands
- Result<T> for error handling
- File-scoped namespaces
- Always pass CancellationToken
- Service abstractions for all external dependencies

### Patterns We DON'T Use (Never Suggest)
- Repository pattern
- AutoMapper
- Exceptions for business logic
- Stored procedures

## Testing
- Unit: domain logic and handlers
- Integration: WebApplicationFactory
- E2E: Playwright, Page Object Model, mobile viewport primary
- FluentAssertions. Naming: `[Method]_[Scenario]_[ExpectedResult]`

## Seed Data
- admin@solodoc.dev / Admin1234! (super-admin)
- bruker@solodoc.dev / Bruker1234! (user)
- Tenants: Fjellbygg AS, Vestland Maskin AS, Midtre Hardanger Gard
- Full sample data: projects, jobs, checklists, deviations, chemicals, machines,
  employees with certs (some expiring), customers, Task Groups, announcements
- Development environment only

## Local Development
- Docker Compose: PostgreSQL, MinIO (localhost:9000), SEQ (localhost:8081)
- `appsettings.Development.json` for connection strings and credentials
- PWA service worker disabled in development

## Git Workflow
- Branches: `feature/`, `bugfix/`, `hotfix/`, `test/`
- Commits: `type: description` (feat, fix, refactor, test, docs)
- Never commit to main. PR for every change. Squash merge.

## Domain Terms

### Core
- **Tenant** — company using Solodoc
- **Project (Prosjekt)** — large structured work with full documentation
- **Job (Oppdrag)** — lightweight task, under 2 min setup, companies or private customers
- **Customer (Kunde)** — company (org number) or private individual (address)
- **Task Group (Oppgavegruppe)** — reusable bundle of checklists, equipment, procedures, chemicals

### Quality & Safety
- **Deviation (Avvik)** — Åpen (red) → Under behandling (amber) → Lukket (green)
- **Checklist (Sjekkliste)** — template + instance, items: OK/Irrelevant + data fields
- **Schema (Skjema)** — checklist with data entry (same builder)
- **Procedure (Prosedyre)** — block-based written document
- **SJA** — risk assessment, auto-signed by participants with dispute option
- **Safety round (Vernerunde)** — scheduled recurring inspection
- **HMS meeting (HMS-møte)** — scheduled recurring meeting

### Employees
- **Profile/CV** — owned by person, viewable by current tenants
- **Certification (Sertifikat)** — OCR expiry extraction, 90/30/0 day alerts
- **Crew (Mannskap)** — auto-built from check-ins and hours, not pre-assigned

### Worksite
- **Check-in (Innsjekking)** — QR scan at entrance, separate from clock-in
- **Personnel registry** — who is on site right now (legal requirement)
