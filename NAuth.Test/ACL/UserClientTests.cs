using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NAuth.ACL;
using NAuth.DTO.Settings;
using NAuth.DTO.User;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace NAuth.Test.ACL
{
    public class UserClientTests
    {
        private readonly Mock<IOptions<NAuthSetting>> _mockOptions;
        private readonly Mock<ILogger<UserClient>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly NAuthSetting _nauthSetting;

        public UserClientTests()
        {
            _mockOptions = new Mock<IOptions<NAuthSetting>>();
            _mockLogger = new Mock<ILogger<UserClient>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            _nauthSetting = new NAuthSetting
            {
                ApiUrl = "https://api.test.com"
            };

            _mockOptions.Setup(o => o.Value).Returns(_nauthSetting);
        }

        private UserClient CreateUserClient()
        {
            var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri(_nauthSetting.ApiUrl)
            };

            var userClient = new UserClient(_mockOptions.Object, _mockLogger.Object, httpClient);

            var httpClientField = typeof(UserClient).GetField("_httpClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpClientField?.SetValue(userClient, httpClient);

            return userClient;
        }

        #region GetUserInSession Tests

        [Fact]
        public void GetUserInSession_WithValidClaims_ShouldReturnUserSessionInfo()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim("userId", "1"),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Email, "test@test.com"),
                new Claim("hash", "test-hash"),
                new Claim("ipAddress", "127.0.0.1"),
                new Claim("userAgent", "Mozilla/5.0"),
                new Claim("fingerprint", "test-fingerprint"),
                new Claim("isAdmin", "true"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(ClaimTypes.Role, "user")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            var userClient = CreateUserClient();

            // Act
            var result = userClient.GetUserInSession(httpContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1L, result.UserId);
            Assert.Equal("Test User", result.Name);
            Assert.Equal("test@test.com", result.Email);
            Assert.Equal("test-hash", result.Hash);
            Assert.Equal("127.0.0.1", result.IpAddress);
            Assert.Equal("Mozilla/5.0", result.UserAgent);
            Assert.Equal("test-fingerprint", result.Fingerprint);
            Assert.True(result.IsAdmin);
            Assert.Equal(2, result.Roles.Count);
            Assert.Contains("admin", result.Roles);
            Assert.Contains("user", result.Roles);
        }

        [Fact]
        public void GetUserInSession_WithNullContext_ShouldReturnNull()
        {
            // Arrange
            var userClient = CreateUserClient();

            // Act
            var result = userClient.GetUserInSession(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetUserInSession_WithNoClaims_ShouldReturnNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var userClient = CreateUserClient();

            // Act
            var result = userClient.GetUserInSession(httpContext);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetMeAsync Tests

        [Fact]
        public async Task GetMeAsync_WithValidToken_ShouldReturnUser()
        {
            // Arrange
            var token = "valid-token";
            var user = new UserInfo
            {
                UserId = 1L,
                Name = "Test User",
                Email = "test@test.com"
            };

            var jsonResponse = JsonConvert.SerializeObject(user);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains("/User/getMe")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.GetMeAsync(token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1L, result.UserId);
            Assert.Equal("Test User", result.Name);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var userId = 1L;
            var token = "valid-token";
            var user = new UserInfo
            {
                UserId = userId,
                Name = "Test User",
                Email = "test@test.com"
            };

            var jsonResponse = JsonConvert.SerializeObject(user);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains($"/User/getById/{userId}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.GetByIdAsync(userId, token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task GetByIdAsync_WhenApiReturnsError_ShouldThrowException()
        {
            // Arrange
            var userId = 999L;
            var token = "valid-token";
            var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("User not found")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => userClient.GetByIdAsync(userId, token));
        }

        #endregion

        #region GetByEmailAsync Tests

        [Fact]
        public async Task GetByEmailAsync_WithValidEmail_ShouldReturnUser()
        {
            // Arrange
            var email = "test@test.com";
            var user = new UserInfo
            {
                UserId = 1L,
                Email = email,
                Name = "Test User"
            };

            var jsonResponse = JsonConvert.SerializeObject(user);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains($"/User/getByEmail/{email}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.GetByEmailAsync(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
        }

        #endregion

        #region GetBySlugAsync Tests

        [Fact]
        public async Task GetBySlugAsync_WithValidSlug_ShouldReturnUser()
        {
            // Arrange
            var slug = "test-user";
            var user = new UserInfo
            {
                UserId = 1L,
                Slug = slug,
                Name = "Test User"
            };

            var jsonResponse = JsonConvert.SerializeObject(user);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains($"/User/getBySlug/{slug}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.GetBySlugAsync(slug);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(slug, result.Slug);
        }

        #endregion

        #region InsertAsync Tests

        [Fact]
        public async Task InsertAsync_WithValidUser_ShouldReturnInsertedUser()
        {
            // Arrange
            var user = new UserInsertedInfo
            {
                Name = "New User",
                Email = "new@test.com",
                Password = "password123"
            };

            var insertedUser = new UserInfo
            {
                UserId = 1L,
                Name = user.Name,
                Email = user.Email
            };

            var jsonResponse = JsonConvert.SerializeObject(insertedUser);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains("/User/insert")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.InsertAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1L, result.UserId);
            Assert.Equal(user.Email, result.Email);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidUser_ShouldReturnUpdatedUser()
        {
            // Arrange
            var token = "valid-token";
            var user = new UserUpdatedInfo
            {
                UserId = 1L,
                Name = "Updated User",
                Email = "updated@test.com"
            };

            var jsonResponse = JsonConvert.SerializeObject(user);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains("/User/update")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.UpdateAsync(user, token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.UserId, result.UserId);
            Assert.Equal(user.Name, result.Name);
        }

        #endregion

        #region LoginWithEmailAsync Tests

        [Fact]
        public async Task LoginWithEmailAsync_WithValidCredentials_ShouldReturnUser()
        {
            // Arrange
            var loginParam = new LoginParam
            {
                Email = "test@test.com",
                Password = "password123"
            };

            var userTokenResult = new UserTokenResult
            {
                Token = "test-token-12345",
                User = new UserInfo
                {
                    UserId = 1L,
                    Email = loginParam.Email,
                    Name = "Test User"
                }
            };

            var jsonResponse = JsonConvert.SerializeObject(userTokenResult);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains("/User/loginWithEmail")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.LoginWithEmailAsync(loginParam);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-token-12345", result.Token);
            Assert.NotNull(result.User);
            Assert.Equal(loginParam.Email, result.User.Email);
        }

        #endregion

        #region HasPasswordAsync Tests

        [Fact]
        public async Task HasPasswordAsync_WhenUserHasPassword_ShouldReturnTrue()
        {
            // Arrange
            var token = "valid-token";
            var jsonResponse = JsonConvert.SerializeObject(true);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains("/User/hasPassword")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.HasPasswordAsync(token);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region ChangePasswordAsync Tests

        [Fact]
        public async Task ChangePasswordAsync_WithValidParams_ShouldReturnTrue()
        {
            // Arrange
            var token = "valid-token";
            var param = new ChangePasswordParam
            {
                OldPassword = "oldpass",
                NewPassword = "newpass"
            };

            var jsonResponse = JsonConvert.SerializeObject(true);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains("/User/changePassword")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.ChangePasswordAsync(param, token);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region SendRecoveryMailAsync Tests

        [Fact]
        public async Task SendRecoveryMailAsync_WithValidEmail_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@test.com";
            var jsonResponse = JsonConvert.SerializeObject(true);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains($"/User/sendRecoveryMail/{email}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.SendRecoveryMailAsync(email);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region ChangePasswordUsingHashAsync Tests

        [Fact]
        public async Task ChangePasswordUsingHashAsync_WithValidParams_ShouldReturnTrue()
        {
            // Arrange
            var param = new ChangePasswordUsingHashParam
            {
                RecoveryHash = "recovery-hash",
                NewPassword = "newpass"
            };

            var jsonResponse = JsonConvert.SerializeObject(true);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains("/User/changePasswordUsingHash")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.ChangePasswordUsingHashAsync(param);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region ListAsync Tests

        [Fact]
        public async Task ListAsync_WithValidTake_ShouldReturnUsers()
        {
            // Arrange
            var take = 10;
            var users = new List<UserInfo>
            {
                new UserInfo { UserId = 1L, Name = "User 1", Email = "user1@test.com" },
                new UserInfo { UserId = 2L, Name = "User 2", Email = "user2@test.com" }
            };

            var jsonResponse = JsonConvert.SerializeObject(users);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri!.ToString().Contains($"/User/list/{take}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.ListAsync(take);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task ListAsync_WithEmptyResult_ShouldReturnEmptyList()
        {
            // Arrange
            var take = 10;
            var users = new List<UserInfo>();

            var jsonResponse = JsonConvert.SerializeObject(users);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.ListAsync(take);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region UploadImageUserAsync Tests

        [Fact]
        public async Task UploadImageUserAsync_WithValidFile_ShouldReturnUrl()
        {
            // Arrange
            var token = "valid-token";
            var fileName = "test.jpg";
            var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("fake image content"));
            var expectedUrl = "https://cdn.test.com/images/test.jpg";

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(expectedUrl, Encoding.UTF8, "text/plain")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains("/User/uploadImageUser")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            var result = await userClient.UploadImageUserAsync(fileStream, fileName, token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUrl, result);
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task GetByIdAsync_ShouldLogUrlAndResult()
        {
            // Arrange
            var userId = 1L;
            var token = "valid-token";
            var user = new UserInfo { UserId = userId, Name = "Test" };

            var jsonResponse = JsonConvert.SerializeObject(user);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            var userClient = CreateUserClient();

            // Act
            await userClient.GetByIdAsync(userId, token);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("GetByIdAsync")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion
    }
}
