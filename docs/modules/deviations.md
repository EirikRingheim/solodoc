# Module Spec: Deviations (Avvik)

## Overview
Deviations are the core of the quality system. Any non-conformance, safety issue,
or quality problem gets reported as a deviation, investigated, and resolved with
corrective actions. Three statuses: Åpen → Under behandling → Lukket.

Deviations are NOT always tied to a project or job — they can be about the office,
warehouse, vehicles, or general safety culture.

---

## Deviation List Page (/deviations)

### Layout
- MudDataGrid with server-side pagination, sorting, search
- Columns: Title, Project (or "Generelt"), Status (color badge), Severity, Assigned To, Reported Date
- Status colors: Åpen (red), Under behandling (amber), Lukket (green)
- Severity badges: Lav (green dot), Middels (amber dot), Høy (red dot)
- Count badge on sidebar: number of Åpen deviations
- Click row → /deviations/{id}

### Filters
- Status: Åpen, Under behandling, Lukket, Alle
- Severity: Lav, Middels, Høy
- Project (including "Ikke prosjektrelatert")
- Assigned to
- Category
- Date range
- Reported by

### Actions
- "Nytt avvik" button → create deviation
- Batch actions: export selected, assign selected, close selected

---

## Severity Levels

Three levels, kept simple for field workers:

| Level | Color | Meaning |
|-------|-------|---------|
| **Lav** | Green dot | Minor issue, no immediate risk, fix when convenient |
| **Middels** | Amber dot | Moderate issue, should be addressed within a reasonable timeframe |
| **Høy** | Red dot | Serious issue, immediate attention needed |

---

## Categories

### Default Categories (Shipped with Solodoc)
- Sikkerhet (Safety)
- Kvalitet (Quality)
- Miljø (Environment)
- Utstyr (Equipment)
- Annet (Other)

### Tenant-Customizable
- Admin can add, rename, and deactivate categories in tenant settings
- Examples tenants might add: Orden og renhold, Prosedyrebrudd, Personvern,
  Brannvern, Elektrisk, Grunnforhold — whatever fits their operation
- Deactivated categories stop appearing in new deviations but remain
  on historical deviations (for reporting consistency)

---

## Deviation Detail Page (/deviations/{id})

### Layout (Single Page, Not Tabs)

**Header:**
- Title
- Status badge (large, prominent)
- Severity badge
- Category badge
- Project link (or "Generelt" if not project-related)
- Reported by, reported date
- Related deviations (if linked)

**Description Section:**
- What happened (rich text)
- Voice-to-text input available (see below)
- Where (location description + GPS map pin if coordinates available)
- When (date/time of incident)

**Photos Section:**
- Grid of attached photos with thumbnails
- Click to enlarge
- Annotation indicators on photos that have been marked up
- Add more photos button
- Before/after comparison view (when corrective action photos exist)

**Corrective Action Section:**
- Description of what was/will be done to fix the issue
- Responsible person (assigned to)
- Deadline for corrective action
- Corrective action photos (the "after" — showing the fix)
- Completion date (when actually done)

**Visibility Control:**
- Default: visible to all users with access to this project/tenant
- "Synlig for..." option: restrict visibility to specific named people
- Anyone not on the list doesn't see the deviation at all (not in lists, search, or reports)
- Used for: HR issues, sensitive incidents, personnel matters
- Creator, admin, and named people can see and manage the deviation

**History Section:**
- Full timeline: created → assigned → status changes → comments → closed
- Each event: who, when, what changed
- Link to audit trail for detailed change history

---

## Create Deviation Flow (Mobile-Optimized Wizard)

### Steps
1. **Describe** — Title (short), Description (what happened, where, when)
   - Voice-to-text button available (see below)
2. **Classify** — Severity (Lav/Middels/Høy), Category (from tenant's list)
3. **Attach photos** — camera capture, multiple photos, annotation available
4. **Link** — optionally link to a project or job (NOT mandatory)
5. **Submit** — GPS captured, timestamp recorded

Should be completable in under 3 minutes on a phone.

### Deviation Templates (For Recurring Types)
- Admin can create deviation templates for commonly reported issues
- Template pre-fills: category, severity, description text
- Worker selects template → adds photos, location → submit
- Examples: "Manglende fallsikring", "Feil lagring av kjemikalier", "Defekt verktøy"
- Templates shown as quick-select cards at the top of the creation wizard
- Saves time for issues that get reported repeatedly

---

## Smart Features

### Voice-to-Text (Stemme til tekst)
- Microphone icon on the description field
- Tap → speak → browser speech recognition converts to text
- Uses the Web Speech API (built into modern browsers)
- Language: follows user's selected language
- Norwegian speech recognition is supported in Chrome and Safari
- Worker reviews the text and can edit before submitting
- Especially useful for workers with gloves, cold hands, or limited typing ability
- Also available on corrective action description

### Photo Annotation
- After taking or selecting a photo, tap "Marker" to open annotation mode
- Tools available:
  - Circle (highlight an area)
  - Arrow (point to something specific)
  - Freehand draw (trace a crack, mark a boundary)
- Color: red (high contrast against most construction photos)
- Line thickness: one size (keep it simple)
- "Angre" (undo) button
- "Ferdig" saves the annotated version
- Original photo is preserved alongside the annotated version
- Annotations are rendered on the photo in the PDF export

### Before/After Comparison
- When a deviation is being closed with corrective action:
  - Worker/admin uploads "after" photos showing the fix
  - These are stored separately from the original "before" photos
- On the deviation detail page: "Vis før/etter" button
  - Side-by-side view: original photos on left, corrective action photos on right
  - Swipe comparison on mobile (drag the divider left/right)
- Included in PDF export when deviation is exported

### Duplicate Detection
- When creating a new deviation, after entering the description:
  - System checks for open deviations on the same project with similar keywords
  - If potential match found: "Det finnes et lignende åpent avvik:"
    - Shows the matching deviation title and status
    - "Er dette det samme?" → Yes (opens existing deviation) / No (continue creating)
- Uses PostgreSQL full-text search with trigram similarity
- Only checks Åpen and Under behandling deviations (not closed ones)
- Prevents duplicate reports for the same issue

### Auto-Escalation (Configurable)
- If a **Høy** severity deviation is not moved to "Under behandling" within
  a configurable time window (default: 24 hours):
  - Automatic escalation notification to tenant-admin
  - Deviation flagged in the admin dashboard: "Ubehandlet høy-prioritet avvik"
- Configurable per tenant in settings:
  - Enable/disable auto-escalation
  - Time window before escalation (12h, 24h, 48h, 72h)
  - Who receives escalation (admin only, or admin + specific people)
- Middels and Lav deviations do not auto-escalate

### Location Trend Detection
- The system tracks deviation locations (from GPS and description text)
- If the same area generates multiple deviations, the reporting module
  surfaces this: "Område: [location] — X avvik siste 30 dager"
- Helps identify systemic problems vs one-off incidents
- Available in the deviation report and on the project dashboard

### Related Deviations
- "Koble til relatert avvik" button on any deviation
- Search for and link other deviations with the same root cause
- Linked deviations show on each other's detail page
- When one is resolved, admin gets a prompt: "Relaterte avvik finnes —
  løser dette tiltaket også de relaterte avvikene?"
- Useful for systemic issues where multiple symptoms have one cause
- Related deviations are grouped in reporting

---

## Deviation Lifecycle

### Åpen → Under behandling
- Admin or project-leader assigns the deviation to someone
- Sets a deadline for corrective action
- Assignee gets notification
- Status changes to "Under behandling"

### Under behandling → Lukket
- Assignee describes the corrective action taken
- Uploads "after" photos (optional but encouraged)
- Sets completion date
- Admin/project-leader reviews and closes
- Status changes to "Lukket"

### Reopening a Closed Deviation
- Admin can reopen if the fix didn't work
- Original close record preserved (audit trail)
- Status goes back to "Under behandling"
- New corrective action round begins
- Reopening logged: "Gjenåpnet av [Name] [Date] — [Reason]"

---

## Reminders & Notifications

### Notification Flow
- **New deviation reported:** project-leader and admin notified
- **Deviation assigned:** assignee notified with deadline
- **Deadline approaching (3 days before):** reminder to assignee
- **Deadline passed:** reminder to assignee + notification to project-leader and admin
- **Auto-escalation (Høy only):** admin notified if not addressed within configured window
- **Deviation closed:** reporter notified that their report was resolved
- **Deviation reopened:** assignee notified

### No Escalation Chain
Reminders only — the system reminds, it doesn't escalate through management layers
(except the auto-escalation for Høy severity which is a single-step notification to admin).

---

## Visibility Control (Confidential Deviations)

### "Synlig for..." Feature
- When creating or editing a deviation, the creator or admin can restrict visibility
- "Synlig for alle" (default) — all users with project/tenant access can see it
- "Synlig for..." → select specific people by name
  - Only selected people + tenant-admin can see the deviation
  - It doesn't appear in lists, search results, or reports for anyone else
  - No indication that a hidden deviation exists (truly invisible)
- Use cases: HR issues, sensitive personnel matters, incidents under investigation,
  legal situations

### Who Can Set Visibility
- The deviation creator (at creation time)
- Tenant-admin (can change at any time)
- Project-leader (for deviations on their projects)

---

## PDF Export

### Individual Deviation Report
- Company logo, deviation number, date
- Status, severity, category
- Description with location
- All photos (original + annotated versions)
- Before/after comparison (if corrective action photos exist)
- Corrective action description
- Timeline of events
- Signatures (if required)
- GPS coordinates and map (if available)

### Deviation Summary Report (From Reporting Module)
- Filtered list of deviations with summary statistics
- Count by status, severity, category
- Trend charts
- Average close time

---

## Offline Support
- Creating a deviation works fully offline
- Photos captured and stored in IndexedDB
- Voice-to-text requires connectivity (browser speech API)
- Photo annotation works offline (client-side only)
- GPS captured at time of report
- Duplicate detection runs after sync (not offline)
- Synced when connectivity returns
