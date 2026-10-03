[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$adapter = Get-NetAdapter | Where-Object { $_.InterfaceDescription -like '*Radmin*' -and $_.Status -eq 'Up' } | Select-Object -First 1
if (-not $adapter) { throw 'Conecte o Radmin VPN antes de iniciar o PortCell.' }
$address = Get-NetIPAddress -InterfaceIndex $adapter.ifIndex -AddressFamily IPv4 | Where-Object { $_.IPAddress -like '26.*' } | Select-Object -First 1
if (-not $address) { throw 'Não foi encontrado um IPv4 ativo na interface Radmin VPN.' }
$radminIp = $address.IPAddress
$keysFile = Join-Path $projectRoot '.local-keys/local-start.ps1'
if (-not (Test-Path -LiteralPath $keysFile)) { throw 'Configure primeiro as chaves locais e a conexão PostgreSQL em .local-keys/local-start.ps1.' }
. $keysFile
$pgData = Join-Path $projectRoot '.pg-local'
if ((Test-Path -LiteralPath (Join-Path $pgData 'PG_VERSION')) -and
    $env:ConnectionStrings__Postgres -match '(?i)(?:^|;)\s*Host\s*=\s*(localhost|127\.0\.0\.1)\s*(;|$)' -and
    $env:ConnectionStrings__Postgres -match '(?i)(?:^|;)\s*Port\s*=\s*55433\s*(;|$)') {
    $pgVersion = (Get-Content -LiteralPath (Join-Path $pgData 'PG_VERSION') -Raw).Trim()
    if ($pgVersion -notmatch '^\d+$') { throw 'Versão PostgreSQL local inválida.' }
    $pgBin = Join-Path $env:ProgramFiles "PostgreSQL/$pgVersion/bin"
    & (Join-Path $pgBin 'pg_isready.exe') -h 127.0.0.1 -p 55433 *> $null
    if ($LASTEXITCODE -ne 0) {
        & (Join-Path $pgBin 'pg_ctl.exe') -D $pgData -l (Join-Path $projectRoot '.pg-local.log') -o '-h 127.0.0.1 -p 55433' -w start
        if ($LASTEXITCODE -ne 0) { throw 'Não foi possível iniciar o PostgreSQL local.' }
    }
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5000'
$env:Frontend__Origin = "http://${radminIp}:5173"
$env:Frontend__AllowedOrigins__0 = 'http://localhost:5173'
$env:Frontend__AllowedOrigins__1 = 'http://127.0.0.1:5173'
$env:LocalProxy__Enabled = 'true'
$env:Logging__LogLevel__Default = 'Warning'
$node = (Get-Command node.exe -ErrorAction Stop).Source
$dotnet = Join-Path $projectRoot '.dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source }
$api = Join-Path $projectRoot 'backend/PortCell.Api/bin/Debug/net10.0/PortCell.Api.dll'
$vite = Join-Path $projectRoot 'frontend/node_modules/vite/bin/vite.js'
if (-not (Test-Path -LiteralPath $api) -or -not (Test-Path -LiteralPath (Join-Path $projectRoot 'frontend/dist/index.html'))) {
    throw 'Compile a API e o frontend antes de iniciar: dotnet build e npm run build.'
}
$runtimePath = Join-Path $projectRoot '.local-keys/radmin'
New-Item -ItemType Directory -Path $runtimePath -Force | Out-Null
$stateFile = Join-Path $runtimePath 'services.json'
if (Test-Path -LiteralPath $stateFile) {
    $state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
    $running = @($state.Services | Where-Object {
        $serviceProcess = Get-Process -Id $_.Id -ErrorAction SilentlyContinue
        $serviceProcess -and $serviceProcess.StartTime.ToUniversalTime().Ticks.ToString() -eq $_.Started
    })
    if ($running.Count -gt 0) {
        if ($running.Count -eq 3 -and $state.RadminIp -eq $radminIp) {
            Write-Output "PortCell já está ativo: http://${radminIp}:5173/"
            return
        }
        throw 'Há serviços de uma execução anterior. Execute scripts/stop-radmin.ps1 antes de reiniciar.'
    }
}
$listeners = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object {
    ($_.LocalPort -eq 5000 -and $_.LocalAddress -in @('127.0.0.1', '0.0.0.0', '::')) -or
    ($_.LocalPort -eq 5173 -and $_.LocalAddress -in @('127.0.0.1', $radminIp, '0.0.0.0', '::'))
}
if ($listeners) { throw 'As portas 5000/5173 já estão em uso. Encerre a execução anterior do projeto antes de iniciar pelo Radmin.' }
$started = [System.Collections.Generic.List[object]]::new()
try {
    foreach ($service in @(
        @{ Name = 'api'; File = $dotnet; Args = @(('"{0}"' -f $api)); Directory = $projectRoot },
        @{ Name = 'frontend-local'; File = $node; Args = @(('"{0}"' -f $vite), 'preview', '--configLoader', 'runner', '--host', '127.0.0.1', '--port', '5173', '--strictPort'); Directory = (Join-Path $projectRoot 'frontend') },
        @{ Name = 'frontend-radmin'; File = $node; Args = @(('"{0}"' -f $vite), 'preview', '--configLoader', 'runner', '--host', $radminIp, '--port', '5173', '--strictPort'); Directory = (Join-Path $projectRoot 'frontend') }
    )) {
        $process = Start-Process -FilePath $service.File -ArgumentList $service.Args -WorkingDirectory $service.Directory -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runtimePath "$($service.Name).out.log") -RedirectStandardError (Join-Path $runtimePath "$($service.Name).err.log")
        $started.Add(@{ Name = $service.Name; Id = $process.Id; Started = $process.StartTime.ToUniversalTime().Ticks.ToString() })
    }
    @{ RadminIp = $radminIp; Services = @($started.ToArray()) } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $stateFile -Encoding utf8
    $ready = $false
    for ($attempt = 0; $attempt -lt 15; $attempt++) {
        try {
            $null = Invoke-RestMethod -Uri "http://${radminIp}:5173/api/health" -TimeoutSec 2
            $ready = $true
            break
        } catch { Start-Sleep -Seconds 1 }
    }
    if (-not $ready) { throw "Os serviços não responderam. Consulte os logs em $runtimePath." }
    Write-Output "PortCell no Radmin: http://${radminIp}:5173/"
    Write-Output 'Acesso local: http://localhost:5173/'
} catch {
    foreach ($service in $started) { Stop-Process -Id $service.Id -ErrorAction SilentlyContinue }
    throw
}
