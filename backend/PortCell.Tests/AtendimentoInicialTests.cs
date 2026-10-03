using System.Text;
using System.Text.RegularExpressions;
using PortCell.Api;

namespace PortCell.Tests;

public class AtendimentoInicialTests
{
    [Fact]
    public void ChecklistVisualExigePeloMenosUmPontoEValidaFaces()
    {
        Assert.False(AtendimentoRules.PontosValidos([]));
        Assert.True(AtendimentoRules.PontosValidos([new("frente", .4m, .6m)]));
        Assert.False(AtendimentoRules.PontosValidos([new("inexistente", .4m, .6m)]));
        Assert.False(AtendimentoRules.PontosValidos([new("frente", 1.1m, .6m)]));
        Assert.False(AtendimentoRules.PontosValidos([new("frente", .4m, .6m), new("frente", .4m, .6m)]));
    }

    [Fact]
    public void ComponenteExigeFaceCorretaDefeitoEDeduplicacao()
    {
        var screen = new PontoAvariaRequest("frente", .5m, .53m, "tela", "Trincada / quebrada");
        Assert.True(AtendimentoRules.PontosValidos([screen]));
        Assert.False(AtendimentoRules.PontosValidos([screen with { Componente = "desconhecido" }]));
        Assert.False(AtendimentoRules.PontosValidos([screen with { Face = "tras" }]));
        Assert.False(AtendimentoRules.PontosValidos([screen with { Defeito = " " }]));
        Assert.False(AtendimentoRules.PontosValidos([screen with { Defeito = new string('a', 61) }]));
        Assert.False(AtendimentoRules.PontosValidos([screen, screen with { X = .2m }]));
        Assert.True(AtendimentoRules.PontosValidos([screen with { Componente = null }]));
        Assert.True(AtendimentoRules.PontosValidos([
            new("tras", .5m, .48m, "bateria", "Estufada"),
            new("tras", .5m, .72m, "traseira", "Trincada / quebrada")
        ]));
    }

    [Fact]
    public void InspecaoAceitaMultiplosDefeitosEObservacoesEmTodasAsFaces()
    {
        var point = new PontoAvariaRequest("tras", .5m, .72m, "traseira", Defeitos: ["Riscada", "Trincada / quebrada"], Observacao: "Riscos profundos no canto inferior.");
        Assert.True(AtendimentoRules.PontosValidos([point]));
        Assert.False(AtendimentoRules.PontosValidos([point with { Defeitos = ["Riscada", " RISCADA "] }]));
        Assert.False(AtendimentoRules.PontosValidos([point with { Observacao = new string('x', 501) }]));
        Assert.False(AtendimentoRules.PontosValidos([point with { Defeitos = [null!] }]));
        Assert.False(AtendimentoRules.PontosValidos([point with { Defeitos = Enumerable.Range(1, 9).Select(x => $"Problema {x}").ToArray() }]));
        foreach (var face in AtendimentoRules.Faces)
        {
            Assert.True(AtendimentoRules.PontosValidos([new(face, .15m, .3m, Defeitos: ["Riscado", "Amassado"], Observacao: "Canto superior") ]));
            Assert.True(AtendimentoRules.PontosValidos([new(face, .15m, .3m, Defeitos: [], Observacao: "Dano no acabamento") ]));
            Assert.False(AtendimentoRules.PontosValidos([new(face, .15m, .3m, Defeitos: [], Observacao: " ") ]));
        }
    }

    [Fact]
    public void OrcamentoInicialExigePecaVinculadaEDezItensNoMaximo()
    {
        var quote = new OrcamentoRequest([new("Peca", "Tela", 1, 150m)], 0, DateTime.UtcNow.AddDays(2), null, "2 dias úteis");
        Assert.False(AtendimentoRules.OrcamentoInicialValido(quote));
        Assert.True(AtendimentoRules.OrcamentoInicialValido(quote with { Itens = [quote.Itens[0] with { PecaId = Guid.NewGuid() }] }));
        Assert.False(AtendimentoRules.OrcamentoInicialValido(quote with { Itens = Enumerable.Repeat(quote.Itens[0], 11).ToList() }));
    }

    [Fact]
    public void RelatorioTemUmaPaginaETodasAsFaces()
    {
        var order = new OrdemServico { Numero = 42, DefeitoRelatado = "Tela quebrada e conector intermitente", Status = StatusOs.AguardandoAprovacao };
        var quote = new OrcamentoVersao { NumeroVersao = 1, Subtotal = 385m, Desconto = 5m, Total = 380m,
            PrazoEstimado = "2 dias úteis", ValidoAte = DateTime.UtcNow.AddDays(7) };
        var items = new List<OrcamentoItem>
        {
            new() { Tipo = "Servico", Descricao = "Troca de tela", Quantidade = 1, ValorUnitario = 235m },
            new() { Tipo = "Peca", Descricao = "Tela OLED compatível", Quantidade = 1, ValorUnitario = 150m }
        };
        var points = AtendimentoRules.Componentes.Select(pair => new PontoAvaria
        {
            Face = pair.Value.Face, X = pair.Value.X, Y = pair.Value.Y, Componente = pair.Key,
            Defeito = pair.Key == "tela" ? "Trincada / quebrada" : "Falha intermitente"
        }).ToList();
        var pdf = OsReportPdf.Build(new OsReportData(new Empresa { Nome = "Assistência PortCell" }, order,
            new Cliente { Nome = "Cliente de Exemplo", Telefone = "(11) 99999-9999", Email = "cliente@exemplo.com" },
            new Aparelho { Marca = "Fabricante", Modelo = "Modelo X", Imei = "123456789012345" }, quote, items, points));
        Assert.StartsWith("%PDF-1.4", Encoding.Latin1.GetString(pdf));
        Assert.Contains("/Count 1", Encoding.Latin1.GetString(pdf));
        Assert.Contains("Ponto entregue com defeito", Encoding.Latin1.GetString(pdf));
        Assert.Contains("Câmera frontal: Falha intermitente", Encoding.Latin1.GetString(pdf));
        Assert.Contains("Tela: Trincada / quebrada", Encoding.Latin1.GetString(pdf));
        Assert.Contains("Tampa traseira: Falha intermitente", Encoding.Latin1.GetString(pdf));
        Assert.Contains("Bateria \\(interna\\): Falha intermitente", Encoding.Latin1.GetString(pdf));
        var content = Encoding.Latin1.GetString(pdf);
        var colors = Regex.Matches(content, @"([\d.]+) ([\d.]+) ([\d.]+) [rR][gG]");
        Assert.NotEmpty(colors);
        Assert.All(colors.Cast<Match>(), color =>
        {
            Assert.Equal(color.Groups[1].Value, color.Groups[2].Value);
            Assert.Equal(color.Groups[2].Value, color.Groups[3].Value);
        });
        Assert.Contains("O atraso não autoriza descarte, venda ou incorporação do aparelho.", content);
        Assert.Contains("Guarda: sem cobrança nesta OS.", content);
        var sample = Environment.GetEnvironmentVariable("PORTCELL_PDF_SAMPLE");
        if (!string.IsNullOrWhiteSpace(sample)) File.WriteAllBytes(sample, pdf);
    }

    [Fact]
    public void RelatorioIncluiTodosOsLocaisLivresAteOLimite()
    {
        var points = Enumerable.Range(1, 24).Select(index => new PontoAvaria
        {
            Face = AtendimentoRules.Faces[(index - 1) % 6], X = index / 25m, Y = .4m,
            Defeitos = ["Riscado", "Amassado"], Observacao = $"Registro {index}"
        }).ToList();
        var items = Enumerable.Range(1, 10).Select(index => new OrcamentoItem
        {
            Tipo = "Servico", Descricao = $"Serviço {index}: teste do relatório com todos os itens preenchidos", Quantidade = 1, ValorUnitario = 100
        }).ToList();
        var pdf = OsReportPdf.Build(new(new Empresa { Nome = "Teste" },
            new OrdemServico { Numero = 1, DefeitoRelatado = "Múltiplas avarias" }, new Cliente { Nome = "Teste", Telefone = "11999999999" },
            new Aparelho { Marca = "Teste", Modelo = "Teste" }, new OrcamentoVersao { PrazoEstimado = "2 dias", Subtotal = 1000, Total = 1000 }, items, points));
        var text = Encoding.Latin1.GetString(pdf);
        Assert.Contains("/Count 1", text);
        Assert.Contains("24. Face inferior: Riscado; Amassado. Obs.: Registro 24", text);
        var sample = Environment.GetEnvironmentVariable("PORTCELL_PDF_STRESS_SAMPLE");
        if (!string.IsNullOrWhiteSpace(sample)) File.WriteAllBytes(sample, pdf);
    }
}
