# Backup and restore (NFR-017; risks R-08, R-11)

Two scripts in `scripts/` wrap the standard PostgreSQL tools. They work against any PostgreSQL 18 server:
the local development database, or staging and production once they exist.

| Script | What it does |
|-|-|
| `backup.ps1` | `pg_dump` in custom format (compressed, can be restored in full or per table). Prints the file's size and SHA-256 so a restore can prove which backup it used. |
| `restore.ps1` | `pg_restore` into a database, then counts the rows in the main tables. With `-CompareWith` it also counts them in the source database and fails if anything differs. Refuses to overwrite the live `civicconnect` database unless `-Force` is given. |

Backups hold personal data and password hashes. `backups/` and `*.dump` are in `.gitignore`; store
backups somewhere only the team (later, the organisation's administrator) can read.

## Passwords

The scripts never take a password on the command line. Either set it for the current terminal only:

```
$env:PGPASSWORD = "<database password>"
```

or let `pg_dump` / `pg_restore` ask for it, or use a `pgpass.conf` file.

## Back up

```
./scripts/backup.ps1                                   # local: localhost, database civicconnect
./scripts/backup.ps1 -DbHost <host> -User <user> -Database civicconnect -OutDir <folder>
```

When:
- before every release that includes a migration (deployment step 2, `deployment.md`);
- on a schedule once there is real data. On a hosted platform, also switch on the provider's automatic
  backups and note their retention against AS-01 (5 years for request history, still to be confirmed).

## Restore

Always restore into a **new, empty** database first and check it, rather than straight over the live one.

1. Create an empty database (needs a user that can create databases, for example `postgres`):

   ```
   psql -h localhost -U postgres -c "CREATE DATABASE civicconnect_restore_test OWNER civicconnect;"
   ```

2. Restore and compare with the source:

   ```
   ./scripts/restore.ps1 -BackupFile backups/<file>.dump -Database civicconnect_restore_test -CompareWith civicconnect
   ```

3. To recover for real: stop the app, restore into the live database with `-Database civicconnect -Clean -Force`,
   start the app, and check `/health` and sign-in.

## Restore test (evidence)

A backup is only proven by restoring it. Record each test here.

| Date | Backup file and SHA-256 | Source rows (requests / audit / users) | Restored into | Row counts match | Time to restore | Done by |
|-|-|-|-|-|-|-|
| | | | | | | |

Before production (R-08): repeat this test against the hosted database, using the provider's backup.
