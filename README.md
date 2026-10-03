# PortCell

Sistema web de assistência técnica em ASP.NET Core 10 LTS, React, Material UI, Entity Framework Core e PostgreSQL. A análise de requisitos fornecida pelo usuário orienta a implementação.

## Preparar o ambiente local

São necessários .NET SDK 10, Node.js 20+ e PostgreSQL 16. O arquivo `compose.yaml` oferece um PostgreSQL local. Copie `.env.example` para `.env`, troque a senha e execute `docker compose up -d`.

O repositório fixa a família do SDK em `global.json` e a ferramenta EF em `dotnet-tools.json`. Após instalar o SDK 10, execute `dotnet tool restore`. Neste ambiente de desenvolvimento, o SDK 10 foi instalado localmente em `.dotnet`; os comandos `dotnet` abaixo podem ser substituídos por `.\.dotnet\dotnet.exe`.

No PowerShell, configure as variáveis da API antes de iniciá-la:

```powershell
$env:PORTCELL_DB_PASSWORD = '<senha definida no .env>'
$env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=portcell;Username=portcell;Password=$env:PORTCELL_DB_PASSWORD"
$env:Jwt__Key = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$env:DeviceAccess__EncryptionKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$env:Admin__Email = 'admin@exemplo.com'
$env:Admin__Password = '<senha inicial com pelo menos 12 caracteres>'
$env:DataProtection__KeysPath = (Join-Path (Get-Location) '.local-keys')
```

Guarde as chaves `Jwt__Key` e `DeviceAccess__EncryptionKey` em um gerenciador de segredos e mantenha **os mesmos valores** entre reinícios. Perder a segunda impede recuperar credenciais de aparelhos ainda abertos. Proteja o diretório de `DataProtection__KeysPath` com permissões restritas e backup. Nunca salve segredos no repositório. Em produção, disponibilize a API por HTTPS, mantenha frontend e API no mesmo site para o cookie de renovação e configure `Frontend__Origin` com a origem exata do frontend.

Crie o esquema e o primeiro usuário com um comando explícito:

```powershell
dotnet run --project backend/PortCell.Api -- --bootstrap-admin
```

Para aplicar migrations posteriores sem recriar o usuário, use `dotnet run --project backend/PortCell.Api -- --migrate`. A aplicação normal não aplica migrations automaticamente:

```powershell
dotnet run --project backend/PortCell.Api
```

A API local usa `http://localhost:5000`; o OpenAPI está em `/openapi/v1.json` no ambiente Development. Em outro terminal:

```powershell
cd frontend
npm ci
npm run dev
```

O frontend chama `/api` no mesmo endereço usado pelo navegador. O Vite encaminha essas chamadas à API em `http://127.0.0.1:5000`, tanto no desenvolvimento quanto no preview. Configure `VITE_API_URL` antes do build somente se a API estiver em outra origem; nesse caso, configure também a origem exata permitida na API.

### Acesso pela rede Radmin VPN

Com o Radmin conectado e as chaves locais configuradas, execute `./scripts/start-radmin.ps1`. O script detecta o IP da interface Radmin, inicia a API em `127.0.0.1:5000` e o frontend compilado em duas interfaces: `127.0.0.1:5173` e `<IP-RADMIN>:5173`. Os outros computadores devem estar na mesma rede Radmin e abrir `http://<IP-RADMIN>:5173/`. O computador servidor precisa permanecer ligado e conectado ao Radmin. O acesso local continua em `http://localhost:5173/`.

O IP identificado em 02/10/2026 é `26.245.9.1`, portanto o endereço atual é `http://26.245.9.1:5173/`. A API e o PostgreSQL permanecem locais; somente o frontend e seu encaminhamento de `/api` são disponibilizados no Radmin. Login, renovação de sessão e relatórios usam o mesmo endereço do navegador. `Frontend__Origin` recebe a URL do Radmin para gerar links de aprovação; `Frontend__AllowedOrigins__0` e `Frontend__AllowedOrigins__1` preservam localhost e loopback. O proxy envia o IP de cada cliente, aceito pela API apenas a partir do proxy local, para manter os limites de requisição por cliente.

Para autorizar a porta quando o Firewall do Windows estiver ativo, execute `./scripts/setup-radmin-firewall.ps1` em PowerShell **como administrador**. A regra permite somente TCP 5173 na interface Radmin e endereços de origem `26.0.0.0/8`. Os scripts não alteram os perfis gerais do firewall. Para encerrar os serviços registrados, use `./scripts/stop-radmin.ps1`; o PostgreSQL permanece ativo. Logs e PIDs ficam em `.local-keys/radmin`, fora do Git. Após editar o código, pare os serviços, compile novamente (`dotnet build` e `npm run build`) e reinicie pelo script.

### Envio da OS por e-mail

A nova abertura de OS gera um PDF A4 de uma página e tenta enviá-lo ao e-mail do cliente, junto do orçamento e do link de aprovação. O relatório usa fundo branco, textos pretos, marcações numeradas e assinaturas centralizadas; números próximos são separados com linhas apontando para os locais originais. Configure o SMTP no ambiente da API:

```powershell
$env:Email__SmtpHost = 'smtp.exemplo.com'
$env:Email__SmtpPort = '587'
$env:Email__FromAddress = 'atendimento@exemplo.com'
$env:Email__Username = 'atendimento@exemplo.com'
$env:Email__Password = '<credencial do SMTP>'
$env:Email__EnableSsl = 'true'
```

Sem servidor SMTP ou e-mail válido do cliente, a OS e o PDF continuam disponíveis; a interface mostra o motivo e permite tentar o envio novamente. Mantenha as credenciais fora do repositório. Configure `Frontend__Origin` com a URL pública do frontend para que o link de aprovação no e-mail funcione.

O termo de retirada foi pesquisado em 01/10/2026 para a assistência no Rio de Janeiro/RJ. Não foi encontrada norma vigente aplicável à loja que determine um prazo automático de armazenamento, abandono ou descarte. O relatório cita o CDC (arts. 6º, III, e 51, IV) e o Código Civil (art. 1.275, III), prevê aviso de retirada e notificação formal e não estipula cobrança de guarda: prazo e preço precisam ser definidos e informados previamente ao cliente. O atraso, por si só, não autoriza venda, descarte ou incorporação. A regra estadual do Paraná de 180 dias não é aplicada ao RJ; o PL federal 2545/2022 permanece aguardando parecer na Câmara. Fontes: [CDC](https://www.planalto.gov.br/ccivil_03/leis/l8078compilado.htm), [Código Civil](https://www.planalto.gov.br/ccivil_03/leis/2002/l10406compilada.htm), [orientação do Procon Campinas, itens 16 a 19](https://procon.campinas.sp.gov.br/sites/procon.campinas.sp.gov.br/files/arquivos-pesquisa/CARTILHA%20ASSISTENCIA%20TECNICA%20REVISADA-2-17.pdf), [tramitação do PL 2545/2022](https://www.camara.leg.br/proposicoesWeb/fichadetramitacao?idProposicao=2335241).

## Validar

```powershell
dotnet test backend/PortCell.Tests/PortCell.Tests.csproj
cd frontend
npm run build
```

As migrations estão versionadas em `backend/PortCell.Api/Migrations`. Os scripts `scripts/backup-postgres.ps1` e `scripts/restore-postgres.ps1` usam `pg_dump`/`pg_restore` com `PGHOST`, `PGDATABASE`, `PGUSER`, `PGPASSWORD` e, opcionalmente, `PGPORT`. A restauração recusa bancos com objetos no esquema `public`. Agende o backup fora da aplicação, defina retenção e teste a restauração em banco vazio antes de produção.

Para validar o fluxo de ponta a ponta em um banco **isolado**, inicie a API apontando `ConnectionStrings__Postgres` para esse banco, configure `Admin__Email` e `Admin__Password` e execute `scripts/smoke-flow.ps1`. O script cria dados de teste: cliente, aparelho, OS, peça, orçamento, pagamentos e retorno em garantia. Também verifica nova aprovação após alterar o orçamento, pausa e retomada por falta de peça, além das rejeições de reparo sem autorização, estoque insuficiente, pagamento acima do saldo e reutilização do link público.

O fluxo de atendimento direto tem uma verificação separada em `scripts/smoke-atendimento-inicial.ps1`, apontada por padrão para `http://127.0.0.1:5001` e também destinada **somente a um banco isolado**. Ela confere a exigência de ao menos um ponto de defeito, a criação do orçamento sem diagnóstico, o PDF, a aprovação pública e o estado do e-mail quando não há SMTP.

## Escopo implementado

- Login interno com hash de senha, JWT curto, refresh token revogável e perfis; administração inicial de usuários.
- Cliente e aparelho, empresa/unidade, busca de aparelho, checklist de entrada configurável com opções específicas e snapshot histórico por OS.
- Novo atendimento em três etapas: cliente e aparelho, problema e inspeção, orçamento. O preenchimento permanece ao voltar entre etapas; cada avanço valida os dados necessários. Cadastro conjunto de cliente e aparelho em um modal, com busca dos cadastros existentes e seleção automática após salvar. Chips de problemas frequentes compõem o relato sem substituir a inspeção. O aparelho 3D gira ao arrastar com mouse/toque e pelos botões de orientação; clicar em um componente abre o registro do defeito. Inclui tela, câmeras, Face ID, botões laterais, conector, tampa traseira e registro da bateria interna. Os defeitos podem ser editados/removidos e aparecem identificados na OS e no PDF. Peças e serviços entram no orçamento inicial e a OS segue diretamente para aprovação, sem etapa de diagnóstico técnico.
- A inspeção aceita até 24 locais, com até 8 problemas por local e observações de até 500 caracteres. A marcação livre funciona nas seis faces, incluindo riscos e amassados nas laterais; a tampa traseira inclui as opções quebrada, riscada e ausente. Registros antigos continuam legíveis. O PDF de uma página numera todos os locais e resume textos extensos; o conteúdo completo fica nos detalhes da OS.
- O orçamento usa um seletor com busca de serviços e peças, criação de serviço personalizado, conferência de quantidade/preço, edição e remoção de itens. O resumo mostra subtotal, desconto, total, prazo e validade.
- Relatório PDF A4 de uma página para assinatura, com as seis faces do aparelho em 2D, pontos de defeito, legenda e orçamento; envio da cópia por SMTP com retorno claro quando não estiver configurado.
- Abertura e consulta de OS, atribuição de técnico, transições controladas, pausa por falta de peça e timeline.
- Diagnóstico, orçamento versionado com prazo estimado e hash do conteúdo, autorização manual ou por link público com token de uso único, expiração e rastreabilidade.
- Tipos de reparo e regra de desbloqueio; credencial do aparelho cifrada, restrita ao técnico atribuído e removida ao encerrar a OS.
- Reparo, consumo rastreável de peça, testes finais configuráveis, pagamentos parciais e estorno, entrega e garantias por serviço, com catálogo administrável.
- Dashboard, relatório resumido por período, consultas filtradas e trilhas de auditoria operacional e administrativa.

## Pendências para cobrir toda a análise

- Completar as telas de edição e busca paginada de clientes, aparelhos e OS; hoje a API expõe mais campos e filtros do que a interface usa.
- Parametrizar canais de autorização, status auxiliares e obrigações por tipo de serviço; atualmente há canais e transições definidos no código.
- Refinar relatórios de tempo por etapa, reincidência e histórico de aprovação; o resumo atual cobre os indicadores operacionais e financeiros básicos.
- Validar responsividade, acessibilidade e permissões por perfil em navegadores reais; a validação atual cobre compilação e fluxo HTTP.

## Antes de produção

O fluxo completo foi exercitado em PostgreSQL 18 local e um backup com 16 migrations foi restaurado em outro banco vazio; a recusa de restauração em banco ocupado também foi verificada. Para implantação ainda é preciso configurar HTTPS, executar backups recorrentes e testes periódicos de restauração, definir retenção de dados, ampliar os testes de integração e validar as telas em navegadores e dispositivos reais. Funcionalidades futuras do documento, como fotos, integrações automáticas, notas fiscais e operação multi-loja completa, não estão incluídas.
