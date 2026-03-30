# Module Spec: Tutorials, Help System & Feedback

## Overview
The help system provides contextual assistance at every level: interactive walkthroughs
for new users, a help button on every page, an AI-powered chatbot for instant answers,
and a feedback/bug reporting mechanism. Tutorials are generated as part of the
development workflow and delivered as living content that updates without redeployment.

---

## Help System Architecture

### Four Layers of Help

| Layer | What | When Used |
|-------|------|-----------|
| **Interactive walkthrough** | Step-by-step overlay guiding users through real UI elements | First-time use of a feature, or triggered manually |
| **Contextual help button** | "?" button on each page opening a quick guide with screenshots | "How does this page work?" moments |
| **AI Chatbot** | Conversational help powered by Anthropic API | "How do I do X?" in natural language |
| **Feedback / bug report** | Widget for reporting issues and suggesting features | Something went wrong or user has an idea |

---

## Interactive Walkthroughs

### How They Work
- Built using a JS walkthrough library (Shepherd.js or Intro.js) via Blazor JS interop
- Highlights actual UI elements on the page with tooltip explanations
- Guides users step-by-step through real workflows
- Each step: highlights an element, shows instruction text, "Neste" / "Hopp over" buttons
- Progress indicator: "Steg 3 av 7"

### When They Trigger
- **Automatic (first time):** when a user accesses a major feature for the first time,
  the walkthrough starts automatically
  - First login → dashboard walkthrough
  - First time opening checklist builder → builder walkthrough
  - First time creating a job → job creation walkthrough
- **Manual:** user can trigger any walkthrough from the help button
- **After updates:** when Solodoc releases a significant feature update,
  the walkthrough for that feature triggers once for all users

### Disable Option
- Users can disable automatic walkthroughs: Min profil → Innstillinger → "Vis veiledninger"
- **Not prominently displayed** — hidden in settings to avoid new users accidentally
  disabling it before they've learned the system
- Even when disabled, walkthroughs are accessible manually via the help button
- Admin can re-enable walkthroughs for specific users (useful for re-training)

### Walkthroughs per Role
Different roles see different walkthroughs:

**Field worker:**
- Dashboard orientation (clock in, quick actions, notifications)
- Filling out a checklist
- Reporting a deviation
- Creating a new job
- Updating certifications in their profile
- Checking in at a worksite

**Project leader:**
- Everything field worker sees, plus:
- Project overview page
- Approving hours
- Assigning checklists to a project
- Viewing project analytics
- Managing subcontractors

**Tenant-admin:**
- Everything project leader sees, plus:
- Creating checklist templates
- Managing employees and permissions
- Setting up the chemical register
- Configuring tenant settings (GPS, languages, notifications)
- Using the export system
- Creating Task Groups

---

## Contextual Help Button

### Placement
- Small "?" icon on every page, positioned consistently (top-right area near the bell)
- Tapping opens a help panel (slide-in from right, like notification drawer)
- Panel shows help content specific to the current page

### Help Content Structure
Each page's help content includes:
- **Title:** "Sjekklister — Hvordan det fungerer"
- **Brief description:** 2-3 sentences explaining the page's purpose
- **Step-by-step guide:** numbered steps with annotated screenshots
- **Tips:** practical advice ("Du kan duplisere en sjekkliste for å bruke den
  på en annen lokasjon")
- **Related help:** links to other relevant guides
- **"Spør chatbot"** button at the bottom for further questions

### Screenshot Annotations
- Screenshots are captured from the actual Solodoc UI during development
- Annotated with numbered circles, arrows, and highlight boxes
- Stored in MinIO/S3 under a system-level help content bucket
- Updated when the UI changes (part of the release process)

---

## AI Chatbot

### What It Is
A conversational help assistant embedded in Solodoc, powered by the Anthropic API.
Users type questions in natural language and get immediate, contextual answers
about how to use Solodoc.

### How It Works
1. User taps the chatbot icon (speech bubble, positioned in bottom-right corner)
2. Chat window opens (overlay on current page)
3. User types a question: "Hvordan eksporterer jeg timer for et prosjekt?"
4. System sends the question to Anthropic API with:
   - The user's question
   - System prompt containing all Solodoc help documentation
   - Context: which page the user is on, their role, their language
5. Claude responds with a helpful, step-by-step answer
6. Response is in the user's selected language (Claude handles all supported languages)

### System Prompt Design
The chatbot's system prompt includes:
- Complete Solodoc feature documentation (all tutorial content)
- User's current role (field-worker, project-leader, admin)
- User's current page/context
- Instructions to:
  - Answer only questions about Solodoc functionality
  - Be concise and practical (steps, not essays)
  - Reference specific pages and buttons by name
  - Suggest the relevant walkthrough if the question is about a basic flow
  - Say "I don't know" rather than guess if unsure
  - Never provide information about other users' data or tenant internals
  - Respond in the user's preferred language

### Chatbot for Different Roles

**Field workers ask:**
- "Hvordan stempler jeg inn?"
- "Hvordan legger jeg til bilde i sjekklisten?"
- "Hvor finner jeg sikkerhetsdatabladet for forskalingsolje?"

**Admins ask:**
- "Hvordan lager jeg en ny sjekklistemal?"
- "Hvordan setter jeg opp GPS-sporing?"
- "Hvordan eksporterer jeg dokumentasjon for et prosjekt?"
- "Hvordan inviterer jeg en underentreprenør?"

**The chatbot adapts its answers based on the user's role.** An admin asking about
templates gets the full builder instructions. A field worker asking about templates
gets instructions on how to fill them out, not how to create them.

### Cost Management
- Short help conversations typically use 500-2000 tokens (very cheap)
- Rate limit: maximum 20 chatbot messages per user per hour (prevent abuse)
- Conversation history is kept for the session only (not stored long-term)
- No personal data is sent to the API beyond the user's role and current page
- Chat history is cleared when the chat window is closed

### Offline Behavior
- Chatbot requires connectivity (API call needed)
- When offline: chatbot icon shows a subtle indicator
- Tapping while offline: "Chatbot krever internettilkobling.
  Bruk hjelpknappen (?) for offline veiledning."
- Contextual help button and walkthroughs work offline (content is cached in PWA)

---

## Tutorial Content Management

### How Tutorials Are Created
Tutorials are created as part of the development workflow:
1. Feature is built and tested
2. Claude Code generates tutorial content: step-by-step text + screenshot annotations
3. Content is reviewed by the Solodoc team
4. Content is published to the help system
5. Feature ships with its tutorial ready

### Content Storage
- Tutorial content is stored as structured markdown in the database
- A simple CMS-like editor in the Solodoc super-admin panel allows
  the Solodoc team to create and update help content
- Content updates are instant — no app redeployment needed
- Content is versioned (can roll back if a tutorial has errors)

### Content Structure (Database)
```
HelpContent:
  - Id (Guid)
  - PageIdentifier (string — e.g., "checklist-builder", "deviation-report")
  - RoleScope (enum — AllRoles, Admin, ProjectLeader, FieldWorker)
  - Title (string)
  - Body (markdown)
  - Screenshots (list of image references in MinIO)
  - WalkthroughSteps (JSON — element selectors + instruction text)
  - SortOrder (int)
  - IsPublished (bool)
  - CreatedAt, UpdatedAt
```

### Multi-Language
- Help content is written in Norwegian by default
- Automatically translated to all supported languages via DeepL
  (same pipeline as other content)
- "Automatisk oversatt" indicator shown on translated help content
- Chatbot responds natively in the user's language (Claude handles this)

---

## Feedback & Bug Report System

### Feedback Widget
- Small floating button in the bottom-left corner (opposite side from chatbot)
- Icon: a speech bubble with a pencil or a flag
- Tapping opens a compact form

### Report Types

**Bug Report (Feilmelding)**
- What happened? (free text, required)
- What did you expect to happen? (free text, optional)
- Severity: "Blokkerer arbeidet mitt" / "Irriterende men fungerer" / "Kosmetisk"
- Screenshot option: capture current screen and attach
- **Auto-attached context (user doesn't see this but it's sent):**
  - Current page URL / route
  - User's role and tenant (anonymized tenant ID, not name)
  - Browser and device info (user agent)
  - Last 5 client-side error log entries from that session
  - App version
  - Timestamp

**Feature Request (Forslag)**
- What would you like? (free text, required)
- Why would this help? (free text, optional)
- Priority: "Viktig for arbeidet mitt" / "Hadde vært fint" / "Bare en idé"

**General Feedback (Tilbakemelding)**
- Free text
- Sentiment: positive / neutral / negative (optional emoji-free buttons)

### Where Feedback Goes
- All feedback is stored in a dedicated `Feedback` table
- Accessible to Solodoc super-admin via an internal dashboard
- Bug reports with "Blokkerer arbeidet mitt" severity trigger an
  immediate notification to the Solodoc support team
- Feature requests are aggregated — popular requests bubble up
- Feedback is never shared with the user's tenant-admin
  (users must feel safe reporting issues)

### User Communication
- On submission: "Takk for tilbakemeldingen! Vi leser alle innmeldinger."
- No automatic response or ticket number (keep it lightweight)
- For critical bugs: Solodoc team can reach out via the user's email if needed

---

## Error Monitoring & System Health

### Client-Side Error Capture
- Blazor error boundary catches all unhandled exceptions
- JavaScript errors captured via `window.onerror` and `unhandledrejection`
- Errors are logged with context: page, user action, component, stack trace
- Sent to the API in batches (not per-error, to avoid flooding)
- Stored in a `ClientError` table with session correlation

### Server-Side Error Capture (Serilog + SEQ)
- All server-side errors logged with full context via Serilog
- SEQ provides real-time search, filtering, and alerting
- Structured properties: TenantId, UserId, RequestId, Endpoint, StatusCode
- Error alerts configured in SEQ for critical failures

### Health Check Job (Quartz.NET)
- Runs every 5 minutes
- Checks:
  - PostgreSQL connectivity and query response time
  - MinIO/S3 availability (can read/write test object)
  - DeepL API reachability
  - Background job queue health (any stuck jobs?)
  - Disk space / storage quotas
- Results logged to SEQ
- If any check fails: alert sent to Solodoc operations team
- Health endpoint: `GET /health` returns system status (for uptime monitoring)

### Bug Report Context Enrichment
When a user submits a bug report, the system automatically attaches:
- Last 5 client-side errors from their current session
- Last failed API request (if any) with status code
- Time since last successful sync
- Current connectivity status (online/offline)
- This context is invisible to the user but invaluable for debugging

---

## Tutorial Content: Standard Set (Created at Launch)

### Field Worker Tutorials
1. Getting started — dashboard orientation
2. Clocking in and out
3. Creating a new job (oppdrag)
4. Filling out a checklist
5. Reporting a deviation
6. Checking in at a worksite
7. Finding chemical safety information
8. Updating your profile and certifications
9. Working offline — what works and what doesn't

### Project Leader Tutorials
1. Managing your projects
2. Approving hours
3. Assigning checklists and managing documents
4. Handling deviations on your project
5. Inviting subcontractors
6. Viewing project analytics
7. Exporting project documentation

### Admin Tutorials
1. First-time setup after registration
2. Inviting employees
3. Creating checklist and SJA templates
4. Setting up the chemical register
5. Configuring tenant settings (GPS, languages, permissions)
6. Creating and using Task Groups
7. Managing certifications and expiry alerts
8. Using reporting and analytics
9. Exporting documentation packages
10. Managing announcements and communication
