# Module Spec: Authentication & Onboarding

## Overview
Solodoc uses a split identity model: a **person** exists independently of any company,
and can be a **member** of one or more **tenants** (companies). This enables certifications
and profile data to follow the person across employers, and allows subcontractors to
participate in projects without full tenant memberships.

---

## Identity Hierarchy

```
Person (global Solodoc account)
  → owns: profile, CV, certifications, emergency contacts
  → can be: tenant member (admin / project-leader / field-worker)
  → can be: subcontractor on specific project(s)
```

- A Person is identified by their email address (unique, one account per email)
- A Person can exist without belonging to any tenant (profile-only state)
- Tenant membership is separate from the person — roles are per-tenant
- Subcontractor access is per-project, not per-tenant

---

## Tenant Registration (BankID Flow)

### Who Can Register
Only persons with one of the following roles in Brønnøysundregistrene can register a company:
- Daglig leder
- Styreleder / styremedlem
- Prokurist

### Flow

1. **Landing page** — visitor clicks "Opprett bedriftskonto"
2. **BankID authentication** — redirected to BankID login (via ID-porten / Signicat)
3. **Company selection** — system calls BRREG API (`data.brreg.no/enhetsregisteret`)
   to fetch all companies where this person has an eligible role.
   Display: company name, org number, address, business type (AS, ENK, etc.)
4. **Select company** — person selects which company to register.
   Even if there's only one (ENK), they must explicitly confirm the selection
   to avoid doubt about which entity they're setting up.
5. **Tenant created** — pre-populated from BRREG data:
   - Company name
   - Org number
   - Business address
   - Business type (AS, ENK, ANS, etc.)
6. **Personal account setup** — the registering person creates their Solodoc credentials:
   - Email address
   - Password (requirements below)
   - They become the first `tenant-admin` of this tenant
7. **Welcome / setup wizard** — guided first steps:
   - Upload company logo (used on QR codes, reports, dashboard)
   - Invite first employees
   - Browse template library
   - (Can be skipped and completed later)

### BankID Technical Integration
- Use ID-porten or Signicat as BankID broker
- BankID is used ONLY for initial tenant registration and ownership transfer
- BankID is NOT used for daily login (too much friction)
- Store a record that BankID verification occurred: person ID (fødselsnummer hash),
  timestamp, which company was verified. Never store the raw fødselsnummer.

### Ownership Transfer
If the original business owner leaves the company, a new person with an eligible
BRREG role must complete a BankID session to claim tenant-admin ownership.
Flow: new person logs in with BankID → system verifies their BRREG role for
that org number → ownership transfers → old owner is downgraded to regular member
(or removed, at new admin's discretion).

### Solodoc Super-Admin Powers
- Can freeze/unfreeze any tenant
- Can trigger a re-verification requirement on a tenant
- Can view tenant metadata (not tenant data) for support purposes
- Cannot access tenant business data without explicit consent

---

## User Account Creation (No BankID)

### Self-Registration (Before Being Invited)
A person can create a Solodoc account at any time without being tied to a company.

1. Go to solodoc.no → "Opprett brukerkonto"
2. Enter: email, password, full name
3. Email verification (click link in email)
4. Account created — they land in profile-only state
5. They can immediately: fill in CV, upload certifications, add emergency contacts
6. When later invited to a company, their existing profile and certs are already there

### Invited Registration (Most Common Path)
1. Admin sends invitation from Solodoc (enters employee's email)
2. Employee receives email with invitation link
3. If they already have a Solodoc account → link connects them to the tenant
4. If they don't → link takes them to account creation, then connects to tenant
5. Either way, they're now a member of the tenant with their assigned role

---

## Password Policy
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 number
- No special character requirement (reduces friction for field workers)
- Passwords are hashed with ASP.NET Core Identity defaults (PBKDF2)
- Account lockout after 5 failed attempts (15-minute lockout)

---

## Passkey / Biometric Login
- Support WebAuthn / FIDO2 passkeys as an alternative to password
- Users can register Face ID or fingerprint after initial password login
- Passkey login is optional, never forced
- Especially important for field workers with dirty hands / gloves
- PWA supports passkeys on both iOS (Face ID) and Android (fingerprint)
- Passkey does not replace password — password remains as fallback

---

## Daily Login Flow
1. Open app (PWA) → if session token is valid → straight to dashboard
2. If token expired → show login screen (email + password, or passkey)
3. After login → if member of multiple tenants → show tenant selector
4. Tenant selector shows: company logo, company name, user's role in each
5. If member of only one tenant → skip selector, go to dashboard
6. If member of zero tenants (profile-only) → show profile page with
   message: "Du er ikke lagt til i en bedrift ennå"
7. Tenant selection is remembered for next login (can switch later in app)

### Personal Profile Access
Editing personal profile (CV, certifications, contact info) does NOT require
selecting a tenant first. A "Min profil" option is always available regardless
of tenant context. This is the user's own data.

---

## Invitation System

### Sending Invitations
- Any user with `employees.manage` permission can send invitations
- Admin enters: email address, intended role (project-leader, field-worker)
- System sends email via transactional email service (SendGrid / Postmark)

### Email Deliverability
- Use a verified custom domain: `noreply@solodoc.no`
- Set up SPF, DKIM, and DMARC records on the domain
- Use a transactional email provider (not bulk/marketing provider)
- Email subject: clear, human-readable, no spam trigger words
  Example: "Du er invitert til [Bedriftsnavn] på Solodoc"
- Email body: plain text + HTML version, company name prominent,
  clear call-to-action button, unsubscribe link (even though it's transactional)
- Include the inviting person's name: "Ola Nordmann har invitert deg..."

### Invitation Lifecycle
- Invitation is valid for **30 days**
- Admin can resend invitation (resets the 30-day timer)
- Admin can revoke a pending invitation
- If the email is already associated with a Solodoc account, the invitation
  still goes via email (for consent — don't auto-add people to tenants)
- After accepting, the invitation record is marked as accepted with timestamp

---

## Delegation of Admin Rights
- The original tenant-admin (business owner) can promote other users to tenant-admin
- Tenant-admin can: invite/remove users, assign roles, manage tenant settings,
  configure billing, manage templates
- There must always be at least one tenant-admin per tenant
- The last remaining tenant-admin cannot demote themselves

---

## Subcontractor Access (Light Account)

### What It Is
A subcontractor is a person who needs access to a **specific project** within a tenant
they don't belong to. They have a regular Solodoc account but a limited, project-scoped
role in the inviting tenant.

### Default Permissions (Standard — admin can remove but not add beyond this)
- ✅ Worksite check-in / check-out
- ✅ Fill out checklists (assigned to their project)
- ✅ Fill out SJA forms
- ✅ Report deviations
- ✅ View HMS-håndbok for the project
- ✅ View SDS / chemical register for the project
- ⚙️ Hours registration (optional — admin enables per subcontractor)

### What Subcontractors CANNOT Do
- ❌ See other projects in the tenant
- ❌ See employee list or other tenant data
- ❌ See financial data, billing codes, or sensitive internal information
- ❌ Create templates or modify project settings
- ❌ Invite other users

### Invitation Flow
1. Admin or project-leader invites subcontractor to a specific project
2. Subcontractor receives email invitation
3. They create a Solodoc account (or use existing one)
4. They see the project in their dashboard under the inviting company's name

### Access Lifecycle
- Access is active while the project status is active
- When the project is marked as **completed/closed**, subcontractor access
  automatically becomes **read-only** (they can view their own submitted
  documents but cannot create new ones)
- Admin can revoke access at any time (immediate)
- Subcontractor retains their personal Solodoc account and certifications after access ends

---

## Worksite Check-In / Check-Out

### Purpose
Legal requirement under byggherreforskriften: all persons on a construction site
must be registered. Also critical for emergency headcounts.

### Check-In Flow
1. **QR code at site entrance** — a unique QR code per project/worksite,
   printed on a weatherproof sign. QR code has Solodoc logo in the center.
2. Worker scans QR code with phone camera → opens Solodoc PWA
3. If logged in → one-tap "Sjekk inn" confirmation with GPS verification
4. If not logged in → login first, then check-in
5. GPS coordinates are captured and compared to the project's registered location
   (soft check — warn if >500m from site, but don't block)
6. Check-in recorded: person, timestamp, GPS, project

### Check-Out Flow
- Worker taps "Sjekk ut" from dashboard or scans QR again
- If a worker forgets to check out, system auto-checks out at midnight
  with a flag "automatisk utsjekking" so admin can correct

### Who's On Site View
- All workers (employees and subcontractors) can see a list of who is currently
  checked in at their worksite
- Displayed as a simple list: name, role, company (for subcontractors), check-in time
- No sensitive information shown (no phone numbers, no personal data)

### Admin Features
- Admin / project-leader can view check-in history per project (table view)
- Default: table view in Solodoc, no push notifications
- **Optional setting** (in project settings): enable push notification to
  project-leader when someone checks in. Off by default.
- Export check-in log as PDF or Excel (for Arbeidstilsynet inspections)

### QR Code Generation
- Generated per project in Solodoc admin
- Includes Solodoc logo in center (branded QR)
- Downloadable as high-res PNG/PDF for printing
- Contains URL: `https://app.solodoc.no/checkin/{projectSlug}`
- QR code is regenerable if compromised (old one stops working)

### Relation to Time Registration
- Worksite check-in is **separate** from time registration
- Check-in = "I am physically at this site" (safety/legal)
- Clock-in = "I am working on this project now" (hours/billing)
- A worker can be checked in at Site A but clocked to Project B
  (e.g., a foreman overseeing multiple projects)
- Optional tenant setting: "Auto-start clock on check-in" (off by default)

---

## Session Management
- JWT access tokens with short expiry (15 minutes)
- Refresh tokens with longer expiry (7 days), rotation on each use
- Tokens cached in localStorage for quick app startup
- Offline mode: cached token allows identity verification without network
- On reconnect: refresh token is used to get new access token
- If refresh token is also expired: user prompted to re-login
- **Critical rule:** never lose offline work. If re-login is needed,
  all queued offline data (deviations, checklists, time entries, photos)
  remains in IndexedDB and syncs after successful re-login
- Dashboard shows "Sist synkronisert: [timestamp]" indicator
- If unsynced items exist, show count: "3 elementer venter på synkronisering"

---

## Account States

### Person States
| State | Description |
|-------|-------------|
| **Unverified** | Account created, email not yet verified |
| **Active (no tenant)** | Email verified, not a member of any tenant. Can manage profile/certs. |
| **Active (with tenant)** | Member of one or more tenants. Full functionality. |
| **Deactivated** | Self-deactivated or Solodoc super-admin action. Cannot log in. Data preserved. |

### Tenant Membership States
| State | Description |
|-------|-------------|
| **Invited** | Invitation sent, not yet accepted |
| **Active** | Full member with assigned role |
| **Suspended** | Blocked by tenant-admin (e.g., safety violation). Cannot access tenant. |
| **Removed** | No longer a member. Historical data preserved for audit trail. |

### Subcontractor Access States
| State | Description |
|-------|-------------|
| **Invited** | Project invitation sent, not yet accepted |
| **Active** | Can access the specific project with light permissions |
| **Read-only** | Project completed/closed. Can view own submissions only. |
| **Revoked** | Access removed by admin. Can view nothing. |

---

## BRREG API Integration
- Endpoint: `https://data.brreg.no/enhetsregisteret/api/enheter`
- Free, public API, no authentication required
- Fetch company data by org number or by person's role
- Data used: organisasjonsnummer, navn, forretningsadresse, organisasjonsform,
  registreringsdatoEnhetsregisteret
- Note: BRREG does not directly expose "which companies does person X control"
  — the BankID verification confirms identity, then the system checks
  BRREG's rolle endpoint or uses Altinn's authorization API to determine
  which entities the person has rights for
- Cache company data locally, refresh on tenant settings page

---

## Data Ownership Rules
- **Person owns:** their profile, CV, certifications, emergency contacts
- **Tenant sees:** a read-only view of member's certs/profile WHILE they are a member
- **After member leaves:** tenant retains a historical snapshot of the certs
  that were valid during the membership period. This snapshot is frozen —
  it does not update when the person updates their profile.
- **Subcontractor:** tenant sees name, company, and relevant certs only.
  No access to full CV or personal details.
- **Certifications:** the person can delete their own certs from their profile.
  Historical snapshots in former tenants remain (for audit compliance).
