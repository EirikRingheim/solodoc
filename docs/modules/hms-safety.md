# Module Spec: HMS (Health, Safety & Environment)

## Overview
The HMS module covers everything related to workplace safety: SJA forms, safety rounds,
deviations (including personal injury and near-miss), risk assessments, and the HMS
handbook. Deviations and incident reports (RUH) are unified into one system with
type classification.

Solodoc sells pre-built HMS content (handbooks, safety round templates) as purchasable
modules that tenants can unlock and customize.

---

## HMS-håndbok Page (/hms)

### What It Is
The HMS handbook is the company's safety documentation — procedures, policies,
emergency plans, organizational charts for safety responsibilities. Required by
internkontrollforskriften. Not used frequently but must exist and be accessible.

### Two Sources

**1. Solodoc-Native Handbook (Purchasable)**
- Pre-built, professional HMS handbook that looks native to Solodoc
- Interactive document with expandable/collapsible sections
- Sections can be hidden if not relevant to the tenant
- Tenant unlocks it (purchase), then can edit and customize freely
- Solodoc updates the base content over time — tenant gets update notifications
  (same model as base template updates)
- Looks integrated with Solodoc's UI, not like an embedded PDF

**2. Uploaded Handbook (Free)**
- Tenant uploads their existing HMS handbook as PDF
- Displayed within Solodoc as a viewable document
- Less interactive than the native version
- Can exist alongside the native version (some companies use both during transition)

### Native Handbook Structure
Interactive document with expandable sections. Each section can be individually
expanded, collapsed, or hidden by the tenant.

Default sections (customizable after purchase):
- **Organisering og ansvar** — who is responsible for what (safety org chart)
- **HMS-policy** — company's safety policy statement
- **Risikovurderinger** — link to risk assessment module
- **Rutiner og prosedyrer** — link to procedures module + inline procedures
- **Beredskapsplan** — emergency plan, evacuation routes, assembly points
- **Avvikshåndtering** — how deviations are reported and managed
- **Opplæring** — training requirements and records
- **Arbeidsmiljø** — work environment policies
- **Brannvern** — fire safety procedures
- **Førstehjelp** — first aid procedures and equipment locations
- **Kjemikaliehåndtering** — link to chemical register
- **Personlig verneutstyr (PVU)** — PPE requirements per activity

### Handbook Features
- Expandable/collapsible sections (click to open/close)
- Admin can hide entire sections not relevant to their business
- Rich text content within each section (from procedure builder)
- Embedded links to relevant procedures, checklists, and templates
- Search within the handbook
- Read confirmation tracking — admin can require employees to confirm
  they've read specific sections. Tracked with timestamp per user.
- Version history — admin sees when sections were last updated

### Subcontractor Access
- Subcontractors see the HMS handbook for projects they're invited to
- Admin can configure which sections subcontractors see
  (e.g., full handbook or just Beredskapsplan + relevant safety sections)
- Read confirmation can be required for subcontractors too

---

## Purchasable Content Model

### How It Works
Solodoc maintains a library of premium content modules:
- HMS-håndbok (complete handbook)
- Vernerunde templates (safety round template sets per industry)
- SJA templates
- Procedure packages (per industry/activity type)
- Onboarding/offboarding checklist packages

### Purchase Flow
1. Admin browses "Solodoc Butikk" or sees locked content in the template library
2. Locked content shows a preview + "Lås opp" button
3. Tenant purchases (payment integration — details TBD with pricing model)
4. Content appears in their library, fully editable
5. Tenant customizes to their needs
6. Future updates from Solodoc available via the standard update notification

### After Purchase
- Content is copied to the tenant's library as their own
- Fully editable — add, remove, modify anything
- Independent from the base after customization (same as base templates)
- Solodoc base updates available but never forced

---

## SJA — Sikker Jobb Analyse (/hms/sja)

### SJA List Page
- MudDataGrid: SJA number, title, project, date, participants, status
- Status: Utkast, Aktiv, Fullført
- "Ny SJA" button → create from template or blank

### SJA Numbering
- Auto-generated: format configurable by admin in tenant settings
- Default format: `SJA-[SEQ]` (e.g., SJA-001, SJA-002)
- Admin can customize: prefix, starting number, include project code
- Example custom: `NBS-SJA-001` (project code + SJA + sequence)

### SJA Form Fields

**Header Information:**
| Field | Required | Notes |
|-------|----------|-------|
| SJA number | Auto-generated | Editable format in admin settings |
| Date | Yes | Date of the analysis |
| Task description | Yes | What work is being performed |
| Company | Yes | Which company is performing the work |
| Project | No | Link to project (optional) |
| Location | No | Where the work takes place |
| Who ordered the work (Oppdragsgiver) | Yes | Client or internal |

**Participant Information:**
| Field | Required | Notes |
|-------|----------|-------|
| Participant name | Yes | Select from employees or type for externals |
| Employment status | Yes | Dropdown: Intern / Ekstern / Innleid / Sommervikar / Lærling / Annet |
| Employment percentage | No | 100%, 80%, etc. |
| Role in the work | No | What they're doing |

**Risk Assessment Questions:**
| Question | Input Type | Notes |
|----------|-----------|-------|
| Has the task been risk-mapped before? | Yes/No | If No → opens risk matrix below |
| Is there enough time to do the job safely? | Yes/No | If No → comment field |
| Is the right equipment available? | Yes/No | If No → comment field |
| Is the job properly planned? | Yes/No | If No → comment field |
| Are necessary certificates and training in place? | Checklist | Auto-checks against participant certifications |

**Certificate Verification (Automatic):**
When participants are added, the system checks their certifications:
- Green check: all required certs are valid
- Amber warning: cert expiring within 30 days
- Red alert: required cert is missing or expired
- This doesn't block the SJA but makes it visible

**Hazard Identification:**
For each identified hazard:
| Field | Input Type |
|-------|-----------|
| What can go wrong? | Text description |
| Preventive measures | Text description |
| Probability | Dropdown: Sjelden / Lite sannsynlig / Mulig / Sannsynlig |
| Consequence | Dropdown: Lav / Middels / Høy / Svært alvorlig |
| Risk level | Auto-calculated from probability × consequence |

Multiple hazards can be added (add row button).

**Risk Matrix (Visual):**
```
                  Consequence
                  Lav      Middels    Høy      Svært alvorlig
Probability
Sannsynlig      | Yellow | Orange  | Red    | Red          |
Mulig           | Green  | Yellow  | Orange | Red          |
Lite sannsynlig | Green  | Green   | Yellow | Orange       |
Sjelden         | Green  | Green   | Green  | Yellow       |
```

Each hazard plots on the matrix. The SJA shows the highest risk level prominently.

**Signatures:**
- All participants must sign (auto-applied with dispute option per digital-signature spec)
- SJA creator/responsible person signs
- Date and time of each signature recorded

### SJA Statuses
| Status | Meaning |
|--------|---------|
| Utkast | Being prepared, not yet signed |
| Aktiv | Signed by all participants, work can proceed |
| Fullført | Work completed, SJA archived |

### SJA on Mobile
- SJA can be created and filled out on mobile (it's done on site before work starts)
- Optimized flow: header → participants → risk questions → hazards → sign
- Each section is one screen/step on mobile
- Can be saved as draft and completed later

---

## Vernerunde — Safety Round (/hms/safety-rounds)

### What It Is
A periodic inspection of the workplace to identify hazards, check safety measures,
and document conditions. Usually weekly or biweekly.

### Architecture
Safety rounds use the **same checklist builder architecture** as regular checklists.
A safety round template is a checklist template with category-level grouping.

### Category-Level Toggling
Safety round templates are organized by categories (sections). Tenants can hide
entire categories that aren't relevant to their operation:

Example categories in a construction safety round template:
- ☑ Adkomst og sikring (Access and barriers)
- ☑ Stillas og fallsikring (Scaffolding and fall protection)
- ☑ Personlig verneutstyr (PPE)
- ☑ Brannvern (Fire safety)
- ☑ Orden og ryddighet (Housekeeping)
- ☐ Gravearbeid (Excavation) — hidden, not relevant for this project
- ☐ Kranarbeid (Crane work) — hidden
- ☑ Elektrisk sikkerhet (Electrical safety)
- ☑ Kjemikaliehåndtering (Chemical handling)

Each category contains checklist items (OK / Not OK / Not applicable + comments + photos).

### Purchasable Safety Round Templates
- Solodoc sells pre-built safety round templates per industry
- Construction, farming, machine operation, office, warehouse
- Tenant unlocks, customizes, hides irrelevant categories
- Can also upload their own existing schema as a starting point

### Scheduling
- Admin/project-leader sets up recurring schedule:
  "Vernerunde hver torsdag" or "Vernerunde annenhver uke"
- Per project or tenant-wide
- System tracks: was a report filed for this period?
- If not: shows as overdue in Forefallende
- Postponeable from Forefallende (new date, reason logged)

### Safety Round Report
- Completed safety round generates a PDF report
- Includes: date, location, participants, all items with responses,
  photos, comments, non-conformances highlighted
- Non-conformances can be auto-converted to deviations:
  "Opprett avvik fra dette punktet" button on any "Not OK" item

---

## HMS-møte — HMS Meeting (/hms/meetings)

### What It Is
Regular safety meetings where the team discusses safety topics, reviews deviations,
and plans safety activities. Usually weekly (Monday mornings).

### Scheduling
- Recurring schedule set by admin: "HMS-møte hver mandag 07:30"
- Tracked same as safety rounds (filed/not filed per period)
- Postponeable from Forefallende

### Meeting Minutes
- Agenda items (text list, reorderable)
- Attendees (select from employees, auto-signed with dispute option)
- Discussion points and decisions (rich text per agenda item)
- Action items: what, who, deadline
  - Action items can be converted to tasks within a project
  - Or converted to deviations if safety issues are identified
- Link to relevant deviations discussed
- Signature by meeting leader
- Generates PDF

### Meeting Minutes Template
- Tenants can create a standard agenda template for their HMS meetings
- Common items pre-filled each time: "Gjennomgang av avvik", "Status HMS-tiltak",
  "Nye risikoforhold", "Eventuelt"
- Saves time — meeting leader just fills in the specifics

---

## Deviation Types (Unified with RUH)

### Classification
Instead of a separate RUH (Rapport om Uønsket Hendelse) module, deviations have
a **type** field that distinguishes between different kinds of incidents:

| Type | Description | Extra Fields | Notifications |
|------|-------------|-------------|---------------|
| **Materiell skade** | Damage to equipment, materials, property | Estimated cost (optional), affected equipment | Standard deviation notifications |
| **Personskade** | Personal injury | Injury description, body part, severity, first aid given, hospital visit (yes/no) | Immediate notification to admin + project-leader. May require Arbeidstilsynet reporting. |
| **Nestenulykke** | Near-miss, could have caused injury | What could have happened, why it didn't | Standard + admin notification |
| **Farlig tilstand** | Dangerous condition identified | Risk assessment | Standard notifications |
| **Kvalitetsavvik** | Quality non-conformance | Standard deviation fields | Standard notifications |
| **Miljøavvik** | Environmental issue | Environmental impact description | Standard notifications |

### Personal Injury (Personskade) — Extra Fields
When type is "Personskade":
- Injured person (select from employees or type name for externals)
- Injury description (free text)
- Body part affected (dropdown: hode, arm, ben, rygg, hånd, fot, annet)
- Severity: Lett (minor first aid) / Moderat (medical treatment) / Alvorlig (hospital)
- First aid given: yes/no, description
- Hospital visit: yes/no
- Days of absence (if known)
- **Arbeidstilsynet reporting reminder:**
  If severity is "Alvorlig" → system shows prominent reminder:
  "Alvorlige personskader skal meldes til Arbeidstilsynet.
  Kontakt Arbeidstilsynet på 73 19 97 00"
  This is a reminder only — Solodoc does not report automatically.

### How This Replaces RUH
- What was previously "RUH" is now a deviation with type Personskade or Nestenulykke
- Same creation wizard, same lifecycle (Åpen → Under behandling → Lukket)
- Same photo annotation, voice-to-text, before/after comparison
- Same reporting and export
- The type field allows filtering and separate reporting:
  "Show me all personal injuries this year" vs "Show me all quality deviations"

---

## Risk Assessment (Risikovurdering)

### What It Is
Systematic evaluation of hazards in the workplace. Required by internkontrollforskriften.
Broader than SJA — covers ongoing operations, not just specific tasks.

### Creation
- Template-based (from template builder or purchasable templates)
- Identify hazards → assess probability and consequence → determine risk level
- Risk matrix (same as SJA)
- Existing measures → additional measures needed
- Responsible person and deadline for each measure
- Review date (when to reassess)

### Storage
- Linked to projects or tenant-level (general risk assessments)
- Part of the HMS handbook documentation
- Versioned — when reassessed, old version preserved
- Review date appears in Forefallende when approaching

---

## Navigation in HMS Section
```
HMS-håndbok (/hms)
  ├── Håndboken (interactive document)
  ├── SJA (/hms/sja)
  ├── Vernerunder (/hms/safety-rounds)
  ├── HMS-møter (/hms/meetings)
  └── Risikovurderinger (/hms/risk-assessments)
```

Note: Deviations (including personal injury and near-miss) live in the
main Deviations module (/deviations) with type filtering, not under HMS.
This keeps all deviations in one place for unified management and reporting.
The HMS section links to deviation reports filtered by type when relevant.
