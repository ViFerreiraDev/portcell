using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace PortCell.Api;

public static class CadastroEndpoints
{
    public static Guid EmpresaId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("empresa_id")!);
    public static Guid UsuarioId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("usuario_id")!);

    public static void MapCadastroEndpoints(this RouteGroupBuilder api)
    {
        var clientes = api.MapGroup("/clientes");
        clientes.MapGet("", async (string? busca, int? pagina, int? tamanho, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var page = Math.Max(1, pagina ?? 1);
            var size = Math.Clamp(tamanho ?? 20, 1, 100);
            var query = db.Clientes.AsNoTracking().Where(x => x.EmpresaId == company && x.Ativo);
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var term = busca.Trim();
                query = query.Where(x => EF.Functions.ILike(x.Nome, $"%{term}%") || x.Telefone.Contains(term));
            }
            var total = await query.CountAsync();
            if (context.User.IsInRole(Perfis.Tecnico))
                return Results.Ok(new { itens = await query.OrderBy(x => x.Nome).Skip((page - 1) * size).Take(size)
                    .Select(x => new { x.Id, x.Nome, x.Telefone, x.Email }).ToListAsync(), total, pagina = page });
            return Results.Ok(new { itens = await query.OrderBy(x => x.Nome).Skip((page - 1) * size).Take(size).ToListAsync(), total, pagina = page });
        });
        clientes.MapGet("/{id:guid}", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var cliente = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (cliente is null) return Results.NotFound();
            if (context.User.IsInRole(Perfis.Tecnico)) return Results.Ok((object)new { cliente.Id, cliente.Nome, cliente.Telefone, cliente.Email });
            return Results.Ok((object)cliente);
        });
        clientes.MapPost("", async (ClienteRequest request, HttpContext context, AppDbContext db) =>
        {
            var error = ValidarCliente(request);
            if (error is not null) return Results.BadRequest(error);
            var cliente = new Cliente { EmpresaId = context.User.EmpresaId(), Nome = request.Nome.Trim(), Telefone = request.Telefone.Trim(),
                Email = NormalizarOpcional(request.Email), Documento = NormalizarOpcional(request.Documento), Endereco = NormalizarOpcional(request.Endereco), Observacoes = NormalizarOpcional(request.Observacoes) };
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync();
            return Results.Created($"/api/clientes/{cliente.Id}", cliente);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
        clientes.MapPut("/{id:guid}", async (Guid id, ClienteRequest request, HttpContext context, AppDbContext db) =>
        {
            var error = ValidarCliente(request);
            if (error is not null) return Results.BadRequest(error);
            var cliente = await db.Clientes.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId() && x.Ativo);
            if (cliente is null) return Results.NotFound();
            cliente.Nome = request.Nome.Trim(); cliente.Telefone = request.Telefone.Trim(); cliente.Email = NormalizarOpcional(request.Email);
            cliente.Documento = NormalizarOpcional(request.Documento); cliente.Endereco = NormalizarOpcional(request.Endereco); cliente.Observacoes = NormalizarOpcional(request.Observacoes);
            await db.SaveChangesAsync();
            return Results.Ok(cliente);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
        clientes.MapDelete("/{id:guid}", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var cliente = await db.Clientes.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId() && x.Ativo);
            if (cliente is null) return Results.NotFound();
            cliente.Ativo = false;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente));

        var aparelhos = api.MapGroup("/aparelhos");
        aparelhos.MapGet("", async (Guid? clienteId, string? busca, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var query = db.Aparelhos.AsNoTracking().Where(x => x.EmpresaId == company);
            if (clienteId.HasValue) query = query.Where(x => x.ClienteId == clienteId.Value);
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var term = busca.Trim();
                var number = long.TryParse(term, out var parsed) ? parsed : -1;
                query = query.Where(x => EF.Functions.ILike(x.Marca, $"%{term}%") || EF.Functions.ILike(x.Modelo, $"%{term}%") ||
                    (x.Imei != null && x.Imei.Contains(term)) || (x.NumeroSerie != null && x.NumeroSerie.Contains(term)) ||
                    db.Clientes.Any(c => c.Id == x.ClienteId && c.EmpresaId == company && c.Telefone.Contains(term)) ||
                    db.OrdensServico.Any(o => o.AparelhoId == x.Id && o.EmpresaId == company && o.Numero == number));
            }
            return Results.Ok(await query.OrderBy(x => x.Marca).ThenBy(x => x.Modelo).Take(100).ToListAsync());
        });
        aparelhos.MapGet("/{id:guid}", async (Guid id, HttpContext context, AppDbContext db) =>
            await db.Aparelhos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId()) is { } aparelho
                ? Results.Ok(aparelho) : Results.NotFound());
        aparelhos.MapPost("", async (AparelhoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (!ValidarAparelho(request))
                return Results.BadRequest("Marca e modelo são obrigatórios e devem respeitar os limites de tamanho.");
            var company = context.User.EmpresaId();
            if (!await db.Clientes.AnyAsync(x => x.Id == request.ClienteId && x.EmpresaId == company && x.Ativo))
                return Results.BadRequest("Cliente inválido.");
            var aparelho = new Aparelho { EmpresaId = company, ClienteId = request.ClienteId, Marca = request.Marca.Trim(), Modelo = request.Modelo.Trim(), Imei = NormalizarOpcional(request.Imei),
                Tipo = request.Tipo.Trim(), Cor = NormalizarOpcional(request.Cor), NumeroSerie = NormalizarOpcional(request.NumeroSerie), Capacidade = NormalizarOpcional(request.Capacidade), Observacoes = NormalizarOpcional(request.Observacoes) };
            db.Aparelhos.Add(aparelho);
            await db.SaveChangesAsync();
            return Results.Created($"/api/aparelhos/{aparelho.Id}", aparelho);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
        aparelhos.MapPut("/{id:guid}", async (Guid id, AparelhoRequest request, HttpContext context, AppDbContext db) =>
        {
            if (!ValidarAparelho(request))
                return Results.BadRequest("Marca, modelo ou IMEI inválidos.");
            var device = await db.Aparelhos.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (device is null) return Results.NotFound();
            if (device.ClienteId != request.ClienteId) return Results.Conflict("Não é permitido transferir o aparelho entre clientes neste fluxo.");
            device.Marca = request.Marca.Trim(); device.Modelo = request.Modelo.Trim(); device.Imei = NormalizarOpcional(request.Imei);
            device.Tipo = request.Tipo.Trim(); device.Cor = NormalizarOpcional(request.Cor); device.NumeroSerie = NormalizarOpcional(request.NumeroSerie);
            device.Capacidade = NormalizarOpcional(request.Capacidade); device.Observacoes = NormalizarOpcional(request.Observacoes);
            await db.SaveChangesAsync();
            return Results.Ok(device);
        }).RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Gerente, Perfis.Atendente));
    }

    private static string? ValidarCliente(ClienteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length > 160) return "Nome é obrigatório e deve ter até 160 caracteres.";
        if (string.IsNullOrWhiteSpace(request.Telefone) || request.Telefone.Length > 32) return "Telefone é obrigatório e deve ter até 32 caracteres.";
        if (request.Email?.Length > 254) return "E-mail deve ter até 254 caracteres.";
        if (request.Documento?.Length > 20 || request.Endereco?.Length > 500 || request.Observacoes?.Length > 2000) return "Documento, endereço ou observações excedem o limite.";
        return null;
    }

    private static bool ValidarAparelho(AparelhoRequest request) =>
        !string.IsNullOrWhiteSpace(request.Marca) && request.Marca.Length <= 80 &&
        !string.IsNullOrWhiteSpace(request.Modelo) && request.Modelo.Length <= 120 &&
        !string.IsNullOrWhiteSpace(request.Tipo) && request.Tipo.Length <= 60 &&
        (request.Imei is null || request.Imei.Length <= 32) && (request.Cor is null || request.Cor.Length <= 60) &&
        (request.NumeroSerie is null || request.NumeroSerie.Length <= 100) &&
        (request.Capacidade is null || request.Capacidade.Length <= 60) && (request.Observacoes is null || request.Observacoes.Length <= 2000);

    private static string? NormalizarOpcional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
