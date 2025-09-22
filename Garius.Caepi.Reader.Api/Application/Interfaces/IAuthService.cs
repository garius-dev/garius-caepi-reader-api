using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Domain.Entities;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Garius.Caepi.Reader.Api.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApplicationUser> CreateUserAsync(RegisterRequest request, bool sendInvitationEmail = false);
        Task ConfirmEmailAsync(string userId, string token);
        Task ForgotPasswordAsync(ForgotPasswordRequest request);
        Task ResetPasswordAsync(ResetPasswordRequest request);

        //Task<TokenResponse> LoginAsync(LoginRequest request);
        //Task<TokenResponse> RefreshTokenAsync(string refreshToken);
        //Task<TokenResponse> ExchangeCode(string code);
        //Task ConfirmEmailAsync(string userId, string token);
        //Task ForgotPasswordAsync(ForgotPasswordRequest request);
        //Task ResetPasswordAsync(ResetPasswordRequest request);
        //ChallengeResult GetExternalLoginChallengeAsync(string provider, string redirectUrl);
        //Task<string> ExternalLoginCallbackAsync(string transitionUrl, string? returnUrl);
    }
}
