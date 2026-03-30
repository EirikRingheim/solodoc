# Module Spec: Full-Text Search

## Overview
Solodoc provides a unified search experience across all entities. Search is available
globally from the dashboard and contextually within projects. Results are grouped by
type with an option to show everything in a flat list.

---

## Technical Approach

### PostgreSQL Full-Text Search (Phase 1)
- Use PostgreSQL `tsvector` columns for full-text indexing
- Norwegian language configuration for stemming ("graving" matches "grave", "gravearbeid")
- `pg_trgm` extension for fuzzy/typo-tolerant matching ("betomg" finds "betong")
- Combined approach: tsvector for relevance-ranked search + trigram for fuzzy fallback
- Search queries use `websearch_to_tsquery` for natural input parsing
- No additional infrastructure — runs on the existing PostgreSQL instance

### Upgrade Path (If Needed Later)
- If search quality or performance becomes a bottleneck at scale,
  migrate to Elasticsearch or Meilisearch
- The search service is abstracted behind `ISearchService` interface
  so the implementation can be swapped without changing the API or UI

### Indexed Entities
| Entity | Indexed Fields |
|--------|---------------|
| Projects | Name, description, address, client name |
| Jobs | Description, customer name, address |
| Deviations | Title, description, corrective actions, comments |
| Checklists (templates) | Name, description, item labels |
| Checklists (instances) | Template name, comments, input values |
| Procedures | Title, all text block content |
| Chemicals | Product name, supplier, CAS number, hazard statements |
| Employees | Name, certifications, competencies |
| Machines | Name, type, registration number, model |
| Contacts | Name, company, email, phone |
| Announcements | Title, body text |

### Index Updates
- Indexes are updated on entity create/update (synchronous, within the same transaction)
- Soft-deleted entities are excluded from search results by default
- Reindexing job available for admin (Quartz.NET scheduled, or manual trigger)

---

## Search UX

### Global Search (Dashboard)
- Search bar on the dashboard, available on all screen sizes
- Mobile: search icon that expands to full-width input on tap
- Desktop: always-visible search bar
- Placeholder text: "Søk i hele systemet..."
- Searches across ALL entities in the current tenant

### Contextual Search (Within a Project)
- When inside a project, the search bar scopes to that project by default
- **Clearly indicated:** the search bar shows the project name as context
  - Example: search bar label reads "Søk i Nybygg Sentrum..." 
  - Visual indicator: project accent color on the search bar border
  - Small tag/badge showing the project name inside or beside the search input
- Results only include entities belonging to that project
- Below results: a clear link "Søk i hele systemet →" to expand to global search
- Switching to global search changes the visual indicator and replaces results

### Search Results Display

**Default: Grouped by type**
```
Avvik (5)
  ● AVV-001 Mangelfull sikring av grøft — Åpen
  ● AVV-003 Feil betongblanding — Lukket
  (Vis alle 5)

Sjekklister (3)
  ● SJL-001 Betongstøp Etasje 1 — Fullført
  ● SJL-002 Betongstøp Etasje 2 — Utkast
  (Vis alle 3)

Kjemikalier (1)
  ● Forskalingsolje Noco Formwork 200
```

- Each category shows up to 3 results with a "Vis alle" link
- Categories are ordered by relevance (most matches first)
- Each result shows: document number/name, status, brief context snippet
  with search term highlighted

**Option: Show everything (flat list)**
- Toggle at the top of results: "Grupper etter type" / "Vis alt"
- Flat list sorted by relevance score
- Each item tagged with its type (small badge: "Avvik", "Sjekkliste", etc.)

### No Results
- "Ingen resultater for '[search term]'"
- If in contextual search: "Prøv å søke i hele systemet →"
- Suggest: "Sjekk stavemåten eller prøv et annet søkeord"

### Recent Searches
- When the search bar is tapped/focused (before typing), show recent searches
- Last 5 searches, stored locally on the device (localStorage)
- Tapping a recent search re-executes it
- "Tøm historikk" link to clear recent searches
- Recent searches are per-user, per-device (not synced across devices)

---

## Search Performance

### Targets
- Results should appear within 300ms for typical queries
- Fuzzy matching (typo tolerance) may add ~100ms — acceptable
- For large tenants (10,000+ entities), ensure tsvector indexes are maintained

### Optimizations
- Debounce search input: 300ms after last keystroke before executing search
- No live/as-you-type search on mobile (too expensive on slow connections)
- Desktop: optional live results after 3+ characters (configurable)
- Search results are paginated (20 per category in grouped view, 50 in flat view)

---

## Permissions & Filtering
- Search respects all existing permission rules
- Field workers only see entities they have access to
- Subcontractors only see results from their assigned project(s)
- Soft-deleted items are excluded (admin can search deleted items separately
  via the "Vis slettede" filter in module list pages)
- Confidential deviations are excluded from non-admin search results
