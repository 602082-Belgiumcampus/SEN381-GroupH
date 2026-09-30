# SEN381 Milestone 1

## Group H

- Migal Groenewald
- Nuvaran Reddy
- Markus Jooste

---

**SEN381 · SOFTWARE ENGINEERING 381 · CIVICCONNECT PROJECT**

**Milestone 1 — Engineering Foundation**

*Problem & Business Need · Stakeholder Analysis · Stakeholder Needs · Scope Baseline · Constraints & Trade-offs*

Migal Groenewald

Nuvaran Reddy

Markus Jooste

SEN381 · SOFTWARE ENGINEERING 381 · CIVICCONNECT PROJECT

Milestone 1 — Engineering Foundation

Problem & Business Need · Stakeholder Analysis · Stakeholder Needs · Scope Baseline · Constraints & Trade-offs

> Purpose of this document<br>This document is part of the CivicConnect PED v1.0 baseline. It covers the foundation for the project: the problem and business need, the stakeholders and their needs, the scope, and the constraints and trade-offs. The detailed requirements, RTM, Risk Register, Decision Log and AI Usage Register are kept as separate parts of the same PED.

# 1. Problem & Business Need

## 1.1 The problem

The client is a community organisation that handles service requests , things like facility faults, damaged equipment, security concerns, IT support, maintenance and lost property. Today these come in through a mix of email, phone, WhatsApp, spreadsheets and paper, with no single record following a request from start to finish. Because the work is spread across these informal channels, requests get lost or duplicated, requesters cannot see progress, staff struggle to prioritise and to know who owns what, changes are not tracked,and managers cannot get reliable figures. Reporting is manual and sensitive information is not handled safely.

## 1.2 Problem statement

> The organisation has no single, reliable record of a service request from start to finish. Requests are spread across separate, informal channels, so work gets lost, duplicated or given to the wrong person, requesters cannot track progress, no one is clearly accountable, management figures are unreliable, and sensitive information is not kept safe.

## 1.3 Business need

The organisation needs one digital platform to log, manage, track and report on service requests. It must let requesters see their own requests, let staff take ownership and move work through set steps, and give managers dependable information they can filter. It must do all this without being too expensive or too complex for a small organisation to run — success is about being reliable and controlled, not about being big.

## 1.4 Business value

The value of CivicConnect is not the software itself, but the results it makes possible. Each problem below maps to a clear business value , and each value belongs to a specific group of stakeholders .

| Current problem | What it causes | Value of fixing it | Who benefits |
| --- | --- | --- | --- |
| Requests spread across email, phone, WhatsApp, spreadsheets and paper | Lost, duplicated or mis-assigned requests | One record for every request | Organisation / Staff |
| Requesters cannot see progress | Repeat calls, chasing, low trust | Requesters can check status and history themselves | Requesters |
| No clear ownership or priority | Slow, uneven response | Clear assignment and set status steps | Staff or Supervisors |
| Changes are not tracked | Disputes; no record of who did what | A traceable record of actions | Management |
| Manual, inconsistent reporting | Managers cannot see open or overdue work | Reliable, filterable figures | Management |
| Sensitive requests handled inconsistently | Privacy and reputation risk | Role-based access to request data | Organisation / Requesters |

# 2. Stakeholder Analysis

Stakeholders are the people and groups whose needs shape the system, who decide whether it succeeds, or who set the rules for the project. We look at their role, how much influence and interest they have, and — most importantly, where there wants clash, because those clashes drive many of the decisions later in this document.

## 2.1 Stakeholder register

| ID | Stakeholder | Type | Role and interest | Influence | Interest |
| --- | --- | --- | --- | --- | --- |
| ST-1 | Requesters / Community members | External | Submit requests, track status and history, get feedback. | Low–Med | High |
| ST-2 | Service / Operational staff | Internal | View, search, take ownership, action and close requests. | Medium | High |
| ST-3 | Supervisors / Team leads | Internal | Assign and coordinate work; monitor progress. | Medium | High |
| ST-4 | Management / Oversight | Internal | Need reliable figures for accountability and performance. | High | High |
| ST-5 | Client organisation (sponsor) | External | Owns the problem; approves scope and defines success. | High | High |
| ST-6 | System administrator | Internal | Manage users, roles and categories. | Medium | Medium |
| ST-7 | Data subjects | External | People whose sensitive data appears in requests; want privacy. | Low | Med–High |
| ST-8 | Project team (3 students) | Internal | Build, secure, test, deploy and defend the solution. | High | High |
| ST-9 | Lecturer / Assessor | External | Set the engineering standards; review and approve the baseline. | High | Med |

Note. ST-1 to ST-7 are product stakeholders whose needs drive the requirements. ST-8 (our team) and ST-9 (the lecturer) are project stakeholders who set the rules we must follow — such as traceability, review and safe handling of data.

## 2.2 Where expectations clash

These are the clashes that most affect the requirements, scope and constraints. Each one is written down with how we handle it, so the decision can be explained and defended.

| ID | The clash | Tension | How we handle it |
| --- | --- | --- | --- |
| CF-1 | Requesters want to see all request detail, but sensitive data must not be exposed | Visibility vs. Privacy | Role-based access: requesters see their own requests; staff see only what their role allows. |
| CF-2 | Management want rich reports, but the team has limited time, money and skill | Insight vs. Deliverability | Provide clear status and category views now; leave advanced analytics for later (see §4.3). |
| CF-3 | Staff want fast, easy handling, but managers need accountability | Speed vs. Control | Use set status steps that record who did what — a small extra effort for a full audit trail (TO-4). |
| CF-4 | The client wants many features, but a small team cannot build everything well | Breadth vs. Quality | Prefer a smaller, solid solution over a bigger unfinished one; new features must justify their cost. |
| CF-5 | Requesters want free-form submission, but staff need structured data to prioritise | Ease vs. Priority | Use a simple category list and guided form — structured enough to sort, simple enough to use. |

> Hardest clash to solve<br>CF-1 (visibility vs. privacy) is the hardest, because both sides are fair and both are central to the project: the whole point is visibility, yet the same requests hold sensitive data. We cannot just pick one side, so we solve it with role-based access built into the design. That is why security is treated as something we handle throughout, not added at the end (see §5).

# 3. Stakeholder Needs

This turns the stakeholders into a clear list of needs. Each need has an ID so it can be traced to a requirement later. Priority uses MoSCoW: Must, Should or Could have.

## 3.1 Needs by role (requesters, staff, management)

| Need ID | Role | Need | Leads to (type) | Priority |
| --- | --- | --- | --- | --- |
| N-R1 | Requester | Submit a new request with the right information | Guided request form (FR) | Must |
| N-R2 | Requester | Choose a category from a set list | Category list (FR) | Must |
| N-R3 | Requester | See the current status of my requests | Status view (FR) | Must |
| N-R4 | Requester | See a history of my past requests | History view (FR) | Must |
| N-R5 | Requester | Get feedback when a request is accepted, rejected, updated or done | Status feedback (FR) | Should |
| N-S1 | Staff | See requests relevant to my role | Role-scoped lists (FR / security) | Must |
| N-S2 | Staff | Search, filter or sort requests | Search and filter (FR) | Must |
| N-S3 | Staff | See full request details | Detail view (FR) | Must |
| N-S4 | Staff | Take ownership of a request | Assignment (FR) | Must |
| N-S5 | Staff | Move a request through set status steps | Controlled status steps (FR) | Must |
| N-S6 | Staff | Record actions, comments or how it was resolved | Action logging (FR) | Must |
| N-S7 | Staff | Resolve or close requests where allowed | Authorised close (FR + access) | Must |
| N-M1 | Management | See useful service activity | Reporting views (FR) | Must |
| N-M2 | Management | Identify open, overdue, resolved and closed requests | Status / aging view (FR) | Must |
| N-M3 | Management | View requests by category, status or grouping | Filterable reports (FR) | Should |
| N-M4 | Management | Enough information for accountability and performance | Auditable record (FR / NFR) | Must |

## 3.2 Needs that cut across the whole system

Some needs are not owned by one role but apply to the whole platform. These usually become quality requirements (NFRs) rather than single features, and they protect the assessor's standards and the data subjects' privacy.

| Need ID | Need | Comes from | Type |
| --- | --- | --- | --- |
| N-X1 | Sensitive data is protected and access matches the user's role | ST-7, ST-5, ST-4 | Security / privacy (NFR) |
| N-X2 | Actions and status changes can be traced to a user | ST-4, ST-9 | Accountability (NFR) |
| N-X3 | The platform is dependable and available when needed | ST-1, ST-2, ST-5 | Reliability (NFR) |
| N-X4 | The platform is easy for non-technical users and busy staff | ST-1, ST-2 | Usability (NFR) |
| N-X5 | Running cost stays affordable for a small organisation | ST-5, ST-8 | Cost (constraint) |

> Traceability note<br>Each need ID (N-*) becomes the starting point of the RTM: Need → Requirement → Acceptance check → Design → Test → Release. Giving needs stable IDs now is what lets us prove later that every requirement comes from a real need.

# 4. Scope Baseline

This sets out what CivicConnect will and will not do, and what is left for later. Fixing this now lets us control the scope: any change made later is checked against this baseline before it is accepted.

## 4.1 In scope

These features are committed for this project. They cover the basic needs of requesters, staff and management, plus the controls the needs above require.

- Submit a request with the right information and a set category (N-R1, N-R2).

- Requester self-service ,  see status and a history of past requests (N-R3, N-R4).

- A set request lifecycle ,defined statuses and allowed steps from submission to close (N-S5).

- Staff handling — role-based lists, search/filter, detail view, ownership, action logging and authorised close (N-S1–N-S7).

- Role-based access for requester , staff, supervisor, management and admin roles (N-X1).

- Management reports showing open , overdue, resolved and closed requests by category or status (N-M1–N-M4).

- An audit record linking status changes and actions to the user who made them (N-X2).

- Admin of users, roles and the category list (ST-6).

## 4.2 Out of scope

These are left out of this project. Leaving them out does not mean they have no value — it means we are not committing to build, secure, test and maintain them within our limits.

| Left out | Why |
| --- | --- |
| Automatic pull-in from WhatsApp, email or SMS/phone | Brings back the very fragmentation we are trying to remove, and adds a lot of cost and security risk (see §4.4). |
| Advanced analytics / BI dashboards | More than the basic reporting the need requires; too much to build and maintain for a small team. |
| Native mobile apps (iOS / Android) | A responsive website meets the access need at much lower cost. |
| Automatic SLA escalation and paging | Needs reliable scheduling and notification setup that is not worth it at this stage. |
| Online payments | Not part of the service-request problem; would add heavy compliance work. |
| Public anonymous portal and multi-language | Not needed by the identified stakeholders; adds security and content work for little value. |
| AI auto-sorting of requests | Adds model and checking work; a simple manual category meets the need. |

## 4.3 For later

These are not built now but are sensible later additions. We note them so today's decisions keep the door open for them without needing rework.

- Notifications (email/SMS) on status changes, for now, feedback (N-R5) is shown in the platform.

- Analytics and trends built on the audit record once there is enough data.

- SLA / overdue escalation added on top of the existing status views.

- A mobile-first experience if take-up justifies it.

## 4.4 One exclusion, explained

> Left out: automatic pull-in from WhatsApp, email and SMS/phone<br>This is the most tempting feature, since those channels are where requests arrive today — so it is worth saying why we leave it out. It works against the whole point of the project: CivicConnect exists to create one controlled record of each request, and pulling in from several informal channels brings back the very fragmentation we are removing (N-X2). The cost and risk are also too high for a three-person team  each channel adds outside dependencies and a large security surface for sensitive data (CF-1, N-X1).<br>Instead, the real need — that no request is lost, however the person first made contact — is met by letting staff log a request on someone's behalf through the same single entry point. Channel pull-in is left for later, after we have checked its cost and security impact.

# 5. Constraints & Trade-offs

Constraints are the fixed limits we must work within. They matter because meeting one often costs something against another — that is a trade-off. This section lists the constraints and what each means for us, then the main trade-offs and their knock-on effects.

## 5.1 Constraints

| Constraint | The limit | What it means for CivicConnect |
| --- | --- | --- |
| Team size | Exactly 3 students; each is marked individually. | Scope and technology must fit what three people can build and explain. |
| Schedule | Four milestones in the SEN381 period; M1 first. | Scope must fit the time; deadline pressure must not quietly cut testing or security (TO-1). |
| Cost | Prefer free / low-cost services; note free-tier limits. | Free-tier limits affect availability, backups and security (TO-2). |
| Scope | Committed scope is fixed and controlled; no creep. | The §4 baseline plus change control; new items need an impact check first. |
| Quality | Quality must be shown with measurable evidence. | NFRs (reliability, usability, security) need measurable checks, not just claims. |
| Security | Handled throughout, not added at the end. | Access control and safe data handling are designed in from the start (N-X1). |
| Technology | No stack is set; choices must be justified. | Technology is a real decision (an ADR in M2), weighing skill, cost and support (TO-5). |

## 5.2 Trade-offs and knock-on effects

The constraints do not act on their own. Below is where meeting one costs something against another, and the knock-on effect for later milestones — the kind of reasoning the defence looks for.

| ID | Trade-off | Our decision and its knock-on effect |
| --- | --- | --- |
| TO-1 | Schedule vs. Quality & Security | Deadlines tempt us to cut testing or security. Decision: keep both as fixed must-haves and reduce scope instead (§4). Knock-on: limits how many features we commit in M2/M3. |
| TO-2 | Cost vs. Reliability & Security | Free services limit availability, storage and backups. Decision: accept a modest availability target and record the limits as risks. Knock-on: shapes the M2 technology choice and the M3 deployment plan. |
| TO-3 | Scope vs Quality | More features mean weaker evidence for a small team. Decision: a smaller scope done well (CF-4). Knock-on: the left-out items (§4.2–4.3) are first in line if time allows later. |
| TO-4 | Security and Accountability vs Ease of use | Set status steps and access control add friction. Decision: accept the friction to guarantee an audit trail (CF-3, N-X2). Knock-on: the M2 design must keep that friction small. |
| TO-5 | Technology vs. Team skill | A new but strong tool risks the deadline; a familiar but weak one risks quality. Decision: settle it with a justified M2 decision record. Knock-on: technology risk stays on the Risk Register until then. |

## 5.3 Example :a change after the baseline

Say the client adds a big feature after sign-off — for example, automatic notifications — but keeps the same deadline. This is a knock-on effect in action: the change cannot just be absorbed. It goes through change control and gets an impact check across requirements, scope, schedule, quality and security. Because the deadline and team size are fixed, the trade-off is clear: either we drop other scope to protect quality and security, or the deadline has to move. The decision and its reason go in the Decision Log.

# Functional Requirements

The functional requirements define the stakeholder needs identified in Section 3 in terms of system functions that can be implemented and verified. Each requirement has a unique identifier, priority, and acceptance criterion so that it can be traced through later design, implementation, testing, and release.

The requirements are deliberately limited to the approved scope baseline. Features identified as out of scope or for later development are not included as baselined requirements. This prevents the requirements from expanding beyond what the three-person team can realistically build, secure, test, and maintain.

The priority uses MoSCoW classification:

- Must: essential to satisfy the agreed business needs and project scope.

- Should: important, but the system could still provide its core purpose without it.

- Could: desirable but not currently required for the baseline.

## 6.1 Requester Requirements

| ID | Stakeholder Need | Functional Requirement | Priority |
| --- | --- | --- | --- |
| FR-001 | N-R1 | The system must allow a requester to submit a service request containing all required information. | Must |
| FR-002 | N-R2 | The system must require each service request to be assigned to a category from the controlled category list. | Must |
| FR-003 | N-R1 / N-X2 | The system must provide each successfully submitted service request with a unique request reference that can be used to identify the request. | Must |
| FR-004 | N-R3 | The system must allow a requester to view the current status of their submitted service requests. | Must |
| FR-005 | N-R4 | The system must allow a requester to view a history of their previously submitted service requests. | Must |
| FR-006 | N-R5 | The system should provide meaningful feedback within the platform when a request is accepted, rejected, updated or completed. | Should |

### Requirement rationale

FR-001 and FR-002 directly support the need for a structured request-entry process. This addresses the existing problem of requests being spread across email, telephone, WhatsApp, spreadsheets, and paper records.

FR-003 is a team-derived requirement rather than a direct quotation from the stakeholder analysis. It follows the need for a single controlled and traceable record of each request and supports later accountability and traceability.

FR-004 to FR-006 address the requester's lack of visibility identified in the problem analysis. The scope baseline specifically commits the project to requester self-service for status and request history, while platform-based feedback is included without committing the project to future email/SMS notifications.

## 6.2 Staff Requirements

| ID | Stakeholder Need | Functional Requirement | Priority |
| --- | --- | --- | --- |
| FR-007 | N-S1 | The system must allow authorized staff to view service requests that are relevant to their assigned role and permissions. | Must |
| FR-008 | N-S2 | The system must allow authorized staff to search, filter and sort service requests using available request information. | Must |
| FR-009 | N-S3 | The system must allow authorized staff to view the details of a service request that they are permitted to access. | Must |
| FR-010 | N-S4 | The system must allow authorized staff to assign or accept responsibility for a service request. | Must |
| FR-011 | N-S5 | The system must allow authorized staff to update a request only through the defined request-status transitions. | Must |
| FR-012 | N-S6 | The system must allow authorized staff to record relevant actions, comments, and resolution of information against a service request. | Must |
| FR-013 | N-S7 | The system must allow authorized staff to resolve or close a service request where they have permission to do so. | Must |

These requirements resolve the issues identified in the stakeholder analysis, namely, the lack of ownership, prioritization, inconsistent management, and accountability.

The use of controlled status transitions in FR-011 is purposeful. The project is not simply providing a database of requests, but a controlled lifecycle for each request, and this also meets the stakeholder's requirement that actions and transitions can be traceable.

## 6.3 Management and Oversight Requirements

| ID | Stakeholder Need | Functional Requirement | Priority |
| --- | --- | --- | --- |
| FR-014 | N-M1 | The system must provide authorized management for users with service activity information for the requests within their permitted scope. | Must |
| FR-015 | N-M2 | The system must allow authorized management users to identify requests that are open, overdue, resolved, or closed. | Must |
| FR-016 | N-M3 | The system should allow authorized management users to view or filter request information by category and status. | Should |
| FR-017 | N-M4 | The system must provide authorized management for users with information sufficient to support service accountability and performance analysis. | Must |

The requirements deliberately focus on the reporting capabilities already established in the scope baseline. Advanced analytics and business-intelligence dashboards are excluded from the current project scope.

## 6.4 Access and Accountability Requirements

| ID | Stakeholder Need | Functional Requirement | Priority |
| --- | --- | --- | --- |
| FR-018 | N-X1 | The system must restrict protected request information and system functions to users with the required permissions. | Must |
| FR-019 | N-X2 | The system must associate significant request actions and status changes with the authorized user who performed them. | Must |

FR-018 directly addresses the hardest stakeholder conflict identified in Section 2.2: visibility versus privacy. Requesters need visibility of their own requests, while sensitive information must not become visible to users without appropriate access.

FR-019 supports management accountability and creates the basis for an auditable record of request activity.

# Non-Functional Requirements

The non-functional requirements define the quality expectations and operational controls that apply across CivicConnect. They are derived primarily from the cross-cutting stakeholder needs N-X1 to N-X5, together with the constraints and trade-offs identified by the team.

The requirements are intentionally measurable where a useful target can be established. Where the exact implementation mechanism is not yet known, the requirement describes the required outcome rather than prematurely specifying an architecture or technology.

## 7.1 Security and Privacy

| ID | Stakeholder Need | Non-Functional Requirement | Priority |
| --- | --- | --- | --- |
| NFR-001 | N-X1 | The system must prevent unauthorized users from accessing protected request information and protected functionality. | Must |
| NFR-002 | N-X1 | The system must enforce authorization at protected operations and shall not rely solely on the user interface to restrict access. | Must |
| NFR-003 | N-X1 | Application secrets, credentials and other sensitive configuration values must not be stored directly in source-controlled application code. | Must |
| NFR-004 | N-X1 / N-X2 | Security-relevant access-control and authorization behavior must have documented verification evidence before release. | Must |

These requirements reflect the project's decision that security must be treated throughout the lifecycle rather than added at the end. The Master Brief specifically requires early consideration of authentication, authorization, least privilege, secrets of protection and verification of relevant security behavior.

## 7.2 Performance

| ID | Stakeholder Need | Non-Functional Requirement | Priority |
| --- | --- | --- | --- |
| NFR-005 | N-X3 | Under the agreed representative test workload, at least 95% of normal request-view and request-list operations should be completed within 2 seconds. | Should |
| NFR-006 | N-X3 | Under the agreed representative test workload, at least 95% of valid request submissions should be completed within 3 seconds, excluding delays caused by unavailable external services. | Should |

The performance values are proposed engineering targets, not claims that the system has already demonstrated these results. The final workload and test method should be agreed before performance verification.

The targets provide measurable expectations without forcing a particular technology or architecture during Milestone 1.

## 7.3 Reliability and Traceability

| ID | Stakeholder Need | Non-Functional Requirement | Priority |
| --- | --- | --- | --- |
| NFR-007 | N-X3 | A successfully submitted service request must not be silently lost and shall receive a persistent request of reference. | Must |
| NFR-008 | N-X2/N-X3 | The system must retain the status and significant action history associated with each service request for the agreed retention period. | Must |

NFR-007 addresses the central business problem of requests being lost between informal channels. NFR-008 supports the requirement for an auditable record of request activity.

## 7.4 Usability and Validation

| ID | Stakeholder Need | Non-Functional Requirement | Priority |
| --- | --- | --- | --- |
| NFR-009 | N-X4 | A representative requester must be able to submit a valid service request without assistance during the agreed usability test. | Must |
| NFR-010 | N-X4 | A representative authorized staff user should be able to locate a specified request using the available search/filter functionality during the agreed usability test. | Should |
| NFR-011 | N-X4 | The request entry process must identify missing or invalid mandatory information before a request is successfully submitted. | Must |

These requirements support the stakeholder expectation that the platform should be usable by community members and busy operational staff rather than becoming another source of administrative burden.

## 7.5 Maintainability, Testability and Change Control

| ID | Stakeholder Need | Non-Functional Requirement | Priority |
| --- | --- | --- | --- |
| NFR-012 | N-X3/N-X4 | Every baselined implemented requirement must have recorded verification of evidence before the corresponding functionality is accepted for release. | Must |
| NFR-013 | N-X2/N-X4 | Changes to baselined requirements must be recorded through the project's change control process and reflected in the relevant requirements and traceable artefacts. | Must |
| NFR-014 | N-X3 | Critical service-request workflows should have automated verification where practical before release. | Should |

NFR-012 and NFR-013 support the project's engineering-control requirements. The Master Brief requires quality claims to be supported by evidence traceable to requirements, quality attributes and risks, while baseline changes must follow controlled impact analysis and verification.

## 7.6 Operational Requirements

| ID | Stakeholder Need | Non-Functional Requirement | Priority |
| --- | --- | --- | --- |
| NFR-015 | N-X3 | The system should provide sufficient logging to support investigation of significant request-processing or system failures. | Should |
| NFR-016 | N-X1 | Environment specific configuration and secrets must be separated from application source code and controlled using an appropriate configuration mechanism. | Must |
| NFR-017 | N-X3 | A documented recovery or rollback consideration should exist before production deployment. | Should |

These requirements are deliberately phrased without selecting a specific deployment platform or implementation of technology. The Master Brief requires environment-specific configuration and secrets to be controlled and requires rollback/recovery to be considered before production release.

# Acceptance Criteria

Acceptance criteria define the observable conditions that must be satisfied with a requirement to be considered met. They are written so that the team can later produce objective verification evidence rather than relying on a statement that the system "works."

The acceptance criteria below use a Given/When/Then structure where practical.

## 8.1 Functional Requirement Acceptance Criteria

| AC ID | Requirement | Acceptance Criterion |
| --- | --- | --- |
| AC-001 | FR-001 | Given that a requester provides all mandatory information, when they submit the request, then the system creates the request and provides its unique request reference. |
| AC-002 | FR-002 | Given a requester attempts to submit a request without selecting a valid category, when they submit the request, then submission is stopped, and the missing/invalid category is identified. |
| AC-003 | FR-003 | Given a service request has been successfully created, when the system displays the request, then a unique request reference is available for identifying it. |
| AC-004 | FR-004 | Given that a requester has submitted a request, when they access their request, then the current lifecycle status is displayed. |
| AC-005 | FR-005 | Given a requester has previously submitted one or more requests, when they access their request history, then their previous requests are listed and identifiable. |
| AC-006 | FR-006 | Given a request reaches an agreed feedback event, when the event occurs, then meaningful feedback about that event is available to the requester within the platform. |
| AC-007 | FR-007 | Given two staff users with different permissions, when each views the request list, then each user can only access requests permitted by their role and permissions. |
| AC-008 | FR-008 | Given an authorized staff user selects a search, filter or sort criterion, when the operation is performed, then the displayed results reflect the selected criterion. |
| AC-009 | FR-009 | Given an authorized staff user selects a permitted request, when they open it, then the relevant request details are displayed. |
| AC-010 | FR-010 | Given an authorized staff user assigns or accepts responsibility for a request, when the action is completed, then the request records the responsible staff member. |
| AC-011 | FR-011 | Given a request is in a defined lifecycle state, when a staff user attempts to change its status, then only an allowed status transition can be completed. |
| AC-012 | FR-012 | Given an authorized staff user records an action, comment or resolution, when the information is saved, then it remains associated with the relevant request. |
| AC-013 | FR-013 | Given an authorized staff user has permission to close a request, when the required resolution information is provided and the request is closed, then the request is recorded as resolved/closed according to the defined lifecycle. |
| AC-014 | FR-014 | Given an authorized management user selects the agreed reporting period or view, when the service activity view is displayed, then it provides information about relevant requests for that period. |
| AC-015 | FR-015 | Given requests exist in different lifecycle states, when an authorized management user views service activity, then open, overdue, resolved and closed requests can be identified according to the defined rules. |
| AC-016 | FR-016 | Given an authorized management user selects a category or status filter, when the filter is applied, then the displayed request information reflects the selected category or status. |
| AC-017 | FR-017 | Given an authorized management user reviews service activity, when they access the relevant management information, then the available information supports the agreed accountability and performance-analysis needs. |
| AC-018 | FR-018 | Given that a user does not have permission to access a protected request or function, when they attempt to access it, then access is rejected, and protected information is not disclosed. |
| AC-019 | FR-019 | Given an authorized user performs a significant request of action or status change, when the action is recorded, then the responsible user can be identified from the audit record. |

## 8.2 Non-Functional Requirement Acceptance Criteria

| AC ID | Requirement | Acceptance Criterion |
| --- | --- | --- |
| AC-020 | NFR-001 | Security testing demonstrates that unauthorized users cannot access protected request information or functionality. |
| AC-021 | NFR-002 | Security testing demonstrates that protected operations enforce authorization independently of client-side interface restrictions. |
| AC-022 | NFR-003 | Repository/configuration inspection demonstrates that application secrets and credentials are not committed as source-controlled values. |
| AC-023 | NFR-004 | Security verification records contain tests/evidence covering the agreed security relevant access-control behavior. |
| AC-024 | NFR-005 | Under the agreed performance workload, recorded test results show at least 95% of normal request-view/list operations completed within 2 seconds. |
| AC-025 | NFR-006 | Under the agreed performance workload, recorded test results show at least 95% of valid request submissions completed within 3 seconds, excluding agreed external-service delays. |
| AC-026 | NFR-007 | A successful request submission results in a persistent request record and request reference, and a verification test confirms that the submitted request can subsequently be retrieved. |
| AC-027 | NFR-008 | Verification demonstrates that the agreed request status and significant-action history remain available after subsequent request activity. |
| AC-028 | NFR-009 | During the agreed usability test, the representative requester successfully submitted a valid request without assistance. |
| AC-029 | NFR-010 | During the agreed usability test, the representative staff user successfully locates the specified request using the available search/filter functionality. |
| AC-030 | NFR-011 | Validation testing demonstrates that missing or invalid mandatory information is identified before a request is successfully submitted. |
| AC-031 | NFR-012 | The final requirements/test evidence review shows verification of evidence linked to every implemented baselined requirement before release acceptance. |
| AC-032 | NFR-013 | A sample approved requirement change demonstrates that the change record, affected requirements and RTM were updated before the changed functionality was accepted. |
| AC-033 | NFR-014 | Automated test evidence demonstrates successful verification of the agreed critical service request workflows. |
| AC-034 | NFR-015 | An induced or observed significant failure produces sufficient log information to support identification and investigation of the failure. |
| AC-035 | NFR-016 | Deployment/configuration inspection demonstrates that environment-specific secrets and configuration are not hard-coded in application source code. |
| AC-036 | NFR-017 | Before production release, the project contains documented evidence showing how rollback or recovery would be considered for the release. |

# RTM

The Requirements Traceability Matrix (RTM) is the controlled derivation of the stakeholder's need and the evidence that will ultimately demonstrate that the requirement was engineered and verified.

The Master Project Brief requires that requirements have an identifier, source, testable statement, acceptance criteria, priority/status, and traceability. It also requires that the engineering record be built across the project milestones, not as a set of independent documents.

Architecture design, implementation, and test evidence fields are not required for milestone 1, so they are TBD (to be determined). This means that these fields will be populated during later project phases (Milestone 2 to Milestone 4) as the work progresses.

## 9.1 Functional Requirements Traceability

| Req ID | Stakeholder Need | Requirement | Priority | Acceptance | Design / Architecture | Issue / PR | Implementation | Test Evidence | Release Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| FR-001 | N-R1 | Submit request with required information | Must | AC-001 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-002 | N-R2 | Controlled request category | Must | AC-002 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-003 | N-R1 / N-X2 | Unique request reference | Must | AC-003 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-004 | N-R3 | View current request status | Must | AC-004 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-005 | N-R4 | View request history | Must | AC-005 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-006 | N-R5 | Platform feedback at lifecycle events | Should | AC-006 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-007 | N-S1 | Role-relevant staff request access | Must | AC-007 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-008 | N-S2 | Search, filter and sort requests | Must | AC-008 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-009 | N-S3 | View request details | Must | AC-009 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-010 | N-S4 | Assign/accept responsibility | Must | AC-010 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-011 | N-S5 | Controlled request status transitions | Must | AC-011 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-012 | N-S6 | Record actions/comments/resolution | Must | AC-012 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-013 | N-S7 | Authorized resolve/close | Must | AC-013 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-014 | N-M1 | Management service activity | Must | AC-014 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-015 | N-M2 | Identify open/overdue/resolved/closed | Must | AC-015 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-016 | N-M3 | Category/status management views | Should | AC-016 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-017 | N-M4 | Accountability/performance information | Must | AC-017 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-018 | N-X1 | Restrict protected information/functions | Must | AC-018 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| FR-019 | N-X2 | Associate actions/status changes with user | Must | AC-019 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |

## 9.2 Non-Functional Requirements Traceability

| Req ID | Stakeholder Need | Requirement Area | Priority | Acceptance | Design / Architecture | Issue / PR | Implementation | Test Evidence | Release Evidence | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| NFR-001 | N-X1 | Unauthorized access prevention | Must | AC-020 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-002 | N-X1 | Operation-level authorization | Must | AC-021 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-003 | N-X1 | Secrets not in source control | Must | AC-022 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-004 | N-X1 / N-X2 | Security verification | Must | AC-023 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-005 | N-X3 | Request-view/list performance | Should | AC-024 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-006 | N-X3 | Request-submission performance | Should | AC-025 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-007 | N-X3 | No silent loss of submitted requests | Must | AC-026 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-008 | N-X2 / N-X3 | Status/action history retention | Must | AC-027 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-009 | N-X4 | Requester usability | Must | AC-028 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-010 | N-X4 | Staff search usability | Should | AC-029 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-011 | N-X4 | Input validation | Must | AC-030 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-012 | N-X3 / N-X4 | Verification evidence | Must | AC-031 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-013 | N-X2 / N-X4 | Controlled requirement changes | Must | AC-032 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-014 | N-X3 | Automated critical-workflow verification | Should | AC-033 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-015 | N-X3 | Failure logging | Should | AC-034 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-016 | N-X1 | Environment configuration/secrets | Must | AC-035 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |
| NFR-017 | N-X3 | Recovery/rollback consideration | Should | AC-036 | TBD M2 | TBD M3 | TBD M3 | TBD M3 | TBD M4 | Baselined |

## 9.3 End-to-End Traceability Example

The following demonstrates how a requirement will eventually be traced across the engineering lifecycle.

N-R1 -> FR-001 -> AC-001 -> Design -> Implementation -> Test -> Release

Stakeholder Need:  
 N-R1 -> The requester needs to submit a new request with the right information.

Functional requirement:  
 FR-001 -> The system shall allow a requester to submit a service request containing all required information.

Acceptance criterion:  
 AC-001 -> Given a requester provides all mandatory information, when they submit the request, then the system creates the request and provides its unique request reference.

Milestone 2 -> design/architecture:  
 TBD -> linked to the relevant architecture/design artefact.

Milestone 3 -> issue/PR and implementation:  
 TBD -> linked to the GitHub issue, pull request and implemented change.

Milestone 3 -> test evidence:  
 TBD -> linked to the test demonstrating successful request submission.

Milestone 4 -> release/acceptance evidence:  
 TBD -> linked to the final release or stakeholder acceptance evidence.

# Risk Register

| ID | Risk | Probability | Impact | Priority | Mitigation | Contingency | Owner | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| R-01 | We underestimate the amount of work required | High | High | Critical | Keep the M1 scope controlled; prioritise Must requirements; track work through GitHub issues | Remove/defer lower-priority features before reducing testing or security work | Markus | Open |
| R-02 | Technology selected in M2 is unfamiliar or difficult to deploy | Medium | High | High | Evaluate team skills, | Select the strongest viable alternative and reduce technology-specific scope if necessary | Migal | Open |
| R-03 | Sensitive request information is exposed to users who should not have access | Medium | High | High | Design role-based access from the requirements stage; identify sensitive data and access boundaries before implementation | Restrict affected access, investigate the exposure and correct the access rule before release | Markus | Open |
| R-04 | Requirements or scope change | Medium | High | High | Baseline requirements and use formal change control with impact analysis | Reassess schedule/scope and either defer the change or remove equivalent lower-priority scope | Nuvaran | Open |
| R-05 | GitHub history does not provide sufficient evidence of authentic individual contribution | Medium | High | High | Use issues, branches, meaningful commits, PRs and peer reviews throughout development | Reconstruct understanding through available repository evidence and correct governance before further work | Everyone | Open |
| R-06 | Security or quality work is postponed because of deadline pressure | Medium | High | High | Treat security and quality as fixed constraints; reduce feature scope instead | Defer non-essential functionality rather than removing required security/testing activities | Everyone | Open |
| R-07 | AI-generated material is accepted without sufficient human verification | Medium | Medium | Medium | Maintain an AI Usage Register and require human review and verification of material AI-assisted work | Re-review affected artefacts and replace unsupported or incorrect material | Everyone | Open |
| R-08 | Free-tier services introduce availability, storage or backup limitations | Medium | Medium | Medium | Record service limitations during technology evaluation and deployment planning | Change service/platform or adjust availability expectations before production deployment | Nuvaran | Open |

# Forward Engineering Register

| ID | Concern | Why it matters now | Downstream implication |
| --- | --- | --- | --- |
| FE-01 | Architecture and system boundaries | The system needs requester, staff, supervisor, management and administrator capabilities with different access rules | M2 architecture must preserve clear boundaries between these responsibilities and avoid unnecessary coupling |
| FE-02 | Technology and platform selection | The final technology must fit team skills, cost, institutional availability and deployment requirements | M2 must evaluate alternatives using evidence rather than familiarity alone |
| FE-03 | Data model and sensitive information | Requests contain information that may require different access permissions | Data structures and access controls must support role-based visibility without exposing sensitive information |
| FE-04 | Authentication and authorization | Different stakeholders require different capabilities | M2 design must distinguish authentication from authorization and support the defined roles |
| FE-05 | Testing and verification strategy | Requirements are expected to be testable and traceable | Later implementation must allow functional, security and quality requirements to be verified with evidence |
| FE-06 | Deployment and operational readiness | A working development system is not automatically a deployable system | M3/M4 must consider environment configuration, monitoring, backup/recovery, availability and rollback |
| FE-07 | Maintainability and future change | Notifications, analytics, SLA escalation and mobile access may be introduced later | Current design should avoid unnecessarily preventing these future capabilities without building them now |

# Decision Log

| ID | Milestone | Decision | Reason | Alternatives considered | Consequence / follow-up |
| --- | --- | --- | --- | --- | --- |
| D-001 | M1 | Keep the scope focused on the core service request lifecycle | A smaller controlled solution is more achievable for three students than a large unfinished system | Build all requested future features immediately | Notifications, advanced analytics and mobile functionality remain deferred |
| D-002 | M1 | Protect main and require peer review for substantive changes | The repository must provide auditable evidence of collaborative engineering | Direct development on main | Substantive work uses branches and PRs, two other team members approve before merge |
| D-003 | M1 | Defer final technology stack selection to M2 | M1 does not yet contain sufficient evidence about technology, deployment, cost and team-skill implications | Select a familiar technology immediately | M2 must perform an evidence based technology evaluation |
| D-004 | M1 | Defer final architecture selection to M2 | Architecture should respond to baselined requirements and quality drivers | Select an architecture based primarily on familiarity | M2 architecture work must use M1 requirements, constraints and risks as inputs |
| D-005 | M1 | Treat security and quality as protected project constraints | Deadline pressure must not result in required security or verification work being removed | Reduce testing/security to complete more features | Lower-priority functionality is reduced or deferred first |

# Team Working Agreement

| Area | Agreement |
| --- | --- |
| Roles | The work will be divided between the three members in M2, but everyone remain collectively responsible for the final system |
| Communication | Whatsapp and Github |
| Response time | Members should respond to project communication within the day |
| Meetings | The team weekly check-ins to review progress, blockers, risks and upcoming work |
| Task allocation | Work is represented using GitHub Issues/tasks and assigned to an appropriate member |
| Branches | Substantive work is completed on a dedicated branch rather than directly on main |
| Pull Requests | Substantive changes enter main through a PR |
| Peer review | PRs require meaningful review and two approvals from team members other than the author |
| Documentation | Important engineering decisions and controlled artefacts are maintained in the repository |
| Disagreements | Disagreements are resolved using requirements, constraints, evidence and project objectives rather than personal preference |
| Quality | Members are expected to identify problems during review rather than approving work simply to keep progress moving |
| Accountability | Each member must be able to explain their own contribution and understand the overall project |

# GitHub

https://github.com/602082-Belgiumcampus/SEN381-GroupH



# Technology
## Technology Selection

The project requires a web-based application with authenticated users, role-based access, structured service-request data, reporting, auditability and a persistent relational database.

The proposed baseline technology stack is:

| Area | Selected Technology | Purpose |
|---|---|---|
| Application framework | ASP.NET Core 10 | Web application and HTTP/API functionality |
| Programming language | C# | Application and domain development |
| UI | ASP.NET Core Razor Pages / MVC | Server-rendered web interface |
| Runtime | .NET 10 LTS | Application runtime |
| ORM / data access | Entity Framework Core 10 | Database access and migrations |
| Database | PostgreSQL | Persistent relational storage |
| Authentication / authorization | ASP.NET Core Authentication & Authorization | User identity and role/policy access control |
| Testing | xUnit | Automated unit testing |
| Source control | Git / GitHub | Version control, collaboration and review |
| Configuration | ASP.NET Core configuration + environment variables | Environment-specific configuration and secrets |
| Deployment | Container-compatible web deployment | Consistent deployment environment |
| API/interface | ASP.NET Core HTTP endpoints where required | Internal/external system integration |

ASP.NET Core provides built-in support for web applications, APIs, dependency injection, configuration, authentication and authorization. It also supports MVC and other web UI approaches within the same platform.

.NET 10 was selected rather than .NET 8 because .NET 10 is the current LTS release and provides a longer supported baseline for the project.

## Database

PostgreSQL was selected as the persistence technology because CivicConnect's core data is relational:

- users have roles;
- requests have categories and statuses;
- requests have requesters;
- staff may be assigned to requests;
- requests have comments/history;
- significant actions must be traceable;
- management reporting requires structured queries.

A relational database therefore fits the domain better than introducing a document-oriented database.

Entity Framework Core will provide the application data-access layer. EF Core migrations allow database schema changes to be represented as source-controlled migration files, allowing the database schema to evolve alongside the application.

---

## Alternatives Considered

The team considered several possible approaches before establishing the technology baseline.

| Decision | Alternative | Selected | Reason |
|---|---|---|---|
| Backend | Node.js/Express | ASP.NET Core | Strong built-in security, authentication, authorization, DI and testing support |
| Backend | Django/Python | ASP.NET Core | C#/.NET provides a unified language/runtime for the proposed application |
| UI | React SPA | Razor Pages/MVC | Lower architectural complexity for a small team and CRUD/reporting-oriented system |
| Database | MongoDB | PostgreSQL | CivicConnect has strongly relational data and reporting requirements |
| Database | MySQL | PostgreSQL | Mature relational database with strong SQL capabilities |
| Data access | Raw SQL | EF Core | Strong integration with .NET and source-controlled migrations |
| API | Separate API project | ASP.NET Core endpoints within application | Avoids unnecessary service separation at current scope |
| Hosting | Complex cloud architecture | Simple container-compatible deployment | Keeps cost and operational complexity proportional to the project |

The team deliberately avoided introducing a microservices architecture. CivicConnect does not currently have the scale, independent deployment requirements or integration complexity that would justify splitting the system into multiple independently deployed services.

The architecture therefore favours a **modular monolithic web application**.

---

## Technology Decision

The selected technology baseline is:

> **ASP.NET Core 10 + C# + Razor Pages/MVC + Entity Framework Core + PostgreSQL**

This combination provides one primary development ecosystem rather than requiring the three-person team to maintain multiple application stacks.

## Authentication and Authorization

Authentication and authorization are treated as separate concerns.

Authentication determines who the user is, while authorization determines what that user is allowed to do.

CivicConnect requires role-scoped access because M1 identified privacy and access control as important stakeholder concerns.

The initial roles are:

|Role |	Intended Access |
|---|---|
|Requester|	Create and view their own requests|
|Service Staff|	View and manage requests assigned/relevant to them|
|Supervisor|	Manage operational requests and staff activity|
|Management|	Access reporting and oversight functions|
|Administrator|	Manage users, roles and system configuration|


## Configuration and Secrets

Environment-specific configuration will not be hard-coded into the application.

The application will use environment configuration for values such as database connection strings, authentication configuration, deployment-specific settings, development/test/production differences.

Secrets must not be committed to GitHub.
.env.example may document the required configuration keys, but actual credentials must remain outside source control.

# Research-Informed Design Decisions & Integration
## Design Problem 1: Role-Based Access Is Not Enough

Problem:
CivicConnect contains sensitive service-request information.

M1 identified the following conflict:
CF-1 — Visibility vs Privacy

The system must allow users to access the information they need while preventing unauthorized access to protected request information. A simple role check with and if statement will not do.

|Approach|	Advantages|	Disadvantages|
|---|---|---|
|Role checks only|	Simple|	Too coarse for resource-level access|
|Hard-coded controller checks|	Easy initially|	Repeated security logic and difficult maintenance|
|Policy-based authorization|	Centralised rules; supports requirements and handlers|	More initial design work|
|Separate authorization service|	Strong separation|	Excessive complexity for CivicConnect|

Decision:
CivicConnect will use role-based authorization for broad capabilities and policy/resource-based authorization for sensitive resource access.

Benefit:
This approach supports the M1 requirements for protected request information, role-scoped functionality, traceable actions, privacy, future expansion of authorization rules.


## Design Problem 2: Controlling the Service Request Lifecycle

Problem:
CivicConnect is fundamentally a service-request lifecycle system.

A request should not be able to move arbitrarily between states.

This relates directly to requirements FR-009 through FR-013 and the stakeholder need for controlled statuses and traceable request history.

Alternatives Considered:
|Approach|	Advantages|	Disadvantages|
|---|---|---|
|Free-form status values|	Very simple|	Allows invalid states|
|Controller if/else checks|	Easy to start|	Logic becomes duplicated|
|Database-only constraints|	Protects stored data|	Does not express complete business behaviour|
|State/transition approach|	Explicit valid transitions|	More design structure required|

Decision:
CivicConnect will model the request lifecycle as an explicit state-transition rule set.

The design makes the business rule explicit instead of allowing each controller or UI screen to interpret statuses independently.

Benefit:
Controlled lifecycle transitions, fewer invalid states, centralised business rules, better testability, auditability, easier future modification

## Research-to-Decision Summary
|Research Finding|	CivicConnect Decision|	Requirement/Concern|
|---|---|---|
|ASP.NET Core supports role and policy-based authorization|	Use roles plus policies for sensitive resources|	FR-018, N-X1|
|Authorization is separate from authentication|	Treat identity and access control as separate responsibilities|	N-X1|
|EF Core migrations allow schema evolution to be source controlled|	Use EF Core migrations|	Data integrity / maintainability|
|Application logging provides security/audit information beyond infrastructure logs|	Record significant request actions/status changes|	FR-019, N-X2|
|GitHub protected branches can require PR reviews and status checks|	Protect main and require review before merge|	Governance / authenticity|
|.NET 10 is current LTS|	Use .NET 10 as runtime baseline|	Maintainability|
|PostgreSQL has a long supported lifecycle|	Use PostgreSQL relational persistence|	Data / maintainability|

## Architecture Decision Records
ADR-001 — Select ASP.NET Core and .NET 10

Status: Accepted

Context:
CivicConnect requires a maintainable web application with authentication, authorization, persistence, testing and reporting. The project has a three-person development team and limited budget.

Decision:
Use ASP.NET Core 10 and C# as the primary application platform.

Alternatives:

Node.js/Express
Django/Python
ASP.NET Core

Reason:
ASP.NET Core provides an integrated ecosystem for web development, APIs, authentication, authorization, configuration, dependency injection and testing. .NET 10 provides an LTS baseline.

Consequences:

Positive:
- single primary language
- strong framework support
- built-in security mechanisms
- long supported baseline
-suitable for a small team

Negative:

- team must remain familiar with the .NET ecosystem;
- changing to another platform later would require significant redevelopment.

ADR-002 — Select PostgreSQL with EF Core

Status: Accepted

Context:
CivicConnect contains strongly related entities and requires reporting, filtering, lifecycle history and data integrity.

Decision:
Use PostgreSQL as the relational database and Entity Framework Core as the primary application data-access technology.

Reason:
The relational model fits the domain. EF Core provides integrated data access and source-controlled migrations.

Consequences:

Positive:

- relational integrity
- structured querying
- suitable reporting
- controlled schema evolution
- database changes can be represented in source control

Negative:

- schema changes require migration management
- database deployment must be coordinated with application versions

ADR-003 — Use Policy-Based Authorization for Sensitive Resources

Status: Accepted

Context:
Was identified privacy as a major concern. Role membership alone may not determine whether a user can access a particular request.

Decision:
Use role-based authorization for broad functionality and policy/resource-based authorization for sensitive resource access.

Reason:
ASP.NET Core provides policy requirements and authorization handlers that allow authorization to consider more than simple role membership.

Consequences:

Positive:

- clearer security boundaries
- centralised authorization rules
- easier testing
- supports future access-control requirements

Negative:

- more complex than role checks alone
- requires dedicated authorization testing


# AI Usage Register

| Date | Student | Tool | Engineering Task | AI contribution | Verification | Decision | Issues found |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 09/09/2026 | Markus | Copilot | Grammar and format check | Correct grammar and ensuring format of tables or cohesive | Ensure only grammar/spelling mistakes have been changed and nothing else | Accepted | none |
| 09/09/2026 | Markus | Copilot | Conversion between markdown and word document | Conversion between docx and md | Ensure only conversion was mad and nothing else | Accepted | none |
| 09/30/2026 | Markus | Copilot | Grammar and format check | Correct grammar and ensuring format of tables or cohesive | Ensure only grammar/spelling mistakes have been changed and nothing else | Accepted | none |
