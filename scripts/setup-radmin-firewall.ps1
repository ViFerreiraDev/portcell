#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
$adapter = Get-NetAdapter | Where-Object { $_.InterfaceDescription -like '*Radmin*' -and $_.Status -eq 'Up' } | Select-Object -First 1
if (-not $adapter) { throw 'Conecte o Radmin VPN antes de configurar o firewall.' }
$address = Get-NetIPAddress -InterfaceIndex $adapter.ifIndex -AddressFamily IPv4 | Where-Object { $_.IPAddress -like '26.*' } | Select-Object -First 1
if (-not $address) { throw 'Não foi encontrado o IP do Radmin.' }
$ruleName = 'PortCell-Radmin-5173'
Get-NetFirewallRule -Name $ruleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule
New-NetFirewallRule -Name $ruleName -DisplayName 'PortCell - acesso via Radmin VPN' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5173 -LocalAddress $address.IPAddress -RemoteAddress '26.0.0.0/8' -InterfaceAlias $adapter.Name -Profile Any | Out-Null
$statusPath = Join-Path (Split-Path -Parent $PSScriptRoot) '.local-keys/radmin/firewall.json'
@{ Rule = $ruleName; Ip = $address.IPAddress; Interface = $adapter.Name; Port = 5173; Updated = [DateTimeOffset]::UtcNow.ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath $statusPath -Encoding utf8
Write-Output "Firewall configurado para $($address.IPAddress):5173 pela interface Radmin VPN."
