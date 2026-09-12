using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    // Append-only audit trail - never update a row, only insert.
    [Table("TransactionApprovalAction")]
    public class TransactionApprovalAction : BaseEntity
    {
        public int? transactionRecordId { get; set; }
        public TransactionRecord transactionRecord { get; set; }

        public string actionType { get; set; }   // Checked, Forwarded, Approved, Rejected, Returned, AutoApproved, AutoForwarded, Processed, Failed
        public int? stepOrder { get; set; }      // null for checker actions

        public int? actionByUserId { get; set; }
        public string actionByUserName { get; set; }

        // snapshot from ASPER.AUTH - which role they acted as (a user may hold more than one eligible role)
        public int? actionByRoleId { get; set; }
        public string actionByRoleName { get; set; }

        public DateTime? actionAt { get; set; }  // DateTime.UtcNow
        public string remarks { get; set; }
        public string ipAddress { get; set; }
    }
}
