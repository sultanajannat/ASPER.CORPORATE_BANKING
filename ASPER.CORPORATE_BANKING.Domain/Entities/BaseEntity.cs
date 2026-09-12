using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    public class BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        [DefaultValue(0)]
        [Column("IS_DELETE")]
        public int? isDelete { get; set; } = 0;
        
        [Column("CREATED_AT")]
        public DateTime? createdAt { get; set; }
        
        [Column("UPDATED_AT")]
        public DateTime? updatedAt { get; set; }
        
        [MaxLength(250)]
        [Column("CREATED_BY")]
        public string createdBy { get; set; }
        
        [MaxLength(250)]
        [Column("UPDATED_BY")]
        public string updatedBy { get; set; }
    }
}
