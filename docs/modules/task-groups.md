# Module Spec: Task Groups

## Overview
A Task Group is a reusable bundle of resources tied to a type of work. Instead of
manually finding and assigning checklists, equipment, safety procedures, and chemicals
every time a project needs concrete foundation work, the admin creates a "Betongstøp
fundamentering" Task Group and applies it with one action.

Task Groups define **what needs to happen** — not who does it. The crew (people)
builds itself automatically from actual work data (check-ins and logged hours).

---

## What a Task Group Contains

| Component | What It Does | Example |
|-----------|-------------|---------|
| **Checklists / Schemas** | Template references — assigned to the project when group is applied | Betongstøp sjekkliste, Forskalingskontroll |
| **Equipment / Machines** | Machine types or specific machines needed | Betongblander, Vibrator, Pumpe |
| **Safety Procedures** | SJA templates and procedure documents relevant to this work | SJA: Betongstøp, Prosedyre: Arbeid med forskalingsolje |
| **Chemicals** | Chemicals likely used, pulled from chemical register | Forskalingsolje, Herdemiddel, Rengjøringsmiddel |
| **Key Roles** | Role placeholders (not specific people) to be filled when applied | Prosjektleder: [velg], HMS-ansvarlig: [velg], Kontaktperson byggherre: [velg] |

### What Task Groups Do NOT Contain
- **Specific workers / crew members** — the actual crew is too fluid in real-world
  construction, farming, and machine operation to pre-assign. Workers rotate,
  availability changes daily, subcontractors come and go. Instead, the crew builds
  itself from check-in and hours data (see Auto-Built Crew below).

---

## Creating a Task Group

### Who Can Create
- Tenant-admin (always)
- Project-leaders if permitted by admin (same permission toggle as templates)

### Builder Flow
1. **Name and description** — "Betongstøp fundamentering", description of when to use it
2. **Add checklists** — browse template library, select which checklist templates to include
3. **Add equipment** — browse machine park or type equipment names
   (can reference specific registered machines or generic equipment types)
4. **Add safety procedures** — browse procedure library, select relevant SJA templates
   and written procedures
5. **Add chemicals** — browse chemical register, select chemicals used for this work type
6. **Add key roles** — define role slots that need to be filled when the group is applied
   (e.g., "Prosjektleder", "Kontaktperson", "HMS-ansvarlig"). These are labels, not
   tied to Solodoc system roles — admin defines whatever roles make sense.
7. **Tags** — auto-generated from name + manual additions (same system as templates)
8. **Save**

### Task Group Settings
- Name (required)
- Description (optional — when should this group be used?)
- Category / tags (auto-generated + manual, for search and filtering)
- Active / Inactive toggle (inactive groups don't appear in the selection list)

---

## Applying a Task Group

### On a Project
1. Admin or project-leader opens a project
2. Navigates to a "Oppgavegrupper" section (or during project creation wizard)
3. Browses / searches available Task Groups
4. Selects one or more groups — **multiple groups can be combined on one project**
5. System pre-populates the project with:
   - Checklist templates → assigned to the project as available checklists
   - Equipment → listed in the project's equipment section
   - Safety procedures → linked in the project's HMS section
   - Chemicals → added to the project's chemical register view
   - Key roles → shown as empty slots to be filled
6. Project leader reviews and adjusts:
   - Remove checklists that aren't relevant for this specific project
   - Add additional checklists not in the group
   - Assign specific machines from the fleet (if group had generic types)
   - Fill in key role slots (select actual people)
7. Everything is now independent of the Task Group — changes to the project
   don't affect the group, and future changes to the group don't affect
   existing projects

### On a Job (Lightweight)
- Task Groups are available on Jobs too, but simplified
- Only checklists are applied from the group (Jobs don't have full equipment
  tracking, chemical registers, or role assignments)
- Use case: a plumber has a "Service VVS" group with a standard service checklist.
  When creating a quick Job, they select the group and the checklist is attached.
- Applying a Task Group to a Job is optional — Jobs can still be created without one

### Combining Multiple Groups
- A project can have multiple Task Groups applied
- When combining, the system merges all components:
  - Duplicate checklists are detected (same template) and only added once
  - Equipment is combined (no duplicate detection — you might need two concrete mixers)
  - Chemicals are merged (duplicates removed)
  - Key roles are merged (duplicate role names combined)
- The project shows which items came from which group (for reference),
  but once applied, everything is editable independently

---

## Auto-Built Crew (Mannskap)

### Philosophy
Instead of trying to pre-assign people to a project (which rarely matches reality),
the project's crew list builds itself automatically from actual work data.

### How It Works
A person is added to the project's crew when they:
- Check in at the project's worksite
- Log hours to the project
- Are invited as a subcontractor on the project
- Are manually added by a project leader (for people who don't use the app)

### What the Crew View Shows
The project page has a "Mannskap" tab showing:

| Name | Rolle | Firma | Timer totalt | Siste aktivitet |
|------|-------|-------|-------------|-----------------|
| Ola Nordmann | Fagarbeider | Fjellbygg AS | 142t | I dag 07:30 |
| Kari Hansen | Bas | Fjellbygg AS | 98t | I går |
| Piotr Kowalski | Betongarbeider | SubCon AS | 64t | 3 dager siden |

- Sortable by any column
- Filterable by: role, company (for subcontractors), active period
- "Active" indicator for workers currently checked in
- Clicking a name shows their profile (certifications, hours breakdown on this project)

### Crew vs Key Roles
- **Key roles** (from Task Group): named positions that need to be filled with a specific
  person. "Prosjektleder: Ola Nordmann", "HMS-ansvarlig: Kari Hansen". These are
  displayed prominently at the top of the project page.
- **Crew** (auto-built): everyone who has worked on the project. This is the full
  personnel list, always up-to-date, not manually maintained.

Both are visible on the project page but serve different purposes.

---

## Task Group Templates from Solodoc (Base Groups)

### Solodoc-Provided Groups
Similar to base templates, Solodoc can ship pre-built Task Groups for common work types:
- Betongstøp (fundamentering, dekke, vegg)
- Grunnarbeider og graving
- Stålmontasje
- Taktekking
- Rørleggerarbeid
- Elektroinstallasjon
- Maskinvedlikehold (periodisk)
- Sprøyting/gjødsling (farming)

These reference Solodoc base checklist templates and common equipment types.

### Using Base Groups
- Tenants can add Solodoc base groups to their library
- Customize them (add/remove components)
- Customized versions are independent (same model as base templates)
- Updates to base groups follow the same notification model as template updates

---

## Search and Organization

### Finding Task Groups
- Searchable by: name, description, tags
- Auto-tagged with keywords from name and component names
- Filterable by: category, active/inactive
- Displayed as cards showing: name, description, component count summary
  (e.g., "3 sjekklister, 2 maskiner, 4 kjemikalier")

### Organizing Groups
- Tags / categories (same system as templates)
- Active / inactive toggle (archive groups you no longer use without deleting)
- Usage statistics: "Brukt i 12 prosjekter" — helps identify valuable vs unused groups

---

## Duplication and Sharing

### Duplicating a Task Group
- "Dupliser" button creates a copy with "(Kopi)" appended to name
- All components are copied (references, not instances)
- Useful for creating variants: "Betongstøp fundamentering" → "Betongstøp dekke"
  where most components are the same but a few checklists differ

### Sharing Between Tenants
- Same model as template sharing: generate a link, recipient imports a copy
- Components that reference tenant-specific items (specific machines, specific chemicals)
  are included as descriptions/types rather than exact references
  (the receiving tenant maps to their own equipment and chemical register)
