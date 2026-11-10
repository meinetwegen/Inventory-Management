namespace InventoryManagement.Web.Services;

public record Toast(Guid Id, string Message, bool IsError);

public class ToastService
{
    public List<Toast> Items { get; } = new();
    public event Action? OnChange;

    public void Show(string message, bool isError = false)
    {
        var t = new Toast(Guid.NewGuid(), message, isError);
        Items.Add(t);
        OnChange?.Invoke();
        _ = Task.Delay(4000).ContinueWith(_ => Dismiss(t.Id));
    }

    public void Dismiss(Guid id)
    {
        Items.RemoveAll(t => t.Id == id);
        OnChange?.Invoke();
    }
}
