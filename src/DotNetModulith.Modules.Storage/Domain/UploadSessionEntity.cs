namespace DotNetModulith.Modules.Storage.Domain;

public sealed class UploadSessionEntity
{
    public Guid Id { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Purpose { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string DeclaredContentType { get; private set; } = string.Empty;
    public string ObjectKey { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public UploadSessionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    private UploadSessionEntity()
    {
    }

    public static UploadSessionEntity Create(
        Guid ownerUserId,
        string purpose,
        string originalFileName,
        string declaredContentType,
        string objectKey,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Purpose = purpose,
            OriginalFileName = originalFileName,
            DeclaredContentType = declaredContentType,
            ObjectKey = objectKey,
            ExpiresAt = expiresAt,
            Status = UploadSessionStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void MarkConsumed(DateTimeOffset now)
    {
        Status = UploadSessionStatus.Consumed;
        ConsumedAt = now;
        UpdatedAt = now;
    }

    public void MarkExpired(DateTimeOffset now)
    {
        Status = UploadSessionStatus.Expired;
        UpdatedAt = now;
    }
}
