using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace PortCell.Api;

public static class AuthTokens
{
    public static bool OriginAllowed(HttpContext context, IConfiguration config)
    {
        var origin = context.Request.Headers.Origin.ToString();
        return string.IsNullOrEmpty(origin) || FrontendOrigins.Allowed(config).Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    public static string Access(Usuario user, string key)
    {
        var claims = new[]
        {
            new Claim("usuario_id", user.Id.ToString()), new Claim("empresa_id", user.EmpresaId.ToString()),
            new Claim("token_version", user.TokenVersion.ToString()), new Claim(ClaimTypes.Name, user.Nome), new Claim(ClaimTypes.Role, user.Perfil)
        };
        var token = new JwtSecurityToken("PortCell", "PortCell.Web", claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static async Task IssueRefreshAsync(Usuario user, AppDbContext db, HttpContext context, bool development)
    {
        var raw = PublicToken.Generate();
        db.RefreshTokens.Add(new RefreshToken { UsuarioId = user.Id, TokenHash = PublicToken.Hash(raw), ExpiraEm = DateTime.UtcNow.AddDays(7) });
        await db.SaveChangesAsync();
        context.Response.Cookies.Append("portcell_refresh", raw, CookieOptions(development));
    }

    public static void ClearCookie(HttpContext context, bool development) => context.Response.Cookies.Delete("portcell_refresh", new CookieOptions
    { HttpOnly = true, Secure = !development, SameSite = SameSiteMode.Strict, Path = "/api/auth" });

    private static CookieOptions CookieOptions(bool development) => new()
    {
        HttpOnly = true, Secure = !development, SameSite = SameSiteMode.Strict,
        Path = "/api/auth", Expires = DateTimeOffset.UtcNow.AddDays(7)
    };
}

public static class UsuarioEndpoints
{
    public static void MapUsuarioEndpoints(this RouteGroupBuilder api)
    {
        var users = api.MapGroup("/usuarios").RequireAuthorization(policy => policy.RequireRole(Perfis.Administrador));
        users.MapGet("", async (HttpContext context, AppDbContext db) =>
            await db.Usuarios.AsNoTracking().Where(x => x.EmpresaId == context.User.EmpresaId())
                .OrderBy(x => x.Nome).Select(x => new { x.Id, x.Nome, x.Email, x.Perfil, x.Ativo }).ToListAsync());
        users.MapPost("", async (NovoUsuarioRequest request, HttpContext context, AppDbContext db, IPasswordHasher<Usuario> hasher) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Length > 160 ||
                string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 ||
                string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 12 ||
                request.Perfil is not (Perfis.Administrador or Perfis.Gerente or Perfis.Atendente or Perfis.Tecnico))
                return Results.BadRequest("Dados de usuário inválidos; senha deve ter pelo menos 12 caracteres.");
            var email = request.Email.Trim().ToLowerInvariant();
            if (await db.Usuarios.AnyAsync(x => x.Email == email)) return Results.Conflict("E-mail já cadastrado.");
            var user = new Usuario { EmpresaId = context.User.EmpresaId(), Nome = request.Nome.Trim(), Email = email, Perfil = request.Perfil, SenhaHash = "" };
            user.SenhaHash = hasher.HashPassword(user, request.Senha);
            db.Usuarios.Add(user);
            Auditoria.Registrar(db, context, "UsuarioCriado", "Usuario", user.Id);
            await db.SaveChangesAsync();
            return Results.Created($"/api/usuarios/{user.Id}", new { user.Id, user.Nome, user.Email, user.Perfil });
        });
        users.MapPatch("/{id:guid}/desativar", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            if (id == context.User.UsuarioId()) return Results.BadRequest("Não é permitido desativar o próprio usuário.");
            var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (user is null) return Results.NotFound();
            user.Ativo = false; user.TokenVersion++;
            Auditoria.Registrar(db, context, "UsuarioDesativado", "Usuario", user.Id);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        users.MapPatch("/{id:guid}/ativar", async (Guid id, HttpContext context, AppDbContext db) =>
        {
            var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (user is null) return Results.NotFound();
            user.Ativo = true;
            Auditoria.Registrar(db, context, "UsuarioAtivado", "Usuario", user.Id);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
        users.MapPost("/{id:guid}/redefinir-senha", async (Guid id, RedefinirSenhaRequest request, HttpContext context, AppDbContext db, IPasswordHasher<Usuario> hasher) =>
        {
            if (string.IsNullOrWhiteSpace(request.NovaSenha) || request.NovaSenha.Length < 12) return Results.BadRequest("Senha deve ter pelo menos 12 caracteres.");
            var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == context.User.EmpresaId());
            if (user is null) return Results.NotFound();
            await using var transaction = await db.Database.BeginTransactionAsync();
            user.SenhaHash = hasher.HashPassword(user, request.NovaSenha);
            user.TokenVersion++;
            Auditoria.Registrar(db, context, "SenhaRedefinida", "Usuario", user.Id);
            await db.RefreshTokens.Where(x => x.UsuarioId == id && x.RevogadoEm == null).ExecuteUpdateAsync(x => x.SetProperty(t => t.RevogadoEm, DateTime.UtcNow));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Results.NoContent();
        });
    }
}
