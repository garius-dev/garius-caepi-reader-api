using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Application.Services;
using Garius.Caepi.Reader.Api.Infrastructure.Auth.Services;
using Garius.Caepi.Reader.Api.Infrastructure.Middleware;
using Garius.Caepi.Reader.Api.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;

namespace Garius.Caepi.Reader.Api.Infrastructure.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationMiddleware>();
            builder.Services.AddSingleton<ICacheService, CacheService>();
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();


            builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            builder.Services.AddScoped<ITenantService, TenantService>();
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            builder.Services.AddScoped<ITenantManagementService, TenantManagementService>();
            builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();


            builder.Services.AddHttpClient<IEmailSender, EmailSender>();

            return builder;
        }
    }
}
