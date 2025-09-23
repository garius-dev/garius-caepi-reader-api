using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;

namespace Garius.Caepi.Reader.Api.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshTokenResponse> GenerateRefreshTokenAsync(ApplicationUser user, Guid tenantId);
        Task<TokenResponse> RefreshAsync(string token);
        Task RevokeAsync(string token);
    }
}
