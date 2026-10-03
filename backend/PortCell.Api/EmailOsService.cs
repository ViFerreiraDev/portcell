using System.Net;
using System.Net.Mail;

namespace PortCell.Api;

public record EmailOsResult(string Status, string Mensagem);

public static class EmailOsService
{
    public static async Task<EmailOsResult> SendAsync(IConfiguration configuration, string? recipient,
        long orderNumber, decimal total, string? approvalLink, byte[] report, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recipient) || !MailAddress.TryCreate(recipient, out var to))
            return new("sem-email", "O cliente não possui e-mail válido cadastrado.");
        var host = configuration["Email:SmtpHost"];
        var from = configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) || !MailAddress.TryCreate(from, out var sender))
            return new("nao-configurado", "Configure Email:SmtpHost e Email:FromAddress para enviar automaticamente.");
        using var message = new MailMessage { From = sender, Subject = $"PortCell - OS #{orderNumber} e orçamento inicial", IsBodyHtml = true };
        message.To.Add(to);
        var linkHtml = approvalLink is null ? "" : $"<p>Para aprovar ou recusar o orçamento, acesse:<br><a href=\"{WebUtility.HtmlEncode(approvalLink)}\">{WebUtility.HtmlEncode(approvalLink)}</a></p>";
        message.Body = $"<p>Olá,</p><p>Segue em anexo a sua ordem de serviço #{orderNumber}, pronta para impressão e assinatura.</p>" +
            $"<p>O orçamento inicial é de <strong>{WebUtility.HtmlEncode(total.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")))}</strong>.</p>" +
            linkHtml + "<p>O reparo será iniciado somente após a aprovação do orçamento.</p><p>PortCell</p>";
        var stream = new MemoryStream(report, writable: false);
        message.Attachments.Add(new Attachment(stream, $"PortCell-OS-{orderNumber}.pdf", "application/pdf"));
        using var smtp = new SmtpClient(host, int.TryParse(configuration["Email:SmtpPort"], out var port) ? port : 587)
        {
            EnableSsl = !bool.TryParse(configuration["Email:EnableSsl"], out var ssl) || ssl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000,
        };
        var username = configuration["Email:Username"];
        if (!string.IsNullOrWhiteSpace(username)) smtp.Credentials = new NetworkCredential(username, configuration["Email:Password"]);
        try
        {
            await smtp.SendMailAsync(message, cancellationToken);
            return new("enviado", $"Cópia da OS e orçamento enviados para {to.Address}.");
        }
        catch (Exception exception) when (exception is SmtpException or IOException or OperationCanceledException or InvalidOperationException)
        {
            return new("falhou", "O envio falhou. Confira as configurações de e-mail e tente novamente.");
        }
    }
}
