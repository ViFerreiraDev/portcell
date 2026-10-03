using PortCell.Api;
using System.Security.Cryptography;

namespace PortCell.Tests;

public class RegrasCriticasTests
{
    [Fact]
    public void StatusNaoPermitePularAprovacaoOuEntrega()
    {
        Assert.True(StatusTransitions.PodeTransicionar(StatusOs.Recebido, StatusOs.AguardandoDiagnostico));
        Assert.False(StatusTransitions.PodeTransicionar(StatusOs.AguardandoAprovacao, StatusOs.Aprovado));
        Assert.False(StatusTransitions.PodeTransicionar(StatusOs.ProntoParaRetirada, StatusOs.Entregue));
    }

    [Fact]
    public void AguardandoPecaPermitePausarERetomarSomenteEtapasAutorizadas()
    {
        Assert.True(StatusTransitions.PodeTransicionar(StatusOs.Aprovado, StatusOs.AguardandoPeca));
        Assert.True(StatusTransitions.PodeTransicionar(StatusOs.EmReparo, StatusOs.AguardandoPeca));
        Assert.True(StatusTransitions.PodeTransicionar(StatusOs.AguardandoPeca, StatusOs.EmReparo));
        Assert.False(StatusTransitions.PodeTransicionar(StatusOs.AguardandoPeca, StatusOs.ProntoParaRetirada));
    }

    [Fact]
    public void OrcamentoCalculaTotalComDesconto()
    {
        var request = new OrcamentoRequest(
            [new OrcamentoItemRequest("Servico", "Troca de conector", 2, 120m), new OrcamentoItemRequest("Peca", "Conector", 1, 35m)],
            15m, DateTime.UtcNow.AddDays(2), null, "2 dias úteis");
        Assert.True(OrcamentoRules.TryCalculate(request, out var subtotal, out var total));
        Assert.Equal(275m, subtotal);
        Assert.Equal(260m, total);
    }

    [Fact]
    public void OrcamentoRejeitaDescontoMaiorQueSubtotal()
    {
        var request = new OrcamentoRequest([new OrcamentoItemRequest("Servico", "Diagnóstico", 1, 50m)], 60m, DateTime.UtcNow.AddDays(1), null, "1 dia útil");
        Assert.False(OrcamentoRules.TryCalculate(request, out _, out _));
    }

    [Fact]
    public void HashDoOrcamentoMudaQuandoPrazoOuCondicaoMuda()
    {
        var original = new OrcamentoRequest([new OrcamentoItemRequest("Servico", "Troca de tela", 1, 100m)],
            0m, DateTime.UtcNow.AddDays(1), "Garantia padrão", "2 dias úteis");
        var prazoAlterado = original with { PrazoEstimado = "3 dias úteis" };
        var condicaoAlterada = original with { Condicoes = "Garantia estendida" };
        var hash = OrcamentoRules.ContentHash(original, 100m, 100m);
        Assert.Equal(64, hash.Length);
        Assert.NotEqual(hash, OrcamentoRules.ContentHash(prazoAlterado, 100m, 100m));
        Assert.NotEqual(hash, OrcamentoRules.ContentHash(condicaoAlterada, 100m, 100m));
    }

    [Fact]
    public void TokenPublicoNaoRepeteENaoRevelaValorArmazenado()
    {
        var first = PublicToken.Generate();
        var second = PublicToken.Generate();
        Assert.Equal(43, first.Length);
        Assert.NotEqual(first, second);
        Assert.NotEqual(first, PublicToken.Hash(first));
        Assert.Equal(PublicToken.Hash(first), PublicToken.Hash(first));
    }

    [Fact]
    public void CredencialDoAparelhoFicaCifradaERecuperavelComChaveCorreta()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var other = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var encrypted = DeviceSecret.Encrypt("1234", key);
        Assert.NotEqual("1234", encrypted);
        Assert.Equal("1234", DeviceSecret.Decrypt(encrypted, key));
        Assert.ThrowsAny<CryptographicException>(() => DeviceSecret.Decrypt(encrypted, other));
    }
}
