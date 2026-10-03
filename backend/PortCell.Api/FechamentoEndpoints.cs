using Microsoft.EntityFrameworkCore;

namespace PortCell.Api;

public static class FechamentoEndpoints
{
    public static void MapFechamentoEndpoints(this RouteGroupBuilder api)
    {
        var suppliers = api.MapGroup("/fornecedores");
        suppliers.MapGet("", async (HttpContext context, AppDbContext db) =>
            await db.Fornecedores.AsNoTracking().Where(x => x.EmpresaId == context.User.EmpresaId() && x.Ativo).OrderBy(x => x.Nome).ToListAsync());
        suppliers.MapPost("", async (FornecedorRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length > 160 || request.Contato?.Length > 200)
                return Results.BadRequest("Dados do fornecedor inválidos.");
            var supplier = new Fornecedor { EmpresaId = context.User.EmpresaId(), Nome = request.Nome.Trim(), Contato = request.Contato?.Trim() };
            db.Fornecedores.Add(supplier);
            Auditoria.Registrar(db, context, "FornecedorCriado", "Fornecedor", supplier.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/fornecedores/{supplier.Id}", supplier);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente));
        var pieces = api.MapGroup("/pecas");
        pieces.MapGet("", async (HttpContext context, AppDbContext db) =>
        {
            var query = db.Pecas.AsNoTracking().Where(x => x.EmpresaId == context.User.EmpresaId()).OrderBy(x => x.Descricao).Take(100);
            if (context.User.IsInRole(Perfis.Administrador) || context.User.IsInRole(Perfis.Gerente)) return Results.Ok((object)await query.ToListAsync());
            if (context.User.IsInRole(Perfis.Atendente)) return Results.Ok((object)await query.Select(x => new { x.Id, x.Sku, x.Descricao, x.Compatibilidade, x.Saldo, x.EstoqueMinimo, x.Preco }).ToListAsync());
            return Results.Ok((object)await query.Select(x => new { x.Id, x.Sku, x.Descricao, x.Compatibilidade, x.Saldo, x.EstoqueMinimo }).ToListAsync());
        });
        pieces.MapPost("", async (PecaRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Sku) || request.Sku.Length > 80 || string.IsNullOrWhiteSpace(request.Descricao) || request.Descricao.Length > 300 ||
                request.Custo < 0 || request.Preco < 0 || request.EstoqueMinimo < 0 || request.Compatibilidade?.Length > 500)
                return Results.BadRequest("Dados da peça inválidos.");
            var company = context.User.EmpresaId();
            if (await db.Pecas.AnyAsync(x => x.EmpresaId == company && x.Sku == request.Sku.Trim())) return Results.Conflict("SKU já cadastrado.");
            var piece = new Peca { EmpresaId = company, Sku = request.Sku.Trim(), Descricao = request.Descricao.Trim(), Custo = request.Custo, Preco = request.Preco, EstoqueMinimo = request.EstoqueMinimo, Compatibilidade = request.Compatibilidade?.Trim() };
            db.Pecas.Add(piece);
            Auditoria.Registrar(db, context, "PecaCriada", "Peca", piece.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/pecas/{piece.Id}", piece);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente));
        pieces.MapPost("/{id:guid}/movimentos", async (Guid id, MovimentoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (request.Tipo is not ("Entrada" or "Saida" or "Ajuste") || request.Quantidade == 0 || request.Quantidade < -100000 || request.Quantidade > 100000 ||
                string.IsNullOrWhiteSpace(request.Justificativa) || request.Justificativa.Length > 1000 || request.DocumentoCompra?.Length > 120)
                return Results.BadRequest("Movimento inválido ou sem justificativa.");
            if (request.FornecedorId.HasValue && !await db.Fornecedores.AnyAsync(x => x.Id == request.FornecedorId && x.EmpresaId == context.User.EmpresaId() && x.Ativo))
                return Results.BadRequest("Fornecedor inválido.");
            var piece = await db.Pecas.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (piece is null) return Results.NotFound();
            var delta = request.Tipo switch { "Entrada" => Math.Abs(request.Quantidade), "Saida" => -Math.Abs(request.Quantidade), _ => request.Quantidade };
            if (piece.Saldo + delta < 0) return Results.Conflict("Estoque insuficiente.");
            piece.Saldo += delta;
            db.MovimentosEstoque.Add(new MovimentoEstoque { EmpresaId = piece.EmpresaId, PecaId = id, UsuarioId = context.User.UsuarioId(), Tipo = request.Tipo,
                Quantidade = delta, Justificativa = request.Justificativa.Trim(), FornecedorId = request.FornecedorId, DocumentoCompra = request.DocumentoCompra?.Trim() });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("Estoque alterado por outro usuário."); }
            return Results.Ok(piece);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente));

        var orders = api.MapGroup("/ordens-servico");
        orders.MapPost("/{id:guid}/reparo/iniciar", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (order.Status != StatusOs.Aprovado) return Results.Conflict("A OS precisa de autorização vigente.");
            if (context.User.IsInRole(Perfis.Tecnico) && order.TecnicoId.HasValue && order.TecnicoId != context.User.UsuarioId()) return Results.Forbid();
            var latest = await db.Orcamentos.Where(x => x.OrdemServicoId == id).OrderByDescending(x => x.NumeroVersao).FirstOrDefaultAsync();
            if (latest is null || !await db.Autorizacoes.AnyAsync(x => x.OrdemServicoId == id && x.OrcamentoVersaoId == latest.Id && x.Aprovado))
                return Results.Conflict("Autorização da versão vigente não encontrada.");
            var user = context.User.UsuarioId();
            order.Status = StatusOs.EmReparo; order.TecnicoId ??= user; order.UpdatedAt = DateTime.UtcNow;
            db.Reparos.Add(new Reparo { EmpresaId = company, OrdemServicoId = id, TecnicoId = user });
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = user, Evento = "Reparo iniciado" });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("OS alterada por outro usuário."); }
            return Results.Ok(order);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Tecnico));
        orders.MapPost("/{id:guid}/pecas", async (Guid id, ConsumoPecaRequest request, HttpContext context, AppDbContext db) =>
        {
            if (request.Quantidade <= 0 || request.Quantidade > 100000) return Results.BadRequest("Quantidade inválida.");
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null || order.Status != StatusOs.EmReparo) return Results.Conflict("OS não está em reparo.");
            if (context.User.IsInRole(Perfis.Tecnico) && order.TecnicoId != context.User.UsuarioId()) return Results.Forbid();
            var piece = await db.Pecas.FirstOrDefaultAsync(x => x.Id == request.PecaId && x.EmpresaId == company);
            if (piece is null) return Results.NotFound();
            if (piece.Saldo < request.Quantidade) return Results.Conflict("Estoque insuficiente.");
            piece.Saldo -= request.Quantidade;
            order.UpdatedAt = DateTime.UtcNow;
            db.MovimentosEstoque.Add(new MovimentoEstoque { EmpresaId = company, PecaId = piece.Id, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Tipo = "Consumo", Quantidade = -request.Quantidade, Justificativa = "Consumo em OS" });
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Evento = "Peça consumida", Detalhes = $"SKU: {piece.Sku}; quantidade: {request.Quantidade}" });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("Estoque alterado por outro usuário."); }
            return Results.Ok(piece);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Tecnico));
        orders.MapPost("/{id:guid}/reparo/concluir", async (Guid id, FinalizarReparoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.ServicosRealizados) || request.ServicosRealizados.Length > 4000 || request.Observacoes?.Length > 4000) return Results.BadRequest("Descreva os serviços realizados e limite as observações a 4000 caracteres.");
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (order.Status != StatusOs.EmReparo) return Results.Conflict("OS não está em reparo.");
            if (context.User.IsInRole(Perfis.Tecnico) && order.TecnicoId != context.User.UsuarioId()) return Results.Forbid();
            var repair = await db.Reparos.Where(x => x.OrdemServicoId == id && x.Fim == null).OrderByDescending(x => x.Inicio).FirstOrDefaultAsync();
            if (repair is null) return Results.Conflict("Reparo em andamento não encontrado.");
            repair.Fim = DateTime.UtcNow; repair.ServicosRealizados = request.ServicosRealizados.Trim(); repair.Observacoes = request.Observacoes?.Trim();
            order.Status = StatusOs.EmTestes; order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Evento = "Reparo concluído" });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("OS alterada por outro usuário."); }
            return Results.Ok(order);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Tecnico));
        orders.MapPost("/{id:guid}/testes", async (Guid id, TesteFinalRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Resultado) || request.Resultado.Length > 4000) return Results.BadRequest("Resultado do teste é obrigatório.");
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (order.Status != StatusOs.EmTestes) return Results.Conflict("OS não está em testes.");
            if (context.User.IsInRole(Perfis.Tecnico) && order.TecnicoId != context.User.UsuarioId()) return Results.Forbid();
            var templates = await db.TesteItens.AsNoTracking().Where(x => x.EmpresaId == company && x.Ativo).ToListAsync();
            var answers = request.Itens ?? [];
            if (answers.Count != templates.Count || answers.Select(x => x.ItemId).Distinct().Count() != templates.Count ||
                templates.Any(item => answers.All(x => x.ItemId != item.Id)) ||
                answers.Any(x => x.Resultado is not ("OK" or "Falha" or "NaoAplicavel") || x.Observacao?.Length > 1000) ||
                (request.Aprovado && answers.Any(x => x.Resultado == "Falha")))
                return Results.BadRequest("Preencha todos os testes ativos; um teste com falha impede aprovação.");
            var user = context.User.UsuarioId();
            var test = new TesteFinal { EmpresaId = company, OrdemServicoId = id, TecnicoId = user, Aprovado = request.Aprovado, Resultado = request.Resultado.Trim() };
            db.TestesFinais.Add(test);
            foreach (var template in templates)
            {
                var answer = answers.Single(x => x.ItemId == template.Id);
                db.TesteRespostas.Add(new TesteResposta { EmpresaId = company, TesteFinalId = test.Id, TemplateItemId = template.Id,
                    ItemNome = template.Nome, Resultado = answer.Resultado, Observacao = answer.Observacao?.Trim() });
            }
            order.Status = request.Aprovado ? StatusOs.ProntoParaRetirada : StatusOs.EmReparo; order.UpdatedAt = DateTime.UtcNow;
            if (!request.Aprovado) db.Reparos.Add(new Reparo { EmpresaId = company, OrdemServicoId = id, TecnicoId = user });
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = user, Evento = request.Aprovado ? "Testes aprovados" : "Testes reprovados" });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("OS alterada por outro usuário."); }
            return Results.Ok(order);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Tecnico));

        orders.MapGet("/{id:guid}/pagamentos", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            if (!await db.OrdensServico.AnyAsync(x => x.Id == id && x.EmpresaId == company)) return Results.NotFound();
            var paid = await db.Pagamentos.AsNoTracking().Where(x => x.OrdemServicoId == id && x.EmpresaId == company).OrderBy(x => x.CreatedAt).ToListAsync();
            var quotedTotal = await db.Orcamentos.Where(x => x.OrdemServicoId == id && x.EmpresaId == company).OrderByDescending(x => x.NumeroVersao).Select(x => (decimal?)x.Total).FirstOrDefaultAsync();
            var total = quotedTotal ?? 0;
            var paidTotal = paid.Sum(x => x.Valor);
            var statusFinanceiro = quotedTotal is null ? "SemOrcamento" : paidTotal == 0 && total > 0 ? "Pendente" : paidTotal < total ? "Parcial" : "Quitado";
            return Results.Ok(new { pagamentos = paid, total, saldo = total - paidTotal, statusFinanceiro });
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
        orders.MapPost("/{id:guid}/pagamentos", async (Guid id, PagamentoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (request.Valor <= 0 || request.Valor > 100000000 || request.Forma is not ("Dinheiro" or "Pix" or "Cartao" or "Transferencia"))
                return Results.BadRequest("Valor ou forma de pagamento inválidos.");
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (order.Status is not (StatusOs.Aprovado or StatusOs.EmReparo or StatusOs.EmTestes or StatusOs.ProntoParaRetirada)) return Results.Conflict("OS não está apta a receber pagamento.");
            var quote = await db.Orcamentos.Where(x => x.OrdemServicoId == id).OrderByDescending(x => x.NumeroVersao).FirstAsync();
            var paid = await db.Pagamentos.Where(x => x.OrdemServicoId == id).SumAsync(x => (decimal?)x.Valor) ?? 0;
            if (paid + request.Valor > quote.Total) return Results.Conflict("Pagamento excede o saldo da OS.");
            var payment = new Pagamento { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Forma = request.Forma, Valor = request.Valor };
            db.Pagamentos.Add(payment);
            order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Evento = "Pagamento registrado", Detalhes = $"{request.Forma}: {request.Valor:N2}" });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("OS alterada por outro usuário."); }
            return Results.Created($"/api/ordens-servico/{id}/pagamentos", payment);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
        orders.MapPost("/{id:guid}/pagamentos/{paymentId:guid}/estorno", async (Guid id, Guid paymentId, EstornoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Justificativa) || request.Justificativa.Length > 1000)
                return Results.BadRequest("Justificativa obrigatória (até 1000 caracteres).");
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            var original = await db.Pagamentos.FirstOrDefaultAsync(x => x.Id == paymentId && x.OrdemServicoId == id && x.EmpresaId == company && x.Valor > 0);
            if (original is null) return Results.NotFound();
            if (await db.Pagamentos.AnyAsync(x => x.EstornoDeId == paymentId)) return Results.Conflict("Pagamento já estornado.");
            var refund = new Pagamento { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(),
                Forma = original.Forma, Valor = -original.Valor, EstornoDeId = original.Id, Justificativa = request.Justificativa.Trim() };
            db.Pagamentos.Add(refund);
            order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(),
                Evento = "Pagamento estornado", Detalhes = $"Pagamento: {paymentId}; motivo: {request.Justificativa.Trim()}" });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("OS alterada por outro usuário."); }
            catch (DbUpdateException) { return Results.Conflict("Pagamento já estornado ou operação inválida."); }
            return Results.Created($"/api/ordens-servico/{id}/pagamentos", refund);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente));
        orders.MapPost("/{id:guid}/entrega", async (Guid id, EntregaRequest request, HttpContext context, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Recebedor) || request.Recebedor.Length > 160 || request.Garantias is null ||
                request.Garantias.Any(x => string.IsNullOrWhiteSpace(x.Servico) || x.Servico.Length > 300 || x.Dias is < 1 or > 3650 || string.IsNullOrWhiteSpace(x.Condicoes) || x.Condicoes.Length > 2000))
                return Results.BadRequest("Recebedor ou garantias inválidos.");
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (order.Status != StatusOs.ProntoParaRetirada) return Results.Conflict("OS não está pronta para retirada.");
            var quote = await db.Orcamentos.Where(x => x.OrdemServicoId == id).OrderByDescending(x => x.NumeroVersao).FirstAsync();
            var paid = await db.Pagamentos.Where(x => x.OrdemServicoId == id).SumAsync(x => (decimal?)x.Valor) ?? 0;
            if (paid < quote.Total) return Results.Conflict("Há saldo pendente.");
            var serviceNames = await db.OrcamentoItens.Where(x => x.OrcamentoVersaoId == quote.Id && x.Tipo == "Servico").Select(x => x.Descricao).ToListAsync();
            var expected = serviceNames.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());
            var provided = request.Garantias.GroupBy(x => x.Servico).ToDictionary(x => x.Key, x => x.Count());
            if (expected.Count != provided.Count || expected.Any(x => !provided.TryGetValue(x.Key, out var count) || count != x.Value))
                return Results.BadRequest("Informe uma garantia para cada serviço aprovado.");
            var now = DateTime.UtcNow;
            var delivery = new Entrega { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Recebedor = request.Recebedor.Trim(), CreatedAt = now };
            db.Entregas.Add(delivery);
            foreach (var warranty in request.Garantias)
                db.Garantias.Add(new Garantia { EmpresaId = company, OrdemServicoId = id, Servico = warranty.Servico, Inicio = now, Fim = now.AddDays(warranty.Dias), Condicoes = warranty.Condicoes.Trim() });
            order.Status = StatusOs.Entregue; order.UpdatedAt = now;
            order.AcessoCriptografado = null;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Evento = "Aparelho entregue", Detalhes = $"Recebedor: {request.Recebedor.Trim()}" });
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { return Results.Conflict("OS alterada por outro usuário."); }
            return Results.Created($"/api/ordens-servico/{id}/entrega", delivery);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
    }
}
