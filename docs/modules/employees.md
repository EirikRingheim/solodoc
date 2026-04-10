# Module Spec: Employees (Ansatte)

## Overview
The employees module handles employee profiles, certifications, internal training
records, vacation tracking, documents, and the employee lifecycle (onboarding to
offboarding). Every user owns their own profile data, but tenant-admins can view,
edit, and manage employees within their organization.

---

## Employee List Page (/employees)

### Layout
- MudDataGrid with server-side pagination, sorting, search
- Columns: Name, Role, Email, Phone, Certifications Status, Vacation Remaining, Status
- Certifications status: green (all valid), amber (expiring within 30 days), red (expired)
- Status: Aktiv / Suspendert / Sykmeldt
- Click row → /employees/{id}

### Filters
- Role (all, tenant-admin, project-leader, field-worker)
- Status (active, suspended, sick leave)
- Certification status (all, has expiring, has expired)
- Search by name, email, certification type

### Actions
- "Inviter ansatt" button → opens invitation dialog (email + role)
- Batch actions: export selected, export certifications

---

## Employee Detail Page (/employees/{id})

### Header
- Full name, profile photo (optional)
- Role badge, status badge
- Contact info: phone, email, address
- Hiring date
- Employment percentage (100%, 80%, 50%, etc.)

### Tabs

**Oversikt (Overview)**
- Contact information: phone, email, address
- Hiring date, employment percentage
- Current schedule (ordning) assignment
- Quick stats: total hours this month, active projects, vacation remaining
- Profile completeness indicator (if missing certifications or info)

**Sertifikater (Certifications)**
- List of all certifications: type, issuer, issue date, expiry date, status
- Status: Gyldig (green), Utløper snart (amber, within 30 days), Utgått (red)
- Each cert shows the uploaded document (photo/PDF) — viewable inline
- "Legg til sertifikat" button → upload flow with OCR expiry extraction
- Admin can upload and edit certifications on behalf of the employee
- "Mangler utløpsdato" warning on certs where OCR failed and date wasn't manually set
- History of previous versions (re-uploads)

**Certifications are organized by type.** When an employee uploads an "HMS-kort",
it's registered under the HMS-kort category. Admin can later export all HMS-kort
across all employees in one action (see export section).

**Intern opplæring (Internal Training / Dokumentert opplæring)**
- Records of internal training the employee has received
- Required by forskrift om utførelse av arbeid
- Each record:
  - Training topic (e.g., "Bruk av lift XYZ-200", "Brannslukking", "Sikker betongstøp")
  - Date of training
  - Trainer (who conducted the training — select from employees)
  - Description / notes
  - Duration (hours)
  - Signed by trainer (stored signature)
  - Signed by trainee (stored signature)
  - Attached documents (training material, presentations)
- "Ny opplæring" button → create training record
- Admin and project-leaders can create training records
- Employees can view their own training records
- Exportable as PDF per employee or as a complete training log

**Kompetanse (Competence)**
- Free-text skills field for informal competencies
  (e.g., "Erfaren med betongstøp", "Kan kjøre hjullaster", "Sveisekurs Klasse B")
- Tags for searchability
- "Oppfyller krav" indicator per project — shows if the employee meets
  the competence requirements for projects they're assigned to
- Simple and lightweight — not overengineered

**Timer (Hours)**
- Hours summary for this employee across all projects
- Filterable by date range and project
- Weekly view with regular vs overtime breakdown
- Allowances summary
- Link to full hours export

**Ferie og fravær (Vacation & Absence)**
- Vacation balance (see Vacation Tracking section below)
- Absence calendar: visual overview of vacation, sick leave, training days
- Register planned vacation
- Register sick leave (egenmelding / sykemelding — see Sick Leave section)

**Prosjekter (Projects)**
- List of projects this employee is assigned to / has logged hours on
- Shows: project name, role, total hours, last activity
- Auto-populated from check-ins and time entries

**Dokumenter (Documents)**
- General document storage for the employee
- Upload: employment contract, NDA, equipment receipt list, performance notes,
  safety introduction confirmation, any other employee-related documents
- Organized by category (admin can create categories)
- Upload by admin or by the employee themselves (for their own documents)
- Separate from certifications — this is for non-expiring documents

**Innfasing / Utfasing (Onboarding / Offboarding)**
- Checklist-based (uses the template builder)
- See Onboarding/Offboarding section below

---

## Self-Service Profile (/profile)

### What the Employee Sees
Every user can access and edit their own profile regardless of tenant context.

**Personal Info:**
- Name, email, phone, address (editable)
- Profile photo (editable)
- Hiring date (read-only, set by admin)

**Signatur (Signature):**
- Draw/update stored signature
- Used for document sign-offs

**Sertifikater (Certifications):**
- Same upload flow as admin view
- Employee can add new certs, re-upload expired ones
- OCR expiry extraction on upload
- Certs follow the person across tenants (they own this data)

**Intern opplæring (Training Records):**
- View training records (read-only — created by admin/trainer)
- Can see what training they've completed

**Dokumenter (Documents):**
- Upload own documents
- View documents admin has added

**Mini CV:**
- Auto-generated one-page summary (see CV section below)
- "Last ned mini-CV" button → PDF download

**Settings:**
- Language preference
- Dark mode toggle
- Password change
- Passkey management (Face ID, fingerprint)
- GPS consent status per tenant
- Notification preferences

**Privacy:**
- "Mine data" download (all personal data as JSON + PDF)
- Account deletion request

---

## Certification Management

### Upload Flow
1. User or admin taps "Legg til sertifikat"
2. Enter certification type (free text — no predefined list, but autocomplete
   from previously used types in the tenant for consistency)
3. Upload photo or PDF (camera capture or file picker)
4. System runs OCR via Tesseract:
   - Extracts text from document
   - Date pattern matching for Norwegian formats (dd.MM.yyyy, MM/yyyy)
   - Looks for keywords: "gyldig til", "utløpsdato", "valid until", "expiry"
5. If expiry date found: pre-filled, user confirms or corrects
6. If not found: user enters expiry date manually (or marks as "Ingen utløpsdato")
7. Optional fields: issuer, issue date, certificate number
8. Save → cert added to profile, document stored in MinIO

### Certification Type Autocomplete
- No predefined system-wide list (industries vary too much)
- Autocomplete suggests types already used within the tenant
- Over time, the tenant builds their own consistent list organically
- Examples that will emerge: HMS-kort, Kranførerbevis, Truckførerbevis,
  Varmearbeid, Førstehjelpskurs, ADK-sertifikat, Maskinførerbevis

### Bulk Export by Certification Type
- Admin can export all certifications of a specific type across all employees
- Example: "Export all HMS-kort" → ZIP with individual PDF/photos per employee
- Accessible from: employee list → actions → "Eksporter sertifikater" → select type
- Also accessible from admin settings or reporting module

### Expiry Notifications (Forefallende Integration)
- Expiring certifications automatically appear in Forefallende
- 90 days: info notification to employee and admin
- 30 days: warning notification to employee, admin, and project-leaders
- Expired: alert notification, employee flagged in project views
- "Mangler utløpsdato" certs flagged in reporting as high-risk items

---

## Internal Training (Dokumentert Opplæring)

### Purpose
Norwegian regulations require documented proof that employees have been trained
on specific tasks, equipment, and safety procedures. This is separate from
external certifications — it's the company's internal training record.

### Creating a Training Record
1. Admin or project-leader selects the employee
2. Fills in: topic, description, date, duration
3. Selects trainer (from employee list)
4. Attaches training material (optional)
5. Trainer signs (stored signature)
6. Trainee signs (stored signature — or auto-applied with dispute option)
7. Record saved and linked to the employee

### Training Record Data
| Field | Required |
|-------|----------|
| Topic / title | Yes |
| Description | No |
| Date | Yes |
| Duration (hours) | No |
| Trainer | Yes (select from employees) |
| Trainee | Yes (the employee) |
| Trainer signature | Yes |
| Trainee signature | Yes |
| Attached documents | No |

### Export
- Per employee: complete training log as PDF
- Per training topic: all employees trained on a specific topic
- Suitable for audits and Arbeidstilsynet inspections

---

## CV Generation

### Mini CV (One Page)
Auto-generated from profile data. Quick document for sending to clients,
subcontractors, or for tender documentation.

**Contains:**
- Name, photo
- Current role / title
- Key certifications (valid ones, with expiry dates)
- Recent internal training (last 5 records)
- Key competencies (from free-text skills)
- Years of experience (calculated from hiring date)

**Generated as PDF** with company logo header.
"Last ned mini-CV" button on profile and employee detail page.

### Full CV
More detailed version for formal purposes.

**Contains everything in mini CV, plus:**
- Full work history (if entered in profile)
- All certifications (including expired, marked as such)
- Complete training log
- Education
- Contact information

**Generated as PDF** with company logo header.
Available from employee detail page (admin) and profile (employee).

---

## Vacation Tracking

### Vacation Balance (Per Employee, Per Year)
| Field | Description |
|-------|-------------|
| Annual allowance (hours) | Set per employee based on schedule and employment % |
| Carried over from last year | Hours not used last year (with notation "fra i fjor") |
| Used this year | Sum of approved vacation hours |
| Remaining | Calculated: allowance + carried over - used |
| Projected at year end | Remaining minus planned but not yet taken vacation |

### Rules
- **Carried-over hours are used first** (ferie fra i fjor brukes først)
- When vacation is registered, system deducts from carry-over balance before current year
- Carry-over limit configurable per tenant (Norwegian law: up to 2 weeks / equivalent hours)
- Hours exceeding carry-over limit at year end are forfeited (or paid out — tenant policy)
- Year-end rollover runs as a scheduled job (January 1)
- Employment percentage affects allowance: 50% employee gets 50% of standard allowance

### Registering Vacation
- Employee or admin registers planned vacation
- Select date range
- System calculates hours based on the employee's schedule
  (a week of vacation for a 37.5h/week employee = 37.5 hours)
- Deducted from balance
- Shows on the absence calendar and the main calendar module
- Approval required? Configurable per tenant:
  - No approval needed (small companies)
  - Project-leader approves
  - Admin approves

### Vacation Overview (Admin)
- Table showing all employees with their vacation balances
- Who's on vacation now / this week / this month
- Calendar view: visual overview of team vacation schedule
- Useful for resource planning
- Export as PDF or Excel

---

## Sick Leave Tracking (Simple)

### What It Is
Simple visibility of sick leave — not a full HR system. Records that an employee
is away sick so it shows on the calendar and hours aren't expected.

### Types
- **Egenmelding** — self-reported sick leave (1-3 days typically)
- **Sykemelding** — doctor-certified sick leave (longer duration)

### Registration
- Admin registers sick leave: employee, type, start date, expected end date
- Employee can register egenmelding themselves
- Shows on the absence calendar
- Time entries are not expected for sick days (no clock-in reminders)

### Not Included (Payroll System Handles These)
- Sick pay calculations
- NAV refund tracking
- Sick leave percentage / Bradford factor
- Return-to-work plans

---

## Onboarding / Offboarding

### Implementation
Uses the existing checklist template builder — onboarding and offboarding are
just checklists with specific templates.

### Onboarding Checklist Template (Default, Customizable)
Admin creates or customizes from Solodoc base template:
- ☐ Arbeidskontrakt signert (employment contract signed)
- ☐ HMS-kort lastet opp (HMS card uploaded)
- ☐ Sikkerhetsintroduksjon gjennomført (safety introduction completed)
- ☐ Utstyr utlevert (equipment issued) — with photo documentation
- ☐ Systemtilgang opprettet (system access granted)
- ☐ Nødkontaktinformasjon registrert (emergency info registered)
- ☐ Intern opplæring planlagt (internal training planned)
- ☐ Relevant sertifikater verifisert (certifications verified)

### Offboarding Checklist Template (Default, Customizable)
- ☐ Sluttsamtale gjennomført (exit interview conducted)
- ☐ Utstyr innlevert (equipment returned) — with photo documentation
- ☐ Systemtilgang fjernet (system access removed)
- ☐ Nøkler/adgangskort innlevert (keys/access cards returned)
- ☐ Siste lønnsoppgjør bekreftet (final payroll confirmed)
- ☐ Sertifikat-snapshot lagret (certification snapshot saved for records)

### How It Works
1. Admin hires a new employee → sends Solodoc invitation
2. Admin creates an onboarding checklist instance for this employee
3. Admin (or the employee where applicable) checks off items as completed
4. Onboarding checklist linked to the employee record
5. Same process in reverse for offboarding
6. Completed checklists stored as documentation (audit trail)

### Tenant Customization
- Tenant can modify the default onboarding/offboarding templates
- Add industry-specific items (e.g., "Sikkerhetsklarering verifisert" for sensitive sites)
- Some items might vary per role (admin sets different templates per role if needed)

---

## Profile Completeness

### How It Works
- Progress bar shown on the employee's profile: "Din profil er 70% komplett"
- Checklist of what's complete and what's missing
- Weighting based on importance:
  - Contact info (name, email, phone): 20%
  - Profile photo: 5%
  - At least one certification uploaded: 25%
  - Emergency contact for current project: 10%
  - Signature registered: 15%
  - Skills / competencies filled in: 10%
  - Address: 5%
  - CV info (work experience): 10%

### Reminders
- If profile is incomplete, a gentle reminder once per month for 3 months
- After 3 reminders, no more automatic nudging
- The progress bar remains visible on the profile page (always accessible)
- Admin can see profile completeness per employee in the employee list

---

## Admin Actions

### Invite Employee
- Enter email, select role, select schedule (ordning)
- Send invitation (30-day expiry, resendable)
- See invitation status (pending, accepted, expired)

### Upload Certifications for Employee
- Admin can upload certs on behalf of any employee
- Same OCR flow — upload, extract expiry, confirm
- Useful for bulk onboarding (admin has a stack of cert copies)
- Cert is linked to the employee's profile

### Suspend Employee
- Immediately blocks tenant access
- Employee's personal account unaffected
- Reversible — admin can reactivate
- Reason field (internal note)

### Remove Employee (Offboarding)
- Triggers offboarding checklist (if configured)
- After checklist complete: removes from tenant
- Historical data preserved (hours, checklists, deviations)
- Tenant retains frozen certification snapshot for 5 years
- Employee keeps their personal Solodoc account and certifications

### Edit Permissions
- Change employee's role
- Change schedule assignment
- Add/remove individual permission overrides
- View effective permissions for this employee
