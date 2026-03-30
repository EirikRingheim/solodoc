# Module Spec: GDPR & Data Retention

## Overview
Solodoc processes personal data on behalf of tenants (companies). Compliance with
GDPR (via Norwegian personopplysningsloven) and Datatilsynet requirements is
non-negotiable. The system is designed with privacy by design: minimal data collection,
clear consent mechanisms, transparent processing, and robust data subject rights.

---

## GDPR Roles

| Role | Who | Responsibility |
|------|-----|---------------|
| **Behandlingsansvarlig** (Data Controller) | Each tenant (company) | Decides what data to collect, responsible for lawful processing |
| **Databehandler** (Data Processor) | Solodoc | Processes data on behalf of tenants, follows their instructions |
| **Registrert** (Data Subject) | Employees, subcontractors | Have rights under GDPR to access, correct, and delete their data |

### Data Processing Agreement (Databehandleravtale)
- A DPA is required between Solodoc and each tenant
- Presented and accepted during tenant registration (part of the signup flow)
- Covers: what data is processed, purpose, security measures, sub-processors,
  breach notification procedures, data return/deletion on contract termination
- Stored as an accepted document with timestamp and the accepting person's identity
- Updated versions require re-acceptance by tenant-admin

---

## Personal Data Inventory

### Data Collected and Legal Basis

| Data Category | Examples | Sensitivity | Legal Basis | Retention |
|---------------|----------|-------------|-------------|-----------|
| Identity | Name, email, phone | Basic | Contract performance | Account lifetime |
| Authentication | Password hash, passkeys, signature image | Basic | Contract performance | Account lifetime |
| Employment | Tenant membership, role, start/end dates | Basic | Contract performance | 5 years after leaving tenant |
| Certifications | Certificate photos/PDFs, expiry dates | Medium | Legitimate interest (safety compliance) | Account lifetime (person owns) / 5 years snapshot (tenant) |
| Time entries | Clock in/out, hours, project, breaks | Medium | Contract performance (employment) | 5 years after project completion |
| GPS coordinates | Check-in location, clock location, deviation location | Medium | **Consent** (explicit opt-in) | Same as parent entity |
| Photos | Checklist photos, deviation photos, before/after | Medium | Legitimate interest (quality documentation) | Lifetime of project documentation |
| Incident/health data | Injury descriptions, incident reports | **High** | Legal obligation (arbeidsmiljøloven) | 5 years minimum, per regulation |
| Certification ID numbers | Fødselsnummer on uploaded cert photos | **High** | Legitimate interest (verification) | Account lifetime, restricted access |
| Device/browser info | User agent, IP address (in audit logs) | Low | Legitimate interest (security) | 12 months |

---

## GPS Consent Model

### Two-Layer Consent
GPS tracking requires explicit consent from both the tenant and the individual user.

**Layer 1: Tenant-Admin Enables GPS**
- GPS features are **disabled by default** for new tenants
- Admin enables GPS in: Innstillinger → Personvern → GPS-sporing
- When enabling, admin sees an explanation:
  "GPS-posisjon vil bli registrert ved innsjekking, timeregistrering og
  avviksrapportering. Alle ansatte må godta dette individuelt."
- Admin confirms they understand and enables the feature

**Layer 2: Individual User Accepts**
- After admin enables GPS, each user sees a consent prompt on next login:
  "Din arbeidsgiver har aktivert GPS-sporing i Solodoc. Posisjonen din
  registreres ved innsjekking, stempling og avviksrapportering.
  Du kan trekke samtykket tilbake når som helst i innstillingene."
- Two buttons: "Godta" / "Avslå"
- If accepted: GPS is captured on relevant actions
- If declined: GPS fields are left empty, app works normally otherwise.
  No blocking, no reduced functionality (except GPS-dependent features
  like map views of their own check-ins)
- Consent status stored on the user's tenant membership record
- User can withdraw consent anytime in: Min profil → Personvern → GPS

### What Happens When GPS is Declined
- Check-in works but without GPS verification (location field empty)
- Time entries record without coordinates
- Deviation reports have no location attached
- Admin sees "(GPS ikke tilgjengelig)" in relevant fields
- The system never nags or repeatedly asks after the user has declined
- Admin cannot force GPS consent or penalize users for declining

### GPS Data Minimization
- GPS is captured only at the moment of action (check-in, clock-in, deviation report)
- Solodoc does NOT continuously track location
- No background location tracking, no movement trails, no geofencing
- Coordinates are stored on the entity they relate to, not in a separate tracking table
- Accuracy is stored alongside coordinates (so imprecise readings are flagged)

---

## Data Subject Rights Implementation

### Right to Access (Innsyn)
- Every user can view all data Solodoc holds about them:
  Min profil → Personvern → "Mine data"
- Shows: profile info, all certifications, all time entries, all documents they've
  signed or created, GPS data points, audit log entries about them
- "Last ned mine data" button → generates a machine-readable export (JSON + PDF)
  containing all their personal data across all tenants
- Export is generated as a background job, download link sent to their email
- This satisfies the right to data portability as well

### Right to Correction (Retting)
- Users can edit their own profile, certifications, and personal info at any time
- For data they can't edit (e.g., time entries approved by admin), they can
  request correction through their admin
- Admin can edit records and the change is logged in the audit trail

### Right to Deletion (Sletting)
- User can request account deletion: Min profil → Personvern → "Slett min konto"
- Deletion request triggers a confirmation flow:
  1. User confirms they understand this is permanent
  2. If they are a member of any tenant: they must first be removed by their admin
     (or request removal). This prevents orphaned data.
  3. Once removed from all tenants (or never a member): account deletion proceeds

**What Deletion Does:**
- Personal profile data is permanently deleted (name, email, phone, CV, emergency contacts)
- Certification files are permanently deleted from MinIO/S3
- Signature images are permanently deleted
- Stored GPS coordinates on their entities are nullified

**What Deletion Does NOT Do (Anonymization Instead):**
- Completed checklists, deviation reports, SJA forms they participated in
  are NOT deleted (regulatory compliance requires retention)
- Instead, their identity is replaced with "Anonymisert bruker" in all
  historical documents
- The document structure, content, timestamps, and other participants remain intact
- Signature images on historical documents are replaced with "[Signatur fjernet]"
- Audit trail entries referencing them are anonymized: user ID replaced with
  "Anonymisert bruker [hash]" (hash allows correlating actions by the same
  anonymized person without revealing identity)

### Right to Object (Protest)
- Users can object to specific processing (e.g., GPS tracking) via the
  consent mechanisms described above
- Objections are logged and respected immediately
- Admin is notified when a user withdraws GPS consent

---

## Tenant Data Retention

### During Active Membership
- Tenant has full access to employee data as described in the auth spec
  (profile, certifications — read-only view)
- All project data, documents, deviations, checklists are accessible

### After Employee Leaves Tenant
- Tenant retains a **frozen snapshot** of the employee's certifications
  that were valid during their membership period
- Snapshot is read-only and does not update when the person updates their profile
- **Snapshot is automatically deleted 5 years after the employee left the tenant**
- When snapshot is deleted, the employee's name in historical documents
  is replaced with "Tidligere ansatt" (not fully anonymized — the tenant
  knows they had this person, just the snapshot certs are gone)

### After Tenant Cancels Subscription
- Tenant data is retained for 90 days after cancellation (grace period)
- During grace period: data is read-only, admin can export everything
- After 90 days: all tenant data is permanently deleted
- Employee personal accounts are NOT affected — they keep their Solodoc
  profiles and certifications (they own this data)
- Employees see the tenant disappear from their tenant list

### After 5-Year Retention (Automated)
- A scheduled background job (Quartz.NET, monthly) scans for:
  - Employee snapshots older than 5 years → delete and anonymize
  - Time entries on completed projects older than 5 years → anonymize employee
  - Audit log entries older than 5 years → anonymize user references
- Anonymization replaces personal identifiers with "Anonymisert bruker"
- Document content, dates, project info, and structure remain intact
- This runs automatically — no admin action needed
- Admin receives an annual summary: "X poster ble anonymisert i henhold til
  retningslinjer for datalagring"

---

## Data Security Measures

### Encryption
- **In transit:** TLS 1.3 for all API communication
- **At rest:** PostgreSQL with encrypted storage (AES-256)
- **File storage:** MinIO/S3 with server-side encryption
- **Backups:** encrypted, stored in a separate geographic location

### Access Control
- All data access goes through authenticated API endpoints
- Tenant isolation via global query filters (never leak cross-tenant data)
- Role-based access control (RBAC) limits what each user can see
- Certification photos with potential fødselsnummer: restricted to the
  person themselves and tenant-admins only. Never shown in list views,
  search results, or to project-leaders/field-workers.

### Breach Notification
- If a data breach occurs, Solodoc notifies:
  - Datatilsynet within 72 hours (legal requirement)
  - Affected tenants (data controllers) immediately
  - Affected individuals if the breach poses high risk to their rights
- Breach response plan documented and tested annually
- Incident logging in a dedicated security log (separate from business audit trail)

### Sub-Processors
- All third-party services that process personal data are documented:
  - Cloud hosting provider (server location: EU/EEA)
  - MinIO/S3 (file storage — EU/EEA)
  - DeepL (translation — EU-based, GDPR compliant)
  - Anthropic (AI summaries — document their data processing terms)
  - SendGrid/Postmark (email — DPA required)
  - BankID/Signicat (identity verification — Norwegian, GDPR compliant)
- All sub-processors must be EU/EEA based or have adequate GDPR safeguards
- Sub-processor list is maintained and available to tenants on request
- Tenants are notified if sub-processors change (as required by DPA)

---

## Privacy by Design Principles

### Data Minimization
- Collect only what's needed for the stated purpose
- GPS: only at moment of action, not continuous tracking
- Certification photos: stored but fødselsnummer never extracted or indexed
- Audit logs: IP address stored for security only, auto-deleted after 12 months

### Purpose Limitation
- Data collected for quality management is used for quality management
- No selling of data to third parties, ever
- No using employee data for purposes beyond what the DPA specifies
- No profiling or automated decision-making that affects individuals

### Storage Limitation
- 5-year retention then anonymization (automated)
- 90-day grace period after tenant cancellation then deletion
- Temporary files (exports) deleted after 7 days
- Session data cleared on logout

---

## Privacy Policy & User-Facing Documents

### Privacy Policy (Personvernerklæring)
- Accessible from: login screen, app footer, Min profil → Personvern
- Written in plain Norwegian (not legal jargon)
- Covers: what data is collected, why, how long it's kept, who has access,
  user rights, how to exercise rights, contact information
- Also available in English
- Updated when processing changes, users notified of updates

### Cookie/Storage Notice
- Solodoc PWA uses localStorage and IndexedDB (not traditional cookies)
- Minimal notice needed: "Solodoc lagrer data lokalt på enheten din for
  offline-funksjonalitet. Ingen sporingsdata sendes til tredjeparter."
- No complex cookie consent banner needed (no marketing cookies, no third-party tracking)

### Data Processing Agreement (Template)
- Standard DPA template provided to all tenants at signup
- Covers all GDPR Article 28 requirements
- Available for download from admin settings
