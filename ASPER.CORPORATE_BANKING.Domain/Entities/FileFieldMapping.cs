using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASPER.CORPORATE_BANKING.Domain.Entities
{
    [Table("FileFieldMapping")]
    public class FileFieldMapping : BaseEntity
    {
        public int? fileTemplateConfigId { get; set; }
        public FileTemplateConfig fileTemplateConfig { get; set; }

        public int? columnOrder { get; set; }
        public string excelColumnName { get; set; }
        public string fieldKey { get; set; }           // BANK_AC_NO, AC_HOLDER_NAME, ROUTING_NO, AMOUNT, ...
        public string dataType { get; set; }           // string, number, date
        public bool? isMandatory { get; set; } = true;
        public string validationRegex { get; set; }
        public int? maxLength { get; set; }
    }
}
