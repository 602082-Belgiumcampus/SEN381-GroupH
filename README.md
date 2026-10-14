# CivicConnect

Service-request management platform for a community organisation (SEN381 project, Group H).
Design and decisions: see `docs/PED/PEDv2.1.md`.

## Status

Early development (Milestone 3 in progress)

**Exists:**
- .NET 10 solution (`CivicConnect.slnx`) with SDK pinned in `global.json`.
- `src/CivicConnect.Modules`: pure rules with no database, plus the module contracts (ADR-008):
  - Lifecycle: table-driven state machine, authorisation rules and engine for FR-011.
  - IdentityAccess: `Actor`, roles, and who may see a request (AS-05, ADR-007).
  - RequestIntake: submit-form validation (CL-05) and the request reference format (FR-003).
- `src/CivicConnect.Infrastructure`: PostgreSQL through EF Core and ASP.NET Core Identity.
  - Tables: users, roles, categories, service requests and the audit trail.
  - Services: request intake, the lifecycle service (status change and audit entry saved together,
    optimistic concurrency on `Version`) and request queries.
  - Seeding: roles and an initial category list.
- `src/CivicConnect.Web`: Razor Pages website.
  - Sign in, sign out, and requester self-registration.
  - Requesters can submit a request and see "My requests".
  - Staff have a work queue; a request's detail page lets staff or a supervisor accept or reject it.
  - The request lifecycle demo page.
- `tests/CivicConnect.Domain.Tests`: xUnit tests for TC-LC-01 to TC-LC-16 and the pure parts of
  TC-IN-01 to TC-IN-03, TC-AZ-01 and TC-AZ-05.

**Not yet implemented:** assign / start / resolve / close in the UI (the engine and service already
support them), integration tests against PostgreSQL (TC-PE), log on behalf (FR-020), feedback notices,
reporting, administration pages, CI.

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for the local PostgreSQL 18 database), or your own PostgreSQL 18

## First-time setup

1. **Start the database.** Copy `.env.example` to `.env` and choose a password, then:

   ```
   docker compose up -d
   ```

2. **Tell the app how to connect.** Secrets are never committed (NFR-003), so use user-secrets:

   ```
   dotnet user-secrets set "ConnectionStrings:CivicConnect" "Host=localhost;Port=5432;Database=civicconnect;Username=civicconnect;Password=<the password from .env>" --project src/CivicConnect.Web
   ```

3. **Optional: demo accounts.** Set a demo password to create one user per role in Development:

   ```
   dotnet user-secrets set "Seed:DemoPassword" "<a password with upper, lower case and a digit>" --project src/CivicConnect.Web
   ```

   The demo users are `requester@`, `staff@`, `supervisor@`, `manager@` and `admin@civicconnect.local`.

4. **Install the EF Core tool** (pinned in `.config/dotnet-tools.json`):

   ```
   dotnet tool restore
   ```

In Development the app applies migrations and seeds roles and categories when it starts.

## Database migrations

Migrations live in `src/CivicConnect.Infrastructure/Persistence/Migrations`. After changing the model:

```
dotnet ef migrations add <Name> --project src/CivicConnect.Infrastructure --startup-project src/CivicConnect.Web --output-dir Persistence/Migrations
```

## Build and test

```
dotnet build
dotnet test
```

## Run the website

```
dotnet run --project src/CivicConnect.Web
```

Then open http://localhost:5146

To try the review flow:
1. Sign in as a requester (or create an account) and submit a request.
2. Sign out, sign in as `staff@` or `supervisor@`, and open the request from the work queue.
3. Accept it, or reject it with a reason.

## Structure

```
src/CivicConnect.Modules/          one folder per module (PED Figure 2), no database code
  IdentityAccess/                  Actor, roles, request visibility
  Lifecycle/                       state table, authorisation rules, engine, service contract
  RequestIntake/                   submit validation, reference format, intake contract
  RequestQueries/                  query contract and read models
src/CivicConnect.Infrastructure/   EF Core + PostgreSQL + Identity
  Identity/                        AppUser
  Persistence/                     DbContext, entities, migrations, startup initialiser
  RequestIntake/ Lifecycle/ RequestQueries/   implementations of the module contracts
src/CivicConnect.Web/              Razor Pages website
  Pages/Account/                   sign in, register, sign out
  Pages/Requests/                  submit, my requests, request detail (accept / reject)
  Pages/Staff/                     work queue
  Security/                        policies and the signed-in user as an Actor
tests/CivicConnect.Domain.Tests/   pure rule tests (no database)
docker-compose.yml                 local PostgreSQL 18
```
