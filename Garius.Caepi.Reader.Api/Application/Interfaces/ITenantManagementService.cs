using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Domain.Constants;
using Garius.Caepi.Reader.Api.Domain.Entities;
using static Garius.Caepi.Reader.Api.Domain.Constants.DBStatus;

namespace Garius.Caepi.Reader.Api.Application.Interfaces
{
    public interface ITenantManagementService
    {
        Task<Tenant> CreateTenantAsync(string tenantName, TenantStatus status);
        Task AssignUserToTenantAsync(Guid userId, Guid tenantId, SystemRoles roleName);
        Task SignupUserAndTenantAsync(SignupTenantRequest request);
        Task SignupConfirmEmailAsync(Guid userId, Guid tenantId, string token);
    }
}
