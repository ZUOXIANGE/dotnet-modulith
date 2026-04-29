using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DotNetModulith.Modules.Storage.Application;

internal sealed class ObjectStorageService : IObjectStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly StorageOptions _options;
    private readonly SemaphoreSlim _bucketInitializationLock = new(1, 1);
    private volatile bool _bucketReady;

    public ObjectStorageService(IAmazonS3 s3Client, IOptions<StorageOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<StorageObjectResult> UploadDirectAsync(IFormFile file, string? objectKey, CancellationToken ct)
    {
        if (file.Length <= 0)
        {
            throw new BusinessException("file is empty", ApiCodes.Common.ValidationFailed, StatusCodes.Status400BadRequest);
        }

        await EnsureBucketExistsAsync(ct);

        var finalKey = BuildObjectKey(file.FileName, objectKey);
        await using var stream = file.OpenReadStream();
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = finalKey,
            InputStream = stream,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
        }, ct);

        return new StorageObjectResult(finalKey, BuildObjectUrl(finalKey), file.Length);
    }

    public async Task<PresignedUploadResult> CreatePresignedUploadAsync(string fileName, string? objectKey, CancellationToken ct)
    {
        await EnsureBucketExistsAsync(ct);

        var finalKey = BuildObjectKey(fileName, objectKey);
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(_options.PresignedUrlExpireSeconds);
        var url = _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = finalKey,
            Verb = HttpVerb.PUT,
            Protocol = _options.UseSsl ? Protocol.HTTPS : Protocol.HTTP,
            Expires = expiresAt.UtcDateTime
        });

        return new PresignedUploadResult(finalKey, url, expiresAt);
    }

    public async Task<byte[]> GetObjectBytesAsync(string objectKey, CancellationToken ct)
    {
        await EnsureBucketExistsAsync(ct);

        try
        {
            var response = await _s3Client.GetObjectAsync(_options.BucketName, objectKey, ct);
            await using var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory, ct);
            return memory.ToArray();
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new BusinessException("object not found", ApiCodes.Common.NotFound, StatusCodes.Status404NotFound);
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        if (_bucketReady)
        {
            return;
        }

        await _bucketInitializationLock.WaitAsync(ct);
        try
        {
            if (_bucketReady)
            {
                return;
            }

            var exists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _options.BucketName);
            if (!exists)
            {
                await _s3Client.PutBucketAsync(_options.BucketName, ct);
            }

            _bucketReady = true;
        }
        finally
        {
            _bucketInitializationLock.Release();
        }
    }

    private string BuildObjectKey(string fileName, string? objectKey)
    {
        if (!string.IsNullOrWhiteSpace(objectKey))
        {
            return objectKey.Trim().TrimStart('/');
        }

        var safeFileName = string.Join("-", fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
            .Replace(' ', '-');
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "file.bin";
        }

        return $"uploads/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}-{safeFileName}";
    }

    private string BuildObjectUrl(string objectKey)
    {
        var endpoint = _options.Endpoint.TrimEnd('/');
        return $"{endpoint}/{_options.BucketName}/{objectKey}";
    }
}
