using System;
using System.Collections.Generic;

namespace ASPER.CORPORATE_BANKING.Application.DTOs
{
    public class TransactionMonitoringResult
    {
        public int TransactionRecordId { get; set; }
        public string InstructionRefNo { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime? LastActionAt { get; set; }
        public double AgingInMinutes { get; set; }
        public string CurrentlyPendingWithRoles { get; set; }
    }

    public class TransactionAuditResult
    {
        public int ActionId { get; set; }
        public string ActionType { get; set; }
        public int? ActionByUserId { get; set; }
        public string ActionByUserName { get; set; }
        public string ActionByRoleId { get; set; }
        public string ActionByRoleName { get; set; }
        public string Remarks { get; set; }
        public DateTime ActionAt { get; set; }
    }
}
