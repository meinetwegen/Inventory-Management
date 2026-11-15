# Testing

```
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

Requires the .NET 8 runtime (or `DOTNET_ROLL_FORWARD=Major`).

## Automated tests (61 cases)

| File | Level | Cases |
|---|---|---|
| `ResultTests` | unit | 4 |
| `ProductLogicTests` | unit | 9 |
| `BinAssignerTests` | unit | 9 |
| `PickPlannerTests` | unit | 4 |
| `RulesTests` (transfer, write-off, permissions, reports) | unit | 10 |
| `CsvExportTests` | unit | 7 |
| `InventoryServiceTests` (SQLite in memory) | integration | 10 |
| `StatCardTests`, `DataTableTests`, `PaginationTests`, `CollapsibleSectionTests` | component (bUnit) | 8 |

The integration tests include a full scenario: receive, transfer, ship, scan, history. `ConcurrentChange` checks that EF Core raises a concurrency exception for a stale `Version`; it does not run two real users.
