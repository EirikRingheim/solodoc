# Module Spec: Contacts (Kontakter)

## Overview
A shared address book for the tenant. The primary purpose is finding the right
person for the job — search by what they do (title/description), not just their name.
Covers customers, subcontractors, suppliers, inspectors, architects, engineers,
and anyone else the company works with.

Contacts are tenant-wide (shared across all projects) and can be linked to
specific projects.

---

## Contacts List Page (/contacts)

### Layout
- MudDataGrid with server-side pagination, sorting, search
- Columns: Name, Title/Description, Company, Type, Phone, Email
- **Title/Description is prominent** — this is how people find contacts
  ("AC", "Rørlegger", "Bygginspektør", "Betong leverandør")
- Type badge: Kunde, Underentreprenør, Leverandør, Inspektør, Rådgiver, Annet
- Click row → /contacts/{id}
- "Ny kontakt" button

### Search
- **Searches across: title/description, name, company, email, phone**
- Title/description is weighted highest in search results
- Searching "AC" returns the AC technician
- Searching "betong" returns the concrete supplier and the concrete subcontractor
- Fuzzy matching (typo tolerance via pg_trgm)

### Filters
- Type: Kunde, Underentreprenør, Leverandør, Inspektør, Rådgiver, Annet
- Linked to project (show contacts for a specific project)
- Company (filter by specific company)

---

## Contact Detail Page (/contacts/{id})

### Primary Fields (Always Visible)
| Field | Required | Notes |
|-------|----------|-------|
| Name | Yes | Person name or company name |
| Title / Description | Yes | **Most important field.** What they do. "AC-tekniker", "Rørlegger", "Kommunal bygginspektør", "Betongleverandør" |
| Company | No | Company they work for (if person) or company name (if company contact) |
| Type | Yes | Kunde / Underentreprenør / Leverandør / Inspektør / Rådgiver / Annet |
| Phone (primary) | Yes | Main phone number |
| Phone (secondary) | No | Alternative number |
| Email | No | |
| Address | No | Street, postal code, city |
| Notes | No | Free text — any useful info |

### Customer-Specific Fields (Type = Kunde)
- Customer type: Bedrift / Privatperson
- Org number (Bedrift only, optional)
- Contact person (if company — the person you actually talk to)
- **Links to Customer Account** (/accounts/{customerId}) — see hours module

### Subcontractor-Specific Fields (Type = Underentreprenør)
- Org number
- Specialization (e.g., "VVS", "Elektro", "Betong", "Maling")
- **Solodoc account link:** if this subcontractor company has a Solodoc account,
  link it here. Shows their Solodoc user status and which projects they have access to.
  This connects the contact registry with the subcontractor access system.

### Linked Projects
- List of projects/jobs this contact is linked to
- "Legg til prosjekt" to link the contact to another project
- Contacts can be linked to multiple projects
- Visible from both the contact page and the project's contact section

---

## Contact Types

### Default Types (Tenant Can Add More)
| Type | Description | Example |
|------|-------------|---------|
| Kunde | Customer (company or private person) | Byggherre, privatperson |
| Underentreprenør | Subcontractor company | Rørlegger, elektriker, maler |
| Leverandør | Material or equipment supplier | Betongleverandør, stålleverandør |
| Inspektør | Inspector or authority contact | Kommunal bygginspektør, Arbeidstilsynet |
| Rådgiver | Consultant, architect, engineer | Arkitekt, geotekniker, statiker |
| Annet | Anything else | Utleiefirma, forsikring, transport |

Admin can add custom types in tenant settings.

---

## Creating a Contact

### Flow (Quick and Simple)
1. Name (required)
2. Title / Description (required) — "Hva gjør denne kontakten?"
   - Autocomplete suggests previously used titles within the tenant
   - Examples: "Rørlegger", "Elektriker", "AC-tekniker", "Betong leverandør",
     "Kommunal bygginspektør", "Arkitekt"
3. Company (optional)
4. Type (dropdown)
5. Phone (required)
6. Email (optional)
7. Address (optional)
8. Notes (optional)
9. Link to project (optional)
10. Save

### Quick-Create from Other Modules
- When creating a project or job and need a new contact:
  "Ny kontakt" inline → compact form → saves and links in one step
- When reporting a deviation and need to reference an inspector:
  search contacts or quick-create

---

## Linking Contacts to Projects

### How It Works
- Contacts are tenant-wide — they exist independently of projects
- A contact can be linked to zero, one, or many projects
- Linking is done from: contact detail page, project contacts section, or during creation
- When viewing a project → "Kontakter" section shows all linked contacts
- When viewing a contact → "Prosjekter" section shows all linked projects

### Project Contact Roles
When linking a contact to a project, optionally assign a role:
- Byggherre (client)
- Byggherre-inspektør (client inspector)
- Arkitekt
- Rådgivende ingeniør (consulting engineer)
- Underentreprenør — [specialization]
- Leverandør — [material type]
- Annet

This makes it easy to find "who is the architect on this project?"

---

## Customer Entity Integration

### Contacts and Customer Accounts Are Connected
- A contact of type "Kunde" IS the customer entity
- When a customer contact is created, it's available in:
  - The contacts list (/contacts)
  - The customer accounts list (/accounts)
  - Project/job customer selection
- No duplicate data — one entity, multiple views
- Creating a customer when setting up a Job auto-creates the contact entry
- The customer account (hours, parts costs) is a view on top of the contact

---

## Subcontractor Solodoc Account Linking

### When a Subcontractor Also Uses Solodoc
- If a subcontractor company has a Solodoc account (their own tenant):
  - Admin can link the contact entry to their Solodoc tenant
  - The contact page shows: "Denne underentreprenøren bruker Solodoc"
  - When inviting them to a project as subcontractor, the system can
    send the invitation directly to their Solodoc account
- If they don't have Solodoc:
  - Contact entry works normally (just contact info)
  - Project invitation creates a light account for them

---

## Export
- Contact list export: PDF or Excel
- Filterable: by type, by project, by company
- PDF: formatted contact directory with name, title, company, phone, email
- Excel: one row per contact, all fields
- Useful for: project documentation handover, tender documentation

---

## Offline Access
- Contact list is cached locally for offline access
- Search works offline against cached contacts
- New contacts created offline are synced when connectivity returns
- Ensures workers can always look up a phone number on site
