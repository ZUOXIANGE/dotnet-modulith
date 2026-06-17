using DotNetModulith.Modules.Users.Domain;
using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Users.Application;

internal sealed class UsersModuleSeeder : IUsersModuleSeeder
{
    public const string DefaultAdminUserName = "admin";
    public const string DefaultAdminPassword = "Admin@123456";
    public const string DefaultLibrarianUserName = "librarian";
    public const string DefaultLibrarianPassword = "Library@123456";

    private static readonly string[] LibrarianPermissions =
    [
        UserPermissions.UsersView,
        UserPermissions.RolesView,
        UserPermissions.BooksView,
        UserPermissions.BooksCreate,
        UserPermissions.BooksEdit,
        UserPermissions.BooksBarcode,
        UserPermissions.BooksImport,
        UserPermissions.CategoriesView,
        UserPermissions.CategoriesCreate,
        UserPermissions.CategoriesEdit,
        UserPermissions.MembersView,
        UserPermissions.MembersCreate,
        UserPermissions.MembersEdit,
        UserPermissions.MemberGroupsView,
        UserPermissions.BorrowingView,
        UserPermissions.BorrowingBorrow,
        UserPermissions.BorrowingReturn,
        UserPermissions.BorrowingRenew,
        UserPermissions.ReservationView,
        UserPermissions.ReservationCreate,
        UserPermissions.ReservationCancel,
        UserPermissions.FinesView,
        UserPermissions.FinesCreate,
        UserPermissions.FinesPay,
        UserPermissions.FinesWaive,
        UserPermissions.ReportsView,
        UserPermissions.ReportsExport,
        UserPermissions.NotificationsView,
        UserPermissions.StorageView,
        UserPermissions.StorageUpload
    ];

    private readonly UsersDbContext _dbContext;
    private readonly IPasswordHasher<UserEntity> _passwordHasher;

    public UsersModuleSeeder(UsersDbContext dbContext, IPasswordHasher<UserEntity> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var adminRole = await _dbContext.Roles
            .AsTracking()
            .Include(x => x.Permissions)
            .SingleOrDefaultAsync(x => x.Name == "Admin", cancellationToken);

        if (adminRole is null)
        {
            adminRole = RoleEntity.Create("Admin", "系统管理员", true, now);
            adminRole.ReplacePermissions(UserPermissions.All, now);
            _dbContext.Roles.Add(adminRole);
        }
        else
        {
            adminRole.ReplacePermissions(UserPermissions.All, now);
        }

        var librarianRole = await _dbContext.Roles
            .AsTracking()
            .Include(x => x.Permissions)
            .SingleOrDefaultAsync(x => x.Name == "Librarian", cancellationToken);

        if (librarianRole is null)
        {
            librarianRole = RoleEntity.Create("Librarian", "图书管理员", true, now);
            librarianRole.ReplacePermissions(LibrarianPermissions, now);
            _dbContext.Roles.Add(librarianRole);
        }
        else
        {
            librarianRole.ReplacePermissions(LibrarianPermissions, now);
        }

        var adminUser = await _dbContext.Users
            .AsTracking()
            .Include(x => x.Roles)
            .SingleOrDefaultAsync(x => x.UserName == DefaultAdminUserName, cancellationToken);

        if (adminUser is null)
        {
            adminUser = UserEntity.Create(DefaultAdminUserName, "系统管理员", "admin@modulith.local", string.Empty, now);
            adminUser.SetPassword(_passwordHasher.HashPassword(adminUser, DefaultAdminPassword), now);
            adminUser.AssignRoles([adminRole.Id], now);
            _dbContext.Users.Add(adminUser);
        }
        else
        {
            if (!adminUser.IsActive)
            {
                adminUser.SetActive(true, now);
            }

            adminUser.AssignRoles([adminRole.Id], now);
        }

        var librarianUser = await _dbContext.Users
            .AsTracking()
            .Include(x => x.Roles)
            .SingleOrDefaultAsync(x => x.UserName == DefaultLibrarianUserName, cancellationToken);

        if (librarianUser is null)
        {
            librarianUser = UserEntity.Create(DefaultLibrarianUserName, "图书管理员", "librarian@modulith.local", string.Empty, now);
            librarianUser.SetPassword(_passwordHasher.HashPassword(librarianUser, DefaultLibrarianPassword), now);
            librarianUser.AssignRoles([librarianRole.Id], now);
            _dbContext.Users.Add(librarianUser);
        }
        else
        {
            if (!librarianUser.IsActive)
            {
                librarianUser.SetActive(true, now);
            }

            librarianUser.AssignRoles([librarianRole.Id], now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
