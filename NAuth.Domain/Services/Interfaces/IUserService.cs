using Microsoft.AspNetCore.Http;
using NAuth.Domain.Models.Models;
using NAuth.DTO.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NAuth.Domain.Services.Interfaces
{
    public interface IUserService
    {
        string GetBucketName();
        IUserModel LoginWithEmail(string email, string password);
        Task<string> CreateToken(long userId, string ipAddress, string userAgent, string fingerprint);
        bool HasPassword(long userId);
        void ChangePasswordUsingHash(string recoveryHash, string newPassword);
        void ChangePassword(long userId, string oldPassword, string newPassword);
        Task<bool> SendRecoveryEmail(string email);

        Task<IUserModel> Insert(UserInsertedInfo user);
        Task<IUserModel> Update(UserUpdatedInfo user);
        IUserModel GetUserByEmail(string email);
        IUserModel GetBySlug(string slug);
        IUserModel GetUserByID(long userId);
        IUserModel GetByStripeId(string stripeId);
        UserSessionInfo GetUserInSession(HttpContext httpContext);
        Task<UserInfo> GetUserInfoFromModel(IUserModel md);
        IList<IUserModel> ListUsers();
        PagedResult<UserInfo> SearchUsers(string searchTerm, int page, int pageSize);
    }
}
