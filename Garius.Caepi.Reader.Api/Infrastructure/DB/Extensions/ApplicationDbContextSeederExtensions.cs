using Garius.Caepi.Reader.Api.Domain.Constants;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Garius.Caepi.Reader.Api.Infrastructure.DB.Extensions
{
    public static class ApplicationDbContextSeederExtensions
    {
        public static async Task SeedDefaultTenantAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            var dbContext = scopedProvider.GetRequiredService<ApplicationDbContext>();

            var defaultTenant = configuration["TenantSettings:DefaultTenantId"];

            var existingTenant = dbContext.Tenants.Find(Guid.Parse(defaultTenant!));
            if (existingTenant == null)
            {
                dbContext.Tenants.Add(new Domain.Entities.Tenant()
                {
                    Id = Guid.Parse(defaultTenant!),
                    Enabled = true,
                    TradeName = "Garius Tech",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
                await dbContext.SaveChangesAsync();
            }
        }

        public static async Task SeedRolesAndPermissionsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;          

            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            foreach (var roleName in SystemRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new ApplicationRole { Name = roleName };
                    await roleManager.CreateAsync(role);

                    if (roleName.IsSuperUser)
                    {
                        await roleManager.AddClaimAsync(role, new Claim("permission", "*"));
                    }
                }
            }
        }
    }
}
