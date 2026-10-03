using Microsoft.EntityFrameworkCore;

namespace PortCell.Api;

public static class OrcamentoEndpoints
{
    public static void MapOrcamentoEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/ordens-servico/{id:guid}/diagnosticos", async (Guid id, DiagnosticoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Descricao) || request.Descricao.Length > 4000 ||
                string.IsNullOrWhiteSpace(request.ResumoCliente) || request.ResumoCliente.Length > 2000 ||
                request.CausaProvavel?.Length > 2000 || request.Recomendacao?.Length > 2000 || request.ObservacoesInternas?.Length > 4000)
                return Results.BadRequest("Diagnóstico técnico e resumo para o cliente são obrigatórios.");
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (order.Status != StatusOs.EmDiagnostico) return Results.Conflict("A OS não está em diagnóstico.");
            var user = context.User.UsuarioId();
            if (context.User.IsInRole(Perfis.Tecnico) && order.TecnicoId.HasValue && order.TecnicoId != user) return Results.Forbid();
            var diagnosis = new Diagnostico { EmpresaId = company, OrdemServicoId = id, TecnicoId = user, Descricao = request.Descricao.Trim(),
                ResumoCliente = request.ResumoCliente.Trim(), CausaProvavel = request.CausaProvavel?.Trim(), Recomendacao = request.Recomendacao?.Trim(), ObservacoesInternas = request.ObservacoesInternas?.Trim() };
            db.Diagnosticos.Add(diagnosis);
            order.TecnicoId ??= user;
            order.Status = StatusOs.AguardandoOrcamento;
            order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = user, Evento = "Diagnóstico concluído" });
            await db.SaveChangesAsync();
            return Results.Created($"/api/ordens-servico/{id}", diagnosis);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Tecnico));

        api.MapGet("/ordens-servico/{id:guid}/orcamentos", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            if (!await db.OrdensServico.AnyAsync(x => x.Id == id && x.EmpresaId == company)) return Results.NotFound();
            var versions = await db.Orcamentos.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company).OrderByDescending(x => x.NumeroVersao).ToListAsync();
            return Results.Ok(versions);
        });
        api.MapGet("/orcamentos/{id:guid}", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var quote = await db.Orcamentos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (quote is null) return Results.NotFound();
            var items = await db.OrcamentoItens.AsNoTracking().Where(x => x.OrcamentoVersaoId == id).ToListAsync();
            return Results.Ok(new { orcamento = quote, itens = items });
        });
        api.MapPost("/ordens-servico/{id:guid}/orcamentos", async (Guid id, OrcamentoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (!OrcamentoRules.TryCalculate(request, out var subtotal, out var total))
                return Results.BadRequest("Itens, desconto, validade ou condições inválidos.");
            var company = context.User.EmpresaId();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (order.Status is not (StatusOs.AguardandoOrcamento or StatusOs.AguardandoAprovacao or StatusOs.Aprovado or StatusOs.OrcamentoRecusado))
                return Results.Conflict("A OS não está apta a receber orçamento.");
            if ((await db.Pagamentos.Where(x => x.OrdemServicoId == id).SumAsync(x => (decimal?)x.Valor) ?? 0) != 0)
                return Results.Conflict("Há pagamentos registrados. Estorne-os antes de alterar o orçamento.");
            if (!order.AtendimentoDireto && !await db.Diagnosticos.AnyAsync(x => x.OrdemServicoId == id && x.EmpresaId == company))
                return Results.Conflict("Registre o diagnóstico antes do orçamento.");
            var pieceIds = request.Itens.Where(x => x.PecaId.HasValue).Select(x => x.PecaId!.Value).Distinct().ToArray();
            if (await db.Pecas.CountAsync(x => x.EmpresaId == company && pieceIds.Contains(x.Id)) != pieceIds.Length)
                return Results.BadRequest("Peça do orçamento inválida.");
            var next = (await db.Orcamentos.Where(x => x.OrdemServicoId == id && x.EmpresaId == company).MaxAsync(x => (int?)x.NumeroVersao) ?? 0) + 1;
            var quote = new OrcamentoVersao { EmpresaId = company, OrdemServicoId = id, NumeroVersao = next, Subtotal = subtotal,
                Desconto = request.Desconto, Total = total, ValidoAte = request.ValidoAte.ToUniversalTime(), Condicoes = request.Condicoes?.Trim(),
                PrazoEstimado = request.PrazoEstimado.Trim(), ConteudoHash = OrcamentoRules.ContentHash(request, subtotal, total) };
            db.Orcamentos.Add(quote);
            foreach (var item in request.Itens)
                db.OrcamentoItens.Add(new OrcamentoItem { OrcamentoVersaoId = quote.Id, Tipo = item.Tipo, Descricao = item.Descricao.Trim(), Quantidade = item.Quantidade, ValorUnitario = item.ValorUnitario, PecaId = item.PecaId });
            order.Status = StatusOs.AguardandoAprovacao;
            order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Evento = $"Orçamento v{next} apresentado" });
            try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("A OS foi alterada. Atualize os dados."); }
            catch (DbUpdateException) { return Results.Conflict("Outra versão do orçamento foi criada. Atualize os dados."); }
            return Results.Created($"/api/orcamentos/{quote.Id}", quote);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));

        api.MapPost("/orcamentos/{id:guid}/enviar-aprovacao", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var quote = await db.Orcamentos.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (quote is null) return Results.NotFound();
            var order = await db.OrdensServico.FirstAsync(x => x.Id == quote.OrdemServicoId);
            if (order.Status != StatusOs.AguardandoAprovacao || !await db.Orcamentos.AnyAsync(x => x.Id == id && x.OrdemServicoId == order.Id && x.NumeroVersao == db.Orcamentos.Where(y => y.OrdemServicoId == order.Id).Max(y => y.NumeroVersao)))
                return Results.Conflict("Apenas a versão vigente aguardando aprovação pode ser enviada.");
            var raw = PublicToken.Generate();
            var solicitation = new SolicitacaoAutorizacao { EmpresaId = company, OrcamentoVersaoId = id, TokenHash = PublicToken.Hash(raw), ExpiraEm = DateTime.UtcNow.AddDays(7) };
            db.SolicitacoesAutorizacao.Add(solicitation);
            await db.SaveChangesAsync();
            var origin = context.RequestServices.GetRequiredService<IConfiguration>()["Frontend:Origin"] ?? "http://localhost:5173";
            return Results.Ok(new { link = $"{origin.TrimEnd('/')}/aprovar/{raw}", expiraEm = solicitation.ExpiraEm });
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));

        api.MapPost("/ordens-servico/{id:guid}/autorizacoes", async (Guid id, AutorizacaoManualRequest request, HttpContext context, AppDbContext db) =>
        {
            if (request.Canal is not ("WhatsApp" or "Email" or "Assinatura" or "Outro") || string.IsNullOrWhiteSpace(request.Evidencia) || request.Evidencia.Length > 1000)
                return Results.BadRequest("Canal e referência da evidência são obrigatórios.");
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            var quote = await db.Orcamentos.FirstOrDefaultAsync(x => x.Id == request.OrcamentoVersaoId && x.OrdemServicoId == id && x.EmpresaId == company);
            if (quote is null) return Results.BadRequest("Versão do orçamento inválida.");
            var latest = await db.Orcamentos.Where(x => x.OrdemServicoId == id).MaxAsync(x => x.NumeroVersao);
            if (quote.NumeroVersao != latest || quote.ValidoAte <= DateTime.UtcNow || order.Status != StatusOs.AguardandoAprovacao)
                return Results.Conflict("Apenas a versão vigente e válida pode ser autorizada.");
            var authorization = new Autorizacao { EmpresaId = company, OrdemServicoId = id, OrcamentoVersaoId = quote.Id, Canal = request.Canal,
                Aprovado = request.Aprovado, UsuarioId = context.User.UsuarioId(), Evidencia = request.Evidencia.Trim() };
            db.Autorizacoes.Add(authorization);
            order.Status = request.Aprovado ? StatusOs.Aprovado : StatusOs.OrcamentoRecusado;
            order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Evento = request.Aprovado ? "Orçamento autorizado manualmente" : "Orçamento recusado manualmente", Detalhes = $"Canal: {request.Canal}; versão: {quote.NumeroVersao}" });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("A OS foi alterada. Atualize os dados."); }
            return Results.Created($"/api/ordens-servico/{id}", authorization);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
    }

    public static void MapAutorizacaoPublicaEndpoints(this WebApplication app)
    {
        var publicApi = app.MapGroup("/api/public/autorizacoes").RequireRateLimiting("public-authorization");
        publicApi.MapGet("/{token}", async (string token, AppDbContext db) =>
        {
            var solicitation = await FindValid(token, db);
            if (solicitation is null) return Results.NotFound();
            var quote = await db.Orcamentos.AsNoTracking().FirstAsync(x => x.Id == solicitation.OrcamentoVersaoId);
            var order = await db.OrdensServico.AsNoTracking().FirstAsync(x => x.Id == quote.OrdemServicoId);
            if (order.Status != StatusOs.AguardandoAprovacao || quote.ValidoAte <= DateTime.UtcNow || !await IsLatest(quote, db)) return Results.NotFound();
            var device = await db.Aparelhos.AsNoTracking().FirstAsync(x => x.Id == order.AparelhoId);
            var diagnosis = await db.Diagnosticos.AsNoTracking().Where(x => x.OrdemServicoId == order.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync();
            var items = await db.OrcamentoItens.AsNoTracking().Where(x => x.OrcamentoVersaoId == quote.Id).ToListAsync();
            return Results.Ok(new { numeroOs = order.Numero, aparelho = $"{device.Marca} {device.Modelo}", diagnostico = diagnosis?.ResumoCliente ?? order.DefeitoRelatado,
                versao = quote.NumeroVersao, itens = items.Select(x => new { x.Tipo, x.Descricao, x.Quantidade, x.ValorUnitario }), quote.Subtotal, quote.Desconto, quote.Total, quote.Condicoes, quote.PrazoEstimado, quote.ConteudoHash, quote.ValidoAte });
        });
        publicApi.MapPost("/{token}/aprovar", (string token, DecisaoPublicaRequest request, HttpContext context, AppDbContext db) => Decide(token, request, true, context, db));
        publicApi.MapPost("/{token}/recusar", (string token, DecisaoPublicaRequest request, HttpContext context, AppDbContext db) => Decide(token, request, false, context, db));
    }

    private static async Task<IResult> Decide(string token, DecisaoPublicaRequest request, bool approved, HttpContext context, AppDbContext db)
    {
        if (!request.ConfirmouLeitura) return Results.BadRequest("Confirme a leitura do orçamento.");
        var solicitation = await FindValid(token, db);
        if (solicitation is null) return Results.NotFound();
        var quote = await db.Orcamentos.FirstAsync(x => x.Id == solicitation.OrcamentoVersaoId);
        var order = await db.OrdensServico.FirstAsync(x => x.Id == quote.OrdemServicoId);
        if (order.Status != StatusOs.AguardandoAprovacao || quote.ValidoAte <= DateTime.UtcNow || !await IsLatest(quote, db)) return Results.NotFound();
        solicitation.UtilizadoEm = DateTime.UtcNow;
        order.Status = approved ? StatusOs.Aprovado : StatusOs.OrcamentoRecusado;
        order.UpdatedAt = DateTime.UtcNow;
        db.Autorizacoes.Add(new Autorizacao { EmpresaId = solicitation.EmpresaId, OrdemServicoId = order.Id, OrcamentoVersaoId = quote.Id,
            Canal = "Link", Aprovado = approved, Ip = context.Connection.RemoteIpAddress?.ToString(), UserAgent = context.Request.Headers.UserAgent.ToString() });
        db.OsHistoricos.Add(new OsHistorico { EmpresaId = solicitation.EmpresaId, OrdemServicoId = order.Id, UsuarioId = Guid.Empty,
            Evento = approved ? "Orçamento autorizado por link" : "Orçamento recusado por link", Detalhes = $"Versão: {quote.NumeroVersao}" });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException) { return Results.Conflict("Esta autorização já foi utilizada."); }
        return Results.Ok(new { resultado = approved ? "aprovado" : "recusado" });
    }

    private static async Task<SolicitacaoAutorizacao?> FindValid(string token, AppDbContext db)
    {
        if (token.Length is < 40 or > 50) return null;
        var hash = PublicToken.Hash(token);
        return await db.SolicitacoesAutorizacao.FirstOrDefaultAsync(x => x.TokenHash == hash && x.UtilizadoEm == null && x.ExpiraEm > DateTime.UtcNow);
    }

    private static async Task<bool> IsLatest(OrcamentoVersao quote, AppDbContext db) =>
        quote.NumeroVersao == await db.Orcamentos.Where(x => x.OrdemServicoId == quote.OrdemServicoId).MaxAsync(x => x.NumeroVersao);

}
