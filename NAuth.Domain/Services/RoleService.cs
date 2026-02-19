using Microsoft.Extensions.Logging;
using NAuth.Domain.Exceptions;
using NAuth.Domain.Factory;
using NAuth.Domain.Models.Models;
using NAuth.Domain.Services.Interfaces;
using NAuth.DTO.User;
using zTools.ACL.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NAuth.Domain.Services
{
    public class RoleService : IRoleService
    {
        private readonly ILogger<RoleService> _logger;
        private readonly DomainFactory _factories;
        private readonly IStringClient _stringClient;

        private const string RoleNotFoundMessage = "Role not found";

        public RoleService(
            ILogger<RoleService> logger,
            DomainFactory factories,
            IStringClient stringClient)
        {
            _logger = logger;
            _factories = factories;
            _stringClient = stringClient;
        }

        public IRoleModel GetById(long roleId)
        {
            if (roleId <= 0)
            {
                throw new UserValidationException("RoleId is invalid");
            }

            var roleModel = _factories.RoleFactory.BuildRoleModel();
            var role = roleModel.GetById(roleId, _factories.RoleFactory);

            if (role == null)
            {
                throw new UserValidationException(RoleNotFoundMessage);
            }

            return role;
        }

        public IRoleModel GetBySlug(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                throw new UserValidationException("Slug is empty");
            }

            var roleModel = _factories.RoleFactory.BuildRoleModel();
            var role = roleModel.GetBySlug(slug, _factories.RoleFactory);

            if (role == null)
            {
                throw new UserValidationException(RoleNotFoundMessage);
            }

            return role;
        }

        public IEnumerable<IRoleModel> ListRoles()
        {
            var roleModel = _factories.RoleFactory.BuildRoleModel();
            return roleModel.ListRoles(_factories.RoleFactory);
        }

        public async Task<IRoleModel> Insert(RoleInfo roleInfo)
        {
            ValidateRoleInfo(roleInfo, isUpdate: false);

            var roleModel = _factories.RoleFactory.BuildRoleModel();

            roleModel.Slug = roleInfo.Slug;
            roleModel.Name = roleInfo.Name;
            roleModel.Slug = await GenerateSlug(roleModel);

            if (roleModel.ExistSlug(0, roleModel.Slug))
            {
                throw new UserValidationException($"Role with slug '{roleModel.Slug}' already exists");
            }

            var newRole = roleModel.Insert(_factories.RoleFactory);

            _logger.LogInformation("Role successfully inserted (RoleId: {RoleId}, Slug: {Slug}, Name: {Name})",
                newRole.RoleId, newRole.Slug, newRole.Name);

            return newRole;
        }

        public async Task<IRoleModel> Update(RoleInfo roleInfo)
        {
            ValidateRoleInfo(roleInfo, isUpdate: true);

            var roleModel = _factories.RoleFactory.BuildRoleModel();
            var existingRole = roleModel.GetById(roleInfo.RoleId, _factories.RoleFactory);

            if (existingRole == null)
            {
                throw new UserValidationException(RoleNotFoundMessage);
            }

            existingRole.Slug = roleInfo.Slug;
            existingRole.Name = roleInfo.Name;
            existingRole.Slug = await GenerateSlug(existingRole);

            if (roleModel.ExistSlug(roleInfo.RoleId, existingRole.Slug))
            {
                throw new UserValidationException($"Role with slug '{existingRole.Slug}' already exists");
            }

            var updatedRole = existingRole.Update(_factories.RoleFactory);

            _logger.LogInformation("Role successfully updated (RoleId: {RoleId}, Slug: {Slug}, Name: {Name})",
                updatedRole.RoleId, updatedRole.Slug, updatedRole.Name);

            return updatedRole;
        }

        public void Delete(long roleId)
        {
            if (roleId <= 0)
            {
                throw new UserValidationException("RoleId is invalid");
            }

            var roleModel = _factories.RoleFactory.BuildRoleModel();
            var role = roleModel.GetById(roleId, _factories.RoleFactory);

            if (role == null)
            {
                throw new UserValidationException(RoleNotFoundMessage);
            }

            roleModel.Delete(roleId);

            _logger.LogInformation("Role successfully deleted (RoleId: {RoleId}, Slug: {Slug}, Name: {Name})",
                role.RoleId, role.Slug, role.Name);
        }

        private async Task<string> GenerateSlug(IRoleModel roleModel)
        {
            string newSlug;
            int counter = 0;
            do
            {
                newSlug = await _stringClient.GenerateSlugAsync(!string.IsNullOrEmpty(roleModel.Slug) ? roleModel.Slug : roleModel.Name);
                if (counter > 0)
                {
                    newSlug += counter.ToString();
                }
                counter++;
            } while (roleModel.ExistSlug(roleModel.RoleId, newSlug));
            return newSlug;
        }

        private static void ValidateRoleInfo(RoleInfo roleInfo, bool isUpdate)
        {
            if (roleInfo == null)
            {
                throw new UserValidationException("Role is empty");
            }

            if (isUpdate && roleInfo.RoleId <= 0)
            {
                throw new UserValidationException("Valid RoleId is required");
            }

            if (string.IsNullOrWhiteSpace(roleInfo.Name))
            {
                throw new UserValidationException("Role name is required");
            }
        }
    }
}
