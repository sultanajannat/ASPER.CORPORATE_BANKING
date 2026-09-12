using System;
using System.Collections.Generic;

namespace ASPER.CORPORATE_BANKING.Application.DTOs
{
    public class CreateTransactionTypeRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CreateFileTemplateRequest
    {
        public List<FileFieldMappingRequest> Mappings { get; set; }
    }

    public class FileFieldMappingRequest
    {
        public int ColumnOrder { get; set; }
        public string ExcelColumnName { get; set; }
        public string FieldKey { get; set; }
        public string DataType { get; set; }
        public bool IsMandatory { get; set; }
        public string ValidationRegex { get; set; }
        public int? MaxLength { get; set; }
    }

    public class PublishApprovalMatrixRequest
    {
        public List<ApprovalMatrixSlabRequest> Slabs { get; set; }
    }

    public class ApprovalMatrixSlabRequest
    {
        public int SlabOrder { get; set; }
        public decimal MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public bool IsAutoApprove { get; set; }
        public bool IsCheckerRequired { get; set; }
        public int? CheckerRoleId { get; set; }
        public string CheckerRoleName { get; set; }
        public List<ApprovalStepRequest> Steps { get; set; }
    }

    public class ApprovalStepRequest
    {
        public int StepOrder { get; set; }
        public string ApprovalLogic { get; set; } // "SINGLE" or "ANY"
        public List<ApprovalStepRoleRequest> StepRoles { get; set; }
    }

    public class ApprovalStepRoleRequest
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
    }
}
