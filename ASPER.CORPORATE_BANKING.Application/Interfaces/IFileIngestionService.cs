using System.IO;
using System.Threading.Tasks;
using ASPER.CORPORATE_BANKING.Application.DTOs;

namespace ASPER.CORPORATE_BANKING.Application.Interfaces
{
    public interface IFileIngestionService
    {
        Task<ResultDto> IngestFileAsync(int transactionTypeId, Stream fileStream, string fileName, int uploadedByUserId, string uploadedByUserName);
        Task<ResultDto> GetMakerBatchesAsync(int uploadedByUserId);
        Task<ResultDto> GetBatchValidationReportAsync(int batchId);
    }
}
