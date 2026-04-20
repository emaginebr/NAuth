using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NAuth.ACL.Interfaces;
using NAuth.DTO.Settings;
using NAuth.DTO.User;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace NAuth.ACL
{
    public class UserClient : IUserClient
    {
        private const string ApplicationJsonMediaType = "application/json";
        private const string BearerAuthenticationScheme = "Bearer";

        private readonly HttpClient _httpClient;
        private readonly IOptions<NAuthSetting> _nauthSetting;
        private readonly ILogger<UserClient> _logger;

        public UserClient(IOptions<NAuthSetting> nauthSetting, ILogger<UserClient> logger, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _nauthSetting = nauthSetting;
            _logger = logger;
        }

        public UserSessionInfo? GetUserInSession(HttpContext httpContext)
        {
            _logger.LogInformation("GetUserInSession called");

            if (httpContext?.User?.Claims == null || !httpContext.User.Claims.Any())
            {
                _logger.LogWarning("No claims found in HttpContext");
                return null;
            }

            var claims = httpContext.User.Claims.ToList();

            var userInfo = new UserSessionInfo
            {
                UserId = long.TryParse(claims.FirstOrDefault(c => c.Type == "userId")?.Value, out var userId) ? userId : 0,
                Name = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value,
                Email = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value,
                Hash = claims.FirstOrDefault(c => c.Type == "hash")?.Value,
                IpAddress = claims.FirstOrDefault(c => c.Type == "ipAddress")?.Value,
                UserAgent = claims.FirstOrDefault(c => c.Type == "userAgent")?.Value,
                Fingerprint = claims.FirstOrDefault(c => c.Type == "fingerprint")?.Value,
                IsAdmin = bool.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out var isAdmin) && isAdmin,
                Roles = claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList()
            };

            _logger.LogInformation("User retrieved from session: UserId={UserId}, Email={Email}", userInfo.UserId, userInfo.Email);

            return userInfo;
        }

        public async Task<UserInfo?> GetMeAsync(string token)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/getMe";
            _logger.LogInformation("GetMeAsync - Accessing URL: {Url}", url);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerAuthenticationScheme, token);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<UserInfo>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation("GetMeAsync - User retrieved: UserId={UserId}", result?.UserId);

            return result;
        }

        public async Task<UserInfo?> GetByIdAsync(long userId, string token)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/getById/{userId}";
            _logger.LogInformation("GetByIdAsync - Accessing URL: {Url}", url);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerAuthenticationScheme, token);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<UserInfo>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation("GetByIdAsync - User retrieved: UserId={UserId}, Email={Email}", result?.UserId, result?.Email);

            return result;
        }

        public async Task<UserInfo?> GetByTokenAsync(string token)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/getByToken/{token}";
            _logger.LogInformation("GetByTokenAsync - Accessing URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<UserInfo>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation("GetByTokenAsync - User retrieved: UserId={UserId}", result?.UserId);

            return result;
        }

        public async Task<UserInfo?> GetByEmailAsync(string email)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/getByEmail/{email}";
            _logger.LogInformation("GetByEmailAsync - Accessing URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<UserInfo>(json);
            _logger.LogInformation("GetByEmailAsync - User retrieved: UserId={UserId}, Email={Email}", result?.UserId, result?.Email);

            return result;
        }

        public async Task<UserInfo?> GetBySlugAsync(string slug)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/getBySlug/{slug}";
            _logger.LogInformation("GetBySlugAsync - Accessing URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<UserInfo>(json);
            _logger.LogInformation("GetBySlugAsync - User retrieved: UserId={UserId}, Slug={Slug}", result?.UserId, result?.Slug);

            return result;
        }

        public async Task<UserInfo?> InsertAsync(UserInsertedInfo user)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/insert";
            _logger.LogInformation("InsertAsync - Accessing URL: {Url}, Email={Email}", url, user?.Email);

            var content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, ApplicationJsonMediaType);
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<UserInfo>(json);
            _logger.LogInformation("InsertAsync - User inserted: UserId={UserId}, Email={Email}", result?.UserId, result?.Email);

            return result;
        }

        public async Task<UserInfo?> UpdateAsync(UserUpdatedInfo user, string token)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/update";
            _logger.LogInformation("UpdateAsync - Accessing URL: {Url}, UserId={UserId}", url, user?.UserId);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerAuthenticationScheme, token);
            var content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, ApplicationJsonMediaType);
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<UserInfo>(json);
            _logger.LogInformation("UpdateAsync - User updated: UserId={UserId}, Email={Email}", result?.UserId, result?.Email);

            return result;
        }

        public async Task<UserTokenResult?> LoginWithEmailAsync(LoginParam param)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/loginWithEmail";
            _logger.LogInformation("LoginWithEmailAsync - Accessing URL: {Url}, Email={Email}", url, param?.Email);

            var content = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, ApplicationJsonMediaType);
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<UserTokenResult>(json);
            _logger.LogInformation("LoginWithEmailAsync - Login successful: Token={Token}, UserId={UserId}, Email={Email}", result?.Token, result?.User.UserId, result?.User.Email);

            return result;
        }

        public async Task<bool> HasPasswordAsync(string token)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/hasPassword";
            _logger.LogInformation("HasPasswordAsync - Accessing URL: {Url}", url);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerAuthenticationScheme, token);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation("HasPasswordAsync - Result: {HasPassword}", result);

            return result;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordParam param, string token)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/changePassword";
            _logger.LogInformation("ChangePasswordAsync - Accessing URL: {Url}", url);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerAuthenticationScheme, token);
            var content = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, ApplicationJsonMediaType);
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation("ChangePasswordAsync - Password changed successfully");

            return result;
        }

        public async Task<bool> SendRecoveryMailAsync(string email)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/sendRecoveryMail/{email}";
            _logger.LogInformation("SendRecoveryMailAsync - Accessing URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation("SendRecoveryMailAsync - Recovery email sent to: {Email}", email);

            return result;
        }

        public async Task<bool> ChangePasswordUsingHashAsync(ChangePasswordUsingHashParam param)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/changePasswordUsingHash";
            _logger.LogInformation("ChangePasswordUsingHashAsync - Accessing URL: {Url}", url);

            var content = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, ApplicationJsonMediaType);
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<bool>(await response.Content.ReadAsStringAsync());
            _logger.LogInformation("ChangePasswordUsingHashAsync - Password changed using hash successfully");

            return result;
        }

        public async Task<IList<UserInfo>> ListAsync(int take)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/list/{take}";
            _logger.LogInformation("ListAsync - Accessing URL: {Url}, Take={Take}", url, take);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<IList<UserInfo>>(json);

            _logger.LogInformation("ListAsync - Retrieved {Count} users", result?.Count ?? 0);

            return result ?? new List<UserInfo>();
        }

        public async Task<string> UploadImageUserAsync(Stream fileStream, string fileName, string token)
        {
            var url = $"{_nauthSetting.Value.ApiUrl}/User/uploadImageUser";
            _logger.LogInformation("UploadImageUserAsync - Accessing URL: {Url}, FileName={FileName}", url, fileName);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerAuthenticationScheme, token);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("UploadImageUserAsync - Image uploaded successfully: {Result}", result);

            return result;
        }
    }

}
