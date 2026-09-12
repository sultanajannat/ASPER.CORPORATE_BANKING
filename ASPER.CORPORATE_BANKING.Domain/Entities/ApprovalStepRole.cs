using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("ApprovalStepRole")]
    public class ApprovalStepRole : BaseEntity, IAuditableEntity
    {
        public int? approvalStepId { get; set; }
        public ApprovalStep approvalStep { get; set; }

        // from ASPER.AUTH via gRPC - snapshot only, no local FK/navigation possible
        public int? roleId { get; set; }
        public string roleName { get; set; }
    }
}
