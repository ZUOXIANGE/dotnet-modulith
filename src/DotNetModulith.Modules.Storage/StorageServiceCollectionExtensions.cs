using Amazon.Runtime;
using Amazon.S3;
using DotNetModulith.Modules.Storage.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.Modules.Storage;

internal static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
            var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);

            return new AmazonS3Client(credentials, new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                ForcePathStyle = options.ForcePathStyle,
                UseHttp = !options.UseSsl
            });
        });

        services.AddSingleton<IObjectStorageService, ObjectStorageService>();
        return services;
    }
}
