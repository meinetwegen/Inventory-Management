using InventoryManagement.Web.Models;

namespace InventoryManagement.Web.Services;

public class SessionState
{
    public SessionUser? Current { get; private set; }
    public event Action? OnChange;

    public void Login(SessionUser user) { Current = user; OnChange?.Invoke(); }
    public void Logout() { Current = null; OnChange?.Invoke(); }
}
