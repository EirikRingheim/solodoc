# Module Spec: Digital Signature Capture

## Overview
Solodoc uses a practical, field-friendly signature system. Workers draw their signature
once during account setup, and it is automatically applied to documents they sign.
For multi-participant documents like SJA, signatures are applied automatically via
notification with the right to dispute. Guest signatures (non-Solodoc users) are
captured on-screen.

No BankID-level digital signatures are implemented at this stage. If customers
require legally binding electronic signatures in the future, this can be added
as an upgrade path.

---

## Signature Levels

| Level | Method | Used For |
|-------|--------|----------|
| **Stored signature** | Drawn once, applied automatically with tap confirmation | Daily checklists, schemas, deviation close-outs, procedure approvals |
| **Auto-applied with dispute** | Signature applied via notification, participant can deny | SJA, multi-participant documents |
| **Guest signature** | On-screen drawing + typed name, no Solodoc account | External inspectors, byggherre representatives, visitors |

---

## Stored Signature (Primary Method)

### Setup Flow
- Signature is **optional** during account creation
- **Required** before the user can submit their first signed document
- When a user tries to sign a document without a stored signature:
  "Du må legge til din signatur før du kan signere dokumenter"
  → opens the signature capture screen
- Signature capture screen: white canvas, draw with finger
  - "Prøv igjen" button to clear and redraw
  - "Lagre signatur" button to confirm
  - Preview of how it will look on a document

### How It's Used
- When submitting a checklist, schema, or other signable document:
  worker taps "Signer og send"
- Their stored signature image is applied to the document
- System records: user ID, timestamp, GPS coordinates (if available), device info
- The signed document shows: signature image + printed name + date/time

### Updating Signature
- User can update their stored signature anytime in: Min profil → Signatur
- **New signature applies to new documents only**
- All previously signed documents retain the signature that was active at the time
- Signature history is preserved (admin can see previous signatures if needed for audit)

### Technical Storage
- Signature is stored as a compressed PNG image (transparent background)
- Maximum canvas size: 600x200px (optimized for document embedding)
- Stored in MinIO/S3 under the user's profile: `{userId}/signature/current.png`
- Previous signatures archived: `{userId}/signature/{timestamp}.png`
- Signature image is embedded directly in generated PDFs (not linked)

---

## Auto-Applied Signature with Dispute (SJA & Multi-Participant Documents)

### Flow

**Creating the document:**
1. Project leader (or responsible person) creates an SJA or multi-participant document
2. They add participants by searching/selecting Solodoc users
3. Upon submission, each participant's stored signature is **automatically applied**
4. Document status: "Signert" next to each participant's name
5. Each participant receives a notification:
   "Du er lagt til som deltaker på [Document Type]: [Title]"

**Participant response:**
- **Default: accepted.** If the participant does nothing, their signature stands.
  No timeout — the auto-signature is valid indefinitely.
- **Dispute:** participant can tap "Avvis deltakelse" at any time
  - Their signature is removed from the document
  - A dispute record is created: who disputed, when, reason (optional comment)
  - The document creator receives a notification: "[Name] avviste deltakelse"
  - The document is flagged: shows "Avvist av [Name] [timestamp]" where their
    signature used to be
  - The creator can then take action (add a different person, discuss with the disputer)

**What the document shows:**
```
Deltakere:
  ✅ Ola Nordmann — Signert (automatisk) 15.03.2026 kl. 08:30
  ✅ Kari Hansen — Signert (automatisk) 15.03.2026 kl. 08:30
  ❌ Per Olsen — Avvist 15.03.2026 kl. 14:22 — "Var ikke til stede"
```

### Audit Trail
- Every action is logged with timestamp:
  - Auto-signature applied: [timestamp]
  - Notification sent: [timestamp]
  - Notification read: [timestamp] (if trackable)
  - Dispute submitted: [timestamp] + reason
- This full history is visible to: document creator, project leader, tenant-admin
- Accessible from the document detail page under "Signeringshistorikk"

### Configurable Settings (Admin)
In tenant settings → Signering:
- **Default mode:** "Automatisk signering med mulighet for avvisning" (default)
- **Alternative mode:** "Krever aktiv bekreftelse fra alle deltakere"
  - When enabled: signatures are NOT auto-applied
  - Each participant must actively tap "Bekreft og signer" in response to notification
  - Document shows "Venter på signatur" until confirmed
  - Reminder notifications sent at admin-configurable intervals (e.g., every 24 hours)
- Admin can set this globally or per document type (e.g., SJA requires active
  confirmation but checklists use auto-apply)

---

## Guest Signature (Non-Solodoc Users)

### When It's Used
An external person needs to sign a document but doesn't have a Solodoc account:
- Byggherre inspector signing off on a completed milestone
- Client confirming work completion
- External auditor signing an inspection report
- Visitor signing a site safety acknowledgment

### Flow
1. The Solodoc user opens the document that needs a guest signature
2. Taps "Legg til ekstern signatur"
3. Hands the phone to the guest (or the guest uses their own device via a shared link)
4. Guest enters:
   - Full name (required)
   - Company/organization (optional)
   - Role/title (optional, e.g., "Byggherre-inspektør")
5. Guest draws their signature on the canvas
6. Guest taps "Signer"
7. Signature is applied to the document with:
   - Drawn signature image
   - Typed name and details
   - Timestamp
   - GPS coordinates
   - Marked as "Ekstern signatur" (visually distinct from employee signatures)

### Guest Signature via Shared Link
For situations where the guest should sign on their own device:
1. Solodoc user taps "Send signeringslenke"
2. Enters the guest's email or phone number
3. Guest receives a link → opens a minimal signing page (no login required)
4. Guest enters name, draws signature, submits
5. Signature is attached to the document
6. Link expires after 7 days or after use (whichever comes first)

### Limitations
- Guest signatures have no user ID authentication — they are documentation-grade,
  not legally binding digital signatures
- They are clearly marked as "Ekstern signatur (uverifisert)" on the document
- Sufficient for construction site documentation and practical quality management
- If legal-grade external signatures are needed in the future, integrate with
  a signing service (e.g., Signicat, BankID Signing)

---

## Signature on PDF Documents

### How Signatures Appear on Exported PDFs
When a signed document is exported as PDF:
- Each signature is rendered as the drawn image
- Below the signature: printed name, date, time
- For auto-applied signatures: "(Digitalt signert via Solodoc)"
- For disputed signatures: "AVVIST — [Name] [Date] [Reason]"
- For guest signatures: "(Ekstern signatur)" + name, company, role
- Signature images are embedded (not linked) so the PDF is self-contained

### Signature Placement
- Checklists/schemas: signature block at the bottom of the document
- SJA: participant signatures in a dedicated "Deltakere" section
- Procedures: sign-off block where configured by the template
- All signature areas include a thin line and label showing who signed

---

## Security & Integrity

### Tamper Detection
- Each signed document has a hash (SHA-256) computed at the time of submission
- The hash covers: all field values, signature images, timestamps, GPS data
- If any data is modified after submission, the hash won't match
- Reopened/edited documents get a new hash — the original hash is preserved
  with the original snapshot
- Hash is displayed on the PDF footer (for verification)

### What's Logged Per Signature Event
- User ID (or guest name for external signatures)
- Timestamp (UTC)
- GPS coordinates (if available)
- Device info (browser/OS, for forensic purposes)
- Document ID and version
- Action: signed / auto-applied / disputed / guest-signed
- IP address (stored but not displayed — for security audit only)

### Data Access
- A user can see all documents they've signed: Min profil → Mine signaturer
- Admin can see all signatures across the tenant
- Signature images are not publicly accessible — served via presigned URLs with expiry
