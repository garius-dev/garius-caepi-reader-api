using Microsoft.AspNetCore.Identity;

namespace Garius.Caepi.Reader.Api.Domain.Entities.Identity
{
    public class ApplicationUserClaim : IdentityUserClaim<Guid>
    {
        public virtual ApplicationUser User { get; set; }
    }
}
