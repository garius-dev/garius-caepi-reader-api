using Garius.Caepi.Reader.Api.Domain.Constants;

namespace Garius.Caepi.Reader.Api.Application.DTOs
{
    public class AssignUserToTenantRequest
    {
        public Guid UserId { get; set; }
        public Guid TenantId { get; set; }
        public SystemRoles RoleName { get; set; } = SystemRoles.User;
    }
}
