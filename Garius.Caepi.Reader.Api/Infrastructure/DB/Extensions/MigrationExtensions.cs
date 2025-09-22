using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;
using static Garius.Caepi.Reader.Api.Configuration.AppSecretsConfiguration;

namespace Garius.Caepi.Reader.Api.Infrastructure.DB.Extensions
{
    public static class MigrationExtensions
    {
        public static async Task SeedRolesAndClaimsAsync(WebApplication app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                Log.Information("Running Roles and Claims creation...");
                await ApplicationDbContextSeederExtensions.SeedRolesAndPermissionsAsync(scope.ServiceProvider);
                Log.Information("Roles and Claims creation completed successfully.");

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Seed Roles and Claims process failed.");
                Log.CloseAndFlush();
                Environment.Exit(1);
            }
        }

        public static async Task RunMigrationsAsync(WebApplication app, ConnectionStringSettings connectionStringSettings, bool isDevelopment, bool isDockerRun)
        {
            var rootConnectionString = connectionStringSettings.GetRootConnectionString(isDevelopment, isDockerRun);
            var appConnectionString = connectionStringSettings.GetConnectionString(isDevelopment, true, isDockerRun);

            try
            {
                await EnsureDatabaseExistsAsync(rootConnectionString, connectionStringSettings.Database);

                await AddExtensions(appConnectionString);

                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                Log.Information("Running migrations...");
                await context.Database.MigrateAsync();
                Log.Information("Migrations completed successfully.");

                Log.Information("Running database seed...");
                await ApplicationDbContextSeederExtensions.SeedDefaultTenantAsync(scope.ServiceProvider);
                Log.Information("Database seed completed successfully.");

                await SeedRolesAndClaimsAsync(app);

                await EnsureUsersAndPermissionsAsync(appConnectionString, connectionStringSettings.Database, connectionStringSettings.Users);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Migration process failed.");
                Log.CloseAndFlush();
                Environment.Exit(1);
            }

            Log.CloseAndFlush();
            Environment.Exit(0);
        }

        private static async Task EnsureDatabaseExistsAsync(string rootConnectionString, string databaseName)
        {
            await using var conn = new NpgsqlConnection(rootConnectionString);
            await conn.OpenAsync();

            var existsSql = "SELECT 1 FROM pg_database WHERE datname = @db";
            await using var cmd = new NpgsqlCommand(existsSql, conn);
            cmd.Parameters.AddWithValue("db", databaseName);

            var exists = await cmd.ExecuteScalarAsync();

            if (exists == null)
            {
                Log.Information("Database {DbName} not found. Creating...", databaseName);
                await using var createDb = new NpgsqlCommand($@"CREATE DATABASE ""{databaseName}"";", conn);
                await createDb.ExecuteNonQueryAsync();
                Log.Information("Database {DbName} has been created.", databaseName);
            }
            else
            {
                Log.Information("Database {DbName} already exists.", databaseName);
            }
        }

        private static async Task EnsureUsersAndPermissionsAsync(string appConnectionString, string databaseName, DatabaseUser users)
        {
            using var conn = new NpgsqlConnection(appConnectionString);
            await conn.OpenAsync();

            await EnsureUserExistsAsync(conn, users.Admin.Name, users.Admin.Pwd);
            await EnsureUserExistsAsync(conn, users.Common.Name, users.Common.Pwd);

            var admin = QuoteIdentifier(users.Admin.Name);
            var common = QuoteIdentifier(users.Common.Name);

            var sql = $@"
                -- Grants para Admin
                GRANT CONNECT ON DATABASE ""{databaseName}"" TO {admin};
                GRANT USAGE, CREATE ON SCHEMA public TO {admin};
                GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO {admin};
                GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO {admin};
                ALTER DEFAULT PRIVILEGES FOR USER {admin} IN SCHEMA public
                    GRANT ALL PRIVILEGES ON TABLES TO {admin};
                ALTER DEFAULT PRIVILEGES FOR USER {admin} IN SCHEMA public
                    GRANT ALL PRIVILEGES ON SEQUENCES TO {admin};

                -- Grants para Common
                GRANT CONNECT ON DATABASE ""{databaseName}"" TO {common};
                GRANT USAGE ON SCHEMA public TO {common};
                GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO {common};
                GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO {common};
                ALTER DEFAULT PRIVILEGES IN SCHEMA public
                    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {common};
                ALTER DEFAULT PRIVILEGES IN SCHEMA public
                    GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO {common};

                -- Permissões herdadas (Admin → Common)
                ALTER DEFAULT PRIVILEGES FOR USER {admin} IN SCHEMA public
                    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO {common};
                ALTER DEFAULT PRIVILEGES FOR USER {admin} IN SCHEMA public
                    GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO {common};
            ";

            await using var cmd = new NpgsqlCommand(sql, conn);
            Log.Information("Executing user creation and permission setup...");
            await cmd.ExecuteNonQueryAsync();
            Log.Information("User creation and permission setup completed successfully.");

            await conn.CloseAsync();
        }

        private static async Task EnsureUserExistsAsync(NpgsqlConnection conn, string userName, string password)
        {
            var quotedName = QuoteIdentifier(userName);

            var sql = $@"
                DO
                $do$
                BEGIN
                   IF NOT EXISTS (SELECT 1 FROM pg_catalog.pg_user WHERE usename = {SqlQuotedLiteral(userName)}) THEN
                      EXECUTE 'CREATE USER ' || {SqlLiteralForIdentifier(userName)} || ' WITH PASSWORD ' || quote_literal({SqlQuotedLiteral(password)});
                   END IF;
                END
                $do$;
            ";

            using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task AddExtensions(string appConnectionString)
        {
            await using var conn = new NpgsqlConnection(appConnectionString);
            await conn.OpenAsync();

            var sqlCommands = new[]
            {
                "CREATE EXTENSION IF NOT EXISTS unaccent;",
                "CREATE EXTENSION IF NOT EXISTS pg_trgm;",
                @"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_ts_config WHERE cfgname = 'simple_unaccent') THEN
                        CREATE TEXT SEARCH CONFIGURATION simple_unaccent ( COPY = simple );
                        ALTER TEXT SEARCH CONFIGURATION simple_unaccent
                            ALTER MAPPING FOR hword, hword_part, word WITH unaccent, simple;
                    END IF;
                END$$;
                "
            };

            Log.Information("Executing extensions creation...");
            foreach (var sql in sqlCommands)
            {
                Log.Information("Executing SQL: {Sql}", sql);
                await using var cmd = new NpgsqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            Log.Information("extensions creation completed successfully.");
        }

        private static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

        private static string SqlQuotedLiteral(string s) => "'" + s.Replace("'", "''") + "'";

        private static string SqlLiteralForIdentifier(string s) => "quote_ident(" + SqlQuotedLiteral(s) + ")";
    }
}