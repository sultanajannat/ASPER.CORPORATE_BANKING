# ProductBuilder Creation and Update Workflow (ASPER.CORPORATE_BANKING)

This document details the complete step-by-step technical workflow, inter-service communication, and business logic that occurs when a DPS `ProductBuilder` is created, updated, and approved.

---

## 1. Creation / Initialization (Maker)
**Endpoint**: `POST /api/Product/SaveProductBuilders`
**Controller**: `ProductController.cs`

When a bank administrator (the Maker) submits a new DPS product from the frontend, the following sequence occurs:

### Step 1.1: Fetch Business Date via gRPC
Before saving, the DPS service needs the current active banking business date.
- **Action**: Calls `_masterDataConfigService.BusinessDateAsync()`
- **Inter-service Communication**: Makes a **gRPC call** to the `ASPER.Config` microservice (`MasterDataConfigProtoService`).

### Step 1.2: Entity Construction & Persistence
- A new `ProductBuilders` entity is constructed.
- Crucially, it is saved in a **Pending** state:
  - `status = false`
  - `productStatusId = 2` (Pending)
  - `IsProductDistributed = 0`, `IsGLMapped = 0`
- **Action**: Saved to the local PostgreSQL database using Entity Framework (`_productService.SaveProductBuilders()`).

### Step 1.3: Dynamic Approval Matrix Generation
The system enforces a Maker-Checker workflow.
- **Action**: Queries the `ApprovalMatrices` and `Approvers` tables to find the active routing rules for the day.
- **Logic**: It generates a chain of `ProductApprovalLog` records:
  - **Creator**: Logged as completed.
  - **First Forwarder/Approver**: Marked as `IsActive = true` (it is now their turn).
  - **Subsequent Approvers**: Marked as `IsActive = false`.
- A historical trail is written to `ProductApprovalLogHistory` with the status `SUBMITTED`.

### Step 1.4: Push Notification (RabbitMQ / MassTransit)
The system notifies the next approver in the chain that a product requires their attention.
- **Action**: Queries the mobile number/username of the `IsActive` approver.
- **Inter-service Communication**: Publishes a `SendPushNotification` event using **MassTransit** to the RabbitMQ exchange.
- **Consumer**: The `ASPER.Notification` microservice listens to this event and pushes the alert to the approver's device.

### Step 1.5: Saving Child Entities
- The system proceeds to save related configurations like `ProductFixedAmount`, `DpsTenure`, and `DpsTenureRateConfig` to the database.

---

## 2. Approval Workflow (Checker)
**Endpoint**: `GET /api/Product/AproveDPSProduct`
**Controller**: `ProductController.cs`

When an approver logs in and clicks "Approve", the request hits the `AproveDPSProduct` endpoint.

### Step 2.1: Logging the Action
- The system finds the current user's active `ProductApprovalLog`.
- It records their action, updates their log to `IsActive = false`, and inserts a new `ProductApprovalLogHistory` indicating they approved or forwarded it.

### Step 2.2: Intermediate Approver vs Final Approver Logic

#### Scenario A: Intermediate Approver (Forwarding)
If the user is NOT the final approver:
1. The next person in the `ProductApprovalLog` chain is set to `IsActive = true`.
2. **Inter-service Communication**: A `SendPushNotification` event is published to RabbitMQ (MassTransit) to alert the *next* approver.

#### Scenario B: Final Approver (Activation)
If the user IS the final approver:
1. The `ProductBuilders` entity is fully activated:
   - `status = true`
   - `productStatusId = 3` (Approved)
2. **Inter-service Communication (gRPC to GL Process)**:
   - The DPS system must ensure that the accounting ledgers for this new product exist in the core ledger system.
   - **Action**: Calls `_glGrpcClient.AutoCreateLedgerAndConfigAsync(glRequest)`.
   - **Target**: Sends a gRPC request to the `ASPER.GLProcess` microservice.
   - If the GL microservice responds with `Success = true`, the DPS system updates `product.IsGLMapped = 1` and `product.IsVoucherConfigured = true`.
3. **Inter-service Communication (Notification)**:
   - Publishes a `SendPushNotification` event to RabbitMQ to inform the original Creator that their product is now live.

---

## 3. Product Updates
**Endpoint**: `POST /api/Product/UpdateProductBuilders`

Updating a product follows a similar stringent workflow. Once a product is Approved (`productStatusId = 4` or active), modifying it does not simply overwrite the live data.
- The existing active product is usually cloned or marked as inactive, and a new `ProductBuilders` record is generated in a Pending state.
- The exact same Maker-Checker routing (Step 1.3) and Push Notifications (Step 1.4) begin anew. The update does not take effect in the live environment until the final approver signs off.

## Summary of Microservice Interactions
1. **ASPER.CORPORATE_BANKING $\rightarrow$ ASPER.Config**: gRPC call to fetch business dates.
2. **ASPER.CORPORATE_BANKING $\rightarrow$ ASPER.Notification**: RabbitMQ (MassTransit) events to send approval alerts.
3. **ASPER.CORPORATE_BANKING $\rightarrow$ ASPER.GLProcess**: gRPC call to automatically scaffold accounting ledgers once a product is fully approved.

