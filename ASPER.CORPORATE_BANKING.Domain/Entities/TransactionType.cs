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
        public bool? isActive { get; set; } = true;
    }
}
