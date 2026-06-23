namespace DotNetModulith.Modules.Books;

public static class BooksPermissions
{
    public const string BooksView = "books.view";
    public const string BooksCreate = "books.create";
    public const string BooksEdit = "books.edit";
    public const string BooksDelete = "books.delete";
    public const string BooksImport = "books.import";
    public const string BooksBarcode = "books.barcode";

    public const string CategoriesView = "categories.view";
    public const string CategoriesCreate = "categories.create";
    public const string CategoriesEdit = "categories.edit";
    public const string CategoriesDelete = "categories.delete";

    public static readonly IReadOnlyList<string> All =
    [
        BooksView, BooksCreate, BooksEdit, BooksDelete, BooksImport, BooksBarcode,
        CategoriesView, CategoriesCreate, CategoriesEdit, CategoriesDelete
    ];
}
