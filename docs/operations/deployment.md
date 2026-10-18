# Deploying CivicConnect to a staging environment

Status: **prepared, not yet deployed.** The hosting platform is still to be chosen (OD-01). Everything
below works on any platform that runs a Docker image and offers PostgreSQL 18. The CI job
"Container image" builds this image, applies the migrations to an empty database, starts the app and
checks `/health` on every pull request, so the image itself is tested even before a platform is picked.

## What gets deployed

| Piece | What it is |
|-|-|
| Container image | Built from the `Dockerfile` in the repository root. Holds the web app and `efbundle`, the database migration bundle. Listens on port 8080 and runs as a non-root user. |
| PostgreSQL 18 database | A managed database from the platform. The app never creates or migrates it on start-up outside Development (D-015). |
| Configuration | Environment variables set in the platform's dashboard. Nothing secret is in the image or the repository (NFR-003, NFR-016). |

## Environment variables

| Variable | Example | Notes |
|-|-|-|
| `ConnectionStrings__CivicConnect` | `Host=…;Port=5432;Database=civicconnect;Username=…;Password=…;SSL Mode=Require` | From the database provider. Mark it as secret. |
| `ASPNETCORE_ENVIRONMENT` | `Staging` | Already the image default. |
| `Seed__AdminEmail` | `admin@yourdomain` | Creates the first administrator on first start, if no account with that email exists. |
| `Seed__AdminPassword` | (secret) | At least 8 characters with upper case, lower case and a digit. Remove both seed variables after the first successful start. |

The image already sets `ASPNETCORE_HTTP_PORTS=8080` and `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`,
so the app trusts the platform's HTTPS proxy and issues secure cookies.

## Steps for each release

1. **Build the image** (or let the platform build it from the repository):

   ```
   docker build -t civicconnect:<version> .
   ```

2. **Back up the database** if it already holds data (see `backup-restore.md`).

3. **Apply migrations** with the bundle in the new image, once, before starting the app (D-015):

   ```
   docker run --rm -e "ConnectionStrings__CivicConnect=<connection string>" --entrypoint ./efbundle civicconnect:<version>
   ```

   On platforms with a "pre-deploy command", use `./efbundle` as that command.

4. **Start the app.** On start-up it checks that no migration is missing (it refuses to start if one is),
   then seeds roles and the initial categories if they are not there yet.

5. **Check it:**
   - `GET /health` returns `Healthy` (point the platform's health check here).
   - Sign in as the administrator, create a staff account, submit a request as a requester and accept it.

6. **Record the release** in the PED: image tag or commit, date, migration applied, health check result.

## Rolling back

- **App only** (no migration in the release): redeploy the previous image tag.
- **With a migration**: restore the backup taken in step 2 into the database, then redeploy the previous
  image. Migrations are not rolled back with `Down()` scripts, because a restore is simpler to test and
  covers data changes too.

## Platform notes (OD-01)

| Option | Fit | Watch out for |
|-|-|-|
| Render (web service from Dockerfile) with Neon (PostgreSQL) | Free tiers, no credit card, deploys from GitHub. | Free web services sleep after inactivity (first request after that is slow). Check Neon's free-tier storage limit against AS-02 (10 000 requests/year is small). |
| Azure App Service (container) with Azure Database for PostgreSQL Flexible Server | Azure for Students credit; closest to a production setup. | More setup steps; watch credit usage. |

Either way, before production: set up automatic backups on the database and test a restore (R-08, R-11).

## Known limitations

- **Sign-in cookies after a restart.** ASP.NET Core's data-protection keys are kept inside the
  container, so a redeploy signs everyone out. Acceptable for staging; for production, persist the keys
  (for example to the database) before go-live.
- **Admin changes are logged but not audited** (FE-09): user, role and category changes appear in the
  application log, not in the audit trail.
