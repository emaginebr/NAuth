using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NAuth.Domain.Factory;
using NAuth.Domain.Factory.Interfaces;
using NAuth.Domain.Models.Models;
using NAuth.Domain.Services;
using NAuth.DTO.Settings;
using NAuth.DTO.User;
using NAuth.Infra.Interfaces;
using zTools.ACL.Interfaces;
using Xunit;

namespace NAuth.Test.Domain.Services
{
    public class UserServiceInsertUpdateTests
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
        private readonly Mock<ITransaction> _mockTransaction;
        private readonly UserService _userService;

        public UserServiceInsertUpdateTests()
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
            _mockTransaction = new Mock<ITransaction>();

            var nauthSetting = new NAuthSetting
            {
                JwtSecret = "test-secret-key",
                BucketName = "test-bucket"
            };

            _mockOptions.Setup(o => o.Value).Returns(nauthSetting);

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

            _mockUnitOfWork.Setup(u => u.BeginTransaction()).Returns(_mockTransaction.Object);
        }

        #region Insert Tests

        [Fact]
        public async Task Insert_WithValidUser_ShouldInsertSuccessfully()
        {
            // Arrange
            var user = new UserInsertedInfo
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "password123",
                IdDocument = "12345678901"
            };

            _mockUserModel.SetupGet(m => m.UserId).Returns(1L);
            _mockUserModel.SetupGet(m => m.Name).Returns(user.Name);
            _mockUserModel.SetupGet(m => m.Email).Returns(user.Email);

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(user.Email, _mockUserFactory.Object))
                .Returns((IUserModel)null!);
            _mockUserModel.Setup(m => m.ExistSlug(It.IsAny<long>(), It.IsAny<string>()))
                .Returns(false);
            _mockUserModel.Setup(m => m.Insert(_mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(true);
            _mockStringClient.Setup(s => s.OnlyNumbersAsync(user.IdDocument))
                .ReturnsAsync(user.IdDocument);
            _mockStringClient.Setup(s => s.GenerateSlugAsync(It.IsAny<string>()))
                .ReturnsAsync("test-user");
            _mockDocumentClient.Setup(d => d.validarCpfOuCnpjAsync(user.IdDocument))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.Insert(user);

            // Assert
            Assert.NotNull(result);
            _mockTransaction.Verify(t => t.Commit(), Times.Once);
            _mockUserModel.Verify(m => m.Insert(_mockUserFactory.Object), Times.Once);
            _mockUserModel.Verify(m => m.ChangePassword(It.IsAny<long>(), user.Password, _mockUserFactory.Object), Times.Once);
        }

        [Fact]
        public async Task Insert_WithEmptyName_ShouldThrowException()
        {
            // Arrange
            var user = new UserInsertedInfo
            {
                Name = "",
                Email = "test@example.com",
                Password = "password123"
            };

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.Insert(user));
            Assert.Contains("Error inserting user", exception.Message);
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public async Task Insert_WithEmptyEmail_ShouldThrowException()
        {
            // Arrange
            var user = new UserInsertedInfo
            {
                Name = "Test User",
                Email = "",
                Password = "password123"
            };

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.Insert(user));
            Assert.Contains("Error inserting user", exception.Message);
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public async Task Insert_WithInvalidEmail_ShouldThrowException()
        {
            // Arrange
            var user = new UserInsertedInfo
            {
                Name = "Test User",
                Email = "invalid-email",
                Password = "password123"
            };

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.Insert(user));
            Assert.Contains("Error inserting user", exception.Message);
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public async Task Insert_WithDuplicateEmail_ShouldThrowException()
        {
            // Arrange
            var user = new UserInsertedInfo
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "password123"
            };

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(user.Email, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);
            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.Insert(user));
            Assert.Contains("Error inserting user", exception.Message);
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public async Task Insert_WithEmptyPassword_ShouldThrowException()
        {
            // Arrange
            var user = new UserInsertedInfo
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = ""
            };

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(user.Email, _mockUserFactory.Object))
                .Returns((IUserModel)null!);
            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.Insert(user));
            Assert.Contains("Error inserting user", exception.Message);
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public async Task Insert_WithInvalidCPF_ShouldThrowException()
        {
            // Arrange
            var user = new UserInsertedInfo
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "password123",
                IdDocument = "12345678901"
            };

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(user.Email, _mockUserFactory.Object))
                .Returns((IUserModel)null!);
            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(true);
            _mockStringClient.Setup(s => s.OnlyNumbersAsync(user.IdDocument))
                .ReturnsAsync(user.IdDocument);
            _mockDocumentClient.Setup(d => d.validarCpfOuCnpjAsync(user.IdDocument))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.Insert(user));
            Assert.Contains("Error inserting user", exception.Message);
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public async Task Insert_WithPhonesAndAddresses_ShouldInsertAll()
        {
            // Arrange
            var mockPhoneModel = new Mock<IUserPhoneModel>();
            var mockAddressModel = new Mock<IUserAddressModel>();

            var user = new UserInsertedInfo
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "password123",
                Phones = new List<UserPhoneInfo>
                {
                    new UserPhoneInfo { Phone = "1234567890" }
                },
                Addresses = new List<UserAddressInfo>
                {
                    new UserAddressInfo
                    {
                        ZipCode = "12345678",
                        Address = "Test Street",
                        Complement = "Apt 1",
                        Neighborhood = "Test",
                        City = "Test City",
                        State = "TS"
                    }
                }
            };

            _mockUserModel.SetupGet(m => m.UserId).Returns(1L);
            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockPhoneFactory.Setup(f => f.BuildUserPhoneModel()).Returns(mockPhoneModel.Object);
            _mockAddressFactory.Setup(f => f.BuildUserAddressModel()).Returns(mockAddressModel.Object);

            _mockUserModel.Setup(m => m.GetByEmail(user.Email, _mockUserFactory.Object))
                .Returns((IUserModel)null!);
            _mockUserModel.Setup(m => m.ExistSlug(It.IsAny<long>(), It.IsAny<string>()))
                .Returns(false);
            _mockUserModel.Setup(m => m.Insert(_mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(true);
            _mockStringClient.Setup(s => s.GenerateSlugAsync(It.IsAny<string>()))
                .ReturnsAsync("test-user");
            _mockStringClient.Setup(s => s.OnlyNumbersAsync(It.IsAny<string>()))
                .Returns<string>(s => Task.FromResult(s));

            // Act
            var result = await _userService.Insert(user);

            // Assert
            Assert.NotNull(result);
            mockPhoneModel.Verify(m => m.Insert(_mockPhoneFactory.Object), Times.Once);
            mockAddressModel.Verify(m => m.Insert(_mockAddressFactory.Object), Times.Once);
            _mockTransaction.Verify(t => t.Commit(), Times.Once);
        }

        [Fact]
        public async Task Insert_WithRoles_ShouldAddRoles()
        {
            // Arrange
            var mockRoleModel = new Mock<IRoleModel>();
            mockRoleModel.SetupGet(r => r.RoleId).Returns(1L);

            var user = new UserInsertedInfo
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "password123",
                Roles = new List<RoleInfo>
                {
                    new RoleInfo { RoleId = 1L }
                }
            };

            _mockUserModel.SetupGet(m => m.UserId).Returns(1L);
            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockRoleFactory.Setup(f => f.BuildRoleModel()).Returns(mockRoleModel.Object);

            _mockUserModel.Setup(m => m.GetByEmail(user.Email, _mockUserFactory.Object))
                .Returns((IUserModel)null!);
            _mockUserModel.Setup(m => m.ExistSlug(It.IsAny<long>(), It.IsAny<string>()))
                .Returns(false);
            _mockUserModel.Setup(m => m.Insert(_mockUserFactory.Object))
                .Returns(_mockUserModel.Object);

            mockRoleModel.Setup(m => m.GetById(1L, _mockRoleFactory.Object))
                .Returns(mockRoleModel.Object);

            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(true);
            _mockStringClient.Setup(s => s.GenerateSlugAsync(It.IsAny<string>()))
                .ReturnsAsync("test-user");

            // Act
            var result = await _userService.Insert(user);

            // Assert
            Assert.NotNull(result);
            _mockUserModel.Verify(m => m.AddRole(1L, 1L), Times.Once);
            _mockTransaction.Verify(t => t.Commit(), Times.Once);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidUser_ShouldUpdateSuccessfully()
        {
            // Arrange
            var mockPhoneModel = new Mock<IUserPhoneModel>();
            var mockAddressModel = new Mock<IUserAddressModel>();

            var user = new UserInfo
            {
                UserId = 1L,
                Name = "Updated User",
                Email = "updated@example.com",
                IdDocument = "12345678901"
            };

            _mockUserModel.SetupGet(m => m.UserId).Returns(1L);
            _mockUserModel.SetupGet(m => m.Name).Returns(user.Name);
            _mockUserModel.SetupGet(m => m.Email).Returns(user.Email);

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockPhoneFactory.Setup(f => f.BuildUserPhoneModel()).Returns(mockPhoneModel.Object);
            _mockAddressFactory.Setup(f => f.BuildUserAddressModel()).Returns(mockAddressModel.Object);

            _mockUserModel.Setup(m => m.GetById(user.UserId, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(user.Email, _mockUserFactory.Object))
                .Returns((IUserModel)null!);
            _mockUserModel.Setup(m => m.ExistSlug(It.IsAny<long>(), It.IsAny<string>()))
                .Returns(false);

            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(true);
            _mockStringClient.Setup(s => s.OnlyNumbersAsync(user.IdDocument))
                .ReturnsAsync(user.IdDocument);
            _mockStringClient.Setup(s => s.GenerateSlugAsync(It.IsAny<string>()))
                .ReturnsAsync("updated-user");
            _mockDocumentClient.Setup(d => d.validarCpfOuCnpjAsync(user.IdDocument))
                .ReturnsAsync(true);

            // Act
            var result = await _userService.Update(user);

            // Assert
            Assert.NotNull(result);
            _mockTransaction.Verify(t => t.Commit(), Times.Once);
            _mockUserModel.Verify(m => m.Update(_mockUserFactory.Object), Times.Once);
            mockPhoneModel.Verify(m => m.DeleteAllByUser(1L), Times.Once);
            mockAddressModel.Verify(m => m.DeleteAllByUser(1L), Times.Once);
            _mockUserModel.Verify(m => m.RemoveAllRoles(1L), Times.Once);
        }

        [Fact]
        public async Task Update_WithInvalidUserId_ShouldThrowException()
        {
            // Arrange
            var user = new UserInfo
            {
                UserId = 0,
                Name = "Test User",
                Email = "test@example.com"
            };

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.Update(user));
            Assert.Contains("Error updating user", exception.Message);
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public async Task Update_WithNonExistentUser_ShouldThrowException()
        {
            // Arrange
            var user = new UserInfo
            {
                UserId = 999L,
                Name = "Test User",
                Email = "test@example.com"
            };

            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetById(user.UserId, _mockUserFactory.Object))
                .Returns((IUserModel)null!);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.Update(user));
            Assert.Contains("Error updating user", exception.Message);
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public async Task Update_WithDuplicateEmail_ShouldThrowException()
        {
            // Arrange
            var mockExistingUser = new Mock<IUserModel>();
            mockExistingUser.SetupGet(m => m.UserId).Returns(2L);

            var user = new UserInfo
            {
                UserId = 1L,
                Name = "Test User",
                Email = "existing@example.com"
            };

            _mockUserModel.SetupGet(m => m.UserId).Returns(1L);
            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetById(user.UserId, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(user.Email, _mockUserFactory.Object))
                .Returns(mockExistingUser.Object);
            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _userService.Update(user));
            Assert.Contains("Error updating user", exception.Message);
            _mockTransaction.Verify(t => t.Rollback(), Times.Once);
        }

        [Fact]
        public async Task Update_WithSameEmail_ShouldUpdateSuccessfully()
        {
            // Arrange
            var mockPhoneModel = new Mock<IUserPhoneModel>();
            var mockAddressModel = new Mock<IUserAddressModel>();

            var user = new UserInfo
            {
                UserId = 1L,
                Name = "Updated User",
                Email = "same@example.com"
            };

            _mockUserModel.SetupGet(m => m.UserId).Returns(1L);
            _mockUserFactory.Setup(f => f.BuildUserModel()).Returns(_mockUserModel.Object);
            _mockPhoneFactory.Setup(f => f.BuildUserPhoneModel()).Returns(mockPhoneModel.Object);
            _mockAddressFactory.Setup(f => f.BuildUserAddressModel()).Returns(mockAddressModel.Object);

            _mockUserModel.Setup(m => m.GetById(user.UserId, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.GetByEmail(user.Email, _mockUserFactory.Object))
                .Returns(_mockUserModel.Object);
            _mockUserModel.Setup(m => m.ExistSlug(It.IsAny<long>(), It.IsAny<string>()))
                .Returns(false);

            _mockMailClient.Setup(m => m.IsValidEmailAsync(user.Email))
                .ReturnsAsync(true);
            _mockStringClient.Setup(s => s.GenerateSlugAsync(It.IsAny<string>()))
                .ReturnsAsync("updated-user");

            // Act
            var result = await _userService.Update(user);

            // Assert
            Assert.NotNull(result);
            _mockTransaction.Verify(t => t.Commit(), Times.Once);
            _mockUserModel.Verify(m => m.Update(_mockUserFactory.Object), Times.Once);
        }

        #endregion
    }
}
