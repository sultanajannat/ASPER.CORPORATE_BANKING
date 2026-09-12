# ASPER.CORPORATE_BANKING — Transaction Approval Matrix
## Backend Design Document (v1.0)

---

## 1. Architecture Overview

Two microservices are involved:

| Service | Owns | Talks to CORPORATE_BANKING via |
|---|---|---|
| **ASPER.AUTH** | Users, Roles, User-Role mapping, Authentication (JWT issuance) | gRPC server (exposed) |
| **ASPER.CORPORATE_BANKING** | Transaction types, File templates, Approval Matrix config, Batch upload, Workflow engine, Audit | gRPC client (consumer) |

**Golden rule:** CORPORATE_BANKING never owns user/role master data. It only *references* roles by `AuthRoleId` + a **snapshot of the role name** at the time the reference was made. This is critical for audit integrity — if someone renames role "A" to "Junior Approver" six months from now, a transaction approved last month should still show what it showed then.

**Two ways role data flows in, don't confuse them:**

1. **Design-time (Admin configuring the matrix):** Live gRPC call to AUTH → `GetAllRoles()` → populate dropdowns. No caching needed here, low traffic, must be fresh.
2. **Run-time (a user trying to act as Checker/Approver A/B/C/D):** Don't call gRPC on every approval click — that's chatty and a latency/availability risk on your critical path. Instead, **bake the user's roles into the JWT claims at login** (AUTH already does this presumably). CORPORATE_BANKING authorizes off the JWT claim, and only falls back to a gRPC `ValidateUserHasRole(userId, roleId)` call if you want a second, stronger check for high-value transactions (recommended for the top slabs with ANY-groups or long approval chains).

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

## 2. Core Design Insight — Make the Matrix Fully Generic

Your example has 5 slabs, and the last one is qualitatively different (ANY-of-3 followed by a mandatory D). If you model each slab as "N required roles," you'll hit a wall the day admin wants a 6th slab with a different shape. So model it one level more abstractly:

> **A Slab = an ordered list of Steps. A Step is either (a) exactly ONE mandatory role, or (b) a group of roles with ANY logic (first one to approve satisfies the step).**

**Sequence applies to every step, always — not just to the ANY-then-D case.** When a slab needs more than one mandatory approver, that is never "one step with two roles approving in parallel." It is **two sequential steps**, each with one role, and the admin explicitly orders them. So if admin configures a slab's two approvers as C then A, C must approve first; only then does it move to A; only when A approves is the transaction complete.

- Slab (5,001–50,000): 1 step → Roles=[A]
- Slab (50,001–100,000): 2 steps, in the order admin sets, e.g. →
  - Step 1: Role=[C]
  - Step 2: Role=[A]
  *(C approves first → moves to A → A approves → done. If admin instead ordered A then C, A would go first.)*
- Slab (100,001–1,000,000): 2 steps, same pattern → Step 1: Role=[A], Step 2: Role=[C] (or whatever order admin sets)
- Slab (1,000,001–10,000,000): 2 steps →
  - Step 1: Logic=ANY → Roles=[A, B, C]  *(any one of the three clears this step)*
  - Step 2: Role=[D]

A step never needs more than one role **unless** it's an ANY step — because "multiple roles, all mandatory" is precisely what sequential single-role steps already express, with the added benefit that the order is explicit and enforced rather than implicit/parallel. This single structure covers every case in your spec *and* any future combination the admin dreams up, with zero code changes — it's pure configuration.

---

## 3. Database Schema — Entity Classes

Convention, matching your `DPSEncashment` style exactly:
- camelCase property names, `[Table("Name")]` attribute, extends `BaseEntity`.
- Every FK is a plain `int` (or `int?` if optional) id property **plus** a single-object navigation property of the related type — e.g. `customerId` / `Customer customer`. Never `ICollection<T>` on either side. This keeps every entity's own dependency graph one level deep and avoids EF Core lazy-loading surprises and circular JSON serialization issues later — if you need "all records in a batch," query `TransactionRecord` filtered by `transactionBatchId`, don't navigate a collection off `TransactionBatch`.
- Where the FK target lives in **ASPER.AUTH** (roles), there is **no navigation property at all** — just the `int` id plus a snapshot `string` name, since that table isn't in this database and can't be joined to. Role identity/freshness for those is resolved through gRPC, not EF.
- Status/type/logic fields follow your `encashmentType`/`creditACType` pattern: plain `string` with an inline comment listing the allowed values, not a C# enum — keeps it consistent with the rest of the codebase and trivial to extend from config without a recompile.

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

        public int? isApproved { get; set; } = 0;      // config-change maker-checker, see §8
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

        public int stepOrder { get; set; }             // 1, 2, 3... enforced strictly — a later step cannot be acted on until every earlier step is satisfied
        public string approvalLogic { get; set; }      // SINGLE (exactly one role, that role must approve), ANY (multiple roles, first one to approve satisfies the step)
    }
}
```

> **Rule:** when `approvalLogic = SINGLE`, this step must have exactly **one** `ApprovalStepRole` row. If a slab needs two or more mandatory approvers, do **not** attach multiple roles to one SINGLE step — create multiple sequential steps instead (one role each), and let the admin order them (e.g. C at `stepOrder=1`, A at `stepOrder=2`). This is what makes ordering between approvers explicit and enforced instead of implicit/parallel. Only `ANY` steps are allowed to carry more than one role.

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
        public byte[] rowVersion { get; set; }           // concurrency token, see §6
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

> This table is the single source of truth for "current hand / last action" on the admin dashboard — no separate holder table needed; derive it per §7. Its own FK/navigation pattern (`transactionRecordId` + `transactionRecord`) is the one exception worth flagging: because it's append-only and queried in bulk ("give me every action for this record"), always query it filtered by `transactionRecordId` — don't be tempted to add a `List<TransactionApprovalAction>` back on `TransactionRecord` just for convenience.

> **Validation rule to enforce on save (application layer, not DB):** slabs within one `ApprovalMatrixConfig` must not overlap and must not leave unintended gaps; exactly one slab may have `maxAmount = null` (the top, open-ended one).

---

## 4. Upload & Validation Flow (Maker side)

1. Maker selects TransactionType → system fetches active `FileTemplateConfig` + `FileFieldMapping`.
2. Excel parsed row by row against the mapping: mandatory fields, regex, data types, duplicate `InstructionRefNo` within file and against DB.
3. Create `TransactionBatch` (Status = Validating → Uploaded/ValidationFailed).
4. For each valid row, **resolve the slab**: fetch the TransactionType's active `ApprovalMatrixConfig`, find the slab where `MinAmount <= Amount <= (MaxAmount ?? ∞)`. Stamp `MatrixSlabId`, `CheckerRequiredSnapshot`, and set initial `Status`/`CurrentStepOrder` per the routing table below.
5. Batch and all valid records are persisted in **one DB transaction**. Invalid rows are reported back to the maker (don't silently drop them — surface a downloadable error report).

**Initial routing per record:**

| Condition | Initial Status | CurrentStepOrder |
|---|---|---|
| Slab.IsAutoApprove = true | `AutoApproved` → immediately queued for processing | null |
| Slab.IsCheckerRequired = true | `PendingCheck` | null |
| Slab.IsCheckerRequired = false | `PendingApproval` | 1 |

---

## 5. Workflow Engine (the actual logic you asked for)

This is the part worth getting right — implement it as a single stateless domain service, e.g. `ApprovalWorkflowEngine`, so it's unit-testable independent of controllers.

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

### 5.2 Approver action — SINGLE/ANY, always sequential
```
Approve(transactionRecordId, userId, roleId, remarks):
    validate record.Status == PendingApproval
    step = GetStep(record.MatrixSlabId, record.CurrentStepOrder)
    validate roleId is in step.ApprovalStepRoles   (eligible for THIS step, and only this step)
    validate no existing Approved action by this roleId for this record+step  (idempotency — no double approval)

    write TransactionApprovalAction(ActionType=Approved, StepOrder=record.CurrentStepOrder)

    if step.ApprovalLogic == ANY:
        stepSatisfied = true                       // any one of the group is enough
    else:  // SINGLE — exactly one role was ever attached to this step, and it just approved
        stepSatisfied = true

    if stepSatisfied:
        nextStep = GetStep(record.MatrixSlabId, record.CurrentStepOrder + 1)
        if nextStep exists:
            record.CurrentStepOrder += 1            // move to next step — the NEXT approver's queue only becomes visible now
        else:
            record.Status = Approved                // all steps done
            enqueue for downstream processing (§8)

    save (within transaction, with concurrency check — see §6)
```

A **rejection at any step** is terminal: `Status = Rejected`, notify the maker, no further movement.

There is no "check if every role in this step has approved" branch anymore, because a SINGLE step only ever has one role by construction (§3 rule). This is what actually enforces sequence for multi-approver slabs: approver #2's item never appears in their queue at all until `CurrentStepOrder` reaches their step — there's no window where two approvers can both act "in parallel" on the same requirement.

### 5.3 Why this generic engine handles your exact spec
- Slabs 2–4 each become a small chain of SINGLE steps in the order the admin configured (e.g. C then A) — the second approver simply cannot act until the first one has.
- Slab 5: Step 1 (ANY of A/B/C) resolves the moment *any one* of them approves — the other two never need to act, and the engine automatically advances `CurrentStepOrder` to 2. Step 2 (SINGLE, role D) only becomes visible once step 1 is satisfied.

---

## 6. Concurrency Control

Since a SINGLE step now has exactly one eligible role, the classic "two approvers click Approve on the same ALL-step simultaneously" race is gone by design — there is only ever one role that can legally act on the current step at any moment. The remaining risks worth guarding against:
- **Optimistic concurrency** (`RowVersion` on `TransactionRecord`) — protects against the same user double-submitting (double-click, retry) or a checker and an approver racing on the same record.
- Wrap the read-evaluate-write sequence in step 5.2 in a single DB transaction so "advance step" and "write the audit log row" are atomic.
- Enforce the idempotency check at the **unique index** level too, not just application logic: unique index on `(TransactionRecordId, StepOrder, ActionByRoleId, ActionType)` where ActionType=Approved.
- For an ANY step with multiple roles, two people could still both click "Approve" at nearly the same instant — that's fine and expected (either one wins, the other's action is simply logged as a no-op/rejected-as-already-satisfied); no data corruption results because `CurrentStepOrder` only advances once.

---

## 7. Admin Monitoring — Query Pattern

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

"Currently in which hand" = if `Status = PendingCheck` → CheckerRole; if `PendingApproval` → the `ApprovalStepRole`(s) of `CurrentStepOrder` — a single role for a SINGLE step, or the whole group for an ANY step (since any one of them clears it). Compute this in the API layer, not SQL, it's cleaner.

Add an SLA/aging column (`MinutesInCurrentHand`) — you'll want this for ops monitoring almost immediately.

---

## 8. A few things worth adding that you didn't ask for but will need

- **Config changes should themselves be maker-checker.** A junior admin editing the approval matrix is itself a high-risk action — consider a lightweight two-eyes approval on `ApprovalMatrixConfig` publishing (one admin drafts, another activates). Cheap to add now, painful to retrofit.
- **Outbox pattern for downstream processing.** When a record reaches `Approved`, don't call the core banking/payment rail synchronously inside the approval API call — write an outbox row in the same DB transaction and let a background worker publish it. Keeps your approval action fast and crash-safe.
- **Duplicate-file protection.** Hash the uploaded file (or check `InstructionRefNo` + amount + account combos) so a maker can't accidentally re-upload the same batch twice.
- **Notifications.** When a record enters `PendingCheck` or `PendingApproval`, look up who holds that role via `AuthService.GetUsersByRole` and fire a notification (email/SMS/in-app) — nice UX win, not core logic.
- **Reject-with-return-to-maker vs hard reject.** You may eventually want "send back to maker for correction" as distinct from a terminal rejection — worth a `Returned` status now even if unused initially, cheaper than a migration later.

---

## 9. Suggested Frontend Pages (backend already exposes what these need)

| # | Page | Role | Backend endpoints it needs |
|---|---|---|---|
| 1 | Transaction Type Management | Admin | CRUD on TransactionType |
| 2 | File Template Configuration | Admin | CRUD on FileTemplateConfig/FileFieldMapping |
| 3 | Approval Matrix Configuration | Admin | CRUD on ApprovalMatrixConfig→Slab→Step→StepRole (this is your most complex UI — nested/dynamic form) |
| 4 | Bulk File Upload | Maker | Upload, validate, error report download |
| 5 | Checker Queue | Checker | List PendingCheck, Check & Forward / Reject |
| 6 | Approval Queue | Approver (A/B/C/D…) | List PendingApproval filtered to records where the user's role is eligible at `CurrentStepOrder`, Approve/Reject |
| 7 | Admin Monitoring Dashboard | Admin | The query in §7, filterable by type/status/date/amount |
| 8 | Transaction Detail / Audit Trail | Admin/all | Full `TransactionApprovalAction` history for one record |

---

## 10. Project / Folder Structure

Standard clean-architecture layering, matching a typical ASPER.* service:

```
ASPER.CORPORATE_BANKING/
├── ASPER.CORPORATE_BANKING.Domain/
│   ├── Entities/              (BaseEntity + all entities from §3)
│   └── Enums/                 (or plain string constants — keep consistent with rest of codebase)
├── ASPER.CORPORATE_BANKING.Application/
│   ├── Interfaces/            (IApprovalWorkflowEngine, IMatrixResolver, IFileValidationService, IAuthGrpcClient...)
│   ├── Services/              (implementations of the above)
│   ├── DTOs/                  (request/response models — never expose entities directly over the API)
│   └── Validators/            (matrix overlap/gap validation, file row validation)
├── ASPER.CORPORATE_BANKING.Infrastructure/
│   ├── Persistence/           (DbContext, EF configurations, migrations)
│   ├── Grpc/                  (generated AUTH client + thin wrapper service)
│   └── Outbox/                (outbox table + background publisher)
├── ASPER.CORPORATE_BANKING.API/
│   ├── Controllers/           (one per bounded concern — see §12)
│   └── Middleware/            (JWT role-claim authorization handlers)
└── ASPER.CORPORATE_BANKING.Tests/
    ├── Unit/                  (ApprovalWorkflowEngine — most important tests in the whole project)
    └── Integration/           (upload → routing → approve-to-completion, end to end)
```

Keep `ApprovalWorkflowEngine` in the Application layer with zero dependency on ASP.NET or EF directly — pass it repository/unit-of-work abstractions so it stays unit-testable without a database.

---

## 11. API Layer — Controller & Endpoint Guideline

Group endpoints by who uses them, not by table — this keeps role-based authorization attributes clean per controller.

**AdminConfigController** *(Admin only)*
| Method | Route | Purpose |
|---|---|---|
| GET/POST | `/api/transaction-types` | List / create transaction types |
| GET/POST | `/api/transaction-types/{id}/file-template` | View / publish a new FileTemplateConfig + mappings |
| GET/POST | `/api/transaction-types/{id}/approval-matrix` | View / publish a new ApprovalMatrixConfig (with nested Slabs→Steps→StepRoles in one payload) |
| GET | `/api/transaction-types/{id}/approval-matrix/history` | View past versions (audit) |

**MakerController** *(Maker role)*
| Method | Route | Purpose |
|---|---|---|
| POST | `/api/transactions/batches/upload` | Upload the excel file for a given TransactionType |
| GET | `/api/transactions/batches/{id}/validation-report` | Download row-level validation errors |
| GET | `/api/transactions/batches` | List maker's own uploaded batches + status |

**CheckerController** *(Checker role)*
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/transactions/checker-queue` | Records with Status=PendingCheck where caller holds the matching CheckerRoleId |
| POST | `/api/transactions/{id}/check-and-forward` | Approve/forward to first approval step |
| POST | `/api/transactions/{id}/check-reject` | Terminal reject |

**ApproverController** *(Approver roles A/B/C/D…)*
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/transactions/approval-queue` | Records at Status=PendingApproval where caller's role is eligible at `CurrentStepOrder` |
| POST | `/api/transactions/{id}/approve` | Runs §5.2 engine logic |
| POST | `/api/transactions/{id}/reject` | Terminal reject |

**AdminMonitoringController** *(Admin only)*
| Method | Route | Purpose |
|---|---|---|
| GET | `/api/admin/transactions` | The §7 query, filterable by type/status/date/amount/aging |
| GET | `/api/admin/transactions/{id}/audit-trail` | Full `TransactionApprovalAction` history for one record |

**Rule:** every controller action authorizes off the JWT role claim first (fast path); only the Approve/CheckAndForward actions optionally re-validate via the gRPC `ValidateUserHasRole` defense-in-depth call for high-value slabs.

---

## 12. `ApprovalWorkflowEngine` — Implementation Guideline

Interface shape (Application layer):

```
IApprovalWorkflowEngine
    Task<Result> CheckAndForward(int transactionRecordId, int userId, int roleId, string remarks)
    Task<Result> CheckReject(int transactionRecordId, int userId, int roleId, string remarks)
    Task<Result> Approve(int transactionRecordId, int userId, int roleId, string remarks)
    Task<Result> Reject(int transactionRecordId, int userId, int roleId, string remarks)
```

Guidelines, not code:
- Keep it **stateless** — every call loads the current `TransactionRecord` + its `ApprovalMatrixSlab`/`ApprovalStep`/`ApprovalStepRole` fresh, does its checks, writes one `TransactionApprovalAction`, updates the record, and returns. No engine-level caching of transaction state between calls.
- Every method should be a single unit-of-work: one DB transaction covering the read, the state check, the write, and the audit log insert — see §6 for why.
- Return a `Result` type (success/failure + reason), don't throw for expected business-rule failures (wrong role, wrong status, step mismatch) — reserve exceptions for actual infrastructure faults.
- The engine should never call the AUTH gRPC service itself for role membership — that's an authorization concern, resolved one layer above (in the controller/middleware) before the engine is invoked. The engine only trusts the `roleId` it's given and checks it against `ApprovalStepRole` rows.
- Unit test matrix to actually write (this is the most valuable test suite in the project):
  - Auto-approve slab → no engine calls needed, straight to Processed.
  - Checker-required=false → uploads land directly in PendingApproval at step 1.
  - Single-role SINGLE step happy path.
  - Two sequential SINGLE steps (e.g. C then A) — assert A's queue is empty until C approves.
  - ANY step with 3 roles — assert the two "losers" never need to act and the step advances on the first approval.
  - ANY-then-SINGLE(D) — the full slab-5 case end to end.
  - Rejection at each stage — assert terminal state and no further step movement.
  - Duplicate approval by the same role on the same step — assert it's rejected as idempotent no-op.

---

## 13. Excel Upload & Validation Service Guideline

Structure as its own service (`IFileIngestionService`), independent of the workflow engine:

1. **Parse** — read the workbook against the transaction type's active `FileFieldMapping` (column order + FieldKey mapping), not hardcoded column indexes, so template changes don't require code changes.
2. **Validate per row** — mandatory fields, regex, data type, `MaxLength`, plus cross-field checks (e.g. Amount > 0, valid currency code).
3. **Validate per batch** — duplicate `InstructionRefNo` within the file itself, and against existing DB rows (idempotency).
4. **Resolve slab per valid row** — look up the transaction type's active `ApprovalMatrixConfig`, find the slab where `MinAmount <= Amount <= (MaxAmount ?? decimal.MaxValue)`, stamp `MatrixSlabId` + `CheckerRequiredSnapshot`.
5. **Persist** — `TransactionBatch` + all valid `TransactionRecord` rows in one DB transaction; invalid rows go into a downloadable error report (row number + reason), never silently dropped.
6. Keep the raw uploaded file (`fileStoragePath`) even after processing — re-audit and dispute resolution will need it.

Recommend a well-known Excel library (EPPlus, ClosedXML, or NPOI depending on your licensing constraints) — don't hand-roll parsing.

---

## 14. gRPC Client Integration Guideline (calling ASPER.AUTH)

- Wrap the generated gRPC client behind your own interface (`IAuthGrpcClient`) in Infrastructure — never let controllers or the workflow engine reference the generated proto client directly. This lets you swap transport or add caching/retry without touching business code.
- `GetAllRoles` — call live on the Approval Matrix Config admin screen; short in-memory cache (a few minutes) is reasonable since role lists change rarely.
- `GetMakerCheckerUsers` — use for populating "who's in this queue" on admin monitoring and for notifications; not on the hot approval path.
- Add resilience: timeout + retry (e.g. Polly) around every gRPC call, and a sane fallback (e.g. serve from last-cached role list) if AUTH is briefly unavailable — a matrix config screen shouldn't be totally unusable because of a transient gRPC blip.
- Never let a gRPC failure block an in-flight Approve/CheckAndForward call if you're relying on JWT claims as the primary authorization source (per §1) — that's precisely why the JWT-first design matters operationally.

---

## 15. Outbox & Downstream Processing Guideline

- When a `TransactionRecord` reaches `Approved` (or `AutoApproved`), write an `OutboxMessage` row (TransactionRecordId, Payload, Status=Pending, CreatedAt) **in the same DB transaction** as the status update — this guarantees you never lose a transaction to a crash between "marked approved" and "sent downstream."
- A separate background worker (hosted service / scheduled job) polls `OutboxMessage` for `Status=Pending`, publishes to the actual payment rail / core banking integration, and marks `Sent` or `Failed` with retry count + backoff.
- On final success from the downstream system, update `TransactionRecord.Status = Processed` and stamp `ExternalSystemRefNo`; on terminal failure, `Status = Failed` with `FailureReason`, and surface it on the admin dashboard for manual intervention.
- Don't call the payment rail synchronously inside the `Approve` API request — keeps that endpoint fast and avoids partial-failure states.

---

## 16. Testing Strategy Summary

| Layer | What to test | Priority |
|---|---|---|
| `ApprovalWorkflowEngine` | All scenarios listed in §13 | **Highest** — this is the core business logic |
| Matrix config validation | Overlap/gap detection, exactly-one-role-per-SINGLE-step rule | High |
| File ingestion | Malformed rows, duplicate InstructionRefNo, slab resolution boundaries (exact min/max amounts) | High |
| Concurrency | Simulated race on the same record from two requests | Medium — write once, rarely re-run manually |
| Integration (end to end) | Upload → checker → approver chain → Approved → outbox → Processed, for at least one slab of each shape | Medium |
| Admin monitoring queries | Correct "current holder" derivation for each status | Low-medium |

---

## 17. Build Order (Backend)

1. TransactionType + FileTemplateConfig + FileFieldMapping (simple CRUD, low risk, gets EF/gRPC plumbing working end to end)
2. gRPC client wiring to AUTH (role lookups) + JWT claim-based authorization middleware
3. ApprovalMatrixConfig → Slab → Step → StepRole CRUD + the overlap/gap validation rule
4. Excel upload + validation + slab resolution (TransactionBatch/TransactionRecord creation)
5. `ApprovalWorkflowEngine` (Checker action, Approver action) — write this with unit tests covering a single-role SINGLE step, a multi-step sequential chain (e.g. C then A), and the ANY-then-SINGLE(D) case first, before wiring controllers
6. Concurrency hardening (RowVersion, unique indexes)
7. Admin monitoring query/API
8. Outbox + downstream processing worker
9. Notifications (optional, last)

---


*This design keeps the matrix itself as pure data (Slab→Step→StepRole), so every rule change you listed — including ones not yet imagined — is a config edit, not a deployment.*




























Corporate Banking API Directory
Here is the complete catalog of all REST API endpoints built into the Corporate Banking microservice, grouped by the role that consumes them. All endpoints require a valid JWT Bearer Token ([Authorize]).

1. Admin Configurations (/api/admin-config)
These endpoints are used by system administrators to define the rules of the system before any files can be uploaded.

Method	Endpoint	Purpose
POST	/api/admin-config/transaction-types	Creates a new root Transaction Type (e.g., "BEFTN", "RTGS", "Payroll").
POST	/api/admin-config/transaction-types/{id}/file-template	Binds an Excel/CSV column mapping template to a specific transaction type so the Maker engine knows how to parse uploaded files.
POST	/api/admin-config/transaction-types/{id}/approval-matrix	Submits the complex multi-level Approval Matrix (Slabs, Steps, and StepRoles). The engine validates overlapping slabs and handles automatic versioning.
2. Maker Operations (/api/maker)
These endpoints are used by Corporate Makers (data entry users) to upload transaction files.

Method	Endpoint	Purpose
POST	/api/maker/transactions/batches/upload	Accepts an IFormFile (e.g., .xlsx). The engine dynamically parses the file against the Admin's template, performs mandatory/regex/duplicate validation, resolves the amount against the Approval Matrix, and saves valid rows into the database safely wrapped in a transaction.
3. Checker Operations (/api/checker)
These endpoints are used by Checkers (first-level verifiers).

Method	Endpoint	Purpose
GET	/api/checker/transactions/pending	Fetches a list of all transactions whose status is strictly PendingCheck.
POST	/api/checker/transactions/{id}/action	Submits a WorkflowActionRequest (Approved = true/false, Remarks). If true, it pushes the status to PendingApproval. If false, it pushes to Rejected. Natively catches DbUpdateConcurrencyException to block double-checks.
4. Approver Operations (/api/approver)
These endpoints are used by Approvers (multi-level finalizers).

Method	Endpoint	Purpose
GET	/api/approver/transactions/pending	Fetches a list of all transactions whose status is strictly PendingApproval.
POST	/api/approver/transactions/{id}/action	Submits an Approve/Reject action. The engine calculates the current stepOrder. If the final step is reached, the status becomes Approved and it atomically inserts a payload into the AuditOutboxMessage table for downstream processing.
5. Admin Monitoring Dashboard (/api/admin/monitoring)
These endpoints give administrators live visibility into the health and blockages of the system.

Method	Endpoint	Purpose
GET	/api/admin/monitoring/pending-transactions	Returns all stuck transactions (PendingCheck or PendingApproval). It dynamically calculates the SLA AgingInMinutes and looks up the exact RoleName holding up the process.
GET	/api/admin/monitoring/transactions/{id}/audit-trail	Returns the absolute, immutable chronological history of every Check/Approve/Reject action taken against a specific transaction.
