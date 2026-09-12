using System;

namespace ASPER.CORPORATE_BANKING.Application.DTOs
{
    public class WorkflowActionRequest
    {
        public int RoleId { get; set; }
        public bool IsApproved { get; set; }
        public string Remarks { get; set; }
    }
}
