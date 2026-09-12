using System.Threading.Tasks;
using ASPER.AuthAPI.Protos;

namespace ASPER.CORPORATE_BANKING.Application.Interfaces
{
    public interface IAuthIntegrationService
    {
        Task<GetUserRoleByUserNameResponse> GetRoleInfoByUserAsync(string userName);
        Task<GetAllUserInfosResponse> GetAllUserInfosAsync();
        //Task<UserDataInfoListResponse> GetAllUserDataListAsync();
        Task<MakerCheckerUsersResponse> GetMakerCheckerUsersAsync();
         Task<GetAllRolesResponse> GetAllRolesAsync();
    }
}
