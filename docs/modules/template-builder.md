# Module Spec: Template Builder & Document System

## Overview
The template system is the core of Solodoc's quality management. It covers three
document types that share a unified builder but serve different purposes:

- **Checklists (Sjekklister)** — step-by-step verification with OK/Irrelevant per item
- **Schemas (Skjemaer)** — checklists with data entry fields (measurements, test results)
- **Procedures (Prosedyrer)** — written documents describing how to perform a task

Checklists and schemas are built with the same tool (a schema is a checklist with
data-entry item types). Procedures use a separate block-based document builder.
All three share the same template management, versioning, and permission system.

---

## Unified Checklist & Schema Builder

### Philosophy
One builder, two outputs. A simple safety checklist uses only "check" items.
A concrete quality schema uses a mix of check items, number inputs, and text fields.
The admin never has to think about whether they're building a "checklist" or a "schema"
— they just add the item types they need.

### Item Types Available in the Builder

| Type | What the Worker Sees | Use Case |
|------|---------------------|----------|
| **Check** | OK button / Irrelevant button | Standard verification ("Stillas kontrollert?") |
| **Text input** | Free text field | Batch numbers, supplier names, descriptions |
| **Number input** | Numeric field with optional unit label | Temperature (°C), slump test (mm), PSI |
| **Date input** | Date picker | Expiry dates, delivery dates |
| **Dropdown** | Select from predefined options | Concrete type, weather conditions, material grade |
| **Photo** | Camera capture button | Document condition, evidence, before/after |
| **Signature** | Finger-on-glass signature pad | Sign-off by worker, inspector, project leader |

### Item Properties (All Types)
- **Label** — the question or instruction text
- **Required** — whether the item must be completed before submission (default: yes)
- **Help text** — optional small gray text below the label explaining what to do
- **Section grouping** — items can be grouped under section headers

### Check Item Specific Behavior
- Two buttons: **OK** and **Irrelevant**
- If "Irrelevant" is selected, an optional comment field appears ("Hvorfor?")
- Admin can set per item whether comment is required when marking Irrelevant
- Optional photo attachment per check item
- Optional comment field per check item

### Builder UX
- Drag-and-drop reordering of items
- Section headers to group related items (collapsible in the builder)
- Duplicate an item (copies label + settings, useful for repetitive inspections)
- Preview mode — see exactly what the worker will see on mobile
- Desktop-focused builder (this is admin work, not field work)

### Template-Level Settings
- Template name
- Description (what is this checklist for)
- Category (auto-suggested, editable — see Auto-Tagging below)
- Tags (auto-generated + manually editable)
- Document number (auto-generated or custom — see Numbering below)
- Require signature at bottom (default: yes, configurable)
- Number of signatures required (1 = worker only, 2+ = worker + approver/inspector)
- Signature roles (who signs: "Utfører", "Kontrollør", "Prosjektleder", custom)

---

## Procedure Builder (Block-Based Document Editor)

### Philosophy
Purpose-built, not a general word processor. The admin assembles predefined block types
in whatever order makes sense. The system enforces consistent styling — no font
fiddling, no color picking. Every procedure looks professional and readable.

### Block Types

**Header Block** (auto-populated, always first)
- Document title
- Document number + revision
- Company logo (from tenant settings)
- Created by, approved by, date
- Not editable in the builder — auto-generated from template metadata

**Text Block**
- Rich text paragraph
- Supports: bold, italic, bullet points, numbered lists
- No font/size/color controls — system enforces consistent typography
- Used for: purpose, scope, description, background

**Step List Block**
- Numbered steps (auto-numbered)
- Each step can have sub-steps (a, b, c)
- Steps can include inline photos/illustrations
- Drag-and-drop reordering

**Responsibility Block**
- Table format: Role → Responsibility
- Roles are text-based, not tied to Solodoc user roles
  (e.g., "Bas", "Fagarbeider", "Verneombud")

**Photo / Illustration Block**
- Upload reference images, diagrams, technical drawings
- Caption text below image
- Images are part of the template (not per-instance)

**Warning / Caution Block**
- Highlighted box with icon
- Three levels: Info (blue), Advarsel (amber), Fare (red)
- Used for safety notices, critical reminders, regulatory references

**Reference Block**
- Links to: regulations, other procedures, related checklists, external URLs
- Structured as a list of references with title + link/document number

**Sign-Off Block**
- Signature fields for document approval
- Configurable: who needs to sign (author, reviewer, approver)
- Used for procedure approval workflow, not field sign-off

### Procedure-Specific Features
- **PDF upload** — admins can upload existing procedure PDFs.
  The PDF is stored as an attachment and displayed alongside the Solodoc procedure.
  Optionally, the admin can rebuild the procedure using the block builder
  (for procedures they want to digitize fully).
- **Read confirmation** — procedures can be assigned for reading.
  Workers must confirm they've read and understood the procedure.
  Tracked with timestamp per user (ties into the announcement system).

---

## Duplication Feature

### Template Duplication
- "Dupliser mal" button on any template
- Creates an exact copy with "(Kopi)" appended to the name
- New copy gets its own document number and starts at revision 1
- All items/blocks are copied
- The duplicate is independent — editing it does not affect the original

### Instance Duplication (For Repeated Work)
- When a worker completes a checklist for "Betongstøp Etasje 1",
  they can tap "Dupliser for ny lokasjon"
- Creates a new blank instance from the same template
- Worker only needs to change the location/identifier
- Pre-fills: project, template, date (today)
- Clears: all check marks, input values, photos, signatures
- Designed for concrete work where the same inspection happens per floor/section

---

## Version Control & Revisions

### How Versioning Works
- Every template has a version number: Rev. 1, Rev. 2, etc.
- When an admin edits a template and saves, a new revision is created
- Previous revisions are preserved and viewable
- **Completed instances are frozen** to the template version they were filled out with
  — they never change retroactively
- New instances always use the latest published version

### Revision History
- Admin can view full revision history of any template
- Side-by-side comparison between any two revisions (diff view)
- Each revision records: who changed it, when, what changed

### Draft vs Published
- Edits to a template are saved as a draft until explicitly published
- Workers only see the latest published version
- Admin can preview a draft before publishing
- Publishing creates a new revision number

---

## Solodoc Base Templates

### What They Are
Solodoc ships a library of professionally crafted templates covering common
use cases in construction, machine operation, and farming:
- SJA (Sikker Jobb Analyse)
- Vernerunde (Safety round)
- RUH (Rapport om uønsket hendelse)
- Betongstøp sjekkliste
- Grunnarbeider sjekkliste
- Maskinvedlikehold sjekkliste
- Brannvernrunde
- Fallsikring kontroll
- And more — library grows over time

### Using Base Templates
- Tenants browse the template library during onboarding or anytime
- "Legg til" adds a copy of the base template to their tenant library
- They can use it as-is or customize it (add/remove items, change labels)
- Once customized, it's their own independent template

### Base Template Updates
When Solodoc releases an updated version of a base template (e.g., due to
regulatory changes):

1. Tenants using an **unmodified** copy see a notification:
   "En oppdatert versjon er tilgjengelig"
   - They can preview the new version
   - Accept → their template is updated to the new base version
   - Dismiss → notification disappears until the NEXT update (not shown again for this version)

2. Tenants using a **customized** copy see the same notification, with options:
   - Preview new version side-by-side with their current version
   - Accept and lose customizations → replaced with new base
   - Accept and merge → new base version with their custom additions preserved
     (system tracks which items are "base" and which are "tenant-added")
   - Dismiss → gone until next update

### How Merge Works (Technical)
- Each item in a template is tagged as `source: base` or `source: tenant`
- When merging, all `source: base` items are replaced with the new base version
- All `source: tenant` items are preserved and appended in their original position
- If a tenant modified a base item (changed the label), it's flagged for manual review
- The admin sees a merge preview before confirming

### Building Base Templates (Internal Solodoc Tool)
The same template builder is used internally by Solodoc staff to create base templates.
Base templates are tagged with `isBaseTemplate: true` and belong to a special
system-level tenant. They are published to all tenants via the template library.

---

## Document Numbering

### Auto-Generated (Default)
- Format: `[PREFIX]-[SEQ] Rev. [N]`
- Prefix derived from document type:
  - SJL = Sjekkliste
  - SKJ = Skjema
  - PRO = Prosedyre
  - SJA = Sikker Jobb Analyse
- Sequence is per-tenant, auto-incrementing
- Example: SJL-001 Rev. 1, SJL-002 Rev. 1, PRO-001 Rev. 3

### Custom Numbering Scheme (Admin Setting)
- Admin can define their own prefix and numbering pattern in tenant settings
- Accessible via: Settings → Document Numbering
- Options:
  - Custom prefix per document type (e.g., "KS" instead of "SJL")
  - Starting number (e.g., start at 100 instead of 001)
  - Include project code in number (e.g., "NBS-SJL-001" for Nybygg Sentrum)
- Once set, applies to all new templates. Existing templates keep their numbers.

---

## Template Permissions

### Default Permissions
- **Tenant-admin:** full access — create, edit, delete, publish templates
- **Project-leader:** can create and edit templates (default, configurable)
- **Field-worker:** can only fill out instances, cannot create or edit templates
- **Subcontractor:** can only fill out assigned instances

### Configurable by Admin
Admin can adjust template permissions in: Settings → Permissions → Templates
- Toggle: "Prosjektledere kan opprette maler" (default: on)
- Toggle: "Prosjektledere kan redigere maler" (default: on)
- Per-template lock: admin can lock a specific template so only admins can edit it

---

## Template Sharing Between Tenants

### How It Works
- Admin clicks "Del mal" on any template they own
- System generates a shareable link (valid for 30 days, regenerable)
- Recipient opens the link → sees a preview of the template
- If they have a Solodoc account with admin rights → "Importer til mitt bibliotek"
- A copy is created in their tenant library (independent from that point)
- No ongoing sync — it's a one-time copy

### Restrictions
- Only tenant-created templates can be shared (not Solodoc base templates directly
  — those are already available to all tenants via the library)
- The sharing admin can revoke the link at any time
- Shared templates carry a "Delt fra [Company Name]" attribution (optional, removable)

---

## Auto-Tagging

### How It Works
- When a template is saved, the system extracts keywords from:
  - Template name
  - Section headers
  - Item labels
- Norwegian stop words are filtered out ("og", "for", "i", "av", "er", "som", "den",
  "det", "en", "et", "til", "på", "med", "fra", "om", "har", "kan", "skal", etc.)
- Remaining meaningful words become auto-tags
- Admin can: accept auto-tags, remove irrelevant ones, add their own manual tags
- Tags are displayed on the template card and used in search

### Examples
- Template: "Sjekkliste for betongstøp - fundamentering"
  → Auto-tags: `betongstøp`, `fundamentering`
- Template: "Prosedyre for arbeid i høyden med fallsikring"
  → Auto-tags: `arbeid`, `høyden`, `fallsikring`

### Tag-Based Search
- Template library is searchable by text and by tag
- Clicking a tag filters to all templates with that tag
- Search also matches item labels within templates (not just the title)

---

## Instance Lifecycle (Filled-Out Forms)

### States
| State | Description |
|-------|-------------|
| **Draft** | Started but not submitted. Worker can return and continue. |
| **Submitted** | Completed and signed. Frozen. Visible to project leader / admin. |
| **Approved** | Reviewed and approved by authorized person (if approval required). |
| **Reopened** | Admin reopened a submitted/approved instance for corrections. |

### Reopening a Submitted Instance
- Admin or project leader can reopen a submitted instance
- The **original submitted version is preserved** as a read-only snapshot
- The reopened version is clearly marked: "Gjenåpnet av [Name] den [Date]"
- Changes are tracked: what was modified, by whom, when
- Both the original and the edited version are accessible (for audit trail)
- The edited version shows a banner: "Dette dokumentet ble redigert etter innsending"

### Print / PDF Export
- All instances can be exported as PDF
- PDF includes:
  - Company logo (header)
  - Document number and revision
  - Template name and description
  - All items with their responses (check marks, input values, photos)
  - Comments
  - Signatures (rendered as images)
  - Timestamps for each action
  - GPS coordinates (if captured)
  - Footer: "Generert fra Solodoc [date]"
- Photos are embedded in the PDF (compressed for reasonable file size)
- Export available from: instance detail page, project document list, bulk export

### Finding Completed Instances
- Searchable by: template name, project, date range, worker, status, tags
- Filterable in list views
- Accessible from: project page → documents tab, and from global search
