using Asp.Versioning;
using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Exceptions;
using Garius.Caepi.Reader.Api.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Garius.Caepi.Reader.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/weatherforecast")]
    [ApiVersion("1.0")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IAuthService _authService;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IAuthService authService)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        //[Authorize]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            RegisterRequest request = new RegisterRequest()
            {
                FirstName = "George",
                LastName = "Souza",
                Email = "georgelucas.souza@gmail.com",
                Password = "Biglok09__@@",
                ConfirmPassword = "Biglok09__@@"
            };

            //await _authService.CreateUserAsync(request, true);
            await _authService.ForgotPasswordAsync(new ForgotPasswordRequest() { Email = request.Email });


            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            await _authService.ConfirmEmailAsync(userId, token);

            return Ok(ApiResponse<string>.Ok("E-mail confirmado com sucesso"));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                throw new ValidationException("Requisição inválida: " + ModelState.ToFormattedErrorString());

            await _authService.ResetPasswordAsync(request);

            return Ok(ApiResponse<string>.Ok("Senha redefinida com sucesso"));
        }
    }
}
