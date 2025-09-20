using Microsoft.AspNetCore.Identity;

namespace Garius.Caepi.Reader.Api.Domain.Entities.Identity
{
    public class ApplicationUserRole : IdentityUserRole<Guid>
    {
        public virtual ApplicationUser User { get; set; }
        public virtual ApplicationRole Role { get; set; }
    }
}
