namespace PortCell.Api;

public record ChecklistDefault(string Nome, string[] Opcoes);

public static class ChecklistDefaults
{
    public static readonly ChecklistDefault[] Itens =
    [
        new("Tela / vidro", ["Sem danos", "Riscado", "Trincado", "Quebrado", "Descolando", "Não testado"]),
        new("Display", ["Imagem normal", "Manchas", "Linhas", "Sem imagem", "Piscando", "Não testado"]),
        new("Touch", ["Normal", "Falhas parciais", "Sem resposta", "Toque fantasma", "Não testado"]),
        new("Tampa traseira", ["Íntegra", "Riscada", "Trincada / quebrada", "Descolando"]),
        new("Laterais / chassi", ["Íntegro", "Riscado", "Amassado", "Empenado"]),
        new("Câmeras / lentes", ["Íntegras", "Lente trincada", "Módulo danificado", "Não testado"]),
        new("Conector de carga", ["Íntegro", "Folga", "Dano aparente", "Oxidação", "Não testado"]),
        new("Botões físicos", ["Funcionando", "Falha parcial", "Sem funcionamento", "Não testado"]),
        new("Áudio", ["Funcionando", "Falha", "Não testado"]),
        new("Sinais de abertura anterior", ["Não identificado", "Identificado", "Parafuso faltando / avariado", "Vedação alterada"]),
        new("Sinais de líquido / oxidação", ["Não aparente", "Indício externo", "Identificado em diagnóstico"]),
        new("Biometria / Face ID", ["Funcionando", "Falha", "Indisponível", "Não testado"]),
        new("Wi-Fi / Bluetooth", ["Funcionando", "Falha", "Não testado"]),
        new("Acessórios entregues", ["Nenhum", "Capa", "Cabo", "Carregador", "Chip", "Cartão", "Caixa", "Outros / vários"]),
        new("Observações gerais", ["Sem observações", "Ver observação"])
    ];
    public static readonly string[] Testes = ["Liga e desliga", "Tela e touch", "Carga", "Áudio", "Câmeras", "Conectividade"];
}
