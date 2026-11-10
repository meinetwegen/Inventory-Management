using InventoryManagement.Web.Models;

namespace InventoryManagement.Web.Logic;

public static class Permissions
{
    public static bool CanManageCatalog(Role r) => r == Role.Administrator;
    public static bool CanWriteOff(Role r) => r is Role.Administrator or Role.Manager;
    public static bool CanSeeReports(Role r) => r is Role.Administrator or Role.Manager;
}
