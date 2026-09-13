using System;

namespace ASPER.CORPORATE_BANKING.Application.DTOs
{
    public class WorkflowActionRequest
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsApproved { get; set; }
        public string Remarks { get; set; }
    }
}
