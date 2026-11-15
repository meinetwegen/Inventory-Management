# Testing

```
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

Requires the .NET 8 runtime (or `DOTNET_ROLL_FORWARD=Major`).

## Automated tests (unit, integration, component)

| File | Level | Cases | What it checks |
|---|---|---|---|
| `ResultTests` | unit | 4 | `Ok`, `Fail`, `Map`, `Bind` |
| `ProductLogicTests` | unit | 9 | Product validation (SKU, barcode, name, minimum) |
| `BinAssignerTests` | unit | 9 | Which bin receives a delivery |
| `PickPlannerTests` | unit | 4 | Which bins a shipment is taken from |
| `RulesTests` | unit | 20 | Transfer and write-off rules, role permissions, reports (see below) |
| `CsvExportTests` | unit | 7 | CSV text, quotes, BOM, formula protection |
| `AuthServiceTests` | integration | 9 | Sign-in against SQLite in memory (see below) |
| `InventoryServiceTests` | integration | 10 | Receive, ship, transfer, write-off, full scenario |
| `StatCardTests`, `DataTableTests`, `PaginationTests` | component (bUnit) | 5 | Shared components |
| `CollapsibleSectionTests` | component (bUnit) | 4 | Lazy content, aria-expanded, expand and collapse events |
| `RoleGuardTests` | component (bUnit) | 4 | Redirect to login, access denied, allowed roles |

Total: 85 cases plus 2 end-to-end tests = 87 (last run: 87 passed, 0 failed). Run `dotnet test` for the exact current number.

### Business rules (`RulesTests`)

| Group | Cases |
|---|---|
| Transfer | valid request; zero quantity; same bin; not enough in the source (message shows both numbers); target too full |
| Write-off | missing, empty or blank reason (3 inputs); zero quantity; more than the bin holds; valid request |
| Permissions | one row per role: Administrator may do everything, Manager may write off and see reports, Operator may do neither |
| Reports | low stock lists only products below the minimum with the largest shortage first (a product exactly at the minimum is not low); movements are grouped by day and type with the newest day first; fill percent is a whole number and a capacity of 0 gives 0 |

### Sign-in (`AuthServiceTests`)

| Case | Expected |
|---|---|
| Correct email and password | Ok, with the right role |
| Email in other case and with spaces | Still Ok |
| Wrong password | "Invalid email or password" |
| Unknown email | The same message, so the form does not reveal which emails exist |
| Disabled account | Rejected with the same message |
| Missing email or password (4 inputs) | "Enter email and password" |

### Component tests

`CollapsibleSection`: a closed section does not create its content (lazy loading) and shows "+"; an open one shows the content and has `aria-expanded="true"`; a click raises `ExpandedChanged` with the opposite value.
`RoleGuard`: a visitor who is not signed in is sent to the login page; a signed-in user sees the content unless the allowed roles exclude their role, in which case "Access denied" is shown.

The integration tests include a full scenario: receive, transfer, ship, scan, history. `ConcurrentChange` checks that EF Core raises a concurrency exception for a stale `Version`; it does not run two real users.

## End-to-end tests (Playwright)

Project: `tests/InventoryManagement.E2E`. A real Chromium browser drives the real application, which the test starts on port 5199 with an empty temporary SQLite database.

| Test | What it checks |
|---|---|
| `Login_Receive_ShowsStockInWarehouse` | Sign in as the administrator, open Warehouse, receive 10 pieces by barcode, see the success message with the chosen bin, see the new row in the Stock section |
| `Login_WithWrongPassword_ShowsError` | A wrong password shows "Invalid email or password" |

Run once after installing the browser:

```
dotnet build tests/InventoryManagement.E2E
pwsh tests/InventoryManagement.E2E/bin/Debug/net8.0/playwright.ps1 install chromium
dotnet test tests/InventoryManagement.E2E
```

To watch the browser, set `E2E_HEADED=1` before running (PowerShell: `$env:E2E_HEADED = "1"`). A screenshot is saved to `bin/Debug/net8.0/screenshots/`.

The tests move between pages by clicking menu links, because a full page load signs the user out (known limitation).
