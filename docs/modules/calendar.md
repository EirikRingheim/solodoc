# Module Spec: Calendar (Kalender)

## Overview
The calendar serves everyone: field workers create personal events, project leaders
schedule meetings and plan resources, admins see the full picture. Includes a
resource planner for assigning people and equipment to projects across days/weeks.

External sync pushes Solodoc events to iCloud/Google/Outlook calendars.

---

## Calendar Page (/calendar)

### Views
- **Week view** (default on mobile)
- **Month view** (default on desktop)
- **Day view** (tap on a specific day)
- Toggle between views

### Who Sees What

**Field Worker:**
- Their own personal events
- Meetings they're invited to
- Their resource assignments ("You're at Nybygg Sentrum Monday-Wednesday")
- Project milestones for projects they're assigned to
- Safety rounds and HMS meetings they're involved in

**Project Leader:**
- Everything field worker sees, plus:
- All events for their projects
- Team assignments (who is where on their projects)
- "Ressursplanlegger" button to open the resource planning view

**Admin:**
- Everything across the tenant
- Full resource planning view for all employees and projects
- All meetings, milestones, and scheduled activities

---

## Event Types (Color-Coded)

| Type | Color | Examples |
|------|-------|---------|
| Personlig hendelse | User's chosen color | "Tannlege 14:00", "Hente barn" |
| Prosjekt milepæl | Blue | "Fundamentering ferdig", "Overlevering" |
| Vernerunde | Green | "Vernerunde — Nybygg Sentrum" |
| HMS-møte | Green (darker) | "HMS-møte mandag 07:30" |
| Sertifikat utløp | Red/amber | "Kranførerbevis Ola — utløper 15.05" |
| Maskin inspeksjon | Orange | "EU-kontroll varebil AB 12345" |
| Fravær (ferie) | Light blue | "Kari Hansen — ferie" |
| Fravær (syk) | Gray | "Per Olsen — sykemeldt" |
| Ressurs-tildeling | Project accent color | "Ola → Nybygg Sentrum" |
| Egendefinert | Tenant-defined | Whatever the admin creates |

---

## Personal Events

### Creating Events
- Any user can create personal events
- Fields: title, date/time (or all-day), description (optional), repeat (optional)
- **Visibility:** creator chooses who can see the event:
  - "Bare meg" (only me) — default
  - "Mitt team / prosjekt" — people on the same projects
  - "Alle i bedriften" — entire tenant
  - Specific people (select by name)
- Events can be hidden/ignored by recipients:
  - If someone shares an event with you, you can hide it from your view
  - Hidden events don't appear on your calendar but still exist for the creator

### Recurring Events
- Repeat: daily, weekly, biweekly, monthly, custom
- End: never, after X occurrences, on a specific date
- Edit single occurrence or all future occurrences

---

## Meeting Invitations

### Creating a Meeting
- Admin or project leader creates a meeting event
- Adds participants (select from employees)
- Each participant receives a notification: "Du er invitert til [Meeting Title]"
- Participants can **accept** or **decline**
  - Accept: meeting appears on their calendar (confirmed)
  - Decline: meeting shows as declined, organizer is notified
    with who declined
- **Meetings cannot be hidden/ignored** — they stay visible even if not accepted
  (declined meetings show as strikethrough/grayed but remain on the calendar)
- Organizer sees attendance overview: who accepted, who declined, who hasn't responded

### Response Tracking
| Status | Display |
|--------|---------|
| Ikke svart | Neutral, shown as pending |
| Akseptert | Green check, confirmed on calendar |
| Avslått | Red X, grayed on calendar but still visible |

### HMS Meeting Integration
- HMS meetings created from the HMS module appear as meeting invitations
- Same accept/decline flow
- Meeting minutes are linked to the calendar event after the meeting

---

## Resource Planning (Ressursplanlegger)

### What It Is
A visual planning tool for assigning employees and equipment to projects across
days and weeks. Helps project leaders and admins answer: "Who is where this week?
Who is available? Are any projects understaffed?"

### Access
- Button on the calendar page: "Ressursplanlegger"
- Opens a full-screen planning view
- Available to: project-leaders (their projects only), admins (all projects)

### Layout (Gantt-Style Grid)

**Rows:** employees (or equipment — toggle between people and machines)
**Columns:** days (week view or two-week view)
**Cells:** project assignment for that person on that day

```
                Mon 14    Tue 15    Wed 16    Thu 17    Fri 18
Ola Nordmann   [Nybygg ][Nybygg ][Nybygg ][Bru Fin][Bru Fin]
Kari Hansen    [Nybygg ][Nybygg ][        ][Nybygg ][Nybygg ]
Per Olsen      [Bru Fin][Bru Fin][Bru Fin][Bru Fin][Bru Fin]
Ahmed Ali      [        ][E6 Utv ][E6 Utv ][E6 Utv ][        ]
```

- Each project assignment is color-coded (project accent color)
- Empty cells = available / not assigned
- Vacation/sick leave shown as gray blocks
- Conflicts (double-booked) highlighted with warning indicator

### Two-Level Planning

**Level 1 — Company Resource Plan (Admin View):**
Which project is each person assigned to on which days.
This is the main Gantt grid described above.

**Level 2 — Project Work Plan (Project Leader View):**
Within a project, what task is each person doing on which day.
Accessed by clicking a project name in the resource planner, or from within
the project page → "Arbeidsplan" tab.

```
Bru Finse — Uke 14
                Mon 14      Tue 15       Wed 16       Thu 17      Fri 18
Ola Nordmann   [Graving   ][Forskaling ][Betongstøp ][Armering  ][Armering  ]
Kari Hansen    [Graving   ][Graving    ][Forskaling ][Betongstøp][Opprydding]
Per Olsen      [Transport ][Forskaling ][Forskaling ][Betongstøp][Betongstøp]
```

- Tasks are **free text with autocomplete** from previously used task names
  within the tenant (keeps it fast and flexible)
- Task Groups can be selected instead of free text — if "Betongstøp" is a
  Task Group, selecting it auto-links the relevant checklists and safety
  procedures for that day
- Project leaders manage this within their project — no admin rights needed
- Changes notify affected workers

**What the Worker Sees:**
```
Min plan denne uken:
  Mandag:    Bru Finse → Graving
  Tirsdag:   Bru Finse → Forskaling
  Onsdag:    Bru Finse → Betongstøp
  Torsdag:   Nybygg Sentrum → Armering
  Fredag:    Nybygg Sentrum → Armering
```

Workers see both which project AND which task. Shown on their dashboard
and calendar. They don't see other people's assignments unless they're
a project leader.

### Interaction (Level 1 — Resource Plan)
- **Drag and drop:** admin drags an employee to a project slot on a day
- **Click empty cell:** opens assignment dialog — select project, optional notes
- **Click existing assignment:** edit or remove
- **Multi-day assignment:** drag across multiple days at once
- **Copy week:** "Kopier denne uken til neste uke" for repeating schedules

### Interaction (Level 2 — Project Work Plan)
- **Click empty cell:** type task name (autocomplete) or select Task Group
- **Click existing task:** edit or remove
- **Drag and drop:** move tasks between days and people
- **Copy week:** repeat the same plan next week

### Equipment Planning
- Toggle: "Ansatte" / "Utstyr"
- Same grid but rows are equipment items instead of people
- Shows which machines are assigned where
- Useful for: "The crane is at Nybygg Sentrum this week, can we move it to
  Bru Finse on Thursday?"

### What Workers See
- Workers see **their own row only** — "Min plan"
- Shown on their dashboard or calendar: "Denne uken:"
  - Mandag–onsdag: Nybygg Sentrum
  - Torsdag–fredag: Bru Finse
- They don't see other people's assignments (unless project-leader)
- Changes to their schedule trigger a notification:
  "Din plan er oppdatert: Torsdag er endret til Bru Finse"

### Schedule Notifications
- When admin changes a worker's assignment, the worker gets notified
- Notification includes: what changed, which days, which project
- Changes are logged in the audit trail

---

## Absence Calendar

### Vacation
- Registered via the employee module (vacation tracking)
- Shows on the calendar as light blue blocks
- Visible to: the employee, their project-leaders, admin
- Appears in resource planner as unavailable days

### Sick Leave
- Registered via the employee module
- Shows on the calendar as gray blocks
- Visible to: the employee, their project-leaders, admin
- Appears in resource planner as unavailable days

### Other Absence
- Kurs/opplæring (training/courses) — shown as purple blocks
- Permisjon (leave) — shown as gray blocks
- Admin registers, shows on calendar and resource planner

---

## External Calendar Sync (One-Way Push)

### How It Works
- Any user can generate a personal iCal feed URL
- Min profil → Kalender → "Generer kalenderlenke"
- URL is unique per user, token-authenticated
- User adds this URL to Google Calendar / iCloud / Outlook as a subscription
- Solodoc events appear in their personal calendar

### What Syncs
- Configurable per user: select which event types to include
- Default: meetings, resource assignments, project milestones
- Personal events only sync if the user opts in
- External calendar polls the feed URL periodically (standard iCal behavior)

### One-Way Only
- Solodoc pushes events out
- Never reads from external calendar
- No privacy concerns — only Solodoc data flows out

### iCal Feed Endpoint
```
GET /api/calendar/feed/{userFeedToken}.ics
```
- Token-based (no login needed for the calendar app to fetch)
- Token is regenerable if compromised
- Returns standard iCalendar (.ics) format
- Feed includes events for the next 90 days by default

---

## Calendar vs Forefallende

### Clear Distinction
- **Calendar** = when things happen (scheduled events, meetings, milestones, assignments)
- **Forefallende** = what needs action (overdue items, approaching deadlines, pending tasks)

### Overlap Is OK
Some items appear in both:
- A safety round scheduled for Thursday shows on the calendar (it's a scheduled event)
  AND in Forefallende if it's approaching or overdue (it needs action)
- An HMS meeting shows on the calendar (scheduled time) AND in Forefallende
  if meeting minutes haven't been filed (it needs action)

The calendar answers: "What's happening this week?"
Forefallende answers: "What do I need to deal with?"
