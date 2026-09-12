using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("ApprovalMatrixSlab")]
    public class ApprovalMatrixSlab : BaseEntity, IAuditableEntity
    {
        public int? approvalMatrixConfigId { get; set; }
        public ApprovalMatrixConfig approvalMatrixConfig { get; set; }

        public int? slabOrder { get; set; }             // display order; ranges validated for no overlap/gap on save
        public decimal? minAmount { get; set; }         // inclusive
        public decimal? maxAmount { get; set; }        // null = open-ended (top slab only)

        public bool? isAutoApprove { get; set; } = false;
        public bool? isCheckerRequired { get; set; } = true;

        // snapshot from ASPER.AUTH via gRPC - no local FK/navigation, that table lives in another service
        public int? checkerRoleId { get; set; }
        public string checkerRoleName { get; set; }

        public bool? isActive { get; set; } = true;
    }
}
