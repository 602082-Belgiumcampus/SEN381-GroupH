# Accessibility evidence (WCAG 2.2 AA intent, PED Section 9.3; supports NFR-009)

## Automated check: axe-core

| Item | Value |
|-|-|
| Date | 18 October 2026 |
| Tool | axe-core 4.10.2, run in the browser against the running app |
| Rules | WCAG 2.0 A and AA, WCAG 2.1 A and AA, WCAG 2.2 AA tags |
| Code | branch `feature/m4-completion` (before its first commit), local Release build, Development data |
| Viewport | 1024 × 768 |

### First run: one problem found and fixed

The green status pill ("Resolved", "Closed", "Active") had text `#1e7f57` on `#e1f3ea`, a contrast of
**4.31:1**, below the 4.5:1 AA minimum for its 13.6 px bold text (`color-contrast`, serious). It showed
on the Reports page, and on any page listing a resolved or closed request. The pill text was changed to
`#1a7350` (**5.04:1**) in `site.css`. The other pill colours were checked by calculation:

| Pill | Text on background | Contrast |
|-|-|-|
| Waiting (Submitted) | `#4a4560` on `#ebeaf1` | 7.6:1 |
| Active (Accepted, Assigned) | `#5a3ea6` on `#ece7f8` | 6.49:1 |
| Working (In progress) | `#8a5c00` on `#fdf2d8` | 5.22:1 |
| Done (Resolved, Closed), fixed | `#1a7350` on `#e1f3ea` | 5.04:1 |
| Stopped (Rejected), Overdue | `#b83a32` on `#f8e3e1` | 4.63:1 |

### Result after the fix: no violations on any page checked

| Page | Signed in as | Violations | Rules passed |
|-|-|-:|-:|
| Home (`/`) | supervisor, administrator | 0 | 23 |
| Sign in, Create an account | (anonymous / administrator) | 0 | 23 / 27 |
| Work queue (`/Staff`), also with search and filters applied | supervisor | 0 | 28 / 26 |
| Log a request (`/Staff/LogRequest`) | supervisor | 0 | 27 |
| Request detail, accepted and rejected requests | supervisor | 0 | 27 / 25 |
| Notices (`/Notices`) | supervisor, administrator | 0 | 23 |
| Reports (`/Reports`) | supervisor | 0 | 28 |
| How requests move (`/Lifecycle`) | supervisor | 0 | 25 |
| Users list, Create a user, Manage user | administrator | 0 | 27 / 27 / 25 |
| Categories list, Edit category | administrator | 0 | 27 / 25 |

Automated tools find only part of the accessibility problems (roughly a third to a half of WCAG issues).
A clean axe run does not by itself mean the site is accessible.

## Manual checks still to do (team)

Record who did them, when, and what was found.

| Check | Done by / date | Result |
|-|-|-|
| Keyboard only: submit a request, then as staff search the queue, open a request and accept it, using only Tab, Shift+Tab, Enter and Space. Focus must always be visible. | | |
| Zoom to 200%: the submit form and the work queue still work without horizontal scrolling. | | |
| Screen reader (NVDA on Windows): the status of a request and the overdue flag are read out as text. | | |
