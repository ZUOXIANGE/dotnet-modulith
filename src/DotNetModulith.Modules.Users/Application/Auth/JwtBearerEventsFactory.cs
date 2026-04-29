using System.Text.Json.Nodes;
using DotNetModulith.Abstractions.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Users.Application.Auth;

internal static class JwtBearerEventsFactory
{
    private const string JsonRpcVersion = "2.0";

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

            if (context.HttpContext.Request.Path.StartsWithSegments("/mcp"))
            {
                await WriteMcpErrorAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    -32001,
                    "unauthorized");
                return;
            }

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Failure("unauthorized", ApiCodes.Common.Unauthorized),
                context.HttpContext.RequestAborted);
        },
        OnForbidden = async context =>
        {
            if (context.HttpContext.Request.Path.StartsWithSegments("/mcp"))
            {
                await WriteMcpErrorAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    -32003,
                    "forbidden");
                return;
            }

            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Failure("forbidden", ApiCodes.Common.Forbidden),
                context.HttpContext.RequestAborted);
        }
    };

    private static async Task WriteMcpErrorAsync(
        HttpContext httpContext,
        int statusCode,
        int errorCode,
        string message)
    {
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new JsonObject
            {
                ["jsonrpc"] = JsonRpcVersion,
                ["id"] = await ReadJsonRpcIdAsync(httpContext),
                ["error"] = new JsonObject
                {
                    ["code"] = errorCode,
                    ["message"] = message
                }
            },
            httpContext.RequestAborted);
    }

    private static async Task<JsonNode?> ReadJsonRpcIdAsync(HttpContext httpContext)
    {
        httpContext.Request.EnableBuffering();

        using var reader = new StreamReader(
            httpContext.Request.Body,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync(httpContext.RequestAborted);
        httpContext.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(body)?["id"]?.DeepClone();
        }
        catch
        {
            return null;
        }
    }
}
