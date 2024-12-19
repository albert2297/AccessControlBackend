using MimeKit;
using MailKit.Security;
using MailKit.Net.Smtp;

namespace AccessControl.Application.Services.EmailService
{
    public class EmailService(IConfiguration configuration, IHostEnvironment hostEnvironment): IEmailService
    {
        public async Task SendEmailAsync(EmailServiceRequest emailServiceRequest)
        {
            var smtpSettings = configuration.GetSection("SmtpSettings");
            var loginNotificationEmail = configuration.GetSection("LoginNotificationEmail");

            var fromKey = "From";
            var passwordKey = "Password";
            var smtpServerKey = "SmtpServer";
            var portKey = "Port";

            var fromMail = smtpSettings[fromKey] ?? throw new ArgumentNullException(fromKey, "From address is missing.");
            var fromPassword = smtpSettings[passwordKey] ?? throw new ArgumentNullException(passwordKey, "Password is missing.");
            var smtpServer = smtpSettings[smtpServerKey] ?? throw new ArgumentNullException(smtpServerKey, "SMTP server is missing.");
            var port = int.TryParse(smtpSettings["Port"], out var parsedPort)
                ? parsedPort
                : throw new ArgumentNullException(portKey, "Port is invalid or missing.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Access Control", fromMail));
            message.To.Add(new MailboxAddress("", loginNotificationEmail["RecipientEmail"]));
            message.Subject = loginNotificationEmail["Subject"] ?? "Login Notification";

            string bodyTemplate = loginNotificationEmail["BodyTemplate"] ?? "Hello, [USER_EMAIL] logged in at [LOGIN_TIMESTAMP].";
            string body = bodyTemplate
                .Replace("[USER_EMAIL]", emailServiceRequest.Email)
                .Replace("[LOGIN_TIMESTAMP]", DateTime.Now.ToString());

            message.Body = new TextPart("html") { Text = body };

            using var client = new SmtpClient();
            if (hostEnvironment.IsDevelopment())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
            }

            await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(fromMail, fromPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
