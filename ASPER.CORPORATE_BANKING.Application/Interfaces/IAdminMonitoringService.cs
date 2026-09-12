using System.Collections.Generic;
using System.Threading.Tasks;
using ASPER.CORPORATE_BANKING.Application.DTOs;

namespace ASPER.CORPORATE_BANKING.Application.Interfaces
{
    public interface IAdminMonitoringService
    {
        Task<ResultDto> GetPendingTransactionsAsync();
        Task<ResultDto> GetTransactionAuditAsync(int transactionRecordId);
    }
}
