using Microsoft.AspNetCore.Authorization;
using static Garius.Caepi.Reader.Api.Domain.Constants.DbConstants;

namespace Garius.Caepi.Reader.Api.Infrastructure.Auth.Services
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // Concede acesso imediato se o usuário tiver uma das roles de super usuário.
            if (SystemRoles.SuperUserRoles.Any(role => context.User.IsInRole(role)))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Verifica se o usuário possui o claim de permissão específico.
            var hasPermission = context.User.HasClaim(c => c.Type == "permission" && c.Value == requirement.Permission);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
