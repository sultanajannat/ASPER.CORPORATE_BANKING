using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Infrastructure.Audit.Outbox
{
    [Table("AuditOutboxMessage")]
    public class AuditOutboxMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [Required]
        public string EventType { get; set; }
        
        [Required]
        public string Payload { get; set; }
        
        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Processed, Failed
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        
        public int RetryCount { get; set; } = 0;
        public string Error { get; set; }
    }
}
