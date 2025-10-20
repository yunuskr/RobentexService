using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace RobentexService.Services.Email;

public sealed class SmtpEmailSender(IOptions<EmailSettings> opt) : IEmailSender
{
    private readonly EmailSettings _cfg = opt.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, string? plainBody = null)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_cfg.FromName, _cfg.User));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = plainBody ?? StripHtml(htmlBody)
        };
        msg.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_cfg.Host, _cfg.Port,
            _cfg.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
        await client.AuthenticateAsync(_cfg.User, _cfg.Pass);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
}
