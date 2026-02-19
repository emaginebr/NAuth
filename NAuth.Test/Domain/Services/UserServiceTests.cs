using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NAuth.Domain.Exceptions;
using NAuth.Domain.Factory;
using NAuth.Domain.Factory.Interfaces;
using NAuth.Domain.Models.Models;
using NAuth.Domain.Services;
using NAuth.DTO.Settings;
using NAuth.Infra.Interfaces;
using zTools.ACL.Interfaces;
using zTools.DTO.MailerSend;
using System.Security.Claims;
using Xunit;

namespace NAuth.Test.Domain.Services
{
    public class UserServiceTests
    {
        private readonly Mock<ILogger<UserService>> _mockLogger;
        private readonly Mock<IOptions<NAuthSetting>> _mockOptions;
        private readonly Mock<IUserDomainFactory> _mockUserFactory;
        private readonly Mock<IUserPhoneDomainFactory> _mockPhoneFactory;
        private readonly Mock<IUserAddressDomainFactory> _mockAddressFactory;
        private readonly Mock<IRoleDomainFactory> _mockRoleFactory;
        private readonly Mock<IMailClient> _mockMailClient;
        private readonly Mock<IFileClient> _mockFileClient;
        private readonly Mock<IStringClient> _mockStringClient;
        private readonly Mock<IDocumentClient> _mockDocumentClient;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserModel> _mockUserModel;
        private readonly UserService _userService;
        private readonly NAuthSetting _nauthSetting;

        public UserServiceTests()
        {
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockOptions = new Mock<IOptions<NAuthSetting>>();
            _mockUserFactory = new Mock<IUserDomainFactory>();
            _mockPhoneFactory = new Mock<IUserPhoneDomainFactory>();
            _mockAddressFactory = new Mock<IUserAddressDomainFactory>();
            _mockRoleFactory = new Mock<IRoleDomainFactory>();
            _mockMailClient = new Mock<IMailClient>();
            _mockFileClient = new Mock<IFileClient>();
            _mockStringClient = new Mock<IStringClient>();
            _mockDocumentClient = new Mock<IDocumentClient>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserModel = new Mock<IUserModel>();

            _nauthSetting = new NAuthSetting
            {
                JwtSecret = "test-secret-key-that-is-long-enough-for-hmac-sha256",
                BucketName = "test-bucket"
            };

            _mockOptions.Setup(o => o.Value).Returns(_nauthSetting);

            var factories = new DomainFactory(
                _mockUserFactory.Object,
                _mockPhoneFactory.Object,
                _mockAddressFactory.Object,
                _mockRoleFactory.Object
            );

            var clients = new ExternalClients(
                _mockMailClient.Object,
                _mockFileClient.Object,
                _mockStringClient.Object,
                _mockDocumentClient.Object
            );

            _userService = new UserService(
                _mockLogger.Object,
                _mockOptions.Object,
                factories,
                clients,
                _mockUnitOfWork.Object
            );
        }

        #region GetBucketName Tests

        [Fact]
        public void GetBucketName_ShouldReturnConfiguredBucketName()
        {
            // Act
            var result = _userService.GetBucketName();

            // Assert
            Assert.Equal("test-bucket", result);
        }

        #endregion

        #region LoginWithEmail Tests

        [Fact]
        public void LoginWithEmail_WithValidCredentials_ShouldReturnUser()
        {
            // Arrange
            var email = "test@example.com";
            var password = "password123";

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.LoginWithEmail(email, password, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            // Act
            var result = _userService.LoginWithEmail(email, password);

            // Assert
            Assert.NotNull(result);
            _mockUserModel.Verify(m => m.LoginWithEmail(email, password, _mockUserFactory.Object), Times.Once);
        }

        #endregion

        #region CreateToken Tests

        [Fact]
        public async Task CreateToken_WithValidParameters_ShouldReturnToken()
        {
            // Arrange
            var userId = 1L;
            var ipAddress = "127.0.0.1";
            var userAgent = "Mozilla/5.0";
            var fingerprint = "test-fingerprint";

            _mockUserModel.SetupGet(m => m.UserId).Returns(userId);
            _mockUserModel.SetupGet(m => m.Name).Returns("Test User");
            _mockUserModel.SetupGet(m => m.Email).Returns("test@example.com");
            _mockUserModel.SetupGet(m => m.Hash).Returns("test-hash");
            _mockUserModel.SetupGet(m => m.IsAdmin).Returns(false);
            _mockUserModel.Setup(m => m.ListRoles(userId, _mockRoleFactory.Object))
                .Returns(new List<IRoleModel>());

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetById(userId, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            // Act
            var result = await _userService.CreateToken(userId, ipAddress, userAgent, fingerprint);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task CreateToken_WithInvalidUserId_ShouldThrowException()
        {
            // Arrange
            var userId = 0L;
            var ipAddress = "127.0.0.1";
            var userAgent = "Mozilla/5.0";
            var fingerprint = "test-fingerprint";

            // Act & Assert
            await Assert.ThrowsAsync<UserValidationException>(() =>
                _userService.CreateToken(userId, ipAddress, userAgent, fingerprint));
        }

        [Fact]
        public async Task CreateToken_WithEmptyIpAddress_ShouldThrowException()
        {
            // Arrange
            var userId = 1L;
            var ipAddress = "";
            var userAgent = "Mozilla/5.0";
            var fingerprint = "test-fingerprint";

            // Act & Assert
            await Assert.ThrowsAsync<UserValidationException>(() =>
                _userService.CreateToken(userId, ipAddress, userAgent, fingerprint));
        }

        [Fact]
        public async Task CreateToken_WithUserNotFound_ShouldThrowException()
        {
            // Arrange
            var userId = 1L;
            var ipAddress = "127.0.0.1";
            var userAgent = "Mozilla/5.0";
            var fingerprint = "test-fingerprint";

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetById(userId, _mockUserFactory.Object))
                .Returns((IUserModel)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() =>
                _userService.CreateToken(userId, ipAddress, userAgent, fingerprint));
            Assert.Equal("User not found", exception.Message);
        }

        #endregion

        #region HasPassword Tests

        [Fact]
        public void HasPassword_WhenUserHasPassword_ShouldReturnTrue()
        {
            // Arrange
            var userId = 1L;
            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.HasPassword(userId, _mockUserFactory.Object)).Returns(true);

            // Act
            var result = _userService.HasPassword(userId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasPassword_WhenUserDoesNotHavePassword_ShouldReturnFalse()
        {
            // Arrange
            var userId = 1L;
            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.HasPassword(userId, _mockUserFactory.Object)).Returns(false);

            // Act
            var result = _userService.HasPassword(userId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ChangePasswordUsingHash Tests

        [Fact]
        public void ChangePasswordUsingHash_WithValidParameters_ShouldChangePassword()
        {
            // Arrange
            var recoveryHash = "valid-hash";
            var newPassword = "newPassword123";

            _mockUserModel.SetupGet(m => m.UserId).Returns(1L);
            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByRecoveryHash(recoveryHash, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            // Act
            _userService.ChangePasswordUsingHash(recoveryHash, newPassword);

            // Assert
            _mockUserModel.Verify(m => m.ChangePassword(1L, newPassword, _mockUserFactory.Object), Times.Once);
        }

        [Fact]
        public void ChangePasswordUsingHash_WithEmptyHash_ShouldThrowException()
        {
            // Arrange
            var recoveryHash = "";
            var newPassword = "newPassword123";

            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() =>
                _userService.ChangePasswordUsingHash(recoveryHash, newPassword));
            Assert.Equal("Recovery hash cant be empty", exception.Message);
        }

        [Fact]
        public void ChangePasswordUsingHash_WithEmptyPassword_ShouldThrowException()
        {
            // Arrange
            var recoveryHash = "valid-hash";
            var newPassword = "";

            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() =>
                _userService.ChangePasswordUsingHash(recoveryHash, newPassword));
            Assert.Equal("Password cant be empty", exception.Message);
        }

        [Fact]
        public void ChangePasswordUsingHash_WithInvalidHash_ShouldThrowException()
        {
            // Arrange
            var recoveryHash = "invalid-hash";
            var newPassword = "newPassword123";

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByRecoveryHash(recoveryHash, _mockUserFactory.Object))
                .Returns((IUserModel)null!);

            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() =>
                _userService.ChangePasswordUsingHash(recoveryHash, newPassword));
            Assert.Equal("User not found", exception.Message);
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public void ChangePassword_WithValidOldPassword_ShouldChangePassword()
        {
            // Arrange
            var userId = 1L;
            var oldPassword = "oldPassword123";
            var newPassword = "newPassword123";
            var email = "test@example.com";

            _mockUserModel.SetupGet(m => m.UserId).Returns(userId);
            _mockUserModel.SetupGet(m => m.Email).Returns(email);

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.HasPassword(userId, _mockUserFactory.Object)).Returns(true);
            _mockUserModel.Setup(m => m.GetById(userId, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.LoginWithEmail(email, oldPassword, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            // Act
            _userService.ChangePassword(userId, oldPassword, newPassword);

            // Assert
            _mockUserModel.Verify(m => m.ChangePassword(userId, newPassword, _mockUserFactory.Object), Times.Once);
        }

        [Fact]
        public void ChangePassword_WithEmptyOldPasswordWhenRequired_ShouldThrowException()
        {
            // Arrange
            var userId = 1L;
            var oldPassword = "";
            var newPassword = "newPassword123";

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.HasPassword(userId, _mockUserFactory.Object)).Returns(true);

            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() =>
                _userService.ChangePassword(userId, oldPassword, newPassword));
            Assert.Equal("Old password cant be empty", exception.Message);
        }

        [Fact]
        public void ChangePassword_WithEmptyNewPassword_ShouldThrowException()
        {
            // Arrange
            var userId = 1L;
            var oldPassword = "oldPassword123";
            var newPassword = "";

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.HasPassword(userId, _mockUserFactory.Object)).Returns(true);

            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() =>
                _userService.ChangePassword(userId, oldPassword, newPassword));
            Assert.Equal("New password cant be empty", exception.Message);
        }

        #endregion

        #region SendRecoveryEmail Tests

        [Fact]
        public async Task SendRecoveryEmail_WithValidEmail_ShouldSendEmail()
        {
            // Arrange
            var email = "test@example.com";
            var recoveryHash = "recovery-hash";

            _mockUserModel.SetupGet(m => m.UserId).Returns(1L);
            _mockUserModel.SetupGet(m => m.Name).Returns("Test User");
            _mockUserModel.SetupGet(m => m.Email).Returns(email);

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(email, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GenerateRecoveryHash(1L, _mockUserFactory.Object))
                .Returns(recoveryHash);
            _mockMailClient.Setup(m => m.SendmailAsync(It.IsAny<MailerInfo>()))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.SendRecoveryEmail(email);

            // Assert
            Assert.True(result);
            _mockMailClient.Verify(m => m.SendmailAsync(It.Is<MailerInfo>(
                mail => mail.To.Any(t => t.Email == email) &&
                        mail.Subject.Contains("Password Recovery"))), Times.Once);
        }

        [Fact]
        public async Task SendRecoveryEmail_WithEmptyEmail_ShouldThrowException()
        {
            // Arrange
            var email = "";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() =>
                _userService.SendRecoveryEmail(email));
            Assert.Equal("Email cant be empty", exception.Message);
        }

        [Fact]
        public async Task SendRecoveryEmail_WithNonExistentEmail_ShouldThrowException()
        {
            // Arrange
            var email = "nonexistent@example.com";

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(email, _mockUserFactory.Object))
                .Returns((IUserModel)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() =>
                _userService.SendRecoveryEmail(email));
            Assert.Equal("User not found", exception.Message);
        }

        #endregion

        #region GetUserByEmail Tests

        [Fact]
        public void GetUserByEmail_WithExistingEmail_ShouldReturnUser()
        {
            // Arrange
            var email = "test@example.com";

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(email, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            // Act
            var result = _userService.GetUserByEmail(email);

            // Assert
            Assert.NotNull(result);
            _mockUserModel.Verify(m => m.GetByEmail(email, _mockUserFactory.Object), Times.Once);
        }

        #endregion

        #region GetUserByID Tests

        [Fact]
        public void GetUserByID_WithExistingId_ShouldReturnUser()
        {
            // Arrange
            var userId = 1L;

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetById(userId, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            // Act
            var result = _userService.GetUserByID(userId);

            // Assert
            Assert.NotNull(result);
            _mockUserModel.Verify(m => m.GetById(userId, _mockUserFactory.Object), Times.Once);
        }

        #endregion

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
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim("hash", "test-hash"),
                new Claim("ipAddress", "127.0.0.1"),
                new Claim("userAgent", "Mozilla/5.0"),
                new Claim("fingerprint", "test-fingerprint"),
                new Claim("isAdmin", "true"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(ClaimTypes.Role, "user")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            // Act
            var result = _userService.GetUserInSession(httpContext);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1L, result.UserId);
            Assert.Equal("Test User", result.Name);
            Assert.Equal("test@example.com", result.Email);
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
            // Act
            var result = _userService.GetUserInSession(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetUserInSession_WithNoClaims_ShouldReturnNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // Act
            var result = _userService.GetUserInSession(httpContext);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetUserInfoFromModel Tests

        [Fact]
        public async Task GetUserInfoFromModel_WithValidModel_ShouldReturnUserInfo()
        {
            // Arrange
            var mockPhoneModel = new Mock<IUserPhoneModel>();
            var mockAddressModel = new Mock<IUserAddressModel>();

            _mockUserModel.SetupGet(m => m.UserId).Returns(1L);
            _mockUserModel.SetupGet(m => m.Hash).Returns("test-hash");
            _mockUserModel.SetupGet(m => m.Slug).Returns("test-slug");
            _mockUserModel.SetupGet(m => m.Image).Returns("test-image.jpg");
            _mockUserModel.SetupGet(m => m.Name).Returns("Test User");
            _mockUserModel.SetupGet(m => m.Email).Returns("test@example.com");
            _mockUserModel.SetupGet(m => m.IsAdmin).Returns(false);
            _mockUserModel.Setup(m => m.ListRoles(1L, _mockRoleFactory.Object))
                .Returns(new List<IRoleModel>());

            _mockPhoneFactory.Setup(f => f.BuildUserPhoneModel()).Returns(mockPhoneModel.Object);
            mockPhoneModel.Setup(m => m.ListByUser(1L, _mockPhoneFactory.Object))
                .Returns(new List<IUserPhoneModel>());

            _mockAddressFactory.Setup(f => f.BuildUserAddressModel()).Returns(mockAddressModel.Object);
            mockAddressModel.Setup(m => m.ListByUser(1L, _mockAddressFactory.Object))
                .Returns(new List<IUserAddressModel>());

            _mockFileClient.Setup(f => f.GetFileUrlAsync("test-bucket", "test-image.jpg"))
                .ReturnsAsync("https://example.com/test-image.jpg");

            // Act
            var result = await _userService.GetUserInfoFromModel(_mockUserModel.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1L, result.UserId);
            Assert.Equal("test-hash", result.Hash);
            Assert.Equal("Test User", result.Name);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task GetUserInfoFromModel_WithNullModel_ShouldReturnNull()
        {
            // Act
            var result = await _userService.GetUserInfoFromModel(null);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByStripeId Tests

        [Fact]
        public void GetByStripeId_WithExistingStripeId_ShouldReturnUser()
        {
            // Arrange
            var stripeId = "stripe_123";

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByStripeId(stripeId, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            // Act
            var result = _userService.GetByStripeId(stripeId);

            // Assert
            Assert.NotNull(result);
            _mockUserModel.Verify(m => m.GetByStripeId(stripeId, _mockUserFactory.Object), Times.Once);
        }

        #endregion

        #region GetBySlug Tests

        [Fact]
        public void GetBySlug_WithExistingSlug_ShouldReturnUser()
        {
            // Arrange
            var slug = "test-slug";

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetBySlug(slug, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            // Act
            var result = _userService.GetBySlug(slug);

            // Assert
            Assert.NotNull(result);
            _mockUserModel.Verify(m => m.GetBySlug(slug, _mockUserFactory.Object), Times.Once);
        }

        #endregion

        #region ListUsers Tests

        [Fact]
        public void ListUsers_ShouldReturnListOfUsers()
        {
            // Arrange
            var userList = new List<IUserModel> { _mockUserModel.Object };

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.ListUsers(_mockUserFactory.Object))
                .Returns(userList);

            // Act
            var result = _userService.ListUsers();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            _mockUserModel.Verify(m => m.ListUsers(_mockUserFactory.Object), Times.Once);
        }

        #endregion
    }
}
