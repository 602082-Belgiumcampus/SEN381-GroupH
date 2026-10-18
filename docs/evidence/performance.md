# Performance evidence (ASR-05; NFR-005, NFR-006)

## Target and workload

| | |
|-|-|
| Target (ASR-05) | 95% of list and view operations under **2 s**; 95% of submissions under **3 s** |
| Workload (AS-02, team assumption, OD-03 still open) | 50 concurrent users; 10 000 requests per year |

## How the test was run

Tool: `tests/CivicConnect.LoadTests` (a console app in this repository).

1. The app (Release build) ran against an **empty temporary schema** in the local PostgreSQL 18, with
   migrations applied and the demo accounts seeded. No real or development data was involved, and the
   schema was dropped afterwards.
2. `--seed 10000` inserted 10 000 requests spread over the past year (one year at the AS-02 rate),
   with a mix of statuses, and one audit entry each, all belonging to the demo requester. That makes
   "My requests" a 10 000-row list, a worst case for that page.
3. 50 virtual users ran for 120 s with 1–3 s of "think time" between actions:
   - 25 staff users (signed in as the demo supervisor, who sees every request): work queue, search
     with a status filter and sort, open a request, reports.
   - 25 requesters: My requests, open the submit form, **submit a request**, open a request.
4. Each submission was checked afterwards in the database: 382 submissions, 382 new requests and
   382 matching audit entries.

Command (credentials come from environment variables, not the command line):

```
$env:LOADTEST_DB = "<connection string of the scratch database>"
$env:LOADTEST_PASSWORD = "<password of the demo accounts>"
dotnet run --project tests/CivicConnect.LoadTests -c Release -- --base-url http://localhost:5160 --users 50 --duration 120 --seed 10000
```

## Result (18 October 2026)

Environment: one developer PC (AMD Ryzen 7 3700X, 16 GB RAM, Windows 11), app and database on the same
machine, app in the Development environment with logging reduced to warnings. Branch
`feature/m4-completion` before its first commit.

3 164 requests in 129 s (24.5 per second), **0 errors**.

| Operation | Count | Errors | p50 (ms) | p95 (ms) | p99 (ms) | Max (ms) |
|-|-:|-:|-:|-:|-:|-:|
| My requests (10 000 rows, paged) | 382 | 0 | 8 | 36 | 254 | 310 |
| Reports | 384 | 0 | 62 | 94 | 121 | 173 |
| Request detail | 766 | 0 | 4 | 10 | 27 | 78 |
| Search and filter | 384 | 0 | 8 | 14 | 32 | 56 |
| Sign in | 50 | 0 | 663 | 850 | 880 | 880 |
| Sign-in page | 50 | 0 | 258 | 345 | 345 | 345 |
| Submit form | 382 | 0 | 2 | 4 | 10 | 37 |
| Submit request | 382 | 0 | 3 | 7 | 13 | 151 |
| Work queue (10 000+ rows, paged) | 384 | 0 | 7 | 35 | 310 | 391 |

| ASR-05 check | p95 (ms) | Target (ms) | Result |
|-|-:|-:|-|
| List and view operations (2 682) | 69 | 2 000 | **Pass** |
| Submissions (382) | 7 | 3 000 | **Pass** |

Sign-in is the slowest operation because password hashing is deliberately slow (NFR-001); it is not a
list, view or submit operation, so it is outside ASR-05. The first requests after start-up (sign-in
page, p50 258 ms) include the app warming up.

## What this does and does not show

- It shows the application code, queries and indexes meet ASR-05 with a wide margin at the assumed
  workload, including a year's worth of data.
- It does **not** show staging or production performance: there the database is on another machine
  (network latency per query) and free-tier hosting is much smaller than this PC. **Re-run the same
  test against staging** once it exists and record the result here; the tool takes `--base-url`.
- The workload is still a team assumption (AS-02, OD-03) until the client confirms it.
