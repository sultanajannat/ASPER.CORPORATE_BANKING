using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("FileTemplateConfig")]
    public class FileTemplateConfig : BaseEntity, IAuditableEntity
    {
        public int? transactionTypeId { get; set; }
        public TransactionType transactionType { get; set; }

        public int? version { get; set; }
        public DateTime? effectiveFrom { get; set; }
        public DateTime? effectiveTo { get; set; }     // null = currently active
        public bool isActive { get; set; } = true;
    }
}
