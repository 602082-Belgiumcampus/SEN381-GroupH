# CivicConnect

Service-request management platform for a community organisation (SEN381 project, Group H).
Design and decisions: see `docs/PED/PEDv2.1.md`.

## Status

Early development

**Exists:**
- .NET 10 solution (`CivicConnect.slnx`) with SDK pinned in `global.json`.
- `src/CivicConnect.Modules`: the Lifecycle module's pure rules for FR-011 (table-driven state machine,
  authorisation rules, engine returning typed outcomes and an audit record) and the `Actor` type.
- `src/CivicConnect.Web`: the Razor Pages shell (layout, home page, error page) and
  Request lifecycle page. That page lists the allowed transitions straight from the lifecycle table and
  has an example request that can be stepped through the states using the real lifecycle engine.
- `tests/CivicConnect.Domain.Tests`: xUnit tests for TC-LC-01 to TC-LC-16 from PED Section 10.5.

**Not yet implemented:** database and EF Core migrations, authentication, request intake, queries,
reporting, feedback, audit persistence, administration, CI.

## Prerequisites

- .NET 10 SDK

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

## Structure

```
src/CivicConnect.Modules/        one folder per module (PED Figure 2)
  IdentityAccess/                Actor and roles
  Lifecycle/                     state table, authorisation rules, engine
src/CivicConnect.Web/            Razor Pages website
  Pages/                         home, request lifecycle, error, shared layout
  wwwroot/                       CSS, JS and client libraries (Bootstrap, jQuery)
tests/CivicConnect.Domain.Tests/ pure rule tests (no database)
```