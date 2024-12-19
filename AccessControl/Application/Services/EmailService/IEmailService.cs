namespace AccessControl.Application.Services.EmailService
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailServiceRequest emailServiceRequest);
    }
}
