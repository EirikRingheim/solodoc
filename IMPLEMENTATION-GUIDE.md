# Solodoc — Claude Code Implementation Guide

## How to Use This Guide
This document contains step-by-step prompts for Claude Code (CLI).
Work through them in order. Each phase builds on the previous one.
After each prompt, verify the result before moving to the next.

**Rules:**
- Give Claude Code ONE prompt at a time
- Wait for it to finish before giving the next
- Verify each step works (run the app, check the database, run tests)
- If something breaks, tell Claude Code: "The last change broke X. Fix it."
- Keep the app running in a separate terminal so you can refresh and check

---

## Phase 0: Setup (Run Once)

### 0.1 — Copy spec files into project
Run this in a regular terminal (NOT Claude Code):
```bash
cd ~/documents/solodoc/solodoc
tar -xzf ~/Downloads/solodoc-final-specs.tar.gz
```

### 0.2 — Verify files are in place
```bash
ls docs/modules/
```
Should show 23 .md files.

### 0.3 — Start infrastructure
```bash
docker-compose up -d
```

### 0.4 — Open Claude Code
```bash
cd ~/documents/solodoc/solodoc
claude
```

---

## Phase 1: Foundation (Do This First)

These prompts set up the core architecture that everything else depends on.

### 1.1 — Update solution structure
```
Read the CLAUDE.md. Verify the .NET solution structure matches the spec. 
If any projects are missing, create them. Ensure all project references 
are correct. Add all NuGet packages specified in the tech stack to the 
appropriate projects. Run dotnet build to verify everything compiles.
```

### 1.2 — Database foundation
```
Read docs/modules/auth-and-onboarding.md. Create or update all domain 
entities for the identity system: Person, Tenant, TenantMembership, 
Invitation, SubcontractorAccess, RefreshToken, PasskeyCredential. 
Include all fields from the spec. Create EF Core configurations with 
proper indexes, constraints, and relationships. Generate a migration. 
Apply it to the database. Verify with dotnet ef database update.
```

### 1.3 — Multi-tenancy infrastructure
```
Implement the multi-tenancy infrastructure: TenantId global query filter 
on all tenant-scoped entities, ITenantProvider service that reads the 
tenant from JWT claims, middleware to set the current tenant context. 
Verify the global filter works by writing an integration test that 
creates data in two tenants and confirms each tenant only sees their own data.
```

### 1.4 — Authentication API
```
Read docs/modules/auth-and-onboarding.md. Implement the auth API endpoints:
POST /api/auth/register — create a new Person with email and password
POST /api/auth/login — authenticate, return JWT access + refresh token
POST /api/auth/refresh — refresh token rotation
POST /api/auth/logout — invalidate refresh token

Use ASP.NET Core Identity for password hashing. JWT with 15-min access 
token and 7-day refresh token. Include tenantId claim in the JWT when 
the user has a tenant membership.

Write integration tests for each endpoint:
- Register_ValidData_ReturnsSuccess
- Register_DuplicateEmail_ReturnsConflict
- Login_ValidCredentials_ReturnsTokens
- Login_InvalidPassword_ReturnsUnauthorized
- Refresh_ValidToken_ReturnsNewTokens
- Refresh_ExpiredToken_ReturnsUnauthorized
```

### 1.5 — Seed data
```
Create a DevelopmentDataSeeder that runs on startup in Development environment.
Seed: 
- Super-admin: admin@solodoc.dev / Admin1234!
- Regular user: bruker@solodoc.dev / Bruker1234!
- Tenant: Fjellbygg AS (org: 999888777, slug: fjellbygg)
- Tenant: Vestland Maskin AS (org: 999888666, slug: vestland-maskin)
- TenantMembership: admin is tenant-admin of both tenants
- TenantMembership: bruker is field-worker of Fjellbygg AS
Make the seeder idempotent — check before inserting.
Verify by running the API and logging in with both accounts.
```

### 1.6 — Client authentication
```
Implement authentication in the Blazor Client:
- Login page at /login with email, password, "Logg inn" button
- Registration page at /register with name, email, password, confirm password
- AuthenticationStateProvider that reads the JWT from localStorage
- HttpClient interceptor that adds the JWT to all API requests
- Redirect unauthenticated users to /login
- After login, if user has multiple tenants, show tenant selector
- After tenant selection, redirect to dashboard
- Store selected tenant in localStorage
Match the clean visual design style (white cards, blue accent, no emojis).
Use Lucide icons. Test by logging in with the seeded accounts.
```

---

## Phase 2: Core Domain Entities

### 2.1 — Project and Job entities
```
Read docs/modules/projects-and-jobs.md. Create domain entities:
Project (with all statuses: Planlagt/Aktiv/Fullført/Kansellert), 
Job (with statuses: Aktiv/VenterPåDeler/Fullført),
Customer (Bedrift/Privatperson with org number, address),
ProjectMembership (links employees to projects),
JobPartsItem (the parts/remember list with status Trengs/Bestilt/Mottatt).
Create EF Core configurations, migration, and apply.

Write unit tests:
- Project_Create_SetsStatusToPlanlagt
- Project_Complete_SetsStatusToFullført
- Job_AddPartsItem_UpdatesJobStatus
- Customer_Bedrift_RequiresName
- Customer_Privatperson_RequiresNameAndAddress
```

### 2.2 — Deviation entities
```
Read docs/modules/deviations.md. Create domain entities:
Deviation (with statuses: Åpen/UnderBehandling/Lukket),
DeviationCategory (tenant-configurable),
DeviationPhoto (with annotation support flag),
DeviationComment,
RelatedDeviation (self-referencing many-to-many).
Include type classification: MateriellSkade/Personskade/Nestenulykke/
FarligTilstand/Kvalitetsavvik/Miljøavvik.
Personskade has extra fields: InjuryDescription, BodyPart, Severity,
FirstAidGiven, HospitalVisit.
Confidential deviations: VisibleTo list of PersonIds.
Create EF Core configurations, migration, apply.

Write unit tests:
- Deviation_Create_SetsStatusToÅpen
- Deviation_Assign_ChangesStatusToUnderBehandling
- Deviation_Close_SetsStatusToLukket
- Deviation_Reopen_SetsStatusToUnderBehandling
- Deviation_Personskade_RequiresInjuryFields
```

### 2.3 — Time entry and schedule entities
```
Read docs/modules/hours-and-time.md. Create domain entities:
TimeEntry (with ProjectId OR JobId, category, GPS, status),
WorkSchedule (Ordning — daily pattern, weekly hours, break rules),
AllowanceRule (time-based/day-based/operation-based/fixed per day),
AllowanceGroup (bundles rules for a crew and period),
TimeEntryAllowance (calculated allowances per time entry),
PublicHoliday (Norwegian røde dager calendar),
EmployeeScheduleAssignment (links employee to a schedule).
Validation: exactly one of ProjectId or JobId must be set.
Create configurations, migration, apply.

Write unit tests:
- TimeEntry_MustHaveProjectOrJob_NotBoth
- TimeEntry_OvertimeCalculation_DailyThreshold
- AllowanceRule_TimeBased_CalculatesCorrectHours
- PublicHoliday_RødDag_AppliesOvertimeRate
```

### 2.4 — Employee and certification entities
```
Read docs/modules/employees.md. Create domain entities:
EmployeeCertification (type, expiry date, document attachment, OCR status),
InternalTraining (topic, trainer, trainee, signatures, date),
VacationBalance (annual allowance, carried over, used),
VacationEntry (date range, hours, status, approval),
SickLeaveEntry (type: egenmelding/sykemelding, dates),
OnboardingChecklist (reuses checklist instance pattern).
Create configurations, migration, apply.

Write unit tests:
- Certification_IsExpired_WhenExpiryDatePassed
- Certification_IsExpiringSoon_WhenWithin30Days
- VacationBalance_DeductsCarriedOverFirst
- VacationBalance_CalculatesRemaining
```

### 2.5 — Checklist and template entities
```
Read docs/modules/template-builder.md. Create domain entities:
ChecklistTemplate (with version tracking),
ChecklistTemplateVersion (frozen snapshot of the template at a point in time),
ChecklistTemplateItem (type: Check/TextInput/NumberInput/DateInput/
Dropdown/Photo/Signature, with label, required, helpText, sectionGroup),
ChecklistInstance (filled-out copy, linked to template version),
ChecklistInstanceItem (response value per item),
ProcedureTemplate (block-based),
ProcedureBlock (type: Text/StepList/Responsibility/Photo/Warning/Reference/SignOff).
Include auto-tagging: extract keywords from template name and item labels.
Create configurations, migration, apply.

Write unit tests:
- Template_NewVersion_IncrementsVersionNumber
- Template_Publish_FreezesCurrentVersion
- Instance_FrozenToTemplateVersion_NotAffectedByLaterEdits
- AutoTag_ExtractsKeywords_FiltersStopWords
```

### 2.6 — Remaining entities
```
Read the following specs and create all remaining domain entities:

docs/modules/task-groups.md — TaskGroup, TaskGroupChecklist, 
TaskGroupEquipment, TaskGroupProcedure, TaskGroupChemical, TaskGroupRole

docs/modules/chemical-register.md — Chemical, ChemicalSds (PDF reference),
ChemicalGhsPictogram, ChemicalPpeRequirement

docs/modules/machine-park.md — Equipment, EquipmentMaintenance, 
EquipmentInspection, EquipmentProjectAssignment

docs/modules/contacts.md — Contact (with type: Kunde/Underentreprenør/
Leverandør/Inspektør/Rådgiver/Annet), ContactProjectLink

docs/modules/calendar.md — CalendarEvent, EventInvitation (with 
accept/decline), ResourceAssignment (person to project per day),
ProjectWorkAssignment (task within project per day)

docs/modules/hms-safety.md — SjaForm, SjaParticipant, SjaHazard,
SafetyRoundSchedule, HmsMeeting, HmsMeetingMinutes, HmsMeetingActionItem

docs/modules/audit-trail-and-export.md — AuditEvent, AuditSnapshot

docs/modules/forefallende.md — no new entities (aggregation view)

docs/modules/tutorials-help-system.md — HelpContent, Feedback

docs/modules/reporting-analytics.md — no new entities (reads from existing)

Create all EF Core configurations, generate one combined migration, apply.
Run dotnet build to verify everything compiles.
```

---

## Phase 3: API Endpoints (Module by Module)

### 3.1 — Projects and Jobs API
```
Read docs/modules/projects-and-jobs.md. Create API endpoints:

Projects:
GET    /api/projects — list with pagination, sorting, search
GET    /api/projects/{id} — detail
POST   /api/projects — create
PUT    /api/projects/{id} — update
PATCH  /api/projects/{id}/status — change status
DELETE /api/projects/{id} — soft delete

Jobs:
GET    /api/jobs — list with pagination, sorting, search
GET    /api/jobs/{id} — detail
POST   /api/jobs — create (available to field-workers)
PUT    /api/jobs/{id} — update
PATCH  /api/jobs/{id}/status — change status
POST   /api/jobs/{id}/parts — add parts item
PUT    /api/jobs/{id}/parts/{partId} — update parts item status
POST   /api/jobs/{id}/promote — promote to project

Customers:
GET    /api/customers — list
POST   /api/customers — create
PUT    /api/customers/{id} — update

Use FluentValidation for all requests. Return Result<T>.
Apply authorization policies per CLAUDE.md roles.

Write integration tests for each endpoint.
Test tenant isolation: ensure tenant A cannot see tenant B's projects.
```

### 3.2 — Deviations API
```
Read docs/modules/deviations.md. Create API endpoints:

GET    /api/deviations — list with filters (status, severity, project, type)
GET    /api/deviations/{id} — detail with photos, comments, history
POST   /api/deviations — create (wizard data)
PUT    /api/deviations/{id} — update
PATCH  /api/deviations/{id}/assign — assign to person with deadline
PATCH  /api/deviations/{id}/close — close with corrective action
PATCH  /api/deviations/{id}/reopen — reopen
POST   /api/deviations/{id}/photos — upload photos
POST   /api/deviations/{id}/comments — add comment
POST   /api/deviations/{id}/related — link related deviation
PUT    /api/deviations/{id}/visibility — set "visible to" list

GET    /api/deviations/categories — tenant's categories
POST   /api/deviations/categories — add category
GET    /api/deviations/templates — deviation templates for quick creation
POST   /api/deviations/check-duplicate — check for similar open deviations

Include notification creation for: new deviation, assignment, deadline, escalation.
Write integration tests for the full lifecycle and tenant isolation.
```

### 3.3 — Hours and time API
```
Read docs/modules/hours-and-time.md. Create API endpoints:

Time entries:
GET    /api/hours — list for current user (filterable by week/project)
GET    /api/hours/admin — all entries (admin/project-leader, filterable)
POST   /api/hours/clock-in — start clock (project/job, category, GPS)
POST   /api/hours/clock-out — stop clock (GPS)
POST   /api/hours — manual time entry
PUT    /api/hours/{id} — edit entry
PATCH  /api/hours/{id}/submit — submit for approval
PATCH  /api/hours/{id}/approve — approve
PATCH  /api/hours/{id}/reject — reject with reason
POST   /api/hours/{id}/allowances — register operation-based allowance

Schedules:
GET    /api/schedules — list all work schedules
POST   /api/schedules — create schedule
PUT    /api/schedules/{id} — update schedule
POST   /api/schedules/{id}/assign — assign employees to schedule

Allowances:
GET    /api/allowances/rules — list all allowance rules
POST   /api/allowances/rules — create rule
PUT    /api/allowances/rules/{id} — update rule

Export:
GET    /api/hours/export/project/{id}?from=&to=&format=pdf|xlsx
GET    /api/hours/export/employee/{id}?from=&to=&format=pdf|xlsx
GET    /api/hours/export/customer/{id}?from=&to=&format=pdf|xlsx
POST   /api/hours/export/bulk — custom export configuration

Implement the overtime calculation engine and allowance calculation engine.
Write unit tests for calculation logic and integration tests for endpoints.
```

### 3.4 — Employees API
```
Read docs/modules/employees.md. Create API endpoints:

GET    /api/employees — list with certification status
GET    /api/employees/{id} — detail with all tabs data
POST   /api/employees/invite — send invitation
PUT    /api/employees/{id} — update employee info
PATCH  /api/employees/{id}/suspend — suspend
PATCH  /api/employees/{id}/activate — reactivate
DELETE /api/employees/{id} — remove from tenant (offboarding)

Certifications:
GET    /api/employees/{id}/certifications — list
POST   /api/employees/{id}/certifications — upload with OCR
PUT    /api/employees/{id}/certifications/{certId} — update
GET    /api/certifications/export?type=HMS-kort — bulk export by type

Training:
GET    /api/employees/{id}/training — list
POST   /api/employees/{id}/training — create record

Profile (self-service):
GET    /api/profile — current user's profile
PUT    /api/profile — update own profile
POST   /api/profile/certifications — upload own certification
POST   /api/profile/signature — upload/update signature
GET    /api/profile/cv/mini — generate mini CV PDF
GET    /api/profile/cv/full — generate full CV PDF

Vacation:
GET    /api/employees/{id}/vacation — balance and entries
POST   /api/employees/{id}/vacation — register vacation
PATCH  /api/employees/{id}/vacation/{entryId}/approve — approve

Implement Tesseract OCR for certification expiry extraction.
Write integration tests.
```

### 3.5 — Remaining module APIs
```
Create API endpoints for all remaining modules. Read each spec file.

Checklists (docs/modules/template-builder.md):
- Template CRUD, version management, publish, duplicate
- Instance create from template, fill out, submit
- Base template library and updates

HMS (docs/modules/hms-safety.md):
- SJA CRUD with hazard list and risk matrix
- Safety round reports (uses checklist architecture)
- HMS meeting minutes with action items
- HMS handbook sections (read, acknowledge)

Equipment (docs/modules/machine-park.md):
- Equipment CRUD with type autocomplete
- Maintenance log entries
- Inspection tracking with OCR
- Vegvesen API integration for EU-kontroll
- Project assignment

Contacts (docs/modules/contacts.md):
- Contact CRUD with title/description search weighting
- Project linking
- Customer account aggregation

Calendar (docs/modules/calendar.md):
- Event CRUD with visibility settings
- Meeting invitations with accept/decline
- Resource assignments (person to project per day)
- Project work assignments (task within project per day)
- iCal feed generation

Task Groups (docs/modules/task-groups.md):
- Task Group CRUD
- Apply to project (merge components)
- Duplicate, share between tenants

Chemical Register (docs/modules/chemical-register.md):
- Chemical CRUD
- SDS Manager database search integration
- Barcode lookup
- AI summary generation via Anthropic API
- GHS pictogram extraction

Notifications:
- GET /api/notifications — current user's notifications
- PATCH /api/notifications/{id}/read — mark as read
- PATCH /api/notifications/read-all — mark all as read

Announcements:
- CRUD with targeting, urgency, acknowledgment tracking

Search:
- GET /api/search?q= — global full-text search across all entities

Write integration tests for critical paths in each module.
```

---

## Phase 4: Blazor Client Pages

### 4.1 — Layout and navigation
```
Read docs/modules/dashboard-and-homescreen.md and 
docs/modules/projects-and-jobs.md (sidebar structure).

Build the main layout:
- Left sidebar on desktop with grouped navigation sections
  (Oversikt, Arbeidsverktøy, Dokumentasjon, Min Konto, Admin)
- Bottom navigation bar on mobile (Dashboard, Stemple, Nytt oppdrag, Avvik, Mer...)
- Context switcher at top: [Logo] Company Name → Project Name
  with accent color derived from tenant logo
- Notification bell top-right with unread count
- Sync status indicator
- Support light and dark mode
- Match the visual style from the original Solodoc screenshot:
  clean white cards, subtle borders, blue accent, lots of white space
- NO stock emojis — use Lucide icons throughout
```

### 4.2 — Dashboard
```
Read docs/modules/dashboard-and-homescreen.md.

Build role-adaptive dashboards:

Field worker dashboard (mobile-first):
- Quick action buttons: Stemple inn/ut, Nytt oppdrag, Avvik, Sjekkliste
- Announcement wall with urgency colors
- Active clock status
- Today's assignments
- Weekly hours summary ("Denne uken: 32t 30min / 37,5t")
- Recent deviations on assigned projects

Admin dashboard (desktop-first):
- Widget grid: projects count, open deviations, hours this week,
  expiring certifications, checklists completion
- Heatmaps for hours and deviations
- Announcement composer
- Each widget clickable → opens full module page
- Calendar with upcoming events

Connect all widgets to real API data.
```

### 4.3 — Projects and Jobs pages
```
Read docs/modules/projects-and-jobs.md.

Build:
- Project list page (/projects) — MudDataGrid
- Project detail page (/projects/{id}) with:
  - Prominent project name and status in header
  - Weather widget (Yr.no API)
  - Customizable mini dashboard (checked in count, deviations, checklists, hours)
  - Customizable tabs (hide/show, preference saved per user)
  - Handover page (Overlevering) for curated documentation export
- Job list page (/jobs)
- Job detail page (/jobs/{id}) with parts list
- Quick-create Job flow (under 2 minutes, mobile-optimized)
- Job → Project promotion flow

Test on mobile viewport (375x812) as primary.
```

### 4.4 — Deviations pages
```
Read docs/modules/deviations.md.

Build:
- Deviation list page (/deviations) — MudDataGrid with status color badges
- Deviation detail page (/deviations/{id}) — single scrollable page
- Create deviation wizard (mobile-optimized):
  Step 1: Describe (with voice-to-text button)
  Step 2: Classify (severity, category, type)
  Step 3: Photos (with annotation tools: circle, arrow, freehand in red)
  Step 4: Link to project (optional)
  Step 5: Submit
- Before/after comparison view
- Duplicate detection warning
- Visibility control ("Synlig for..." selector)
- Deviation templates for quick creation
```

### 4.5 — Hours pages
```
Read docs/modules/hours-and-time.md.

Build:
- Timelister page (/hours) — week view, day-by-day, with allowance tags
- Clock in/out flow from dashboard (recent projects/jobs at top)
- Manual time entry form
- Operation-based allowance registration
- Hours approval page (/admin/hours) — MudDataGrid with batch approve
- Customer account page (/accounts) — list and detail with hours/parts aggregation
- Hours export dialog with flexible options (scope, format, what to include)
```

### 4.6 — Employees pages
```
Read docs/modules/employees.md.

Build:
- Employee list page (/employees) — with cert status badges
- Employee detail page (/employees/{id}) — all tabs
- Self-service profile (/profile) with certification upload and OCR
- Signature capture pad (SignaturePad.razor component)
- Certification upload flow with OCR preview and confirmation
- Vacation balance display and registration
- Mini CV and Full CV PDF generation
- Onboarding/offboarding checklist integration
```

### 4.7 — Remaining pages
```
Build all remaining pages. Read each relevant spec.

Forefallende (/forefallende):
- Four-column kanban: Over frist, Denne uken, Denne måneden, Seinare
- Subtle color language (red/amber/neutral/gray left borders)
- Customizable item type visibility per user
- Postpone/reschedule button on cards
- Snooze for certification expiry

HMS (/hms):
- Interactive HMS handbook with expandable sections
- SJA form with risk matrix, participant cert checking, signatures
- Safety round using checklist architecture with category toggling
- HMS meeting minutes with agenda, action items, attendance

Equipment (/machines):
- Equipment list and detail with all tabs
- EU-kontroll auto-lookup via Vegvesen API
- Maintenance log and inspection tracking

Contacts (/contacts):
- Contact list with title/description as primary search
- Contact detail with project linking
- Quick-create from other modules

Calendar (/calendar):
- Week and month views
- Personal events with visibility settings
- Meeting invitations with accept/decline
- Resource planner (Ressursplanlegger) — Gantt grid
- Project work planner (sub-level task assignments)
- iCal feed generation in profile settings

Checklists (/checklists):
- Template builder (drag-and-drop items, all item types)
- Procedure builder (block-based)
- Checklist completion flow (step-by-step on mobile)
- Template version management

Chemical Register (/chemicals):
- Chemical list with GHS pictograms
- Add chemical via SDS Manager search
- Barcode scanner component
- AI summary display
- Offline cached summaries

Admin Settings (/admin/settings):
- All sections from docs/modules/admin-settings.md
- Company profile with logo upload
- All configuration options organized by section

Search:
- Global search bar on all pages
- Contextual search within projects
- Results grouped by type
```

---

## Phase 5: Background Jobs and Integration

### 5.1 — Quartz.NET background jobs
```
Read CLAUDE.md background jobs section. Implement all scheduled jobs:

- CertificationExpiryCheck (daily 06:00) — scan all certs, create notifications
- SdsRevisionCheck (weekly Monday 07:00) — check SDS Manager for updates
- OverdueDeviationReminder (daily 08:00) — remind about overdue deviations
- ScheduledActivityCheck (daily 09:00) — flag missed safety rounds/HMS meetings
- SyncQueueCleanup (hourly) — remove old synced entries
- DataAnonymization (monthly) — 5-year retention enforcement
- AuditArchival (monthly) — move old audit data to cold storage
- SystemHealthCheck (every 5 minutes) — DB, MinIO, API health
- VehicleEuKontrollRefresh (monthly) — re-check Vegvesen API for all vehicles
- VacationYearEndRollover (January 1) — carry over vacation balances
- DeviationAutoEscalation (hourly) — escalate unaddressed Høy deviations

Each job: resolve service from DI scope, call service method, log result.
Write unit tests for each job's core logic.
```

### 5.2 — File storage service
```
Implement IFileStorageService using AWSSDK.S3:
- UploadFile (stream, key, content type) → returns storage key
- DownloadFile (key) → returns stream
- GetPresignedUrl (key, expiry) → returns temporary download URL
- DeleteFile (key)
- Bucket: solodoc-{environment}, key prefix: {tenantId}/{entityType}/{entityId}/

Configure to use MinIO in development (localhost:9000).
Write integration tests that upload and download a file via MinIO.
```

### 5.3 — Translation service
```
Read docs/modules/multi-language.md. Implement ITranslationService:
- TranslateText(text, sourceLanguage, targetLanguage) → translated text
- TranslateBatch(texts[], sourceLanguage, targetLanguage) → translated texts[]
- Use DeepL API (https://api-free.deepl.com/v2/translate)
- Cache translations in the Translation table
- Only translate when content changes
- Include the domain glossary for construction terms

Write unit tests with a mock DeepL service.
Write one integration test that calls DeepL (requires API key in .env).
```

### 5.4 — Audit trail service
```
Read docs/modules/audit-trail-and-export.md. Implement:
- IAuditService with LogEvent() and CreateSnapshot() methods
- Intercept SaveChanges in DbContext to auto-log changes
- Full snapshots for document entities (checklists, deviations, SJA)
- Event logs for administrative changes
- Soft delete tracking (who deleted, when)

Write integration tests:
- ModifyDeviation_CreatesAuditEvent
- SubmitChecklist_CreatesSnapshot
- SoftDelete_LogsDeletionEvent
```

---

## Phase 6: PDF Generation and Export

### 6.1 — PDF reports
```
Read docs/modules/audit-trail-and-export.md and relevant module specs.
Implement QuestPDF report services:

- DeviationReportPdf — single deviation with photos, annotations, history
- ChecklistReportPdf — completed checklist with all responses and photos
- SjaReportPdf — SJA with risk matrix, participants, signatures
- HoursExportPdf — hours summary with overtime and allowance breakdown
- ProjectSummaryPdf — all key metrics for one project
- MiniCvPdf — one-page employee CV
- FullCvPdf — detailed employee CV
- EquipmentReportPdf — equipment with maintenance and inspection history

All PDFs include: company logo header, document number, revision, 
signatures, timestamps, GPS where applicable, SHA-256 hash in footer.

Write unit tests that generate each PDF type and verify they're valid.
```

### 6.2 — Export system
```
Read docs/modules/audit-trail-and-export.md. Implement the export wizard:

POST /api/export/project/{id} — project documentation package
POST /api/export/employee/{id} — employee documentation package
POST /api/export/custom — custom selection
GET  /api/export/{id}/status — check progress
GET  /api/export/{id}/download — download result

Three modes: Combined PDF, Structured ZIP, Individual Files.
Photo options: full, compressed, thumbnail, none.
Large exports as background jobs with notification when ready.
Temporary storage in MinIO (auto-delete after 7 days).

Write integration tests for each export mode.
```

---

## Phase 7: Testing

### 7.1 — Comprehensive test suite
```
Review all existing tests. Add any missing tests for:

Unit tests (domain logic):
- All entity creation and validation rules
- Overtime calculation with various schedules
- Allowance calculation with various rules
- Vacation balance calculations
- Certification expiry detection
- Auto-tagging keyword extraction
- Document numbering sequence generation

Integration tests (API):
- Full CRUD for every entity
- Tenant isolation on every endpoint
- Authorization (field-worker can't access admin endpoints)
- Subcontractor can only see their assigned project
- File upload and download via MinIO
- Search returning correctly grouped results

E2E tests (Playwright):
- Login flow (email + password)
- Create a project
- Report a deviation with photo
- Complete a checklist
- Clock in and out
- Create a job as field-worker
- Admin approves hours
- Export a project documentation package

Run all tests: dotnet test
All tests must pass before proceeding.
```

### 7.2 — Mobile testing
```
Run all E2E tests on mobile viewport (375x812).
Verify:
- Bottom navigation works on mobile
- All forms are usable on small screens
- Touch targets are minimum 48x48px
- Photo capture works via camera
- Clock in/out is one-tap from dashboard
- Deviation wizard completes in under 3 minutes
Fix any mobile-specific issues.
```

---

## Phase 8: Polish and Documentation

### 8.1 — Error handling
```
Review all API endpoints and ensure:
- Every endpoint returns proper HTTP status codes
- All errors use the Result<T> pattern
- Validation errors return 400 with field-level messages
- Not found returns 404
- Unauthorized returns 401
- Forbidden returns 403
- All errors are logged via Serilog with context
- Client shows user-friendly error messages in Norwegian
```

### 8.2 — Generate tutorial content
```
Read docs/modules/tutorials-help-system.md. 
For each completed page, create help content:
- HelpContent entries in the database seeder
- Page identifier, title, body (markdown), role scope
- Cover the standard tutorial set listed in the spec:
  Field worker tutorials (9), Project leader tutorials (7), Admin tutorials (10)
Keep it concise and practical — steps, not essays.
```

---

## Running the App

**Terminal Tab 1 — Infrastructure:**
```bash
cd ~/documents/solodoc/solodoc
docker-compose up -d
```

**Terminal Tab 2 — API:**
```bash
cd ~/documents/solodoc/solodoc
dotnet run --project src/Api
```

**Terminal Tab 3 — Client:**
```bash
cd ~/documents/solodoc/solodoc
dotnet run --project src/Client
```

**Terminal Tab 4 — Claude Code:**
```bash
cd ~/documents/solodoc/solodoc
claude
```

**Run tests:**
```bash
cd ~/documents/solodoc/solodoc
dotnet test
```
