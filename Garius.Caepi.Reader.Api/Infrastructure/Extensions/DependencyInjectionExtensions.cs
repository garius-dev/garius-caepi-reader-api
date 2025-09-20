using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Application.Services;
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
            builder.Services.AddSingleton<ITenantService, TenantService>();
            builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

            return builder;
        }
    }
}
