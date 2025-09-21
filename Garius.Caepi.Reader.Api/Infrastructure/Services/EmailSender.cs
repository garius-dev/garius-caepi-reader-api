using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Application.Services;
using Garius.Caepi.Reader.Api.Exceptions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Net.Http.Headers;
using System.Text;
using static Garius.Caepi.Reader.Api.Configuration.AppSecretsConfiguration;

namespace Garius.Caepi.Reader.Api.Infrastructure.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly ResendSettings _settings;
        private readonly IServiceProvider _serviceProvider;

        public EmailSender(HttpClient httpClient, 
            IOptions<ResendSettings> settings, 
            IServiceProvider serviceProvider)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
        }

        public void SendEmailInBackground(string toEmail, string subject, Func<IEmailTemplateService, Task<string>> templateBuilder)
        {
            Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                var templateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
                try
                {
                    var body = await templateBuilder(templateService);
                    await emailSender.SendEmailAsync(toEmail, subject, body);
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Falha ao enviar e-mail '{Subject}' para {Email}", subject, toEmail);
                }
            });
        }

        public async Task SendEmailAsync(string toEmail, string subject, string contentHtml)
        {
            var payload = new
            {
                from = _settings.FromEmail,
                to = new[] { toEmail },
                subject,
                html = contentHtml
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new ServiceUnavailableException("Falha ao enviar e-mail de confirmação.");
            }
        }
    }
}
