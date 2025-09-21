﻿using Garius.Caepi.Reader.Api.Application.DTOs;
using Garius.Caepi.Reader.Api.Exceptions;
using System.Text.Json;

namespace Garius.Caepi.Reader.Api.Infrastructure.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (BaseException ex)
            {
                _logger.LogWarning(ex, "Exceção de domínio");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)ex.StatusCode;

                var response = ApiResponse<string>.Fail(ex.Message, context.Response.StatusCode);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
                };

                var result = JsonSerializer.Serialize(response, options);

                await context.Response.WriteAsync(result).ConfigureAwait(false);
            }
        }
    }
}