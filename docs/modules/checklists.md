# Module Spec: Checklists (Sjekklister)

## Overview
This spec covers the user-facing checklist experience: how workers find, fill out,
submit, and manage checklists. For template creation and the builder, see
`template-builder.md`. This spec focuses on the day-to-day use of checklists.

---

## Checklist List Page (/checklists)

### Layout
- MudDataGrid with server-side pagination, sorting, search
- Columns: Template Name, Project, Status, Assigned To, Due Date, Completed Date
- Status badges:
  - Utkast (gray) — started but not submitted
  - Innsendt (blue) — submitted, awaiting approval
  - Godkjent (green) — approved
  - Gjenåpnet (amber) — was submitted/approved but reopened for corrections
- Filter by: status, project, template, assigned to, date range
- "Ny sjekkliste" button → select template → select project → start filling out

### Quick Access
- From the dashboard: "Sjekklister" in quick actions opens this page
- From within a project: project's Sjekklister tab shows only that project's checklists
- From Forefallende: overdue checklists link directly to the fill-out page

---

## Assigning Checklists

### How Checklists Get Assigned
- **From a project:** project-leader adds a template to the project and assigns
  it to a worker with an optional due date
- **From a Task Group:** when a Task Group is applied to a project, its checklist
  templates are automatically assigned to the project
- **Self-initiated:** a worker can start a checklist themselves by selecting
  a template and linking it to their current project or job

### Assignment Data
- Template (which checklist template to use)
- Project or Job (where the work is)
- Assigned to (which worker — optional, can be unassigned)
- Due date (optional)
- Location / identifier (e.g., "Etasje 3", "Akse A-C" — free text)

---

## Filling Out a Checklist (Mobile-Optimized)

### Entry Point
Worker taps a checklist from:
- Dashboard quick actions → "Sjekkliste" → select from assigned list
- Checklist list page → click an assigned/draft checklist
- Project page → Sjekklister tab → click a checklist
- Forefallende → click an overdue checklist card

### Mobile Flow (Step-by-Step)
On mobile, the checklist is presented **one item per screen** for focus and
ease of use with gloves/dirty hands.

**Screen layout per item:**
```
┌─────────────────────────────┐
│ Sjekkliste: Betongstøp      │
│ Punkt 3 av 12               │
│                             │
│ ┌─────────────────────────┐ │
│ │ Er forskalingen         │ │
│ │ tilstrekkelig sikret?   │ │
│ │                         │ │
│ │ (Help text if set by    │ │
│ │  template creator)      │ │
│ └─────────────────────────┘ │
│                             │
│   ┌─────┐    ┌───────────┐  │
│   │ OK  │    │ Irrelevant│  │
│   └─────┘    └───────────┘  │
│                             │
│   📷 Legg til bilde         │
│   💬 Legg til kommentar     │
│                             │
│  ← Forrige    Neste →       │
└─────────────────────────────┘
```

### Item Type Behavior

**Check Item (OK / Irrelevant)**
- Two large tap-friendly buttons: **OK** (green) and **Irrelevant** (gray)
- Tapping OK: item is marked as checked, moves to next
- Tapping Irrelevant: 
  - Comment field appears: "Hvorfor er dette irrelevant?"
  - Comment may be required or optional (set per item in the template)
  - Item is marked as irrelevant with the comment
- Both buttons show a checkmark after selection (can be changed by tapping again)
- Optional photo per item: "Legg til bilde" opens camera
- Optional comment per item: "Legg til kommentar" opens text field

**Text Input Item**
- Shows a text field where the worker types a value
- Placeholder text from template help text
- Example: "Batch-nummer", "Leverandør", "Beskrivelse"

**Number Input Item**
- Shows a numeric field with optional unit label
- Numeric keyboard on mobile
- Example: "Temperatur (°C)", "Slump (mm)", "Trykk (bar)"
- Optional min/max validation (set in template)

**Date Input Item**
- Date picker
- Example: "Leveringsdato", "Neste inspeksjonsdato"

**Dropdown Item**
- Select from predefined options (set in template)
- Example: "Betongtype: B30 / B35 / B45 / Annet"

**Photo Item**
- Camera opens directly — take a photo
- Required or optional (set in template)
- Preview shown after capture
- Can retake

**Signature Item**
- Signature pad opens
- Worker draws signature or tap-to-sign (uses stored signature)
- Can be placed anywhere in the checklist (not just at the bottom)

### Desktop Flow
On desktop, all items are shown on one scrollable page (not step-by-step).
Same item types and behavior, just presented as a continuous form.
Section headers group related items.

### Progress Indicator
- Top of screen: "Punkt 3 av 12" with progress bar
- Items already completed show a green checkmark in the progress bar
- Items marked Irrelevant show a gray dash
- Skipped items show as empty

### Saving Progress
- Auto-save as draft after each item is completed
- Worker can leave and come back — progress is preserved
- Draft checklists appear in their list with "Utkast" status
- Works offline — saved to IndexedDB, synced later

---

## Submitting a Checklist

### Submission Flow
1. Worker reaches the last item and completes it
2. Summary screen shows: all items with their responses
   - OK items: green check
   - Irrelevant items: gray dash with comment
   - Input items: value entered
   - Missing/skipped items: red highlight "Ikke utfylt"
3. Required items that are missing block submission:
   "X obligatoriske punkt er ikke utfylt"
4. Signature section at the bottom (if template requires signature)
   - Worker taps "Signer" → stored signature applied
   - Multiple signatures if template requires (e.g., worker + inspector)
5. "Send inn" button submits the checklist
6. Status changes to "Innsendt"
7. GPS coordinates captured at submission
8. Timestamp recorded
9. Notification to project-leader (if configured)

### What Gets Recorded on Submission
- All item responses (check status, input values, selected options)
- All photos (stored in MinIO)
- All comments
- Signature images
- GPS coordinates
- Timestamp
- Template version (frozen — the instance is tied to this version forever)
- SHA-256 hash of all data (tamper detection)

---

## Viewing a Completed Checklist (/checklists/{id})

### Layout
Single scrollable page showing all items with their responses:
- Template name, document number, revision
- Project name, location
- Completed by, completion date and time
- GPS location (map link if available)

**Per item:**
- Item label
- Response: OK (green check) / Irrelevant (gray, with comment) / value entered
- Photos (thumbnails, click to enlarge)
- Comments
- Timestamp per item (when it was filled out)

**Signature section:**
- All signatures with names and timestamps

**Footer:**
- SHA-256 hash for integrity verification
- "Generert fra Solodoc"

### Actions
- "Eksporter PDF" → generates PDF with all items, photos, signatures
- "Dupliser for ny lokasjon" → creates new blank instance (see below)
- "Gjenåpne" (admin/project-leader only) → reopens for corrections

---

## Duplication for Repeated Work

### Use Case
A foreman completes "Betongstøp sjekkliste" for Etasje 1. Now they need to do
the same inspection for Etasje 2, Etasje 3, etc. Instead of navigating back to
templates and starting from scratch, they duplicate directly.

### Flow
1. On a completed checklist, tap "Dupliser for ny lokasjon"
2. Change the location/identifier: "Etasje 1" → "Etasje 2"
3. A new blank instance is created from the same template
4. Pre-filled: project, template, date (today), assigned to (same worker)
5. Cleared: all check marks, input values, photos, signatures
6. Worker starts filling out the new instance immediately

### Where the Button Appears
- On the completed checklist detail page
- On the submission confirmation screen ("Sjekklisten er sendt inn. Dupliser for ny lokasjon?")

---

## Reopening a Submitted Checklist

### Who Can Reopen
- Admin
- Project-leader (for checklists on their projects)

### Flow
1. Admin/project-leader opens a submitted or approved checklist
2. Taps "Gjenåpne"
3. Enters reason: "Hvorfor gjenåpnes denne?" (required)
4. Original version is preserved as a read-only snapshot
5. The checklist becomes editable again
6. Status changes to "Gjenåpnet"
7. Banner on the checklist: "Gjenåpnet av [Name] den [Date] — [Reason]"
8. When re-submitted, both versions are accessible:
   - "Vis original" shows the first submitted version
   - "Vis gjeldende" shows the current/edited version
9. Changes are tracked in the audit trail

---

## Approval Flow (Optional)

### When Enabled
- Tenant setting: "Sjekklister krever godkjenning" (default: off)
- When on: submitted checklists go to "Innsendt" status and need approval

### Approval
1. Checklist appears in project-leader's approval queue
2. Project-leader reviews all items and responses
3. "Godkjenn" → status changes to "Godkjent"
4. "Avvis" → reason required → worker notified → status back to "Utkast"
   with the rejection reason visible

### When Disabled
- Submitted checklists go directly to "Godkjent" status (no approval step)
- Simpler for small companies

---

## Section Headers and Grouping

### How Sections Work
Templates can group items under section headers (defined in the template builder).
In the fill-out flow:

**Mobile:** sections appear as separators between items. The current section name
is shown at the top of the screen alongside the progress.

**Desktop:** sections are collapsible groups. Click a section header to
expand/collapse all items within it.

### Category-Level Toggle (Safety Rounds)
For safety round checklists specifically (see hms-safety.md), entire categories
can be hidden:
- Admin marks a category as "Ikke relevant for dette prosjektet"
- All items in that category are skipped
- They don't appear in the fill-out flow or in the PDF

---

## Checklist Instance in Context

### On a Project Page
The project's "Sjekklister" tab shows:
- Assigned templates: which templates are available for this project
- Instances: all filled-out checklists, grouped by template
- Per template: "3 av 5 fullført" progress
- "Ny sjekkliste" button to start a new instance

### On a Job Page
Jobs have a simpler view:
- If a Task Group was applied, its checklists appear
- Worker can also select any template to fill out for this job
- Completed checklists listed on the job page

### In Forefallende
- Assigned checklists with due dates appear in Forefallende columns
- Overdue checklists appear in "Over frist"
- Postponeable (new due date, reason logged)

---

## Offline Support

### What Works Offline
- Viewing assigned checklists (cached from last sync)
- Filling out all item types (stored in IndexedDB)
- Taking photos (stored as base64 in IndexedDB)
- Saving drafts
- Submitting (queued for sync)

### What Requires Connectivity
- Starting a checklist from a template not yet cached
- Uploading photos to MinIO (happens on sync)
- PDF export
- Approval actions

### Sync Behavior
- Draft auto-saves are synced when connectivity returns
- Submitted checklists are synced in order
- Photos are uploaded to MinIO during sync
- If sync fails, data remains in queue with retry
- "Venter på synkronisering" indicator on the checklist

---

## PDF Export

### Per Instance
Each completed checklist can be exported as a PDF:

**Header:**
- Company logo
- Template name
- Document number and revision
- Project name, location

**Body:**
- All items with their responses, organized by section
- OK items: ✓ checkmark
- Irrelevant items: — dash with comment
- Input values displayed
- Photos embedded (compressed for reasonable file size)
- Comments shown below each item

**Footer:**
- All signatures with names and timestamps
- GPS coordinates
- Completion date and time
- SHA-256 hash
- "Generert fra Solodoc [date]"

### Bulk Export
- From project page: export all checklists for the project
- From checklist list: export selected checklists
- Uses the documentation export system (combined PDF or structured ZIP)

---

## Notifications

- **Checklist assigned:** worker notified with template name, project, due date
- **Due date approaching (3 days):** reminder to assigned worker
- **Due date passed:** appears in Forefallende as "Over frist"
- **Checklist submitted:** project-leader notified (if approval enabled)
- **Checklist approved:** worker notified
- **Checklist rejected:** worker notified with reason
- **Checklist reopened:** worker notified with reason
