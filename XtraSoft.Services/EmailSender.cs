using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace XtraSoft.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string message, string? from = null)
        {
            var emailFrom = from ?? _configuration["SmtpSettings:From"];
            var smtpHost = _configuration["SmtpSettings:Host"];
            var smtpPortString = _configuration["SmtpSettings:Port"];
            var smtpUser = _configuration["SmtpSettings:Username"];
            var smtpPass = _configuration["SmtpSettings:Password"];

            if (smtpPortString == null)
            {
                throw new InvalidOperationException("SMTP port configuration is missing.");
            }

            var smtpPort = int.Parse(smtpPortString);

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Administrator", emailFrom));
            emailMessage.To.Add(new MailboxAddress("", to));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = message };

            using var smtpClient = new SmtpClient();
            smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

            // Set timeout settings
            smtpClient.Timeout = 30000; // 30 seconds

            try
            {
                await smtpClient.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(smtpUser, smtpPass);
                await smtpClient.SendAsync(emailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
            finally
            {
                await smtpClient.DisconnectAsync(true);
            }
        }

        public async Task SendFromContactAsync(string name, string email, string subject, string message)
        {
            var emailFrom = _configuration["SmtpSettings:From"];
            var smtpHost = _configuration["SmtpSettings:Host"];
            var smtpPortString = _configuration["SmtpSettings:Port"];
            var smtpUser = _configuration["SmtpSettings:Username"];
            var smtpPass = _configuration["SmtpSettings:Password"];

            if (smtpPortString == null)
            {
                throw new InvalidOperationException("SMTP port configuration is missing.");
            }

            var smtpPort = int.Parse(smtpPortString);

            //From Address    
            string FromAddress = email;
            string FromAddressTitle = name;
            //To Address    
            string? ToAddress = emailFrom ?? throw new InvalidOperationException("SMTP 'From' address configuration is missing.");
            string ToAddressTitle = "Quebec IPTV";
            string Subject = subject;
            string BodyContent = message;

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress
                                    (FromAddressTitle,
                                     FromAddress
                                     ));
            mimeMessage.To.Add(new MailboxAddress
                                     (ToAddressTitle,
                                     ToAddress
                                     ));
            mimeMessage.Subject = Subject; //Subject  
            mimeMessage.Body = new TextPart("plain")
            {
                Text = BodyContent
            };


            using var smtpClient = new SmtpClient();
            smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

            // Set timeout settings
            smtpClient.Timeout = 30000; // 30 seconds

            try
            {
                await smtpClient.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(smtpUser, smtpPass);
                await smtpClient.SendAsync(mimeMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
            finally
            {
                await smtpClient.DisconnectAsync(true);
            }
        }
    }
}
