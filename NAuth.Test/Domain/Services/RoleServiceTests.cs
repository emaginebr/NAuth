using Microsoft.Extensions.Logging;
using Moq;
using NAuth.Domain.Exceptions;
using NAuth.Domain.Factory;
using NAuth.Domain.Factory.Interfaces;
using NAuth.Domain.Models.Models;
using NAuth.Domain.Services;
using NAuth.DTO.User;
using zTools.ACL.Interfaces;
using Xunit;

namespace NAuth.Test.Domain.Services
{
    public class RoleServiceTests
    {
        private readonly Mock<ILogger<RoleService>> _mockLogger;
        private readonly DomainFactory _factory;
        private readonly Mock<IRoleDomainFactory> _mockRoleFactory;
        private readonly Mock<IRoleModel> _mockRoleModel;
        private readonly Mock<IStringClient> _mockStringClient;
        private readonly RoleService _roleService;

        public RoleServiceTests()
        {
            _mockLogger = new Mock<ILogger<RoleService>>();
            _mockRoleFactory = new Mock<IRoleDomainFactory>();
            _mockRoleModel = new Mock<IRoleModel>();
            _mockStringClient = new Mock<IStringClient>();

            _factory = new DomainFactory(
                Mock.Of<IUserDomainFactory>(),
                Mock.Of<IUserPhoneDomainFactory>(),
                Mock.Of<IUserAddressDomainFactory>(),
                _mockRoleFactory.Object
            );

            _mockRoleFactory.Setup(f => f.BuildRoleModel()).Returns(_mockRoleModel.Object);

            _roleService = new RoleService(_mockLogger.Object, _factory, _mockStringClient.Object);
        }

        #region GetById Tests

        [Fact]
        public void GetById_WithValidId_ShouldReturnRole()
        {
            // Arrange
            long roleId = 1;
            var expectedRole = new Mock<IRoleModel>();
            expectedRole.SetupGet(r => r.RoleId).Returns(roleId);
            expectedRole.SetupGet(r => r.Slug).Returns("admin");
            expectedRole.SetupGet(r => r.Name).Returns("Administrator");

            _mockRoleModel.Setup(m => m.GetById(roleId, _mockRoleFactory.Object))
                .Returns(expectedRole.Object);

            // Act
            var result = _roleService.GetById(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roleId, result.RoleId);
            _mockRoleModel.Verify(m => m.GetById(roleId, _mockRoleFactory.Object), Times.Once);
        }

        [Fact]
        public void GetById_WithInvalidId_ShouldThrowException()
        {
            // Arrange
            long roleId = 0;

            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() => _roleService.GetById(roleId));
            Assert.Equal("RoleId is invalid", exception.Message);
        }

        [Fact]
        public void GetById_WithNonExistentId_ShouldThrowException()
        {
            // Arrange
            long roleId = 999;
            _mockRoleModel.Setup(m => m.GetById(roleId, _mockRoleFactory.Object))
                .Returns((IRoleModel)null);

            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() => _roleService.GetById(roleId));
            Assert.Equal("Role not found", exception.Message);
        }

        #endregion

        #region GetBySlug Tests

        [Fact]
        public void GetBySlug_WithValidSlug_ShouldReturnRole()
        {
            // Arrange
            string slug = "admin";
            var expectedRole = new Mock<IRoleModel>();
            expectedRole.SetupGet(r => r.Slug).Returns(slug);

            _mockRoleModel.Setup(m => m.GetBySlug(slug, _mockRoleFactory.Object))
                .Returns(expectedRole.Object);

            // Act
            var result = _roleService.GetBySlug(slug);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(slug, result.Slug);
            _mockRoleModel.Verify(m => m.GetBySlug(slug, _mockRoleFactory.Object), Times.Once);
        }

        [Fact]
        public void GetBySlug_WithEmptySlug_ShouldThrowException()
        {
            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() => _roleService.GetBySlug(""));
            Assert.Equal("Slug is empty", exception.Message);
        }

        [Fact]
        public void GetBySlug_WithNonExistentSlug_ShouldThrowException()
        {
            // Arrange
            string slug = "nonexistent";
            _mockRoleModel.Setup(m => m.GetBySlug(slug, _mockRoleFactory.Object))
                .Returns((IRoleModel)null);

            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() => _roleService.GetBySlug(slug));
            Assert.Equal("Role not found", exception.Message);
        }

        #endregion

        #region ListRoles Tests

        [Fact]
        public void ListRoles_ShouldReturnRoles()
        {
            // Arrange
            var roles = new List<IRoleModel>
            {
                Mock.Of<IRoleModel>(),
                Mock.Of<IRoleModel>()
            };

            _mockRoleModel.Setup(m => m.ListRoles(_mockRoleFactory.Object))
                .Returns(roles);

            // Act
            var result = _roleService.ListRoles();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockRoleModel.Verify(m => m.ListRoles(_mockRoleFactory.Object), Times.Once);
        }

        [Fact]
        public void ListRoles_WithNoRoles_ShouldReturnEmptyList()
        {
            // Arrange
            var roles = new List<IRoleModel>();

            _mockRoleModel.Setup(m => m.ListRoles(_mockRoleFactory.Object))
                .Returns(roles);

            // Act
            var result = _roleService.ListRoles();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRoleModel.Verify(m => m.ListRoles(_mockRoleFactory.Object), Times.Once);
        }

        #endregion

        #region Insert Tests

        [Fact]
        public async Task Insert_WithValidData_ShouldInsertRole()
        {
            // Arrange
            var roleInfo = new RoleInfo
            {
                Slug = "admin",
                Name = "Administrator"
            };

            var insertedRole = new Mock<IRoleModel>();
            insertedRole.SetupGet(r => r.RoleId).Returns(1);
            insertedRole.SetupGet(r => r.Slug).Returns("admin");
            insertedRole.SetupGet(r => r.Name).Returns(roleInfo.Name);

            _mockRoleModel.SetupProperty(m => m.Slug);
            _mockRoleModel.SetupProperty(m => m.Name);
            _mockRoleModel.SetupGet(m => m.RoleId).Returns(0);
            
            _mockStringClient.Setup(c => c.GenerateSlugAsync(It.IsAny<string>()))
                .ReturnsAsync("admin");
            _mockRoleModel.Setup(m => m.ExistSlug(0, "admin")).Returns(false);
            _mockRoleModel.Setup(m => m.Insert(_mockRoleFactory.Object))
                .Returns(insertedRole.Object);

            // Act
            var result = await _roleService.Insert(roleInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.RoleId);
            Assert.Equal("admin", result.Slug);
            Assert.Equal(roleInfo.Name, result.Name);
            _mockRoleModel.Verify(m => m.Insert(_mockRoleFactory.Object), Times.Once);
            _mockStringClient.Verify(c => c.GenerateSlugAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Insert_WithNullRoleInfo_ShouldThrowException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() => _roleService.Insert(null));
            Assert.Equal("Role is empty", exception.Message);
        }

        [Fact]
        public async Task Insert_WithEmptyName_ShouldThrowException()
        {
            // Arrange
            var roleInfo = new RoleInfo
            {
                Slug = "admin",
                Name = ""
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() => _roleService.Insert(roleInfo));
            Assert.Equal("Role name is required", exception.Message);
        }

        [Fact]
        public async Task Insert_WithExistingSlug_ShouldThrowException()
        {
            // Arrange
            var roleInfo = new RoleInfo
            {
                Slug = "admin",
                Name = "Administrator"
            };

            _mockRoleModel.SetupProperty(m => m.Slug);
            _mockRoleModel.SetupProperty(m => m.Name);
            _mockRoleModel.SetupGet(m => m.RoleId).Returns(0);
            
            _mockStringClient.Setup(c => c.GenerateSlugAsync(It.IsAny<string>()))
                .ReturnsAsync("admin");
            
            // First call in GenerateSlug loop returns false, so slug generation succeeds
            // Second call after GenerateSlug returns true, triggering the exception
            _mockRoleModel.SetupSequence(m => m.ExistSlug(0, "admin"))
                .Returns(false)  // First call in GenerateSlug loop - slug is available
                .Returns(true);  // Second call after GenerateSlug - slug exists now

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() => _roleService.Insert(roleInfo));
            Assert.Equal("Role with slug 'admin' already exists", exception.Message);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidData_ShouldUpdateRole()
        {
            // Arrange
            var roleInfo = new RoleInfo
            {
                RoleId = 1,
                Slug = "super-admin",
                Name = "Super Administrator"
            };

            var existingRole = new Mock<IRoleModel>();
            existingRole.SetupGet(r => r.RoleId).Returns(roleInfo.RoleId);
            existingRole.SetupProperty(r => r.Slug);
            existingRole.SetupProperty(r => r.Name);
            existingRole.Setup(r => r.Update(_mockRoleFactory.Object)).Returns(existingRole.Object);

            _mockRoleModel.Setup(m => m.GetById(roleInfo.RoleId, _mockRoleFactory.Object))
                .Returns(existingRole.Object);
            _mockStringClient.Setup(c => c.GenerateSlugAsync(It.IsAny<string>()))
                .ReturnsAsync("super-admin");
            _mockRoleModel.Setup(m => m.ExistSlug(roleInfo.RoleId, "super-admin")).Returns(false);

            // Act
            var result = await _roleService.Update(roleInfo);

            // Assert
            Assert.NotNull(result);
            existingRole.Verify(r => r.Update(_mockRoleFactory.Object), Times.Once);
            _mockStringClient.Verify(c => c.GenerateSlugAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Update_WithNullRoleInfo_ShouldThrowException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() => _roleService.Update(null));
            Assert.Equal("Role is empty", exception.Message);
        }

        [Fact]
        public async Task Update_WithInvalidRoleId_ShouldThrowException()
        {
            // Arrange
            var roleInfo = new RoleInfo
            {
                RoleId = 0,
                Slug = "admin",
                Name = "Administrator"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() => _roleService.Update(roleInfo));
            Assert.Equal("Valid RoleId is required", exception.Message);
        }

        [Fact]
        public async Task Update_WithNonExistentRole_ShouldThrowException()
        {
            // Arrange
            var roleInfo = new RoleInfo
            {
                RoleId = 999,
                Slug = "admin",
                Name = "Administrator"
            };

            _mockRoleModel.Setup(m => m.GetById(roleInfo.RoleId, _mockRoleFactory.Object))
                .Returns((IRoleModel)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() => _roleService.Update(roleInfo));
            Assert.Equal("Role not found", exception.Message);
        }

        [Fact]
        public async Task Update_WithExistingSlug_ShouldThrowException()
        {
            // Arrange
            var roleInfo = new RoleInfo
            {
                RoleId = 1,
                Slug = "admin",
                Name = "Administrator"
            };

            var existingRole = new Mock<IRoleModel>();
            existingRole.SetupGet(r => r.RoleId).Returns(roleInfo.RoleId);
            existingRole.SetupProperty(r => r.Slug);
            existingRole.SetupProperty(r => r.Name);
            
            _mockRoleModel.Setup(m => m.GetById(roleInfo.RoleId, _mockRoleFactory.Object))
                .Returns(existingRole.Object);
            _mockStringClient.Setup(c => c.GenerateSlugAsync(It.IsAny<string>()))
                .ReturnsAsync("admin");
            
            // First call in GenerateSlug loop returns false (slug generation succeeds)
            // Second call after GenerateSlug returns true (slug exists, triggering exception)
            existingRole.SetupSequence(m => m.ExistSlug(roleInfo.RoleId, "admin"))
                .Returns(false)  // First call in GenerateSlug loop
                .Returns(true);  // Would be called again but exception thrown before
            
            _mockRoleModel.Setup(m => m.ExistSlug(roleInfo.RoleId, "admin")).Returns(true);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UserValidationException>(() => _roleService.Update(roleInfo));
            Assert.Equal("Role with slug 'admin' already exists", exception.Message);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public void Delete_WithValidId_ShouldDeleteRole()
        {
            // Arrange
            long roleId = 1;
            var existingRole = new Mock<IRoleModel>();
            existingRole.SetupGet(r => r.RoleId).Returns(roleId);
            existingRole.SetupGet(r => r.Slug).Returns("admin");
            existingRole.SetupGet(r => r.Name).Returns("Administrator");

            _mockRoleModel.Setup(m => m.GetById(roleId, _mockRoleFactory.Object))
                .Returns(existingRole.Object);

            // Act
            _roleService.Delete(roleId);

            // Assert
            _mockRoleModel.Verify(m => m.Delete(roleId), Times.Once);
        }

        [Fact]
        public void Delete_WithInvalidId_ShouldThrowException()
        {
            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() => _roleService.Delete(0));
            Assert.Equal("RoleId is invalid", exception.Message);
        }

        [Fact]
        public void Delete_WithNonExistentRole_ShouldThrowException()
        {
            // Arrange
            long roleId = 999;
            _mockRoleModel.Setup(m => m.GetById(roleId, _mockRoleFactory.Object))
                .Returns((IRoleModel)null);

            // Act & Assert
            var exception = Assert.Throws<UserValidationException>(() => _roleService.Delete(roleId));
            Assert.Equal("Role not found", exception.Message);
        }

        #endregion
    }
}
