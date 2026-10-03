using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace PortCell.Api;

public static class AcessoDispositivoEndpoints
{
    public static void MapAcessoDispositivoEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/ordens-servico/{id:guid}/acesso-dispositivo")
            .RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador, Perfis.Tecnico));
        group.MapPut("", async (Guid id, AcessoDispositivoRequest request, HttpContext context, AppDbContext db, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(request.Credencial) || request.Credencial.Length > 200) return Results.BadRequest("Credencial inválida.");
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (context.User.IsInRole(Perfis.Tecnico) && order.TecnicoId != context.User.UsuarioId()) return Results.Forbid();
            if (!order.AcessoNecessario || StatusOs.Encerrado(order.Status))
                return Results.Conflict("Esta OS não permite guardar acesso ao dispositivo.");
            order.AcessoCriptografado = DeviceSecret.Encrypt(request.Credencial, config["DeviceAccess:EncryptionKey"]);
            order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Evento = "Credencial do dispositivo registrada" });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        group.MapGet("", async (Guid id, HttpContext context, AppDbContext db, IConfiguration config) =>
        {
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (context.User.IsInRole(Perfis.Tecnico) && order.TecnicoId != context.User.UsuarioId()) return Results.Forbid();
            if (!order.AcessoNecessario || order.AcessoCriptografado is null || StatusOs.Encerrado(order.Status))
                return Results.NotFound();
            context.Response.Headers.CacheControl = "no-store";
            var credential = DeviceSecret.Decrypt(order.AcessoCriptografado, config["DeviceAccess:EncryptionKey"]);
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Evento = "Credencial do dispositivo consultada" });
            await db.SaveChangesAsync();
            return Results.Ok(new { credencial = credential });
        });
        group.MapDelete("", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var company = context.User.EmpresaId();
            var order = await db.OrdensServico.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == company);
            if (order is null) return Results.NotFound();
            if (context.User.IsInRole(Perfis.Tecnico) && order.TecnicoId != context.User.UsuarioId()) return Results.Forbid();
            order.AcessoCriptografado = null;
            order.UpdatedAt = DateTime.UtcNow;
            db.OsHistoricos.Add(new OsHistorico { EmpresaId = company, OrdemServicoId = id, UsuarioId = context.User.UsuarioId(), Evento = "Credencial do dispositivo removida" });
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}

public static class DeviceSecret
{
    public static string Encrypt(string plaintext, string? base64Key)
    {
        var key = Key(base64Key);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var value = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[value.Length];
        var tag = new byte[16];
        try
        {
            using (var aes = new AesGcm(key, tag.Length)) aes.Encrypt(nonce, value, cipher, tag);
            return Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(value); }
    }

    public static string Decrypt(string ciphertext, string? base64Key)
    {
        var key = Key(base64Key);
        var data = Convert.FromBase64String(ciphertext);
        if (data.Length < 28) throw new CryptographicException("Credencial cifrada inválida.");
        var plaintext = new byte[data.Length - 28];
        try
        {
            using (var aes = new AesGcm(key, 16)) aes.Decrypt(data.AsSpan(0, 12), data.AsSpan(28), data.AsSpan(12, 16), plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plaintext); }
    }

    private static byte[] Key(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key)) throw new InvalidOperationException("Configure DeviceAccess:EncryptionKey para registrar acesso ao aparelho.");
        var key = Convert.FromBase64String(base64Key);
        if (key.Length != 32) throw new InvalidOperationException("DeviceAccess:EncryptionKey deve conter 32 bytes em base64.");
        return key;
    }
}
