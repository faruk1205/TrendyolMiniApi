// Infrastructure/Locking/IDistributedLockService.cs
public interface IDistributedLockService
{
    Task<TResult> ExecuteAsync<TResult>(
        IEnumerable<string> resourceKeys,
        string ownerId,
        Func<Task<TResult>> action,
        DistributedLockOptions? options = null);
}