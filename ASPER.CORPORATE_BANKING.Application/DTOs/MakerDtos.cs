using System;
using System.Collections.Generic;

namespace ASPER.CORPORATE_BANKING.Application.DTOs
{
    public class UploadBatchResponse
    {
        public Guid BatchNo { get; set; }
        public int TotalRecords { get; set; }
        public int ValidRecords { get; set; }
        public int InvalidRecords { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}
