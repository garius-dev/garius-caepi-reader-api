using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using System.Security.Claims;

namespace Garius.Caepi.Reader.Api.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(ApplicationUser user, IList<string> roles, IEnumerable<string> permissions);
    }
}
