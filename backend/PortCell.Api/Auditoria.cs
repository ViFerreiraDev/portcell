namespace PortCell.Api;

public static class Auditoria
{
    public static void Registrar(AppDbContext db, HttpContext context, string evento, string entidade, Guid entidadeId) =>
        db.AuditoriasAdministrativas.Add(new AuditoriaAdministrativa
        {
            EmpresaId = context.User.EmpresaId(), UsuarioId = context.User.UsuarioId(),
            Evento = evento, Entidade = entidade, EntidadeId = entidadeId
        });
}
