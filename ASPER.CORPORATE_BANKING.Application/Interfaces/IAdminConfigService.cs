using System.Threading.Tasks;
using ASPER.CORPORATE_BANKING.Application.DTOs;

namespace ASPER.CORPORATE_BANKING.Application.Interfaces
{
    public interface IAdminConfigService
    {
        Task<ResultDto> CreateTransactionTypeAsync(CreateTransactionTypeRequest request);
        Task<ResultDto> PublishFileTemplateAsync(int transactionTypeId, CreateFileTemplateRequest request);
        Task<ResultDto> PublishApprovalMatrixAsync(int transactionTypeId, PublishApprovalMatrixRequest request, int actionByUserId);
    }
}
