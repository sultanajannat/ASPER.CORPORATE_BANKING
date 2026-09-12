using System;

namespace ASPER.CORPORATE_BANKING.Application.DTOs
{
    public class ApprovedTransactionIntegrationEvent
    {
        public int TransactionRecordId { get; set; }
        public string InstructionRefNo { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string BeneficiaryAccount { get; set; }
        public string SenderAccount { get; set; }
        public string RoutingNumber { get; set; }
        public string PurposeCode { get; set; }
    }
}
