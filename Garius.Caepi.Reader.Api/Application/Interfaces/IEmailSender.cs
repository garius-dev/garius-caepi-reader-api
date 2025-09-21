namespace Garius.Caepi.Reader.Api.Application.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string contentHtml);
        void SendEmailInBackground(string toEmail, string subject, Func<IEmailTemplateService, Task<string>> templateBuilder);
    }
}
