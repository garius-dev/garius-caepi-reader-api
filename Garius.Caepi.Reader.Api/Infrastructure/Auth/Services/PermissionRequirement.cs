using Microsoft.AspNetCore.Authorization;

namespace Garius.Caepi.Reader.Api.Infrastructure.Auth.Services
{
    public class PermissionRequirement(string permission) : IAuthorizationRequirement
    {
        public string Permission { get; } = permission;
    }
}
