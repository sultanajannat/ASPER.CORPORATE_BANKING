# ASPER.CORPORATE_BANKING Audit Trail System Architecture & Flow

## Overview
The Audit Trail system in this project is designed to automatically capture and record changes made to specific entities within the database. It employs modern enterprise patterns, including the **Interceptor Pattern** for seamless data capture and the **Transactional Outbox Pattern** to guarantee reliable delivery of audit events to an external system (e.g., RabbitMQ).

---

## 1. How is it working? (The Flow)

The flow of the audit trail operates in a multi-step process, ensuring data consistency and decoupling the audit process from the core business transaction.

### Step 1: Entity Interception
When an application saves changes to the database (calling `DbContext.SaveChanges()`), Entity Framework Core triggers the **`AuditInterceptor`**. This interceptor hooks into the EF Core pipeline.

### Step 2: Change Tracking & Filtering
The interceptor inspects the `ChangeTracker`. It specifically filters for entities that:
1. Implement the **`IAuditableEntity`** marker interface.
2. Are in an `Added` (Create), `Modified` (Update), or `Deleted` (Delete) state.

### Step 3: Event Construction
For each matched entity, the `AuditEventBuilder` constructs an `AuditEvent`. It iterates over the entity's properties and captures:
- **Old Values**: The original state before modification (for updates and deletes).
- **New Values**: The current state being saved (for creates and updates).
- **Changed Columns**: A list of specifically modified property names.
- **Contextual Data**: Information injected via `IAuditContext` (CorrelationId, User/PerformedBy, IPAddress, DeviceInfo, BranchCode, ServiceName).

### Step 4: The Outbox Pattern (Database Save)
Instead of publishing the audit event directly to a message broker (which could fail and cause inconsistencies), the interceptor serializes the `AuditEvent` and creates an **`AuditOutboxMessage`** with a status of `"Pending"`. 
This outbox message is added to the *same DbContext transaction* as the original business entity change. This guarantees that the business data and the audit data are committed to the database atomically.

### Step 5: Background Processing
A background service, **`AuditPublisherWorker`**, continuously polls the database (every 5 seconds) for `AuditOutboxMessage` records that are `"Pending"` or `"Failed"` (with a retry limit).
It picks up these messages, deserializes the payload, and delegates it to an `IAuditPublisher` (e.g., `RabbitMqAuditPublisher`) to send it to the external audit service/queue. Upon successful publish, it marks the message as `"Processed"`.

---

## 2. Where is it working?

The Audit Trail infrastructure is primarily located in the `ASPER.CORPORATE_BANKING.Infrastructure` layer:
- **`ASPER.CORPORATE_BANKING.Infrastructure.Audit.Interceptors.AuditInterceptor`**: The core interceptor logic.
- **`ASPER.CORPORATE_BANKING.Domain.Entities.IAuditableEntity`**: The marker interface used to tag entities for auditing.
- **`ASPER.CORPORATE_BANKING.Infrastructure.Audit.Services.AuditEventBuilder`**: The logic that extracts old/new values.
- **`ASPER.CORPORATE_BANKING.Infrastructure.Audit.Services.AuditPublisherWorker`**: The background polling service.
- **`ASPER.CORPORATE_BANKING.Infrastructure.Audit.Outbox.AuditOutboxMessage`**: The entity storing the serialized events.

---

## 3. When is it working?

The audit trail triggers **automatically whenever a transactional save operation (`SaveChanges` or `SaveChangesAsync`) is executed** on the `DbContext`, provided that the entities being saved implement `IAuditableEntity`. 

The actual publishing to the message broker happens **asynchronously** in the background, shortly after the database transaction completes (typically within 5 seconds, driven by the `AuditPublisherWorker` loop).

---

## 4. Why is it working this way? (Business Logic & Rationale)

- **Decoupling (Why the interface?):** Using the `IAuditableEntity` interface ensures the core Domain layer doesn't need to know *how* auditing works. It simply marks entities that require it.
- **Consistency (Why Interceptors?):** By using EF Core interceptors, developers don't have to manually write audit logs in every Application service or Repository. It is centralized, reducing code duplication and human error.
- **Reliability (Why the Outbox Pattern?):** If the system tried to write to RabbitMQ synchronously during the HTTP request and RabbitMQ was down, the business transaction would fail. By writing the audit log to a local database table (`AuditOutboxMessage`) first, the system achieves "Eventual Consistency" and guarantees no audit logs are lost due to network or broker outages.

---

## 5. Specific Analysis: The `ProductBuilders` Entity

### Code Reference
```csharp
[Table("PRODUCT_BUILDER")]
public class ProductBuilders : BaseEntity, IAuditableEntity 
{ 
    // ... properties ... 
}
```

### How Audit Works for `ProductBuilders`
Because `ProductBuilders` inherits from `IAuditableEntity`, it is automatically opted into the audit system.

- **Creation**: When a new `ProductBuilders` record is added to the system and saved, the `AuditInterceptor` captures the `Added` state. It records all non-null properties as `NewValues`, sets the ActionType to `Create`, and Severity to `Medium`.
- **Modification**: If a user updates the `InterestRateMax` or `status` of an existing product, the interceptor captures the `Modified` state. It records *only the changed columns* (e.g., `["InterestRateMax", "status"]`), storing the original values in `OldValues` and the updated values in `NewValues`. ActionType is `Update`, Severity is `Low`.
- **Deletion**: If a product is removed (assuming hard delete), the interceptor captures the `Deleted` state. It records the state before deletion into `OldValues`, ActionType is `Delete`, and Severity is `High`.

All these changes, along with *who* changed the product (`PerformedBy`), *when*, and from which *IP Address*, are saved to the outbox and eventually published to the auditing service.

