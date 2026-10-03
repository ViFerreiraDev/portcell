$ErrorActionPreference = 'Stop'
$stateFile = Join-Path (Split-Path -Parent $PSScriptRoot) '.local-keys/radmin/services.json'
if (-not (Test-Path -LiteralPath $stateFile)) { Write-Output 'Não há execução Radmin registrada.'; return }
$state = Get-Content -LiteralPath $stateFile -Raw | ConvertFrom-Json
foreach ($service in $state.Services) {
    $process = Get-Process -Id $service.Id -ErrorAction SilentlyContinue
    if ($process -and $process.StartTime.ToUniversalTime().Ticks.ToString() -eq $service.Started) {
        Stop-Process -Id $process.Id
    }
}
Remove-Item -LiteralPath $stateFile
Write-Output 'Serviços PortCell do Radmin encerrados. O PostgreSQL permanece ativo.'
