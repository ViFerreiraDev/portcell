using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PortCell.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
// ASP.NET Core scopes include RequestPath, which contains public approval tokens.
builder.Logging.AddJsonConsole(options => options.IncludeScopes = false);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
var connection = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Postgres.");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Configure Jwt:Key.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key precisa de pelo menos 32 bytes.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection));
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
{
    Directory.CreateDirectory(dataProtectionPath);
    var protection = builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
    if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
}
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy("web", policy => policy
    .WithOrigins(FrontendOrigins.Allowed(builder.Configuration))
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("public-authorization", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = "PortCell",
        ValidateAudience = true, ValidAudience = "PortCell.Web",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var id = context.Principal?.FindFirstValue("usuario_id");
            var company = context.Principal?.FindFirstValue("empresa_id");
            var version = context.Principal?.FindFirstValue("token_version");
            if (!Guid.TryParse(id, out var userId) || !Guid.TryParse(company, out var companyId) || !int.TryParse(version, out var tokenVersion))
            { context.Fail("Token inválido."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!await db.Usuarios.AnyAsync(x => x.Id == userId && x.EmpresaId == companyId && x.Ativo && x.TokenVersion == tokenVersion))
                context.Fail("Usuário inativo.");
        }
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
// Only the local frontend proxy is trusted to supply the original client address.
// ForwardLimit=1 uses the address appended by that proxy, ignoring client-supplied entries.
if (builder.Configuration.GetValue<bool>("LocalProxy:Enabled"))
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    });
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseRouting();
app.Use(async (context, next) =>
{
    var proposed = context.Request.Headers["X-Correlation-ID"].ToString();
    var correlationId = proposed.Length is > 0 and <= 64 && proposed.All(x => char.IsLetterOrDigit(x) || x == '-')
        ? proposed : Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    await next(context);
    var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
    app.Logger.LogInformation("HTTP {Method} {Route} {StatusCode} {CorrelationId}",
        context.Request.Method, route, context.Response.StatusCode, correlationId);
});
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();

if (args.Contains("--migrate") || args.Contains("--bootstrap-admin"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (args.Contains("--bootstrap-admin"))
    {
        var email = builder.Configuration["Admin:Email"]?.Trim().ToLowerInvariant();
        var password = builder.Configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("Configure Admin:Email e Admin:Password com pelo menos 12 caracteres.");
        if (!await db.Usuarios.AnyAsync())
        {
            var company = new Empresa { Nome = builder.Configuration["Empresa:Nome"] ?? "PortCell" };
            var unit = new Unidade { EmpresaId = company.Id, Nome = "Matriz" };
            var admin = new Usuario { EmpresaId = company.Id, Nome = "Administrador", Email = email, SenhaHash = "", Perfil = Perfis.Administrador };
            admin.SenhaHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>().HashPassword(admin, password);
            db.AddRange(company, unit, admin);
            foreach (var (item, order) in ChecklistDefaults.Itens.Select((item, index) => (item, index + 1)))
                db.ChecklistItens.Add(new ChecklistItemTemplate { EmpresaId = company.Id, Nome = item.Nome, Opcoes = item.Opcoes, Ordem = order });
            foreach (var (name, order) in ChecklistDefaults.Testes.Select((name, index) => (name, index + 1)))
                db.TesteItens.Add(new TesteItemTemplate { EmpresaId = company.Id, Nome = name, Ordem = order });
            await db.SaveChangesAsync();
        }
    }
    return;
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/api/auth/login", async (LoginRequest request, HttpContext context, AppDbContext db, IPasswordHasher<Usuario> hasher) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Senha)) return Results.Unauthorized();
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email && x.Ativo);
    if (user is null || hasher.VerifyHashedPassword(user, user.SenhaHash, request.Senha) == PasswordVerificationResult.Failed)
        return Results.Unauthorized();
    await AuthTokens.IssueRefreshAsync(user, db, context, app.Environment.IsDevelopment());
    return Results.Ok(new { token = AuthTokens.Access(user, jwtKey), nome = user.Nome, perfil = user.Perfil });
}).RequireRateLimiting("login");
app.MapPost("/api/auth/refresh", async (HttpContext context, AppDbContext db) =>
{
    if (!AuthTokens.OriginAllowed(context, builder.Configuration)) return Results.Forbid();
    if (!context.Request.Cookies.TryGetValue("portcell_refresh", out var raw) || string.IsNullOrWhiteSpace(raw)) return Results.Unauthorized();
    var token = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == PublicToken.Hash(raw) && x.RevogadoEm == null && x.ExpiraEm > DateTime.UtcNow);
    if (token is null) return Results.Unauthorized();
    var user = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == token.UsuarioId && x.Ativo);
    if (user is null) return Results.Unauthorized();
    token.RevogadoEm = DateTime.UtcNow;
    try { await db.SaveChangesAsync(); }
    catch (DbUpdateConcurrencyException) { return Results.Unauthorized(); }
    await AuthTokens.IssueRefreshAsync(user, db, context, app.Environment.IsDevelopment());
    return Results.Ok(new { token = AuthTokens.Access(user, jwtKey), nome = user.Nome, perfil = user.Perfil });
}).RequireRateLimiting("login");
app.MapPost("/api/auth/logout", async (HttpContext context, AppDbContext db) =>
{
    if (!AuthTokens.OriginAllowed(context, builder.Configuration)) return Results.Forbid();
    if (context.Request.Cookies.TryGetValue("portcell_refresh", out var raw))
    {
        var token = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == PublicToken.Hash(raw) && x.RevogadoEm == null);
        if (token is not null) { token.RevogadoEm = DateTime.UtcNow; await db.SaveChangesAsync(); }
    }
    AuthTokens.ClearCookie(context, app.Environment.IsDevelopment());
    return Results.NoContent();
});

var api = app.MapGroup("/api").RequireAuthorization();
api.MapCadastroEndpoints();
api.MapOrdemServicoEndpoints();
api.MapAtendimentoInicialEndpoints();
api.MapOrcamentoEndpoints();
api.MapFechamentoEndpoints();
api.MapAcessoDispositivoEndpoints();
api.MapGestaoEndpoints();
api.MapUsuarioEndpoints();
api.MapServicoCatalogoEndpoints();
app.MapAutorizacaoPublicaEndpoints();
app.Run();
