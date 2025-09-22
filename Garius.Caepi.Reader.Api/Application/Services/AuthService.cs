using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Garius.Caepi.Reader.Api.Exceptions;
using Garius.Caepi.Reader.Api.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.DB;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using static Garius.Caepi.Reader.Api.Configuration.AppSecretsConfiguration;

namespace Garius.Caepi.Reader.Api.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppKeyManagementSettings _appKeyManagementSettings;

        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ICacheService _cacheService;
        private readonly ITenantService _tenantService;
        private readonly IEmailSender _emailSender;
        private readonly UrlSettings _urlSettings;

        public AuthService(
            IOptions<AppKeyManagementSettings> appKeyManagementSettings,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtTokenService jwtTokenService,
            ICacheService cacheService,
            ITenantService tenantService,
            IOptions<UrlSettings> urlSettings,
            IEmailSender emailSender)
        {
            _appKeyManagementSettings = appKeyManagementSettings.Value;

            _dbContext = dbContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtTokenService = jwtTokenService;
            _cacheService = cacheService;
            _tenantService = tenantService;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _urlSettings = urlSettings.Value;
        }

        private class LoginPayload
        {
            public Guid UserId { get; set; }
            public IList<string> Roles { get; set; } = new List<string>();
            public IList<Claim> Claims { get; set; } = new List<Claim>();
        }

        //public async void xxx()
        //{
        //    var strategy = _dbContext.Database.CreateExecutionStrategy();

        //    await strategy.ExecuteAsync(async () =>
        //    {
        //        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        //        try
        //        {
        //            await transaction.CommitAsync();
        //        }
        //        catch (Exception)
        //        {
        //            await transaction.RollbackAsync();
        //            throw;
        //        }
        //    });
        //}

        public async Task<ApplicationUser> CreateUserAsync(RegisterRequest request, bool sendInvitationEmail = false)
        {
            var emailEncrypt = request.Email.EncryptAndHashText(_appKeyManagementSettings);

            ApplicationUser? user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.EmailHash == emailEncrypt.textHash);

            if (user != null)
                throw new ValidationException("Usuário já cadastrado.");

            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = Guid.NewGuid().ToString(),
                EmailEncrypted = emailEncrypt.encryptedText,
                EmailHash = emailEncrypt.textHash,
                FirstName = request.FirstName.SanitizeInput(),
                LastName = request.LastName.SanitizeInput(),
            };
            user.FullName = $"{user.FirstName} {user.LastName}".Trim();
            user.NormalizedFullName = user.FullName.ToUpperInvariant();

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new ValidationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            if (sendInvitationEmail)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = token.Base64UrlEncode();

                SendConfirmationEmailInBackground(
                    _urlSettings.FrontendBaseUrl,
                    _urlSettings.EmailConfirmationPath,
                    request.Email,
                    user.FirstName,
                    user.Id.ToString(),
                    encodedToken);
            }

            return user;
        }

        public async Task ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                throw new BadRequestException("Link inválido ou expirado.");

            string decodedToken;
            try
            {
                var tokenBytes = token.Base64UrlDecode();
                decodedToken = System.Text.Encoding.UTF8.GetString(tokenBytes);
            }
            catch
            {
                throw new BadRequestException("Link inválido ou expirado.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
                throw new BadRequestException("Link inválido ou expirado.");
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var emailEncrypt = request.Email.EncryptAndHashText(_appKeyManagementSettings);

            ApplicationUser? user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.EmailHash == emailEncrypt.textHash);

            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));

                SendPasswordResetEmailInBackground(
                    _urlSettings.FrontendBaseUrl,
                    _urlSettings.PasswordResetPath,
                    request.Email!,
                    user.FirstName,
                    user.Id.ToString(),
                    encodedToken);
            }
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            if (user == null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                return;
            }

            string decodedToken;
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(request.Token);
                decodedToken = System.Text.Encoding.UTF8.GetString(tokenBytes);
            }
            catch
            {
                throw new BadRequestException("Não foi possível redefinir a senha. Link inválido ou expirado.");
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!result.Succeeded)
                throw new BadRequestException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        public ChallengeResult GetExternalLoginChallengeAsync(string provider, string redirectUrl)
        {
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return new ChallengeResult(provider, properties);
        }

        public async Task<string> ExternalLoginCallbackAsync(string transitionUrl, string? returnUrl)
        {
            ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync().ConfigureAwait(false)
                ?? throw new ValidationException("Não foi possível obter informações do provedor externo.");

            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            if (user == null)
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email)
                    ?? throw new ValidationException("E-mail não fornecido pelo provedor externo.");

                user = await CreateUserAsync(new RegisterRequest
                {
                    Email = email,
                    FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Usuário",
                    LastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? ""
                });

                var loginResult = await _userManager.AddLoginAsync(user, info);
                if (!loginResult.Succeeded)
                {
                    var errors = string.Join(", ", loginResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationAppException($"Falha ao associar login externo: {errors}");
                }
            }

            var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            var claims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);

            var code = SecurityExtensions.CreateOneTimeCode();
            var codeHash = code.ComputeHash();

            await _cacheService.SetAsync(
                $"ext_code:{codeHash}",
                new LoginPayload { UserId = user.Id, Roles = roles, Claims = claims },
                TimeSpan.FromMinutes(1)).ConfigureAwait(false);

            var parts = new List<string> { $"code={Uri.EscapeDataString(code)}" };
            if (!string.IsNullOrWhiteSpace(returnUrl))
                parts.Add($"returnUrl={Uri.EscapeDataString(returnUrl)}");

            return $"{transitionUrl}#{string.Join("&", parts)}";
        }

        private void SendConfirmationEmailInBackground(string baseUrl, string emailConfirmationUrl, string email, string userName, string userId, string encodedToken)
        {
            var confirmLink = $"{baseUrl}{emailConfirmationUrl}?userId={userId}&token={encodedToken}";
            _emailSender.SendEmailInBackground(email, "Confirme seu cadastro",
                templateService => templateService.GetEmailConfirmationTemplateAsync(userName, confirmLink));
        }

        private void SendPasswordResetEmailInBackground(string baseUrl, string passwordResetUrl, string email, string userName, string userId, string encodedToken)
        {
            var resetLink = $"{baseUrl}{passwordResetUrl}?userId={userId}&token={encodedToken}";
            _emailSender.SendEmailInBackground(email, "Redefinição de senha",
                templateService => templateService.GetPasswordResetTemplateAsync(userName, resetLink));
        }

        
    }
}