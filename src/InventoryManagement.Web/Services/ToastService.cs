namespace InventoryManagement.Web.Services;

public record Toast(Guid Id, string Message, bool IsError);

public class ToastService
{
    readonly object _lock = new();
    readonly List<Toast> _items = new();
    public event Action? OnChange;

    // snapshot copy: the auto-dismiss timer runs on another thread
    public IReadOnlyList<Toast> Items { get { lock (_lock) return _items.ToList(); } }

    public void Show(string message, bool isError = false)
    {
        var t = new Toast(Guid.NewGuid(), message, isError);
        lock (_lock) _items.Add(t);
        OnChange?.Invoke();
        _ = Task.Delay(4000).ContinueWith(_ => Dismiss(t.Id));
    }

    public void Dismiss(Guid id)
    {
        lock (_lock) _items.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }
}
