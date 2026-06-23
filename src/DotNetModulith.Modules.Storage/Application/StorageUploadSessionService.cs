using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Storage.Api;
using DotNetModulith.Modules.Storage.Domain;
using DotNetModulith.Modules.Storage.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Storage.Application;

internal sealed class StorageUploadSessionService : IStorageUploadSessionService
{
    private static readonly HashSet<string> ImageContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    ];

    private readonly StorageDbContext _dbContext;
    private readonly IObjectStorageService _objectStorageService;
    private readonly StorageOptions _options;

    public StorageUploadSessionService(
        StorageDbContext dbContext,
        IObjectStorageService objectStorageService,
        Microsoft.Extensions.Options.IOptions<StorageOptions> options)
    {
        _dbContext = dbContext;
        _objectStorageService = objectStorageService;
        _options = options.Value;
    }

    public async Task<UploadSessionDescriptor> CreateUploadSessionAsync(
        Guid ownerUserId,
        CreateUploadSessionInput input,
        CancellationToken ct)
    {
        var purpose = NormalizePurpose(input.Purpose);
        ValidatePurpose(purpose);

        var fileName = input.FileName.Trim();
        var contentType = input.ContentType.Trim().ToLowerInvariant();
        ValidateContentType(purpose, contentType);

        var objectKey = BuildPurposeObjectKey(purpose, fileName);
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(_options.PresignedUrlExpireSeconds);
        var now = DateTimeOffset.UtcNow;

        var session = UploadSessionEntity.Create(
            ownerUserId,
            purpose,
            fileName,
            contentType,
            objectKey,
            expiresAt,
            now);

        var upload = await _objectStorageService.CreatePresignedUploadAsync(fileName, objectKey, ct);

        _dbContext.UploadSessions.Add(session);
        await _dbContext.SaveChangesAsync(ct);

        return new UploadSessionDescriptor(session.Id, upload.ObjectKey, upload.UploadUrl, upload.ExpiresAtUtc);
    }

    public async Task<ConsumedStorageObject> ConsumeUploadSessionAsync(
        Guid ownerUserId,
        Guid uploadId,
        string purpose,
        CancellationToken ct)
    {
        var normalizedPurpose = NormalizePurpose(purpose);
        var now = DateTimeOffset.UtcNow;

        var session = await _dbContext.UploadSessions
            .AsTracking()
            .SingleOrDefaultAsync(x => x.Id == uploadId, ct)
            ?? throw new BusinessException("upload session not found", ApiCodes.Common.NotFound, StatusCodes.Status404NotFound);

        if (session.OwnerUserId != ownerUserId)
            throw new BusinessException("upload session does not belong to current user", ApiCodes.Common.Forbidden, StatusCodes.Status403Forbidden);

        if (!string.Equals(session.Purpose, normalizedPurpose, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("upload session purpose mismatch", ApiCodes.Common.ValidationFailed, StatusCodes.Status400BadRequest);

        if (session.Status == UploadSessionStatus.Consumed)
            throw new BusinessException("upload session has already been consumed", ApiCodes.Common.ValidationFailed, StatusCodes.Status400BadRequest);

        if (session.ExpiresAt < now)
        {
            session.MarkExpired(now);
            await _dbContext.SaveChangesAsync(ct);
            throw new BusinessException("upload session has expired", ApiCodes.Common.ValidationFailed, StatusCodes.Status400BadRequest);
        }

        var objectInfo = await _objectStorageService.GetObjectInfoAsync(session.ObjectKey, ct);
        if (objectInfo is null)
            throw new BusinessException("uploaded object not found", ApiCodes.Common.ValidationFailed, StatusCodes.Status400BadRequest);

        if (objectInfo.Size <= 0)
            throw new BusinessException("uploaded object is empty", ApiCodes.Common.ValidationFailed, StatusCodes.Status400BadRequest);

        ValidateContentType(normalizedPurpose, objectInfo.ContentType);

        session.MarkConsumed(now);
        await _dbContext.SaveChangesAsync(ct);

        return new ConsumedStorageObject(
            session.Id,
            session.ObjectKey,
            _objectStorageService.BuildObjectUrl(session.ObjectKey),
            objectInfo.ContentType,
            objectInfo.Size);
    }

    private static string NormalizePurpose(string purpose) => purpose.Trim().ToLowerInvariant();

    private static void ValidatePurpose(string purpose)
    {
        if (!UploadPurposes.IsSupported(purpose))
            throw new BusinessException("upload purpose is invalid", ApiCodes.Common.ValidationFailed, StatusCodes.Status400BadRequest);
    }

    private static void ValidateContentType(string purpose, string contentType)
    {
        if (!ImageContentTypes.Contains(contentType))
            throw new BusinessException("content type is not allowed", ApiCodes.Common.ValidationFailed, StatusCodes.Status400BadRequest);

        if (string.Equals(purpose, UploadPurposes.BookCover, StringComparison.OrdinalIgnoreCase)
            || string.Equals(purpose, UploadPurposes.UserAvatar, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new BusinessException("upload purpose is invalid", ApiCodes.Common.ValidationFailed, StatusCodes.Status400BadRequest);
    }

    private static string BuildPurposeObjectKey(string purpose, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".bin";

        var folder = purpose switch
        {
            UploadPurposes.BookCover => "books/covers",
            UploadPurposes.UserAvatar => "users/avatars",
            _ => "uploads"
        };

        return $"{folder}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";
    }
}
