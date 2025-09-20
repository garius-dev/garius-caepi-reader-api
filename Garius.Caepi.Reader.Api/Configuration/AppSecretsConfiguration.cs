using Npgsql;
using System.ComponentModel.DataAnnotations;

namespace Garius.Caepi.Reader.Api.Configuration
{
    public static class AppSecretsConfiguration
    {
        public class JwtSettings
        {
            [Required(ErrorMessage = "A chave secreta do JWT ('Secret') é obrigatória.")]
            public string Secret { get; set; } = string.Empty;

            [Required(ErrorMessage = "O emissor do JWT ('Issuer') é obrigatório.")]
            public string Issuer { get; set; } = string.Empty;

            [Range(1, 1440, ErrorMessage = "O tempo de expiração do JWT ('ExpirationInMinutes') deve estar entre 1 e 1440 minutos.")]
            public int ExpirationInMinutes { get; set; }

            [Required(ErrorMessage = "A audiência do JWT ('Audience') é obrigatória.")]
            public string Audience { get; set; } = string.Empty;

            public string EmailConfirmationUrl { get; set; } = default!;
            public string PasswordResetUrl { get; set; } = default!;
        }

        public class ConnectionStringSettings
        {
            public string Host { get; set; } = string.Empty;
            public string Port { get; set; } = "5432";
            public string Database { get; set; } = string.Empty;
            public DatabaseUser Users { get; set; } = new();

            public void Validate()
            {
                if (string.IsNullOrWhiteSpace(Host)) throw new InvalidOperationException("ConnectionStringSettings.Host is missing.");
                if (string.IsNullOrWhiteSpace(Database)) throw new InvalidOperationException("ConnectionStringSettings.Database is missing.");
                if (Users == null) throw new InvalidOperationException("ConnectionStringSettings.Users is missing.");
                if (string.IsNullOrWhiteSpace(Users.SuperAdmin?.Name) || string.IsNullOrWhiteSpace(Users.SuperAdmin?.Pwd))
                    throw new InvalidOperationException("SuperAdmin credentials missing.");
                if (string.IsNullOrWhiteSpace(Users.Admin?.Name) || string.IsNullOrWhiteSpace(Users.Admin?.Pwd))
                    throw new InvalidOperationException("Admin credentials missing.");
                if (string.IsNullOrWhiteSpace(Users.Common?.Name) || string.IsNullOrWhiteSpace(Users.Common?.Pwd))
                    throw new InvalidOperationException("Common credentials missing.");
            }

            public string GetConnectionString(bool isDevelopment, bool isMigrateOnly, bool isDockerRun)
            {
                var user = isMigrateOnly ? Users.SuperAdmin :
                   (isDevelopment ? Users.Admin : Users.Common);

                var host = isDevelopment && !isDockerRun ? "localhost" : Host;
                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = host,
                    Port = int.TryParse(Port, out var p) ? p : 5432,
                    Database = Database,
                    Username = user.Name,
                    Password = user.Pwd,
                };

                return builder.ConnectionString;
            }

            public string GetRootConnectionString(bool isDevelopment, bool isDockerRun)
            {
                var host = isDevelopment && !isDockerRun ? "localhost" : Host;
                var root = Users.SuperAdmin;
                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = host,
                    Port = int.TryParse(Port, out var p) ? p : 5432,
                    Database = "postgres",
                    Username = root.Name,
                    Password = root.Pwd,
                };

                return builder.ConnectionString;
            }
        }

        public class DatabaseUser
        {
            public DatabaseUserSettings SuperAdmin { get; set; } = new();
            public DatabaseUserSettings Admin { get; set; } = new();
            public DatabaseUserSettings Common { get; set; } = new();
        }

        public class DatabaseUserSettings
        {
            public string Name { get; set; } = string.Empty;
            public string Pwd { get; set; } = string.Empty;
        }

        public class TenantSettings
        {
            public Guid DefaultTenantId { get; set; }
        }

        public class RedisSettings
        {
            public string Host { get; set; } = "localhost";
            public string Port { get; set; } = "6379";
            public string Pwd { get; set; } = string.Empty;

            public string GetConfiguration(bool isDevelopment, bool isDockerRun)
            {
                var host = isDevelopment && !isDockerRun ? "localhost" : Host;
                return $"{host}:{Port},password={Pwd}";
            }

            public void Validate()
            {
                if (string.IsNullOrWhiteSpace(Host)) throw new InvalidOperationException("RedisSettings.Host is missing.");
                if (string.IsNullOrWhiteSpace(Port)) throw new InvalidOperationException("RedisSettings.Port is missing.");
                if (string.IsNullOrWhiteSpace(Pwd)) throw new InvalidOperationException("RedisSettings.Pwd is missing.");
            }
        }
    }
}