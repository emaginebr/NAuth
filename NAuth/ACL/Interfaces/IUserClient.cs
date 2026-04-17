using Microsoft.AspNetCore.Http;
using NAuth.DTO.User;

namespace NAuth.ACL.Interfaces
{
    public interface IUserClient
    {
        UserSessionInfo? GetUserInSession(HttpContext httpContext);
        Task<UserInfo?> GetMeAsync(string token);
        Task<UserInfo?> GetByIdAsync(long userId, string token);
        Task<UserInfo?> GetByTokenAsync(string token);
        Task<UserInfo?> GetByEmailAsync(string email);
        Task<UserInfo?> GetBySlugAsync(string slug);
        Task<UserInfo?> InsertAsync(UserInsertedInfo user);
        Task<UserInfo?> UpdateAsync(UserUpdatedInfo user, string token);
        Task<UserTokenResult?> LoginWithEmailAsync(LoginParam param);
        Task<bool> HasPasswordAsync(string token);
        Task<bool> ChangePasswordAsync(ChangePasswordParam param, string token);
        Task<bool> SendRecoveryMailAsync(string email);
        Task<bool> ChangePasswordUsingHashAsync(ChangePasswordUsingHashParam param);
        Task<IList<UserInfo>> ListAsync(int take);
        Task<string> UploadImageUserAsync(Stream fileStream, string fileName, string token);
    }
}
