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
        private readonly ITenantService _tenantService;
        private readonly AppKeyManagementSettings _appKeyManagementSettings;

        public TenantManagementService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IAuthService authService,
            IOptions<UrlSettings> urlSettings,
            ITenantService tenantService,
            IOptions<AppKeyManagementSettings> appKeyManagementSettings,
            IEmailSender emailSender)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _authService = authService;
            _emailSender = emailSender;
            _urlSettings = urlSettings.Value;
            _tenantService = tenantService;
            _appKeyManagementSettings = appKeyManagementSettings.Value;

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
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                throw new NotFoundException("Usuário não encontrado.");

            var role = await _roleManager.FindByNameAsync(roleName);

            if (role == null)
                throw new NotFoundException("Função não encontrada.");

            var existingMembership = _dbContext.UserTenants
                .Any(ut => ut.TenantId == tenantId && ut.UserId == userId);

            if (existingMembership)
                throw new ValidationException("Usuário já foi registrado.");

            var userTenant = new UserTenant()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = tenantId,
                RoleId = role.Id
            };

            _dbContext.UserTenants.Add(userTenant);
            await _dbContext.SaveChangesAsync();

            //verifica se o usuário já está na role, se não estiver adiciona
            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
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
                    await _authService.ConfirmEmailAsync(userId.ToString(), token);

                    await AssignUserToTenantAsync(userId, tenantId, SystemRoles.Owner);

                    var tenant = await _dbContext.Tenants
                        .FirstOrDefaultAsync(t => t.Id == tenantId);

                    if (tenant == null)
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

        public async Task<InviteUserToTenantResponse> InviteUserToTenantAsync(InviteUserToTenantRequest request)
        {
            var tenantId = _tenantService.GetTenantId();

            var tenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
                throw new NotFoundException("Tenant não encontrado.");

            var emailEncrypt = request.Email.EncryptAndHashText(_appKeyManagementSettings);

            ApplicationUser? user = await _userManager.Users
                .IgnoreQueryFilters()
                .Include(ur => ur.UserRoles)
                    .ThenInclude(r => r.Role)
                    .ThenInclude(rn => rn.Claims)
                .Include(t => t.TenantMemberships.Where(w => w.Enabled))
                    .ThenInclude(tu => tu.Tenant)
                .FirstOrDefaultAsync(u => u.EmailHash == emailEncrypt.textHash);

            if (user == null)
            {
                user = await _authService.CreateUserAsync(new RegisterRequest
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Password = SecurityExtensions.CreateOneTimeCode(12).ComputeHash(),
                    ConfirmPassword = SecurityExtensions.CreateOneTimeCode(12).ComputeHash()
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

                return new InviteUserToTenantResponse(false, "Usuário convidado com sucesso.");
            }

            if(user.TenantMemberships.Any(a => a.TenantId == tenantId && a.Enabled))
                throw new ValidationException("Usuário já possui acesso ao sistema.");

            await AssignUserToTenantAsync(user.Id, tenantId, request.RoleName ?? SystemRoles.User);

            return new InviteUserToTenantResponse(true, "Acesso ao sistema concedido com sucesso.");
        }

        public async Task<ConfirmInviteUserToTenantRequest> ValidateInviteUserToTenantAsync(Guid userId, Guid tenantId, string token)
        {
            var tenant = await _dbContext.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null)
                throw new NotFoundException("Tenant não encontrado.");

            ApplicationUser? user = await _userManager.Users
                .IgnoreQueryFilters()
                .Include(ur => ur.UserRoles)
                    .ThenInclude(r => r.Role)
                    .ThenInclude(rn => rn.Claims)
                .Include(t => t.TenantMemberships.Where(w => w.Enabled))
                    .ThenInclude(tu => tu.Tenant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if(user == null)
                throw new NotFoundException("Usuário não encontrado.");

            await _authService.ConfirmEmailAsync(userId.ToString(), token);

            var userEmail = user.EmailEncrypted.Decrypt(_appKeyManagementSettings.GetKeyBytes(), _appKeyManagementSettings.GetIVBytes());

            ConfirmInviteUserToTenantRequest response = new ConfirmInviteUserToTenantRequest()
            {
                UserId = user.Id,
                TenantId = tenant.Id,
                Email = userEmail,
            };

            return response;
        }

        private void SendTenantConfirmationEmailInBackground(string baseUrl, string emailConfirmationUrl, string email, string userName, string tenantName, string userId, string tenantId, string encodedToken)
        {
            var confirmLink = $"{baseUrl}{emailConfirmationUrl}?userId={userId}&tenantId={tenantId}&token={encodedToken}";
            _emailSender.SendEmailInBackground(email, "Confirme seu cadastro",
                templateService => templateService.GetUserInvitationTemplateAsync(tenantName, userName, confirmLink));
        }
    }
}