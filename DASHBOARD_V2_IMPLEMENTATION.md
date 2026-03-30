# Dashboard Analytics V2 Implementation Summary

## Overview
Created a new V2 dashboard analytics endpoint that provides 12-month historical data for two distinct dashboard types: **General Dashboard** and **VATTable Dashboard**.

## Files Created

### 1. Domain Enum
**File**: `src/Core/EInvoiceIntegrator.Domain/Enums/DashboardType.cs`
- Defines dashboard types: `General = 1` and `VATTable = 2`

### 2. DTOs
**File**: `src/Core/EInvoiceIntegrator.Application/Features/DashboardAnalytics/DTOs/DashboardAnalyticsV2Dto.cs`

#### Main Response DTO:
- `DashboardAnalyticsV2Dto` - Contains either GeneralDashboard or VATTableDashboard based on request

#### General Dashboard DTOs:
- `GeneralDashboardDto` - Contains metrics and 4 chart datasets
- `InvoiceSummaryMetricsDto` - Top-level metrics including:
  - Total Customer/Vendor Invoices (count and amount)
  - Total VAT on Vendor/Customer Invoices
  - Total Invoice Value
  - Percentage changes (week-over-week)
- `SalesVsPurchasesMonthlyDto` - 12-month sales vs purchases comparison
- `VATTrendAnalysisMonthlyDto` - 12-month Input VAT vs Output VAT trends
- `SalesAndPaymentMonthlyDto` - 12-month sales and payment tracking
- `SalesPerRegionMonthlyDto` - 12-month sales breakdown by region (state)

#### VATTable Dashboard DTOs (No top-level metrics):
- `VATTableDashboardDto` - Contains only chart datasets
- `VATTableByCurrencyMonthlyDto` - 12-month VATable amounts by currency (NGN, USD, GBP)
- `ExemptVATTableByCurrencyMonthlyDto` - 12-month exempt VAT amounts by currency
- `VATTableVsNonVATTableMonthlyDto` - 12-month comparison of vatable vs non-vatable for sales and purchases
- `VATTableVsNonVATTableTrendDto` - 12-month trend with percentage changes

### 3. Query
**File**: `src/Core/EInvoiceIntegrator.Application/Features/DashboardAnalytics/Queries/GetDashboardAnalyticsV2Query.cs`
- Simple query record with `DashboardType` parameter

### 4. Query Handler
**File**: `src/Core/EInvoiceIntegrator.Application/Features/DashboardAnalytics/Queries/GetDashboardAnalyticsV2QueryHandler.cs`

#### Key Features:
- **Security**: Respects user permissions (Platform Admin sees all, Business users see only their data)
- **12-Month Data**: All calculations based on last 12 months from current date
- **Dashboard Type Switching**: Routes to appropriate calculation based on dashboard type

#### General Dashboard Calculations:
1. **Metrics**: Aggregates invoice counts, amounts, VAT totals with percentage changes
2. **Sales vs Purchases**: Monthly comparison of outgoing invoices (sales) vs received invoices (purchases)
3. **VAT Trend**: Monthly Input VAT (from purchases) vs Output VAT (from sales)
4. **Sales & Payment**: Monthly sales vs actual payments received
5. **Sales per Region**: Monthly sales broken down by customer state/region

#### VATTable Dashboard Calculations:
1. **VATTable by Currency**: Monthly VATable amounts grouped by currency (NGN, USD, GBP)
2. **Exempt VATTable**: Monthly tax-exempt amounts by currency
3. **VATTable vs Non-VATTable**: Monthly breakdown showing:
   - Sales Vatable/Non-vatable
   - Purchase Vatable/Non-vatable
4. **Trend Analysis**: Monthly trend showing vatable vs non-vatable totals with percentage changes

#### VAT Determination Logic:
- **Vatable Items**: Items with `TaxCategory.Percent > 0`
- **Non-Vatable Items**: Items with `TaxCategory.Percent = 0`
- **Customer Invoices (Sales)**: VAT calculated from line items using tax category percentage
- **Vendor Invoices (Purchases)**: VAT taken from `ReceivedInvoice.TotalTaxAmount`

### 5. Controller Endpoint
**File**: `src/Presentation/EInvoiceIntegrator.API/Controllers/BusinessController.cs`

#### Endpoint Details:
- **Route**: `GET /api/v{version}/Business/dashboard-analytics-v2`
- **Parameter**: `dashboardType` (query parameter, defaults to `General`)
- **Authorization**: Requires `KPMGAdmin`, `ClientAdmin`, or `ClientUser` role
- **Response**: Returns appropriate dashboard data based on type

#### Example Requests:
```http
GET /api/v1/Business/dashboard-analytics-v2?dashboardType=1  // General Dashboard
GET /api/v1/Business/dashboard-analytics-v2?dashboardType=2  // VATTable Dashboard
```

## Data Flow

1. **Request** ? Controller receives `DashboardType` enum
2. **Query** ? Creates `GetDashboardAnalyticsV2Query` with dashboard type
3. **Handler** ? 
   - Fetches last 12 months of invoices and received invoices
   - Applies security filters (business-specific or all)
   - Routes to appropriate calculation method
   - Returns populated DTO
4. **Response** ? Returns dashboard data with 12-month historical information

## Key Design Decisions

### 1. Separate Dashboard Types
- **General Dashboard**: Full metrics + multiple chart types (aligned with first UI design)
- **VATTable Dashboard**: Only VAT-related breakdowns (aligned with second UI design highlighting)

### 2. 12-Month Rolling Window
- Always calculates from 11 months ago to current month
- Ensures consistent historical data regardless of when API is called

### 3. Currency Breakdown
- Supports NGN (Naira), USD (Dollar), and GBP (Pounds)
- Extensible to add more currencies if needed

### 4. Region Grouping
- Uses `Party.Address.State` for regional breakdown
- Groups by state to show geographical sales distribution

### 5. VAT Calculations
- **Output VAT** (from sales): Calculated from line items using tax category percentage
- **Input VAT** (from purchases): Uses pre-calculated total from received invoices

## Response Structure Examples

### General Dashboard Response:
```json
{
  "generalDashboard": {
    "metrics": {
      "totalCustomerInvoicesCount": 30,
      "totalCustomerInvoicesAmount": 700000000,
      "totalVendorInvoicesCount": 25,
      "totalVendorInvoicesAmount": 350000000,
      "totalVATOnVendorInvoices": 1800000,
      "totalVATOnCustomerInvoices": 3400000,
      "totalInvoiceValue": 45200000,
      "vatOnVendorPercentageChange": -4.0,
      "vatOnCustomerPercentageChange": -4.0,
      "totalInvoiceValuePercentageChange": 12.0
    },
    "salesVsPurchases": [
      {
        "year": 2024,
        "month": 1,
        "monthName": "Jan",
        "salesAmount": 14200000,
        "purchasesAmount": 8500000
      }
      // ... 11 more months
    ],
    "vatTrendAnalysis": [...],
    "salesAndPaymentPerMonth": [...],
    "salesPerRegion": [...]
  }
}
```

### VATTable Dashboard Response:
```json
{
  "vatTableDashboard": {
    "vatTableByCurrency": [
      {
        "year": 2024,
        "month": 1,
        "monthName": "Jan",
        "nairaAmount": 5000000,
        "dollarAmount": 12000,
        "poundsAmount": 8000
      }
      // ... 11 more months
    ],
    "exemptVATTableByCurrency": [...],
    "vatTableVsNonVATTableSalesAndPurchase": [...],
    "vatTableVsNonVATTableTrend": [...]
  }
}
```

## Testing Recommendations

1. **Test both dashboard types** to ensure proper data routing
2. **Verify 12-month data** is correctly calculated and ordered
3. **Test with different user roles** to ensure security filters work
4. **Verify currency grouping** for multi-currency invoices
5. **Test edge cases**: 
   - No invoices in the system
   - Only vatable or only non-vatable items
   - Single currency vs multi-currency scenarios
   - Different regions/states

## Future Enhancements

1. Add date range parameters for custom time periods
2. Add filtering by specific regions
3. Add export functionality (CSV, Excel)
4. Add caching for improved performance
5. Add pagination for large datasets
6. Add more currencies as needed
