<#
.SYNOPSIS
    Backs up the CivicConnect PostgreSQL database to a single compressed file (NFR-017).

.DESCRIPTION
    Uses pg_dump in custom format (-Fc), which pg_restore can restore in full or table by table.
    The password is never passed on the command line or stored in the repository: set PGPASSWORD for
    this session, use a pgpass file, or type it when pg_dump asks.
    Backups contain personal data and password hashes. Keep them out of git (backups/ is ignored)
    and store them somewhere access-controlled.

.EXAMPLE
    $env:PGPASSWORD = Read-Host -AsSecureString | ConvertFrom-SecureString -AsPlainText   # PowerShell 7
    ./scripts/backup.ps1

.EXAMPLE
    ./scripts/backup.ps1 -DbHost my-staging-host.example -Database civicconnect -User civicconnect -OutDir D:\backups
#>
param(
    [string]$DbHost = "localhost",
    [int]$Port = 5432,
    [string]$Database = "civicconnect",
    [string]$User = "civicconnect",
    [string]$OutDir = (Join-Path $PSScriptRoot "..\backups")
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

$pgDump = Find-PgTool "pg_dump"
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$file = Join-Path (Resolve-Path $OutDir) "$Database-$stamp.dump"

Write-Host "Backing up $Database on ${DbHost}:$Port to $file"
$started = Get-Date

# --no-owner / --no-privileges: the backup can be restored under a different database user
& $pgDump --host $DbHost --port $Port --username $User --dbname $Database `
    --format custom --compress 9 --no-owner --no-privileges --file $file
if ($LASTEXITCODE -ne 0) { throw "pg_dump failed with exit code $LASTEXITCODE." }

$seconds = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
$size = (Get-Item $file).Length
$hash = (Get-FileHash $file -Algorithm SHA256).Hash.ToLowerInvariant()

Write-Host "Done in $seconds s: $([math]::Round($size / 1KB, 1)) KB, SHA-256 $hash"
Write-Host "Record the file name and hash, so a restore can prove it used this exact backup."

[pscustomobject]@{ File = $file; Bytes = $size; Sha256 = $hash; Seconds = $seconds }
