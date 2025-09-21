using AngleSharp.Css;
using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Domain.Entities;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Garius.Caepi.Reader.Api.Exceptions;
using Garius.Caepi.Reader.Api.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.DB;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Garius.Caepi.Reader.Api.Configuration.AppSecretsConfiguration;
using static Garius.Caepi.Reader.Api.Domain.Constants.DbConstants;

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

        public AuthService(
            IOptions<AppKeyManagementSettings> appKeyManagementSettings,
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IJwtTokenService jwtTokenService,
            ICacheService cacheService,
            ITenantService tenantService,
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

            if(user != null)
                throw new ValidationException("Usuário já cadastrado.");

            user = new ApplicationUser
            {
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
                SendConfirmationEmailInBackground("https://localhost:7090/api/v1/weatherforecast", "/confirm-email", request.Email, user.FirstName, user.Id.ToString(), encodedToken);
            }

            return user;
        }

        public async Task<Tenant> CreateTenantAsync(string tenantName)
        {
            Tenant? tenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Name.ToUpper() == tenantName.ToUpper());

            if (tenant != null)
                throw new ValidationException("Tenant já cadastrado.");

            tenant = new Tenant()
            {
                Name = tenantName.SanitizeInput(),
            };

            _dbContext.Tenants.Add(tenant);
            await _dbContext.SaveChangesAsync();

            return tenant;
        }

        public async Task AssignUserToTenantAsync(Guid userId, Guid tenantId, string roleName)
        {            
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role == null)
                throw new NotFoundException("Função não encontrada.");

            var existingMembership = _dbContext.UserTenants
                .Any(ut => ut.TenantId == tenantId && ut.UserId == userId);

            if (existingMembership)
                throw new ValidationException("Usuário já foi regustrado.");

            var userTenant = new UserTenant()
            {
                UserId = userId,
                TenantId = tenantId,
                RoleId = role.Id

            };
            
            _dbContext.UserTenants.Add(userTenant);
            await _dbContext.SaveChangesAsync();
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
                SendPasswordResetEmailInBackground("https://localhost:7090/api/v1/weatherforecast", "/reset-password", request.Email!, user.FirstName, user.Id.ToString(), encodedToken);
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

        private void SendInvitationEmailInBackground(string baseUrl, string validateInvitationUrl, string email, string tenantName, string encodedToken)
        {
            var invitationLink = $"{baseUrl}{validateInvitationUrl}/{encodedToken}";
            _emailSender.SendEmailInBackground(email, $"Convite para {tenantName}",
                templateService => templateService.GetUserInvitationTemplateAsync(tenantName, invitationLink));
        }
    }
}