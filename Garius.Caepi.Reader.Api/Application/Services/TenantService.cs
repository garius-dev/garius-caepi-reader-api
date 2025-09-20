using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using static Garius.Caepi.Reader.Api.Configuration.AppSecretsConfiguration;

namespace Garius.Caepi.Reader.Api.Application.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Guid _defaultTenantId;

        public TenantService(IHttpContextAccessor httpContextAccessor, IOptions<TenantSettings> tenantSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _defaultTenantId = tenantSettings.Value.DefaultTenantId;
        }

        public Guid GetTenantId()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader) == true &&
                Guid.TryParse(tenantIdHeader, out var headerTenantId))
            {
                return headerTenantId;
            }

            var tenantIdClaim = httpContext?.User?.FindFirstValue("Tid");
            if (Guid.TryParse(tenantIdClaim, out var claimTenantId))
            {
                return claimTenantId;
            }

            return _defaultTenantId;
        }
    }
}
