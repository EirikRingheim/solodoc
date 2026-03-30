# Module Spec: Data Migration from Other Platforms

## Overview
Companies switching to Solodoc from SmartDok, Kuba, or other HMS/KS systems will
have years of historical data. Solodoc provides a Migration Wizard and generic import
tools to make the transition as smooth as possible, without requiring direct API
integrations with competitor platforms.

---

## Migration Strategy

### Approach: Generic Import, Not Direct Integration
- SmartDok, Kuba, and others don't offer public migration APIs
- Their export formats change without notice
- Building direct integrations creates maintenance burden with no upside
- Instead: make the generic import pipeline so good that any Excel/CSV/PDF export
  from any system can be mapped and imported

### What Can Be Migrated

| Data Type | Import Method | Priority |
|-----------|--------------|----------|
| Employee list | Excel/CSV import | High — needed immediately |
| Certifications | Bulk PDF/photo upload + OCR | High — safety-critical |
| Chemical register | Excel import + SDS search/download | High — compliance |
| Checklist templates | Manual recreation in template builder | Medium — templates are often improved during migration |
| Historical deviations | Excel/CSV import | Medium — for trend continuity |
| Project list | Excel/CSV import | Medium — for reference |
| Hours history | Excel/CSV import | Medium — for payroll reference |
| Documents & photos | Bulk file upload (ZIP) | Low — most relevant docs can be re-uploaded selectively |
| Procedures | PDF upload or manual recreation | Low — often rewritten during migration |

### What Typically Isn't Worth Migrating
- System settings and configurations (faster to set up fresh)
- User accounts (employees create new Solodoc accounts)
- Audit trails from the old system (start fresh in Solodoc)
- Old completed checklists (archive as PDFs if needed, don't import as structured data)

---

## Migration Wizard

### Flow
A dedicated wizard accessible to tenant-admins: Innstillinger → Datamigrasjon

**Step 1: Welcome & Planning**
- Overview of what can be migrated
- Checklist of what to export from the old system (guidance per platform):
  - "Fra SmartDok: Gå til Administrasjon → Eksporter → velg CSV..."
  - "Fra Kuba: Gå til Innstillinger → Dataeksport..."
  - "Generelt: Eksporter ansattliste, kjemikalieregister, prosjektliste som Excel/CSV"
- We provide platform-specific export guides where possible
  (updated as we learn from customers)

**Step 2: Import Employees**
- Upload Excel/CSV with employee data
- Map columns: name, email, phone, role, start date
- Preview: shows what will be imported, flags issues
- Import creates user invitation emails (employees create their own accounts)
- For certifications: separate bulk upload step (see below)

**Step 3: Import Certifications**
- Bulk upload: ZIP file containing certification photos/PDFs
- File naming convention suggested: "OlaNordmann_Kranfører_2026.pdf"
- System runs OCR on each file to extract expiry dates
- Admin reviews and confirms extracted data per certification
- Matched to employees by name or email from Step 2

**Step 4: Import Chemical Register**
- Upload Excel/CSV with chemical names and suppliers
- System searches SDS Manager database for each chemical
- Matches are auto-linked, mismatches flagged for manual resolution
- SDS PDFs downloaded automatically for matched chemicals
- Unmatched chemicals: admin uploads SDS PDFs manually

**Step 5: Import Projects & Deviations (Optional)**
- Upload Excel/CSV with project list (name, address, client, dates)
- Upload Excel/CSV with deviation history (title, description, status, date, project)
- Imported as historical records (read-only, not editable)
- Marked as "Importert fra [System]" for clarity

**Step 6: Import Hours History (Optional)**
- Upload Excel/CSV with time entries
- Map columns: employee, project, date, hours, overtime
- Imported as historical records for reference
- Not editable in Solodoc (the source system is the record of truth for old data)

**Step 7: Summary & Completion**
- Overview of everything imported: X employees, Y certifications, Z chemicals, etc.
- Flagged issues that need manual attention
- "Migration complete" status stored on the tenant record

### Migration Status Tracking
- Admin can see migration progress: Innstillinger → Datamigrasjon → Status
- Each step shows: completed / in progress / not started / skipped
- Can revisit steps to import additional data later
- Migration is not a one-time event — it can be done incrementally

---

## Platform-Specific Export Guides

### Guidance (Not Integration)
For each known competitor, provide a written guide explaining how to export data:

**SmartDok:**
- How to export employee list as CSV
- How to export project list
- How to download checklist templates
- How to export hours data
- Where to find deviation exports
- How to download documents from Dokumentsenteret

**Kuba HMS:**
- How to export from Kuba's modules
- Where to find data export options
- Known limitations of Kuba's export

**Generic (Any System):**
- What data to export and in what format
- Suggested column headers for Solodoc to auto-map
- How to organize bulk file uploads

These guides are maintained in the help system (tutorial content) and updated
based on customer feedback and competitor changes.

---

## Import Pipeline (Technical)

### Reuses Existing Import Architecture
The migration wizard uses the same import infrastructure defined in the main CLAUDE.md:
- `ExcelImportParser<TRow>` for parsing Excel/CSV files
- `ImportWizard.razor` component (adapted for migration context)
- Same validation, preview, and confirm workflow

### Migration-Specific Additions
- **Column auto-mapping:** system guesses column mappings based on header names
  (e.g., "Navn" → Name, "Epost" → Email, "Telefon" → Phone)
  Common Norwegian and English headers are pre-mapped.
- **Bulk file processor:** accepts ZIP uploads, extracts files, runs OCR pipeline
  on each file, presents results for confirmation
- **Historical data flag:** imported historical records are marked with
  `IsHistoricalImport = true` and `ImportSource = "SmartDok"` (or whatever)
  These records are read-only in Solodoc.

---

## Professional Migration Service

### For Larger Companies
- Solodoc offers a paid migration service for companies with complex data
- Solodoc team handles: data extraction from old system, mapping, cleanup, import
- Includes: a kickoff call, data review, test import, final import, verification
- Priced as a one-time setup fee (separate from subscription)
- This is a revenue opportunity and reduces churn risk from bad migrations
