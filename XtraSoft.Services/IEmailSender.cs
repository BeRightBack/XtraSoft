namespace XtraSoft.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message, string? from = null);
        Task SendFromContactAsync(string name, string email, string subject, string message);
    }
}