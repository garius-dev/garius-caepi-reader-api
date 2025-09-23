using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Domain.Entities;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Garius.Caepi.Reader.Api.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;
using System.Reflection.Emit;
using static Garius.Caepi.Reader.Api.Domain.Constants.DBStatus;

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
        private readonly ITenantService _tenantService;


        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<UserTenant> UserTenants { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }


        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // CONVERSORES DE STATUS
            var tenantStatusConverter = new ValueConverter<TenantStatus, string>(
                v => v.Value, // como salvar no banco
                v => TenantStatus.FromValue(v) // como ler do banco
            );

            // Filtro Global Automático
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (entityType.ClrType.Assembly != typeof(ApplicationUser).Assembly) continue;

                bool isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType);
                bool isBaseEntity = typeof(IBaseEntity).IsAssignableFrom(entityType.ClrType);

                if (!isTenantEntity && !isBaseEntity) continue;

                var parameter = Expression.Parameter(entityType.ClrType, "e");
                Expression? finalFilter = null;

                if (isTenantEntity && entityType.ClrType != typeof(Tenant))
                {
                    var tenantProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                    var tenantId = Expression.Constant(_tenantService.GetTenantId());
                    finalFilter = Expression.Equal(tenantProperty, tenantId);
                }

                if (isBaseEntity)
                {
                    var enabledProperty = Expression.Property(parameter, nameof(IBaseEntity.Enabled));
                    var enabledFilter = Expression.Equal(enabledProperty, Expression.Constant(true));
                    finalFilter = finalFilter == null ? enabledFilter : Expression.AndAlso(finalFilter, enabledFilter);
                }

                if (finalFilter != null)
                {
                    builder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(finalFilter, parameter));
                }
            }

            // Configuração dos Relacionamentos, Nomes de Tabela e ÍNDICES
            builder.Entity<Tenant>(e =>
            {
                e.ToTable("AspNetTenants");

                e.Property(t => t.Status)
                   .HasConversion(tenantStatusConverter);
            });

            builder.Entity<ApplicationUser>(e =>
            {
                // Índices para busca rápida e para garantir unicidade de dados sensíveis.
                e.HasIndex(u => u.EmailHash).IsUnique();

                // Garante que o CpfHash seja único, mas permite múltiplos usuários com CPF nulo.
                //e.HasIndex(u => u.CpfHash).IsUnique().HasFilter("\"CpfHash\" IS NOT NULL");

                // Configuração das relações
                e.HasMany(u => u.Claims).WithOne(c => c.User).HasForeignKey(c => c.UserId).IsRequired();
                e.HasMany(u => u.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId).IsRequired();
                e.HasMany(u => u.RefreshTokens).WithOne(rt => rt.User).HasForeignKey(rt => rt.UserId);
                e.HasMany(u => u.TenantMemberships).WithOne(ut => ut.User).HasForeignKey(ut => ut.UserId);
            });

            builder.Entity<ApplicationRole>(b =>
            {
                b.HasMany(r => r.Claims).WithOne(rc => rc.Role).HasForeignKey(rc => rc.RoleId).IsRequired();
                b.HasMany(r => r.UserRoles).WithOne(ur => ur.Role).HasForeignKey(ur => ur.RoleId).IsRequired();
            });

            builder.Entity<UserTenant>(e =>
            {
                e.ToTable("AspNetUserTenants");
                e.HasKey(ut => new { ut.UserId, ut.TenantId });

               
                // Índice para otimizar a busca de "todos os usuários de um tenant".
                e.HasIndex(ut => ut.TenantId);

                // Configuração das relações
                e.HasOne(ut => ut.User).WithMany(u => u.TenantMemberships).HasForeignKey(ut => ut.UserId);
                e.HasOne(ut => ut.Tenant).WithMany(t => t.UserMemberships).HasForeignKey(ut => ut.TenantId);
                e.HasOne(ut => ut.Role).WithMany().HasForeignKey(ut => ut.RoleId);
            });

            builder.Entity<RefreshToken>(e =>
            {
                e.ToTable("AspNetUserRefreshTokens");
                e.HasIndex(rt => rt.Token).IsUnique();

                e.HasOne(rt => rt.User).WithMany(u => u.RefreshTokens).HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(rt => rt.Tenant).WithMany().HasForeignKey(rt => rt.TenantId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}