using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Garius.Caepi.Reader.Api.Configuration;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Garius.Caepi.Reader.Api.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.DB;
using Garius.Caepi.Reader.Api.Infrastructure.DB.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using static Garius.Caepi.Reader.Api.Configuration.AppSecretsConfiguration;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURA��O DAS VARI�VEIS DE AMBIENTE ---
var enableHttpsRedirect =
    builder.Configuration.GetValue<bool?>("HTTPS_REDIRECTION_ENABLED") ?? true;

bool enableDebugEndpoints =
    builder.Configuration.GetValue<bool?>("DEV_ENDPOINTS_ENABLED") ?? false;

bool enableSwagger =
    builder.Configuration.GetValue<bool?>("SWAGGER_ENABLED") ?? false;

bool migrateOnly =
    builder.Configuration.GetValue<bool?>("MIGRATE_ONLY") ?? false;

bool isDockerRun =
    builder.Configuration.GetValue<bool?>("DOCKER_RUN") ?? false;

// --- CONFIGURA��O DO LOG ---
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

if (builder.Environment.IsDevelopment())
{
    Serilog.Debugging.SelfLog.Enable(m => Console.Error.WriteLine(m));
}

builder.Host.UseSerilog((ctx, services, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("app", "garius-api")
    .Enrich.WithProperty("env", ctx.HostingEnvironment.EnvironmentName));

// --- CONFIGURA��O DO RATE LIMITER ---
builder.Services.AddCustomRateLimiter();

// --- CONFIGURA��O DO CORS ---
builder.Services.AddCustomCors(builder.Environment);

// --- CONFIGURA��O DO GOOGLE SECRETS ---
var secretConfig = builder.AddGoogleSecrets("tcm-secrets");

// --- ADICIONA O GOOGLE SECRETS � CONFIGURA��O GLOBAL ---
builder.Configuration.AddConfiguration(secretConfig);

// --- CONFIGURA��O DAS VARIAVEIS GLOBAIS ---
builder.Services.AddValidatedSettings<ConnectionStringSettings>(builder.Configuration, "ConnectionStringSettings");
builder.Services.AddValidatedSettings<GoogleExternalAuthSettings>(builder.Configuration, "GoogleExternalAuthSettings");
builder.Services.AddValidatedSettings<MicrosoftExternalAuthSettings>(builder.Configuration, "MicrosoftExternalAuthSettings");
builder.Services.AddValidatedSettings<CloudflareSettings>(builder.Configuration, "CloudflareSettings");
builder.Services.AddValidatedSettings<ResendSettings>(builder.Configuration, "ResendSettings");
builder.Services.AddValidatedSettings<JwtSettings>(builder.Configuration, "JwtSettings");
builder.Services.AddValidatedSettings<RedisSettings>(builder.Configuration, "RedisSettings");
builder.Services.AddValidatedSettings<AppKeyManagementSettings>(builder.Configuration, "AppKeyManagementSettings");
builder.Services.AddValidatedSettings<TenantSettings>(builder.Configuration, "TenantSettings");
builder.Services.AddValidatedSettings<UrlSettings>(builder.Configuration, "UrlSettings");

// --- CONFIGURA��O DE CONEX�O DO REDIS E DB ---
var redisSettings = builder.Configuration.GetSection("RedisSettings").Get<RedisSettings>()!;
redisSettings.Validate();
var redisConnectionString = redisSettings.GetConfiguration(builder.Environment.IsDevelopment(), isDockerRun);

var connectionStringSettings = builder.Configuration.GetSection("ConnectionStringSettings").Get<ConnectionStringSettings>()!;
connectionStringSettings.Validate();
var connectionString = connectionStringSettings.GetConnectionString(builder.Environment.IsDevelopment(), migrateOnly, isDockerRun);

// --- CONFIGURA��O DO SWAGGER ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<SwaggerConfiguration>();

// --- CONFIGURA��O DO VERSIONAMENTO DO SWAGGER ---
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// --- CONFIGURA��O DO BANCO DE DADOS ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// --- CONFIGURA��O DO REDIS ---
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "Garius:";
});

// --- CONFIGURA��O DO USER IDENTITY ---
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Configura��es de senha
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredUniqueChars = 1;

        // Configura��es de Lockout
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Configura��es de usu�rio
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = false;

        // Configura��es de SignIn
        options.SignIn.RequireConfirmedAccount = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// --- CONFIGURA��O DOS COOKIES DE AUTENTICA��O ---
builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme, options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/api/v1/auth/login";
    options.AccessDeniedPath = "/api/v1/auth/access-denied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtConfig = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig.Issuer,
        ValidAudience = jwtConfig.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
        ClockSkew = TimeSpan.Zero,
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            Log.Information($"Auth failed: {ctx.Exception}");
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            Log.Information($"Token validado para {ctx.Principal.Identity?.Name}");
            return Task.CompletedTask;
        }
    };
})
.AddGoogle("Google", options =>
{
    var google = builder.Configuration.GetSection("GoogleExternalAuthSettings").Get<GoogleExternalAuthSettings>()!;
    options.CorrelationCookie.SameSite = SameSiteMode.None;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ClientId = google.ClientId;
    options.ClientSecret = google.ClientSecret;
    options.SaveTokens = true;
    options.CallbackPath = "/signin-google";
    options.Scope.Add("profile");
    options.Scope.Add("email");
})
.AddMicrosoftAccount("Microsoft", options =>
{
    var ms = builder.Configuration.GetSection("MicrosoftExternalAuthSettings").Get<MicrosoftExternalAuthSettings>()!;
    options.ClientId = ms.ClientId;
    options.ClientSecret = ms.ClientSecret;
    options.SaveTokens = true;
    options.CallbackPath = "/signin-microsoft";
    options.CorrelationCookie.SameSite = SameSiteMode.None;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
});

// --- CONFIGURA��O DOS CONTROLLERS ---
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// --- CONFIGURA��O DO DATA PROTECTION ---
var mux = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services
    .AddDataProtection()
    .SetApplicationName("Garius.Api")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90))
    .PersistKeysToStackExchangeRedis(mux, "DataProtection-Keys");

// --- CONFIGURA��O DO HEALTH CHECK ---
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "PostgreSQL",
               failureStatus: HealthStatus.Unhealthy,
               tags: new[] { "db" })
    .AddRedis(mux, name: "Redis",
              failureStatus: HealthStatus.Unhealthy,
              tags: new[] { "redis" })
    .AddCheck("self", () => HealthCheckResult.Healthy("UP"));

// --- INJE��O DE DEPEND�NCIAS ---
builder.AddApplicationServices();

var app = builder.Build();

// --- CONFIGURA��O DO LOG DE REQUISI��ES ---
app.UseSerilogRequestLogging(o =>
{
    o.EnrichDiagnosticContext = (d, ctx) =>
    {
        d.Set("RequestPath", ctx.Request.Path);
        d.Set("ClientIP", ctx.Connection.RemoteIpAddress?.ToString() ?? "UNKNOWN");
        d.Set("XForwardedFor", ctx.Request.Headers["X-Forwarded-For"].ToString());
        d.Set("CFConnectingIP", ctx.Request.Headers["CF-Connecting-IP"].ToString());
        d.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
        d.Set("StatusCode", ctx.Response?.StatusCode ?? 0);
    };
});

app.UseForwardedHeaders();

// --- CONFIGURA��O DO MIDDLEWARE DE TRATAMENTO DE EXCE��ES ---
app.UseMiddleware<ExceptionHandlingMiddleware>();

// --- CONFIGURA��O DOS HEADERS DE SEGURAN�A ---
var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
app.AddSecurityHeaders(corsOrigins);

// --- HABILITA A REDIRECIONA DE HTTP PARA HTTPS ---
if (enableHttpsRedirect)
{
    app.UseHttpsRedirection();
}

// --- CONFIGURA��O DA BUILD DE MIGRATION ---
if (migrateOnly)
{
    Log.Information("Running in migration-only mode...");
    await MigrationExtensions.RunMigrationsAsync(app, connectionStringSettings, builder.Environment.IsDevelopment(), isDockerRun);
}

await MigrationExtensions.SeedRolesAndClaimsAsync(app);

// --- CONFIGURA��O DO SWAGGER UI ---
var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                $"Garius.Caepi.Reader.Api {description.GroupName.ToUpper(System.Globalization.CultureInfo.InvariantCulture)}");
        }

        options.RoutePrefix = "swagger";
        options.DefaultModelExpandDepth(-1);
    });
}

// --- CONFIGURA��O DO PIPELINE DE REQUISI��ES ---
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseCustomCors();

// --- CONFIGURA��O DA AUTENTICA��O E AUTORIZA��O ---
app.UseAuthentication();
app.UseAuthorization();

// --- CONFIGURA��O DOS CONTROLLERS ---
app.MapControllers();

// --- CRIA��O DO ENDPOINT DE HEALTH CHECK ---
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            details = report.Entries.Select(e => new
            {
                key = e.Key,
                status = e.Value.Status.ToString(),
                description = string.IsNullOrEmpty(e.Value.Description)
                    ? e.Value.Status switch
                    {
                        HealthStatus.Healthy => "UP",
                        HealthStatus.Unhealthy => "DOWN",
                        HealthStatus.Degraded => "DEGRADED",
                        _ => "UNKNOWN",
                    }
                    : e.Value.Description,
                data = e.Value.Data,
            }),
        });
        await context.Response.WriteAsync(result);
    },
});

Log.Information("Iniciando a aplica��o Garius.Caepi.Reader.Api...");
app.Run();