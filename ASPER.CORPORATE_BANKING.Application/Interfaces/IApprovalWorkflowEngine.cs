using System.Threading.Tasks;
using ASPER.CORPORATE_BANKING.Application.DTOs;

namespace ASPER.CORPORATE_BANKING.Application.Interfaces
{
    public interface IApprovalWorkflowEngine
    {
        Task<ResultDto> CheckAndForward(int transactionRecordId, string userName, string roleName, string remarks);
        Task<ResultDto> CheckReject(int transactionRecordId, string userName, string roleName, string remarks);
        Task<ResultDto> Approve(int transactionRecordId, string userName, string roleName, string remarks);
        Task<ResultDto> Reject(int transactionRecordId, string userName, string roleName, string remarks);
    }
}
