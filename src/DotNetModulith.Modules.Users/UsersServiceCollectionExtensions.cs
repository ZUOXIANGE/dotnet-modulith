using System.Text;
using DotNetModulith.Modules.Users.Application;
using DotNetModulith.Modules.Users.Application.Auth;
using DotNetModulith.Modules.Users.Domain;
using DotNetModulith.Modules.Users.Infrastructure;
using DotNetModulith.Modules.Users.Infrastructure.Caching;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace DotNetModulith.Modules.Users;

/// <summary>
/// 用户模块服务注册扩展方法
/// </summary>
public static class UsersServiceCollectionExtensions
{
    public static IServiceCollection AddUsersInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("modulithdb")
            ?? throw new InvalidOperationException("Connection string 'modulithdb' not found.");

        services.AddDbContext<UsersDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(UsersDbContext).Assembly.FullName);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        return services;
    }

    public static IServiceCollection AddUsersApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AuthCacheOptions>()
            .Bind(configuration.GetSection(AuthCacheOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var redisConnection = configuration.GetConnectionString("redis");
        services.RemoveAll<IDistributedCache>();
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "dotnet-modulith:";
            });
        }

        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IJwtSessionValidator, JwtSessionValidator>();
        services.AddScoped<IUserAuthCache, DistributedUserAuthCache>();
        services.AddScoped<ITokenBlacklistStore, DistributedTokenBlacklistStore>();
        services.AddScoped<IUsersModuleSeeder, UsersModuleSeeder>();
        services.AddScoped<JwtTokenFactory>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IPasswordHasher<UserEntity>, PasswordHasher<UserEntity>>();

        return services;
    }

    public static IServiceCollection AddUsersAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = signingKey,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = JwtBearerEventsFactory.Create();
        });

        services.AddAuthorization(options =>
        {
            foreach (var permission in UserPermissions.All)
            {
                options.AddPolicy(permission, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new PermissionRequirement(permission));
                });
            }
        });

        return services;
    }

    public static async Task SeedUsersModuleAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IUsersModuleSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
}
