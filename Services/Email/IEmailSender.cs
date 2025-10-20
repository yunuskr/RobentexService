using System.Threading.Tasks;

namespace RobentexService.Services.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, string? plainBody = null);
}
