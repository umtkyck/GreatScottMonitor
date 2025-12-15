namespace CXA.Client.Services;

public interface ISessionLockService
{
    Task LockAsync(string reason);
    Task UnlockAsync();
}






