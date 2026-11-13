# Inventory Management

A warehouse web application for small teams: know what you have, where it is stored, and what happened to it.

The application tracks products and storage bins, places incoming goods automatically, plans shipments, moves and writes off stock, looks up items by barcode, keeps a full history of every operation, and produces reports that can be downloaded as CSV. Access is controlled by three roles.

It was built by team of 2 students for the course Introduction to Functional Programming (C# / .NET), using Blazor Server, Entity Framework Core and SQLite.

## Contents

1. [Features](#features)
2. [Technology stack](#technology-stack)
3. [Quick start](#quick-start)
4. [Demo accounts and demo data](#demo-accounts-and-demo-data)
5. [A two-minute tour](#a-two-minute-tour)
6. [Roles and permissions](#roles-and-permissions)
7. [How the business rules work](#how-the-business-rules-work)
8. [Architecture](#architecture)
9. [Project structure](#project-structure)
10. [Data model](#data-model)
11. [Configuration](#configuration)
12. [Testing](#testing)
13. [Troubleshooting](#troubleshooting)
14. [Known limitations](#known-limitations)
15. [Roadmap](#roadmap)
16. [Development workflow](#development-workflow)
17. [Documentation index](#documentation-index)
18. [Sources and credits](#sources-and-credits)

## Features

- **Dashboard.** Number of products, units in stock, bins in use and products below their minimum level, with the five most urgent shortages. It refreshes after every operation.
- **Warehouse page.** One page with four collapsible sections: Operations, Stock, Products and Bins. A section loads its data only when it is opened for the first time.
- **Operations.** Receive, Ship, Transfer and Write off, plus a Scan tab for barcode lookup. Receiving chooses the bin for you; shipping chooses which bins to take from.
- **Products.** Search, paging, add, edit and disable. Products are never deleted, so history always stays valid.
- **Bins.** Capacity, current fill and a fill bar. Administrators can add bins.
- **History.** Every operation with date, type, product, quantity, source and target bins, user and reason. Filter by type, 15 rows per page.
- **Reports.** Low stock, bin fill and movements for the last 30 days. Each report is a collapsible section with paging and a CSV download.
- **Role-based access.** Administrator, Manager and Operator. Rules are enforced in the menu, on pages and again inside the services.
- **Data safety.** Every operation changes stock and writes its history record in one database save, so it either fully succeeds or does nothing. A version token detects two people changing the same stock row.
- **Responsive layout.** Breakpoints at 900 px and 600 px. On phones tables become cards and buttons are at least 44 px high.
- **Accessibility basics.** Labelled form fields, announced notifications, keyboard operation, dialog roles.

## Technology stack

| Area | Technology | Why |
|---|---|---|
| Language and runtime | C# 12, .NET 8 | Course language, long-term support |
| UI | Blazor Server (interactive server rendering) | One language for UI and logic; no separate API is needed |
| Data access | Entity Framework Core 8 with SQLite | Code-first model, constraints, no database installation |
| Passwords | BCrypt.Net-Next | Salted, deliberately slow hashes |
| Unit and integration tests | xUnit | Standard .NET test framework |
| Component tests | bUnit | Renders Blazor components without a browser |
| Packaging | Docker and Docker Compose | Same start on Windows, macOS and Linux |

There is intentionally no REST API. Blazor Server runs on the server and calls the services directly.

## Quick start

### Option A: Docker (recommended)

Requirements: Docker Desktop (on Windows with WSL 2) or Docker Engine with the Compose plugin.

```
docker compose up --build
```

Open http://localhost:8080 and sign in with one of the demo accounts below. The database lives in a Docker volume, so your data survives restarts. The compose file also turns on the demo data set (200 extra products) so that paging is visible.

To stop: press Ctrl+C, or run `docker compose down`. To wipe all data and start fresh: `docker compose down -v`.

### Option B: without Docker

Requirements: the .NET 8 SDK (check with `dotnet --version`).

```
dotnet run --project src/InventoryManagement.Web
```

The console prints the address (usually http://localhost:5000 or a similar port). A file named `inventory.db` is created next to the application. To include the 200 demo products first set the environment variable:

```
# PowerShell
$env:SEED_DEMO = "true"
# bash
export SEED_DEMO=true
```

To start fresh, stop the app and delete `inventory.db`.

## Demo accounts and demo data

| Role | Email | Password |
|---|---|---|
| Administrator | admin@demo.com | Admin123! |
| Manager | manager@demo.com | Manager123! |
| Operator | operator@demo.com | Operator123! |

Seeded bins:

| Bin | Capacity |
|---|---|
| A-01, A-02 | 100 each |
| B-01, B-02 | 50 each |
| C-01 | 20 |

Seeded products:

| SKU | Name | Barcode | Minimum |
|---|---|---|---|
| LAP-001 | Laptop 15" | 4006381333931 | 5 |
| MOU-001 | Wireless Mouse | 5901234123457 | 20 |
| KEY-001 | Mechanical Keyboard | 9780201379624 | 10 |
| MON-001 | Monitor 27" | 7312345678901 | 4 |

The database starts with no stock at all, so every product begins in the Low stock report. With demo data enabled there are also products DEMO-001 to DEMO-200.

Seeding only runs when the database has no users, so restarting never creates duplicates.

## A two-minute tour

Sign in as the administrator and open Warehouse.

1. In Operations choose Receive, enter barcode `5901234123457` and quantity 10. The message says the mice were placed in bin C-01 (the smallest empty bin that fits).
2. Receive 5 more of the same barcode. They go to the same bin.
3. Try to receive 500 keyboards (`9780201379624`). You get "No free space in any bin" and nothing changes.
4. Open Transfer, move 4 mice from C-01 to A-01.
5. Open Ship and try to ship 20 mice. The error shows how many exist and how many were requested. Ship 8 instead: stock is taken from the smaller pile first.
6. Open the Scan tab and look up `5901234123457` to see where the remaining mice are.
7. Open the Stock section to see the result, then History to see every step.
8. Open Reports, expand Low stock and press Download CSV.

Then sign out and sign in as the operator to see what is hidden: no Reports link and no Write off tab.

## Roles and permissions

| Action | Administrator | Manager | Operator |
|---|---|---|---|
| Add, edit and disable products; add bins | Yes | | |
| Write off stock | Yes | Yes | |
| View reports and download CSV | Yes | Yes | |
| Receive, ship, transfer, scan | Yes | Yes | Yes |
| View stock, products, bins, history, dashboard | Yes | Yes | Yes |

Permissions are defined in one place (`Logic/Permissions.cs`) and checked three times: the menu hides links, pages guard their content with `RoleGuard`, and every service checks the role again.

## How the business rules work

### Receiving (bin assignment)

1. Use a bin that already holds this product and has room. If several qualify, take the fullest one so the product is not spread out.
2. Otherwise use the empty bin with the smallest capacity that fits the whole delivery.
3. Otherwise fail with "No free space in any bin".

### Shipping (pick planning)

Stock is taken from the bins with the smallest quantity first, so bins are emptied and become free for new goods. If the total on hand is too small, nothing is shipped and the message states how much exists and how much was requested. A shipment from two bins creates two history records.

### Transfer

Allowed only when the source and target bins differ, the source holds enough, and the target has enough free capacity.

### Write-off

Requires a reason (damaged, expired, lost and so on) and cannot exceed what is in the bin. Operators cannot write off.

### Data integrity

- Each operation saves stock changes and the history record with a single `SaveChangesAsync`.
- A `Version` number on every stock row is increased on each change. If someone else changed the row first, the second save fails and the user sees "Stock was changed by someone else. Please refresh and try again."
- The database itself rejects negative stock, zero or negative bin capacity, zero quantities in history, and duplicate SKUs, barcodes, bin codes and emails.

## Architecture

The project is a layered application. Dependencies point only downwards:

```
Components  (pages, panels, shared components)
    |
Services    (Auth, Product, Bin, Inventory, Report; state containers)
    |            |
  Data         Logic     (pure functions, no I/O)
    |            |
        Models
```

- `Logic` knows nothing about the database or the UI, so it is tested without either.
- Business rules return `Result<T>` (success or an error message) instead of throwing exceptions. `Bind` chains steps that can fail, `Map` transforms a successful value.
- Three state containers raise an `OnChange` event: `SessionState` (current user), `ToastService` (notifications) and `InventoryState` (dashboard summary). Panels subscribe, so the screen updates without reloading.
- The current user is also passed down the component tree as a cascading value.
- Each operation uses a short-lived `DbContext` created from `IDbContextFactory`, which is the safe pattern for Blazor Server.


## Project structure

```
InventoryManagement/
  src/InventoryManagement.Web/
    Models/       entities, enums, SessionUser
    Data/         AppDbContext (indexes, constraints), DbSeeder (demo data)
    Logic/        Result, Permissions, ProductLogic, BinAssigner, PickPlanner,
                  TransferLogic, WriteOffLogic, Reports, CsvExport
    Services/     AuthService, ProductService, BinService, ReportService,
                  InventoryService (partial class, one file per operation),
                  SessionState, ToastService, InventoryState
    Components/
      Layout/     MainLayout, NavMenu
      Pages/      Login, Dashboard, Warehouse, Transactions, Reports
      Panels/     OperationsPanel, ScanPanel, StockPanel, ProductsPanel, BinsPanel
      Shared/     DataTable<T>, Pagination, ConfirmModal, FormField, StatCard,
                  RoleGuard, ToastHost, StockOperationForm, CollapsibleSection
    wwwroot/      css/app.css, js/download.js
  tests/InventoryManagement.Tests/   unit, SQLite in-memory and bUnit tests
  docs/           architecture, user guide, QA checklist, testing notes, criteria
  Dockerfile, docker-compose.yml
```

## Data model

| Entity | Main fields | Rules |
|---|---|---|
| AppUser | Email, PasswordHash, Role, IsActive | Email unique |
| Product | Sku, Barcode, Name, MinStockLevel, IsActive | SKU and barcode unique; disabled, never deleted |
| Bin | Code, Capacity | Code unique; capacity greater than 0 |
| StockItem | Product, Bin, Quantity, Version | One row per (product, bin); quantity not below 0 |
| StockTransaction | Type, Product, FromBin, ToBin, Quantity, Reason, User, CreatedAt | Quantity greater than 0; types Inbound, Outbound, Transfer, WriteOff |

## Configuration

| Setting | Where | Default | Meaning |
|---|---|---|---|
| `ConnectionStrings__Default` | environment variable or appsettings | `Data Source=inventory.db` | SQLite file location |
| `SEED_DEMO` | environment variable | not set | `true` adds 200 demo products on first start |
| `ASPNETCORE_URLS` | environment variable | set to `http://+:8080` in Docker | Address the server listens on |

In Docker the database is stored at `/data/inventory.db` inside the `inventory-data` volume. Application logs (who did what, and every error) are written to the console: use `docker compose logs -f web`.

## Testing

```
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

The solution contains about 60 automated tests. Run the command for the exact number.

| Group | What it checks |
|---|---|
| Logic | `Result<T>`, product validation, bin assignment, pick planning, transfer and write-off rules, permissions, reports, CSV export |
| Services | Receive, ship, transfer, write-off and a full scenario against a real SQLite database in memory, including the shortage and no-space cases |
| Components | StatCard, DataTable, Pagination and CollapsibleSection with bUnit |

There is no browser end-to-end test. The manual checklist in `docs/qa-checklist.md` covers every role, each operation with valid and invalid input, paging, the phone layout and several browsers.

## Troubleshooting

| Problem | Cause and fix |
|---|---|
| `dotnet test` says it must install .NET 8 | Only a newer runtime is installed. Install the .NET 8 runtime, or set `DOTNET_ROLL_FORWARD=Major` for the current terminal. |
| Port 8080 is already in use | Change `"8080:8080"` in `docker-compose.yml` to another host port, for example `"8081:8080"`. |
| Docker fails to start on Windows | Make sure Docker Desktop is running and WSL 2 is enabled. Give Docker enough memory in its settings. |
| Changed the data model and the app crashes on start | The database is created with `EnsureCreated`, not migrations. Run `docker compose down -v`, or delete `inventory.db`. |
| Demo products do not appear | Seeding only runs on an empty database. Wipe the data and set `SEED_DEMO=true`. |
| Logged out after refreshing the page | Known limitation, see below. |
| CSV does not download | Check that `js/download.js` loads (browser developer tools, Network tab) and that pop-up or download blocking is off. |
| Excel shows garbled characters in CSV | Open the file with File > Open or Data > From Text, and choose UTF-8. The file has a byte-order mark, so normal double-click opening should work. |

## Known limitations

- The sign-in is kept in memory for the browser tab. Refreshing the page signs you out. Cookie-based authentication is the planned fix.
- Users cannot be managed in the interface; accounts come from the seeder.
- SQLite is a single-file database. It is fine for a demo and a small team; PostgreSQL would be the choice for production.
- The database is created with `EnsureCreated`; there are no migrations yet.
- Dashboard state is per browser session, not shared between users in real time.
- No browser end-to-end tests.

## Roadmap

- Cookie authentication and a user management page.
- Live updates of the low-stock list with SignalR.
- PostgreSQL and EF Core migrations.
- Browser end-to-end tests, for example with Playwright.
- Purchase and sales documents, batch numbers and expiry dates.

## Development workflow

- `main` always builds. Work happens on `feature/...` branches and enters `main` through a pull request reviewed by the other student.
- Commit prefixes: `feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`.
- Definition of done: it builds, tests pass, `docker compose up --build` works, the other student reviewed it.
- To add a new stock operation: add a pure rule in `Logic/`, add a partial file `InventoryService.<Name>.cs` that finds the data, calls the rule, changes stock, adds a transaction and calls the shared `SaveAsync`, then add a tab or form and tests.

## Documentation index

| File | Content |
|---|---|
| `docs/architecture.md` | Layers, key decisions, entity diagram, algorithms |
| `docs/user-guide.md` | How to use each screen |
| `docs/qa-checklist.md` | Manual test checklist |
| `docs/testing.md` | Test groups, commands, results |
| `docs/criteria.md` | How the project covers the 20 evaluation criteria |
| `COMMITS.md` | Commit plan for the two students |

## Sources and credits

All code, tests and documentation in this repository were written by the team. We looked at three public projects for ideas only and did not copy code from them:

- Inventory-System/Inventory-Management-System: split into layers, a separate tests project, separate inbound and outbound flows.
- mandeep-sps/Blazor_WareHouseManager: role-based administration, docker-compose start, seeded data.
- Njabulo240/InventorySystem: a reports page and a default administrator account.

The functional programming patterns (`Result<T>` with `Bind` and `Map`, pure functions, LINQ) come from the course assignments.

Team: Alexandra Koziy and Zhannel Malikajdar. Course project; no open-source license has been chosen.
