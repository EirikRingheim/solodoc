# Module Spec: Chemical Register & SDS Management

## Overview
The chemical register (stoffkartotek) is a compliance requirement under
stoffkartotekforskriften. Every company using hazardous chemicals must maintain
a register with safety data sheets accessible to all employees.

Solodoc's approach optimizes for **fast setup, low maintenance, and instant access**
— recognizing that most companies need this to exist and be correct, but don't
interact with it daily.

---

## Design Philosophy
- Setup should be fast: search → select → auto-extract → confirm → done
- Daily use is rare but must be instant when needed
- Compliance documentation must be exportable on demand
- Don't create busywork — no inventory tracking, no quantity management
- AI summarization makes SDS content actually useful for field workers
- GHS pictograms are universal and language-independent (critical for foreign workers)

---

## Adding Chemicals to the Register

### Flow

**Step 1: Search**
- Admin goes to Chemical Register → "Legg til kjemikalie"
- Types product name, supplier name, or both
- System searches SDS Manager's free database (16M+ SDS)
- Results show: product name, supplier, SDS revision date, language

**Step 2: Select**
- Admin selects the correct product from search results
- SDS PDF is downloaded and stored in MinIO/S3 under the tenant's bucket
- If the product isn't found in the database:
  - Option A: upload a PDF manually (drag and drop or file picker)
  - Option B: request the SDS from the supplier via email (template provided)

**Step 3: Auto-Extraction**
System extracts structured data from the SDS PDF using PdfPig + OCR:
- **Section 1:** Product name, supplier, emergency phone number
- **Section 2:** Hazard classification, GHS pictograms, signal word, H-statements, P-statements
- **Section 4:** First aid measures (per exposure route: inhalation, skin, eyes, ingestion)
- **Section 7:** Handling and storage precautions
- **Section 8:** PPE requirements (auto-detected):
  - Gloves (type if specified, e.g., "nitrilhansker")
  - Eye protection (safety goggles, face shield)
  - Respiratory protection (type if specified)
  - Protective clothing
- **Section 15:** Regulatory information

**Step 4: AI Summary Generation**
- System generates a plain-language summary of the SDS
- Written for field workers, not chemists
- Covers: what it is, main dangers, required PPE, what to do if exposed, storage rules
- Example:
  > "Forskalingsolje brukt til behandling av forskaling før betongstøp.
  > Bruk nitrilhansker og vernebriller. Unngå innånding av damp — bruk
  > i godt ventilerte områder. Ved hudkontakt, vask med såpe og vann.
  > Brannfarlig — hold unna varmekilder og åpen flamme."
- Summary is stored as text alongside the SDS record
- Admin can edit the summary before saving (in case extraction missed nuances)

**Step 5: Confirm and Save**
- Admin reviews: extracted data, pictograms, PPE, AI summary
- Corrects anything the extraction got wrong
- Adds usage metadata:
  - Usage frequency: Daglig / Ukentlig / Månedlig / Sjelden
  - Environment: Innendørs / Utendørs / Begge
  - Typical use: free text (e.g., "Brukes ved forskalingarbeid")
- Saves → chemical is now in the register

### Manual Upload (Fallback)
For chemicals not found in SDS Manager's database:
- Admin uploads a PDF (or photo of a physical SDS)
- Same auto-extraction pipeline runs on the uploaded file
- OCR handles scanned documents and photos
- Admin confirms extracted data
- The chemical is added with a flag: "Manuelt opplastet SDS"

---

## Chemical Register Data Model

### Per Chemical Entry
| Field | Source |
|-------|--------|
| Product name | Extracted from SDS Section 1 |
| Supplier / manufacturer | Extracted from SDS Section 1 |
| Emergency phone number | Extracted from SDS Section 1 |
| Barcode (EAN/GTIN) | Entered by admin or scanned |
| GHS pictograms | Extracted from SDS Section 2 (stored as enum flags) |
| Signal word | Extracted (Fare / Advarsel) |
| H-statements (hazard) | Extracted from SDS Section 2 |
| P-statements (precautionary) | Extracted from SDS Section 2 |
| PPE requirements | Auto-extracted from SDS Section 8 |
| First aid measures | Extracted from SDS Section 4 |
| Handling precautions | Extracted from SDS Section 7 |
| AI summary | Generated, editable by admin |
| Usage frequency | Admin input (Daglig/Ukentlig/Månedlig/Sjelden) |
| Usage environment | Admin input (Innendørs/Utendørs/Begge) |
| Typical use description | Admin input (free text) |
| SDS PDF | Stored in MinIO/S3 |
| SDS revision date | Extracted from SDS |
| Date added to register | Auto-generated |
| Added by | User ID |
| Status | Active / Archived |

---

## Barcode Scanning

### How It Works
1. Worker opens Chemical Register on their phone
2. Taps the barcode scan button (camera icon)
3. Points camera at the product's existing barcode (EAN/GTIN)
4. System looks up the barcode in the tenant's chemical register

### Results
- **Found:** opens the chemical's info page with summary, pictograms, PPE
- **Not found:** 
  - Worker sees: "Dette produktet er ikke registrert i stoffkartoteket"
  - A notification is sent to tenant-admin: "Ukjent kjemikalie skannet: [barcode]
    av [worker name] på [project/location] [timestamp]"
  - This helps admins discover unregistered chemicals being used on site

### Barcode Registration
- When adding a chemical, admin can scan or type the barcode
- One chemical can have multiple barcodes (different container sizes)
- Barcode field is optional (some industrial chemicals don't have standard barcodes)

---

## Worker View (Field Access)

### Chemical Info Page
When a worker looks up a chemical (via search, browse, or barcode scan):

**Top section (always visible):**
- Product name and supplier
- GHS pictograms displayed large and prominent (language-independent)
- Signal word (Fare / Advarsel) with appropriate color (red / orange)
- PPE icons (auto-generated from extracted data):
  gloves, goggles, respirator, clothing — shown as clear icons

**AI Summary section:**
- Plain-language summary (the generated text)
- Readable in 10 seconds — the most important safety info

**Expandable sections (tap to open):**
- First aid measures (by exposure route)
- Handling and storage
- Full H-statements and P-statements
- "Åpne full SDS" → opens the complete PDF (requires connectivity)

### Offline Availability
- Chemical list, AI summaries, GHS pictograms, PPE data, and first aid
  measures are cached locally for offline access
- Full SDS PDFs are NOT cached offline (too much storage)
- Barcode lookup works offline against the cached register
- When offline, "Åpne full SDS" shows: "Krever internettilkobling"

---

## SDS Revision Tracking

### Automatic Check (Background Job)
- Weekly scheduled job (Quartz.NET) checks for updated SDS versions
- For each chemical in the register, queries SDS Manager's database
  for the same product
- If a newer revision date is found → creates a notification:
  "Nytt sikkerhetsdatablad tilgjengelig for [Product Name]"
- Notification goes to tenant-admin
- Admin can: preview the new SDS, update the register entry, or dismiss

### Update Flow
1. Admin receives notification about updated SDS
2. Opens the chemical → sees "Ny versjon tilgjengelig" banner
3. Taps "Oppdater" → new PDF is downloaded
4. Auto-extraction runs on the new PDF
5. Admin reviews changes (old vs new data shown side-by-side)
6. Confirms → register updated, AI summary regenerated
7. Old SDS PDF is archived (kept for audit trail, not deleted)

### Revision History
- Each chemical maintains a history of SDS versions
- Accessible via "Versjonshistorikk" on the chemical detail page
- Shows: revision date, when it was added to the register, by whom

---

## GHS Pictograms

### Stored and Displayed
The system stores GHS pictogram codes as enum flags on each chemical.
Pictograms are rendered as standardized icons (not extracted images from PDFs).

| Code | Pictogram | Meaning |
|------|-----------|---------|
| GHS01 | Exploding bomb | Explosive |
| GHS02 | Flame | Flammable |
| GHS03 | Flame over circle | Oxidizing |
| GHS04 | Gas cylinder | Compressed gas |
| GHS05 | Corrosion | Corrosive |
| GHS06 | Skull and crossbones | Acute toxicity (severe) |
| GHS07 | Exclamation mark | Irritant / harmful |
| GHS08 | Health hazard | Serious health hazard |
| GHS09 | Environment | Hazardous to environment |

### Why This Matters for Multi-Language
GHS pictograms are internationally standardized and understood regardless
of language. For Polish, Lithuanian, or Romanian workers who can't read
Norwegian, the pictograms + PPE icons provide essential safety information
without translation. The AI summary can also be generated in multiple
languages (see Multi-Language module spec).

---

## Project-Level Chemical View

### Linking Chemicals to Projects
- When a Task Group is applied to a project, its chemicals are linked
- Admin/project-leader can also manually add chemicals to a project
- Workers on the project see only chemicals relevant to their project
  (in addition to having access to the full tenant register)

### Project Chemical List
- Shown on the project page under a "Kjemikalier" tab
- Lists all chemicals used on this project
- Quick access to pictograms, PPE, and AI summary per chemical
- Exportable as part of the project documentation package

---

## Compliance Export

### For Arbeidstilsynet Inspections
- Export full chemical register as PDF or Excel
- PDF version: formatted register with one page per chemical
  (product name, supplier, pictograms, H/P-statements, PPE, usage info)
- Excel version: one row per chemical, all fields as columns
- Includes: SDS revision dates, date added to register
- Filter options: all chemicals, active only, by project, by hazard type

### Register Overview Report
- Summary page: total chemicals, breakdown by hazard type,
  most frequently used, chemicals with outdated SDS
- Suitable for internal HMS review meetings

---

## API Architecture (Future-Proofed)

### Current Approach (No Paid API)
```
Admin searches → SDS Manager free database search (web)
  → Downloads SDS PDF
  → Stores in MinIO
  → PdfPig + OCR extracts structured data
  → AI generates summary
  → Admin confirms
```

### Future Upgrade Path (Paid API)
```
Admin searches → SDS Manager REST API (structured JSON)
  → Receives pre-extracted data for all 16 sections
  → Stores SDS PDF from API
  → AI generates summary from structured data (more accurate)
  → Admin confirms
```

The switch from free-to-paid is a configuration change in `IChemicalDataService`.
The rest of the system (storage, display, offline caching, export) stays the same.
Design the `IChemicalDataService` interface to return the same DTO regardless
of whether the data came from PDF extraction or API response.
