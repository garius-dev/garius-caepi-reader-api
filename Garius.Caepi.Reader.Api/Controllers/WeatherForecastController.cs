using Asp.Versioning;
using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Application.Interfaces;
using Garius.Caepi.Reader.Api.Domain.Constants;
using Garius.Caepi.Reader.Api.Exceptions;
using Garius.Caepi.Reader.Api.Extensions;
using Garius.Caepi.Reader.Api.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Garius.Caepi.Reader.Api.Domain.Constants.SystemPermissions;

namespace Garius.Caepi.Reader.Api.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/weatherforecast")]
    [ApiVersion("1.0")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, ITenantService tenantService)
        {
            _tenantService = tenantService;
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        [Authorize(Policy = TenantPermissions.Read)]
        public IEnumerable<WeatherForecast> Get()
        {

            var tenantId = _tenantService.GetTenantId();

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
