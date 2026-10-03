using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace PortCell.Api;

public class Empresa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Nome { get; set; }
}

public class Unidade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
}

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public required string Email { get; set; }
    public required string SenhaHash { get; set; }
    public string Perfil { get; set; } = Perfis.Atendente;
    public bool Ativo { get; set; } = true;
    public int TokenVersion { get; set; }
}

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? RevogadoEm { get; set; }
    public uint Version { get; set; }
}

public static class Perfis
{
    public const string Administrador = "Administrador";
    public const string Gerente = "Gerente";
    public const string Atendente = "Atendente";
    public const string Tecnico = "Tecnico";
}

public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public required string Telefone { get; set; }
    public string? Email { get; set; }
    public string? Documento { get; set; }
    public string? Endereco { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Aparelho
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid ClienteId { get; set; }
    public required string Marca { get; set; }
    public required string Modelo { get; set; }
    public string? Imei { get; set; }
    public string Tipo { get; set; } = "Celular";
    public string? Cor { get; set; }
    public string? NumeroSerie { get; set; }
    public string? Capacidade { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrdemServico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Numero { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid UnidadeId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid AparelhoId { get; set; }
    public Guid AtendenteId { get; set; }
    public Guid? TecnicoId { get; set; }
    public Guid? OrdemOrigemGarantiaId { get; set; }
    public bool AcessoNecessario { get; set; }
    public bool AtendimentoDireto { get; set; }
    [JsonIgnore]
    public string? AcessoCriptografado { get; set; }
    public string Status { get; set; } = StatusOs.Recebido;
    public string Prioridade { get; set; } = "Normal";
    public required string DefeitoRelatado { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint Version { get; set; }
}

public static class StatusOs
{
    public const string Recebido = "Recebido";
    public const string AguardandoDiagnostico = "AguardandoDiagnostico";
    public const string EmDiagnostico = "EmDiagnostico";
    public const string AguardandoOrcamento = "AguardandoOrcamento";
    public const string AguardandoAprovacao = "AguardandoAprovacao";
    public const string Aprovado = "Aprovado";
    public const string AguardandoPeca = "AguardandoPeca";
    public const string EmReparo = "EmReparo";
    public const string EmTestes = "EmTestes";
    public const string ProntoParaRetirada = "ProntoParaRetirada";
    public const string Entregue = "Entregue";
    public const string OrcamentoRecusado = "OrcamentoRecusado";
    public const string SemReparo = "SemReparo";
    public const string Irreparavel = "Irreparavel";
    public const string Cancelado = "Cancelado";
    public static bool Encerrado(string status) => status is Entregue or Cancelado or SemReparo or Irreparavel;
}

public class OsHistorico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid UsuarioId { get; set; }
    public required string Evento { get; set; }
    public string? Detalhes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AuditoriaAdministrativa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid UsuarioId { get; set; }
    public required string Evento { get; set; }
    public required string Entidade { get; set; }
    public Guid EntidadeId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChecklistItemTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;
    public string[] Opcoes { get; set; } = ["OK", "Avariado", "NaoTestado", "NaoAplicavel"];
    public bool PermiteObservacao { get; set; } = true;
}

public class TipoReparo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public bool RequerDesbloqueio { get; set; }
    public bool Ativo { get; set; } = true;
}

public class OsTipoReparo
{
    public Guid OrdemServicoId { get; set; }
    public Guid TipoReparoId { get; set; }
}

public class ChecklistResposta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid TemplateItemId { get; set; }
    public required string ItemNome { get; set; }
    public string[] ItemOpcoes { get; set; } = [];
    public bool PermiteObservacao { get; set; }
    public required string Resposta { get; set; }
    public string? Observacao { get; set; }
}

public class PontoAvaria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public required string Face { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public string? Componente { get; set; }
    public string? Defeito { get; set; }
    public string[] Defeitos { get; set; } = [];
    public string? Observacao { get; set; }
}

public class Diagnostico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid TecnicoId { get; set; }
    public required string Descricao { get; set; }
    public required string ResumoCliente { get; set; }
    public string? CausaProvavel { get; set; }
    public string? Recomendacao { get; set; }
    public string? ObservacoesInternas { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrcamentoVersao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public int NumeroVersao { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public string? Condicoes { get; set; }
    public string PrazoEstimado { get; set; } = "";
    public string ConteudoHash { get; set; } = "";
    public DateTime ValidoAte { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrcamentoItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrcamentoVersaoId { get; set; }
    public required string Tipo { get; set; }
    public required string Descricao { get; set; }
    public Guid? PecaId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
}

public class SolicitacaoAutorizacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrcamentoVersaoId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiraEm { get; set; }
    public DateTime? UtilizadoEm { get; set; }
    public uint Version { get; set; }
}

public class Autorizacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid OrcamentoVersaoId { get; set; }
    public required string Canal { get; set; }
    public bool Aprovado { get; set; }
    public Guid? UsuarioId { get; set; }
    public string? Evidencia { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Peca
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public required string Sku { get; set; }
    public required string Descricao { get; set; }
    public string? Compatibilidade { get; set; }
    public decimal Custo { get; set; }
    public decimal Preco { get; set; }
    public decimal Saldo { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public uint Version { get; set; }
}

public class ServicoCatalogo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public decimal PrecoPadrao { get; set; }
    public int GarantiaDias { get; set; }
    public required string CondicoesGarantia { get; set; }
    public bool Ativo { get; set; } = true;
}

public class Fornecedor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public string? Contato { get; set; }
    public bool Ativo { get; set; } = true;
}

public class MovimentoEstoque
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid PecaId { get; set; }
    public Guid? OrdemServicoId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid? FornecedorId { get; set; }
    public string? DocumentoCompra { get; set; }
    public required string Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public required string Justificativa { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Reparo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid TecnicoId { get; set; }
    public DateTime Inicio { get; set; } = DateTime.UtcNow;
    public DateTime? Fim { get; set; }
    public string? ServicosRealizados { get; set; }
    public string? Observacoes { get; set; }
}

public class TesteFinal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid TecnicoId { get; set; }
    public bool Aprovado { get; set; }
    public required string Resultado { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TesteItemTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public int Ordem { get; set; }
    public bool Ativo { get; set; } = true;
}

public class TesteResposta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid TesteFinalId { get; set; }
    public Guid TemplateItemId { get; set; }
    public required string ItemNome { get; set; }
    public required string Resultado { get; set; }
    public string? Observacao { get; set; }
}

public class Pagamento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid UsuarioId { get; set; }
    public decimal Valor { get; set; }
    public required string Forma { get; set; }
    public Guid? EstornoDeId { get; set; }
    public string? Justificativa { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Entrega
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public Guid UsuarioId { get; set; }
    public required string Recebedor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Garantia
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public required string Servico { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public required string Condicoes { get; set; }
}

public record LoginRequest(string Email, string Senha);
public record NovoUsuarioRequest(string Nome, string Email, string Senha, string Perfil);
public record RedefinirSenhaRequest(string NovaSenha);
public record ClienteRequest(string Nome, string Telefone, string? Email, string? Documento = null, string? Endereco = null, string? Observacoes = null);
public record AparelhoRequest(Guid ClienteId, string Marca, string Modelo, string? Imei, string Tipo = "Celular", string? Cor = null, string? NumeroSerie = null, string? Capacidade = null, string? Observacoes = null);
public record OrdemServicoRequest(Guid UnidadeId, Guid ClienteId, Guid AparelhoId, string DefeitoRelatado, string? Observacoes, string Prioridade, List<ChecklistRespostaRequest> Checklist, Guid? OrdemOrigemGarantiaId = null, List<Guid>? TiposReparo = null);
public record PontoAvariaRequest(string Face, decimal X, decimal Y, string? Componente = null, string? Defeito = null, string[]? Defeitos = null, string? Observacao = null);
public record AtendimentoInicialRequest(Guid UnidadeId, Guid ClienteId, Guid AparelhoId, string DefeitoRelatado, string? Observacoes, string Prioridade, List<PontoAvariaRequest> PontosAvaria, OrcamentoRequest Orcamento, List<Guid>? TiposReparo = null);
public record ChecklistRespostaRequest(Guid ItemId, string Resposta, string? Observacao);
public record StatusRequest(string Status, string? Justificativa);
public record ChecklistItemRequest(string Nome, int Ordem, bool Ativo, string[]? Opcoes = null, bool PermiteObservacao = true);
public record AtribuicaoTecnicoRequest(Guid TecnicoId);
public record TipoReparoRequest(string Nome, bool RequerDesbloqueio, bool Ativo);
public record AcessoDispositivoRequest(string Credencial);
public record DiagnosticoRequest(string Descricao, string ResumoCliente, string? CausaProvavel = null, string? Recomendacao = null, string? ObservacoesInternas = null);
public record OrcamentoItemRequest(string Tipo, string Descricao, decimal Quantidade, decimal ValorUnitario, Guid? PecaId = null);
public record OrcamentoRequest(List<OrcamentoItemRequest> Itens, decimal Desconto, DateTime ValidoAte, string? Condicoes, string PrazoEstimado);
public record DecisaoPublicaRequest(bool ConfirmouLeitura);
public record AutorizacaoManualRequest(Guid OrcamentoVersaoId, string Canal, bool Aprovado, string Evidencia);
public record PecaRequest(string Sku, string Descricao, decimal Custo, decimal Preco, decimal EstoqueMinimo, string? Compatibilidade = null);
public record ServicoCatalogoRequest(string Nome, decimal PrecoPadrao, int GarantiaDias, string CondicoesGarantia, bool Ativo);
public record FornecedorRequest(string Nome, string? Contato);
public record MovimentoRequest(decimal Quantidade, string Tipo, string Justificativa, Guid? FornecedorId = null, string? DocumentoCompra = null);
public record ConsumoPecaRequest(Guid PecaId, decimal Quantidade);
public record FinalizarReparoRequest(string ServicosRealizados, string? Observacoes = null);
public record TesteFinalRequest(bool Aprovado, string Resultado, List<TesteRespostaRequest>? Itens = null);
public record TesteRespostaRequest(Guid ItemId, string Resultado, string? Observacao);
public record TesteItemRequest(string Nome, int Ordem, bool Ativo);
public record PagamentoRequest(decimal Valor, string Forma);
public record EstornoRequest(string Justificativa);
public record EntregaRequest(string Recebedor, List<GarantiaRequest> Garantias);
public record GarantiaRequest(string Servico, int Dias, string Condicoes);

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Unidade> Unidades => Set<Unidade>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Aparelho> Aparelhos => Set<Aparelho>();
    public DbSet<OrdemServico> OrdensServico => Set<OrdemServico>();
    public DbSet<OsHistorico> OsHistoricos => Set<OsHistorico>();
    public DbSet<AuditoriaAdministrativa> AuditoriasAdministrativas => Set<AuditoriaAdministrativa>();
    public DbSet<ChecklistItemTemplate> ChecklistItens => Set<ChecklistItemTemplate>();
    public DbSet<TipoReparo> TiposReparo => Set<TipoReparo>();
    public DbSet<OsTipoReparo> OsTiposReparo => Set<OsTipoReparo>();
    public DbSet<ChecklistResposta> ChecklistRespostas => Set<ChecklistResposta>();
    public DbSet<PontoAvaria> PontosAvaria => Set<PontoAvaria>();
    public DbSet<Diagnostico> Diagnosticos => Set<Diagnostico>();
    public DbSet<OrcamentoVersao> Orcamentos => Set<OrcamentoVersao>();
    public DbSet<OrcamentoItem> OrcamentoItens => Set<OrcamentoItem>();
    public DbSet<SolicitacaoAutorizacao> SolicitacoesAutorizacao => Set<SolicitacaoAutorizacao>();
    public DbSet<Autorizacao> Autorizacoes => Set<Autorizacao>();
    public DbSet<Peca> Pecas => Set<Peca>();
    public DbSet<ServicoCatalogo> Servicos => Set<ServicoCatalogo>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<MovimentoEstoque> MovimentosEstoque => Set<MovimentoEstoque>();
    public DbSet<Reparo> Reparos => Set<Reparo>();
    public DbSet<TesteFinal> TestesFinais => Set<TesteFinal>();
    public DbSet<TesteItemTemplate> TesteItens => Set<TesteItemTemplate>();
    public DbSet<TesteResposta> TesteRespostas => Set<TesteResposta>();
    public DbSet<Pagamento> Pagamentos => Set<Pagamento>();
    public DbSet<Entrega> Entregas => Set<Entrega>();
    public DbSet<Garantia> Garantias => Set<Garantia>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Usuario>().HasIndex(x => x.Email).IsUnique();
        model.Entity<Usuario>().HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        model.Entity<RefreshToken>().Property(x => x.Version).IsRowVersion();
        model.Entity<RefreshToken>().HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Cliente>().HasIndex(x => new { x.EmpresaId, x.Telefone });
        model.Entity<Cliente>().HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Aparelho>().HasIndex(x => new { x.EmpresaId, x.Imei });
        model.Entity<Aparelho>().HasIndex(x => new { x.EmpresaId, x.NumeroSerie });
        model.Entity<Aparelho>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Aparelho>().HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Unidade>().HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrdemServico>().Property(x => x.Numero).UseIdentityByDefaultColumn();
        model.Entity<OrdemServico>().Property(x => x.Version).IsRowVersion();
        model.Entity<OrdemServico>().HasIndex(x => x.Numero).IsUnique();
        model.Entity<OrdemServico>().HasIndex(x => new { x.EmpresaId, x.Status, x.CreatedAt });
        model.Entity<OrdemServico>().HasOne<Empresa>().WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrdemServico>().HasOne<Unidade>().WithMany().HasForeignKey(x => x.UnidadeId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrdemServico>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrdemServico>().HasOne<Aparelho>().WithMany().HasForeignKey(x => x.AparelhoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrdemServico>().HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemOrigemGarantiaId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OsHistorico>().HasIndex(x => new { x.OrdemServicoId, x.CreatedAt });
        model.Entity<AuditoriaAdministrativa>().HasIndex(x => new { x.EmpresaId, x.CreatedAt });
        model.Entity<OsHistorico>().HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ChecklistResposta>().HasIndex(x => new { x.OrdemServicoId, x.TemplateItemId }).IsUnique();
        model.Entity<ChecklistResposta>().HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<PontoAvaria>().HasIndex(x => x.OrdemServicoId);
        model.Entity<PontoAvaria>().Property(x => x.X).HasPrecision(5, 4);
        model.Entity<PontoAvaria>().Property(x => x.Y).HasPrecision(5, 4);
        model.Entity<PontoAvaria>().Property(x => x.Componente).HasMaxLength(40);
        model.Entity<PontoAvaria>().Property(x => x.Defeito).HasMaxLength(60);
        model.Entity<PontoAvaria>().Property(x => x.Defeitos).HasColumnType("text[]");
        model.Entity<PontoAvaria>().Property(x => x.Observacao).HasMaxLength(500);
        model.Entity<PontoAvaria>().HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<ChecklistItemTemplate>().HasIndex(x => new { x.EmpresaId, x.Ordem });
        model.Entity<TipoReparo>().HasIndex(x => new { x.EmpresaId, x.Nome }).IsUnique();
        model.Entity<OsTipoReparo>().HasKey(x => new { x.OrdemServicoId, x.TipoReparoId });
        model.Entity<OsTipoReparo>().HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OsTipoReparo>().HasOne<TipoReparo>().WithMany().HasForeignKey(x => x.TipoReparoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Diagnostico>().HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrcamentoVersao>().HasIndex(x => new { x.OrdemServicoId, x.NumeroVersao }).IsUnique();
        model.Entity<OrcamentoVersao>().Property(x => x.Subtotal).HasPrecision(14, 2);
        model.Entity<OrcamentoVersao>().Property(x => x.Desconto).HasPrecision(14, 2);
        model.Entity<OrcamentoVersao>().Property(x => x.Total).HasPrecision(14, 2);
        model.Entity<OrcamentoVersao>().HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrcamentoItem>().Property(x => x.Quantidade).HasPrecision(14, 3);
        model.Entity<OrcamentoItem>().Property(x => x.ValorUnitario).HasPrecision(14, 2);
        model.Entity<OrcamentoItem>().HasOne<OrcamentoVersao>().WithMany().HasForeignKey(x => x.OrcamentoVersaoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<OrcamentoItem>().HasOne<Peca>().WithMany().HasForeignKey(x => x.PecaId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<SolicitacaoAutorizacao>().HasIndex(x => x.TokenHash).IsUnique();
        model.Entity<SolicitacaoAutorizacao>().Property(x => x.Version).IsRowVersion();
        model.Entity<SolicitacaoAutorizacao>().HasOne<OrcamentoVersao>().WithMany().HasForeignKey(x => x.OrcamentoVersaoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Autorizacao>().HasOne<OrcamentoVersao>().WithMany().HasForeignKey(x => x.OrcamentoVersaoId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Autorizacao>().HasIndex(x => new { x.OrdemServicoId, x.CreatedAt });
        model.Entity<Peca>().HasIndex(x => new { x.EmpresaId, x.Sku }).IsUnique();
        model.Entity<ServicoCatalogo>().HasIndex(x => new { x.EmpresaId, x.Nome }).IsUnique();
        model.Entity<ServicoCatalogo>().Property(x => x.PrecoPadrao).HasPrecision(14, 2);
        model.Entity<Fornecedor>().HasIndex(x => new { x.EmpresaId, x.Nome });
        model.Entity<Peca>().Property(x => x.Custo).HasPrecision(14, 2);
        model.Entity<Peca>().Property(x => x.Preco).HasPrecision(14, 2);
        model.Entity<Peca>().Property(x => x.Saldo).HasPrecision(14, 3);
        model.Entity<Peca>().Property(x => x.EstoqueMinimo).HasPrecision(14, 3);
        model.Entity<Peca>().Property(x => x.Version).IsRowVersion();
        model.Entity<MovimentoEstoque>().Property(x => x.Quantidade).HasPrecision(14, 3);
        model.Entity<MovimentoEstoque>().HasOne<Peca>().WithMany().HasForeignKey(x => x.PecaId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<MovimentoEstoque>().HasOne<Fornecedor>().WithMany().HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Pagamento>().Property(x => x.Valor).HasPrecision(14, 2);
        model.Entity<TesteItemTemplate>().HasIndex(x => new { x.EmpresaId, x.Ordem });
        model.Entity<TesteResposta>().HasIndex(x => new { x.TesteFinalId, x.TemplateItemId }).IsUnique();
        model.Entity<TesteResposta>().HasOne<TesteFinal>().WithMany().HasForeignKey(x => x.TesteFinalId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Pagamento>().HasIndex(x => x.EstornoDeId).IsUnique();
        model.Entity<Entrega>().HasIndex(x => x.OrdemServicoId).IsUnique();
        model.Entity<Garantia>().HasOne<OrdemServico>().WithMany().HasForeignKey(x => x.OrdemServicoId).OnDelete(DeleteBehavior.Restrict);
    }
}
