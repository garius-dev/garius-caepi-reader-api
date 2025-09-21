using Garius.Caepi.Reader.Api.Domain.Abstractions;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;

namespace Garius.Caepi.Reader.Api.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<UserTenant> UserMemberships { get; set; } = new List<UserTenant>();
    }
}