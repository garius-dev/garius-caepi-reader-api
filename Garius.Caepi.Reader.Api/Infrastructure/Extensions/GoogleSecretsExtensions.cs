using Google.Cloud.SecretManager.V1;
using System.Text;

namespace Garius.Caepi.Reader.Api.Infrastructure.Extensions
{
    public static class GoogleSecretsExtensions
    {
        public static IConfiguration AddGoogleSecrets(this WebApplicationBuilder builder, string secretName)
        {
            var projectId = builder.Configuration["GoogleSecretManager:ProjectId"] ?? throw new Exception("GoogleSecretManager:ProjectId is missing.");

            var client = SecretManagerServiceClient.Create();
            var secretVersion = new SecretVersionName(projectId, secretName, "latest");
            var result = client.AccessSecretVersion(secretVersion);
            var secretPayload = result.Payload.Data.ToStringUtf8();

            var config = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(secretPayload)))
                .Build();

            return config;
        }
    }
}
