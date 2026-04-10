# Module Spec: Admin Settings (Innstillinger)

## Overview
Central configuration for the tenant. Primarily accessed by tenant-admin.
Organized into clear sections so the admin can find what they need without
hunting through menus.

---

## Settings Page (/admin/settings)

### Navigation
Settings are organized as a left-side menu with sections. Clicking a section
shows its settings on the right. Not tabs — a persistent sidebar within the
settings page for easy navigation between sections.

---

## Bedriftsprofil (Company Profile)

### Company Information
- Company name (pre-filled from BRREG at registration, editable)
- Org number (from BRREG, read-only)
- Business address (street, postal code, city)
- Postal address (if different from business address)
- Phone (company main number)
- Email (company main email)
- Website (optional)
- Business type (AS, ENK, ANS, etc. — from BRREG, read-only)

### Company Logo
- Upload company logo (drag and drop or file picker)
- Used in: context switcher, PDF reports, QR codes (check-in), email headers,
  document exports, handover packages
- Recommended: PNG or SVG, minimum 200x200px
- System extracts accent color from logo for the tenant's visual theme
- Preview: shows how the logo appears in the context switcher and on a PDF header

### Contact Persons (Bedriftens kontaktpersoner)
- List of company contact persons with roles
- Each entry:
  - Name
  - Title / role (e.g., "Daglig leder", "HMS-ansvarlig", "Økonomiansvarlig",
    "Driftsleder", "Kvalitetsansvarlig")
  - Phone (direct)
  - Email (direct)
  - Is primary contact (toggle — one person marked as primary)
- These are the company's official contacts — shown in HMS handbook,
  project documentation, and handover packages
- Different from the Contacts module — these are internal company representatives,
  not external contacts
- Editable by tenant-admin only

### Company Description (Optional)
- Short description of the company and what they do
- Used in: tender documentation, handover packages, company profile exports
- Rich text (from procedure builder block style — simple formatting)

---

## Språk (Languages)

- Enable/disable languages for the tenant
- Available: nb-NO, en, pl, lt, ro, es
- Default language for new users
- Only enabled languages appear in the user language picker
- Enabling a language triggers background translation of existing templates
- Disabling a language hides it from the picker but preserves translations

---

## GPS-sporing (GPS Tracking)

- Enable/disable GPS features for the tenant (default: disabled)
- When enabled: explanation text shown to admin about what GPS captures
- Status overview: list of employees with their consent status
  (Godtatt / Avslått / Ikke svart)
- Cannot force consent — employees decide individually
- GPS verification distance for check-in (default: 500m, adjustable)

---

## Varsler (Notifications)

### Email Notifications
- Enable/disable email notifications for the tenant (default: disabled)
- When enabled: configure per notification type:

| Notification Type | In-App | Email (configurable) |
|-------------------|--------|---------------------|
| Deviation reported | Always | Toggle |
| Deviation assigned | Always | Toggle |
| Deviation deadline approaching | Always | Toggle |
| Deviation escalation (Høy) | Always | Toggle |
| Certification expiry (90 days) | Always | Toggle |
| Certification expiry (30 days) | Always | Toggle |
| Certification expired | Always | Toggle |
| Hours approved/rejected | Always | Toggle |
| Meeting invitation | Always | Toggle |
| Schedule change | Always | Toggle |
| Announcement posted | Always | Toggle |
| Check-in notification | Off default | Toggle |

### Sender Configuration
- Default sender: noreply@solodoc.no
- Custom SMTP: optional for tenants who want emails from their own domain
  (advanced setting, most tenants won't use this)

---

## Tillatelser (Permissions)

### Role Configuration
Configure what each role can do within the tenant:

**Project Leader Permissions (toggles):**
- ☑ Create and edit templates (default: on)
- ☑ Create Task Groups (default: on)
- ☑ Approve hours (default: on)
- ☑ Manage subcontractors on their projects (default: on)
- ☑ Create and manage safety rounds (default: on)
- ☑ Create and manage HMS meetings (default: on)
- ☐ Manage employees (default: off — admin only)
- ☐ Access tenant settings (default: off — admin only)

**Field Worker Permissions (toggles):**
- ☑ Create Jobs/Oppdrag (default: on)
- ☑ Register hours (default: on)
- ☑ Report deviations (default: on)
- ☑ Complete checklists (default: on)
- ☑ Edit own profile and certifications (default: on — always on)
- ☐ Create personal calendar events visible to team (default: off)

### Individual Overrides
- Admin can override permissions per individual employee
- "Vis effektive tillatelser" shows what a specific person can actually do

---

## Avvik (Deviation Settings)

### Categories
- List of active deviation categories
- Add new, rename, deactivate existing
- Deactivated categories don't appear in new deviations but remain on historical ones
- Default categories: Sikkerhet, Kvalitet, Miljø, Utstyr, Annet

### Deviation Rules
- Require signature on close (toggle, default: off)
- Auto-escalation for Høy severity (toggle, default: on)
- Escalation time window: 12h / 24h / 48h / 72h (default: 24h)
- Escalation recipients: admin only / admin + specific people

---

## Timer og lønn (Hours & Payroll Settings)

### Work Schedules (Ordninger)
- List of all schedule templates
- "Ny ordning" button → create new schedule
- Edit existing schedules
- See how many employees are assigned to each schedule
- See schedule details: weekly hours, daily pattern, break rules

### Overtime Thresholds
- Configured per schedule (not globally)
- Daily threshold (default: 7.5 hours)
- Weekly threshold (default: 37.5 hours)

### Allowance Rules (Tillegg)
- List of all allowance rules
- "Nytt tillegg" button → create rule wizard
- Edit, activate, deactivate existing rules
- Rule types: time-based, day-based, operation-based, fixed per day
- Configuration per rule: amount, time range, applicable days, for whom

### Allowance Groups
- Create and manage allowance groups
- Assign employees and time periods

### Røde Dager (Public Holidays)
- System-maintained Norwegian public holiday calendar (read-only)
- Tenant-configurable:
  - Halv-røde dager cutoff time (default: 12:00, adjustable to 14:00, etc.)
  - Add custom halv-røde dager
  - Rød dag overtime rate (default: 100%)

### Hours Approval
- Approval required: Yes / No (default: No for small companies)
- Who approves: Project-leader / Admin / Both
- Operation-based allowances in approval flow: Yes / No
  (default: No — trusted, but visible in review)

### Break Rules
- Default break duration per schedule (e.g., 30 minutes)
- Auto-deduct break from hours: Yes / No
- Break is part of working hours: Yes / No (varies by agreement)

### Time Entry Categories
- List of available categories (Arbeid, Reise, Kontorarbeid, etc.)
- Add custom categories
- Deactivate categories not used
- Order: sorted by usage (automatic) or manually set

---

## Dokumentnummerering (Document Numbering)

### Per Document Type
| Type | Default Prefix | Configurable |
|------|---------------|-------------|
| Sjekkliste | SJL | Yes |
| Skjema | SKJ | Yes |
| Prosedyre | PRO | Yes |
| SJA | SJA | Yes |
| Avvik | AVV | Yes |

### Settings
- Custom prefix per type
- Starting sequence number (e.g., start at 100 instead of 001)
- Include project code in number: Yes / No
  (e.g., "NBS-SJL-001" where NBS is the project code)
- Format preview: shows what the next number will look like

---

## Sjekk inn (Check-In Settings)

- Enable worksite check-in (toggle, default: on)
- Auto-checkout time (default: 00:00 midnight)
- GPS verification distance (default: 500m — warn if worker checks in further away)
- Auto-start clock on check-in (toggle, default: off)
- Check-in notifications to project-leader (toggle, default: off)

### QR Code Management
- Generate QR codes per project
- Download as PNG/PDF for printing
- Regenerate if compromised (old QR stops working)
- QR codes include company logo in center

---

## Planlagte aktiviteter (Scheduled Activities)

### Safety Rounds (Vernerunder)
- Set up recurring schedules per project or tenant-wide
- Frequency: weekly, biweekly, monthly
- Day of week
- Assigned template (from checklist library)
- Who is responsible

### HMS Meetings (HMS-møter)
- Set up recurring schedules per project or tenant-wide
- Frequency: weekly, biweekly, monthly
- Day and time
- Default agenda template (optional)
- Default participants

---

## Ferie (Vacation Settings)

- Standard annual vacation allowance in hours (default: based on Norwegian law)
- Carry-over limit: maximum hours that can roll to next year
  (default: 2 weeks equivalent)
- Carry-over policy: forfeit excess / pay out excess (tenant decides)
- Vacation approval required: Yes / No
- Who approves vacation: Project-leader / Admin

---

## Utstyrspark (Equipment Settings)

### Vegvesen API
- API key status (connected / not connected)
- "Koble til Statens vegvesen" → instructions for ordering API key
- Test connection button
- Last successful sync date

### Maintenance Reminder Defaults
- Default reminder interval for new equipment (e.g., 12 months)
- Notification recipients for equipment inspections

---

## Abonnement (Subscription)

- Current plan name and tier
- User count: X of Y used (progress bar)
- Subcontractor count (free, shown for info)
- Purchased modules: list of unlocked premium content
- "Oppgrader" button (when implemented)
- Billing history (when implemented)
- Next billing date (when implemented)

---

## Datamigrasjon (Data Migration)

- Migration wizard access
- Migration status per step: completed / in progress / not started / skipped
- Can revisit steps to import additional data
- Link to platform-specific export guides

---

## Personvern (Privacy & Data)

- Data processing agreement (view/download DPA)
- Data retention policy (view)
- Sub-processor list (view)
- "Eksporter all bedriftsdata" → full tenant data export (background job)
- Data deletion request process (for tenant cancellation)
- Cookie/storage notice text (viewable)

---

## Solodoc Butikk (Purchasable Content)

- Browse available premium content modules
- Categories: HMS-håndbøker, Vernerunde-maler, SJA-maler,
  Prosedyrepakker, Sjekkliste-pakker
- Each item shows: preview, description, price
- Purchase flow (payment integration TBD)
- Purchased items appear in the relevant module's template library
- "Mine kjøp" — list of unlocked content

---

## Settings Access

### Who Can Access What
| Section | Admin | Project Leader | Field Worker |
|---------|-------|---------------|--------------|
| Bedriftsprofil | Full edit | View only | Not visible |
| Språk | Full edit | Not visible | Not visible |
| GPS | Full edit | Not visible | Not visible |
| Varsler | Full edit | Not visible | Not visible |
| Tillatelser | Full edit | Not visible | Not visible |
| Avvik | Full edit | View only | Not visible |
| Timer og lønn | Full edit | View schedules | Not visible |
| Dokumentnummerering | Full edit | Not visible | Not visible |
| Sjekk inn | Full edit | View | Not visible |
| Planlagte aktiviteter | Full edit | Edit for their projects | Not visible |
| Ferie | Full edit | Not visible | Not visible |
| Utstyrspark | Full edit | Not visible | Not visible |
| Abonnement | Full edit | Not visible | Not visible |
| Datamigrasjon | Full edit | Not visible | Not visible |
| Personvern | Full edit | Not visible | Not visible |
| Solodoc Butikk | Full edit | View/browse | Not visible |
