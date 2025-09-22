using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Domain.Constants;
using Garius.Caepi.Reader.Api.Domain.Entities;
using Garius.Caepi.Reader.Api.Domain.Entities.Identity;
using Garius.Caepi.Reader.Api.Exceptions;
using Garius.Caepi.Reader.Api.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.DB;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static Garius.Caepi.Reader.Api.Configuration.AppSecretsConfiguration;
using static Garius.Caepi.Reader.Api.Domain.Constants.DBStatus;

namespace Garius.Caepi.Reader.Api.Application.Services
{
    public class TenantManagementService : ITenantManagementService
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAuthService _authService;
        private readonly IEmailSender _emailSender;
        private readonly UrlSettings _urlSettings;

        public TenantManagementService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IAuthService authService,
            IOptions<UrlSettings> urlSettings,
            IEmailSender emailSender)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _authService = authService;
            _emailSender = emailSender;
            _urlSettings = urlSettings.Value;

            _urlSettings.FrontendBaseUrl = "https://localhost:7090/api/v1/tenant";
        }

        public async Task<Tenant> CreateTenantAsync(string tenantName, TenantStatus status)
        {
            Tenant? tenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.TradeName.ToUpper() == tenantName.ToUpper());

            if (tenant != null)
                throw new ValidationException($"{tenantName} já foi cadastrado.");

            tenant = new Tenant()
            {
                Id = Guid.NewGuid(),
                TradeName = tenantName.SanitizeInput(),
                Status = status,
            };

            _dbContext.Tenants.Add(tenant);
            await _dbContext.SaveChangesAsync();

            return tenant;
        }

        public async Task AssignUserToTenantAsync(Guid userId, Guid tenantId, SystemRoles roleName)
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
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = tenantId,
                RoleId = role.Id
            };

            _dbContext.UserTenants.Add(userTenant);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SignupUserAndTenantAsync(SignupTenantRequest request)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    var tenant = await CreateTenantAsync(request.TenantName, TenantStatus.Pending);

                    var user = await _authService.CreateUserAsync(new RegisterRequest
                    {
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        Email = request.Email,
                        Password = request.Password,
                        ConfirmPassword = request.ConfirmPassword
                    }, false);

                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var encodedToken = token.Base64UrlEncode();

                    SendTenantConfirmationEmailInBackground(
                        _urlSettings.FrontendBaseUrl,
                        _urlSettings.TenantEmailConfirmationPath,
                        request.Email,
                        request.FirstName,
                        tenant.TradeName,
                        user.Id.ToString(),
                        tenant.Id.ToString(),
                        encodedToken);

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task SignupConfirmEmailAsync(Guid userId, Guid tenantId, string token)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    await AssignUserToTenantAsync(userId, tenantId, SystemRoles.Owner);

                    var tenant = await _dbContext.Tenants
                        .FirstOrDefaultAsync(t => t.Id == tenantId);

                    if(tenant == null)
                        throw new NotFoundException("Tenant não encontrado.");

                    //atualiza status do tenant para ativo
                    tenant.Status = TenantStatus.Active;
                    _dbContext.Tenants.Update(tenant);
                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private void SendTenantConfirmationEmailInBackground(string baseUrl, string emailConfirmationUrl, string email, string userName, string tenantName, string userId, string tenantId, string encodedToken)
        {
            var confirmLink = $"{baseUrl}{emailConfirmationUrl}?userId={userId}&tenantId={tenantId}&token={encodedToken}";
            _emailSender.SendEmailInBackground(email, "Confirme seu cadastro",
                templateService => templateService.GetUserInvitationTemplateAsync(tenantName, userName, confirmLink));
        }
    }
}
