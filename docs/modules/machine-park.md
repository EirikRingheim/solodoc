# Module Spec: Equipment Park (Utstyrspark)

## Overview
Registry of all equipment owned or used by the tenant: vehicles, machines, tools,
trailers, and any other equipment that needs tracking. Covers maintenance scheduling,
inspection tracking, project assignment, and EU-kontroll reminders via Statens
vegvesen API integration.

The scope is broad — from a crane to a drill to a trailer to a concrete mixer.

---

## Equipment List Page (/machines)

### Layout
- MudDataGrid with server-side pagination, sorting, search
- Columns: Name, Type, Registration/ID, Status, Next Inspection, Current Project
- Status: Aktiv (green), Vedlikehold (amber), Ute av drift (red)
- Inspection due within 30 days: amber warning badge
- Inspection overdue: red badge
- EU-kontroll due within 30 days: amber badge (for registered vehicles)
- Click row → /machines/{id}
- "Nytt utstyr" button (admin only)

### Filters
- Type (tenant-defined categories)
- Status (active, maintenance, out of service)
- Inspection status (all, due soon, overdue)
- Current project assignment
- Has registration number (vehicles vs non-registered equipment)

---

## Equipment Detail Page (/machines/{id})

### Header
- Name, type, status badge
- Photo of the equipment
- Registration number (if applicable)
- Current project assignment (if any)

### Tabs

**Oversikt (Overview)**
- Name (e.g., "Gravemaskin CAT 320", "Hilti borhammer", "Brenderup tilhenger")
- Type (tenant-defined, autocomplete from previously used types)
- Make / manufacturer
- Model
- Year
- Registration number (for vehicles/trailers with reg plates)
- Serial number
- Internal ID (tenant's own numbering, e.g., "M-003", "V-012")
- Status: Aktiv / Vedlikehold / Ute av drift
- Current project assignment
- Photo(s)
- Notes (free text)

**For registered vehicles (has registration number):**
- EU-kontroll section showing data from Statens vegvesen API
- See EU-kontroll integration below

**Vedlikehold (Maintenance)**
- Maintenance log: chronological list of all maintenance events
- Each entry:
  - Date
  - Type: Planlagt (scheduled) / Uplanlagt (unscheduled/repair)
  - Description of work performed
  - Performed by (person or workshop name)
  - Cost (optional — can be added later)
  - Attachments (receipts, photos, reports)
  - Hours/km at time of maintenance (optional, for usage-based scheduling)
- "Ny vedlikeholdspost" button → add maintenance record

**Maintenance Reminders:**
- Time-based: "Service every 12 months" → system tracks and reminds
- Usage-based: "Service every 500 hours" → requires manual hours logging
  or odometer reading to trigger
- Admin sets up reminders per equipment item
- Reminders appear in Forefallende when due
- Postponeable from Forefallende

**Kontroller og sertifikater (Inspections & Certifications)**
- Equipment inspections and certifications
  (e.g., annual crane inspection, lift certification, pressure vessel test)
- Same pattern as employee certifications:
  - Upload document (photo/PDF)
  - OCR extracts next inspection date
  - Expiry notifications: 90/30/0 days
- History of all inspections
- For registered vehicles: EU-kontroll shown here automatically from API

**Prosjekter (Projects)**
- Which projects this equipment is/has been assigned to
- Assignment history with dates
- Hours of usage per project (if tracked)

**Dokumenter (Documents)**
- Manuals, registration papers (vognkort), insurance documents
- Upload and organize by category
- For vehicles: registration certificate auto-fetched from Vegvesen API

---

## Equipment Types (Tenant-Defined)

### No Predefined System List
Equipment types vary too much between industries. The system uses autocomplete
from previously used types within the tenant.

### Examples That Will Emerge Per Industry

**Construction:**
- Gravemaskin (Excavator)
- Hjullaster (Wheel loader)
- Kran (Crane)
- Lift (Lift/cherry picker)
- Dumper
- Kompressor (Compressor)
- Generator
- Stillas (Scaffolding sets)
- Betongblander (Concrete mixer)
- Vibrator
- Borerigg

**Farming:**
- Traktor (Tractor)
- Plog (Plow)
- Såmaskin (Seeder)
- Sprøyte (Sprayer)
- Rundballepresse (Baler)
- Fôrutlegger (Feed mixer)

**Tools:**
- Borhammer (Rotary hammer)
- Vinkelsliper (Angle grinder)
- Sveiseapparat (Welding machine)
- Laser/nivellering (Laser level)
- Motorsag (Chainsaw)

**Vehicles & trailers:**
- Varebil (Van)
- Lastebil (Truck)
- Tilhenger (Trailer)
- Pickup

---

## EU-kontroll Integration (Statens vegvesen API)

### What It Does
When admin enters a Norwegian registration number on a vehicle or trailer,
Solodoc automatically fetches vehicle data and EU-kontroll deadlines from
Statens vegvesen's public API.

### API Details
- Endpoint: Statens vegvesen kjøretøyopplysninger API
- Authentication: API key (ordered via vegvesen.no, requires BankID)
- Lookup by: registration number (kjennemerke) or chassis number (understellsnummer)
- Returns: technical data, last EU-kontroll date, **next EU-kontroll deadline**,
  registration dates, vehicle specifications

### Flow
1. Admin creates new equipment with type "vehicle" or "trailer"
2. Enters registration number (e.g., "AB 12345")
3. System calls Vegvesen API
4. Auto-populates:
   - Make and model
   - Year (first registration)
   - Vehicle type
   - Next EU-kontroll deadline
   - Technical specifications (weight, etc.)
5. Admin confirms and saves
6. EU-kontroll deadline feeds into the notification system

### EU-kontroll Notifications
Same three-tier system as certifications:
- 90 days before: info notification to admin
- 30 days before: warning notification to admin
- Overdue: alert notification
- Appears in Forefallende as a deadline item

### Periodic Refresh
- Background job (Quartz.NET, monthly) re-checks all registered vehicles
  against the Vegvesen API
- If the EU-kontroll has been completed (new date returned), the system
  updates automatically — no manual action needed
- If a vehicle fails EU-kontroll or is deregistered, admin is notified

### Non-Registered Equipment
Equipment without a registration number (tools, scaffolding, generators, etc.)
does not use the Vegvesen API. Inspection dates are tracked manually via the
inspection/certification upload with OCR.

---

## Current Location / Project Assignment

### How It Works
- Each piece of equipment can be assigned to a project (or "Ikke tildelt")
- Assignment is manual: admin or project-leader assigns from the equipment
  detail page or from the project's equipment tab
- When assigned, the equipment shows under the project's "Maskiner" tab
- When reassigned to a different project, history is preserved:
  "Gravemaskin CAT 320: Nybygg Sentrum (01.01–15.03) → Bru Finse (16.03–)"

### Equipment Overview for Admin
- Dashboard widget or list view showing all equipment and current assignment
- Quickly see: what's assigned where, what's available, what's in maintenance
- Useful for resource planning across projects

---

## Adding Equipment

### Flow
1. Name (required)
2. Type (autocomplete from tenant's previously used types)
3. Has registration number? (toggle)
   - If yes: enter reg number → Vegvesen API lookup → auto-populate details
   - If no: enter details manually (make, model, year, serial number)
4. Internal ID (optional — tenant's own numbering)
5. Photo(s) (optional)
6. Current project assignment (optional)
7. Save

### Bulk Import
- Excel import for companies with many pieces of equipment
- Uses the standard import architecture
- Columns: name, type, registration number, serial number, internal ID
- Registration numbers trigger Vegvesen API lookup during import

---

## Maintenance Scheduling

### Time-Based Reminders
- "Service every X months" — admin sets interval per equipment item
- System calculates next due date from last maintenance record
- Reminder appears in Forefallende when due
- Example: "Årsservice gravemaskin — forfaller 15.06.2026"

### Usage-Based Reminders (Optional)
- "Service every X hours" — for equipment with hour meters
- Requires manual logging of current hours (admin or operator enters reading)
- When logged hours approach the threshold → reminder triggered
- Not mandatory — many companies don't track equipment hours

### Reminder Configuration
- Set per equipment item: Innstillinger on the equipment detail page
- Multiple reminders per item possible (service interval + annual inspection + EU-kontroll)
- Each reminder: name, interval (months or hours), notification recipients

---

## Integration with Other Modules

### Time Entries
- When logging hours, worker can optionally select which equipment they used
- "Brukt utstyr" dropdown on time entry (optional field)
- Enables: hours per equipment per project reporting
- Useful for billing machine hours and tracking equipment utilization

### Task Groups
- Task Groups can include equipment types (not specific items)
- When applied to a project, admin assigns specific equipment to fill the slots

### Deviations
- Deviation category "Utstyr" links to the equipment registry
- When reporting an equipment-related deviation, worker can select which
  equipment item it concerns → deviation linked to the equipment record
- Equipment detail page shows related deviations

### Export
- Equipment list export: PDF or Excel, all equipment with status and inspection dates
- Per-equipment report: maintenance history, inspection history, project history
- Useful for audits and insurance documentation
