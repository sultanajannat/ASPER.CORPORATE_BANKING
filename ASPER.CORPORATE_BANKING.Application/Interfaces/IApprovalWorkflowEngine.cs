using System.Threading.Tasks;
using ASPER.CORPORATE_BANKING.Application.DTOs;

namespace ASPER.CORPORATE_BANKING.Application.Interfaces
{
    public interface IApprovalWorkflowEngine
    {
        Task<ResultDto> CheckAndForward(int transactionRecordId, int userId, int roleId, string remarks);
        Task<ResultDto> CheckReject(int transactionRecordId, int userId, int roleId, string remarks);
        Task<ResultDto> Approve(int transactionRecordId, int userId, int roleId, string remarks);
        Task<ResultDto> Reject(int transactionRecordId, int userId, int roleId, string remarks);
    }
}
