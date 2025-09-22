using Microsoft.AspNetCore.Authorization;

namespace Garius.Caepi.Reader.Api.Infrastructure.Auth.Services
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            // Concede acesso imediato se o usuário tiver uma das roles de super usuário.
            if (context.User.HasClaim(c => c.Type == "permission" && c.Value == "*"))
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
