using System.Threading.Tasks;
using ASPER.AuthAPI.Grpc.Client.Services;
using ASPER.AuthAPI.Protos;
using ASPER.CORPORATE_BANKING.Application.Interfaces;

namespace ASPER.CORPORATE_BANKING.Application.Services
{
    public class AuthIntegrationService : IAuthIntegrationService
    {
        private readonly IUserInfoGrpcService _userInfoGrpcService;

        public AuthIntegrationService(IUserInfoGrpcService userInfoGrpcService)
        {
            _userInfoGrpcService = userInfoGrpcService;
        }

        public async Task<GetUserRoleByUserNameResponse> GetRoleInfoByUserAsync(string userName)
        {
            var request = new GetUserRoleByUserNameRequest { Username = userName };
            return await _userInfoGrpcService.GetRoleInfoByUserAsync(request);
        }

        public async Task<GetAllUserInfosResponse> GetAllUserInfosAsync()
        {
            return await _userInfoGrpcService.GetAllUserInfos();
        }

        //public async Task<UserDataInfoListResponse> GetAllUserDataListAsync()
        //{
        //    // Note: If IUserInfoGrpcService doesn't expose this directly, we might need the raw client.
        //    // Assuming IUserInfoGrpcService exposes it based on the standard pattern.
        //    return await _userInfoGrpcService.GetAllUserDataList();
        //}

        public async Task<MakerCheckerUsersResponse> GetMakerCheckerUsersAsync()
        {
            return await _userInfoGrpcService.GetMakerCheckerUsers();
        }

        
        public async Task<GetAllRolesResponse> GetAllRolesAsync()
        {
            return await _userInfoGrpcService.GetAllRoles();
        }
       
    }
}
