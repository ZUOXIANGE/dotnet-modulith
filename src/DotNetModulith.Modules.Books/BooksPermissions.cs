namespace DotNetModulith.Modules.Books;

public static class BooksPermissions
{
    public const string BooksView = "books.view";
    public const string BooksManage = "books.manage";
    public const string BooksImport = "books.import";
    public const string BooksBarcode = "books.barcode";
    public const string CategoriesManage = "categories.manage";

    public static readonly IReadOnlyList<string> All = [BooksView, BooksManage, BooksImport, BooksBarcode, CategoriesManage];
}
