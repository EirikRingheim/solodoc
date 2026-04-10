# Module Spec: Projects & Jobs (Prosjekter & Oppdrag)

## Overview
Projects and Jobs are the two work containers in Solodoc. Projects are large and
admin-managed. Jobs are lightweight and employee-created. Both support time entries,
deviations, and photo attachments. Only Projects support checklists, milestones,
tasks, competence requirements, and Task Groups.

---

## Project Context Visibility

### Critical UX Rule
When a user is inside a project, it must be **unmistakable** which project they're in.
This prevents the "filled out the checklist for the wrong project" problem.

**Implementation:**
- Large project name displayed persistently in the top bar (not just a small breadcrumb)
- Project status badge next to the name (Planlagt / Aktiv / Fullført / Kansellert)
- Subtle background tint or colored top border derived from the project (or tenant accent)
- When navigating to sub-pages (checklists, deviations, hours) from within a project,
  the project context stays visible at the top
- Clear distinction between "viewing all deviations" vs "viewing deviations for Nybygg Sentrum"
- Back button or breadcrumb to return to the project dashboard

---

## Projects

### Project List Page (/projects)
- MudDataGrid with server-side pagination, sorting, search
- Columns: Name, Client, Status, Start Date, Open Deviations, Checklist Completion %
- Status badge: Planlagt (blue), Aktiv (green), Fullført (gray), Kansellert (red)
- Click row → /projects/{id}
- "Nytt prosjekt" button (admin and project-leader only)

### Project Detail Page (/projects/{id})

**Header:**
- Project name (large, prominent), status badge
- Client name
- Key role assignments (Prosjektleder, HMS-ansvarlig, etc.)
- Action buttons: Edit, Close, Export, Overlevering
- Weather widget (small, current + 5 day forecast for project location via Yr.no/MET Norway API)

**Project Mini Dashboard (First View — Customizable)**
When opening a project, the first thing the user sees is a customizable mini dashboard.
Not a static overview — a live, at-a-glance view of the project's health.

Default widgets (user can rearrange, hide, add):
- **Checked in now:** count + expandable list ("4 personer på byggeplass" → tap to see names, companies, check-in times)
- **Open deviations:** count with severity breakdown
- **Checklist completion:** X / Y completed (progress bar)
- **Hours this week:** total hours, overtime indicator
- **Upcoming deadlines:** next 3 items from Forefallende for this project
- **Weather:** current conditions + 5 day forecast for project location

Customization:
- "Rediger dashboard" button → drag to rearrange, toggle widgets on/off
- Preference saved **per user globally** (same widget layout on all projects)
- Can un-hide specific widgets per project if needed

**Tabs (Customizable Visibility)**

| Tab | Content |
|-----|---------|
| Sjekklister | Templates assigned, instances completed/in-progress |
| Avvik | Deviations for this project, filterable by status |
| Timer | Hours per employee, regular vs overtime, approval |
| Mannskap | Auto-built crew from check-ins and hours |
| Oppgavegrupper | Task Groups applied to this project |
| Dokumenter | All project documents, export/handover access |
| Kjemikalier | Chemicals linked to this project |
| Maskiner | Equipment assigned to this project |
| Innsjekk | Full check-in/out log, exportable |

**Tab Customization:**
- User can hide tabs they don't use (right-click or settings menu on the tab bar)
- Hidden tabs stay hidden when reopening the project
- Preference saved **per user globally** — hide "Kjemikalier" once, hidden on all projects
- "Vis skjulte faner" option to bring them back or unhide for a specific project
- Tab order is rearrangeable by dragging

### Create Project Flow
1. Project name (required)
2. Client (search existing or create new)
3. Address / location (used for weather widget and GPS check-in verification)
4. Start date, planned end date
5. Estimated hours (optional — can be added later)
6. Description
7. Apply Task Groups (optional) → pre-populates checklists, equipment, etc.
8. Assign key roles (optional) → fill in role slots
9. Save → project created, navigate to project dashboard

### Estimated Hours
- Optional field on project creation
- Editable at any time by admin/project-leader
- If set, shows on the project mini dashboard: "150 av 200 timer brukt (75%)"
- Visual progress bar (green → amber → red as approaching/exceeding estimate)
- Not mandatory — many projects won't have this initially

### Project Statuses
| Status | Meaning |
|--------|---------|
| Planlagt | Created but work hasn't started |
| Aktiv | Work in progress |
| Fullført | Project completed, all documentation finalized |
| Kansellert | Project cancelled |

Subcontractor access becomes read-only when project is Fullført or Kansellert.

---

## Project Handover (Overlevering)

### Purpose
When a project is completed, the admin packages documentation for the client (byggherre).
Unlike the general export system, this is a curated handover where internal-only
documents are excluded.

### Flow
1. Admin clicks "Overlevering" on the project page
2. System presents all project documentation, grouped by type:
   - Checklists, SJA forms, deviation reports, procedures, hours, check-in logs,
     chemical register, certifications, meeting minutes, safety round reports
3. Each document has a checkbox (default: selected)
4. Admin **deselects** documents that are internal only:
   - Internal deviations (HR issues, cost disputes)
   - Internal meeting notes
   - Draft documents
   - Anything not relevant for the client
5. Preview the handover package
6. Choose format: Combined PDF or Structured ZIP
7. Generate → downloadable package with cover page:
   - Project name, client name, date range, responsible person
   - Table of contents
   - "Dokumentasjon overlevert via Solodoc [date]"
8. Optionally: send directly to client via email with a download link
   (link expires after 30 days)
9. Handover event is logged in the audit trail

### Handover Record
- The system records that a handover was made: date, who prepared it,
  what was included, who it was sent to
- Previous handovers are listed on the project page (in case of multiple handovers)

---

## Weather Widget

### Data Source
- MET Norway / Yr.no API (free, no authentication required for basic use)
- Endpoint: `https://api.met.no/weatherapi/locationforecast/2.0/compact`
- Requires project latitude/longitude (from project address)

### Display
- Small widget on the project mini dashboard
- Shows: current temperature, weather icon (sun/cloud/rain/snow), wind
- 5-day forecast as small icons with high/low temperature
- Tapping expands to a more detailed view
- Data cached, refreshed every hour

### No Recommendations
- The widget shows weather data only — no suggestions or warnings
- Workers are professionals who know what weather means for their work
- Useful context for deviation reports (outdoor conditions at the time)
- Useful for weekly planning meetings (upcoming weather for the week)

### When Weather is Unavailable
- If project has no address/coordinates: widget shows "Legg til adresse for vær"
- If API is unreachable: widget shows last cached data with "Oppdatert [time]"

---

## Jobs (Oppdrag)

### Job List Page (/jobs)
- MudDataGrid with pagination, sorting, search
- Columns: Description, Customer, Status, Created by, Date, Hours
- Status badge: Aktiv (green), Venter på deler (amber), Fullført (gray)
- Click row → /jobs/{id}
- "Nytt oppdrag" button (available to field-workers)

### Job Detail Page (/jobs/{id})
- Simpler than Project — no tabs, single page layout

**Sections:**
- **Info:** description, customer, address, status, created by, date
- **Before/after photos:** photo grid showing documentation
- **Time entries:** hours logged to this job
- **Deviations:** any deviations reported during this job
- **Parts list (Huskeliste / Deler):** items needed, with status
- **Notes:** free text notes
- Action buttons: Edit, Complete, Export as PDF

### Quick-Create Job Flow (Mobile-Optimized)
Must be completable in under 2 minutes on a phone:

1. Customer: search existing or quick-create (name + address)
   - Toggle: Bedrift / Privatperson
   - For Bedrift: company name, org number (optional)
   - For Privatperson: name, address
2. Description: what's the job? (free text, 1-2 lines)
3. Before-photos: take photos of current state (optional but encouraged)
4. Start clock: optionally start time registration immediately
5. Save → job created, worker is on-site and working

### Parts / Remember List (Huskeliste)
A simple list tied to the Job for tracking parts to order or things to remember
for a follow-up visit.

**Each item:**
| Field | Description |
|-------|-------------|
| Description | What's needed ("15mm kobberkobling", "Bestill nytt filter") |
| Status | Trengs / Bestilt / Mottatt |
| Notes | Optional details (supplier, order number, price) |
| Added by | Auto from auth |
| Added at | Timestamp |

**UX:**
- "Legg til" button → quick text input + status dropdown
- List shows on the Job detail page, always visible
- Items with status "Trengs" are highlighted
- When all items are "Mottatt", the list shows a green check
- If any items have status "Trengs", the Job shows a badge in the job list:
  "Venter på deler"
- **Job status automatically updates:**
  - If any parts have status "Trengs" → Job can be set to "Venter på deler"
  - This is visible in the job list so the worker knows they need to come back

**Why this matters:**
- Better than phone notes because it's tied to the Job, customer, and address
- If another worker takes over the Job, they see the parts list
- Searchable and documented
- Admin has visibility into which Jobs are waiting on parts

### Completing a Job
1. Worker taps "Fullfør oppdrag"
2. After-photos: document completed work
3. Notes: any additional info
4. Stop clock (if running)
5. Optional: have customer sign (guest signature)
6. Parts list: verify all items are "Mottatt" or mark remaining as not needed
7. Save → status changes to Fullført

### Job Statuses
| Status | Meaning |
|--------|---------|
| Aktiv | Work in progress or planned |
| Venter på deler | Parts/materials needed before work can continue |
| Fullført | Job completed |

### Promoting Job to Project
- If a Job grows in scope, admin can "Oppgrader til prosjekt"
- Creates a new Project with the Job's data (customer, description, hours, photos)
- Parts list items transfer as notes on the project
- The Job is archived and linked to the new Project
- One-way operation (Project cannot be demoted to Job)

---

## Customer Entity (Kunde)

### Shared Between Projects and Jobs
- Company name OR person name
- Type: Bedrift / Privatperson
- Org number (Bedrift only, optional)
- Address (street, postal code, city)
- Contact person name
- Phone, email
- Notes

### Customer List Page (/contacts/customers)
- Part of the broader Contacts module
- MudDataGrid: name, type, address, projects/jobs count
- Search by name, address, org number

---

## Navigation

### Sidebar Structure
```
OVERSIKT
  Dashboard
  Prosjekter          → /projects
  Oppdrag             → /jobs
  Forefallende (3)    → /forefallende
  Kalender            → /calendar

ARBEIDSVERKTØY
  Timelister          → /hours
  Avvik (4)           → /deviations
  Sjekklister         → /checklists
  Prosedyrer          → /procedures

DOKUMENTASJON
  Kontakter           → /contacts
  Maskinpark          → /machines
  HMS-håndbok         → /hms
  Stoffkartotek       → /chemicals

MIN KONTO
  Min profil          → /profile

ADMIN (admin/project-leader only)
  Timer-oversikt      → /admin/hours
  Ansatte             → /employees
  Maler               → /admin/templates
  Oppgavegrupper      → /admin/task-groups
  Innstillinger       → /admin/settings
```

### Mobile Bottom Navigation (Field Workers)
On mobile, the sidebar becomes a bottom navigation bar with the most important items:
- Dashboard (home icon)
- Stemple inn/ut (clock icon — most used action)
- Nytt oppdrag (+ icon)
- Avvik (warning icon)
- Mer... (hamburger → opens full sidebar as drawer)
