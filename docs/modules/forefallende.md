# Module Spec: Forefallende (Upcoming & Pending Items)

## Overview
"Forefallende" is a unified view of everything that needs attention — overdue items,
upcoming deadlines, and scheduled activities. It acts as a personal task board showing
what the user needs to deal with, organized by urgency.

This is NOT a separate module in the database — it aggregates data from deviations,
checklists, certifications, safety rounds, HMS meetings, calendar events, and other
modules into one prioritized view.

---

## Visual Design

### Color Language (Subtle, Professional)
Urgency is communicated through a colored left border on each card. No exclamation
marks, no alarm icons, no dramatic backgrounds. The color does the talking.

| Column | Left Border Color | Text Style |
|--------|------------------|------------|
| Over frist | Red | Normal weight, calm |
| Denne uken | Amber | Normal weight |
| Denne måneden | Neutral / light gray | Normal weight |
| Seinare | Gray | Lighter text |

Cards are clean white with subtle shadow, colored left border only.
Status text is descriptive but not alarming: "Forfalt 3 dager" not "FORFALT!!!"

---

## Dashboard Widget (Compact View)

### Layout
Kanban-style columns on the dashboard:

| Over frist | Denne uken | Denne måneden | Seinare |
|-----------|-----------|--------------|---------|

Each column shows cards for pending items. Cards are compact:
- Small icon indicating type (deviation, checklist, meeting, certification)
- Title / description (short)
- Subtitle: deadline or status info, time since overdue
- Colored left border per column

### "Rediger visning" Button
- User can customize which item types appear in their Forefallende view
- Toggle on/off per item type
- Saved per user (user preference in database)
- Each user sees what matters to them — a field worker might hide certification
  warnings, a project leader might hide personal calendar items

### "Se alle →" Button
- Opens the full Forefallende page (/forefallende)

---

## Full Page (/forefallende)

### Layout
Same four columns as the widget, but expanded with more detail and filtering.

### Filters
- Item type: Avvik, Sjekkliste, Møte, Vernerunde, Sertifikat, Kalender, Oppgave
- Project: filter by specific project
- Date range: custom range for "Seinare" column
- Assigned to: me / my team / all (admin only)

---

## Item Types That Appear

### Deviations (Avvik)
- Open deviations assigned to the user
- Deviations where corrective action deadline is approaching/passed
- Card shows: title, project, severity (color dot), deadline
- "Over frist" if corrective action deadline has passed
- **Postpone action:** extend deadline (reason required, logged in audit trail)

### Checklists (Sjekklister)
- Checklists assigned to the user that haven't been completed
- Checklists with a due date approaching
- Card shows: template name, project, due date
- **Postpone action:** set new due date (reason optional, logged)

### Safety Rounds (Vernerunder)
- Scheduled safety rounds that are due or overdue
- Based on recurring schedule set by admin/project-leader
- Card shows: "Vernerunde — [Project]", scheduled date
- "Over frist" if the scheduled date passed without a report being submitted
- **Postpone action:** reschedule to a new date (reason optional, logged).
  Original scheduled date preserved in audit trail.

### HMS Meetings (HMS-møter)
- Scheduled meetings coming up or overdue
- Card shows: "HMS-møte — [Project/Tenant]", scheduled date/time
- "Over frist" if no meeting minutes were filed for a past scheduled meeting
- **Postpone action:** reschedule to a new date/time (reason optional, logged)

### Certification Expiry (Sertifikater)
- Own certifications expiring within the view window
- For admins: employee certifications expiring
- Card shows: cert type, employee name (if admin), expiry date
- Color: amber left border if within 30 days, red if expired
- **Cannot postpone** — expiry dates are fixed
- **Snooze action:** "Påminn meg igjen om 7 dager" — hides the card temporarily
  but the certification remains expired/expiring. Snooze logged.

### Calendar Events (Kalender)
- Upcoming events from the internal Solodoc calendar
- Project milestones, scheduled inspections, custom events
- Card shows: event title, date/time, project (if linked)
- Events that are also synced to external calendars (iCloud/Google/Outlook)
  still show here — Forefallende is the internal canonical view
- **Postpone action:** reschedule event (updates both internal calendar and
  external sync feed)

### Tasks (Oppgaver — Within Projects)
- Tasks assigned to the user that are approaching deadline
- Card shows: task name, project, deadline
- **Postpone action:** extend deadline (reason optional, logged)

---

## Column Logic

### Over frist (Overdue)
- Items where the deadline or scheduled date has **passed**
- Sorted by: how many days overdue (most overdue first)
- Red left border
- Count shown on the "Forefallende" sidebar badge

### Denne uken (This Week)
- Items due within the current week (Monday to Sunday)
- Sorted by: date, earliest first
- Amber left border

### Denne måneden (This Month)
- Items due within the current month but not this week
- Sorted by: date, earliest first
- Neutral border

### Seinare (Later)
- Items due beyond the current month
- Sorted by: date, earliest first
- Gray border
- Limited to next 90 days by default (adjustable via filter)

---

## Postpone / Reschedule Feature

### How It Works
Each card (except certification expiry) has a subtle "Utsett" button.
Tapping it opens a compact dialog:

**For scheduled activities (safety rounds, HMS meetings, calendar events):**
- New date picker (and time picker for meetings)
- Reason field (optional for some, required for safety rounds)
- "Utsett" confirms the reschedule

**For deadlined items (deviations, checklists, tasks):**
- New deadline date picker
- Reason field (required for deviations, optional for others)
- "Utsett" confirms the new deadline

**For certification expiry:**
- No postpone available
- Instead: "Påminn meg igjen om..." → 7 dager / 14 dager / 30 dager
- Snoozes the notification card, does not change the expiry date
- Snooze is per-user (other people still see the expiry warning)

### Audit Trail
Every postpone action is logged:
- What was postponed
- Original date/deadline
- New date/deadline
- Who postponed it
- When
- Reason (if provided)

Visible in the item's history and in the admin audit log.
Prevents abuse — if someone keeps postponing safety rounds, admin can see it
in the reporting module.

---

## Role-Based View

### Field Worker Sees
- Their own assigned deviations, checklists, tasks
- Their own certification expiry warnings
- Safety rounds and HMS meetings they're involved in
- Their calendar events

### Project Leader Sees
- Everything field worker sees, plus:
- All deviations on their projects (not just assigned to them)
- All overdue checklists on their projects
- All safety round and HMS meeting schedules for their projects
- Team certification expiry warnings
- Project milestones

### Admin Sees
- Everything across the tenant
- All employee certification expiry warnings
- All overdue safety activities
- All unassigned deviations
- Tenant-wide calendar events

---

## Interaction

### Clicking an Item Card
Opens the detail page for that item:
- Deviation card → /deviations/{id}
- Checklist card → /checklists/{id}
- Safety round card → safety round report page
- HMS meeting card → meeting minutes page
- Certification card → /employees/{id} certifications tab (or /profile/certifications)
- Calendar event card → /calendar (focused on that event)
- Task card → /projects/{projectId} tasks section

No quick actions on the cards beyond "Utsett" — clicking through to the main
page is sufficient for all other actions. Keeps the cards clean and simple.

---

## Notification Badge

### Sidebar Navigation
- "Forefallende" in the sidebar shows a count badge
- Badge number = total items in **"Over frist" column only** (the urgent stuff)
- Badge color: red
- Updates when items become overdue or are resolved
- Badge is 0 (hidden) when nothing is overdue

---

## User Customization Persistence

### What's Saved Per User
- Which item types are visible (toggle per type)
- These preferences apply to both the dashboard widget and the full page
- Stored in user preferences in the database (synced across devices)
- Default: all item types visible
- Customization via "Rediger visning" button

### What's NOT Customizable
- Column definitions (Over frist / Denne uken / Denne måneden / Seinare) are fixed
- Sort order within columns is fixed (most urgent first)
- Color language is fixed (consistency across the platform)
