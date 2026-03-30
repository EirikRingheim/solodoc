# Module Spec: Reporting & Analytics

## Overview
Reporting turns collected data into actionable insight. The approach is pre-built
reports with flexible filters and export — not a custom report builder. Every report
supports date range filtering, PDF export (formatted), and Excel export (raw data).

For anything the pre-built reports don't cover, users export to Excel and do
their own analysis.

---

## Design Philosophy
- Pre-built reports covering the most common questions
- Every report has: date range filter, project filter, employee filter where relevant
- Visual: heatmaps, bar charts, trend lines — not just tables
- Export on every report: PDF (formatted), Excel (raw data)
- Admin sees everything across the tenant
- Project leader sees their projects
- Field workers see their own data only
- Desktop-optimized (reporting is office work, not field work)

---

## Deviation Status Model

Three statuses, three colors, used consistently across all reporting and views:

| Status | Color | Meaning |
|--------|-------|---------|
| **Åpen** | Red | Newly reported, not yet assigned or being worked on |
| **Under behandling** | Amber | Assigned to someone, corrective action in progress |
| **Lukket** | Green | Corrective action completed and verified. Done. |

Lifecycle: **Åpen → Under behandling → Lukket**

Invalid or false reports: admin closes the deviation with a "Ikke relevant" note
in the close reason field. The deviation counts as closed. Audit trail preserved.

---

## Pre-Built Reports

### 1. Hours Report (Timer)

**Filters:** date range, project, employee, department/team

**Views:**
- **Per month:** total hours across the company, bar chart per month
- **Per week:** total hours, broken into regular vs overtime (stacked bar chart —
  regular hours in one color, overtime in a contrasting color)
- **Per project:** hours spent on each project in the period, ranked
- **Per employee:** hours per employee, with overtime highlighted
- **Overtime summary:** who had overtime, how much, which weeks

**Visuals:**
- Stacked bar chart: regular hours + overtime per week/month
- Heatmap: employees × weeks, color intensity = hours worked
- Plain text summary always shown alongside charts:
  "Totalt denne måneden: 1 247 timer (herav 89 timer overtid)"

**Export:**
- PDF: formatted report with charts and summary
- Excel: one row per time entry, all fields, filterable

---

### 2. Deviation Report (Avvik)

**Filters:** date range, project, severity, status, category

**Views:**
- **Overview:** total deviations by status (Åpen / Under behandling / Lukket)
  shown as colored counters at the top
- **Trend:** deviations opened vs closed per month (line chart)
- **By project:** which projects have the most open deviations
- **By category:** most common deviation types (bar chart)
- **Average close time:** how many days from opened to closed (trend over time)
- **Heatmap:** calendar view with color-coded dots per day
  - Red dots = deviations opened
  - Green dots = deviations closed
  - Intensity = count (more deviations = stronger color)

**Quarterly comparison:** available but not prominent — accessible via
"Vis avansert" toggle. Shows current quarter vs previous quarter side-by-side.

**Export:**
- PDF: formatted deviation summary report
- Excel: one row per deviation, all fields

---

### 3. Checklist Report (Sjekklister)

**Filters:** date range, project, template, employee

**Views:**
- **Completion rate:** per project — X completed / Y assigned (progress bars)
- **Overdue:** checklists assigned but not completed past due date
- **Most used templates:** which templates are used most frequently
- **Completion timeline:** when checklists were completed (useful for proving
  timely quality control)

**Export:**
- PDF: completion summary per project
- Excel: one row per checklist instance, status, completion date, who completed

---

### 4. Certification Report (Sertifikater)

**Filters:** employee, certification type, expiry window

**Views:**
- **Expiring soon:** grouped by urgency
  - Within 30 days (red)
  - Within 60 days (amber)
  - Within 90 days (yellow)
- **Already expired:** list with employee name, cert type, expiry date (red highlight)
- **Per employee:** all certifications for a selected employee with status
- **Missing expiry date:** certifications uploaded as photos/PDFs where OCR
  failed to extract an expiry date. Flagged with "Mangler utløpsdato" warning.
  These are high-risk — they might be expired and nobody knows.
  Admin can manually input the expiry date from this view.
- **Coverage overview:** matrix of employees × required cert types,
  showing who has what and what's missing

**Export:**
- PDF: certification overview per employee or for all employees
- Excel: one row per certification, all fields
- Individual cert files: links to export spec for per-employee file packages

---

### 5. Safety Activity Report (HMS-aktivitet)

**Filters:** date range, project

**Views:**
- **SJA count:** how many SJA forms completed per project, per month
- **Safety rounds (Vernerunder):** completed vs scheduled
  - Shows: scheduled date, completed date, overdue indicator
  - "Vernerunde ikke utført denne uken" highlighted in red
- **HMS meetings (HMS-møter):** completed vs scheduled
  - Same pattern as safety rounds
- **Incident reports (RUH):** count, severity, trend
- **Overall safety score:** a simple metric combining SJA completion rate,
  safety round adherence, and deviation close rate. Presented as a percentage
  or traffic light (green/amber/red). Gives admin a quick pulse on safety culture.

**Scheduled Activity Tracking:**
- Admin or project leader sets up recurring schedules:
  - "Vernerunde: Hver torsdag" (weekly safety round)
  - "HMS-møte: Hver mandag 07:30" (weekly HMS meeting)
  - Custom intervals: weekly, biweekly, monthly
- The system tracks whether the corresponding document was submitted:
  - Safety round report submitted that week? ✅ / ❌
  - HMS meeting minutes filed? ✅ / ❌
- Overdue items show in the dashboard and in this report
- This proves to Arbeidstilsynet that safety activities happen consistently

**Export:**
- PDF: safety activity summary for the period
- Excel: detailed log of all safety activities

---

### 6. Project Summary Report (Prosjektsammendrag)

**Filters:** single project selection

**Views:**
All key metrics for one project on one page:
- Project status and timeline
- Hours summary (total, overtime, per worker)
- Deviation summary (open, in progress, closed — with trend)
- Checklist completion rate
- Safety activities (SJA, safety rounds — completed vs scheduled)
- Crew list (auto-built from check-ins and hours)
- Active chemicals on this project
- Key roles assigned

This is the "everything about this project at a glance" view.
Admins and project leaders see the same data.

**Export:**
- PDF: comprehensive project report (suitable for byggherre handover)
- This connects to the documentation export system — "Eksporter full
  prosjektdokumentasjon" links to the export wizard

---

## Report Access by Role

| Report | Admin | Project Leader | Field Worker |
|--------|-------|---------------|--------------|
| Hours | All employees, all projects | Their projects | Own hours only |
| Deviations | All projects | Their projects | Own reported deviations |
| Checklists | All projects | Their projects | Own completed checklists |
| Certifications | All employees | Their project team | Own certifications |
| Safety Activity | All projects | Their projects | Not accessible |
| Project Summary | All projects | Their projects | Not accessible |

---

## Export From Reports

Every report page has an "Eksporter" button in the top-right corner.
Clicking it opens a dropdown:

```
Eksporter
  ├── PDF (Formatert rapport)
  ├── Excel (Rådata)
  └── Dokumentasjonspakke... (opens export wizard)
```

- **PDF:** the report as currently displayed, with charts and formatting.
  Company logo in header, date range and filters noted, page numbers.
- **Excel:** raw data underlying the report. One sheet per data type.
  All current filters applied. Column headers in Norwegian.
  Suitable for further analysis in Excel, Power BI, etc.
- **Dokumentasjonspakke:** links to the export wizard (from the audit/export spec)
  for full project documentation packages. Only shown on project-scoped reports.

---

## Scheduled Report Delivery (Future Enhancement)

Not in initial launch, but design the architecture to support:
- Admin configures: "Send me the hours summary every Friday at 16:00"
- System generates the report as PDF and emails it
- Configurable per report, per recipient, per schedule
- Uses the same report generation engine as the manual export

---

## Chart & Visualization Guidelines

- Use consistent color palette across all reports
- Deviation colors: Red (Åpen), Amber (Under behandling), Green (Lukket)
- Hours colors: Primary color (regular hours), Accent color (overtime)
- Heatmaps: sequential single-hue palette, colorblind-friendly
- All charts must have a plain-text summary nearby (not just visual)
  — essential for accessibility and for quick scanning
- Charts are interactive on desktop (hover for details)
- Charts are simplified on mobile (tap for details, fewer data points)
- No stock emojis, no decorative icons — clean, professional visualization
