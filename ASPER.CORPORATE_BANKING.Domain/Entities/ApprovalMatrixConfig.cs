using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    // Never edit an active row in place - publish a new version and deactivate the old one.
    [Table("ApprovalMatrixConfig")]
    public class ApprovalMatrixConfig : BaseEntity
    {
        public int? transactionTypeId { get; set; }
        public TransactionType transactionType { get; set; }

        public int? version { get; set; }
        public DateTime? effectiveFrom { get; set; }
        public DateTime? effectiveTo { get; set; }
        public bool isActive { get; set; } = true;     // only one active per transactionTypeId

        public int? isApproved { get; set; } = 0;      // config-change maker-checker, see §8
        public int? approvedByUserId { get; set; }
        public DateTime? approvedDate { get; set; }
    }
}
