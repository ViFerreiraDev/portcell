using Microsoft.EntityFrameworkCore;

namespace PortCell.Api;

public static class GestaoEndpoints
{
    public static void MapGestaoEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/dashboard", async (HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var open = await db.OrdensServico.AsNoTracking()
                .Where(x => x.EmpresaId == company && x.Status != StatusOs.Entregue && x.Status != StatusOs.Cancelado && x.Status != StatusOs.SemReparo && x.Status != StatusOs.Irreparavel)
                .GroupBy(x => x.Status).Select(x => new { Status = x.Key, Count = x.Count() }).ToListAsync();
            decimal? revenue = null;
            decimal? averageTicket = null;
            if (context.User.IsInRole(Perfis.Administrador) || context.User.IsInRole(Perfis.Gerente))
            {
                revenue = await db.Pagamentos.Where(x => x.EmpresaId == company).SumAsync(x => (decimal?)x.Valor) ?? 0;
                var delivered = await db.OrdensServico.CountAsync(x => x.EmpresaId == company && x.Status == StatusOs.Entregue);
                var deliveredRevenue = await db.Pagamentos.Where(x => x.EmpresaId == company &&
                    db.OrdensServico.Any(o => o.Id == x.OrdemServicoId && o.EmpresaId == company && o.Status == StatusOs.Entregue))
                    .SumAsync(x => (decimal?)x.Valor) ?? 0;
                averageTicket = delivered == 0 ? 0 : decimal.Round(deliveredRevenue / delivered, 2);
            }
            return Results.Ok(new { abertas = open.Sum(x => x.Count), porStatus = open, faturamento = revenue, ticketMedio = averageTicket });
        });
        api.MapGet("/auditoria", async (Guid? ordemServicoId, Guid? usuarioId, DateTime? inicio, DateTime? fim, int? pagina, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var query = db.OsHistoricos.AsNoTracking().Where(x => x.EmpresaId == company);
            if (ordemServicoId.HasValue) query = query.Where(x => x.OrdemServicoId == ordemServicoId.Value);
            if (usuarioId.HasValue) query = query.Where(x => x.UsuarioId == usuarioId.Value);
            if (inicio.HasValue) query = query.Where(x => x.CreatedAt >= inicio.Value.ToUniversalTime());
            if (fim.HasValue) query = query.Where(x => x.CreatedAt <= fim.Value.ToUniversalTime());
            var page = Math.Max(1, pagina ?? 1);
            return Results.Ok(new { itens = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * 50).Take(50).ToListAsync(), total = await query.CountAsync(), pagina = page });
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente));
        api.MapGet("/auditoria/administrativa", async (int? pagina, HttpContext context, AppDbContext db) =>
        {
            var page = Math.Max(1, pagina ?? 1);
            return await db.AuditoriasAdministrativas.AsNoTracking().Where(x => x.EmpresaId == context.User.EmpresaId())
                .OrderByDescending(x => x.CreatedAt).Skip((page - 1) * 50).Take(50).ToListAsync();
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente));

        api.MapGet("/relatorios/resumo", async (DateTime? inicio, DateTime? fim, HttpContext context, AppDbContext db) =>
        {
            var start = (inicio ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
            var end = (fim ?? DateTime.UtcNow).ToUniversalTime();
            if (start > end || end - start > TimeSpan.FromDays(366)) return Results.BadRequest("Informe um período válido de até 366 dias.");
            var company = context.User.EmpresaId();
            var orders = db.OrdensServico.AsNoTracking().Where(x => x.EmpresaId == company && x.CreatedAt >= start && x.CreatedAt <= end);
            var byStatus = await orders.GroupBy(x => x.Status).Select(x => new { status = x.Key, quantidade = x.Count() }).ToListAsync();
            var byTechnician = await orders.Where(x => x.TecnicoId != null).GroupBy(x => x.TecnicoId)
                .Select(x => new { tecnicoId = x.Key, quantidade = x.Count() }).ToListAsync();
            var technicianIds = byTechnician.Where(x => x.tecnicoId.HasValue).Select(x => x.tecnicoId!.Value).ToList();
            var technicians = await db.Usuarios.AsNoTracking().Where(x => x.EmpresaId == company && technicianIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.Nome);
            var technicianSummary = byTechnician.Select(x => new { x.tecnicoId,
                nome = x.tecnicoId.HasValue && technicians.TryGetValue(x.tecnicoId.Value, out var name) ? name : "Usuário indisponível", x.quantidade });
            var warrantyReturns = await orders.CountAsync(x => x.OrdemOrigemGarantiaId != null);
            var delivered = await orders.CountAsync(x => x.Status == StatusOs.Entregue);
            var deliveredRevenue = await db.Pagamentos.AsNoTracking().Where(x => x.EmpresaId == company &&
                orders.Any(o => o.Id == x.OrdemServicoId && o.Status == StatusOs.Entregue)).SumAsync(x => (decimal?)x.Valor) ?? 0;
            var payments = db.Pagamentos.AsNoTracking().Where(x => x.EmpresaId == company && x.CreatedAt >= start && x.CreatedAt <= end);
            var byMethod = await payments.GroupBy(x => x.Forma).Select(x => new { forma = x.Key, valor = x.Sum(y => y.Valor), movimentos = x.Count() }).ToListAsync();
            var revenue = await payments.SumAsync(x => (decimal?)x.Valor) ?? 0;
            var lowStock = await db.Pecas.AsNoTracking().Where(x => x.EmpresaId == company && x.Saldo <= x.EstoqueMinimo)
                .OrderBy(x => x.Saldo).Take(100).Select(x => new { x.Sku, x.Descricao, x.Saldo, x.EstoqueMinimo }).ToListAsync();
            var pending = await db.OrdensServico.AsNoTracking().Where(x => x.EmpresaId == company &&
                x.Status != StatusOs.Cancelado && x.Status != StatusOs.SemReparo && x.Status != StatusOs.Irreparavel)
                .Select(x => new
                {
                    x.Numero,
                    total = db.Orcamentos.Where(q => q.OrdemServicoId == x.Id).OrderByDescending(q => q.NumeroVersao)
                        .Select(q => (decimal?)q.Total).FirstOrDefault(),
                    pago = db.Pagamentos.Where(p => p.OrdemServicoId == x.Id).Sum(p => (decimal?)p.Valor) ?? 0
                })
                .Where(x => x.total != null && x.total > x.pago)
                .OrderByDescending(x => x.Numero).Take(100).ToListAsync();
            return Results.Ok(new { inicio = start, fim = end, porStatus = byStatus, porTecnico = technicianSummary,
                retornosGarantia = warrantyReturns, pagamentosPorForma = byMethod, faturamento = revenue,
                ticketMedio = delivered == 0 ? 0 : decimal.Round(deliveredRevenue / delivered, 2),
                estoqueMinimo = lowStock, saldosPendentes = pending });
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente));
    }
}
