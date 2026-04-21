using DotNetModulith.Abstractions.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Users.Application;

internal static class JwtBearerEventsFactory
{
    public static JwtBearerEvents Create() => new()
    {
        OnTokenValidated = async context =>
        {
            var validator = context.HttpContext.RequestServices.GetRequiredService<IJwtSessionValidator>();
            var principal = await validator.ValidateAsync(context.Principal!, context.HttpContext.RequestAborted);
            if (principal is null)
            {
                context.Fail("invalid token session");
                return;
            }

            context.Principal = principal;
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Failure("unauthorized", ApiCodes.Common.Unauthorized),
                context.HttpContext.RequestAborted);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Failure("forbidden", ApiCodes.Common.Forbidden),
                context.HttpContext.RequestAborted);
        }
    };
}
