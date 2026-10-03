using System.Globalization;
using System.Text;

namespace PortCell.Api;

public record OsReportData(Empresa Empresa, OrdemServico Ordem, Cliente Cliente, Aparelho Aparelho,
    OrcamentoVersao Orcamento, IReadOnlyList<OrcamentoItem> Itens, IReadOnlyList<PontoAvaria> Pontos);

public static class OsReportPdf
{
    private const double PageWidth = 595.28;
    private const double PageHeight = 841.89;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static string N(double value) => value.ToString("0.##", Invariant);
    private static string Money(decimal value) => value.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

    public static byte[] Build(OsReportData data)
    {
        var pdf = new Canvas();
        // White paper and black strokes remain legible on monochrome printers and copies.
        pdf.Text(36, 804, "PORTCELL", 18, true);
        pdf.Text(36, 787, Fit(data.Empresa.Nome, 315, 9), 9);
        pdf.RightText(559, 806, "ORDEM DE SERVIÇO", 9, true);
        pdf.RightText(559, 786, $"#{data.Ordem.Numero}", 17, true);
        pdf.Text(36, 770, $"Entrada: {data.Ordem.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}", 8);
        pdf.RightText(559, 770, "Via do cliente | 1 de 1", 8);
        pdf.Line(36, 760, 559, 760);

        pdf.StrokeRect(36, 691, 523, 59);
        pdf.Line(310, 691, 310, 750);
        pdf.Text(48, 737, "CLIENTE", 7, true);
        pdf.Text(48, 722, Fit(data.Cliente.Nome, 250, 10, true), 10, true);
        pdf.Text(48, 708, Fit(data.Cliente.Telefone, 250, 8), 8);
        pdf.Text(48, 697, Fit(data.Cliente.Email ?? "E-mail não informado", 250, 7.5), 7.5);
        pdf.Text(322, 737, "APARELHO", 7, true);
        pdf.Text(322, 722, Fit($"{data.Aparelho.Marca} {data.Aparelho.Modelo}", 225, 10, true), 10, true);
        pdf.Text(322, 708, Fit($"IMEI: {data.Aparelho.Imei ?? "Não informado"}", 225, 8), 8);
        pdf.Text(322, 697, Fit($"Cor: {data.Aparelho.Cor ?? "Não informada"}", 225, 7.5), 7.5);

        pdf.Text(36, 674, "PROBLEMA RELATADO", 9, true);
        var problem = WrapToWidth(data.Ordem.DefeitoRelatado, 523, 8.5, 2);
        for (var i = 0; i < problem.Count; i++) pdf.Text(36, 660 - i * 11, problem[i], 8.5);
        if (!string.IsNullOrWhiteSpace(data.Ordem.Observacoes))
            pdf.Text(36, 634, Fit($"Observações: {data.Ordem.Observacoes}", 523, 7.5), 7.5);
        pdf.Text(36, 615, "CONDIÇÃO DO APARELHO NA ENTRADA", 9, true);
        pdf.Text(36, 602, "Inspeção visual: as seis faces do aparelho estão representadas abaixo.", 8);

        DrawFace(pdf, data.Pontos, "frente", "Frente", 62, 536, 39, 56);
        DrawFace(pdf, data.Pontos, "tras", "Traseira", 149, 536, 39, 56);
        DrawFace(pdf, data.Pontos, "esquerda", "Esquerda", 249, 536, 12, 56);
        DrawFace(pdf, data.Pontos, "direita", "Direita", 336, 536, 12, 56);
        DrawFace(pdf, data.Pontos, "superior", "Superior", 401, 557, 55, 14);
        DrawFace(pdf, data.Pontos, "inferior", "Inferior", 488, 557, 55, 14);
        var rows = Math.Max(1, (int)Math.Ceiling(data.Pontos.Count / 2d));
        var rowHeight = 90d / rows;
        var findingFont = rows <= 4 ? 8d : rows <= 8 ? 7.2d : 6.4d;
        var lineHeight = findingFont + 1;
        var lineCount = Math.Max(1, (int)(rowHeight / lineHeight));
        for (var index = 0; index < data.Pontos.Count; index++)
        {
            var point = data.Pontos[index];
            var name = point.Componente is not null && AtendimentoRules.Componentes.TryGetValue(point.Componente, out var component)
                ? component.Nome : $"Face {point.Face switch { "tras" => "traseira", _ => point.Face }}";
            var findings = string.Join("; ", AtendimentoRules.DefeitosDoPonto(point));
            var description = $"{index + 1}. {name}" + (findings.Length > 0 ? $": {findings}" : "") + (string.IsNullOrWhiteSpace(point.Observacao) ? "" : $". Obs.: {point.Observacao}");
            var lines = WrapToWidth(description, 253, findingFont, lineCount);
            for (var line = 0; line < lines.Count; line++)
                pdf.Text(index < rows ? 36 : 306, 503 - (index % rows) * rowHeight - line * lineHeight, lines[line], findingFont);
        }
        pdf.Circle(40, 398, 3.5);
        pdf.Text(49, 395, "Ponto entregue com defeito à assistência técnica. A numeração identifica o registro acima.", 7.5, true);
        pdf.Text(36, 382, "Sem marcação: sem avaria constatada na inspeção. Textos extensos estão resumidos; íntegra na OS.", 7);

        pdf.Text(36, 362, $"ORÇAMENTO - VERSÃO {data.Orcamento.NumeroVersao}", 9, true);
        pdf.Line(36, 351, 559, 351);
        pdf.Text(43, 339, "DESCRIÇÃO / SERVIÇO OU PEÇA", 7.5, true);
        pdf.CenterText(435, 339, "QTD.", 7.5, true);
        pdf.RightText(551, 339, "VALOR", 7.5, true);
        pdf.Line(36, 333, 559, 333);
        var y = 321d;
        foreach (var item in data.Itens.Take(10))
        {
            pdf.Text(43, y, Fit(item.Descricao, 368, 8), 8);
            pdf.CenterText(435, y, item.Quantidade.ToString("0.###", Invariant), 8);
            pdf.RightText(551, y, Money(decimal.Round(item.Quantidade * item.ValorUnitario, 2)), 8);
            pdf.Line(36, y - 4, 559, y - 4, "0.65 0.65 0.65");
            y -= 11;
        }
        pdf.Line(36, 209, 559, 209);
        pdf.Text(36, 195, $"Subtotal: {Money(data.Orcamento.Subtotal)}    Desconto: {Money(data.Orcamento.Desconto)}", 8);
        pdf.RightText(559, 194, $"TOTAL  {Money(data.Orcamento.Total)}", 11, true);
        pdf.Text(36, 178, Fit($"Prazo após aprovação: {data.Orcamento.PrazoEstimado}  |  Válido até: {data.Orcamento.ValidoAte.ToLocalTime():dd/MM/yyyy}", 523, 8), 8);
        pdf.Text(36, 166, Fit($"Condições: {data.Orcamento.Condicoes ?? "Aprovação específica do orçamento necessária antes do reparo."}", 523, 7.5), 7.5);

        pdf.Text(36, 146, "RETIRADA, GUARDA E APARELHO NÃO RETIRADO", 8, true);
        var terms = new[]
        {
            "Retirada: aviso de disponibilidade e, sem retirada, notificação formal.",
            "Guarda: sem cobrança nesta OS. Taxa exige prazo e valor acordados.",
            "O atraso não autoriza descarte, venda ou incorporação do aparelho.",
            "Destinação: autorização específica do proprietário ou decisão judicial."
        };
        var termY = 133d;
        foreach (var term in terms)
        {
            foreach (var line in WrapToWidth(term, 523, 12, 2))
            {
                pdf.Text(36, termY, line, 12);
                termY -= 14;
            }
        }
        pdf.Text(36, 77, "Referências: CDC, arts. 6º, III, e 51, IV; Código Civil, art. 1.275, III.", 7.5);
        pdf.Line(58, 49, 270, 49);
        pdf.Line(325, 49, 537, 49);
        pdf.CenterText(164, 36, "Assinatura do cliente / responsável", 10);
        pdf.CenterText(431, 36, "Assinatura da assistência", 10);
        pdf.CenterText(PageWidth / 2, 19, "Ciência das condições de entrada e retirada. A execução do reparo depende de aprovação específica do orçamento.", 7);
        return pdf.Build();
    }

    private static void DrawFace(Canvas pdf, IReadOnlyList<PontoAvaria> points, string key, string label, double x, double y, double width, double height)
    {
        pdf.StrokeRect(x, y, width, height);
        if (key is "frente" or "tras")
        {
            pdf.StrokeRect(x + 5, y + 7, width - 10, height - 14, "0.65 0.65 0.65");
            pdf.Circle(x + width / 2, y + height - 5, 1.8);
        }
        var anchors = points.Select((point, index) => (point, index)).Where(p => p.point.Face == key)
            .Select(p => (X: x + (double)p.point.X * width, Y: y + (1 - (double)p.point.Y) * height, Number: p.index + 1)).ToList();
        var useCallouts = anchors.Any(a => anchors.Any(b => a.Number != b.Number && Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) < 110));
        var slots = (from column in Enumerable.Range(-3, 7)
                     from row in Enumerable.Range(0, 5)
                     select (X: x + width / 2 + column * 11d, Y: 541 + row * 11d)).ToList();
        var markers = new List<(double X, double Y, int Number)>();
        foreach (var anchor in anchors)
        {
            var mx = anchor.X;
            var my = anchor.Y;
            if (useCallouts)
            {
                // Callouts preserve the actual finding coordinates while separating nearby numbers.
                var position = slots.MinBy(slot => Math.Pow(anchor.X - slot.X, 2) + Math.Pow(anchor.Y - slot.Y, 2));
                slots.Remove(position);
                mx = position.X;
                my = position.Y;
                pdf.Line(anchor.X, anchor.Y, mx, my, "0.4 0.4 0.4");
                pdf.Circle(anchor.X, anchor.Y, 1.1);
            }
            markers.Add((mx, my, anchor.Number));
        }
        foreach (var marker in markers)
        {
            pdf.Circle(marker.X, marker.Y, 4.5);
            pdf.CenterText(marker.X, marker.Y - 1.8, marker.Number.ToString(Invariant), 5.5, true, "1 1 1");
        }
        pdf.CenterText(x + width / 2, 523, label, 7.5);
    }

    private static string Fit(string value, double width, double size, bool bold = false)
    {
        var cleaned = Safe(value, 4000);
        if (TextWidth(cleaned, size, bold) <= width) return cleaned;
        while (cleaned.Length > 0 && TextWidth(cleaned + "...", size, bold) > width) cleaned = cleaned[..^1];
        return cleaned + "...";
    }

    private static List<string> WrapToWidth(string value, double width, double size, int maxLines)
    {
        var remaining = Safe(value, 4000).Trim();
        var lines = new List<string>();
        while (TextWidth(remaining, size) > width && lines.Count < maxLines - 1)
        {
            var length = 1;
            while (length < remaining.Length && TextWidth(remaining[..(length + 1)], size) <= width) length++;
            var split = remaining.LastIndexOf(' ', length);
            if (split <= 0) split = length;
            lines.Add(remaining[..split]);
            remaining = remaining[split..].TrimStart();
        }
        lines.Add(Fit(remaining, width, size));
        return lines;
    }

    private static string Safe(string value, int max)
    {
        var cleaned = new string(value.Select(ch => ch is '\n' or '\r' or '\t' ? ' ' : ch <= 255 ? ch : '?').ToArray());
        return cleaned.Length <= max ? cleaned : cleaned[..(max - 3)] + "...";
    }

    private static readonly int[] RegularWidths = [278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556, 1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556, 333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 278, 333, 556, 556, 556, 556, 260, 556, 333, 737, 370, 556, 584, 333, 737, 333, 400, 584, 333, 333, 333, 556, 537, 278, 333, 333, 365, 556, 834, 834, 834, 611, 667, 667, 667, 667, 667, 667, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278, 722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611, 556, 556, 556, 556, 556, 556, 889, 500, 556, 556, 556, 556, 278, 278, 278, 278, 556, 556, 556, 556, 556, 556, 556, 584, 611, 556, 556, 556, 556, 500, 556, 500];
    private static readonly int[] BoldWidths = [278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611, 975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556, 333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611, 611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 761, 278, 333, 556, 556, 556, 556, 280, 556, 333, 737, 370, 556, 584, 333, 737, 333, 400, 584, 333, 333, 333, 611, 556, 278, 333, 333, 365, 556, 834, 834, 834, 611, 722, 722, 722, 722, 722, 722, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278, 722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611, 556, 556, 556, 556, 556, 556, 889, 556, 556, 556, 556, 556, 278, 278, 278, 278, 611, 611, 611, 611, 611, 611, 611, 584, 611, 611, 611, 611, 611, 556, 611, 556];

    private static double TextWidth(string value, double size, bool bold = false) =>
        Safe(value, 4000).Sum(ch => (bold ? BoldWidths : RegularWidths)[Math.Clamp((int)ch, 32, 255) - 32]) * size / 1000d;

    private sealed class Canvas
    {
        private readonly StringBuilder content = new();
        public void Rect(double x, double y, double w, double h, string color = "0 0 0") => content.AppendLine($"{color} rg {N(x)} {N(y)} {N(w)} {N(h)} re f");
        public void StrokeRect(double x, double y, double w, double h, string color = "0 0 0") => content.AppendLine($"{color} RG 0.7 w {N(x)} {N(y)} {N(w)} {N(h)} re S");
        public void Line(double x1, double y1, double x2, double y2, string color = "0 0 0") => content.AppendLine($"{color} RG 0.7 w {N(x1)} {N(y1)} m {N(x2)} {N(y2)} l S");
        public void Circle(double x, double y, double r, string color = "0 0 0")
        {
            var k = r * 0.55228475;
            content.AppendLine($"{color} rg {N(x + r)} {N(y)} m {N(x + r)} {N(y + k)} {N(x + k)} {N(y + r)} {N(x)} {N(y + r)} c {N(x - k)} {N(y + r)} {N(x - r)} {N(y + k)} {N(x - r)} {N(y)} c {N(x - r)} {N(y - k)} {N(x - k)} {N(y - r)} {N(x)} {N(y - r)} c {N(x + k)} {N(y - r)} {N(x + r)} {N(y - k)} {N(x + r)} {N(y)} c f");
        }
        public void Text(double x, double y, string value, double size, bool bold = false, string color = "0 0 0")
        {
            var escaped = Safe(value, 500).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            content.AppendLine($"{color} rg BT /{(bold ? "F2" : "F1")} {N(size)} Tf {N(x)} {N(y)} Td ({escaped}) Tj ET");
        }
        public void CenterText(double center, double y, string value, double size, bool bold = false, string color = "0 0 0") =>
            Text(center - TextWidth(value, size, bold) / 2, y, value, size, bold, color);
        public void RightText(double right, double y, string value, double size, bool bold = false) =>
            Text(right - TextWidth(value, size, bold), y, value, size, bold);
        public byte[] Build()
        {
            var contentBytes = Encoding.Latin1.GetBytes(content.ToString());
            var objects = new byte[][]
            {
                Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
                Encoding.ASCII.GetBytes("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
                Encoding.ASCII.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(PageWidth)} {N(PageHeight)}] /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> /Contents 4 0 R >>"),
                Combine(Encoding.ASCII.GetBytes($"<< /Length {contentBytes.Length} >>\nstream\n"), contentBytes, Encoding.ASCII.GetBytes("\nendstream")),
                Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
                Encoding.ASCII.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")
            };
            using var output = new MemoryStream();
            Write(output, "%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
            var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Length; i++)
            {
                offsets.Add(output.Position);
                Write(output, $"{i + 1} 0 obj\n");
                output.Write(objects[i]);
                Write(output, "\nendobj\n");
            }
            var xref = output.Position;
            Write(output, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1)) Write(output, $"{offset:0000000000} 00000 n \n");
            Write(output, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
            return output.ToArray();
        }
        private static byte[] Combine(params byte[][] parts) { using var ms = new MemoryStream(); foreach (var part in parts) ms.Write(part); return ms.ToArray(); }
        private static void Write(Stream stream, string value) => stream.Write(Encoding.Latin1.GetBytes(value));
    }
}
