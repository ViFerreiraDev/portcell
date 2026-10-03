param([Parameter(Mandatory = $true)][string]$OutputPath)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command pg_dump -ErrorAction SilentlyContinue)) { throw 'pg_dump não está disponível no PATH.' }
foreach ($name in @('PGHOST', 'PGDATABASE', 'PGUSER', 'PGPASSWORD')) {
    if (-not [Environment]::GetEnvironmentVariable($name)) { throw "Configure $name antes do backup." }
}
$resolved = [System.IO.Path]::GetFullPath($OutputPath)
$directory = [System.IO.Path]::GetDirectoryName($resolved)
if (-not [System.IO.Directory]::Exists($directory)) { throw "Diretório não existe: $directory" }
& pg_dump --format=custom --no-owner --file=$resolved
if ($LASTEXITCODE -ne 0) { throw "pg_dump falhou com código $LASTEXITCODE" }
Write-Output "Backup criado em $resolved"
