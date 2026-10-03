using Microsoft.EntityFrameworkCore;

namespace PortCell.Api;

public static class AtendimentoInicialEndpoints
{
    public static void MapAtendimentoInicialEndpoints(this RouteGroupBuilder api)
    {
        var orders = api.MapGroup("/ordens-servico");
        orders.MapPost("/atendimento-inicial", async (AtendimentoInicialRequest request, HttpContext context, AppDbContext db,
            IConfiguration configuration, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.DefeitoRelatado) || request.DefeitoRelatado.Length > 4000 ||
                request.Observacoes?.Length > 4000 || request.Prioridade is not ("Normal" or "Alta"))
                return Results.BadRequest("Descreva o problema e confira os dados do atendimento.");
            if (!AtendimentoRules.PontosValidos(request.PontosAvaria))
                return Results.BadRequest("Registre ao menos um defeito no aparelho e confira o componente e a descrição (máximo de 24 registros).");
            if (!AtendimentoRules.OrcamentoInicialValido(request.Orcamento) ||
                !OrcamentoRules.TryCalculate(request.Orcamento, out var subtotal, out var total))
                return Results.BadRequest("O orçamento inicial deve ter de 1 a 10 itens válidos, incluindo as peças do catálogo.");

            var company = context.User.EmpresaId();
            var user = context.User.UsuarioId();
            if (!await db.Unidades.AnyAsync(x => x.Id == request.UnidadeId && x.EmpresaId == company, cancellationToken))
                return Results.BadRequest("Unidade inválida.");
            var client = await db.Clientes.FirstOrDefaultAsync(x => x.Id == request.ClienteId && x.EmpresaId == company && x.Ativo, cancellationToken);
            if (client is null) return Results.BadRequest("Cliente inválido.");
            var device = await db.Aparelhos.FirstOrDefaultAsync(x => x.Id == request.AparelhoId && x.ClienteId == client.Id && x.EmpresaId == company, cancellationToken);
            if (device is null) return Results.BadRequest("Aparelho não pertence ao cliente.");
            var typeIds = request.TiposReparo ?? [];
            if (typeIds.Count != typeIds.Distinct().Count()) return Results.BadRequest("Tipos de reparo duplicados.");
            var types = await db.TiposReparo.Where(x => x.EmpresaId == company && x.Ativo && typeIds.Contains(x.Id)).ToListAsync(cancellationToken);
            if (types.Count != typeIds.Count) return Results.BadRequest("Tipo de reparo inválido ou inativo.");
            var pieceIds = request.Orcamento.Itens.Where(x => x.PecaId.HasValue).Select(x => x.PecaId!.Value).Distinct().ToArray();
            var pieces = await db.Pecas.AsNoTracking().Where(x => x.EmpresaId == company && pieceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
            if (pieces.Count != pieceIds.Length) return Results.BadRequest("Peça do orçamento inválida.");
            var items = request.Orcamento.Itens.Select(item => item.Tipo == "Peca" && item.PecaId.HasValue
                ? item with { Descricao = pieces[item.PecaId.Value].Descricao }
                : item with { Descricao = item.Descricao.Trim() }).ToList();
            var normalizedQuote = request.Orcamento with { Itens = items };
            if (!OrcamentoRules.TryCalculate(normalizedQuote, out subtotal, out total)) return Results.BadRequest("Orçamento inválido.");

            var companyEntity = await db.Empresas.AsNoTracking().FirstAsync(x => x.Id == company, cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var order = new OrdemServico
            {
                EmpresaId = company, UnidadeId = request.UnidadeId, ClienteId = client.Id, AparelhoId = device.Id,
                AtendenteId = user, DefeitoRelatado = request.DefeitoRelatado.Trim(), Observacoes = request.Observacoes?.Trim(),
                Prioridade = request.Prioridade, Status = StatusOs.AguardandoAprovacao, AtendimentoDireto = true,
                AcessoNecessario = types.Any(x => x.RequerDesbloqueio)
            };
            var quote = new OrcamentoVersao
            {
                EmpresaId = company, OrdemServicoId = order.Id, NumeroVersao = 1, Subtotal = subtotal, Desconto = normalizedQuote.Desconto,
                Total = total, ValidoAte = normalizedQuote.ValidoAte.ToUniversalTime(), Condicoes = normalizedQuote.Condicoes?.Trim(),
                PrazoEstimado = normalizedQuote.PrazoEstimado.Trim(), ConteudoHash = OrcamentoRules.ContentHash(normalizedQuote, subtotal, total)
            };
            var savedItems = items.Select(item => new OrcamentoItem { OrcamentoVersaoId = quote.Id, Tipo = item.Tipo,
                Descricao = item.Descricao.Trim(), Quantidade = item.Quantidade, ValorUnitario = item.ValorUnitario, PecaId = item.PecaId }).ToList();
            var points = request.PontosAvaria.Select(point =>
            {
                var component = point.Componente is null ? null : AtendimentoRules.Componentes[point.Componente];
                var defects = (point.Defeitos ?? (point.Defeito is null ? [] : new[] { point.Defeito })).Select(value => value.Trim()).ToArray();
                return new PontoAvaria { EmpresaId = company, OrdemServicoId = order.Id,
                    Face = component?.Face ?? point.Face, X = component?.X ?? point.X, Y = component?.Y ?? point.Y,
                    Componente = point.Componente, Defeito = defects.FirstOrDefault(), Defeitos = defects,
                    Observacao = string.IsNullOrWhiteSpace(point.Observacao) ? null : point.Observacao.Trim() };
            }).ToList();
            db.OrdensServico.Add(order);
            db.Orcamentos.Add(quote);
            db.OrcamentoItens.AddRange(savedItems);
            db.PontosAvaria.AddRange(points);
            foreach (var type in types) db.OsTiposReparo.Add(new OsTipoReparo { OrdemServicoId = order.Id, TipoReparoId = type.Id });
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = order.Id, UsuarioId = user,
                Evento = "Atendimento inicial concluído", Detalhes = $"Orçamento v1 apresentado; {points.Count} ponto(s) de avaria registrado(s)" });
            var raw = PublicToken.Generate();
            db.SolicitacoesAutorizacao.Add(new SolicitacaoAutorizacao { EmpresaId = company, OrcamentoVersaoId = quote.Id,
                TokenHash = PublicToken.Hash(raw), ExpiraEm = DateTime.UtcNow.AddDays(7) });
            byte[] report;
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                report = OsReportPdf.Build(new OsReportData(companyEntity, order, client, device, quote, savedItems, points));
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException) { return Results.Conflict("Os dados mudaram durante a abertura. Atualize e tente novamente."); }

            var origin = configuration["Frontend:Origin"] ?? "http://localhost:5173";
            var approvalLink = $"{origin.TrimEnd('/')}/aprovar/{raw}";
            EmailOsResult email;
            try { email = await EmailOsService.SendAsync(configuration, client.Email, order.Numero, quote.Total, approvalLink, report, cancellationToken); }
            catch (Exception exception)
            {
                loggerFactory.CreateLogger("AtendimentoInicial").LogError(exception, "Falha no envio da OS {Numero}", order.Numero);
                email = new("falhou", "A OS foi criada, mas o envio por e-mail falhou. Tente novamente.");
            }
            if (email.Status == "enviado")
            {
                db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = order.Id, UsuarioId = user,
                    Evento = "OS e orçamento enviados por e-mail" });
                try { await db.SaveChangesAsync(cancellationToken); }
                catch (DbUpdateException exception) { loggerFactory.CreateLogger("AtendimentoInicial").LogError(exception, "Falha ao registrar envio da OS {Numero}", order.Numero); }
            }
            return Results.Created($"/api/ordens-servico/{order.Id}", new
            {
                ordem = order, orcamento = quote, linkAprovacao = approvalLink, email,
                relatorioUrl = $"/api/ordens-servico/{order.Id}/relatorio.pdf"
            });
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));

        orders.MapGet("/{id:guid}/relatorio.pdf", async (Guid id, HttpContext context, AppDbContext db, CancellationToken cancellationToken) =>
        {
            var data = await LoadReport(id, context.User.EmpresaId(), db, cancellationToken);
            return data is null ? Results.NotFound() : Results.File(OsReportPdf.Build(data), "application/pdf", $"PortCell-OS-{data.Ordem.Numero}.pdf");
        });

        orders.MapPost("/{id:guid}/enviar-email", async (Guid id, HttpContext context, AppDbContext db,
            IConfiguration configuration, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
        {
            var company = context.User.EmpresaId();
            var data = await LoadReport(id, company, db, cancellationToken);
            if (data is null) return Results.NotFound();
            string? link = null;
            if (data.Ordem.Status == StatusOs.AguardandoAprovacao)
            {
                var latest = await db.Orcamentos.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company)
                    .OrderByDescending(x => x.NumeroVersao).FirstAsync(cancellationToken);
                var raw = PublicToken.Generate();
                db.SolicitacoesAutorizacao.Add(new SolicitacaoAutorizacao { EmpresaId = company, OrcamentoVersaoId = latest.Id,
                    TokenHash = PublicToken.Hash(raw), ExpiraEm = DateTime.UtcNow.AddDays(7) });
                await db.SaveChangesAsync(cancellationToken);
                var origin = configuration["Frontend:Origin"] ?? "http://localhost:5173";
                link = $"{origin.TrimEnd('/')}/aprovar/{raw}";
            }
            EmailOsResult email;
            try { email = await EmailOsService.SendAsync(configuration, data.Cliente.Email, data.Ordem.Numero,
                data.Orcamento.Total, link, OsReportPdf.Build(data), cancellationToken); }
            catch (Exception exception)
            {
                loggerFactory.CreateLogger("AtendimentoInicial").LogError(exception, "Falha no reenvio da OS {Numero}", data.Ordem.Numero);
                email = new("falhou", "O envio por e-mail falhou. Tente novamente.");
            }
            if (email.Status == "enviado")
            {
                db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(),
                    Evento = "OS e orçamento reenviados por e-mail" });
                try { await db.SaveChangesAsync(cancellationToken); }
                catch (DbUpdateException exception) { loggerFactory.CreateLogger("AtendimentoInicial").LogError(exception, "Falha ao registrar reenvio da OS {Numero}", data.Ordem.Numero); }
            }
            return Results.Ok(new { email, linkAprovacao = link });
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
    }

    private static async Task<OsReportData?> LoadReport(Guid id, Guid company, AppDbContext db, CancellationToken cancellationToken)
    {
        var order = await db.OrdensServico.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company && x.AtendimentoDireto, cancellationToken);
        if (order is null) return null;
        var quote = await db.Orcamentos.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company)
            .OrderByDescending(x => x.NumeroVersao).FirstOrDefaultAsync(cancellationToken);
        if (quote is null) return null;
        var client = await db.Clientes.AsNoTracking().FirstAsync(x => x.Id == order.ClienteId, cancellationToken);
        var device = await db.Aparelhos.AsNoTracking().FirstAsync(x => x.Id == order.AparelhoId, cancellationToken);
        var companyEntity = await db.Empresas.AsNoTracking().FirstAsync(x => x.Id == company, cancellationToken);
        var items = await db.OrcamentoItens.AsNoTracking().Where(x => x.OrcamentoVersaoId == quote.Id).OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var points = await db.PontosAvaria.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company).ToListAsync(cancellationToken);
        return new OsReportData(companyEntity, order, client, device, quote, items, points);
    }
}
