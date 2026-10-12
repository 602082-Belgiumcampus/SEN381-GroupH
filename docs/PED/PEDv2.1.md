# CivicConnect Project Engineering Document

**SEN381 · Software Engineering 381 · CivicConnect Project**

## PED v2.1

|||
|-|-|
|**Team**|Group H|
|**Members**|Migal Groenewald · Nuvaran Reddy · Markus Jooste|
|**GitHub repository**|https://github.com/602082-Belgiumcampus/SEN381-GroupH|
|**Document version**|PED v2.1|
|**Date**|12 October 2026|

\---

# 0\. Document Control and Corrections

## 0.1 Version history

|Version|Date|Author(s)|Change summary|Review / approval|
|-|-|-|-|-|
|1.0|Sept 2026|M. Groenewald, N. Reddy, M. Jooste|M1 Engineering Foundation baseline: problem, stakeholders, scope, requirements, acceptance criteria, RTM, risks, Forward Engineering Register, Decision Log.|Approved at M1|
|1.1|30/09/2026|M. Groenewald|M1 baseline review (Section 1.1); clarifications CL-01 to CL-04; change requests CR-001 to CR-003 raised.|Approved at M2|
|1.2|30/09/2026|M. Groenewald, N. Reddy, M. Jooste|ASRs, architecture decision, architecture diagrams, data model, technology stack, design decisions, interface decision.|Approved at M2|
|2.0|30/09/2026|Team|Architecture, Technology \& Initial Design Baseline (BL-M2) as submitted for M2. Retained unchanged in the repository history.|Approved at M2|
|2.1|12/10/2026|Team, AI-assisted drafting (see Section 12, AI-03 and AI-04)|Corrections to PED v2.0 against the M2 brief and Master Project Brief (Section 0.3). Adds CL-05, AS-05 to AS-07, OD-07, R-14 to R-17, ADR-004 to ADR-008, the attribute-level data model, information architecture, and the verification specification.|Pending|

## 0.2 Contributors and M2 responsibilities

|Contributor|M2 responsibility in PED v2.x|
|-|-|
|Migal Groenewald|Architecture and requirements evolution: baseline review, change control, ASRs, architecture, ADR-001 to ADR-003, baseline statement.|
|Nuvaran Reddy|Data, interfaces and traceability: data and persistence model (ADR-005), interface decision (ADR-008), RTM implementation and verification updates, end-to-end trace.|
|Markus Jooste|Technology, design and development: technology stack and deployment compatibility (ADR-004), design-pattern decisions (ADR-006, ADR-007), application development, README and GitHub evidence.|

*All three members remain collectively responsible for the whole document (Master Project Brief, Section 8).*

## 0.3 Corrections log: PED v2.0 to v2.1

PED v2.0 was reviewed against the M2 brief (Sections 5 to 13), the Master Project Brief and the M1 baseline. The findings below were corrected. Original v2.0 content that remains valid has been kept; superseded content is recorded here rather than silently removed (Master Project Brief, Section 6.1).

|ID|M2 criterion|Finding in PED v2.0|Correction in PED v2.1|Where|
|-|-|-|-|-|
|C-01|A, B|ADR-001 to ADR-003 were defined twice with different meanings (architecture ADRs in Section B, technology ADRs in Section D). ADR-004 to ADR-008 were cited by the baseline and RTM but did not exist.|One ADR set, ADR-001 to ADR-008, each in the master-brief decision-record format. Old IDs mapped in Section 0.4.|0.4, 5.4, 6.8, 7.7, 8|
|C-02|A, B|ADR status was inconsistent ("Proposed" in B, "Accepted" in D, none on the FSM decision). Baseline sign-off and CRs were all pending, so BL-M2 was not actually approved.|All ADRs and CRs are shown as Proposed until merged with two non-author approvals. The sign-off table is blank by design and tracked in Appendix A.|1.3, 5.4, 11.4|
|C-03|E|Lifecycle was inconsistent: CL-03 defined Submitted → Accepted/Rejected → Assigned…, but the Section E tests used "Submitted → Assigned" and the Rejected state vanished.|One authoritative state model with transitions T1 to T6, guards and roles. Test specifications corrected. Reopen stays out until OD-05 is decided.|8.1, 10.5|
|C-04|C|The data model was a prose entity list with no attributes, keys or ERD. It disagreed with the RTM and CRs (no logged-by, contact channel or target period; "Assignment" and "RequestHistory" vs "assignee FK" and "AuditEntry").|Attribute-level model with ERD, constraints and indexes. Assignment history lives in AuditEntry. CR-001/002 fields added. Optimistic-concurrency column defined.|6|
|C-05|B, C|Figure 2 said each module accesses only its own tables, yet Reporting and Request Queries read request data.|Rule restated as write-ownership with read-only access for query modules. Ownership matrix added.|5.3, 6.3|
|C-06|C, E|Section C ordered the operation "Validate → Authorise"; Section E ordered it "Authorise → Validate".|Authorise first, so an unauthorised caller learns nothing about a request or its state.|6.4, 8.1|
|C-07|D|Stack choice rested on one-line reasons. No versions for PostgreSQL, EF Core provider or test tools, no cost or learning-curve analysis, no weighted comparison, hosting not analysed.|Verified versions and support dates, weighted decision matrix with sensitivity check, cost/licence and learning-curve analysis, deployment-compatibility analysis (M2 brief, Section 5.8).|7|
|C-08|E|"Design Problem 1" appeared twice with different subjects (authorisation, lifecycle). The authorisation problem had no ADR or research reference.|Two genuine design problems, each with problem → alternatives → decision → trade-off → ADR → application translation: lifecycle (ADR-006) and authorisation (ADR-007).|8.1, 8.2|
|C-09|E|No initial interface decision although the baseline cited ADR-008.|ADR-008: in-process module contracts, server-rendered UI, no network API yet, with validation, error behaviour and security boundary.|8.3|
|C-10|Master 20.2|No information architecture, wireframes or usability/accessibility rationale.|Role-based page map, two wireframes, accessibility design intent.|9|
|C-11|F|No development evidence. Every RTM row was "Planned"; the end-to-end trace ended in placeholders; no README, repo structure, tests or PR evidence.|FR-011 end-to-end trace and 30 specified test cases added (Sections 10.4 and 10.5). Repository structure, README and GitHub governance evidence are not part of this revision; they will be added in PED v3.0 once the repository evidence exists.|10, App. A|
|C-12|Master 10|AI Usage Register held only two format/grammar entries and a date that did not match v1.1.|Register corrected; this revision's AI use is recorded with verification still to be completed by the team.|12|
|C-13|All|Sections C to E cited "Assignment 2", PostgreSQL and Microsoft documentation without in-text citations or a reference list (references covered Sections A and B only).|In-text citations throughout and one reference list. A2 section references are for the team to fill.|13|
|C-14|A|The document had no title/version block and inconsistent section lettering ("Technology" had no letter).|One numbered structure (Sections 0 to 13, Appendix A). v2.0 sections A to E map to Sections 1 to 8 as shown in 0.4.|0, 0.4|
|C-15|A|Mandatory request fields were never defined (FR-001, AC-001, NFR-011 untestable). The risk register listed only some M1 risks. Overdue edge cases (Rejected requests, time basis) were undefined.|CL-05 (mandatory fields), AS-05 to AS-07, OD-07, a full risk register with all required fields, and a tightened overdue definition in CR-001.|1.2, 1.3, 1.4, 3|

## 0.4 Mapping from v2.0 to v2.1 (preserved history)

|Item in PED v2.0|Now|Note|
|-|-|-|
|Section A (continuity, CL, CR, assumptions)|Section 1|Extended; CRs use the Appendix E template fields.|
|A.6 / A.7 / A.8 (risks, forward engineering, decision log)|Sections 2 and 3|Risk register completed with all M1 risks.|
|A.9 / Table A.9 (RTM)|Section 4|Columns kept; verification specifications added.|
|Section B (ASRs, architecture, ADR-001 to ADR-003, BL-M2)|Sections 5 and 11|ADR IDs unchanged.|
|Section C (data / persistence)|Section 6|Rewritten with attribute-level model.|
|Section D "Technology": ADR-001 "Select ASP.NET Core and .NET 10"|ADR-004|Renumbered to remove the clash with ADR-001.|
|Section D: ADR-002 "PostgreSQL with EF Core"|ADR-004 (selection) and ADR-005 (persistence design)|Split into stack choice and persistence design.|
|Section D: ADR-003 "Policy-Based Authorization"|ADR-007|Renumbered; ADR-002 keeps the architecture-level decision.|
|Section D "Design Problem 1: role-based access is not enough"|Section 8.2 and ADR-007|Merged with the authorisation design decision.|
|Section E (lifecycle) and unnumbered FSM ADR|Section 8.1 and ADR-006|Numbered; tests corrected.|
|Section D "Design Problem 2: controlling the lifecycle"|Section 8.1|Duplicate of Section E; merged.|
|ADR-008 (cited, missing)|Section 8.3|New.|

\---

# 1\. PED Continuity, Requirements Evolution and Controlled M1 Changes

This section records how the approved M1 baseline (PED v1.0) was reviewed and carried forward. M1 content is retained. Where M2 evidence justified a change, the change goes through change control (NFR-013) and the original wording is preserved in the M1 section of the controlled repository.

## 1.1 Review of the M1 baseline

Every M1 requirement, acceptance criterion, scope statement, constraint and forward-engineering item was checked against three questions: is it still correct, is it precise enough to design and test against, and is anything in the approved scope missing a requirement? Most of the baseline carried forward unchanged.

|M1 item|Finding|Outcome|Reference|Effect|
|-|-|-|-|-|
|FR-001 to FR-013, FR-016, FR-018, FR-019|Correct and testable.|Unchanged|—|None|
|FR-001, AC-001, AC-002, NFR-011|"All required information" is never defined, so mandatory-field validation cannot be designed or tested.|Clarified|CL-05|Defines the mandatory fields.|
|Scope 4.4|Scope says staff can log a request on someone's behalf, but no requirement covers it.|Gap: new requirement|CR-002|FR-020, AC-037|
|Scope 4.1 / ST-6|Administration of users, roles and categories is in scope but has no need or requirement.|Gap: new need and requirements|CR-003|N-A1, FR-021, FR-022, AC-038, AC-039|
|FR-015 / AC-015|"Overdue" is used but never defined, so AC-015 cannot be tested.|Changed|CR-001|Definition added; Category gains a target period.|
|FR-014, FR-017|Wording error: "authorized management for users with" should read "authorised management users with".|Clarified (editorial)|CL-01|None|
|FR-006 / AC-006|"Agreed feedback event" is not listed.|Clarified|CL-02|Events listed.|
|FR-011 / AC-011|The "defined status transitions" are not defined.|Clarified|CL-03|State model owned by design (Section 8.1).|
|NFR-001|Authentication is implied but not stated.|Clarified|CL-04|None|
|NFR-005, NFR-006|"Agreed representative workload" not yet agreed.|Assumption|AS-02|Open decision OD-03|
|NFR-008|"Agreed retention period" not yet agreed with the client.|Assumption; open decision|AS-01, OD-02|Confirm before M3.|
|Decision Log D-003, D-004|The deferred technology and architecture decisions are now made.|Superseded|ADR-004; ADR-001|Section 2.2|

## 1.2 Clarifications (non-material)

Clarifications improve precision without changing what a requirement commits the team to, so they do not need a change request. The original wording stays in the M1 section; the clarified wording applies from v1.1 (CL-05 from v2.1).

|ID|Affects|Clarification|
|-|-|-|
|CL-01|FR-014, FR-017|Read as: "The system must provide authorised management users with …". Editorial correction only.|
|CL-02|FR-006, AC-006|The feedback events are: request accepted, request rejected (with reason), status updated, and request resolved/closed.|
|CL-03|FR-011, AC-011|Lifecycle states: Submitted → Accepted or Rejected; Accepted → Assigned → In Progress → Resolved → Closed. Rejected and Closed are terminal. Whether a Resolved request can be reopened is open (OD-05). The enforced transition table is in Section 8.1 (ADR-006).|
|CL-04|NFR-001|All non-public functions require an authenticated user. Authorisation (what that user may do) is checked separately (ADR-002).|
|CL-05|FR-001, AC-001, AC-002, NFR-011|The mandatory fields of a service request are: **category** (active category from the controlled list), **title** and **description**. Optional: location. System-set fields (reference, requester, created time, contact channel) are not entered by the requester.|

## 1.3 Change requests

These changes alter the approved requirements baseline, so they follow change control (NFR-013) and use the impact-analysis fields of the Master Project Brief, Appendix E. All three are **Proposed**: none may be treated as approved until the PR is merged with two non-author approvals.

|Field|CR-001: define "overdue" (FR-015)|CR-002: add FR-020, staff log on behalf|CR-003: add N-A1, FR-021, FR-022|
|-|-|-|-|
|**Requested by / date**|M. Groenewald, 30/09/2026|M. Groenewald, 30/09/2026|M. Groenewald, 30/09/2026|
|**Requested change**|FR-015 wording unchanged. Add: "A request is overdue when it is open (not Resolved, Closed or Rejected) and the time since it was created exceeds the target resolution period of its category. Target periods are maintained by the administrator." Target periods count calendar days (AS-06).|FR-020 (Must): "The system must allow authorised staff to submit a service request on behalf of a requester, recording the staff member who logged it and the channel of first contact." AC-037: Given an authorised staff user logs a request on behalf of a requester, when it is submitted, then the request is created with a unique reference, is linked to the requester, and records the logging staff member and contact channel.|N-A1 (Administrator): manage users, roles and the category list. FR-021 (Must): create, deactivate and assign roles to user accounts. FR-022 (Must): maintain the controlled category list, including deactivating categories and setting each category's target resolution period. AC-038: a role change takes effect and a deactivated user can no longer sign in. AC-039: a deactivated category disappears for new requests (existing requests keep it) and overdue calculations use a changed period.|
|**Reason / expected value**|AC-015 cannot be verified without a definition (M1 review). Excluding Rejected requests prevents terminal requests being reported as overdue.|This is how the M1 scope meets "no request is lost" without channel integration (M1, Section 4.4). Without FR-020 the scope decision cannot be traced or verified.|FR-002 depends on a controlled category list and FR-018 on roles, but nothing defines how either is maintained.|
|**Requirements affected**|FR-015, AC-015, FR-022|FR-020, AC-037 (new); FR-001, FR-003, FR-018|FR-021, FR-022, AC-038, AC-039 (new); FR-002, FR-018, FR-015|
|**Architecture / design affected**|Reporting calculates overdue; the value is calculated, never stored.|Request Intake gains submitOnBehalf; Identity \& Access gains a staff permission.|New Administration module; Identity \& Access user and role management.|
|**UI / API / data affected**|Category.target\_resolution\_days; overdue flag in reports.|ServiceRequest.logged\_by\_id and contact\_channel; AuditEntry event LoggedOnBehalf.|AppUser, Role, UserRole, Category tables; admin pages.|
|**Security / privacy**|None.|Staff-only permission; negative test required (NFR-004). Open question OD-07: how a requester without an account is represented.|Highest-privilege functions; negative tests mandatory (NFR-004, R-10). Auditing of admin actions is deferred (FE-09).|
|**Quality / testing**|AC-015 becomes testable with boundary tests at the target period (TC-RP-01).|Positive and negative authorisation tests (TC-IN-04); reference-uniqueness test shared with FR-003.|Authorisation tests for each admin operation (TC-AZ-06); deactivated-user sign-in test.|
|**Scope impact**|None. SLA escalation stays out of scope (M1, Section 4.2).|Closes a baseline gap; adds no feature beyond M1, Section 4.4.|Closes a baseline gap. Scope control: seed initial roles and categories by migration and build a minimal admin UI first.|
|**Schedule / resource**|Low (under half a day).|Low to medium.|Medium (CRUD pages). Mitigated by the seed-first approach.|
|**Cost**|None.|None.|None.|
|**Risk impact**|Reduces R-13. Target values need client confirmation.|Touches R-03, R-10; raises OD-07.|Touches R-01, R-10; raises FE-09.|
|**Recommendation**|ACCEPT|ACCEPT (resolve OD-07 first)|ACCEPT with scope control|

## 1.4 Assumptions and dependencies

|ID|Assumption / dependency|Confirm by|Related risk|
|-|-|-|-|
|AS-01|Request and audit history are kept for at least 5 years (NFR-008). Team proposal, pending client confirmation.|Client, before M3 (OD-02)|R-13|
|AS-02|Representative workload for NFR-005/006: up to 50 concurrent users and 10 000 requests per year. Team assumption, not yet agreed.|Client and team, before performance testing (OD-03)|R-13|
|AS-03|Users access CivicConnect through a modern web browser; no native app (M1, Section 4.2). Unchanged from M1.|—|—|
|AS-04|The organisation can obtain hosting within free or low-cost tiers (constraint N-X5, TO-2).|M3 deployment decision (OD-01)|R-08|
|AS-05|Staff visibility (FR-007): a service-staff user sees requests assigned to them plus unassigned Submitted/Accepted requests; supervisors and management see all requests. Management reports do not expose requester personal details.|Client, before FR-007 is implemented|R-03, R-13|
|AS-06|Overdue periods count calendar days from the request's creation time (CR-001), not business days.|Client, with the target values|R-13|
|AS-07|The .NET 10 SDK and PostgreSQL can be installed and run on the BC Desktop environment, or on the members' own machines.||R-14|

\---

# 2\. Forward Engineering Register and Decision Log

## 2.1 Forward Engineering Register

|ID|Concern|M2 status|How it was addressed / what remains|
|-|-|-|-|
|FE-01|Architecture and system boundaries|Addressed|ADR-001: modules with explicit responsibilities and allowed dependencies (Section 5).|
|FE-02|Technology and platform selection|Partly addressed|Stack chosen in ADR-004 (Section 7). Hosting platform deferred (OD-01) until M3 cost and free-tier evidence exists.|
|FE-03|Data model and sensitive information|Addressed at design level|ADR-005 and the attribute-level model (Section 6): write-ownership by module, role-scoped reads, no personal data in reports (AS-05).|
|FE-04|Authentication and authorisation|Addressed at design level|ADR-002 (where authorisation lives) and ADR-007 (policy and resource-based mechanism). Verification is specified (TC-AZ-01 to 06) and runs in M3.|
|FE-05|Testing and verification strategy|Partly addressed|RTM tracks verification per requirement; 30 test cases are specified in Section 10.5. Strategy, tooling and CI gates are decided in M3 (OD-06).|
|FE-06|Deployment and operational readiness|Partly addressed|Deployment direction, environments, configuration, state and networking implications in Section 7.6. Platform, monitoring and rollback are decided in M3 (OD-01).|
|FE-07|Maintainability and future change|Addressed at architecture level|ADR-003 lifecycle events let notifications, SLA escalation and analytics be added without changing lifecycle rules; ADR-006 makes adding a transition a one-row change.|
|FE-08|Future notification channels (new)|Deferred|Would attach to lifecycle events through the Feedback module (OD-04). In-process events are not durable (R-15), which must be revisited first.|
|FE-09|Audit of administrative actions (new)|Open|FR-019 covers request actions and status changes only. User, role and category changes by an administrator are not audited. Needs a client view on admin accountability before a requirement is proposed.|

## 2.2 Decision Log

|ID|Milestone|Decision|Reason / alternatives|Status / link|
|-|-|-|-|-|
|D-001|M1|Keep scope focused on the core service-request lifecycle.|A smaller controlled solution is more achievable than a large unfinished one. Alternative: build all requested future features.|Active|
|D-002|M1|Protect main; require peer review for substantive changes.|Auditable collaborative evidence. Alternative: direct development on main.|Active; GitHub governance evidence to be added in PED v3.0|
|D-003|M1|Defer technology stack selection to M2.|Insufficient evidence in M1.|Superseded by D-008 / ADR-004|
|D-004|M1|Defer architecture selection to M2.|Architecture should respond to baselined requirements and quality drivers.|Superseded by D-007 / ADR-001|
|D-005|M1|Treat security and quality as protected constraints.|Deadline pressure must not remove required security or verification work.|Active|
|D-006|M2|Raise CR-001 to CR-003 from the M1 review instead of editing baselined requirements.|Preserves the baseline and history (NFR-013).|Section 1.3; Proposed|
|D-007|M2|Adopt modular monolith, central authorisation and lifecycle-owned status changes.|ASR-driven. Alternatives: layered monolith, microservices, BaaS (Section 5.2).|ADR-001 to ADR-003; Proposed|
|D-008|M2|Adopt ASP.NET Core / .NET 10, Razor Pages, EF Core, PostgreSQL, xUnit.|Weighted comparison and sensitivity check (Section 7.3).|ADR-004; Proposed|
|D-009|M2|Relational model with application-managed short transactions, DB constraints and optimistic concurrency.|Integrity of the request lifecycle (ASR-02). Alternatives: DB-centric stored procedures, last-write-wins.|ADR-005; Proposed|
|D-010|M2|Model the request lifecycle as a table-driven finite-state machine.|Centralised, testable transitions. Alternatives: State pattern, enum with guards.|ADR-006; Proposed|
|D-011|M2|Use role-based policies for broad capability and resource-based handlers for request access.|Role checks alone are too coarse for ownership rules (CF-1).|ADR-007; Proposed|
|D-012|M2|In-process module contracts and a server-rendered UI; no network API between modules yet.|Proportionate to scale and team. Alternative: separate REST API and SPA.|ADR-008; Proposed|
|D-013|v2.1|Use one ADR numbering (ADR-001 to ADR-008) and record the v2.0 → v2.1 mapping.|Fixes the duplicate IDs found in the v2.0 review.|Section 0.4|
|D-014|v2.1|Keep the current assignee on ServiceRequest and record assignment history in AuditEntry. No separate Assignment table.|Avoids two sources of truth for who handled a request. Alternative: Assignment table with history rows.|ADR-005; Proposed|
|D-015|v2.1|Apply EF Core migrations through an explicit deployment step in non-development environments, not automatically at application start-up.|Avoids concurrent-migration races and unreviewed schema changes in staging or production. Alternative: migrate on start-up.|Section 7.6; Proposed|

\---

# 3\. Risk Register (live; reviewed at every milestone)

M1 risks R-01 to R-08 remain in the register with their M2 status. R-09 to R-17 are new in M2. Probability (P) and impact (I): H = High, M = Medium, L = Low. Priority: H×H = Critical; M×H = High; M×M or L×H = Medium; lower = Low. A risk that materialises becomes an issue and is linked to an action or decision.

|ID|Risk (event)|Cause|P|I|Priority|Mitigation|Contingency|Owner|Status / M2 update|
|-|-|-|-|-|-|-|-|-|-|
|R-01|The total work (original FRs, CR-002 and CR-003, tests, deployment) exceeds what three students can deliver.|Effort underestimated; scope grew in M2; development evidence is behind plan.|H|H|Critical|Deliver Must requirements first; seed roles and categories before building admin UI; track all work as GitHub issues; re-plan at weekly check-ins.|Defer Should items (FR-006, FR-016, NFR-005/006/010) before reducing testing or security (D-005).|Markus|Open. Elevated in v2.1.|
|R-02|The chosen stack proves unfamiliar or hard to deploy.|Learning curve; deployment platform not yet chosen.|M|H|High|Evaluated in ADR-004; bootstrap proof-of-concept; stay on framework defaults and documented patterns.|Reduce stack surface; fall back to simpler hosting.|Markus (from Migal)|Open (reducing).|
|R-03|Sensitive request or requester data is visible to users who should not see it.|Missing or wrong authorisation or ownership checks; personal data in reports.|M|H|High|Central policy (ADR-002, ADR-007); 404 for requests the caller may not see; negative tests TC-AZ-01 to 06; AS-05 limits report content.|Restrict access, investigate exposure, correct the rule before release.|Markus|Open (reducing).|
|R-04|Baselined requirements or scope change without impact analysis.|Client or lecturer requests; gaps found in review.|M|H|High|Change control with Appendix E impact analysis (CR-001 to CR-003).|Reassess schedule and scope; defer the change or remove equivalent lower-priority scope.|Nuvaran|Open.|
|R-05|GitHub history does not show authentic, progressive, individually attributable work.|Late bulk commits; direct pushes to main; approvals without review.|H|H|Critical|Protected main; two non-author approvals; an issue per task; small commits; PR template; review comments that show actual checking.|Correct governance immediately and continue honestly; do not reconstruct history.|Everyone|Open. P raised from M to H in v2.1: PED v2.0 showed no development evidence.|
|R-06|Security or quality work is postponed under deadline pressure.|Schedule pressure.|M|H|High|Treat as fixed constraints (D-005); reduce feature scope instead.|Defer non-essential functionality rather than required security or testing.|Everyone|Open.|
|R-07|AI-generated material (including this PED revision) is accepted without sufficient human verification.|Time pressure; plausible but wrong output.|M|M|Medium|AI Usage Register; every member must be able to explain each section; verify versions and support dates against official sources.|Re-review affected artefacts and replace unsupported material.|Everyone|Open. Version and support-date claims in Section 7 checked against web sources; the rest awaits team review.|
|R-08|Free-tier hosting limits (sleeping apps, expiring or size-capped databases, missing backups) break availability or recovery.|Cost constraint N-X5 and TO-2.|M|M|Medium|Record limits for each candidate in the OD-01 research; require a backup and restore test before production.|Change platform or lower the availability expectation before production.|Nuvaran|Open.|
|R-09|Module boundaries erode: modules write each other's tables, turning the modular monolith into tightly coupled code.|Boundaries are enforced by discipline, not by the network (ADR-001).|M|M|Medium|Write-ownership rule (Section 6.3); code structure mirrors modules; boundary check in PR review; automated architecture test.|Refactor the offending dependency before merge.|Migal|Open.|
|R-10|A new operation is added without an authorisation check.|Developer forgets the policy call.|M|H|High|Single policy component (ADR-002); a negative authorisation test is required for every protected operation (NFR-004); PR template checkbox.|Block the merge; add the check and test.|Markus|Open.|
|R-11|The single application instance and single database are single points of failure.|Proportionate architecture (ASR-04, TO-2).|M|M|Medium|Accepted under TO-2. Backups and a documented, tested recovery procedure before production (NFR-017).|Restore the latest backup; communicate downtime.|Nuvaran|Open.|
|R-12|A status change bypasses the audit record.|Code path updates status outside Request Lifecycle.|L|H|Medium|Only Request Lifecycle changes status; audit written in the same transaction (ADR-003); application database role has no UPDATE or DELETE on AuditEntry; test TC-PE-03.|Investigate and repair the gap; add a regression test.|Migal|Open.|
|R-13|Undefined business rules (overdue, retention, reopening, staff visibility, mandatory fields) cause rework.|Rules not agreed with the client.|M|M|Medium|CR-001; CL-05; AS-01, AS-05, AS-06; confirm OD-02, OD-05, OD-07 with the client.|Implement the safest interpretation and record it as a controlled change.|Nuvaran|Open.|
|R-14|The .NET 10 SDK or PostgreSQL cannot be installed or run on the BC Desktop environment.|Institutional environment is not guaranteed (Master Brief, Section 25).|M|H|High|Check availability now (AS-07); develop on members' own machines or containers if needed; keep setup steps in the README.|Use personal machines for development and a hosted environment for demonstrations.|Markus|Open.|
|R-15|A feedback notice is lost if the process stops after commit and before the event handler runs.|In-process events are not durable (ADR-003).|L|L|Low|Feedback is a Should requirement; notices can be re-derived from AuditEntry; revisit before any external notification (OD-04).|Rebuild missing notices from the audit trail.|Migal|Open (accepted).|
|R-16|A vulnerable or outdated NuGet dependency is shipped.|Third-party packages and transitive dependencies.|M|M|Medium|Pin versions in the repository; enable Dependabot alerts; run a vulnerable-package check in CI in M3.|Upgrade or replace the package; record the finding.|Nuvaran|Open.|
|R-17|PED, RTM, ADRs and code drift apart (as found in the PED v2.0 review).|Several authors, no cross-check.|M|M|Medium|PR template asks which RTM rows and ADRs a change affects; pre-baseline consistency review (Appendix A); one owner per artefact.|Reconcile before the next baseline; log the drift.|Nuvaran|Open. Realised in v2.0; corrected in v2.1.|

\---

# 4\. Evolved Requirements Traceability Matrix (RTM v2.1)

The M1 columns are kept. The M2 brief (Section 4.2) adds ASR link, module, data impact, design/interface decision, technology, implementation evidence, verification evidence and status. M1 columns "Issue/PR" and "Release evidence" remain in the controlled RTM file and are omitted here for width. The controlled RTM file in the repository is the master; this table is its PED v2.1 snapshot.

**Key.** Pri: M = Must, S = Should. **Planned** = no implementation or verification evidence exists yet. **Specified** = a test case is written down (Section 10.5) with its expected result, but it has **not been executed**; it becomes a result only when a run is recorded. Replace "Planned" with the branch, PR, class, endpoint or test name when it exists, and change Status to In Development or Implemented. No row is marked Implemented, because this document contains no execution evidence.

|Req|Need|Pri|AC|ASR|Module|Data / persistence impact|Design / interface decision|Technology|Impl. evidence|Verif. evidence|Status|ADR / CR / risk|
|-|-|-|-|-|-|-|-|-|-|-|-|-|
|FR-001|N-R1|M|AC-001|ASR-02|Request Intake|ServiceRequest created and linked to requester; category, title, description NOT NULL (CL-05).|SubmitRequest contract (ADR-008); server-side validation before save.|ASP.NET Core + EF Core|Planned|Specified: TC-IN-01|Approved (clarified CL-05)|ADR-001, ADR-008; CL-05|
|FR-002|N-R2|M|AC-002|—|Request Intake|category\_id FK, required; only active categories accepted.|Category list supplied by Administration.|ASP.NET Core + EF Core|Planned|Specified: TC-IN-02, TC-IN-03|Approved (clarified CL-05)|ADR-001; CR-003|
|FR-003|N-R1, N-X2|M|AC-003|ASR-02|Request Intake|reference UNIQUE, generated server-side.|Reference shown only after successful commit.|PostgreSQL unique index|Planned|Specified: TC-IN-01|Approved|ADR-005|
|FR-004|N-R3|M|AC-004|ASR-01, 05|Request Queries|Query filtered by requester\_id.|Own-requests query; ownership via resource policy.|EF Core LINQ, paged|Planned|Specified: TC-AZ-01|Approved|ADR-002, ADR-007|
|FR-005|N-R4|M|AC-005|ASR-01, 05|Request Queries|Index on (requester\_id, created\_at).|Paged own-history query.|EF Core LINQ, paged|Planned|Planned|Approved|ADR-007|
|FR-006|N-R5|S|AC-006|ASR-03|Feedback|FeedbackNotice per lifecycle event (CL-02).|After-commit event handler.|In-process events (DI)|Planned|Planned|Approved (clarified CL-02)|ADR-003; R-15|
|FR-007|N-S1|M|AC-007|ASR-01|Request Queries|Role-scoped list (AS-05).|Scoped query plus policy check per call.|EF Core LINQ, paged|Planned|Specified: TC-AZ-05|Approved|ADR-007; R-10; AS-05|
|FR-008|N-S2|M|AC-008|ASR-05|Request Queries|Indexes on status, category\_id, created\_at.|Search, filter and sort parameters validated server-side.|EF Core LINQ, paged|Planned|Planned|Approved|ADR-005|
|FR-009|N-S3|M|AC-009|ASR-01|Request Queries|Detail read; requester personal details staff-only.|Resource check; 404 if not permitted.|EF Core LINQ|Planned|Specified: TC-AZ-01|Approved|ADR-007; R-03|
|FR-010|N-S4|M|AC-010|ASR-02|Request Lifecycle|assigned\_to\_id FK; AuditEntry event Assigned.|Assign operation (T3); emits lifecycle event.|EF Core transaction|Planned|Specified: TC-LC-03|Approved|ADR-003, ADR-006|
|FR-011|N-S5|M|AC-011|ASR-02|Request Lifecycle|status CHECK; version concurrency token; AuditEntry per transition.|All transitions go through one table-driven FSM (CL-03, ADR-006).|C# 14 + EF Core optimistic concurrency|-|Specified: TC-LC-01 to 16, TC-PE-01 to 03|Approved (clarified CL-03)|ADR-003, ADR-005, ADR-006; R-12|
|FR-012|N-S6|M|AC-012|ASR-02|Request Lifecycle|RequestAction rows linked to request and author.|RecordAction operation; emits lifecycle event.|EF Core transaction|Planned|Planned|Approved|ADR-003|
|FR-013|N-S7|M|AC-013|ASR-01, 02|Request Lifecycle|resolution\_note required at Resolve; closed\_at at Close.|Close (T6) requires close permission.|EF Core transaction|Planned|Specified: TC-LC-06, TC-AZ-03|Approved|ADR-002, ADR-006|
|FR-014|N-M1|M|AC-014|ASR-03|Reporting|Read-only aggregates over requests.|Read-only report queries; no writes.|EF Core read-only queries|Planned|Planned|Approved (clarified CL-01)|ADR-001|
|FR-015|N-M2|M|AC-015|ASR-03|Reporting|Category.target\_resolution\_days; overdue calculated, never stored.|Overdue rule in Reporting (CR-001).|EF Core read-only queries|Planned|Specified: TC-RP-01|Changed (CR-001, Proposed)|CR-001; R-13|
|FR-016|N-M3|S|AC-016|ASR-05|Reporting|Uses status and category indexes.|Filter parameters validated.|EF Core read-only queries|Planned|Planned|Approved|ADR-005|
|FR-017|N-M4|M|AC-017|ASR-02, 03|Reporting|Reads AuditEntry (read-only).|Accountability views built on audit data.|EF Core read-only queries|Planned|Planned|Approved (clarified CL-01)|ADR-003|
|FR-018|N-X1|M|AC-018|ASR-01|Identity \& Access|AppUser–Role–UserRole mapping.|Central policy check at every protected operation.|ASP.NET Core Identity + authorisation policies|Planned|Specified: TC-AZ-01 to 06|Approved|ADR-002, ADR-007; R-03, R-10|
|FR-019|N-X2|M|AC-019|ASR-02|Audit|Append-only AuditEntry (actor, time, from→to).|Written in the same transaction as the change.|EF Core + PostgreSQL|Planned|Specified: TC-PE-03|Approved|ADR-003; R-12|
|FR-020|N-R1, 4.4|M|AC-037|ASR-01, 02|Request Intake|logged\_by\_id; contact\_channel.|SubmitOnBehalf contract; staff permission required.|ASP.NET Core + EF Core|Planned|Specified: TC-IN-04|Proposed (CR-002)|CR-002; OD-07|
|FR-021|N-A1|M|AC-038|ASR-01|Administration|AppUser, Role, UserRole; deactivate, never delete.|Admin-only operations.|ASP.NET Core Identity|Planned|Planned|Proposed (CR-003)|CR-003; FE-09|
|FR-022|N-A1|M|AC-039|ASR-03|Administration|Category: name, is\_active, target\_resolution\_days.|Admin-only; inactive categories hidden from intake.|ASP.NET Core Identity|Planned|Planned|Proposed (CR-003)|CR-003; CR-001|
|NFR-001|N-X1|M|AC-020|ASR-01|Identity \& Access|Credentials stored hashed by Identity, never plain text.|Authentication required for non-public functions (CL-04).|ASP.NET Core Identity|Planned|Specified: TC-AZ-04|Approved (clarified CL-04)|ADR-002|
|NFR-002|N-X1|M|AC-021|ASR-01|Identity \& Access|—|Authorisation enforced server-side, never UI-only.|ASP.NET Core authorisation|Planned|Specified: TC-AZ-02|Approved|ADR-002; R-10|
|NFR-003|N-X1|M|AC-022|ASR-04|All (config)|Database credentials outside the repository.|Configuration from environment variables and user-secrets.|ASP.NET Core configuration|Planned|Planned (secrets scan in M3)|Approved|Section 7.6|
|NFR-004|N-X1, N-X2|M|AC-023|ASR-01|Identity \& Access|—|Negative authorisation test per protected operation.|xUnit|Planned|Specified: TC-AZ-01 to 06|Approved|ADR-002|
|NFR-005|N-X3|S|AC-024|ASR-05|Request Queries|Indexes; paged queries.|Pagination on all list operations.|EF Core|Planned|Planned|Approved|AS-02|
|NFR-006|N-X3|S|AC-025|ASR-05|Request Intake|Single-transaction insert.|No external calls on the submit path.|ASP.NET Core + EF Core|Planned|Planned|Approved|AS-02|
|NFR-007|N-X3|M|AC-026|ASR-02|Request Intake|Request committed before reference is returned.|Reference shown only after commit.|ASP.NET Core + EF Core|Planned|Specified: TC-PE-01, TC-IN-01|Approved|ADR-003, ADR-005|
|NFR-008|N-X2, N-X3|M|AC-027|ASR-02|Audit|History kept for retention period (AS-01); no deletes.|Append-only audit.|EF Core + PostgreSQL|Planned|Planned|Approved|AS-01; OD-02|
|NFR-009|N-X4|M|AC-028|—|Presentation|—|Guided submission form (Section 9).|Razor Pages|Planned|Planned (usability test)|Approved|Section 9|
|NFR-010|N-X4|S|AC-029|—|Presentation|—|Search and filter UI (Section 9).|Razor Pages|Planned|Planned (usability test)|Approved|Section 9|
|NFR-011|N-X4|M|AC-030|ASR-02|Request Intake + UI|Constraints mirror validation rules.|Validation in UI for usability, on the server for correctness.|Model validation + DB constraints|Planned|Specified: TC-IN-02|Approved (clarified CL-05)|ADR-005; CL-05|
|NFR-012|N-X3, N-X4|M|AC-031|—|Process|—|RTM verification column maintained.|—|This RTM|RTM|Approved|RTM|
|NFR-013|N-X2, N-X4|M|AC-032|—|Process|—|Change control demonstrated by CR-001 to CR-003.|—|CR records (Proposed)|CR records|Approved|D-006|
|NFR-014|N-X3|S|AC-033|ASR-02|Request Lifecycle|—|Lifecycle rules testable without the UI (pure FSM, ADR-006).|xUnit|Planned|Specified: TC-LC-01 to 16|Approved|ADR-006|
|NFR-015|N-X3|S|AC-034|ASR-04|All|—|Structured error logging without sensitive data.|ILogger|Planned|Planned|Approved|FE-06|
|NFR-016|N-X1|M|AC-035|ASR-04|All (config)|—|Environment-specific configuration injected at runtime.|ASP.NET Core configuration|Planned|Planned|Approved|Section 7.6|
|NFR-017|N-X3|S|AC-036|ASR-04|Data host|Database backups.|Recovery procedure before production.|—|Planned|Planned|Approved|R-11; OD-01|

\---

# 5\. Architecturally Significant Requirements, Architecture and ADRs

## 5.1 Architecturally significant requirements (ASRs)

Only quality attributes that would lead to a different architecture if ignored are treated as ASRs. Each comes from the M1 baseline and has a measurable expectation so it can later be verified (M2 brief, Section 5.2).

|ASR|Quality attribute|Source in M1 baseline|Measurable expectation|Architectural influence|
|-|-|-|-|-|
|ASR-01|Security: access control and privacy|N-X1; CF-1 (hardest clash); FR-018; NFR-001/002/004; R-03|100% of protected operations reject unauthorised calls in automated negative tests, including direct calls that bypass the UI.|One server-side authorisation point used by every module (ADR-002); no business logic in the client.|
|ASR-02|Integrity and auditability of the request lifecycle|N-X2; CF-3; FR-011, FR-019; NFR-007/008|Every status change produces exactly one audit entry (actor, time, from→to); illegal transitions are rejected; no acknowledged request is lost.|Only one module may change status; audit written in the same transaction (ADR-003); one transactional data store.|
|ASR-03|Modifiability for deferred features|M1 Section 4.3; FE-07; D-001|Adding a feedback event or a future notification, escalation or analytics feature needs no change to lifecycle transition rules (checked in PR design review).|Internal lifecycle events that new features subscribe to (ADR-003); read-only Reporting module.|
|ASR-04|Operational simplicity and low cost|N-X5; TO-2; team size; R-08|Runs as one application process plus one database within free or low-cost tier limits recorded in R-08.|Single deployable unit; no message broker, gateway or service mesh (ADR-001).|
|ASR-05|Responsiveness|N-X3; NFR-005/006|95% of list and view operations under 2 s and 95% of submissions under 3 s under the agreed workload (AS-02).|Moderate: favours in-process calls, indexed and paged queries; does not by itself justify distribution.|

**Deliberately not ASRs.** Scalability is low-significance: the client is a small organisation (M1, Section 1.3, "reliable and controlled, not big"), and designing for scale would add cost against ASR-04. Usability (NFR-009 to NFR-011) matters to stakeholders but is addressed in the user-interface design (Section 9), not the architecture.

## 5.2 Architecture alternatives considered

"Architecture" here means how responsibilities are structured and interact. The technologies that build it are a separate decision (ADR-004).

|Criterion|Layered monolith|Modular monolith|Microservices|Client-centric BaaS / serverless|
|-|-|-|-|-|
|**ASR-01 access control**|Adequate: central checks possible, but nothing prevents bypass as code grows.|Strong: one policy component all modules must call.|Adequate: repeated or gateway-enforced per service.|Weak: rules split between client code and platform security rules.|
|**ASR-02 lifecycle integrity**|Adequate: single transaction, but status logic can spread across layers.|Strong: single transaction; only Lifecycle changes status.|Weak: audit and request data in different services need distributed consistency.|Weak: transition rules hard to guarantee when clients write data.|
|**ASR-03 modifiability**|Weak: features cut across all layers.|Strong: new modules subscribe to lifecycle events.|Strong in principle, at high cost.|Adequate.|
|**ASR-04 cost / simplicity**|Strong.|Strong: still one deployable.|Weak: many deployables, networking, monitoring.|Strong at first; vendor lock-in.|
|**Team capability (3 students)**|Strong.|Strong: familiar code plus boundary discipline.|Weak: distributed-systems skills not held.|Adequate.|
|**Overall**|**Viable fallback**|**Selected**|**Rejected: complexity not justified by any ASR**|**Rejected: weakens ASR-01 and ASR-02**|

A more distributed architecture is not automatically better. Microservices mainly pay off when parts of a system need independent scaling, independent deployment by separate teams, or technology diversity (Richards and Ford, 2020). CivicConnect has none of these drivers, and splitting the request from its audit record would weaken ASR-02.

## 5.3 Architecture views

The views separate logical structure (modules and dependencies) from physical deployment tiers, so one module does not imply one server.

!\[Figure 1: System context](figures/fig1\_context.png)

*Figure 1: System context. **CivicConnect**, its user roles, and deliberately excluded external channels.*

!\[Figure 2: Logical view](figures/fig2\_logical.png)

*Figure 2: Logical view. Modules, responsibilities, traced requirements and allowed dependencies.*

**Dependency rules for Figure 2.** Every module that exposes a protected operation calls Identity \& Access. Only Request Lifecycle may change a request's status. Audit and Feedback receive lifecycle events and never call back. Reporting and Request Queries only read. Each table has exactly one writing module; other modules read it through that module's public interface or a read-only query (corrected from v2.0, which said "each module accesses only its own tables"; see Section 6.3).

!\[Figure 3: Deployment view](figures/fig3\_deploy.png)

*Figure 3: Deployment view. Three physical tiers; all logical modules run in one application process.*

## 5.4 Architecture Decision Records

All ADRs are **Proposed** and become Accepted only when merged with two non-author approvals (Master Project Brief, Section 9). Later-consequence rows are completed when evidence emerges (Master Project Brief, Section 13).

### ADR-001: Modular monolith as the architecture style

|||
|-|-|
|**Status**|Proposed 30/09/2026, M. Groenewald.|
|**Context**|Drivers ASR-01 to ASR-05. Three-person team, fixed schedule, free or low-cost hosting (TO-2), security throughout (D-005). M1 decision D-004 deferred this choice.|
|**Constraints**|Team size and skills; schedule; cost; one deployment target.|
|**Alternatives**|Layered monolith; modular monolith; microservices; client-centric BaaS (Section 5.2).|
|**Decision**|One deployable application organised into the modules in Figure 2, each internally layered (presentation → application → domain → data access). Each table has one writing module; modules interact through public interfaces.|
|**Rationale and trade-offs**|+ Single transaction for request plus audit (ASR-02); one place for authorisation (ASR-01); one process to deploy (ASR-04); path to extract a module later (ASR-03). − Boundaries rely on discipline and review, not the network (R-09); the whole app scales and fails as one unit (R-11), accepted under TO-2.|
|**Evidence**|Bass, Clements and Kazman (2021); Richards and Ford (2020).|
|**Later consequence**||
|**Affects**|All RTM Module entries; R-09, R-11; FE-01; supersedes D-004.|

### ADR-002: Server-side authorisation through one Identity \& Access policy component

|||
|-|-|
|**Status**|Proposed 30/09/2026, M. Groenewald.|
|**Context**|ASR-01; CF-1 (visibility vs. privacy); NFR-002 forbids relying on the UI to restrict access; R-03.|
|**Alternatives**|(a) hide functions in the UI only; (b) separate checks in each module; (c) one policy component called by every protected operation.|
|**Decision**|Option (c). Authentication establishes who the user is; the policy decides what that user may do, including ownership rules. The UI may hide options for convenience, but the server check is authoritative. The mechanism (role policies plus resource handlers) is decided in ADR-007.|
|**Trade-offs and risks**|+ Rules in one place, easy to review and test (NFR-004). − Every new operation must remember to call the policy (R-10); mitigated by a required negative test per protected operation and a PR checklist item.|
|**Evidence**|Section 8.2; Microsoft (2026a).|
|**Affects**|FR-004, 007, 009, 013, 018, 020 to 022; NFR-001, 002, 004; FE-04; R-03, R-10.|

### ADR-003: Status changes only through Request Lifecycle, with in-process lifecycle events

|||
|-|-|
|**Status**|Proposed 30/09/2026, M. Groenewald.|
|**Context**|ASR-02 (every change audited, no silent loss) and ASR-03 (deferred notifications, escalation, analytics). ASR-04 rules out a message broker.|
|**Alternatives**|(a) any module updates status and writes its own audit entry; (b) external message broker; (c) one module owns status changes and raises in-process events.|
|**Decision**|Option (c). Request Lifecycle is the only code that changes status. Each change raises a lifecycle event. The Audit handler runs in the same database transaction, so a change and its audit entry are saved together or not at all. The Feedback handler runs after commit, so a feedback problem cannot undo a valid change.|
|**Trade-offs and risks**|+ Audit completeness is guaranteed by design; future features subscribe without touching transition rules. − Events exist only inside the process, so a notice can be lost if the process stops after commit (R-15); acceptable while feedback is in-platform only, revisited before external notifications (OD-04).|
|**Affects**|FR-006, 010 to 013, 017, 019; NFR-007, 008, 014; R-12, R-15; FE-07, FE-08.|

\---

# 6\. Data and Persistence Engineering

The model is centred on the service request, because the project needs one controlled record that follows a request from submission to closure (M1, Section 1.2). This section replaces the prose entity list in PED v2.0 with an attribute-level model that agrees with the RTM and CR-001 to CR-003.

## 6.1 Entity-relationship model

!\[Figure 4: Logical data model](figures/fig4\_erd.png)

*Figure 4: Logical data model. PK = primary key, FK = foreign key, UK = unique key.*

## 6.2 Entities, constraints and indexes

|Entity|Purpose and key attributes|Constraints|Requirements served|
|-|-|-|-|
|AppUser|A person who signs in. email, display\_name, is\_active, created\_at. Credentials are held by ASP.NET Core Identity as salted hashes.|email UNIQUE; users are deactivated, never deleted.|FR-018, FR-021, NFR-001|
|Role / UserRole|Requester, ServiceStaff, Supervisor, Management, Administrator; many-to-many with users.|Role.name UNIQUE; composite PK (user\_id, role\_id).|FR-018, FR-021|
|Category|Controlled category list: name, is\_active, target\_resolution\_days (CR-001).|name UNIQUE; target\_resolution\_days > 0; inactive categories are hidden from new requests but kept on existing ones.|FR-002, FR-015, FR-022|
|ServiceRequest|The authoritative request record: reference, requester\_id, category\_id, assigned\_to\_id (null), logged\_by\_id (null, CR-002), contact\_channel (CR-002), title, description, status, version, created\_at, updated\_at, closed\_at (null), resolution\_note (null).|reference UNIQUE; FKs to AppUser and Category; title, description, category NOT NULL (CL-05); status CHECK in the seven states; version is the concurrency token; resolution\_note required when status is Resolved or later.|FR-001 to 005, 010, 011, 013, 020; NFR-007|
|RequestAction|Staff comments and resolution information: request\_id, author\_id, action\_type, body, created\_at.|FKs NOT NULL; not editable.|FR-012|
|AuditEntry|Append-only record of significant actions: request\_id, actor\_id, event\_type, from\_status, to\_status, detail, occurred\_at. Assignment history lives here (D-014).|FKs NOT NULL; the application database role has INSERT and SELECT only, no UPDATE or DELETE.|FR-017, FR-019, NFR-008; R-12|
|FeedbackNotice|In-platform notice per lifecycle event: request\_id, recipient\_id, event\_type, message, created\_at, read\_at.|FKs NOT NULL.|FR-006|

## 6.3 Data ownership and access

Authentication does not imply access to every request. Access depends on role and on the user's relationship to the request (ADR-007). Each table has one **writing** module; query and reporting modules read through read-only queries.

|Table|Writing module|Read by|Note|
|-|-|-|-|
|AppUser, Role, UserRole|Administration (user management); Identity \& Access (sign-in state)|Identity \& Access; all modules for names|Staff see display names only for assigned/visible requests.|
|Category|Administration|Request Intake, Reporting|Intake reads active categories only.|
|ServiceRequest|Request Intake (insert); Request Lifecycle (status, assignee, close fields)|Request Queries, Reporting|Requester personal details are staff-only; reports use aggregates (AS-05).|
|RequestAction|Request Lifecycle|Request Queries|—|
|AuditEntry|Audit|Reporting|Append-only.|
|FeedbackNotice|Feedback|Request Queries (own notices)|Requester sees own notices only.|

## 6.4 Integrity, consistency and atomicity

A status change is not simply "UPDATE status". A valid operation authorises the caller, checks that the FSM allows the transition from the current state, updates the request, records the audit entry, and commits all of it or none of it. The order is **authorise first** (v2.0 contradicted itself), so an unauthorised caller learns nothing about whether a request or state exists.

!\[Figure 5: Lifecycle operation sequence](figures/fig6\_opflow.png)

*Figure 5: Lifecycle operation sequence and failure outcomes (ADR-003, ADR-005, ADR-006, ADR-007).*

Two complementary layers protect correctness: **application rules** (authorisation, lifecycle and business validation) and **database constraints** (keys, NOT NULL, unique, check). Neither replaces the other. The UI gives fast feedback but is never the authority (NFR-002, NFR-011).

|Layer|Validation responsibility|
|-|-|
|UI / client|Immediate feedback and basic input checks (usability only).|
|Application / service|Authorisation, lifecycle rules, mandatory-field and business validation.|
|Database|Structural and referential integrity, uniqueness, status CHECK, append-only audit permissions.|

## 6.5 Concurrency

Two staff users may open the same request. Without protection, an older view could silently overwrite a newer change. The design uses **optimistic concurrency** with the ServiceRequest.version token: the update succeeds only if the version equals the version that was read, and increments it; if zero rows are updated, the operation fails with a conflict, the transaction rolls back, and the user must reload the current state. Last-write-wins is rejected because accountability matters more than convenience (ASR-02). EF Core supports concurrency tokens natively (Microsoft, 2026b).

## 6.6 Authoritative data and caching

The database is the authoritative source of request status. No caching is introduced in the status-transition path, because a stale value could produce an incorrect lifecycle decision. Caching of read-heavy reports may be considered later only if M3 performance evidence shows it is needed.

## 6.7 Scalability, availability and single point of failure

Scalability is handled proportionately (M1, Section 1.3): appropriate indexes, paged queries, short transactions, and controlled growth of the audit table. The database is not distributed. The single database and single application instance are recorded single points of failure (R-11). Initial mitigation: reliable managed hosting, regular backups, a documented and **tested** restore procedure before production (NFR-017), storage and capacity monitoring, and treating the database as a critical deployment dependency. Replication and automated failover would add cost and operational complexity not justified by the known scale (AS-02).

## 6.8 ADR-005: Relational persistence with application-managed transactions

|||
|-|-|
|**Status**|Proposed.|
|**Context**|ASR-02, ASR-05; data is strongly relational (users, roles, requests, categories, history); reporting needs structured queries; sensitive data (CF-1).|
|**Alternatives**|Relational with application-managed transactions (selected); relational with database-centric stored procedures; document store; last-write-wins updates.|
|**Decision**|PostgreSQL relational schema as in Figure 4. Lifecycle rules live in application code (testable by a three-person team) and the database provides the atomic boundary through a short transaction. Optimistic concurrency via a version column. The current assignee is kept on ServiceRequest and assignment history is recorded in AuditEntry (D-014).|
|**Trade-offs**|+ Rules stay understandable and unit-testable; atomic persistence guaranteed. − Constraints must be duplicated in code and database; stored procedures would centralise enforcement but couple rules to database-specific code. Concurrency conflicts surface to users as a reload prompt.|
|**Risks**|R-11 (single point of failure); R-12 (audit bypass).|
|**Evidence**|PostgreSQL Global Development Group (2026a, 2026b); Microsoft (2026b).|
|**Later consequence**|-|
|**Affects**|FR-001 to 003, 010 to 013, 019, 020; NFR-007, 008; RTM data column; D-009, D-014.|

\---

# 7\. Technology Stack and Deployment Compatibility

## 7.1 Selected stack and verified versions

|Area|Selection|Version / support|Evidence and note|
|-|-|-|-|
|Runtime and framework|.NET and ASP.NET Core, language C#|.NET 10 (LTS), released 11 Nov 2025, end of support 14 Nov 2028|Microsoft .NET support policy (Microsoft, 2026c): the page lists .NET 10 as LTS with end of support 14 November 2028 (checked 12 Oct 2026; the latest patch then was 10.0.12). Pin the SDK in global.json.|
|Web UI|ASP.NET Core Razor Pages (server-rendered)|Part of ASP.NET Core 10|Lower complexity than a SPA for a form- and report-oriented system (Section 7.3).|
|Data access|Entity Framework Core 10 with migrations|EF Core 10|Source-controlled schema evolution (Microsoft, 2026b).|
|Database provider|Npgsql.EntityFrameworkCore.PostgreSQL|10.0.x; version 10.0.1 listed on NuGet (NuGet, 2026)|Provider major version matches EF Core 10.|
|Database|PostgreSQL|Major version 18 (supported to 14 Nov 2030). Versions 14 and below must not be used: 14 reaches end of life on 12 Nov 2026.|PostgreSQL Global Development Group versioning policy (2026a); the 18 and 14 dates were checked against secondary sources that cite it, so choosing 18 gives the longest support runway.|
|Authentication|ASP.NET Core Identity; cookie authentication|Part of ASP.NET Core 10|Standard framework mechanism; credentials hashed (NFR-001).|
|Authorisation|Role policies plus resource-based handlers|Part of ASP.NET Core 10|ADR-007; Microsoft (2026a).|
|Testing|xUnit, and ASP.NET Core integration testing against PostgreSQL|—|Versions were not verified in this revision.|
|Source control and review|Git and GitHub, protected main, PR template|—|GitHub governance evidence to be added in PED v3.0.|
|Configuration and secrets|ASP.NET Core configuration, environment variables, user-secrets in development|—|NFR-003, NFR-016; never committed.|

## 7.2 Why the technology question is bounded

The decision is constrained by three facts from M1: three students, a fixed schedule, and free or low-cost operation (TO-2, TO-5). A broad stack would add learning-curve risk (R-02) without serving any ASR. The comparison therefore weights **team capability** most heavily.

## 7.3 Weighted decision matrix

Weights follow the factors required by Master Project Brief, Section 18.1. Scores run from 1 (poor) to 5 (strong). The scores are the author's judgement from the evidence in 7.4; the team must review and, where they disagree, change a score.

|Criterion|Weight|A|B|C|D|
|-|-|-|-|-|-|
|Requirement and architecture fit|15|5|4|4|4|
|Team capability and learning curve|20|4|3|3|3|
|Availability in the development environment|10|4|4|3|4|
|Security ecosystem and dependency maturity|15|5|3|4|4|
|Maintainability and future change|10|4|3|4|3|
|Testing and automation tooling|10|5|4|4|4|
|Deployment and operational compatibility|10|4|4|4|3|
|Development and operating cost|5|5|5|5|5|
|Vendor lock-in and unavailability consequence|5|4|5|5|4|
|**Weighted total (max 5.00)**|**100**|**4.45**|**3.65**|**3.80**|**3.65**|
|**Sensitivity: team-capability weight 10**||4.51|3.73|3.90|3.73|
|**Sensitivity: team-capability weight 30**||4.39|3.57|3.70|3.57|
|**Sensitivity: team-capability ignored (0)**||4.56|3.81|4.00|3.81|

**Reading the result.** Option A scores highest at 4.45, against 3.65 (B), 3.80 (C) and 3.65 (D). The sensitivity rows show whether the result depends on how much weight the team-capability criterion gets. With that weight at 10, 30 or 0, the best option is A, A and A respectively. Option A wins even if team capability is ignored, so the decision does not rest on familiarity alone.

*The matrix is a structured way to expose assumptions, not proof that A is best. It cannot capture a member's actual skill, which is why the team capability scores need team confirmation.*

## 7.4 Evidence behind the scores

|Criterion|Evidence and reasoning|
|-|-|
|Fit|Relational, form-and-report-oriented system with strict transitions (ASR-02) and central authorisation (ASR-01). A single ASP.NET Core application hosts all modules (ADR-001).|
|Team capability|v2.0 recorded one primary language and the need to stay familiar with .NET.|
|Environment availability|.NET 10 SDK and PostgreSQL are installable on common development machines, but institutional availability is not guaranteed (Master Brief, Section 25). Recorded as AS-07 and R-14.|
|Security ecosystem|Authentication, authorisation, anti-forgery and data-protection features are built into ASP.NET Core and supported by Microsoft within the LTS window. Dependencies are limited, which reduces third-party exposure (R-16).|
|Maintainability|LTS runtime to November 2028; one language across modules; strongly typed domain and tests. Razor Pages avoids a second front-end stack (options B and D).|
|Testing tooling|xUnit and ASP.NET Core integration-test support allow lifecycle rules to be tested without the UI (NFR-014).|
|Deployment|ASP.NET Core runs on common cloud and container hosts and can be published as a self-contained artefact. Hosting is deferred (OD-01); see 7.6.|

## 7.5 Cost, licensing and learning-curve analysis

|Item|Analysis|
|-|-|
|Licensing|.NET and ASP.NET Core are open-source and free to use; PostgreSQL is open-source under the PostgreSQL licence. No licence fee is expected for the selected stack.|
|Development cost|Free tooling (.NET SDK, a free IDE or editor, GitHub). Educational GitHub features may apply.|
|Operating cost|The hosting platform is not chosen (OD-01), so **no operating cost is claimed**. The platform research must record for each candidate: database size and retention limits, idle or sleep behaviour, backup features, and the price after any free period (R-08). The phrase "the platform is free" is not acceptable evidence (Master Brief, Section 23).|
|Learning curve|Largest learning risks: EF Core migrations and concurrency tokens; ASP.NET Core resource-based authorisation; integration testing with a real database. Mitigation: build one vertical slice first (FR-011, Section 10.4), keep to framework defaults, and pair on the first policy handler.|
|If the technology becomes unsuitable|Because the architecture separates modules and the domain rules (the FSM) are plain C# with no framework dependency, the lifecycle logic is portable. Changing platform would still require redevelopment of the UI and data access (accepted consequence).|

## 7.6 Deployment compatibility (M2 brief, Section 5.8)

**Deployment direction (assumption, not a decision).** One application process plus one managed PostgreSQL database, reached over HTTPS, as in Figure 3. This is plausible for the selected stack because ASP.NET Core and PostgreSQL both run on standard Linux hosts and common managed platforms. The specific platform is deferred to M3 (OD-01) because the cost, backup and free-tier evidence is not yet available.

|Concern|Implication and current position|
|-|-|
|Configuration and secrets|Connection strings and keys come from environment variables (and user-secrets in development), never from the repository (NFR-003, NFR-016). A .env.example documents the keys without values.|
|Persistence and state|The application keeps no durable state in memory or on local disk; all state is in PostgreSQL. This permits restarts and later redeployment. In-process events are not durable (R-15).|
|Schema deployment|EF Core migrations are applied by an explicit deployment step in staging and production, not automatically at start-up (D-015), to avoid unreviewed or concurrent schema changes.|
|Networking and TLS|HTTPS to the application; TLS and a least-privilege database account for the database connection (Figure 3). The application database role must not own the schema in production.|
|Availability and backup|Single instance and single database (R-11). A restore test is required before any production use (NFR-017).|
|Environments|Development (local, user-secrets, seeded data), test (automated, disposable database), staging and production (M3, environment variables). The same build artefact should move between environments; only configuration differs.|
|Deferred|Hosting platform; monitoring and alerting; rollback procedure; container packaging. Evidence needed: platform cost and limits research (OD-01), M3 staging results.|

## 7.7 ADR-004: Technology stack

|||
|-|-|
|**Status**|Proposed|
|**Context**|Web application with authentication, role and resource-based access, relational data, reporting and auditability; three students; fixed schedule; free or low-cost operation.|
|**Constraints**|Team capability, schedule, cost, security, BC Desktop availability (R-14), no hosting chosen.|
|**Alternatives**|Options A to D in Section 7.3. Each was compared against the same criteria and weights.|
|**Decision**|Option A: ASP.NET Core 10 on .NET 10 LTS, C#, Razor Pages, EF Core 10 with the Npgsql provider, PostgreSQL 18, ASP.NET Core Identity, xUnit.|
|**Rationale and trade-offs**|+ One language and ecosystem; strong built-in security and testing support; LTS to November 2028. − Team must stay current with .NET; the stack is less common in some free hosting tiers than Node.js (OD-01); changing platform later means significant redevelopment.|
|**Risks**|R-02 (unfamiliar or hard to deploy), R-14 (environment availability), R-16 (dependencies), R-08 (hosting cost).|
|**Evidence**|Sections 7.3 to 7.6; Microsoft (2026a, 2026b, 2026c); PostgreSQL Global Development Group (2026a); NuGet (2026).|
|**Later consequence**||
|**Affects**|RTM Technology column; D-008; supersedes D-003; FE-02, FE-06.|

\---

# 8\. Initial Design and Interface Decisions

## 8.1 Design problem 1: controlling the request lifecycle

### Problem

How should CivicConnect represent and enforce valid request-status transitions without spreading lifecycle rules across the application? Scattered conditions such as "if status is Submitted…" in controllers and services raise coupling and make rules hard to test or change (FR-011, ASR-02, ASR-03).

### Authoritative state model

One state model applies everywhere (this replaces the inconsistent lists in v2.0). Rejected and Closed are terminal. **Reopening a Resolved request is not in the table** until the client decides OD-05; adding it later is one new row plus tests.

!\[Figure 6: Request lifecycle state model](figures/fig5\_fsm.png)

*Figure 6: Request lifecycle state model with transitions T1 to T6.*

|ID|From → To|Event|Who may do it (policy, ADR-007)|Guard and effect|
|-|-|-|-|-|
|T1|Submitted → Accepted|Accept|ServiceStaff, Supervisor|Audit entry; Feedback "accepted".|
|T2|Submitted → Rejected|Reject|ServiceStaff, Supervisor|Reason required. Audit; Feedback "rejected (with reason)".|
|T3|Accepted → Assigned|Assign|Supervisor: any staff. ServiceStaff: self only|Sets assigned\_to\_id. Audit records the assignee.|
|T4|Assigned → In Progress|Start|The assignee|Audit; Feedback "status updated".|
|T5|In Progress → Resolved|Resolve|The assignee|resolution\_note required. Audit; Feedback "resolved".|
|T6|Resolved → Closed|Close|Users with close permission|Sets closed\_at. Audit; Feedback "closed".|

### Alternatives considered

|Criterion|State pattern|Table-driven FSM|Enum + guarded method|
|-|-|-|-|
|**Initial complexity**|High: a class per state.|Moderate: one table plus a small engine.|Low.|
|**Transition visibility**|Spread across state classes.|Centralised in one table.|Centralised in conditionals.|
|**Adding a transition**|Add or change state behaviour.|Add a row.|Edit conditional logic.|
|**Testability**|Good.|High: table is data, engine is a pure function.|Good at first, declines as rules grow.|
|**Over-engineering risk**|Higher.|Moderate.|Low.|
|**CivicConnect fit**|Reserve option if a state gains substantially different behaviour.|**Selected**|Acceptable fallback.|

### Decision, benefit and trade-off

**Decision (ADR-006):** a small, project-specific table-driven finite-state machine. **Benefit:** transition rules are visible in one place, adding or removing a transition is a data change, and the engine can be unit-tested with no database or UI (NFR-014). **Complexity introduced:** more structure than an enum, a separate guard concept, and a dedicated test set. The State pattern is not rejected permanently; it stays available through change control.

### Application translation (design sketch, not yet implemented)

```csharp
// Domain, no framework dependency
public enum RequestStatus { Submitted, Accepted, Rejected, Assigned, InProgress, Resolved, Closed }
public enum LifecycleEvent { Accept, Reject, Assign, Start, Resolve, Close }

public static class LifecycleTable
{
    // (From, Event) -> To. Anything not listed is illegal.
    static readonly IReadOnlyDictionary<(RequestStatus, LifecycleEvent), RequestStatus> Rules = ...;

    public static TransitionResult Evaluate(RequestStatus from, LifecycleEvent e, TransitionContext ctx);
}
```

Components: LifecycleTable and TransitionContext (domain); RequestLifecycleService (application: authorise → evaluate → transaction → audit → commit → event); ServiceRequest entity with the version token; AuditEntry writer; the Request Lifecycle module owns all of them (Figure 2). The service calls the policy first, so an unauthorised caller learns nothing (Section 6.4).

### Verification specification

Tests must prove that **invalid** transitions are rejected, not only that valid ones succeed. The full list is TC-LC-01 to TC-LC-16 in Section 10.5. Status: specified, not executed.

### ADR-006: Request lifecycle as a table-driven FSM

|||
|-|-|
|**Status**|Proposed. Was an unnumbered ADR in v2.0.|
|**Context**|FR-011 and AC-011; ASR-02 and ASR-03; state model T1 to T6; CL-03.|
|**Alternatives**|State pattern; table-driven FSM; enum with guarded method.|
|**Decision**|Table-driven FSM in the domain layer, called only from RequestLifecycleService.|
|**Rationale and trade-offs**|Centralised, testable, moderate complexity; suits three students. More structure than an enum; guards and permissions must be kept consistent with ADR-007.|
|**Evidence**|Assignment 2; Richards and Ford (2020) on proportionate design.|
|**Affected requirements, data, components**|FR-010 to 013, 019; NFR-014; ServiceRequest.status and version; AuditEntry; Request Lifecycle module.|
|**Implementation evidence**||
|**Later consequence**||

## 8.2 Design problem 2: authorisation that depends on the request, not only the role

### Problem

Requesters must see their own requests, staff only what their role and assignment allow, and management aggregates, while sensitive details stay private (CF-1, FR-007, FR-009, FR-018, NFR-002). A role check alone cannot express "a requester may view this request only if they own it". How should that rule be written once and applied everywhere?

### Alternatives considered

|Approach|Advantage|Disadvantage|Verdict|
|-|-|-|-|
|Role checks only|Simple.|Too coarse for ownership and assignment rules.|Rejected|
|Checks written in each controller or page|Easy to start.|Repeated logic; easy to forget (R-10).|Rejected|
|Role policies plus resource-based handlers|Central, testable rules; framework support.|More design work; handlers need tests.|**Selected**|
|Separate authorisation service|Strong separation.|Excessive for one process and three students.|Rejected|

### Decision, benefit and trade-off

**Decision (ADR-007):** role-based policies for broad capability (for example "may use the staff queue") and resource-based handlers for per-request access (owner, assignee, close permission). **Benefit:** the rules live in one place, are testable without the UI, and satisfy ASR-01. **Complexity introduced:** handlers and requirements to design and maintain, and a rule that every protected operation calls the policy, backed by a required negative test (NFR-004) and a PR checklist item.

**Components:** Identity \& Access module (policies, handlers), called by Request Intake, Request Queries, Request Lifecycle, Reporting and Administration. When a caller may not see a request, the response is the same as for a request that does not exist (404), so existence is not disclosed.

### ADR-007: Policy and resource-based authorisation

|||
|-|-|
|**Status**|Proposed. Renumbered from v2.0 ADR-003 (Technology section).|
|**Context**|ASR-01; CF-1; FR-007, 009, 018; NFR-001, 002, 004; R-03, R-10.|
|**Alternatives**|Roles only; per-controller checks; policy plus resource handlers; separate service.|
|**Decision**|Policy plus resource handlers, always evaluated server-side before any data is returned or changed.|
|**Rationale and trade-offs**|+ Central, testable, supports future rules. − More initial design; needs dedicated tests (TC-AZ-01 to 06).|
|**Evidence**|Microsoft (2026a); Assignment 2.|
|**Implementation evidence**||
|**Later consequence**||
|**Affects**|FR-004, 007, 009, 013, 018, 020 to 022; NFR-001, 002, 004; ADR-002.|

## 8.3 Initial interface and integration decision

No external system is integrated (M1, Section 4.2). The interactions that exist are between modules inside one process and between the browser and the server. The M2 brief (Section 5.7) asks that no network boundary is created merely because APIs were taught.

|Aspect|Decision|
|-|-|
|Browser ↔ server|Server-rendered Razor Pages over HTTPS. Form posts carry anti-forgery tokens. No separate public REST API in M2.|
|Module ↔ module|In-process public interfaces (C# interfaces injected by dependency injection). For example: ISubmitRequest, ILifecycleService, IRequestQueries, IAuthorisationPolicy. Lifecycle events use an in-process dispatcher (ADR-003).|
|Information exchanged|Request commands carry the acting user, request id, event and payload (note, reason, assignee) and the expected version; results return the new version or a typed error.|
|Validation and errors|Validation at the module boundary before any write. Typed outcomes: Forbidden/NotFound (indistinguishable to the caller), InvalidTransition, ValidationFailed, Conflict (version changed). Pages map them to messages; no stack traces or sensitive data are shown (NFR-015).|
|Security boundary|The server-side policy is the boundary (ADR-002, ADR-007). Browser input is untrusted.|
|Change and versioning|Interfaces are internal and change with the code. If a network API is later needed (for example a mobile client), that is a change request and a new ADR, and these contracts become the starting point.|
|Deferred|Any external or network API; notification channels (OD-04, FE-08).|

### ADR-008: In-process module contracts and server-rendered UI

|||
|-|-|
|**Status**|Proposed|
|**Context**|ASR-04 and ASR-05; no external integration; team size.|
|**Alternatives**|Separate REST API with a SPA; in-process contracts with server-rendered pages (selected).|
|**Decision**|Section 8.3 table.|
|**Rationale and trade-offs**|+ Fewer moving parts; one deployable; no extra attack surface. − Not directly reusable by other clients; extraction later needs new contracts and a new ADR.|
|**Evidence**|Richards and Ford (2020); Assignment 2.|
|**Affects**|RTM Design/interface column; FE-08; D-012.|

\---

# 9\. Information Architecture and Interface Design

The Master Project Brief (Section 20.2) expects information architecture, wireframes and a usability and accessibility rationale. PED v2.0 had none. This section is **design intent**, not implemented screens.

## 9.1 Information architecture by role

|Role|Pages (menu items)|Notes|
|-|-|-|
|Requester|Submit request · My requests (status and history) · Request detail · Notices|Sees only their own requests (ADR-007). Reference shown after successful submission.|
|Service staff|Work queue (search, filter, sort) · Request detail with actions · Log request on behalf|Actions offered depend on the current state (FSM).|
|Supervisor|Everything staff have · Assign work|May assign any staff; sees all requests (AS-05).|
|Management|Service activity · Open / overdue / resolved / closed · By category and status · Accountability view|Read-only; no requester personal details (AS-05).|
|Administrator|Users and roles · Categories (active flag, target resolution days)|Highest privilege; negative tests mandatory.|

## 9.2 Wireframes

Low-fidelity wireframes for the two screens that carry the most risk: the requester submission form (FR-001 to FR-003, NFR-009, NFR-011) and the staff request detail with state-dependent actions (FR-011, FR-012).

!\[Figure 7: Wireframe, requester submit form](figures/wf\_submit.png)

*Figure 7: Wireframe, requester submit form.*

!\[Figure 8: Wireframe, staff request detail](figures/wf\_detail.png)

*Figure 8: Wireframe, staff request detail (state "In Progress").*

## 9.3 Usability and accessibility rationale

* **Guided form (NFR-009, NFR-011).** Mandatory fields marked, category chosen from a list, errors shown beside the field before submit. This also serves CF-5 (structured enough to sort, simple enough to use). Validation messages are generated by the server so correctness does not depend on the browser.
* **State-dependent actions (FR-011).** The page offers only actions the FSM allows. This reduces errors and keeps the UI consistent with the server, which re-checks every action (NFR-002).
* **Conflict handling.** A 409 conflict shows a clear message and a reload prompt (Section 6.5) instead of silently losing a colleague's change.
* **Accessibility intent.** Target WCAG 2.2 level AA where practical (W3C, 2023): labelled form fields, keyboard operation, visible focus, sufficient contrast, status not conveyed by colour alone (status shown as text), semantic headings. Responsive layout for phones (AS-03). Not yet verified; verification (for example an automated checker plus a keyboard-only walkthrough) is planned for M3.
* **Usability test (AC-028, AC-029).** Planned: one representative requester submits a valid request unassisted; one staff user finds a named request using search or filter. Not yet run; participants and results go in M3.

\---

# 10\. End-to-End Trace and Verification Specification

> \\\*\\\*Scope of this revision.\\\*\\\* Sections 10.1 to 10.3 (repository structure, development expectations for M2, and GitHub governance evidence) are intentionally not included in this revision. No repository structure or development evidence exists yet to document, and nothing is claimed here. They will be added to PED v3.0 once the repository and its evidence exist. The numbering of 10.4 and 10.5 is kept so existing references (RTM, Forward Engineering Register, corrections log) stay valid.

## 10.4 End-to-end trace: FR-011 (controlled status transitions)

|Link|CivicConnect content|Evidence|
|-|-|-|
|Stakeholder need|N-S5: staff move a request through set status steps.|M1, Section 3.1|
|Requirement|FR-011: only defined transitions can be completed (clarified by CL-03).|M1; Section 1.2|
|Acceptance criterion|AC-011: given a defined state, only an allowed transition completes.|M1, Section 8.1|
|ASR / constraint|ASR-02 integrity and auditability; D-005 quality as a protected constraint.|Section 5.1|
|Architecture responsibility|Request Lifecycle is the only module that changes status; Audit in the same transaction.|ADR-001, ADR-003; Figure 2|
|Data decision|ServiceRequest.status CHECK and version token; AuditEntry append-only; short transaction with optimistic concurrency.|ADR-005; Figure 4|
|Design / interface decision|Table-driven FSM (T1 to T6); typed outcomes (InvalidTransition, Conflict); authorise first.|ADR-006, ADR-007, ADR-008|
|Technology / ADR|C# domain library, EF Core transaction and concurrency token, xUnit.|ADR-004|
|Application artefact|\[TEAM: PR #, file path and commit of LifecycleTable and RequestLifecycleService]||
|Initial verification|\[TEAM: result of TC-LC-01 to 16 (and TC-PE-02 when available), with run date and commit]||

## 10.5 Verification specification (30 test cases)

These cases are **specified, not executed**. Each has a traceable basis, a deliberately chosen input, and an expected result derived from the requirements before running anything.

|ID|Basis|Technique|Input / precondition|Expected result|Status|
|-|-|-|-|-|-|
|TC-LC-01|FR-011, AC-011; T1|State transition (valid)|Request Submitted; actor ServiceStaff.|Accept → Accepted; one AuditEntry (Submitted→Accepted); version +1.|Specified, not run|
|TC-LC-02|FR-011; T2; CL-02|State transition (valid) + guard|Request Submitted; reason supplied.|Reject → Rejected; reason stored; one AuditEntry.|Specified, not run|
|TC-LC-03|FR-010, AC-010; T3|State transition (valid)|Request Accepted; actor Supervisor; target staff user.|Assign → Assigned; assigned\_to\_id set; AuditEntry records assignee.|Specified, not run|
|TC-LC-04|FR-011; T4|State transition (valid)|Request Assigned; actor is the assignee.|Start → In Progress.|Specified, not run|
|TC-LC-05|FR-011, FR-012; T5|State transition (valid) + guard|Request In Progress; actor assignee; note supplied.|Resolve → Resolved; resolution\_note stored.|Specified, not run|
|TC-LC-06|FR-013, AC-013; T6|State transition (valid)|Request Resolved; actor has close permission.|Close → Closed; closed\_at set.|Specified, not run|
|TC-LC-07|FR-011, AC-011|State transition (invalid)|Request Submitted.|Close → InvalidTransition; status, version and audit unchanged.|Specified, not run|
|TC-LC-08|FR-011, CL-03|State transition (invalid)|Request Submitted.|Assign → InvalidTransition (must pass through Accepted). Corrects the v2.0 test list.|Specified, not run|
|TC-LC-09|FR-011|State transition (invalid)|Request Closed.|Start → InvalidTransition.|Specified, not run|
|TC-LC-10|FR-011|State transition (invalid)|Request Resolved.|Assign → InvalidTransition.|Specified, not run|
|TC-LC-11|FR-011; OD-05|State transition (invalid)|Request Rejected.|Accept → InvalidTransition (Rejected is terminal).|Specified, not run|
|TC-LC-12|FR-011|State transition (invalid)|Request Accepted.|Resolve → InvalidTransition.|Specified, not run|
|TC-LC-13|FR-011; CL-02|Negative; EP on reason (empty vs present)|Request Submitted; reason empty.|Reject → ValidationFailed; nothing saved.|Specified, not run|
|TC-LC-14|FR-013; AC-013|Negative; EP on note (empty vs present)|Request In Progress; note empty.|Resolve → ValidationFailed; nothing saved.|Specified, not run|
|TC-LC-15|FR-011; ADR-007|Negative; authorisation|Request Assigned; actor is not the assignee.|Start → Forbidden; status unchanged.|Specified, not run|
|TC-LC-16|FR-010; ADR-007|Negative; authorisation|Request Accepted; ServiceStaff tries to assign another staff member.|Assign → Forbidden; staff may only self-assign.|Specified, not run|
|TC-PE-01|NFR-007, AC-026|Integration (real PostgreSQL)|Valid submission committed.|Reference returned only after commit; request retrievable afterwards by its reference.|Specified, not run|
|TC-PE-02|ASR-02; ADR-005|Integration; concurrency|Two actors load the same request (version 7); both attempt a transition.|First succeeds (version 8); second returns Conflict; exactly one AuditEntry added.|Specified, not run|
|TC-PE-03|FR-019, R-12; ADR-003|Integration; atomicity (fault injection)|Force the AuditEntry insert to fail during a transition.|Transaction rolls back; status and version unchanged; no partial audit.|Specified, not run|
|TC-AZ-01|FR-004, 009, 018; AC-018; R-03|Negative; unauthorised|Requester B requests the detail of requester A's request.|NotFound; no request data in the response.|Specified, not run|
|TC-AZ-02|NFR-002, AC-021|Negative; bypass UI|Requester sends a direct Accept request for their own Submitted request.|Forbidden; status unchanged.|Specified, not run|
|TC-AZ-03|FR-013; AC-013|Negative; unauthorised|Staff without close permission closes a Resolved request.|Forbidden; status unchanged.|Specified, not run|
|TC-AZ-04|NFR-001, AC-020|Negative; unauthenticated|Anonymous call to a protected page.|Redirected to sign-in; no data returned.|Specified, not run|
|TC-AZ-05|FR-007, AC-007; AS-05|Decision table (role × assignment)|Staff A (assigned), Staff B (not assigned), Supervisor.|Lists differ per AS-05: A sees own plus unassigned Submitted/Accepted; Supervisor sees all.|Specified, not run|
|TC-AZ-06|NFR-004, AC-023|Negative; unauthorised|Non-administrator calls each Administration operation.|Forbidden for every operation; no user, role or category changed.|Specified, not run|
|TC-IN-01|FR-001, 003; AC-001, 003|Equivalence partition (valid)|Valid category, title, description.|Request created, reference unique (two submissions give two references), status Submitted.|Specified, not run|
|TC-IN-02|FR-002, NFR-011; AC-002, 030; CL-05|EP / BVA on mandatory fields|Missing category; empty title; empty description.|Rejected with the missing field identified; nothing saved.|Specified, not run|
|TC-IN-03|FR-002, FR-022; AC-039|Negative; EP on category state|Category is inactive.|Submission rejected; existing requests keep the category.|Specified, not run|
|TC-IN-04|FR-020, AC-037; CR-002|Negative; unauthorised|A requester calls submit-on-behalf.|Forbidden; no request created.|Specified, not run|
|TC-RP-01|FR-015, AC-015; CR-001|Boundary value analysis|Category target 5 days; open requests aged 4d 23h, 5d 0h, 5d 1h; also Closed and Rejected requests older than 5 days.|Not overdue, not overdue (elapsed must exceed target), overdue; Closed and Rejected never overdue.|Specified, not run|

\---

# 11\. Architecture, Technology \& Initial Design Baseline (BL-M2)

## 11.1 What the baseline contains

|Baseline item|Content / reference|
|-|-|
|Identifier and version|BL-M2 · PED v2.1 · effective only on sign-off (Section 11.4). Supersedes the unsigned BL-M2 of PED v2.0.|
|Requirements|M1 requirements as clarified by CL-01 to CL-05, plus CR-001 to CR-003 once approved.|
|Architecture|ASR-01 to ASR-05; ADR-001 to ADR-003; Figures 1 to 3.|
|Technology|ADR-004: .NET 10 LTS, ASP.NET Core, C#, Razor Pages, EF Core 10 with Npgsql, PostgreSQL 18, Identity, xUnit; versions in Section 7.1.|
|Data|ADR-005; Figure 4; Sections 6.2 to 6.7.|
|Initial design and interface|ADR-006 (lifecycle FSM), ADR-007 (authorisation), ADR-008 (interfaces); Sections 8 and 9.|
|Traceability|RTM v2.1 (Section 4); FR-011 trace (Section 10.4); Risk Register (Section 3); Forward Engineering Register (Section 2.1).|

## 11.2 How the ASRs drove the decisions

|ASR|Where it changed the architecture|
|-|-|
|ASR-01|Identity \& Access as a module every other module depends on (Figure 2); ADR-002 and ADR-007; BaaS rejected.|
|ASR-02|Single deployable and transactional database; Lifecycle as sole owner of status; audit in the same transaction (ADR-003); optimistic concurrency (ADR-005); FSM (ADR-006); microservices rejected.|
|ASR-03|Lifecycle events and a read-only Reporting module so deferred features plug in.|
|ASR-04|One application process and one database (Figure 3); no broker, gateway or service mesh; hosting deferred until cost evidence exists.|
|ASR-05|In-process module calls; indexed and paged queries recorded as data impacts in the RTM.|

## 11.3 Open and deliberately deferred decisions

|ID|Decision still open|Why deferred|Evidence needed / by when|
|-|-|-|-|
|OD-01|Hosting and deployment platform|Cost, backup and free-tier evidence not yet gathered.|Platform comparison with cost and limits (R-08) · M3|
|OD-02|Retention period for request and audit history (NFR-008)|Business or legal decision for the client.|Client confirmation · before M3|
|OD-03|Representative performance workload (NFR-005/006)|Realistic usage not yet known.|Client usage estimate · before performance testing|
|OD-04|External notification channel (email/SMS)|Out of scope (M1, Section 4.3).|Change request if the client asks; revisit ADR-003 and R-15|
|OD-05|Whether Resolved requests can be reopened|Changes lifecycle rules and reports.|Client decision · before FR-011 is finished|
|OD-06|Scope of automated CI checks|Adopted progressively.|M3 quality strategy|
|OD-07|How a requester without an account is represented when staff log on their behalf (CR-002)|Affects the data model (requester\_id is required) and privacy.|Client and team decision · before FR-020 is built|

## 11.4 Baseline approval

Baseline sign-off template from Master Project Brief, Appendix D. **Left blank deliberately.** It is completed only after the PR containing this document has two non-author approvals, and it must reflect what actually happened.

|Name|Role|Decision|Date|
|-|-|-|-|
|Migal Groenewald|Architecture and requirements owner|||
|Nuvaran Reddy|Data, interfaces and traceability owner|||
|Markus Jooste|Technology, design and development owner|||

\---

# 12\. AI Usage Register

Fields follow Master Project Brief, Section 10.1. AI output is not authoritative evidence; each member must be able to explain and modify anything below (Section 10). Verification entries that say "pending" are not verification; they must be completed by a team member and the result written here.

|ID|Date|Student|Tool|Task|AI contribution|Verification|Decision|Issues found|
|-|-|-|-|-|-|-|-|-|
|AI-01|09/09/2026|Markus Jooste|Copilot|Grammar and format check (M1)|Corrected grammar and spelling and table format.|Checked that only grammar and spelling changed.|Accepted|None|
|AI-02|30/09/2026|Markus Jooste|Copilot|Markdown ↔ Word conversion and grammar check (PED v2.0)|Converted between markdown and docx; corrected grammar.|Checked that only conversion and grammar changed.|Accepted|None recorded|
|AI-03|12/10/2026|Markus Jooste|Claude|Review of PED v2.0 against the M1, M2 and M3 briefs and the Master Brief|Identified inconsistencies, duplicate ADR IDs, lifecycle and data-model mismatches, missing evidence|||None|
|AI-04|12/10/2026|Markus Jooste|Claude|Drafting PED v2.1 sections, diagrams (ERD, FSM, sequence, wireframes), test specifications, weighted matrix|Drafted text, tables and figures; computed the matrix totals.|.NET 10 support date (14 Nov 2028) and PostgreSQL 18 support date checked against web sources during drafting; Npgsql provider 10.0.1 listing found on NuGet.||None|

\---

# 13\. References

Bass, L., Clements, P. and Kazman, R. (2021) *Software Architecture in Practice*. 4th edn. Boston, MA: Addison-Wesley.

Microsoft (2026a) *ASP.NET Core security and authorization documentation*. Available at: https://learn.microsoft.com/aspnet/core/security/ (Accessed: 12 October 2026).

Microsoft (2026b) *Entity Framework Core documentation: migrations and handling concurrency conflicts*. Available at: https://learn.microsoft.com/ef/core/ (Accessed: 12 October 2026).

Microsoft (2026c) *.NET support policy*. Available at: https://dotnet.microsoft.com/platform/support/policy (Accessed: 12 October 2026).

NuGet (2026) *Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1*. Available at: https://www.nuget.org/packages/npgsql.entityframeworkcore.postgresql/10.0.1 (Accessed: 12 October 2026).

PostgreSQL Global Development Group (2026a) *Versioning policy*. Available at: https://www.postgresql.org/support/versioning/ (Accessed: 12 October 2026).

PostgreSQL Global Development Group (2026b) *PostgreSQL documentation: transactions and constraints*. Available at: https://www.postgresql.org/docs/ (Accessed: 12 October 2026).

Richards, M. and Ford, N. (2020) *Fundamentals of Software Architecture: An Engineering Approach*. Sebastopol, CA: O'Reilly Media.

W3C (2023) *Web Content Accessibility Guidelines (WCAG) 2.2*. Available at: https://www.w3.org/TR/WCAG22/ (Accessed: 12 October 2026).

\---

# Appendix A. Items the team must complete before submission

Every open item in this document is listed here by group so none is missed. Do not leave a placeholder in a submitted document, and do not fill one with anything that did not happen.

|#|Group|What to do|
|-|-|-|
|1|Review and approval|Open a PR for this document; obtain two non-author approvals; then complete Section 0.1 (PR numbers), the CR and ADR status lines, and the sign-off in Section 11.4. Change ADR and CR status to Accepted or Approved only when that is true.|
|2|Decisions to confirm|CL-05 mandatory fields; AS-05 staff visibility; who may Accept and Reject (T1, T2); OD-05 reopen; OD-07 on-behalf requesters. Ask the client or lecturer, and record the answer.|
|3|Technology evidence|Confirm every matrix score as a team (Section 7.3); state real team experience (7.4); pin SDK, Npgsql and xUnit versions; re-check the .NET and PostgreSQL support dates on the official pages; cite licence pages; record the BC Desktop availability check (AS-07, R-14).|
|4|A2 references|In ADR-001, ADR-002, ADR-005, ADR-006, ADR-007 and ADR-008 and the note in Section 8, cite the A2 section or finding, and say whether the M2 decision matched or differed from the A2 recommendation. Do not copy A2 text.|
|5|Development evidence|Deferred to PED v3.0. Add the real repository structure, bootstrap, migration, FR-011 vertical slice (PR, files, commit), and a test run, and complete the two evidence rows in Section 10.4. If something does not exist yet, say so plainly rather than claim it.|
|6|GitHub evidence|Deferred to PED v3.0. Add branch protection, PR list including documents, one genuine review thread, board link, secrets scan, and commit history per member.|
|7|RTM|Update the controlled RTM file in the repository (not only this snapshot). Change Impl. and Status only where evidence exists. Record its path (Section 4).|
|8|AI register|Complete AI-03 and AI-04 with who, how each claim was verified, and what was changed or rejected.|
|9|Consistency check|Before sign-off, one member reads Sections 4, 6, 8 and 10 against each other (R-17): states and transitions, entity and column names, ADR numbers, test IDs.|
|10|Presentation|Each member should be able to trace one requirement end to end, defend one ADR including its alternatives, and explain what is still unproven (M2 brief, Sections 14 and 15).|