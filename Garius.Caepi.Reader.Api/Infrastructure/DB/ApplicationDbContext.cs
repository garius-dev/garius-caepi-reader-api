using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Domain.Entities;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Garius.Caepi.Reader.Api.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Garius.Caepi.Reader.Api.Infrastructure.DB
{
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        Guid,
        ApplicationUserClaim,
        ApplicationUserRole,
        IdentityUserLogin<Guid>,
        ApplicationRoleClaim,
        IdentityUserToken<Guid>>
    {
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        private readonly ITenantService _tenantService;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ### FILTRO DE QUERY GLOBAL PARA MULTI-TENANCY ###
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                // Verifica se a entidade implementa a nossa interface de tenant
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    // Constrói a expressão de filtro: e => e.TenantId == _tenantService.GetTenantId()
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                    var tenantId = Expression.Constant(_tenantService.GetTenantId());
                    var tenantFilter = Expression.Equal(property, tenantId);

                    // Aplica o filtro de SOFT-DELETE (Enabled)
                    if (typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType))
                    {
                        var enabledProperty = Expression.Property(parameter, nameof(IBaseEntity.Enabled));
                        var enabledFilter = Expression.Equal(enabledProperty, Expression.Constant(true));

                        // Combina os dois filtros: e.TenantId == tenantId && e.Enabled == true
                        var combinedFilter = Expression.AndAlso(tenantFilter, enabledFilter);
                        var lambda = Expression.Lambda(combinedFilter, parameter);
                        builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                    }
                    else
                    {
                        // Se não for IBaseEntity, aplica apenas o filtro de tenant
                        var lambda = Expression.Lambda(tenantFilter, parameter);
                        builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                    }
                }
                else if (typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(IBaseEntity.Enabled));
                    var body = Expression.Equal(property, Expression.Constant(true));
                    var lambda = Expression.Lambda(body, parameter);
                    builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }

            builder.Entity<RefreshToken>(e =>
            {
                e.ToTable("AspNetUserRefreshTokens");
                e.HasIndex(rt => rt.Token).IsUnique();
            });

            builder.Entity<ApplicationRole>(b =>
            {
                b.HasMany(r => r.UserRoles)
                 .WithOne(ur => ur.Role)
                 .HasForeignKey(ur => ur.RoleId);
            });

            builder.Entity<ApplicationUserClaim>()
                .HasOne(c => c.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(c => c.UserId);

            builder.Entity<ApplicationRoleClaim>()
                .HasOne(rc => rc.Role)
                .WithMany(r => r.Claims)
                .HasForeignKey(rc => rc.RoleId);

            builder.Entity<ApplicationUser>(e =>
            {
                e.HasMany(u => u.UserRoles)
                 .WithOne(ur => ur.User)
                 .HasForeignKey(ur => ur.UserId);

                e.HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId);

                e.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .IsRequired();
            });

            builder.Entity<Tenant>(e =>
            {
                e.ToTable("AspNetTenants");
            });
        }
    }
}