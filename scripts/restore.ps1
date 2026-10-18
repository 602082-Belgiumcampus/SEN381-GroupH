<#
.SYNOPSIS
    Restores a CivicConnect backup into a database and checks that the data arrived (NFR-017).

.DESCRIPTION
    Restores a pg_dump custom-format file with pg_restore, then counts the rows in the main tables.
    With -CompareWith, it also counts the same tables in another database (normally the one that was
    backed up) and fails if any count differs.

    Safety: the target database must already exist and be empty, unless -Clean is given, which drops
    and recreates every object in it first. Restoring over the live "civicconnect" database is refused
    unless -Force is also given.

.EXAMPLE
    # Restore test into a scratch database, comparing with the source
    ./scripts/restore.ps1 -BackupFile backups/civicconnect-20261018-120000.dump -Database civicconnect_restore_test -CompareWith civicconnect
#>
param(
    [Parameter(Mandatory)] [string]$BackupFile,
    [Parameter(Mandatory)] [string]$Database,
    [string]$DbHost = "localhost",
    [int]$Port = 5432,
    [string]$User = "civicconnect",
    [string]$CompareWith,
    [switch]$Clean,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Find-PgTool([string]$name) {
    $onPath = Get-Command $name -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    $installed = Get-ChildItem "C:\Program Files\PostgreSQL\*\bin\$name.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1
    if ($installed) { return $installed.FullName }
    throw "$name was not found. Install the PostgreSQL client tools or add their bin folder to PATH."
}

if (-not (Test-Path $BackupFile)) { throw "Backup file not found: $BackupFile" }
if ($Database -eq "civicconnect" -and -not $Force) {
    throw "Refusing to restore over the 'civicconnect' database. Restore into a scratch database first, or add -Force if you really mean to replace it."
}

$pgRestore = Find-PgTool "pg_restore"
$psql = Find-PgTool "psql"

# The tables that hold the system's data; Identity's lookup tables come along with them
$tables = @("AspNetUsers", "AspNetRoles", "AspNetUserRoles", "Categories", "ServiceRequests",
            "AuditEntries", "RequestActions", "FeedbackNotices", "__EFMigrationsHistory")

function Get-RowCounts([string]$db) {
    $counts = [ordered]@{}
    foreach ($table in $tables) {
        $value = & $psql --host $DbHost --port $Port --username $User --dbname $db --no-psqlrc -At -c "SELECT count(*) FROM `"$table`";"
        if ($LASTEXITCODE -ne 0) { throw "Could not count $table in $db." }
        $counts[$table] = [int]$value
    }
    return $counts
}

$hash = (Get-FileHash $BackupFile -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Host "Restoring $BackupFile (SHA-256 $hash) into $Database on ${DbHost}:$Port"
$started = Get-Date

$restoreArgs = @("--host", $DbHost, "--port", $Port, "--username", $User, "--dbname", $Database,
                 "--no-owner", "--no-privileges", "--exit-on-error")
if ($Clean) { $restoreArgs += @("--clean", "--if-exists") }

& $pgRestore @restoreArgs $BackupFile
if ($LASTEXITCODE -ne 0) { throw "pg_restore failed with exit code $LASTEXITCODE." }

$seconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
Write-Host "Restore finished in $seconds s. Row counts in ${Database}:"

$restored = Get-RowCounts $Database
$report = foreach ($table in $tables) { [pscustomobject]@{ Table = $table; Restored = $restored[$table] } }

if ($CompareWith) {
    $source = Get-RowCounts $CompareWith
    $report = foreach ($row in $report) {
        $row | Add-Member -NotePropertyName Source -NotePropertyValue $source[$row.Table] -PassThru |
               Add-Member -NotePropertyName Match -NotePropertyValue ($source[$row.Table] -eq $row.Restored) -PassThru
    }
}

$report | Format-Table -AutoSize | Out-String | Write-Host

if ($CompareWith -and ($report | Where-Object { -not $_.Match })) {
    throw "Row counts differ between $CompareWith and the restored $Database. The data may have changed since the backup was taken, or the restore is incomplete."
}

Write-Host "Restore check passed."
