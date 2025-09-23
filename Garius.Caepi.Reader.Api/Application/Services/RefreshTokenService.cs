using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Garius.Caepi.Reader.Api.Exceptions;
using Garius.Caepi.Reader.Api.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.DB;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Garius.Caepi.Reader.Api.Application.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenService _jwtTokenService;

        public RefreshTokenService(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IJwtTokenService jwtTokenService)
        {
            _db = db;
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<RefreshTokenResponse> GenerateRefreshTokenAsync(ApplicationUser user, Guid tenantId)
        {
            var plainToken = SecurityExtensions.CreateOneTimeCode(64);
            var tokenHash = plainToken.ComputeHash();

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                TenantId = tenantId,
                Token = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return new RefreshTokenResponse(refreshToken.Token, refreshToken.ExpiresAt);
        }

        public async Task<TokenResponse> RefreshAsync(string token)
        {
            var tokenHash = token.ComputeHash();

            var existingToken = await _db.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == tokenHash);

            if (existingToken == null || !existingToken.IsActive)
                throw new UnauthorizedAccessAppException("Token inválido ou expirado");

            existingToken.RevokedAt = DateTime.UtcNow;

            var user = existingToken.User;
            var tenantId = existingToken.TenantId;

            var roles = await _userManager.GetRolesAsync(user);
            var claims = await _userManager.GetClaimsAsync(user);

            var accessToken = _jwtTokenService.GenerateToken(user, tenantId.ToString(), roles, claims);
            var refreshTokenDto = await GenerateRefreshTokenAsync(user, tenantId);

            await _db.SaveChangesAsync();

            return new TokenResponse(accessToken, refreshTokenDto);
        }

        public async Task RevokeAsync(string token)
        {
            var existingToken = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
            if (existingToken == null) return;

            existingToken.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}