# User guide

Sign in with a demo account (see README). The menu shows Dashboard, Warehouse, History and, for managers and administrators, Reports.

## Dashboard
Products, units in stock, bins in use, number of products below minimum, and the five biggest shortages. Refreshes after every operation.

## Warehouse
One page with four sections. Click **+** to open a section and **-** to close it. A section loads its data when first opened.

- **Operations** tabs: Receive, Ship, Transfer, WriteOff (managers and administrators), Scan.
  - Receive: barcode and quantity; the bin is chosen automatically.
  - Ship: barcode and quantity; stock is taken from the smallest quantities first.
  - Transfer: barcode, source bin, target bin, quantity.
  - WriteOff: barcode, bin, quantity and a reason.
  - Scan: barcode and Enter; shows the total and the bins that hold the product.
- **Stock**: quantity per bin, 10 rows per page.
- **Products**: search by name, SKU or barcode; administrators add, edit and disable. Barcode is 8 to 14 digits.
- **Bins**: capacity and fill bar; administrators add bins.

## History
Every operation with date (UTC), type, SKU, quantity, bins, user and reason. Filter by type; 15 rows per page.

## Reports (manager, administrator)
Low stock, Bin fill and Movements (30 days). Each is a section with paging; **Download CSV** saves the whole report.

## Messages
Green means success, red means an error. Click a message to close it; it also disappears after 4 seconds.
