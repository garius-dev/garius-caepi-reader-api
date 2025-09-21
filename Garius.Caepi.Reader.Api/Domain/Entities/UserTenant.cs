using Garius.Caepi.Reader.Api.Domain.Abstractions;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Garius.Caepi.Reader.Api.Domain.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Garius.Caepi.Reader.Api.Domain.Entities
{
    public class UserTenant : BaseEntity, ITenantEntity
    {
        [ForeignKey(nameof(User))]
        public Guid UserId { get; set; }
        public virtual ApplicationUser User { get; set; } = default!;

        [ForeignKey(nameof(Tenant))]
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; } = default!;

        [ForeignKey(nameof(Role))]
        public Guid RoleId { get; set; }
        public virtual ApplicationRole Role { get; set; } = default!;
    }
}
