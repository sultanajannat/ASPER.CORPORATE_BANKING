using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("CUSTOMER")]
    public class Customer : BaseEntity, IAuditableEntity
    {
        [Column("CUSTOMER_CODE")]
        public string CustomerRCode { get; set; }

        [Column("CUSTOMER_NAME_ENG")]
        public string CustomerNameEng { get; set; }

        [Column("FATHER_NAME_ENG")]
        public string FatherNameEng { get; set; }

        [Column("MOTHER_NAME_ENG")]
        public string MotherNameEng { get; set; }

        [Column("DATE_OF_BIRTH")]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string MobileNo { get; set; }

        [Column("EMAIL")]
        public string Email { get; set; }

        [Column("NID_NO")]
        public string NidNo { get; set; }

        [Column("MARITAL_STATUS")]
        public string MaritalStatus { get; set; }

        public string username { get; set; }

        public string? channelName { get; set; }
        public string? TIN { get; set; }
        public DateTime? LastTINUpdatedAt { get; set; }
        public bool? IsTINapproved { get; set; } = false;
    }
}
