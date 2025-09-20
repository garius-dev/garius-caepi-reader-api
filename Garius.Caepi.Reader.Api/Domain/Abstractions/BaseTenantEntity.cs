using Garius.Caepi.Reader.Api.Domain.Entities;
using Garius.Caepi.Reader.Api.Domain.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace Garius.Caepi.Reader.Api.Domain.Abstractions
{
    public abstract class BaseTenantEntity : BaseEntity, ITenantEntity
    {
        [ForeignKey(nameof(Tenant))]
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }

    }
}
