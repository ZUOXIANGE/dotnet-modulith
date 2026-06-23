namespace DotNetModulith.Modules.Books.Domain;

public sealed class BookEntity
{
    public Guid Id { get; private set; }
    public string Isbn { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public string Publisher { get; private set; } = string.Empty;
    public DateOnly PublishDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public int TotalCopies { get; private set; }
    public int AvailableCopies { get; private set; }
    public string CoverImageUrl { get; private set; } = string.Empty;
    public BookStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public CategoryEntity? Category { get; private set; }

    private BookEntity()
    {
    }

    public static BookEntity Create(
        string isbn,
        string title,
        string author,
        string publisher,
        DateOnly publishDate,
        string description,
        Guid categoryId,
        int totalCopies,
        string coverImageUrl,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            Isbn = isbn,
            Title = title,
            Author = author,
            Publisher = publisher,
            PublishDate = publishDate,
            Description = description,
            CategoryId = categoryId,
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies,
            CoverImageUrl = coverImageUrl,
            Status = totalCopies > 0 ? BookStatus.Available : BookStatus.OutOfStock,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void UpdateInfo(
        string isbn,
        string title,
        string author,
        string publisher,
        DateOnly publishDate,
        string description,
        Guid categoryId,
        int totalCopies,
        string coverImageUrl,
        DateTimeOffset now)
    {
        Isbn = isbn;
        Title = title;
        Author = author;
        Publisher = publisher;
        PublishDate = publishDate;
        Description = description;
        CategoryId = categoryId;
        TotalCopies = totalCopies;
        CoverImageUrl = coverImageUrl;
        Status = totalCopies > 0 ? BookStatus.Available : BookStatus.OutOfStock;
        UpdatedAt = now;
    }

    public void BorrowCopy(DateTimeOffset now)
    {
        if (AvailableCopies <= 0)
            throw new InvalidOperationException("No available copies to borrow");

        AvailableCopies--;
        if (AvailableCopies == 0)
            Status = BookStatus.OutOfStock;
        UpdatedAt = now;
    }

    public void ReturnCopy(DateTimeOffset now)
    {
        if (AvailableCopies >= TotalCopies)
            throw new InvalidOperationException("All copies are already returned");

        AvailableCopies++;
        if (AvailableCopies > 0 && Status == BookStatus.OutOfStock)
            Status = BookStatus.Available;
        UpdatedAt = now;
    }

    public void SetStatus(BookStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAt = now;
    }
}
