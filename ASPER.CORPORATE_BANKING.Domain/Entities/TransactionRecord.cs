using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("TransactionRecord")]
    public class TransactionRecord : BaseEntity
    {
        public int? transactionBatchId { get; set; }
        public TransactionBatch transactionBatch { get; set; }

        public int? rowNo { get; set; }
        public string instructionRefNo { get; set; }   // unique - idempotency / duplicate detection

        public string bankAccountNo { get; set; }
        public string accountHolderName { get; set; }
        public string routingNumber { get; set; }

        public string beneficiaryBankName { get; set; }
        public string beneficiaryBranchName { get; set; }
        public string beneficiaryAccountType { get; set; }   // Savings, Current

        public string senderAccountNo { get; set; }
        public string senderAccountName { get; set; }

        public decimal? amount { get; set; }
        public string currency { get; set; } = "BDT";
        public DateTime? valueDate { get; set; }
        public string purposeCode { get; set; }         // Bangladesh Bank purpose code for BEFTN/RTGS/NPSB
        public string priority { get; set; }            // Normal, Urgent - matters for RTGS
        public string narration { get; set; }

        public int? matrixSlabId { get; set; }           // snapshot: which slab this row resolved into
        public ApprovalMatrixSlab matrixSlab { get; set; }

        public bool? checkerRequiredSnapshot { get; set; }

        public string status { get; set; }              // PendingCheck, PendingApproval, Approved, Rejected, AutoApproved, Processed, Failed, Returned
        public int? currentStepOrder { get; set; }       // which ApprovalStep it is sitting at right now

        public string externalSystemRefNo { get; set; } // reference from the payment rail after processing
        public string failureReason { get; set; }

        [Timestamp]
        public byte[] rowVersion { get; set; }           // concurrency token, see §6
    }
}
