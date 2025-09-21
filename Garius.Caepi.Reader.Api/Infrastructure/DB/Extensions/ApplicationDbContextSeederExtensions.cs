using Garius.Caepi.Reader.Api.Domain.Constants;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Garius.Caepi.Reader.Api.Infrastructure.DB.Extensions
{
    public static class ApplicationDbContextSeederExtensions
    {
        public static async Task SeedRolesAndPermissionsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var defaultTenant = configuration["TenantSettings:DefaultTenantId"];

            var dbContext = scopedProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            var existingTenant = dbContext.Tenants.Find(Guid.Parse(defaultTenant!));
            if (existingTenant == null)
            {
                dbContext.Tenants.Add(new Domain.Entities.Tenant()
                {
                    Id = Guid.Parse(defaultTenant!),
                    Enabled = true,
                    Name = "Garius Tech",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
                await dbContext.SaveChangesAsync();
            }

            var allPermissions = DbConstants.GetAllPermissions();
            foreach (var roleName in DbConstants.SystemRoles.SuperUserRoles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null)
                {
                    role = new ApplicationRole(roleName, $"{roleName} role with all permissions.", 0);
                    await roleManager.CreateAsync(role);
                }

                var currentClaims = await roleManager.GetClaimsAsync(role);
                var currentPermissions = currentClaims.Where(c => c.Type == "permission").Select(c => c.Value).ToHashSet();

                foreach (var permission in allPermissions)
                {
                    if (!currentPermissions.Contains(permission))
                    {
                        await roleManager.AddClaimAsync(role, new Claim("permission", permission));
                    }
                }
            }

            if (await roleManager.FindByNameAsync(DbConstants.SystemRoles.Basic) == null)
            {
                await roleManager.CreateAsync(new ApplicationRole(DbConstants.SystemRoles.Basic, "Basic user role with default permissions.", 10));
            }
        }
    }
}
