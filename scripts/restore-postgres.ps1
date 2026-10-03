param([Parameter(Mandatory = $true)][string]$InputPath)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command pg_restore -ErrorAction SilentlyContinue)) { throw 'pg_restore não está disponível no PATH.' }
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw 'psql não está disponível no PATH.' }
foreach ($name in @('PGHOST', 'PGDATABASE', 'PGUSER', 'PGPASSWORD')) {
    if (-not [Environment]::GetEnvironmentVariable($name)) { throw "Configure $name antes da restauração." }
}
$resolved = [System.IO.Path]::GetFullPath($InputPath)
if (-not [System.IO.File]::Exists($resolved)) { throw "Backup não encontrado: $resolved" }
$objects = & psql --no-psqlrc --tuples-only --no-align --set=ON_ERROR_STOP=1 --dbname=$env:PGDATABASE --command "SELECT count(*) FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' AND c.relkind IN ('r', 'p', 'v', 'm', 'S');"
if ($LASTEXITCODE -ne 0) { throw "Não foi possível validar se o banco $env:PGDATABASE está vazio." }
if ([int]$objects -ne 0) { throw "O banco $env:PGDATABASE não está vazio; a restauração foi recusada." }
& pg_restore --exit-on-error --no-owner --dbname=$env:PGDATABASE $resolved
if ($LASTEXITCODE -ne 0) { throw "pg_restore falhou com código $LASTEXITCODE" }
Write-Output 'Restauração concluída.'
