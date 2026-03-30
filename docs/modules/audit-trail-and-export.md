# Module Spec: Audit Trail & Documentation Export

## Overview
Every change in Solodoc is recorded. Documents (checklists, SJA, deviations, procedures)
use full snapshot history for perfect reconstruction. Administrative changes use event
logging. Nothing is truly deleted — soft-deleted items remain visible in the audit trail.

The export system allows admins to extract documentation in flexible formats:
combined reports, structured folder packages, or individual files — customizable
per use case.

---

## Audit Trail Architecture

### Hybrid Approach

**Full Snapshot History — for documents:**
- Checklists / schema instances
- SJA forms
- Deviation reports
- Procedures
- Any signed document

When any of these is modified, the complete previous state is saved as a frozen
snapshot before the change is applied. This enables perfect reconstruction:
"Show me exactly what this deviation report looked like before it was edited."

**Event Log — for administrative changes:**
- Project/job created, edited, closed
- Employee added/removed from tenant
- Template created/edited/published
- Permissions changed
- Settings changed
- Machine/equipment entries modified
- Chemical register entries modified

Event logs record: who, what changed, old value, new value, when.

### What Gets Recorded (Per Event)

| Field | Description |
|-------|-------------|
| `Id` | Unique event ID |
| `EntityType` | What was changed (Deviation, Checklist, Project, Employee, etc.) |
| `EntityId` | ID of the changed entity |
| `Action` | Created, Updated, Deleted, Reopened, Signed, Disputed, StatusChanged, etc. |
| `UserId` | Who made the change |
| `Timestamp` | When (UTC) |
| `GpsLatitude` / `GpsLongitude` | Where (if available) |
| `Changes` | JSON object with field-level changes: `{ "status": { "old": "Open", "new": "Closed" } }` |
| `SnapshotId` | For documents: reference to the full snapshot (nullable for event-only logs) |
| `IpAddress` | For security audit (stored but not displayed to users) |

### Snapshot Storage
- Snapshots are stored as serialized JSON in a dedicated `AuditSnapshot` table
- Each snapshot contains the complete entity state at that point in time
- Snapshots reference the entity type, entity ID, and version number
- Snapshots are immutable — never modified after creation

---

## Visibility & Access

### Who Can See What

| Role | Can See |
|------|---------|
| **Field worker** | Change history on documents they're involved in (their own checklists, deviations they reported, SJA they participated in). Can see who approved, who edited, when. |
| **Project leader** | All change history within their projects. Full document history, status changes, signature events. |
| **Tenant-admin** | Everything across the tenant. Including: who deleted items, permission changes, employee management actions. |
| **Subcontractor** | Only their own submissions and signature events on the specific project. |

### How Users Access Audit History
- **Per document:** "Historikk" tab or button on any document detail page.
  Shows a timeline of all events for that document, newest first.
- **Per entity:** same pattern for projects, employees, machines, etc.
- **Global audit log:** admin-only page showing all events across the tenant.
  Filterable by: entity type, user, date range, action type.

### Displaying Changes
- Timeline format: each event shows an icon, description, user name, timestamp
- For document edits: "Vis endringer" button opens a diff view showing
  what changed between two versions
- For reopened documents: clear banner on the current version
  "Gjenåpnet av [Name] [Date] — opprinnelig versjon tilgjengelig"
  with a link to view the original snapshot

---

## Soft Delete & Deletion Audit

### How Soft Delete Works
- No entity is ever physically removed from the database
- Deleted items have `IsDeleted = true` and `DeletedAt`, `DeletedBy` fields
- Deleted items are hidden from normal views but visible in:
  - Audit trail / history views
  - Admin "Deleted items" view (filterable, restorable)
- Deletion is logged as an audit event with the full entity state as a snapshot

### Admin Deleted Items View
- Accessible from: admin settings or from each module's list page (filter: "Vis slettede")
- Shows: what was deleted, who deleted it, when
- **Restore** option: admin can restore a soft-deleted item (also logged)
- Useful for accidental deletions or disputes

---

## Data Retention & Archiving

### Retention Policy
- **Audit logs and snapshots are retained for the lifetime of the tenant**
- No automatic purging of audit data
- This supports construction industry requirements where documentation
  must be available for the lifetime of the building

### Archiving Strategy (Performance Optimization)
- **Active data:** last 12 months — stored in primary database, fast access
- **Archived data:** older than 12 months — moved to cold storage
  (separate database table or object storage as compressed JSON)
- Archived data is still fully accessible but may take a few seconds to load
- When a user accesses archived data: "Henter arkiverte data..." loading indicator
- Archiving is transparent — the user doesn't need to know where data lives
- Archival runs as a scheduled background job (Quartz.NET, monthly)

### What Gets Archived
- Audit event logs older than 12 months
- Document snapshots for completed/closed projects older than 12 months
- The current state of entities is NEVER archived (always fast access)
- Only historical versions and logs move to cold storage

---

## Documentation Export System

### Philosophy
The export system must handle three very different needs:
1. **Combined report** — one PDF with everything, for handing to a client or inspector
2. **Structured folder package** — organized ZIP with folders and individual files
3. **Individual file export** — single documents exported one at a time for uploading
   to other systems

The user chooses the format and content through a customizable export wizard.

### Export Wizard Flow

**Step 1: What to export (scope)**
- Select scope:
  - Entire project
  - Specific date range
  - Specific document types
  - Specific employees
  - Custom selection (cherry-pick individual documents)
- Filter options:
  - Document type (checklists, SJA, deviations, procedures, hours, check-in logs)
  - Date range
  - Status (all, completed only, open only)
  - Employee / worker
  - Tags / categories

**Step 2: What to include (content)**
- Toggles for:
  - ☑ Checklists and schemas
  - ☑ SJA forms
  - ☑ Deviation reports (with corrective actions)
  - ☑ Procedures
  - ☑ Hours summary
  - ☑ Check-in / check-out logs
  - ☑ Chemical register (project-relevant chemicals)
  - ☑ Employee certifications (valid during project period)
  - ☑ Audit trail / change history
- Photo options:
  - With full-resolution photos
  - With compressed photos (recommended for large exports)
  - With thumbnail photos only
  - Without photos

**Step 3: Format and structure**

**Option A: Combined PDF Report**
- One PDF document containing all selected items
- Table of contents at the beginning
- Sections with headers for each document type
- Company logo on every page header
- Page numbers, project name in footer
- Photos embedded inline
- Best for: handing to byggherre, Arbeidstilsynet inspection, client delivery

**Option B: Structured ZIP Package**
- ZIP file with organized folder structure:
  ```
  Prosjekt_Nybygg_Sentrum_2026/
    01_Sjekklister/
      SJL-001_Betongstøp_Etasje1.pdf
      SJL-002_Betongstøp_Etasje2.pdf
    02_SJA/
      SJA-001_Gravearbeid.pdf
      SJA-002_Arbeid_i_høyden.pdf
    03_Avvik/
      AVV-001_Mangelfull_sikring.pdf
      AVV-002_Feil_betongblanding.pdf
    04_Prosedyrer/
      PRO-001_Betongstøp_prosedyre.pdf
    05_Timer/
      Timer_oppsummering.pdf
      Timer_detaljer.xlsx
    06_Innsjekk/
      Innsjekk_logg.pdf
    07_Kjemikalier/
      Kjemikalieregister.pdf
    08_Sertifikater/
      Ola_Nordmann_sertifikater.pdf
      Kari_Hansen_sertifikater.pdf
    09_Endringslogg/
      Audit_trail.pdf
  ```
- Each document is a standalone PDF that can be opened independently
- Folder names are numbered for logical ordering
- File names include document number and description
- Best for: archiving, uploading to other systems, organized storage

**Option C: Individual Files**
- Export selected documents as individual PDF files
- Downloaded one at a time or as a flat ZIP (no folder structure)
- Each file is self-contained with headers, logos, signatures
- Best for: uploading employee certifications to another system,
  sending a single deviation report, cherry-picking specific documents

### Export Customization Options
- **Folder naming:** admin can customize folder names in the ZIP structure
- **File naming pattern:** configurable (default: `[DocNumber]_[Title].[ext]`)
- **Cover page:** optional project summary cover page for combined PDF
  (project name, client, address, date range, responsible person)
- **Include table of contents:** toggle for combined PDF
- **Include audit trail:** toggle to append change history per document

### Employee Documentation Export
Specifically for exporting employee-related documents:
- Select employees (all, specific team, specific individuals)
- Export types:
  - All certifications for selected employees (individual PDFs per cert)
  - HMS cards
  - CV / competence overview
  - Training records / procedure acknowledgments
- **Individual files are the default** for employee exports
  (because these typically need to be uploaded individually to other systems)
- Option to bundle per employee:
  ```
  Sertifikater/
    Ola_Nordmann/
      Kranfører_sertifikat.pdf
      Førstehjelpskurs.pdf
      Varmearbeid_sertifikat.pdf
    Kari_Hansen/
      Truckfører_sertifikat.pdf
      HMS-kort.pdf
  ```

### Bulk Export Limits
- Combined PDF: warn if estimated size exceeds 100MB, suggest ZIP instead
- ZIP packages: no hard limit, but show estimated size before generating
- Large exports are generated as background jobs — user gets a notification
  when the export is ready to download
- Export files are stored temporarily in MinIO (auto-deleted after 7 days)

### Export Endpoints (API)
```
POST /api/export/project/{projectId}     — project documentation package
POST /api/export/employee/{employeeId}   — employee documentation package  
POST /api/export/custom                  — custom selection export
GET  /api/export/{exportId}/status       — check if export is ready
GET  /api/export/{exportId}/download     — download the generated file
```

All export endpoints accept a JSON body with the filter and format options
described above.

---

## Audit Trail on Exported Documents

### What Appears on Each Exported PDF
- Document header: company logo, document number, revision
- Document footer: "Generert fra Solodoc [timestamp]"
- If the document was reopened/edited after submission:
  Banner: "Dette dokumentet ble redigert etter innsending — se endringslogg"
- Signature block with all signatures, timestamps, and any disputes
- SHA-256 document hash (for integrity verification)

### Change History Appendix (Optional)
When "Include audit trail" is toggled on in the export wizard:
- Each document gets an appendix page showing the complete change history
- Timeline format: action, user, timestamp, what changed
- For reopened documents: side-by-side showing original vs current values
