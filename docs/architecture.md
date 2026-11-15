# Architecture

## Layers

```mermaid
flowchart TD
  C["Components: pages, panels, shared"] --> S["Services: Auth, Product, Bin, Inventory, Report, state containers"]
  S --> D["Data: AppDbContext, SQLite"]
  S --> L["Logic: pure functions"]
  D --> M["Models"]
  L --> M
```

Dependencies point only downwards. `Logic` depends on `Models` only, so it is tested without a database or a browser. Blazor components act as views; services and state containers act as view-models (comparable to MVVM).

## Key decisions

| Decision | Reason |
|---|---|
| `Result<T>` with `Bind` and `Map` | Business rules return success or a message; no exceptions for expected failures |
| Pure `Logic` functions | `BinAssigner`, `PickPlanner`, `ProductLogic`, `CsvExport` are easy to test |
| One `SaveChangesAsync` per operation | Stock change and history record are saved together or not at all |
| `Version` concurrency token on `StockItem` | A second writer gets "refresh and retry" instead of overwriting |
| `IDbContextFactory` | Short-lived context per operation, safe in Blazor Server |
| `AsNoTracking`, `Select`, paging in the database | Less memory, faster queries |
| State containers with `OnChange` | `SessionState`, `ToastService`, `InventoryState` refresh the UI without reload |
| Cascading `CurrentUser` | Components read the user without injecting `SessionState` |
| Roles checked in UI and services | Hiding a button is not security |
| `InventoryService` as a partial class | One file per operation |

## Entities

```mermaid
erDiagram
    AppUser ||--o{ StockTransaction : makes
    Product ||--o{ StockItem : stored_as
    Bin ||--o{ StockItem : holds
    Product ||--o{ StockTransaction : concerns
    Bin ||--o{ StockTransaction : from_to
    AppUser { int Id string Email string PasswordHash Role Role bool IsActive }
    Product { int Id string Sku string Barcode string Name int MinStockLevel bool IsActive }
    Bin { int Id string Code int Capacity }
    StockItem { int Id int ProductId int BinId int Quantity int Version }
    StockTransaction { int Id TxType Type int ProductId int FromBinId int ToBinId int Quantity string Reason int UserId datetime CreatedAt }
```

Database rules: unique `Email`, `Sku`, `Barcode`, bin `Code`, and `(ProductId, BinId)`; `Capacity > 0`; `StockItem.Quantity >= 0`; `StockTransaction.Quantity > 0`; index on `CreatedAt`.

## Algorithms

- **Receive (`BinAssigner`).** Fullest bin that already holds the product and has room; otherwise the smallest empty bin that fits; otherwise "No free space in any bin".
- **Ship (`PickPlanner`).** Take from the bins with the smallest quantity first; fail with "have X, need Y" if the total is too small.
- **Transfer.** Different bins, enough in the source, enough free space in the target.
- **Write-off.** Reason required, quantity not above the bin's stock.

## Ship sequence

```mermaid
sequenceDiagram
  participant Page
  participant Service as InventoryService
  participant DB as Database
  participant Planner as PickPlanner
  participant State as InventoryState
  Page->>Service: ShipAsync(user, barcode, qty)
  Service->>DB: find product, load stock rows
  Service->>Planner: Plan(bins, qty)
  Planner-->>Service: picks or error
  Service->>DB: one SaveChangesAsync (stock + history)
  Service->>State: RefreshAsync()
  State-->>Page: OnChange, panels reload
  Service-->>Page: Result with message
```
