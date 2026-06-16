namespace DotNetModulith.Modules.Books.Domain;

public sealed class CategoryEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public CategoryEntity? Parent { get; private set; }
    public ICollection<CategoryEntity> Children { get; } = [];
    public ICollection<BookEntity> Books { get; } = [];

    private CategoryEntity()
    {
    }

    public static CategoryEntity Create(string name, string description, Guid? parentId, int sortOrder, DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            ParentId = parentId,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

    public void UpdateInfo(string name, string description, Guid? parentId, int sortOrder, DateTimeOffset now)
    {
        Name = name;
        Description = description;
        ParentId = parentId;
        SortOrder = sortOrder;
        UpdatedAt = now;
    }
}
