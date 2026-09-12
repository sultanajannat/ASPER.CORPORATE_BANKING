using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("ApprovalStep")]
    public class ApprovalStep : BaseEntity, IAuditableEntity
    {
        public int? approvalMatrixSlabId { get; set; }
        public ApprovalMatrixSlab approvalMatrixSlab { get; set; }

        public int? stepOrder { get; set; }             // 1, 2, 3... sequence enforced by the workflow engine
        public string approvalLogic { get; set; }      // ALL, ANY
    }
}
