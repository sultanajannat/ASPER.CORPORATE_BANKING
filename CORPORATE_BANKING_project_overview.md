# ASPER.CORPORATE_BANKING Project Overview

This document provides a comprehensive overview of the **ASPER.CORPORATE_BANKING** (Deposit Pension Scheme) project, which is part of the broader ASPER microservices ecosystem. It outlines the project's primary purpose, the business logic encapsulated within its components, and the end-to-end operational workflow.

## 1. Purpose of the Project
The **ASPER.CORPORATE_BANKING** project is a dedicated microservice responsible for managing the complete lifecycle of Deposit Pension Scheme (DPS) products for a financial institution. A DPS allows customers to deposit a fixed amount of money periodically (usually monthly) over a specified tenure, earning interest that is paid out upon maturity. 

This project handles everything from defining the dynamic rules and slabs for DPS products to customer account creation, periodic payment collection (including Direct Debit), daily interest calculation (COB - Close of Business), and the final encashment (both mature and premature) including the application of necessary taxes and excise duties.

## 2. Architecture & Tech Stack
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, Contract, API).
- **Communication**: Inter-service communication is handled heavily via **gRPC** (integrating with `CBSCore`, `AuthAPI`, `GLProcess`, `Config`, `DEPOSIT`, etc.). Event-driven processing is handled via **MassTransit/RabbitMQ** (evidenced by the `Consumers` folder).
- **Background Jobs**: **Quartz.NET** is used for scheduled tasks like daily COB (Close of Business) processing and Direct Debit triggers.
- **Database**: Entity Framework Core with **PostgreSQL** (Npgsql).
- **Document Generation**: Uses `DinkToPdf` / `wkhtmltox` for generating reports/receipts.

## 3. Business Logic and Core Components
The system is divided into several business areas, identifiable by the entities and controllers.

### A. Product Engine & Configuration
Files: `ProductController`, `TenureController`, `SlabController`, `PrematureEncashRateController`
- **Logic**: Allows administrators to define flexible DPS products (`ProductBuilders`). A product is defined by its available tenures (`DpsTenure`), interest calculation rules, premature encashment rates, and slabs. 
- **Approval Workflow**: A Maker-Checker mechanism (`ApprovalMatrix`, `ProductApprovalLog`) ensures that changes to high-risk product configurations are peer-reviewed before becoming active.

### B. Account Management
Files: `DPSAccountController`, `DPSController`
- **Logic**: Handles the onboarding of a customer into a specific DPS product (`CustomerAccount`, `DPSCollection`). It maps the customer to the product rules and generates a schedule for expected installment payments (`DPSSchedules`).

### C. Collection & Direct Debit (Auto-Debit)
Files: `DPSPaymentController`, `DirectDebitController`, `DirectDebitSimulationController`
- **Logic**: Collects periodic installments from customers. The `DirectDebitController` automates this by pulling funds from a customer's linked savings/current account (`DirectDebitAccount`, `AutoDebitStatsViewModel`) at a scheduled time. 

### D. COB (Close of Business) & End of Day Processing
Files: `COBProcessController`, `DailyInterestProcess`
- **Logic**: At the end of each business day, the COB job calculates the accrued interest for all active DPS accounts based on the product rules. It also checks for missed payments, applies penalties if configured, and calculates monthly provisions.

### E. Encashment (Maturity & Premature)
Files: `DpsEncashmentController`, `FundTransferSimulationController`
- **Logic**: Handles the closing of a DPS account (`DPSEncashment`).
  - **Maturity**: If the customer completes the tenure, the principal and accrued interest are paid out fully.
  - **Premature**: If the customer breaks the DPS early, the system calculates the payout using premature encashment rates and slabs (`PrematureEncashAmountSlab`, `PrematureEncashTenureSlab`). The interest rate applied is typically significantly lower.

### F. Taxes and Excise Duty
Files: `TaxController`, `ExciseDutyController`
- **Logic**: Adheres to government regulations by applying taxes (`TaxKey`, `TaxDocumentListItemDto`) on the earned interest and deducting excise duties based on the balance size over the year (`ExciseDuty`, `ExciseDutyDetails`).

### G. General Ledger (GL) Mapping & Integration
Files: `GLMappingController`
- **Logic**: Every financial transaction (collection, interest accrual, encashment, tax deduction) needs to be recorded in the bank's general ledger. The DPS system maps specific transaction types to GL accounts and uses gRPC (`AccountingIntegrationServiceClient`, `DpsEncashmentVoucherIntegrationServiceClient`) to post vouchers to the `ASPER.GLProcess` microservice.

### H. Reporting
Files: `DPSReportApiController`, `DPSReportController`
- **Logic**: Provides dashboards and detailed reports on active DPS portfolios, collections, auto-debit statistics, and daily business KPIs (`DPSDashboardSummaryVm`, `KpiViewModel`).

## 4. Standard Operational Workflow

1. **Product Setup**:
   - The Bank Admin (Maker) defines a new DPS Product (e.g., "Monthly High Yield DPS") with 3, 5, and 10-year tenures. 
   - They attach penalty rules, interest calculation rules, tax rules, and premature encashment rates.
   - Another Admin (Checker) approves the product.
2. **Account Opening**:
   - A customer approaches the bank (or uses the digital app). 
   - A `CustomerAccount` is opened and linked to the DPS product.
   - A payment schedule is generated, and a source account is linked for Direct Debit.
3. **Daily Operations (Collections & Accruals)**:
   - **Collections**: On the specified date, the Quartz job triggers the Direct Debit service to deduct the installment from the source account. 
   - **EOD / COB**: At the end of the day, the COB process runs, calculates daily accrued interest, updates the `DailyAccountBalance`, and posts integration logs to the General Ledger.
4. **Encashment**:
   - Once the tenure is reached, the system calculates the final payout (Principal + Interest - Taxes).
   - The funds are transferred to the customer's linked savings account, and the appropriate GL vouchers are generated via gRPC to the core accounting system.

