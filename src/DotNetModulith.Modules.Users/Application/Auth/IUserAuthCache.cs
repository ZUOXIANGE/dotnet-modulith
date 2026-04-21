namespace DotNetModulith.Modules.Users.Application;

public interface IUserAuthCache
{
    Task<UserAuthSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task SetAsync(UserAuthSnapshot snapshot, CancellationToken cancellationToken);

    Task RemoveAsync(Guid userId, CancellationToken cancellationToken);
}
