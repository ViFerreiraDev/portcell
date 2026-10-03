param([string]$BaseUrl = 'http://127.0.0.1:5000')

$ErrorActionPreference = 'Stop'
if ($BaseUrl -notmatch '^http://127\.0\.0\.1:[0-9]+$') { throw 'Use somente uma API local isolada para este teste.' }
if (-not $env:Admin__Email -or -not $env:Admin__Password) { throw 'Configure Admin__Email e Admin__Password do banco de teste.' }
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$script:accessToken = ''

function Api([string]$method, [string]$path, $body = $null, [bool]$authenticated = $true) {
    $args = @{ Uri = "$BaseUrl$path"; Method = $method; WebSession = $session }
    if ($authenticated) { $args.Headers = @{ Authorization = "Bearer $script:accessToken" } }
    if ($null -ne $body) { $args.ContentType = 'application/json'; $args.Body = ($body | ConvertTo-Json -Depth 10 -Compress) }
    return Invoke-RestMethod @args
}

function Check([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function ExpectStatus([string]$method, [string]$path, $body, [int]$expected) {
    $args = @{ Uri = "$BaseUrl$path"; Method = $method; WebSession = $session; SkipHttpErrorCheck = $true; Headers = @{ Authorization = "Bearer $script:accessToken" } }
    if ($null -ne $body) { $args.ContentType = 'application/json'; $args.Body = ($body | ConvertTo-Json -Depth 10 -Compress) }
    $response = Invoke-WebRequest @args
    Check ($response.StatusCode -eq $expected) "Esperado HTTP $expected em $path; recebido $($response.StatusCode)."
}

$login = Api 'POST' '/api/auth/login' @{ email = $env:Admin__Email; senha = $env:Admin__Password } $false
$script:accessToken = $login.token
Check ($login.perfil -eq 'Administrador') 'Login não retornou o perfil esperado.'
$refresh = Api 'POST' '/api/auth/refresh' $null $false
$script:accessToken = $refresh.token
Check (-not [string]::IsNullOrWhiteSpace($script:accessToken)) 'Refresh token não renovou a sessão.'

$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$client = Api 'POST' '/api/clientes' @{ nome = "Cliente Teste $suffix"; telefone = "1199$suffix"; email = "cliente-$suffix@example.test" }
$device = Api 'POST' '/api/aparelhos' @{ clienteId = $client.id; marca = 'Marca Teste'; modelo = 'Modelo Teste'; imei = "123456$suffix" }
$unit = (Api 'GET' '/api/unidades')[0]
$checklist = (Api 'GET' '/api/configuracoes/checklist') | Where-Object ativo
$tests = (Api 'GET' '/api/configuracoes/testes') | Where-Object ativo
Check ($checklist.Count -gt 0 -and $tests.Count -gt 0) 'Configurações iniciais não carregadas.'
$repairType = Api 'POST' '/api/tipos-reparo' @{ nome = "Software $suffix"; requerDesbloqueio = $true; ativo = $true }
$service = Api 'POST' '/api/servicos' @{ nome = "Troca de conector $suffix"; precoPadrao = 120; garantiaDias = 90; condicoesGarantia = 'Peça e mão de obra.'; ativo = $true }
Check ((Api 'GET' '/api/servicos').id -contains $service.id) 'Serviço não apareceu no catálogo.'
$answers = @($checklist | ForEach-Object { @{ itemId = $_.id; resposta = $_.opcoes[0] } })
$order = Api 'POST' '/api/ordens-servico' @{ unidadeId = $unit.id; clienteId = $client.id; aparelhoId = $device.id; defeitoRelatado = 'Não carrega'; prioridade = 'Normal'; tiposReparo = @($repairType.id); checklist = $answers }
Check ($order.numero -gt 0 -and $order.acessoNecessario) 'OS não foi aberta com numeração e regra de acesso.'
$technician = Api 'POST' '/api/usuarios' @{ nome = "Técnico $suffix"; email = "tecnico-$suffix@portcell.test"; senha = 'senha-tecnico-teste-123'; perfil = 'Tecnico' }
$technicianLogin = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -ContentType 'application/json' -Body (@{ email = "tecnico-$suffix@portcell.test"; senha = 'senha-tecnico-teste-123' } | ConvertTo-Json)
$financialAccess = Invoke-WebRequest -Uri "$BaseUrl/api/ordens-servico/$($order.id)/pagamentos" -Method GET -Headers @{ Authorization = "Bearer $($technicianLogin.token)" } -SkipHttpErrorCheck
Check ($financialAccess.StatusCode -eq 403) 'Perfil técnico acessou dados financeiros.'
$deviceAccess = Invoke-WebRequest -Uri "$BaseUrl/api/ordens-servico/$($order.id)/acesso-dispositivo" -Method GET -Headers @{ Authorization = "Bearer $($technicianLogin.token)" } -SkipHttpErrorCheck
Check ($deviceAccess.StatusCode -eq 403) 'Técnico não atribuído acessou credencial do aparelho.'
ExpectStatus 'POST' "/api/ordens-servico/$($order.id)/reparo/iniciar" $null 409
Api 'PUT' "/api/ordens-servico/$($order.id)/acesso-dispositivo" @{ credencial = 'PIN-de-teste-1234' } | Out-Null
$access = Api 'GET' "/api/ordens-servico/$($order.id)/acesso-dispositivo"
Check ($access.credencial -eq 'PIN-de-teste-1234') 'Credencial cifrada não foi recuperada.'
$detail = Api 'GET' "/api/ordens-servico/$($order.id)"
Check ($detail.checklist.Count -eq $checklist.Count) 'Snapshot do checklist incompleto.'
Api 'PATCH' "/api/ordens-servico/$($order.id)/status" @{ status = 'AguardandoDiagnostico' } | Out-Null
Api 'PATCH' "/api/ordens-servico/$($order.id)/status" @{ status = 'EmDiagnostico' } | Out-Null
Api 'POST' "/api/ordens-servico/$($order.id)/diagnosticos" @{ descricao = 'Nota interna restrita'; resumoCliente = 'Conector de carga danificado.' } | Out-Null

$piece = Api 'POST' '/api/pecas' @{ sku = "SKU-$suffix"; descricao = 'Conector'; custo = 10; preco = 30; estoqueMinimo = 1 }
Api 'POST' "/api/pecas/$($piece.id)/movimentos" @{ quantidade = 2; tipo = 'Entrada'; justificativa = 'Compra para teste' } | Out-Null
$quote = Api 'POST' "/api/ordens-servico/$($order.id)/orcamentos" @{ itens = @(@{ tipo = 'Servico'; descricao = $service.nome; quantidade = 1; valorUnitario = 120 }, @{ tipo = 'Peca'; descricao = 'Conector'; quantidade = 1; valorUnitario = 30 }); desconto = 0; validoAte = [DateTime]::UtcNow.AddDays(2).ToString('o'); prazoEstimado = 'Dois dias úteis'; condicoes = 'Garantia conforme termo' }
Check ($quote.total -eq 150 -and $quote.numeroVersao -eq 1) 'Orçamento incorreto.'
$link = Api 'POST' "/api/orcamentos/$($quote.id)/enviar-aprovacao"
$publicToken = ($link.link -split '/')[-1]
$publicQuote = Api 'GET' "/api/public/autorizacoes/$publicToken" $null $false
Check ($publicQuote.diagnostico -eq 'Conector de carga danificado.') 'A página pública não usou o resumo do cliente.'
Check ($publicQuote.prazoEstimado -eq 'Dois dias úteis' -and $publicQuote.conteudoHash -eq $quote.conteudoHash) 'Prazo ou hash da versão não foram apresentados corretamente.'
Api 'POST' "/api/public/autorizacoes/$publicToken/aprovar" @{ confirmouLeitura = $true } $false | Out-Null
$reuse = Invoke-WebRequest -Uri "$BaseUrl/api/public/autorizacoes/$publicToken" -Method GET -SkipHttpErrorCheck
Check ($reuse.StatusCode -eq 404) 'Token público foi reutilizado.'

$revised = Api 'POST' "/api/ordens-servico/$($order.id)/orcamentos" @{ itens = @(@{ tipo = 'Servico'; descricao = $service.nome; quantidade = 1; valorUnitario = 120 }, @{ tipo = 'Peca'; descricao = 'Conector'; quantidade = 1; valorUnitario = 30 }); desconto = 0; validoAte = [DateTime]::UtcNow.AddDays(2).ToString('o'); prazoEstimado = 'Três dias úteis'; condicoes = 'Garantia conforme termo' }
Check ($revised.numeroVersao -eq 2) 'Nova condição não gerou outra versão do orçamento.'
Check ($revised.conteudoHash -ne $quote.conteudoHash) 'Hash não mudou com o prazo da nova versão.'
ExpectStatus 'POST' "/api/ordens-servico/$($order.id)/reparo/iniciar" $null 409
$revisedLink = Api 'POST' "/api/orcamentos/$($revised.id)/enviar-aprovacao"
$revisedToken = ($revisedLink.link -split '/')[-1]
Api 'POST' "/api/public/autorizacoes/$revisedToken/aprovar" @{ confirmouLeitura = $true } $false | Out-Null

Api 'PATCH' "/api/ordens-servico/$($order.id)/status" @{ status = 'AguardandoPeca'; justificativa = 'Conector ainda não disponível' } | Out-Null
ExpectStatus 'PATCH' "/api/ordens-servico/$($order.id)/status" @{ status = 'EmReparo' } 409
Api 'PATCH' "/api/ordens-servico/$($order.id)/status" @{ status = 'Aprovado' } | Out-Null
Api 'POST' "/api/ordens-servico/$($order.id)/reparo/iniciar" | Out-Null
$foreignConsumption = Invoke-WebRequest -Uri "$BaseUrl/api/ordens-servico/$($order.id)/pecas" -Method POST -ContentType 'application/json' -Body (@{ pecaId = $piece.id; quantidade = 1 } | ConvertTo-Json) -Headers @{ Authorization = "Bearer $($technicianLogin.token)" } -SkipHttpErrorCheck
Check ($foreignConsumption.StatusCode -eq 403) 'Técnico não atribuído consumiu peça em outra OS.'
Api 'PATCH' "/api/usuarios/$($technician.id)/desativar" | Out-Null
$revokedAccess = Invoke-WebRequest -Uri "$BaseUrl/api/ordens-servico/$($order.id)" -Method GET -Headers @{ Authorization = "Bearer $($technicianLogin.token)" } -SkipHttpErrorCheck
Check ($revokedAccess.StatusCode -eq 401) 'Token de usuário desativado continuou válido.'
Api 'PATCH' "/api/ordens-servico/$($order.id)/status" @{ status = 'AguardandoPeca'; justificativa = 'Aguardar ferramenta' } | Out-Null
ExpectStatus 'PATCH' "/api/ordens-servico/$($order.id)/status" @{ status = 'Aprovado' } 409
Api 'PATCH' "/api/ordens-servico/$($order.id)/status" @{ status = 'EmReparo' } | Out-Null
ExpectStatus 'POST' "/api/ordens-servico/$($order.id)/pecas" @{ pecaId = $piece.id; quantidade = 3 } 409
Api 'POST' "/api/ordens-servico/$($order.id)/pecas" @{ pecaId = $piece.id; quantidade = 1 } | Out-Null
Api 'POST' "/api/ordens-servico/$($order.id)/reparo/concluir" @{ servicosRealizados = 'Conector substituído.'; observacoes = 'Aparelho sem outras falhas aparentes.' } | Out-Null
$repairDetail = Api 'GET' "/api/ordens-servico/$($order.id)"
Check ($repairDetail.reparos[-1].observacoes -eq 'Aparelho sem outras falhas aparentes.') 'Observações do reparo não foram registradas.'
$testAnswers = @($tests | ForEach-Object { @{ itemId = $_.id; resultado = 'OK' } })
Api 'POST' "/api/ordens-servico/$($order.id)/testes" @{ aprovado = $true; resultado = 'Todos os testes passaram.'; itens = $testAnswers } | Out-Null
Api 'POST' "/api/ordens-servico/$($order.id)/pagamentos" @{ valor = 50; forma = 'Pix' } | Out-Null
ExpectStatus 'POST' "/api/ordens-servico/$($order.id)/pagamentos" @{ valor = 101; forma = 'Pix' } 409
Api 'POST' "/api/ordens-servico/$($order.id)/pagamentos" @{ valor = 100; forma = 'Dinheiro' } | Out-Null
$payment = Api 'GET' "/api/ordens-servico/$($order.id)/pagamentos"
Check ($payment.saldo -eq 0 -and $payment.statusFinanceiro -eq 'Quitado') 'Saldo ou status financeiro da OS incorreto.'
Api 'POST' "/api/ordens-servico/$($order.id)/entrega" @{ recebedor = 'Cliente Teste'; garantias = @(@{ servico = $service.nome; dias = $service.garantiaDias; condicoes = $service.condicoesGarantia }) } | Out-Null
$delivered = Api 'GET' "/api/ordens-servico/$($order.id)"
Check ($delivered.ordem.status -eq 'Entregue') 'OS não foi entregue.'
$warrantyOrder = Api 'POST' '/api/ordens-servico' @{ unidadeId = $unit.id; clienteId = $client.id; aparelhoId = $device.id; defeitoRelatado = 'Retorno em garantia'; prioridade = 'Normal'; ordemOrigemGarantiaId = $order.id; checklist = $answers }
Check ($warrantyOrder.ordemOrigemGarantiaId -eq $order.id) 'Retorno em garantia não foi vinculado.'
$irreparable = Api 'POST' '/api/ordens-servico' @{ unidadeId = $unit.id; clienteId = $client.id; aparelhoId = $device.id; defeitoRelatado = 'Falha sem conserto'; prioridade = 'Normal'; tiposReparo = @($repairType.id); checklist = $answers }
Api 'PUT' "/api/ordens-servico/$($irreparable.id)/acesso-dispositivo" @{ credencial = 'PIN-removido-no-encerramento' } | Out-Null
Api 'PATCH' "/api/ordens-servico/$($irreparable.id)/status" @{ status = 'AguardandoDiagnostico' } | Out-Null
Api 'PATCH' "/api/ordens-servico/$($irreparable.id)/status" @{ status = 'EmDiagnostico' } | Out-Null
Api 'PATCH' "/api/ordens-servico/$($irreparable.id)/status" @{ status = 'Irreparavel'; justificativa = 'Placa sem recuperação' } | Out-Null
ExpectStatus 'GET' "/api/ordens-servico/$($irreparable.id)/acesso-dispositivo" $null 404
$firstItem = $checklist[0]
Api 'PUT' "/api/configuracoes/checklist/$($firstItem.id)" @{ nome = "Item alterado $suffix"; ordem = $firstItem.ordem; ativo = $false; opcoes = @('OK'); permiteObservacao = $true } | Out-Null
$historic = Api 'GET' "/api/ordens-servico/$($order.id)"
$historicItem = @($historic.checklist | Where-Object templateItemId -eq $firstItem.id)[0]
Check ($historicItem.itemNome -eq $firstItem.nome -and $historicItem.itemOpcoes[0] -eq $firstItem.opcoes[0]) 'Alteração do template modificou o checklist histórico.'
$activeNow = (Api 'GET' '/api/configuracoes/checklist') | Where-Object ativo
$newAnswers = @($activeNow | ForEach-Object { @{ itemId = $_.id; resposta = $_.opcoes[0] } })
$newOrder = Api 'POST' '/api/ordens-servico' @{ unidadeId = $unit.id; clienteId = $client.id; aparelhoId = $device.id; defeitoRelatado = 'Novo atendimento'; prioridade = 'Normal'; checklist = $newAnswers }
$newDetail = Api 'GET' "/api/ordens-servico/$($newOrder.id)"
Check ($newDetail.checklist.Count -eq $checklist.Count - 1) 'Item desativado apareceu em nova OS.'
$report = Api 'GET' '/api/relatorios/resumo'
Check ($report.faturamento -ge 150 -and $report.ticketMedio -ge 0 -and $report.porStatus.Count -gt 0) 'Resumo gerencial não retornou os indicadores esperados.'
Write-Output "Fluxo integrado aprovado: OS $($order.numero), garantia OS $($warrantyOrder.numero)."
