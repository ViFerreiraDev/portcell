using Microsoft.EntityFrameworkCore;

namespace PortCell.Api;

public static class OrdemServicoEndpoints
{
    public static void MapOrdemServicoEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/unidades", async (HttpContext context, AppDbContext db) =>
            await db.Unidades.AsNoTracking().Where(x => x.EmpresaId == context.User.EmpresaId()).OrderBy(x => x.Nome).ToListAsync());

        var checklist = api.MapGroup("/configuracoes/checklist");
        checklist.MapGet("", async (HttpContext context, AppDbContext db) =>
            await db.ChecklistItens.AsNoTracking().Where(x => x.EmpresaId == context.User.EmpresaId()).OrderBy(x => x.Ordem).ToListAsync());
        checklist.MapPost("", async (ChecklistItemRequest request, HttpContext context, AppDbContext db) =>
        {
            if (!ValidarItem(request)) return Results.BadRequest("Item ou opções inválidos.");
            var item = new ChecklistItemTemplate { EmpresaId = context.User.EmpresaId(), Nome = request.Nome.Trim(), Ordem = request.Ordem, Ativo = request.Ativo,
                Opcoes = request.Opcoes ?? ["OK", "Avariado", "NaoTestado", "NaoAplicavel"], PermiteObservacao = request.PermiteObservacao };
            db.ChecklistItens.Add(item);
            Auditoria.Registrar(db, context, "ChecklistItemCriado", "ChecklistItemTemplate", item.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/configuracoes/checklist/{item.Id}", item);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador));
        checklist.MapPut("/{id:guid}", async (Guid id, ChecklistItemRequest request, HttpContext context, AppDbContext db) =>
        {
            if (!ValidarItem(request)) return Results.BadRequest("Item ou opções inválidos.");
            var item = await db.ChecklistItens.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (item is null) return Results.NotFound();
            item.Nome = request.Nome.Trim(); item.Ordem = request.Ordem; item.Ativo = request.Ativo;
            item.Opcoes = request.Opcoes ?? item.Opcoes; item.PermiteObservacao = request.PermiteObservacao;
            Auditoria.Registrar(db, context, "ChecklistItemAlterado", "ChecklistItemTemplate", item.Id);
            await db.SaveChangesAsync();
            return Results.Ok(item);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador));

        var tests = api.MapGroup("/configuracoes/testes");
        tests.MapGet("", async (HttpContext context, AppDbContext db) =>
            await db.TesteItens.AsNoTracking().Where(x => x.EmpresaId == context.User.EmpresaId()).OrderBy(x => x.Ordem).ToListAsync());
        tests.MapPost("", async (TesteItemRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length > 120 || request.Ordem < 0) return Results.BadRequest("Item inválido.");
            var item = new TesteItemTemplate { EmpresaId = context.User.EmpresaId(), Nome = request.Nome.Trim(), Ordem = request.Ordem, Ativo = request.Ativo };
            db.TesteItens.Add(item);
            Auditoria.Registrar(db, context, "TesteItemCriado", "TesteItemTemplate", item.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/configuracoes/testes/{item.Id}", item);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador));
        tests.MapPut("/{id:guid}", async (Guid id, TesteItemRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length > 120 || request.Ordem < 0) return Results.BadRequest("Item inválido.");
            var item = await db.TesteItens.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (item is null) return Results.NotFound();
            item.Nome = request.Nome.Trim(); item.Ordem = request.Ordem; item.Ativo = request.Ativo;
            Auditoria.Registrar(db, context, "TesteItemAlterado", "TesteItemTemplate", item.Id);
            await db.SaveChangesAsync();
            return Results.Ok(item);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador));

        var repairTypes = api.MapGroup("/tipos-reparo");
        repairTypes.MapGet("", async (HttpContext context, AppDbContext db) =>
            await db.TiposReparo.AsNoTracking().Where(x => x.EmpresaId == context.User.EmpresaId()).OrderBy(x => x.Nome).ToListAsync());
        repairTypes.MapPost("", async (TipoReparoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length > 120) return Results.BadRequest("Nome inválido.");
            var company = context.User.EmpresaId();
            if (await db.TiposReparo.AnyAsync(x => x.EmpresaId == company && x.Nome == request.Nome.Trim())) return Results.Conflict("Tipo já cadastrado.");
            var type = new TipoReparo { EmpresaId = company, Nome = request.Nome.Trim(), RequerDesbloqueio = request.RequerDesbloqueio, Ativo = request.Ativo };
            db.TiposReparo.Add(type);
            Auditoria.Registrar(db, context, "TipoReparoCriado", "TipoReparo", type.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/tipos-reparo/{type.Id}", type);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador));
        repairTypes.MapPut("/{id:guid}", async (Guid id, TipoReparoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length > 120) return Results.BadRequest("Nome inválido.");
            var type = await db.TiposReparo.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (type is null) return Results.NotFound();
            type.Nome = request.Nome.Trim(); type.RequerDesbloqueio = request.RequerDesbloqueio; type.Ativo = request.Ativo;
            Auditoria.Registrar(db, context, "TipoReparoAlterado", "TipoReparo", type.Id);
            await db.SaveChangesAsync();
            return Results.Ok(type);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador));

        var orders = api.MapGroup("/ordens-servico");
        orders.MapGet("", async (string? status, Guid? clienteId, Guid? aparelhoId, Guid? tecnicoId, Guid? atendenteId, DateTime? inicio, DateTime? fim, int? pagina, int? tamanho, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var page = Math.Max(1, pagina ?? 1);
            var size = Math.Clamp(tamanho ?? 20, 1, 100);
            var query = db.OrdensServico.AsNoTracking().Where(x => x.EmpresaId == company);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
            if (clienteId.HasValue) query = query.Where(x => x.ClienteId == clienteId.Value);
            if (aparelhoId.HasValue) query = query.Where(x => x.AparelhoId == aparelhoId.Value);
            if (tecnicoId.HasValue) query = query.Where(x => x.TecnicoId == tecnicoId.Value);
            if (atendenteId.HasValue) query = query.Where(x => x.AtendenteId == atendenteId.Value);
            if (inicio.HasValue) query = query.Where(x => x.CreatedAt >= inicio.Value.ToUniversalTime());
            if (fim.HasValue) query = query.Where(x => x.CreatedAt <= fim.Value.ToUniversalTime());
            return Results.Ok(new { itens = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync(), total = await query.CountAsync(), pagina = page });
        });
        orders.MapGet("/{id:guid}", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            var timeline = await db.OsHistoricos.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company).OrderBy(x => x.CreatedAt).ToListAsync();
            var checklist = await db.ChecklistRespostas.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company).ToListAsync();
            var pontosAvaria = await db.PontosAvaria.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company).ToListAsync();
            var tests = await db.TestesFinais.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company).OrderBy(x => x.CreatedAt).ToListAsync();
            var repairs = await db.Reparos.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company).OrderBy(x => x.Inicio).ToListAsync();
            var testIds = tests.Select(x => x.Id).ToList();
            var testAnswers = await db.TesteRespostas.AsNoTracking().Where(x => x.EmpresaId == company && testIds.Contains(x.TesteFinalId)).ToListAsync();
            var reparoEmAndamento = await db.Reparos.AnyAsync(x => x.OrdemServicoId == id && x.EmpresaId == company && x.Fim == null);
            return Results.Ok(new { ordem = order, timeline, checklist, pontosAvaria, reparos = repairs, testes = tests, respostasTeste = testAnswers, reparoEmAndamento });
        });
        orders.MapPatch("/{id:guid}/tecnico", async (Guid id, AtribuicaoTecnicoRequest request, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (StatusOs.Encerrado(order.Status)) return Results.Conflict("OS encerrada.");
            if (!await db.Usuarios.AnyAsync(x => x.Id == request.TecnicoId && x.EmpresaId == company && x.Ativo && x.Perfil == Perfis.Tecnico))
                return Results.BadRequest("Técnico inválido.");
            var previous = order.TecnicoId;
            order.TecnicoId = request.TecnicoId;
            order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(),
                Evento = "Técnico atribuído", Detalhes = $"Anterior: {previous?.ToString() ?? "nenhum"}; novo: {request.TecnicoId}" });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("OS alterada por outro usuário."); }
            return Results.Ok(order);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente));
        orders.MapPost("", async (OrdemServicoRequest request, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var user = context.User.UsuarioId();
            if (string.IsNullOrWhiteSpace(request.DefeitoRelatado) || request.DefeitoRelatado.Length > 4000 || request.Observacoes?.Length > 4000)
                return Results.BadRequest("Defeito relatado é obrigatório e os textos devem ter até 4000 caracteres.");
            if (request.Prioridade is not ("Normal" or "Alta")) return Results.BadRequest("Prioridade inválida.");
            if (!await db.Unidades.AnyAsync(x => x.Id == request.UnidadeId && x.EmpresaId == company)) return Results.BadRequest("Unidade inválida.");
            if (!await db.Clientes.AnyAsync(x => x.Id == request.ClienteId && x.EmpresaId == company && x.Ativo)) return Results.BadRequest("Cliente inválido.");
            if (!await db.Aparelhos.AnyAsync(x => x.Id == request.AparelhoId && x.ClienteId == request.ClienteId && x.EmpresaId == company))
                return Results.BadRequest("Aparelho não pertence ao cliente.");
            if (request.OrdemOrigemGarantiaId is { } originalId)
            {
                if (!await db.OrdensServico.AnyAsync(x => x.Id == originalId && x.EmpresaId == company && x.ClienteId == request.ClienteId && x.AparelhoId == request.AparelhoId && x.Status == StatusOs.Entregue) ||
                    !await db.Garantias.AnyAsync(x => x.OrdemServicoId == originalId && x.EmpresaId == company && x.Fim >= DateTime.UtcNow))
                    return Results.BadRequest("OS de origem ou garantia vigente inválida.");
            }
            var typeIds = request.TiposReparo ?? [];
            if (typeIds.Count != typeIds.Distinct().Count()) return Results.BadRequest("Tipos de reparo duplicados.");
            var selectedTypes = await db.TiposReparo.Where(x => x.EmpresaId == company && x.Ativo && typeIds.Contains(x.Id)).ToListAsync();
            if (selectedTypes.Count != typeIds.Count) return Results.BadRequest("Tipo de reparo inválido ou inativo.");
            var activeItems = await db.ChecklistItens.AsNoTracking().Where(x => x.EmpresaId == company && x.Ativo).ToListAsync();
            var answers = request.Checklist ?? [];
            if (answers.Count != activeItems.Count || answers.Select(x => x.ItemId).Distinct().Count() != activeItems.Count ||
                activeItems.Any(x => answers.All(a => a.ItemId != x.Id)) ||
                activeItems.Any(item => answers.Any(x => x.ItemId == item.Id &&
                    (!item.Opcoes.Contains(x.Resposta) || x.Observacao?.Length > 1000 || (!item.PermiteObservacao && !string.IsNullOrWhiteSpace(x.Observacao))))))
                return Results.BadRequest("Preencha todos os itens ativos do checklist com resposta válida.");
            var order = new OrdemServico
            {
                EmpresaId = company, UnidadeId = request.UnidadeId, ClienteId = request.ClienteId,
                AparelhoId = request.AparelhoId, AtendenteId = user, DefeitoRelatado = request.DefeitoRelatado.Trim(),
                Observacoes = request.Observacoes?.Trim(), Prioridade = request.Prioridade, OrdemOrigemGarantiaId = request.OrdemOrigemGarantiaId,
                AcessoNecessario = selectedTypes.Any(x => x.RequerDesbloqueio)
            };
            db.OrdensServico.Add(order);
            foreach (var type in selectedTypes) db.OsTiposReparo.Add(new OsTipoReparo { OrdemServicoId = order.Id, TipoReparoId = type.Id });
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = order.Id, UsuarioId = user,
                Evento = "OS aberta", Detalhes = order.AcessoNecessario ? "Acesso ao dispositivo necessário" : "Acesso ao dispositivo não necessário para os serviços desta OS" });
            foreach (var item in activeItems)
            {
                var answer = answers.Single(x => x.ItemId == item.Id);
                db.ChecklistRespostas.Add(new ChecklistResposta
                {
                    EmpresaId = company, OrdemServicoId = order.Id, TemplateItemId = item.Id,
                    ItemNome = item.Nome, ItemOpcoes = item.Opcoes.ToArray(), PermiteObservacao = item.PermiteObservacao,
                    Resposta = answer.Resposta, Observacao = answer.Observacao?.Trim()
                });
            }
            await db.SaveChangesAsync();
            return Results.Created($"/api/ordens-servico/{order.Id}", order);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
        orders.MapPatch("/{id:guid}/status", async (Guid id, StatusRequest request, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (!StatusTransitions.PodeTransicionar(order.Status, request.Status)) return Results.BadRequest("Transição de status inválida.");
            if (request.Status is StatusOs.Cancelado or StatusOs.SemReparo or StatusOs.Irreparavel)
            {
                if (!context.User.IsInRole(Perfis.Administrador) && !context.User.IsInRole(Perfis.Gerente)) return Results.Forbid();
                if (string.IsNullOrWhiteSpace(request.Justificativa)) return Results.BadRequest("Justificativa obrigatória.");
            }
            if (request.Status == StatusOs.AguardandoPeca || order.Status == StatusOs.AguardandoPeca)
            {
                if (!context.User.IsInRole(Perfis.Administrador) && !context.User.IsInRole(Perfis.Gerente) && !context.User.IsInRole(Perfis.Tecnico))
                    return Results.Forbid();
                if (context.User.IsInRole(Perfis.Tecnico) && order.TecnicoId != context.User.UsuarioId()) return Results.Forbid();
                if (request.Status == StatusOs.AguardandoPeca && string.IsNullOrWhiteSpace(request.Justificativa))
                    return Results.BadRequest("Informe a peça pendente na justificativa.");
                var activeRepair = await db.Reparos.AnyAsync(x => x.OrdemServicoId == id && x.EmpresaId == company && x.Fim == null);
                if (order.Status == StatusOs.AguardandoPeca &&
                    (request.Status == StatusOs.EmReparo) != activeRepair && request.Status != StatusOs.Cancelado)
                    return Results.Conflict("Retomada incompatível com o reparo em andamento.");
                if (order.Status == StatusOs.AguardandoPeca && request.Status != StatusOs.Cancelado)
                {
                    var latest = await db.Orcamentos.Where(x => x.OrdemServicoId == id && x.EmpresaId == company)
                        .OrderByDescending(x => x.NumeroVersao).Select(x => (Guid?)x.Id).FirstOrDefaultAsync();
                    if (latest is null || !await db.Autorizacoes.AnyAsync(x => x.OrdemServicoId == id && x.OrcamentoVersaoId == latest && x.Aprovado))
                        return Results.Conflict("A versão vigente do orçamento precisa de autorização.");
                }
            }
            if (request.Status == StatusOs.Cancelado && (await db.Pagamentos.Where(x => x.OrdemServicoId == id).SumAsync(x => (decimal?)x.Valor) ?? 0) != 0)
                return Results.Conflict("Há pagamentos registrados. Estorne-os antes de cancelar a OS.");
            if (request.Justificativa?.Length > 1000) return Results.BadRequest("Justificativa deve ter até 1000 caracteres.");
            var previous = order.Status;
            order.Status = request.Status;
            if (StatusOs.Encerrado(request.Status)) order.AcessoCriptografado = null;
            order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico
            {
                EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(),
                Evento = $"Status: {previous} → {request.Status}", Detalhes = request.Justificativa?.Trim()
            });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("A OS foi alterada por outro usuário. Atualize os dados."); }
            return Results.Ok(order);
        });
    }

    private static bool ValidarItem(ChecklistItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length > 120 || request.Ordem < 0) return false;
        var options = request.Opcoes;
        return options is null || options.Length is >= 1 and <= 12 && options.All(x => !string.IsNullOrWhiteSpace(x) && x.Length <= 60) && options.Distinct(StringComparer.OrdinalIgnoreCase).Count() == options.Length;
    }
}

public static class StatusTransitions
{
    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        [StatusOs.Recebido] = [StatusOs.AguardandoDiagnostico, StatusOs.Cancelado],
        [StatusOs.AguardandoDiagnostico] = [StatusOs.EmDiagnostico, StatusOs.Cancelado],
        [StatusOs.EmDiagnostico] = [StatusOs.Irreparavel, StatusOs.Cancelado],
        [StatusOs.AguardandoOrcamento] = [StatusOs.SemReparo, StatusOs.Cancelado],
        [StatusOs.AguardandoAprovacao] = [StatusOs.Cancelado],
        [StatusOs.Aprovado] = [StatusOs.AguardandoPeca, StatusOs.Cancelado],
        [StatusOs.AguardandoPeca] = [StatusOs.Aprovado, StatusOs.EmReparo, StatusOs.Cancelado],
        [StatusOs.EmReparo] = [StatusOs.AguardandoPeca, StatusOs.Cancelado]
    };
    public static bool PodeTransicionar(string atual, string proximo) => Allowed.TryGetValue(atual, out var next) && next.Contains(proximo);
}
