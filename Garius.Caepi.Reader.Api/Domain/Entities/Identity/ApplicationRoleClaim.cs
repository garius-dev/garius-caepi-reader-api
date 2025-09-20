using Microsoft.AspNetCore.Identity;

namespace Garius.Caepi.Reader.Api.Domain.Entities.Identity
{
    public class ApplicationRoleClaim : IdentityRoleClaim<Guid>
    {
        public virtual ApplicationRole Role { get; set; }
    }
}
