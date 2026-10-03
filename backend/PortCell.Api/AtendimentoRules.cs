namespace PortCell.Api;

public record ComponenteAparelho(string Nome, string Face, decimal X, decimal Y);

public static class AtendimentoRules
{
    public static readonly string[] Faces = ["frente", "tras", "esquerda", "direita", "superior", "inferior"];
    public static readonly IReadOnlyDictionary<string, ComponenteAparelho> Componentes = new Dictionary<string, ComponenteAparelho>
    {
        ["tela"] = new("Tela", "frente", .5m, .53m),
        ["camera-frontal"] = new("Câmera frontal", "frente", .67m, .085m),
        ["face-id"] = new("Face ID", "frente", .41m, .085m),
        ["botoes-volume"] = new("Botões de volume", "esquerda", .5m, .37m),
        ["botao-lateral"] = new("Botão lateral", "direita", .5m, .39m),
        ["conector"] = new("Conector de carga", "inferior", .5m, .5m),
        ["camera-traseira"] = new("Câmera traseira", "tras", .24m, .18m),
        ["traseira"] = new("Tampa traseira", "tras", .5m, .72m),
        ["bateria"] = new("Bateria (interna)", "tras", .5m, .48m)
    };

    public static bool PontosValidos(IReadOnlyList<PontoAvariaRequest>? points) =>
        points is { Count: >= 1 and <= 24 } &&
        points.All(point => point is not null && Faces.Contains(point.Face, StringComparer.Ordinal) &&
            point.X is >= 0m and <= 1m && point.Y is >= 0m and <= 1m &&
            DescricaoValida(point) &&
            (point.Componente is null || Componentes.TryGetValue(point.Componente, out var component) && component.Face == point.Face)) &&
        points.Where(point => point.Componente is null).Select(point => (point.Face, point.X, point.Y)).Distinct().Count() == points.Count(point => point.Componente is null) &&
        points.Where(point => point.Componente is not null).Select(point => point.Componente).Distinct().Count() == points.Count(point => point.Componente is not null);

    private static bool DescricaoValida(PontoAvariaRequest point)
    {
        if (point.Observacao?.Length > 500 || point.Defeito?.Length > 60) return false;
        var defects = point.Defeitos ?? (point.Defeito is null ? [] : new[] { point.Defeito });
        if (defects.Length > 8 || defects.Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 60) ||
            defects.Select(value => value.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != defects.Length) return false;
        // Legacy coordinate-only records remain valid. New records need a finding or a note.
        return defects.Length > 0 || !string.IsNullOrWhiteSpace(point.Observacao) ||
            point.Componente is null && point.Defeitos is null && point.Defeito is null;
    }

    public static string[] DefeitosDoPonto(PontoAvaria point) => point.Defeitos.Length > 0
        ? point.Defeitos : point.Defeito is null ? [] : [point.Defeito];

    public static bool OrcamentoInicialValido(OrcamentoRequest? quote) =>
        quote is not null && quote.Itens is { Count: >= 1 and <= 10 } &&
        quote.Itens.All(item => item.Tipo != "Peca" || item.PecaId.HasValue);
}
