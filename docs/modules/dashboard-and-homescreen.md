# Module Spec: Dashboard & Home Screen

## Overview
The dashboard is the first thing every user sees when they open Solodoc. It must be
role-adaptive — showing different layouts and information based on the user's role
and context. The design philosophy varies by role:

- **Field workers / subcontractors:** mobile-first, minimal, action-oriented
- **Project leaders:** hybrid mobile/desktop, project-focused with light analytics
- **Tenant-admins:** desktop-first, analytics and overview-heavy, widget-based

---

## Global UI Elements (All Roles, All Screens)

### Context Switcher (Top Bar)
A persistent bar at the top of every screen showing the current company and project context.

```
[Logo] Fjellbygg AS  →  Prosjekt: Nybygg Sentrum
```

- Tapping company name opens a company switcher (for multi-tenant users)
- Tapping project/job name opens a project/job switcher
- Company logo is displayed alongside the name
- **Accent color** derived from the tenant's uploaded logo (extract dominant color)
  — used as subtle tint on the context bar, active states, and highlights
- Each tenant gets a distinct visual identity so workers immediately know which
  company context they're in
- On mobile: condensed to logo + short name, expandable on tap

### Notification Bell (Top Right)
- Always visible on every screen, all roles
- Unread count badge (red circle with number)
- Tapping opens a notification drawer (slide-in from right)
- Notification types:
  - Announcements from admin (with urgency level color)
  - Deviation assignments and updates
  - Certification expiry warnings (90-day, 30-day, expired)
  - Checklist deadlines approaching
  - Hours approved / rejected
  - Subcontractor check-in (if opted in by admin)
  - Job created (for admins, when field workers create jobs)
- Each notification links to the relevant entity (deep link)
- "Merk alle som lest" button
- Notifications are stored in database and synced across devices

### Sync Status Indicator
- Shown when relevant (not always visible — only when there's something to report)
- "Sist synkronisert: 5 min siden" — subtle, non-intrusive
- If unsynced items exist: "3 elementer venter på synkronisering" with a manual sync button
- If offline: "Du er frakoblet — endringer lagres lokalt" banner (non-blocking)

### Search Bar
- Available on all dashboards, prominently placed
- Searches across: projects, jobs, deviations, chemicals, employees, contacts, machines
- Results grouped by category
- On mobile: collapsed to a search icon, expands on tap
- Powered by full-text search (PostgreSQL tsvector or Elasticsearch — TBD)

---

## Icon & Visual Design Rules
- **No stock emojis anywhere in the UI** — this is a firm convention
- Use a professional, consistent icon library (Lucide or equivalent)
- Icons should be clean line-style, monochrome, subtle
- Use color only for status indicators and urgency levels, not decoratively
- Heatmaps where data density makes them useful (hours, deviations over time)
- Support both **light mode** and **dark mode**
  - Light mode: default, optimized for outdoor/bright conditions
  - Dark mode: optimized for indoor/low-light (tunnels, basements, night shifts)
  - User preference stored in profile, follows system setting as default
  - High contrast in both modes for readability in harsh conditions

---

## Field Worker Dashboard (Mobile-First)

### Layout Priority (Top to Bottom)

**1. Quick Actions (Top, Always Visible)**
A row of large, tap-friendly buttons. Minimum 48x48px touch targets.

```
[Stemple inn/ut]  [Nytt oppdrag]  [Avvik]  [Sjekkliste]
```

- "Stemple inn/ut" toggles based on current clock state
  - If clocked out: shows "Stemple inn" — tapping opens project/job selector then starts clock
  - If clocked in: shows "Stemple ut" with current duration visible, tapping stops clock
- "Nytt oppdrag" — creates a lightweight job (quick flow, under 2 minutes)
- "Avvik" — opens deviation reporting wizard
- "Sjekkliste" — opens list of assigned/available checklists for current project

**2. Announcement Wall**
- Shows recent announcements from admin/project-leader
- Most recent at top, scrollable
- Color-coded urgency: info (blue), warning (amber), critical (red)
- Rich text with formatting, links, and photo/PDF attachments
- Dismissable per user (but admin can mark as "must acknowledge")
- "Must acknowledge" announcements show a confirmation button and are tracked

**3. Active Clock Status**
- If clocked in: prominent display showing
  - Current project/job name
  - Clock started at (time)
  - Running duration
  - GPS location where clock-in happened
- If clocked out: shows nothing (or subtle "Ikke stemplet inn")

**4. Today's Assignments**
- Checklists assigned to this worker for today (or overdue)
- SJA forms that need sign-off
- Deviation follow-up items assigned to this worker

**5. Weekly Hours Summary**
- Simple display: "Denne uken: 32t 30min / 37,5t"
- Overtime indicator if threshold exceeded
- Tapping opens full hours detail for the week

**6. Recent Deviations (Project Scope)**
- Shows recent deviations on projects the worker is assigned to
- Excludes deviations marked as "confidential" by admin
- Brief: title, status (open/closed), severity, date
- Tapping opens the deviation detail

### What Field Workers Do NOT See on Dashboard
- Analytics / statistics
- Employee management
- Template editing
- Billing / subscription info
- Other tenants' data

---

## Project Leader Dashboard (Hybrid Mobile/Desktop)

Everything from the field worker dashboard, plus:

**Additional Sections:**

**1. Project Overview Cards**
- One card per project they lead
- Each card shows:
  - Project name and status
  - Workers currently on site (from check-in data)
  - Open deviations count (with severity breakdown)
  - Checklists: X completed / Y total
  - Hours this week across all workers on the project
- Tapping a card opens the full project page

**2. Hours Awaiting Approval**
- List of time entries submitted by workers that need approval
- Quick approve / reject actions
- Total hours pending

**3. Deviation Summary**
- New deviations reported today/this week
- Overdue deviations (past corrective action deadline)
- Tapping opens deviation list filtered to their projects

**4. Project Analytics (Inline)**
- Key metrics shown directly on the project page (not just dashboard):
  - Open vs closed deviations (trend)
  - Checklist completion rate
  - Hours per worker this week
  - "Se full rapport" button for advanced analytics

**5. Who's On Site**
- Live check-in list for each of their projects
- Name, role, company (for subcontractors), check-in time
- Accessible from dashboard and from within each project

---

## Tenant-Admin Dashboard (Desktop-First)

### Layout: Widget Grid
The admin dashboard uses a widget/tile grid layout (similar to a phone home screen).
Each widget shows a summary metric with a visual indicator. Clicking any widget opens
a dedicated full-page view for that module.

**Widget Tiles:**

**Projects & Jobs**
- Active projects count
- Active jobs count
- Overdue milestones
- Click → full project/job list

**Deviations (Avvik)**
- Open deviations count
- Overdue deviations (past deadline) — highlighted red
- Deviations this month vs last month (trend arrow)
- Heatmap: deviations over time (calendar view — shows hot days)
- Click → deviation management page

**Hours & Time**
- Total hours this week (all employees)
- Hours awaiting approval count
- **Heatmap: hours per day** (shows workload distribution across the week/month)
- Hours per project (bar chart or top 5 list)
- Overtime summary
- Click → hours management / approval page

**Employees & Certifications**
- Total active employees
- Certifications expiring within 30 days (count + list of names)
- Certifications already expired (count — red alert)
- Click → employee list with certification overview

**Checklists**
- Templates available count
- Checklists completed this week
- Overdue checklists (assigned but not completed past due date)
- Click → checklist management

**Chemical Register**
- Total chemicals registered
- SDS sheets due for review
- Click → chemical register

**Machine Park**
- Total machines
- Inspections due within 30 days
- Click → machine management

**Announcements**
- Compose new announcement button (prominent)
- Recent announcements with read/acknowledgment stats
- Click → announcement management

### Admin Calendar (Internal)
- Calendar view within Solodoc
- Shows: project milestones, checklist deadlines, certification expiry dates,
  scheduled safety rounds, employee absences
- Can filter by: project, employee, event type
- Week and month views
- **External sync (one-way push):**
  - Generates an iCal/CalDAV feed URL unique to the admin
  - Admin adds this URL to their Google Calendar / iCloud / Outlook
  - Solodoc events appear in their personal calendar alongside other appointments
  - One-way: Solodoc pushes to external calendar, never reads from it
  - No privacy concerns — only Solodoc data flows out, nothing flows in
  - Configurable: admin chooses which event types to sync

### Heatmap Usage Guidelines
Use heatmaps where data density makes them valuable:
- **Hours heatmap:** days × weeks grid, color intensity = hours worked. Instantly shows
  busy periods, holidays, seasonal patterns.
- **Deviation heatmap:** calendar-style, color intensity = number of deviations per day.
  Reveals patterns (weather-related? Monday mornings? Specific project phases?)
- **Check-in heatmap:** per project, shows which days had most workers on site.
- Heatmaps always include a legend and are colorblind-friendly (use sequential
  single-hue palette, not red-green)

---

## Subcontractor Dashboard (Mobile-First, Minimal)

**Layout:**

**1. Worksite Check-In Button (Prominent)**
- Large, impossible-to-miss button at the top
- Shows current status: checked in (green) / not checked in (neutral)
- GPS confirmation on check-in

**2. Who's On Site**
- List of people currently checked in at the same worksite
- Name, role, company

**3. Today's Assignments**
- Assigned checklists
- SJA forms to sign
- Deviations they reported (status updates)

**4. HMS-Håndbok**
- Quick access to the project's HMS handbook
- Required reading for subcontractors to follow the company's safety protocols

**5. Chemical Register**
- Access to SDS / chemicals relevant to the project
- GHS pictograms always displayed (language-independent safety)

**6. Solodoc Subtle Promotion**
- Small, non-intrusive banner at bottom of dashboard
- "Bruk Solodoc for din egen bedrift? Prøv gratis →"
- Dismissable, doesn't reappear for 30 days after dismissal
- Never shown during critical workflows (check-in, checklists, deviations)

---

## Profile-Only User Dashboard (No Tenant)

For users who have created a Solodoc account but aren't yet a member of any company.

**Layout:**

**1. Welcome Message**
- "Velkommen til Solodoc! Du er ikke lagt til i en bedrift ennå."
- "Når en bedrift inviterer deg, ser du dem her."

**2. Profile Completeness Progress Bar**
- Visual progress: "Din profil er 60% komplett"
- Checklist of what's missing:
  - ☑ Navn og kontaktinfo
  - ☐ Last opp sertifikater
  - ☐ Legg til pårørende
  - ☐ Fyll inn arbeidserfaring
- Completing the profile makes them immediately useful when invited to a company

**3. Certification Upload**
- Prominent prompt to upload their certifications now
- "Last opp sertifikater nå — de følger deg automatisk når du blir lagt til i en bedrift"

---

## Announcement / Message Wall System

### Creating Announcements (Admin / Project-Leader)
- Rich text editor with formatting (bold, italic, lists, links)
- Attach photos or PDF documents
- Set **target audience:**
  - All employees in tenant
  - Specific project team(s)
  - Specific individuals
  - Subcontractors on project (if applicable)
- Set **urgency level:**
  - Info (blue) — general updates, schedule changes
  - Warning (amber) — safety notices, weather alerts, procedure changes
  - Critical (red) — immediate safety hazard, emergency, stop-work orders
- Set **expiry date** (optional) — announcement auto-hides after this date
- **Require acknowledgment** toggle — if on, recipients must tap "Bekrefter"
  and this is logged with timestamp (useful for safety-critical communications)

### Viewing Announcements
- Shown on dashboard wall, most recent first
- Urgency color coding on the left border/accent
- Critical announcements pinned to top regardless of date
- Attachments viewable inline (images) or downloadable (PDFs)
- "Bekrefter" button visible on acknowledgment-required announcements

### Announcement Analytics (Admin)
- Per announcement: how many read, how many acknowledged
- List of who has NOT acknowledged (for follow-up)
- Export acknowledgment log as PDF (for documentation / audits)

---

## Mobile vs Desktop Behavior

| Element | Mobile | Desktop |
|---------|--------|---------|
| Quick actions | Large buttons, top of screen | Sidebar or top bar |
| Widget grid (admin) | Vertical stack, scrollable | 2-3 column grid |
| Heatmaps | Simplified, tap to expand | Full size with hover details |
| Calendar | Week view default | Month view default |
| Navigation | Bottom tab bar | Left sidebar |
| Announcement wall | Full width cards | Column within dashboard |
| Context switcher | Logo + abbreviated name | Logo + full name + project |
| Search | Icon, expands on tap | Always-visible search bar |
