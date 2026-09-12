using System.Collections.Generic;
using System.Threading.Tasks;
using ASPER.CORPORATE_BANKING.Application.DTOs;

namespace ASPER.CORPORATE_BANKING.Application.Interfaces
{
    public interface IAdminMonitoringService
    {
        Task<ResultDto> GetPendingTransactionsAsync();
        Task<ResultDto> GetTransactionAuditAsync(int transactionRecordId);
        Task<ResultDto> GetTransactionDetailAsync(int transactionRecordId);
        Task<ResultDto> GetAllBatchesAsync();
        Task<ResultDto> GetTransactionsAsync(string status, int? typeId, DateTime? dateFrom, DateTime? dateTo, decimal? minAmount, decimal? maxAmount);
    }
}



