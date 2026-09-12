using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("TransactionBatch")]
    public class TransactionBatch : BaseEntity
    {
        public Guid batchNo { get; set; } = Guid.NewGuid();   // external-safe reference

        public int? transactionTypeId { get; set; }
        public TransactionType transactionType { get; set; }

        public int? approvalMatrixConfigId { get; set; }       // snapshot: config version active at upload time
        public ApprovalMatrixConfig approvalMatrixConfig { get; set; }

        public string fileName { get; set; }
        public string fileStoragePath { get; set; }           // raw file kept for re-audit

        public int? uploadedByUserId { get; set; }
        public string uploadedByUserName { get; set; }
        public DateTime? uploadedAt { get; set; }

        public int? totalRecords { get; set; }
        public int? validRecords { get; set; }
        public int? invalidRecords { get; set; }
        public decimal? totalAmount { get; set; }
        public string validationErrors { get; set; } // JSON string of row-level errors

        public string status { get; set; }   // Uploaded, Validating, ValidationFailed, InProgress, PartiallyCompleted, Completed, Rejected, Cancelled
    }
}
