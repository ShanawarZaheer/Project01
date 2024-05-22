using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;

namespace Project01.Clients.SMTP
{

    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }

    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpConfiguration _smtpConfig;

        public SmtpEmailSender(IOptions<SmtpConfiguration> smtpConfig)
        {
            _smtpConfig = smtpConfig.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            using (var client = new SmtpClient(_smtpConfig.Server, _smtpConfig.Port))
            {
                client.Credentials = new NetworkCredential(_smtpConfig.UserName, _smtpConfig.Password);
                client.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpConfig.UserName, _smtpConfig.DisplayName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
        }



    }
}
