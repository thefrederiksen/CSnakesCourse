namespace FaceVault.Services;

public interface IDatabaseChangeNotificationService
{
    event Func<Task>? DatabaseChanged;
    Task NotifyDatabaseChangedAsync();
}

public class DatabaseChangeNotificationService : IDatabaseChangeNotificationService
{
    public event Func<Task>? DatabaseChanged;

    public async Task NotifyDatabaseChangedAsync()
    {
        if (DatabaseChanged != null)
        {
            var tasks = DatabaseChanged.GetInvocationList()
                .Cast<Func<Task>>()
                .Select(handler => handler.Invoke());
            
            await Task.WhenAll(tasks);
        }
    }
}