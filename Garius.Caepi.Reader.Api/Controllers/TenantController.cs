using Asp.Versioning;
using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Application.Services;
using Garius.Caepi.Reader.Api.Domain.Constants;
using Garius.Caepi.Reader.Api.Exceptions;
using Garius.Caepi.Reader.Api.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using static Garius.Caepi.Reader.Api.Domain.Constants.DBStatus;

namespace Garius.Caepi.Reader.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/tenant")]
    [ApiVersion("1.0")]
    public class TenantController : ControllerBase
    {
        private readonly ITenantManagementService _tenantManagementService;
        public TenantController(ITenantManagementService tenantManagementService)
        {
            _tenantManagementService = tenantManagementService;
        }

        [HttpPost("register")]
        [Authorize(Roles = "Developer")]
        [EnableRateLimiting(RateLimiterExtensions.RegisterPolicy)]
        public async Task<IActionResult> Register([FromBody] RegisterTenantRequest request)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Requisição inválida: " + ModelState.ToFormattedErrorString());

            var tenant = await _tenantManagementService.CreateTenantAsync(
                request.TenantName.SanitizeInput(), TenantStatus.Active);

            return Ok(ApiResponse<object>.Ok(tenant.Id, "Tenant Registrado com Sucesso"));
        }

        [HttpPost("assign-user")]
        //[Authorize]
        public async Task<IActionResult> AssignUserToTenant([FromBody] AssignUserToTenantRequest request)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Requisição inválida: " + ModelState.ToFormattedErrorString());

            await _tenantManagementService.AssignUserToTenantAsync(request.UserId, request.TenantId, request.RoleName);

            return Ok(ApiResponse<object>.Ok("Usuário atribuído ao Tenant com sucesso"));
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignupUserAndTenant([FromBody] SignupTenantRequest request)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Requisição inválida: " + ModelState.ToFormattedErrorString());

            await _tenantManagementService.SignupUserAndTenantAsync(request);

            return Ok(ApiResponse<object>.Ok("Tenant cadastrado com sucesso, verifique o email de confirmação."));
        }

        [HttpGet("confirm-signup")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] Guid tenantId, [FromQuery] string token)
        {
            await _tenantManagementService.SignupConfirmEmailAsync(userId, tenantId, token);

            return Ok(ApiResponse<object>.Ok(new { userId, tenantId }, "Cadastro confirmado com sucesso."));
        }
    }
}
