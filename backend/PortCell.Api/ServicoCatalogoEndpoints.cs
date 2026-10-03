using Microsoft.EntityFrameworkCore;

namespace PortCell.Api;

public static class ServicoCatalogoEndpoints
{
    public static void MapServicoCatalogoEndpoints(this RouteGroupBuilder api)
    {
        var services = api.MapGroup("/servicos");
        services.MapGet("", async (bool? todos, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var query = db.Servicos.AsNoTracking().Where(x => x.EmpresaId == company);
            if (todos != true || !context.User.IsInRole(Perfis.Administrador)) query = query.Where(x => x.Ativo);
            return await query.OrderBy(x => x.Nome).ToListAsync();
        });
        services.MapPost("", async (ServicoCatalogoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (!Valid(request)) return Results.BadRequest("Dados do serviço inválidos.");
            var company = context.User.EmpresaId();
            var name = request.Nome.Trim();
            if (await db.Servicos.AnyAsync(x => x.EmpresaId == company && x.Nome == name)) return Results.Conflict("Serviço já cadastrado.");
            var service = new ServicoCatalogo { EmpresaId = company, Nome = name, PrecoPadrao = request.PrecoPadrao,
                GarantiaDias = request.GarantiaDias, CondicoesGarantia = request.CondicoesGarantia.Trim(), Ativo = request.Ativo };
            db.Servicos.Add(service);
            Auditoria.Registrar(db, context, "ServicoCriado", "ServicoCatalogo", service.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/servicos/{service.Id}", service);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador));
        services.MapPut("/{id:guid}", async (Guid id, ServicoCatalogoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (!Valid(request)) return Results.BadRequest("Dados do serviço inválidos.");
            var company = context.User.EmpresaId();
            var service = await db.Servicos.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (service is null) return Results.NotFound();
            var name = request.Nome.Trim();
            if (await db.Servicos.AnyAsync(x => x.EmpresaId == company && x.Id != id && x.Nome == name)) return Results.Conflict("Serviço já cadastrado.");
            service.Nome = name; service.PrecoPadrao = request.PrecoPadrao; service.GarantiaDias = request.GarantiaDias;
            service.CondicoesGarantia = request.CondicoesGarantia.Trim(); service.Ativo = request.Ativo;
            Auditoria.Registrar(db, context, "ServicoAlterado", "ServicoCatalogo", service.Id);
            await db.SaveChangesAsync();
            return Results.Ok(service);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador));
    }

    private static bool Valid(ServicoCatalogoRequest request) =>
        !string.IsNullOrWhiteSpace(request.Nome) && request.Nome.Length <= 300 &&
        request.PrecoPadrao is >= 0 and <= 1000000 && request.GarantiaDias is >= 1 and <= 3650 &&
        !string.IsNullOrWhiteSpace(request.CondicoesGarantia) && request.CondicoesGarantia.Length <= 2000;
}
