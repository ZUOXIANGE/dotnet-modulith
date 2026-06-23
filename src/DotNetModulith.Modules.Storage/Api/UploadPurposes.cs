namespace DotNetModulith.Modules.Storage.Api;

public static class UploadPurposes
{
    public const string BookCover = "book-cover";
    public const string UserAvatar = "user-avatar";

    public static bool IsSupported(string purpose)
        => string.Equals(purpose, BookCover, StringComparison.OrdinalIgnoreCase)
            || string.Equals(purpose, UserAvatar, StringComparison.OrdinalIgnoreCase);
}
