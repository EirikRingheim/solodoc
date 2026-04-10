# Module Spec: Hours & Time Tracking (Timer)

## Overview
Time tracking is one of the most-used features in Solodoc. Field workers clock in/out
daily. The system automatically calculates overtime and applies shift allowances based
on tenant-configured rules. The output is a structured hours summary that payroll
systems can consume — Solodoc calculates hours and allowances, not pay amounts.

---

## Time Registration (Field Worker View)

### Clock In/Out from Dashboard
- Prominent "Stemple inn" / "Stemple ut" button on dashboard
- Tap "Stemple inn" → select Project or Job
  - **Recent list:** last 3 projects/jobs the worker clocked into shown at top
  - Below: searchable list of all active projects and jobs
- GPS captured at clock-in (if consented)
- Active clock shows: project/job name, start time, running duration, category
- Tap "Stemple ut" → clock stops, GPS captured

### Time Entry Categories
- Arbeid (regular work — default)
- Reise (travel)
- Kontorarbeid (office work)
- Lagerarbeid (storage/warehouse)
- Kurs/opplæring (training)
- Annet (other)
- Tenant can add custom categories in admin settings

**Category display order:** sorted by usage frequency per user. Most-used categories
appear first. The full list is always accessible but common ones are at the top.
No manual configuration needed — learns from behavior.

Each category can be:
- Tied to a project/job (work done for that specific project)
- General (not project-specific, e.g., office work, training)

### Time Entry Data
| Field | Type | Required |
|-------|------|----------|
| EmployeeId | Guid | Yes (auto from auth) |
| ProjectId | Guid | One of ProjectId/JobId/General |
| JobId | Guid | One of ProjectId/JobId/General |
| Category | enum | Yes (default: Arbeid) |
| StartTime | DateTimeOffset | Yes |
| EndTime | DateTimeOffset | Yes (on clock-out) |
| BreakMinutes | int | From schedule or manual |
| OvertimeMinutes | int | Calculated from rules |
| Allowances | list | Calculated + manual operation-based |
| GpsLatitudeIn | double | If GPS enabled |
| GpsLongitudeIn | double | If GPS enabled |
| GpsLatitudeOut | double | If GPS enabled |
| GpsLongitudeOut | double | If GPS enabled |
| Notes | string | No |
| Status | enum | Auto |

### Manual Time Entry
- Worker can add time entries manually (for forgotten clock-ins)
- Same fields, entered after the fact
- Marked as "Manuell registrering"

### Operation-Based Allowance Registration
- Worker can register operation-based allowances during or after a shift
- Simple flow: tap "Legg til tillegg" → select allowance type → done
- Examples: risk allowance, blasting operation, diving, height work
- Trusted by default — no approval needed per registration
- **Visible in the hours overview** — when admin/project-leader reviews hours,
  operation-based allowances are highlighted so they're noticed
- Can be configured to require approval in tenant settings (same setting as hours approval)

---

## Work Schedules (Ordninger)

### What a Schedule Defines
A schedule template ("Ordning") defines what "normal working hours" looks like
for a group of employees. The system uses this to calculate overtime and apply
time-based allowances automatically.

### Schedule Template Configuration (Admin)

**Basic Settings:**
- Name (e.g., "Standard dagskift", "Skiftordning betong", "Kontorarbeid")
- Weekly hours (e.g., 37.5)
- Default break per day (e.g., 30 minutes, auto-deducted)

**Daily Pattern:**
| Day | Start | End | Break | Active |
|-----|-------|-----|-------|--------|
| Mandag | 07:00 | 15:00 | 30 min | ✅ |
| Tirsdag | 07:00 | 15:00 | 30 min | ✅ |
| Onsdag | 07:00 | 15:00 | 30 min | ✅ |
| Torsdag | 07:00 | 15:00 | 30 min | ✅ |
| Fredag | 07:00 | 15:00 | 30 min | ✅ |
| Lørdag | — | — | — | ❌ |
| Søndag | — | — | — | ❌ |

**Overtime Thresholds:**
- Daily: hours beyond scheduled end time (e.g., after 7.5 hours)
- Weekly: hours beyond weekly limit (e.g., after 37.5 hours)

### Assigning Schedules
- Admin assigns a schedule to individual employees or groups
- Different roles can have different schedules within the same tenant
- An employee has exactly one active schedule at a time
- Schedule can be changed (effective from a date)

---

## Allowance Rules (Tillegg)

### Allowance Types

**Time-Based (Automatic)**
Triggered automatically based on when the work happened.

| Rule | Time Range | Amount | Type |
|------|-----------|--------|------|
| Kveldstillegg | 15:00-21:00 | 50 kr/time | Fixed per hour |
| Natt-tillegg | 21:00-06:00 | 100 kr/time | Fixed per hour |
| Helgetillegg lørdag | All Saturday | 30% | Percentage |
| Helgetillegg søndag | All Sunday | 100% | Percentage |

**Day-Based (Automatic)**
Triggered by calendar — public holidays and special days.

| Rule | Trigger | Amount | Type |
|------|---------|--------|------|
| Røde dager | Public holiday | 100% | Percentage |
| Halv-rød | Custom day + cutoff time | Normal until cutoff, then 100% | Mixed |

**Operation-Based (Worker Registers)**
Worker manually registers when applicable.

| Rule | Trigger | Amount | Type |
|------|---------|--------|------|
| Risikotillegg | Risky operation | 500 kr/operasjon | Fixed per operation |
| Sprengning | Blasting work | 750 kr/operasjon | Fixed per operation |
| Høydetillegg | Height work | 75 kr/time | Fixed per hour |

**Fixed Per Day (Automatic)**
Applied when the worker is clocked in on applicable days.

| Rule | Trigger | Amount | Type |
|------|---------|--------|------|
| Diett | Away from base | 350 kr/dag | Fixed per day |
| Reisetillegg | Travel day | 200 kr/dag | Fixed per day |

### Allowance Rule Configuration (Admin)

Creating a new allowance rule:
- Name
- Type: time-based / day-based / operation-based / fixed per day
- Amount: fixed kroner OR percentage
- Per: hour / day / operation
- Time range: from-to (for time-based)
- Applicable days: weekdays / Saturday / Sunday / røde dager / specific days
- For whom: all employees / specific schedule / specific employees
- Active from date

### Allowance Groups
For situations where the same set of allowances applies to a whole crew for
an entire period (e.g., same shift pattern all week):

- Admin creates an allowance group: "Betong-laget uke 14"
- Selects employees in the group
- Selects applicable allowances
- Date range (e.g., Monday to Friday this week)
- All selected allowances are applied to all time entries for those
  employees in that period — no need to set it up per person per day

---

## Norwegian Public Holiday Calendar (Røde Dager)

### System-Maintained
Solodoc maintains the Norwegian public holiday calendar. Updated by Solodoc staff
when needed (dates are mostly fixed or calculable).

**Fixed Holidays:**
- 1. januar (Nyttårsdag)
- 1. mai (Arbeidernes dag)
- 17. mai (Grunnlovsdag)
- 25. desember (1. juledag)
- 26. desember (2. juledag)

**Moving Holidays (Calculated from Easter):**
- Skjærtorsdag
- Langfredag
- 1. påskedag
- 2. påskedag
- Kristi himmelfartsdag
- 1. pinsedag
- 2. pinsedag

**Halv-røde dager (Default, Configurable):**
- 24. desember (Julaften) — normal until 12:00, then rød dag rate
- 31. desember (Nyttårsaften) — normal until 12:00, then rød dag rate

Tenant can:
- Adjust the cutoff time for halv-røde dager (12:00, 14:00, etc.)
- Add custom halv-røde dager if their agreement has more
- The system auto-applies the correct rate based on clock times

---

## Automatic Calculation Engine

### How It Works
When a time entry is saved (clock-out or manual entry), the system calculates:

1. **Total hours worked** (EndTime - StartTime - BreakMinutes)
2. **Normal vs overtime split** based on the employee's schedule
3. **Time-based allowances** based on which hours fall in which time ranges
4. **Day-based allowances** based on whether it's a rød dag or halv-rød dag
5. **Fixed per day allowances** if applicable
6. **Operation-based allowances** from worker's manual registrations

### Example Calculation
Employee on "Standard dagskift" (07:00-15:00, 37.5h/week) works:
- Thursday (rød dag): 07:00 - 19:00, 30 min break

Result:
- Total: 11.5 hours
- Normal: 7.5 hours (scheduled)
- Overtime: 4 hours (beyond schedule)
- Rød dag tillegg: 11.5 hours × rød dag rate
- If evening allowance rule exists: 3 hours (15:00-18:00 after schedule, up to kveldstillegg start... depends on rules)

The exact calculation depends entirely on the tenant's configured rules.
The engine applies all applicable rules in priority order.

### Calculation Output (Per Time Entry)
```json
{
  "totalHours": 11.5,
  "normalHours": 7.5,
  "overtimeHours": 4.0,
  "allowances": [
    { "rule": "Rød dag tillegg", "hours": 11.5, "type": "percentage", "rate": 100 },
    { "rule": "Kveldstillegg", "hours": 3.0, "type": "fixed", "amount": 50 }
  ]
}
```

---

## Timelister Page (/hours) — Employee View

### Layout
- Current week by default
- Day-by-day view: each day shows time entries with project/job, category, start/end, hours
- Allowances shown per entry (subtle tags: "Kveld +3t", "Risiko ×1")
- Weekly summary at top: total hours, overtime, break time
- "Denne uken: 34t 30min / 37,5t" with visual progress bar
- Overtime highlighted when threshold exceeded
- Navigate between weeks (← forrige uke / neste uke →)
- Monthly view toggle

### Adding Entries
- "Ny registrering" button → manual time entry form
- "Legg til tillegg" button → register operation-based allowance

### Entry Statuses
| Status | Meaning |
|--------|---------|
| Utkast | Clock is running or entry not submitted |
| Innsendt | Submitted for approval (if approval required) |
| Godkjent | Approved (green) |
| Avvist | Rejected with reason (red) |

---

## Timer-oversikt Page (/admin/hours) — Admin/Project-Leader View

### Layout
- All time entries across the tenant (admin) or their projects (project-leader)
- Filterable by: employee, project, date range, status, category
- "Venter på godkjenning" tab shows entries needing approval
- Batch approve: select multiple → "Godkjenn valgte"

### Approval Configuration (Tenant Setting)
- **No approval required:** hours are automatically accepted (small companies)
- **Project-leader approves:** project-leader approves hours for their projects
- **Admin approves:** only admin can approve
- **Configurable per tenant** in admin settings

### Approval Flow (When Enabled)
1. Employee submits time entry
2. Appears in approver's queue
3. Reviewer sees: hours, category, project, allowances registered, GPS data
4. **Operation-based allowances are highlighted** — reviewer notices them
   without needing to hunt for them
5. Approve → Godkjent
6. Reject → must enter reason → employee notified

### Operation-Based Allowance Visibility
- When reviewing hours, operation-based allowances registered by the worker
  are shown with a distinct visual indicator (e.g., amber tag)
- Reviewer can see: what type, how many times, which days
- This provides oversight without requiring per-registration approval
- If tenant wants stricter control: enable "Godkjenn tillegg" in settings,
  which makes operation-based allowances part of the approval flow

---

## Customer Account (Kundekonto)

### Purpose
Aggregates all work done for a specific customer across Jobs and completed Projects.
Not an invoice — a clear overview for the company to use when billing through
their separate accounting system.

### Customer Account Page (/accounts)
- List of customers with aggregated data
- Toggle: Bedrifter / Privatpersoner / Alle
- Columns: Customer name, Type, Active Jobs, Completed Projects, Total Hours, Total Parts Cost
- Click → /accounts/{customerId}

### Customer Account Detail (/accounts/{customerId})

**Header:**
- Customer name, type (Bedrift/Privatperson), address, contact info

**Summary Cards:**
- Total hours (all time)
- Total parts cost
- Active jobs count
- Completed projects count

**Aktive oppdrag (Active Jobs) Section:**
- List of active Jobs for this customer
- Each shows: description, status, hours so far, parts cost so far
- Hours and costs accumulate in real-time
- These contribute to the running totals in the summary

**Fullførte prosjekter (Completed Projects) Section:**
- List of completed Projects for this customer
- Each shows: project name, final hours total, date range
- Only added to the account when project is marked Fullført
- Separated from active Jobs to prevent double-counting
- Shows as a settled/final record

**Fullførte oppdrag (Completed Jobs) Section:**
- Completed Jobs with final totals
- Hours, parts cost, completion date

**Export:**
- "Eksporter kundeoversikt" → PDF or Excel
- Choose date range, which jobs/projects to include
- PDF: formatted customer statement with itemized jobs and hours
- Excel: raw data, one row per time entry/parts item

---

## Hours Export

### Flexible Export Options
When exporting, the user gets a configuration dialog:

**Select scope:**
- Specific employees (multi-select)
- Specific projects/jobs (multi-select)
- Date range
- All / only approved / only pending

**Export format:**
- PDF summary
- PDF detailed (one page per employee with daily breakdown)
- Excel (raw data)

**What's included:**
- Toggle: include allowance breakdown
- Toggle: include overtime breakdown
- Toggle: include GPS data
- Toggle: include notes

**Partial summaries:**
- When exporting multiple employees, option to include a summary page
  per employee AND a grand total page
- When exporting multiple projects, option to include per-project subtotals

### PDF Summary Contains
- Company logo, report period
- Per employee section: name, schedule, total hours, overtime, allowances summary
- Per project section (if project-scoped): total hours, overtime, per employee
- Allowance breakdown table: rule name, hours/count, rate, total
- Subtotals per section, grand total at bottom
- Signature line for approval (optional)

### Excel Detail Contains
- One row per time entry
- Columns: employee, project/job, category, date, start, end, break,
  normal hours, overtime hours, each allowance type with hours and amount,
  GPS in/out, notes, status
- Suitable for import into payroll systems (Visma, Tripletex, etc.)
- Column headers in Norwegian

### Export Endpoints
```
GET /api/hours/export/project/{projectId}?from=&to=&format=pdf|xlsx
GET /api/hours/export/job/{jobId}?from=&to=&format=pdf|xlsx
GET /api/hours/export/employee/{employeeId}?from=&to=&format=pdf|xlsx
GET /api/hours/export/customer/{customerId}?from=&to=&format=pdf|xlsx
POST /api/hours/export/bulk — multiple projects/employees, custom config
```

---

## Offline Time Registration
- Clock in/out works fully offline
- Stored in IndexedDB sync queue
- GPS captured at moment of action (if available)
- Allowance calculations happen server-side after sync
- Operation-based allowances can be registered offline
- Synced when connectivity returns
- Dashboard shows pending sync count
