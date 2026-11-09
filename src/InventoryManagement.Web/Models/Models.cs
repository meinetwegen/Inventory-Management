namespace InventoryManagement.Web.Models;

public enum Role { Operator, Manager, Administrator }
public enum TxType { Inbound, Outbound, Transfer, WriteOff }

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Name { get; set; } = "";
    public int MinStockLevel { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Bin
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int Capacity { get; set; }
}

public class StockItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int BinId { get; set; }
    public int Quantity { get; set; }
    public int Version { get; set; } // concurrency token, increased on every change
    public Product Product { get; set; } = null!;
    public Bin Bin { get; set; } = null!;
}

public class StockTransaction
{
    public int Id { get; set; }
    public TxType Type { get; set; }
    public int ProductId { get; set; }
    public int? FromBinId { get; set; }
    public int? ToBinId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Product Product { get; set; } = null!;
    public Bin? FromBin { get; set; }
    public Bin? ToBin { get; set; }
    public AppUser User { get; set; } = null!;
}

public record SessionUser(int Id, string Email, Role Role);
