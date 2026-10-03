using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PortCell.Api;

public static class OrcamentoRules
{
    public static bool TryCalculate(OrcamentoRequest request, out decimal subtotal, out decimal total)
    {
        subtotal = total = 0;
        if (request.Itens is null || request.Itens.Count is < 1 or > 100 || request.Itens.Any(x =>
            x.Tipo is not ("Servico" or "Peca") || string.IsNullOrWhiteSpace(x.Descricao) || x.Descricao.Length > 300 ||
            x.Quantidade <= 0 || x.Quantidade > 10000 || x.ValorUnitario < 0 || x.ValorUnitario > 1000000 ||
            (x.PecaId.HasValue && x.Tipo != "Peca")) ||
            request.Desconto < 0 || request.ValidoAte.ToUniversalTime() <= DateTime.UtcNow || request.Condicoes?.Length > 2000 ||
            string.IsNullOrWhiteSpace(request.PrazoEstimado) || request.PrazoEstimado.Length > 120)
            return false;
        try { subtotal = request.Itens.Sum(x => decimal.Round(x.Quantidade * x.ValorUnitario, 2)); }
        catch (OverflowException) { return false; }
        if (subtotal > 100_000_000_000m || request.Desconto > subtotal) return false;
        total = subtotal - request.Desconto;
        return true;
    }

    public static string ContentHash(OrcamentoRequest request, decimal subtotal, decimal total)
    {
        var snapshot = new
        {
            Itens = request.Itens.Select(x => new { x.Tipo, Descricao = x.Descricao.Trim(), x.Quantidade, x.ValorUnitario, x.PecaId }),
            Subtotal = subtotal, request.Desconto, Total = total,
            ValidoAte = request.ValidoAte.ToUniversalTime(),
            Condicoes = request.Condicoes?.Trim(), PrazoEstimado = request.PrazoEstimado.Trim()
        };
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(snapshot)));
    }
}

public static class PublicToken
{
    public static string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
