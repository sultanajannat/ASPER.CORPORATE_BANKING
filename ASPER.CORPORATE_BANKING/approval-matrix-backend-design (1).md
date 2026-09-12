# ASPER.CORPORATE_BANKING â€” Transaction Approval Matrix
## Backend Design Document (v1.0)

---

## 1. Architecture Overview

Two microservices are involved:

| Service | Owns | Talks to CORPORATE_BANKING via |
|---|---|---|
| **ASPER.AUTH** | Users, Roles, User-Role mapping, Authentication (JWT issuance) | gRPC server (exposed) |
| **ASPER.CORPORATE_BANKING** | Transaction types, File templates, Approval Matrix config, Batch upload, Workflow engine, Audit | gRPC client (consumer) |

**Golden rule:** CORPORATE_BANKING never owns user/role master data. It only *references* roles by `AuthRoleId` + a **snapshot of the role name** at the time the reference was made. This is critical for audit integrity â€” if someone renames role "A" to "Junior Approver" six months from now, a transaction approved last month should still show what it showed then.

**Two ways role data flows in, don't confuse them:**

1. **Design-time (Admin configuring the matrix):** Live gRPC call to AUTH â†’ `GetAllRoles()` â†’ populate dropdowns. No caching needed here, low traffic, must be fresh.
2. **Run-time (a user trying to act as Checker/Approver A/B/C/D):** Don't call gRPC on every approval click â€” that's chatty and a latency/availability risk on your critical path. Instead, **bake the user's roles into the JWT claims at login** (AUTH already does this presumably). CORPORATE_BANKING authorizes off the JWT claim, and only falls back to a gRPC `ValidateUserHasRole(userId, roleId)` call if you want a second, stronger check for high-value transactions (recommended for the top slabs with ANY-groups or long approval chains).

Suggested gRPC contract from AUTH:

```protobuf
service AuthService {
  rpc GetAllRoles(Empty) returns (RoleListResponse);
  rpc GetRoleById(RoleRequest) returns (RoleResponse);
  rpc GetUsersByRole(RoleRequest) returns (UserListResponse);   // for "who is in the approver queue" / notifications
  rpc ValidateUserHasRole(UserRoleRequest) returns (BoolResponse); // defense-in-depth check
  rpc GetUserById(UserRequest) returns (UserResponse);
}
```

---

## 2. Core Design Insight â€” Make the Matrix Fully Generic

Your example has 5 slabs, and the last one is qualitatively different (ANY-of-3 followed by a mandatory D). If you model each slab as "N required roles," you'll hit a wall the day admin wants a 6th slab with a different shape. So model it one level more abstractly:

> **A Slab = an ordered list of Steps. A Step is either (a) exactly ONE mandatory role, or (b) a group of roles with ANY logic (first one to approve satisfies the step).**

**Sequence applies to every step, always â€” not just to the ANY-then-D case.** When a slab needs more than one mandatory approver, that is never "one step with two roles approving in parallel." It is **two sequential steps**, each with one role, and the admin explicitly orders them. So if admin configures a slab's two approvers as C then A, C must approve first; only then does it move to A; only when A approves is the transaction complete.

- Slab (5,001â€“50,000): 1 step â†’ Roles=[A]
- Slab (50,001â€“100,000): 2 steps, in the order admin sets, e.g. â†’
  - Step 1: Role=[C]
  - Step 2: Role=[A]
  *(C approves first â†’ moves to A â†’ A approves â†’ done. If admin instead ordered A then C, A would go first.)*
- Slab (100,001â€“1,000,000): 2 steps, same pattern â†’ Step 1: Role=[A], Step 2: Role=[C] (or whatever order admin sets)
- Slab (1,000,001â€“10,000,000): 2 steps â†’
  - Step 1: Logic=ANY â†’ Roles=[A, B, C]  *(any one of the three clears this step)*
  - Step 2: Role=[D]

A step never needs more than one role **unless** it's an ANY step â€” because "multiple roles, all mandatory" is precisely what sequential single-role steps already express, with the added benefit that the order is explicit and enforced rather than implicit/parallel. This single structure covers every case in your spec *and* any future combination the admin dreams up, with zero code changes â€” it's pure configuration.

---

## 3. Database Schema â€” Entity Classes

Convention, matching your `DPSEncashment` style exactly:
- camelCase property names, `[Table("Name")]` attribute, extends `BaseEntity`.
- Every FK is a plain `int` (or `int?` if optional) id property **plus** a single-object navigation property of the related type â€” e.g. `customerId` / `Customer customer`. Never `ICollection<T>` on either side. This keeps every entity's own dependency graph one level deep and avoids EF Core lazy-loading surprises and circular JSON serialization issues later â€” if you need "all records in a batch," query `TransactionRecord` filtered by `transactionBatchId`, don't navigate a collection off `TransactionBatch`.
- Where the FK target lives in **ASPER.AUTH** (roles), there is **no navigation property at all** â€” just the `int` id plus a snapshot `string` name, since that table isn't in this database and can't be joined to. Role identity/freshness for those is resolved through gRPC, not EF.
- Status/type/logic fields follow your `encashmentType`/`creditACType` pattern: plain `string` with an inline comment listing the allowed values, not a C# enum â€” keeps it consistent with the rest of the codebase and trivial to extend from config without a recompile.

### 3.1 Configuration / Master entities

```csharp
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("TransactionType")]
    public class TransactionType : BaseEntity
    {
        public string code { get; set; }              // BEFTN, RTGS, NPSB - extensible
        public string name { get; set; }
        public string description { get; set; }
        public bool isActive { get; set; } = true;
    }
}
```

```csharp
namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("FileTemplateConfig")]
    public class FileTemplateConfig : BaseEntity
    {
        public int transactionTypeId { get; set; }
        public TransactionType transactionType { get; set; }

        public int version { get; set; }
        public DateTime? effectiveFrom { get; set; }
        public DateTime? effectiveTo { get; set; }     // null = currently active
        public bool isActive { get; set; } = true;
    }
}
```

```csharp
namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("FileFieldMapping")]
    public class FileFieldMapping : BaseEntity
    {
        public int fileTemplateConfigId { get; set; }
        public FileTemplateConfig fileTemplateConfig { get; set; }

        public int columnOrder { get; set; }
        public string excelColumnName { get; set; }
        public string fieldKey { get; set; }           // BANK_AC_NO, AC_HOLDER_NAME, ROUTING_NO, AMOUNT, ...
        public string dataType { get; set; }           // string, number, date
        public bool isMandatory { get; set; } = true;
        public string validationRegex { get; set; }
        public int? maxLength { get; set; }
    }
}
```

```csharp
namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    // Never edit an active row in place - publish a new version and deactivate the old one.
    [Table("ApprovalMatrixConfig")]
    public class ApprovalMatrixConfig : BaseEntity
    {
        public int transactionTypeId { get; set; }
        public TransactionType transactionType { get; set; }

        public int version { get; set; }
        public DateTime? effectiveFrom { get; set; }
        public DateTime? effectiveTo { get; set; }
        public bool isActive { get; set; } = true;     // only one active per transactionTypeId

        public int? isApproved { get; set; } = 0;      // config-change maker-checker, see Â§8
        public int? approvedByUserId { get; set; }
        public DateTime? approvedDate { get; set; }
    }
}
```

```csharp
namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("ApprovalMatrixSlab")]
    public class ApprovalMatrixSlab : BaseEntity
    {
        public int approvalMatrixConfigId { get; set; }
        public ApprovalMatrixConfig approvalMatrixConfig { get; set; }

        public int slabOrder { get; set; }             // display order; ranges validated for no overlap/gap on save
        public decimal minAmount { get; set; }         // inclusive
        public decimal? maxAmount { get; set; }        // null = open-ended (top slab only)

        public bool isAutoApprove { get; set; } = false;
        public bool isCheckerRequired { get; set; } = true;

        // snapshot from ASPER.AUTH via gRPC - no local FK/navigation, that table lives in another service
        public int? checkerRoleId { get; set; }
        public string checkerRoleName { get; set; }

        public bool isActive { get; set; } = true;
    }
}
```

```csharp
namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("ApprovalStep")]
    public class ApprovalStep : BaseEntity
    {
        public int approvalMatrixSlabId { get; set; }
        public ApprovalMatrixSlab approvalMatrixSlab { get; set; }

        public int stepOrder { get; set; }             // 1, 2, 3... enforced strictly â€” a later step cannot be acted on until every earlier step is satisfied
        public string approvalLogic { get; set; }      // SINGLE (exactly one role, that role must approve), ANY (multiple roles, first one to approve satisfies the step)
    }
}
```

> **Rule:** when `approvalLogic = SINGLE`, this step must have exactly **one** `ApprovalStepRole` row. If a slab needs two or more mandatory approvers, do **not** attach multiple roles to one SINGLE step â€” create multiple sequential steps instead (one role each), and let the admin order them (e.g. C at `stepOrder=1`, A at `stepOrder=2`). This is what makes ordering between approvers explicit and enforced instead of implicit/parallel. Only `ANY` steps are allowed to carry more than one role.

```csharp
namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("ApprovalStepRole")]
    public class ApprovalStepRole : BaseEntity
    {
        public int approvalStepId { get; set; }
        public ApprovalStep approvalStep { get; set; }

        // from ASPER.AUTH via gRPC - snapshot only, no local FK/navigation possible
        public int roleId { get; set; }
        public string roleName { get; set; }
    }
}
```

### 3.2 Runtime / transactional entities

```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("TransactionBatch")]
    public class TransactionBatch : BaseEntity
    {
        public Guid batchNo { get; set; } = Guid.NewGuid();   // external-safe reference

        public int transactionTypeId { get; set; }
        public TransactionType transactionType { get; set; }

        public int approvalMatrixConfigId { get; set; }       // snapshot: config version active at upload time
        public ApprovalMatrixConfig approvalMatrixConfig { get; set; }

        public string fileName { get; set; }
        public string fileStoragePath { get; set; }           // raw file kept for re-audit

        public int uploadedByUserId { get; set; }
        public string uploadedByUserName { get; set; }
        public DateTime? uploadedAt { get; set; }

        public int? totalRecords { get; set; }
        public int? validRecords { get; set; }
        public int? invalidRecords { get; set; }
        public decimal? totalAmount { get; set; }

        public string status { get; set; }   // Uploaded, Validating, ValidationFailed, InProgress, PartiallyCompleted, Completed, Rejected, Cancelled
    }
}
```

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("TransactionRecord")]
    public class TransactionRecord : BaseEntity
    {
        public int transactionBatchId { get; set; }
        public TransactionBatch transactionBatch { get; set; }

        public int rowNo { get; set; }
        public string instructionRefNo { get; set; }   // unique - idempotency / duplicate detection

        public string bankAccountNo { get; set; }
        public string accountHolderName { get; set; }
        public string routingNumber { get; set; }

        public string beneficiaryBankName { get; set; }
        public string beneficiaryBranchName { get; set; }
        public string beneficiaryAccountType { get; set; }   // Savings, Current

        public string senderAccountNo { get; set; }
        public string senderAccountName { get; set; }

        public decimal amount { get; set; }
        public string currency { get; set; } = "BDT";
        public DateTime? valueDate { get; set; }
        public string purposeCode { get; set; }         // Bangladesh Bank purpose code for BEFTN/RTGS/NPSB
        public string priority { get; set; }            // Normal, Urgent - matters for RTGS
        public string narration { get; set; }

        public int matrixSlabId { get; set; }           // snapshot: which slab this row resolved into
        public ApprovalMatrixSlab matrixSlab { get; set; }

        public bool checkerRequiredSnapshot { get; set; }

        public string status { get; set; }              // PendingCheck, PendingApproval, Approved, Rejected, AutoApproved, Processed, Failed, Returned
        public int? currentStepOrder { get; set; }       // which ApprovalStep it is sitting at right now

        public string externalSystemRefNo { get; set; } // reference from the payment rail after processing
        public string failureReason { get; set; }

        [Timestamp]
        public byte[] rowVersion { get; set; }           // concurrency token, see Â§6
    }
}
```

```csharp
namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    // Append-only audit trail - never update a row, only insert.
    [Table("TransactionApprovalAction")]
    public class TransactionApprovalAction : BaseEntity
    {
        public int transactionRecordId { get; set; }
        public TransactionRecord transactionRecord { get; set; }

        public string actionType { get; set; }   // Checked, Forwarded, Approved, Rejected, Returned, AutoApproved, AutoForwarded, Processed, Failed
        public int? stepOrder { get; set; }      // null for checker actions

        public int actionByUserId { get; set; }
        public string actionByUserName { get; set; }

        // snapshot from ASPER.AUTH - which role they acted as (a user may hold more than one eligible role)
        public int actionByRoleId { get; set; }
        public string actionByRoleName { get; set; }

        public DateTime? actionAt { get; set; }  // DateTime.UtcNow
        public string remarks { get; set; }
        public string ipAddress { get; set; }
    }
}
```

> This table is the single source of truth for "current hand / last action" on the admin dashboard â€” no separate holder table needed; derive it per Â§7. Its own FK/navigation pattern (`transactionRecordId` + `transactionRecord`) is the one exception worth flagging: because it's append-only and queried in bulk ("give me every action for this record"), always query it filtered by `transactionRecordId` â€” don't be tempted to add a `List<TransactionApprovalAction>` back on `TransactionRecord` just for convenience.

> **Validation rule to enforce on save (application layer, not DB):** slabs within one `ApprovalMatrixConfig` must not overlap and must not leave unintended gaps; exactly one slab may have `maxAmount = null` (the top, open-ended one).

---

## 4. Upload & Validation Flow (Maker side)

1. Maker selects TransactionType â†’ system fetches active `FileTemplateConfig` + `FileFieldMapping`.
2. Excel parsed row by row against the mapping: mandatory fields, regex, data types, duplicate `InstructionRefNo` within file and against DB.
3. Create `TransactionBatch` (Status = Validating â†’ Uploaded/ValidationFailed).
4. For each valid row, **resolve the slab**: fetch the TransactionType's active `ApprovalMatrixConfig`, find the slab where `MinAmount <= Amount <= (MaxAmount ?? âˆž)`. Stamp `MatrixSlabId`, `CheckerRequiredSnapshot`, and set initial `Status`/`CurrentStepOrder` per the routing table below.
5. Batch and all valid records are persisted in **one DB transaction**. Invalid rows are reported back to the maker (don't silently drop them â€” surface a downloadable error report).

**Initial routing per record:**

| Condition | Initial Status | CurrentStepOrder |
|---|---|---|
| Slab.IsAutoApprove = true | `AutoApproved` â†’ immediately queued for processing | null |
| Slab.IsCheckerRequired = true | `PendingCheck` | null |
| Slab.IsCheckerRequired = false | `PendingApproval` | 1 |

---

## 5. Workflow Engine (the actual logic you asked for)

This is the part worth getting right â€” implement it as a single stateless domain service, e.g. `ApprovalWorkflowEngine`, so it's unit-testable independent of controllers.

### 5.1 Checker action
```
CheckAndForward(transactionRecordId, userId, roleId, remarks):
    validate record.Status == PendingCheck
    validate user holds roleId == record.MatrixSlab.CheckerRoleId  (JWT claim, +optional gRPC re-check)
    write TransactionApprovalAction(ActionType=Checked)
    record.Status = PendingApproval
    record.CurrentStepOrder = 1
    save (within transaction)
```
A checker **rejecting** simply sets `Status = Rejected`, terminal, and logs `ActionType=Rejected`.

### 5.2 Approver action â€” SINGLE/ANY, always sequential
```
Approve(transactionRecordId, userId, roleId, remarks):
    validate record.Status == PendingApproval
    step = GetStep(record.MatrixSlabId, record.CurrentStepOrder)
    validate roleId is in step.ApprovalStepRoles   (eligible for THIS step, and only this step)
    validate no existing Approved action by this roleId for this record+step  (idempotency â€” no double approval)

    write TransactionApprovalAction(ActionType=Approved, StepOrder=record.CurrentStepOrder)

    if step.ApprovalLogic == ANY:
        stepSatisfied = true                       // any one of the group is enough
    else:  // SINGLE â€” exactly one role was ever attached to this step, and it just approved
        stepSatisfied = true

    if stepSatisfied:
        nextStep = GetStep(record.MatrixSlabId, record.CurrentStepOrder + 1)
        if nextStep exists:
            record.CurrentStepOrder += 1            // move to next step â€” the NEXT approver's queue only becomes visible now
        else:
            record.Status = Approved                // all steps done
            enqueue for downstream processing (Â§8)

    save (within transaction, with concurrency check â€” see Â§6)
```

A **rejection at any step** is terminal: `Status = Rejected`, notify the maker, no further movement.

There is no "check if every role in this step has approved" branch anymore, because a SINGLE step only ever has one role by construction (Â§3 rule). This is what actually enforces sequence for multi-approver slabs: approver #2's item never appears in their queue at all until `CurrentStepOrder` reaches their step â€” there's no window where two approvers can both act "in parallel" on the same requirement.

### 5.3 Why this generic engine handles your exact spec
- Slabs 2â€“4 each become a small chain of SINGLE steps in the order the admin configured (e.g. C then A) â€” the second approver simply cannot act until the first one has.
- Slab 5: Step 1 (ANY of A/B/C) resolves the moment *any one* of them approves â€” the other two never need to act, and the engine automatically advances `CurrentStepOrder` to 2. Step 2 (SINGLE, role D) only becomes visible once step 1 is satisfied.

---

## 6. Concurrency Control

Since a SINGLE step now has exactly one eligible role, the classic "two approvers click Approve on the same ALL-step simultaneously" race is gone by design â€” there is only ever one role that can legally act on the current step at any moment. The remaining risks worth guarding against:
- **Optimistic concurrency** (`RowVersion` on `TransactionRecord`) â€” protects against the same user double-submitting (double-click, retry) or a checker and an approver racing on the same record.
- Wrap the read-evaluate-write sequence in step 5.2 in a single DB transaction so "advance step" and "write the audit log row" are atomic.
- Enforce the idempotency check at the **unique index** level too, not just application logic: unique index on `(TransactionRecordId, StepOrder, ActionByRoleId, ActionType)` where ActionType=Approved.
- For an ANY step with multiple roles, two people could still both click "Approve" at nearly the same instant â€” that's fine and expected (either one wins, the other's action is simply logged as a no-op/rejected-as-already-satisfied); no data corruption results because `CurrentStepOrder` only advances once.

---

## 7. Admin Monitoring â€” Query Pattern

You don't need a separate "current status" table; derive everything from `TransactionRecord` + latest `TransactionApprovalAction`:

```sql
SELECT
    tr.*,
    tb.BatchNo, tb.TransactionTypeId, tb.UploadedByUserName,
    la.ActionByUserName  AS LastActedBy,
    la.ActionByRoleName  AS LastActedAsRole,
    la.ActionAt          AS LastActionAt,
    DATEDIFF(MINUTE, la.ActionAt, GETUTCDATE()) AS MinutesInCurrentHand
FROM TransactionRecord tr
JOIN TransactionBatch tb ON tb.Id = tr.TransactionBatchId
OUTER APPLY (
    SELECT TOP 1 * FROM TransactionApprovalAction
    WHERE TransactionRecordId = tr.Id
    ORDER BY ActionAt DESC
) la
WHERE tr.Status NOT IN ('Processed','Rejected','Failed')  -- or remove filter for full history view
```

"Currently in which hand" = if `Status = PendingCheck` â†’ CheckerRole; if `PendingApproval` â†’ the `ApprovalStepRole`(s) of `CurrentStepOrder` â€” a single role for a SINGLE step, or the whole group for an ANY step (since any one of them clears it). Compute this in the API layer, not SQL, it's cleaner.

Add an SLA/aging column (`MinutesInCurrentHand`) â€” you'll want this for ops monitoring almost immediately.

---

## 8. A few things worth adding that you didn't ask for but will need

- **Config changes should themselves be maker-checker.** A junior admin editing the approval matrix is itself a high-risk action â€” consider a lightweight two-eyes approval on `ApprovalMatrixConfig` publishing (one admin drafts, another activates). Cheap to add now, painful to retrofit.
- **Outbox pattern for downstream processing.** When a record reaches `Approved`, don't call the core banking/payment rail synchronously inside the approval API call â€” write an outbox row in the same DB transaction and let a background worker publish it. Keeps your approval action fast and crash-safe.
- **Duplicate-file protection.** Hash the uploaded file (or check `InstructionRefNo` + amount + account combos) so a maker can't accidentally re-upload the same batch twice.
- **Notifications.** When a record enters `PendingCheck` or `PendingApproval`, look up who holds that role via `AuthService.GetUsersByRole` and fire a notification (email/SMS/in-app) â€” nice UX win, not core logic.
- **Reject-with-return-to-maker vs hard reject.** You may eventually want "send back to maker for correction" as distinct from a terminal rejection â€” worth a `Returned` status now even if unused initially, cheaper than a migration later.

---

## 9. Suggested Frontend Pages (backend already exposes what these need)

| # | Page | Role | Backend endpoints it needs |
|---|---|---|---|
| 1 | Transaction Type Management | Admin | CRUD on TransactionType |
| 2 | File Template Configuration | Admin | CRUD on FileTemplateConfig/FileFieldMapping |
| 3 | Approval Matrix Configuration | Admin | CRUD on ApprovalMatrixConfigâ†’Slabâ†’Stepâ†’StepRole (this is your most complex UI â€” nested/dynamic form) |
| 4 | Bulk File Upload | Maker | Upload, validate, error report download |
| 5 | Checker Queue | Checker | List PendingCheck, Check & Forward / Reject |
| 6 | Approval Queue | Approver (A/B/C/Dâ€¦) | List PendingApproval filtered to records where the user's role is eligible at `CurrentStepOrder`, Approve/Reject |
| 7 | Admin Monitoring Dashboard | Admin | The query in Â§7, filterable by type/status/date/amount |
| 8 | Transaction Detail / Audit Trail | Admin/all | Full `TransactionApprovalAction` history for one record |

---

## 10. Build Order (Backend)

1. TransactionType + FileTemplateConfig + FileFieldMapping (simple CRUD, low risk, gets EF/gRPC plumbing working end to end)
2. gRPC client wiring to AUTH (role lookups) + JWT claim-based authorization middleware
3. ApprovalMatrixConfig â†’ Slab â†’ Step â†’ StepRole CRUD + the overlap/gap validation rule
4. Excel upload + validation + slab resolution (TransactionBatch/TransactionRecord creation)
5. `ApprovalWorkflowEngine` (Checker action, Approver action) â€” write this with unit tests covering a single-role SINGLE step, a multi-step sequential chain (e.g. C then A), and the ANY-then-SINGLE(D) case first, before wiring controllers
6. Concurrency hardening (RowVersion, unique indexes)
7. Admin monitoring query/API
8. Outbox + downstream processing worker
9. Notifications (optional, last)

---

*This design keeps the matrix itself as pure data (Slabâ†’Stepâ†’StepRole), so every rule change you listed â€” including ones not yet imagined â€” is a config edit, not a deployment.*
