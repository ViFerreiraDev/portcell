param([string]$BaseUrl = 'http://127.0.0.1:5001')

$ErrorActionPreference = 'Stop'
if ($BaseUrl -notmatch '^http://127\.0\.0\.1:[0-9]+$') { throw 'Use somente uma API local isolada para este teste.' }
if (-not $env:Admin__Email -or -not $env:Admin__Password) { throw 'Configure as credenciais do banco de teste.' }
$token = (Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType 'application/json' -Body (@{ email = $env:Admin__Email; senha = $env:Admin__Password } | ConvertTo-Json)).token
$headers = @{ Authorization = "Bearer $token" }
function Api([string]$method, [string]$path, $body = $null) {
    $args = @{ Uri = "$BaseUrl$path"; Method = $method; Headers = $headers }
    if ($null -ne $body) { $args.ContentType = 'application/json'; $args.Body = ($body | ConvertTo-Json -Depth 10 -Compress) }
    Invoke-RestMethod @args
}
function Check([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }

$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$client = Api 'POST' '/api/clientes' @{ nome = "Cliente $suffix"; telefone = '11999999999'; email = "teste-$suffix@example.test" }
$device = Api 'POST' '/api/aparelhos' @{ clienteId = $client.id; marca = 'Samsung'; modelo = 'Galaxy de teste'; imei = "35$suffix" }
$unit = (Api 'GET' '/api/unidades')[0]
$piece = Api 'POST' '/api/pecas' @{ sku = "TELA-$suffix"; descricao = 'Tela de teste'; custo = 30; preco = 90; estoqueMinimo = 0 }
$quote = @{ itens = @(
    @{ tipo = 'Servico'; descricao = 'Troca de tela'; quantidade = 1; valorUnitario = 110 },
    @{ tipo = 'Peca'; descricao = 'Texto ignorado pelo servidor'; quantidade = 1; valorUnitario = 90; pecaId = $piece.id }
); desconto = 10; validoAte = [DateTime]::UtcNow.AddDays(7).ToString('o'); prazoEstimado = '2 dias úteis'; condicoes = 'Após aprovação' }
$body = @{ unidadeId = $unit.id; clienteId = $client.id; aparelhoId = $device.id; defeitoRelatado = 'Tela quebrada'; observacoes = 'Sem outros danos visíveis'; prioridade = 'Normal'; pontosAvaria = @(); tiposReparo = @(); orcamento = $quote }
$invalid = Invoke-WebRequest -Uri "$BaseUrl/api/ordens-servico/atendimento-inicial" -Method Post -Headers $headers -ContentType 'application/json' -Body ($body | ConvertTo-Json -Depth 10 -Compress) -SkipHttpErrorCheck
Check ($invalid.StatusCode -eq 400) 'A abertura sem ponto de defeito foi aceita.'
$body.pontosAvaria = @(
    @{ face = 'frente'; x = 0.42; y = 0.27; componente = 'tela'; defeitos = @('Trincada / quebrada', 'Touch com falhas'); observacao = 'Canto inferior danificado' },
    @{ face = 'inferior'; x = 0.55; y = 0.7; componente = 'conector'; defeito = 'Mau contato' },
    @{ face = 'esquerda'; x = 0.25; y = 0.31; defeitos = @('Riscado', 'Amassado'); observacao = 'Dano na lateral' },
    @{ face = 'superior'; x = 0.35; y = 0.5; defeitos = @(); observacao = 'Acabamento descascado' }
)
$created = Api 'POST' '/api/ordens-servico/atendimento-inicial' $body
Check ($created.ordem.status -eq 'AguardandoAprovacao' -and $created.ordem.atendimentoDireto) 'OS não entrou diretamente em aprovação.'
Check ($created.orcamento.total -eq 190 -and $created.orcamento.numeroVersao -eq 1) 'Orçamento inicial incorreto.'
Check ($created.email.status -eq 'nao-configurado') 'O e-mail deveria sinalizar ausência de SMTP neste teste.'
$detail = Api 'GET' "/api/ordens-servico/$($created.ordem.id)"
Check ($detail.pontosAvaria.Count -eq 4 -and $detail.checklist.Count -eq 0) 'A inspeção visual não foi preservada na OS.'
$screenPoint = $detail.pontosAvaria | Where-Object componente -eq 'tela'
Check ($screenPoint.defeito -eq 'Trincada / quebrada' -and $screenPoint.x -eq 0.5 -and $screenPoint.y -eq 0.53) 'O componente e o defeito não foram gravados com sua posição no aparelho.'
Check ($screenPoint.defeitos.Count -eq 2 -and $screenPoint.observacao -eq 'Canto inferior danificado') 'Múltiplos defeitos e observações não foram preservados.'
$sidePoint = $detail.pontosAvaria | Where-Object face -eq 'esquerda'
Check ($sidePoint.x -eq 0.25 -and $sidePoint.y -eq 0.31 -and $sidePoint.defeitos.Count -eq 2) 'A marcação livre não preservou a localização e os problemas.'
$pdf = Invoke-WebRequest -Uri "$BaseUrl$($created.relatorioUrl)" -Headers $headers
Check ((($pdf.Headers['Content-Type'] -join '') -match 'application/pdf')) 'Relatório não foi devolvido como PDF.'
Check ($pdf.RawContentStream.Length -gt 1000) 'PDF vazio ou incompleto.'
$initialPdfText = [Text.Encoding]::Latin1.GetString($pdf.RawContentStream.ToArray())
$initialPdfText = ([regex]::Matches($initialPdfText, '\((.*?)\) Tj') | ForEach-Object { $_.Groups[1].Value }) -join ' '
Check ($initialPdfText.Contains('Tela: Trincada / quebrada') -and $initialPdfText.Contains('Conector de carga: Mau contato')) 'O relatório omitiu os defeitos dos componentes.'
Check ($initialPdfText.Contains('Touch com falhas') -and $initialPdfText.Contains('Canto inferior') -and $initialPdfText.Contains('danificado') -and $initialPdfText.Contains('Dano na lateral') -and $initialPdfText.Contains('Acabamento descascado')) 'O relatório omitiu problemas ou observações das áreas livres.'
if ($env:PORTCELL_PDF_SAMPLE) { [IO.File]::WriteAllBytes($env:PORTCELL_PDF_SAMPLE, $pdf.RawContentStream.ToArray()) }
$publicToken = ($created.linkAprovacao -split '/')[-1]
$public = Invoke-RestMethod -Uri "$BaseUrl/api/public/autorizacoes/$publicToken"
Check ($public.diagnostico -eq 'Tela quebrada') 'A aprovação pública dependeu de diagnóstico técnico.'
$quote.prazoEstimado = '3 dias úteis'
$quote.itens[0].valorUnitario = 120
$revised = Api 'POST' "/api/ordens-servico/$($created.ordem.id)/orcamentos" $quote
Check ($revised.numeroVersao -eq 2 -and $revised.total -eq 200) 'Revisão do orçamento direto falhou.'
$revisedPdf = Invoke-WebRequest -Uri "$BaseUrl$($created.relatorioUrl)" -Headers $headers
$pdfText = [Text.Encoding]::Latin1.GetString($revisedPdf.RawContentStream.ToArray())
Check ($pdfText.Contains('VERSÃO 2') -and $pdfText.Contains('3 dias úteis')) 'O relatório não refletiu o orçamento vigente.'
$resent = Api 'POST' "/api/ordens-servico/$($created.ordem.id)/enviar-email"
Check ($resent.email.status -eq 'nao-configurado') 'Reenvio sem SMTP não apresentou o estado correto.'
Check (-not [string]::IsNullOrWhiteSpace($resent.linkAprovacao)) 'Reenvio não gerou link para a versão vigente.'
Write-Output "Atendimento inicial validado: OS #$($created.ordem.numero), orçamento R$ $($created.orcamento.total), relatório PDF e aprovação pública."
