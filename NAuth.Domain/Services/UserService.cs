using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NAuth.Domain.Exceptions;
using NAuth.Domain.Factory;
using NAuth.Domain.Models.Models;
using NAuth.Domain.Services.Interfaces;
using NAuth.DTO.Settings;
using NAuth.DTO.User;
using NAuth.Infra.Interfaces;
using zTools.ACL.Interfaces;
using zTools.DTO.MailerSend;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NAuth.Domain.Services
{
    public class ExternalClients
    {
        public IMailClient MailClient { get; }
        public IFileClient FileClient { get; }
        public IStringClient StringClient { get; }
        public IDocumentClient DocumentClient { get; }

        public ExternalClients(
            IMailClient mailClient,
            IFileClient fileClient,
            IStringClient stringClient,
            IDocumentClient documentClient)
        {
            MailClient = mailClient;
            FileClient = fileClient;
            StringClient = stringClient;
            DocumentClient = documentClient;
        }
    }

    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly NAuthSetting _nauthSetting;
        private readonly DomainFactory _factories;
        private readonly ExternalClients _clients;
        private readonly IUnitOfWork _unitOfWork;

        private const string UserNotFoundMessage = "User not found";

        public UserService(
            ILogger<UserService> logger,
            IOptions<NAuthSetting> nauthSetting,
            DomainFactory factories,
            ExternalClients clients,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _nauthSetting = nauthSetting.Value;
            _factories = factories;
            _clients = clients;
            _unitOfWork = unitOfWork;
        }

        public string GetBucketName()
        {
            return _nauthSetting.BucketName;
        }

        public IUserModel LoginWithEmail(string email, string password)
        {
            return _factories.UserFactory.BuildUserModel().LoginWithEmail(email, password, _factories.UserFactory);
        }

        public async Task<string> CreateToken(long userId, string ipAddress, string userAgent, string fingerprint)
        {
            _logger.LogTrace(
                "Creating JWT token for user with ID={UserId}, IP={IpAddress}, UserAgent={UserAgent} and {Fingerprint}",
                userId, ipAddress, userAgent, fingerprint
            );
            ValidateTokenParameters(userId, ipAddress, userAgent, fingerprint);

            var user = _factories.UserFactory.BuildUserModel().GetById(userId, _factories.UserFactory);
            if (user == null)
            {
                throw new UserValidationException(UserNotFoundMessage);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_nauthSetting.JwtSecret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("userId", userId.ToString()),
                new Claim("hash", user.Hash),
                new Claim("ipAddress", ipAddress),
                new Claim("userAgent", userAgent),
                new Claim("fingerprint", fingerprint),
                new Claim("isAdmin", user.IsAdmin.ToString())
            };

            // Adicionar roles como claims
            var roles = user.ListRoles(userId, _factories.RoleFactory);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Slug));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMonths(2),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256
                ),
                Issuer = "NAuth",
                Audience = "NAuth.API"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            _logger.LogInformation("JWT token created successfully for user {UserId}, expires at {ExpiresAt}",
                userId, tokenDescriptor.Expires);

            return await Task.FromResult(tokenString);
        }

        private static void ValidateTokenParameters(long userId, string ipAddress, string userAgent, string fingerprint)
        {
            if (userId <= 0)
            {
                throw new UserValidationException("UserId is invalid");
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                throw new UserValidationException("IP Address is empty");
            }
            if (string.IsNullOrEmpty(userAgent))
            {
                throw new UserValidationException("User Agent is empty");
            }
            if (string.IsNullOrEmpty(fingerprint))
            {
                throw new UserValidationException("Fingerprint is empty");
            }
        }

        public bool HasPassword(long userId)
        {
            return _factories.UserFactory.BuildUserModel().HasPassword(userId, _factories.UserFactory);
        }

        public void ChangePasswordUsingHash(string recoveryHash, string newPassword)
        {
            if (string.IsNullOrEmpty(recoveryHash))
            {
                throw new UserValidationException("Recovery hash cant be empty");
            }
            if (string.IsNullOrEmpty(newPassword))
            {
                throw new UserValidationException("Password cant be empty");
            }

            _logger.LogTrace("Changing password using recovery hash: {@recoveryHash}, new password: {@newPassword}", recoveryHash, newPassword);

            var md = _factories.UserFactory.BuildUserModel();
            var user = md.GetByRecoveryHash(recoveryHash, _factories.UserFactory);
            if (user == null)
            {
                throw new UserValidationException(UserNotFoundMessage);
            }
            md.ChangePassword(user.UserId, newPassword, _factories.UserFactory);

            _logger.LogTrace("Password successful changed using recovery hash: {@recoveryHash}", recoveryHash);
        }

        public void ChangePassword(long userId, string oldPassword, string newPassword)
        {
            bool hasPassword = HasPassword(userId);
            if (hasPassword && string.IsNullOrEmpty(oldPassword))
            {
                throw new UserValidationException("Old password cant be empty");
            }
            if (string.IsNullOrEmpty(newPassword))
            {
                throw new UserValidationException("New password cant be empty");
            }
            var md = _factories.UserFactory.BuildUserModel();
            var user = md.GetById(userId, _factories.UserFactory);
            if (user == null)
            {
                throw new UserValidationException(UserNotFoundMessage);
            }
            if (string.IsNullOrEmpty(user.Email))
            {
                throw new UserValidationException("To change password you need a email");
            }
            if (hasPassword)
            {
                var mdUser = md.LoginWithEmail(user.Email, oldPassword, _factories.UserFactory);
                if (mdUser == null)
                {
                    throw new UserValidationException("Email or password is wrong");
                }
            }
            _logger.LogTrace("Changing password using old password: email: {0}, old password: {1}, new password: {2}", user.Email, oldPassword, newPassword);
            md.ChangePassword(user.UserId, newPassword, _factories.UserFactory);
            _logger.LogTrace("Password successful changed using old password");
        }

        public async Task<bool> SendRecoveryEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new UserValidationException("Email cant be empty");
            }
            var md = _factories.UserFactory.BuildUserModel();
            var user = md.GetByEmail(email, _factories.UserFactory);
            if (user == null)
            {
                throw new UserValidationException(UserNotFoundMessage);
            }
            var recoveryHash = md.GenerateRecoveryHash(user.UserId, _factories.UserFactory);
            var recoveryUrl = $"https://nochainswap.org/recoverypassword/{recoveryHash}";

            var mail = BuildRecoveryEmail(user, recoveryUrl);
            await _clients.MailClient.SendmailAsync(mail);
            return await Task.FromResult(true);
        }

        private static MailerInfo BuildRecoveryEmail(IUserModel user, string recoveryUrl)
        {
            var textMessage =
                $"Hi {user.Name},\r\n\r\n" +
                "We received a request to reset your password. If you made this request, " +
                "please click the link below to reset your password:\r\n\r\n" +
                recoveryUrl + "\r\n\r\n" +
                "If you didn't request a password reset, please ignore this email or contact " +
                "our support team if you have any concerns.\r\n\r\n" +
                "Best regards,\r\n" +
                "NoChainSwap Team";
            var htmlMessage =
                $"Hi <b>{user.Name}</b>,<br />\r\n<br />\r\n" +
                "We received a request to reset your password. If you made this request, " +
                "please click the link below to reset your password:<br />\r\n<br />\r\n" +
                $"<a href=\"{recoveryUrl}\">{recoveryUrl}</a><br />\r\n<br />\r\n" +
                "If you didn't request a password reset, please ignore this email or contact " +
                "our support team if you have any concerns.<br />\r\n<br />\r\n" +
                "Best regards,<br />\r\n" +
                "<b>NoChainSwap Team</b>";

            return new MailerInfo
            {
                From = new MailerRecipientInfo
                {
                    Email = "contact@nochainswap.org",
                    Name = "NoChainSwap Mailmaster"
                },
                To = new List<MailerRecipientInfo> {
                    new MailerRecipientInfo {
                        Email = user.Email,
                        Name = user.Name ?? user.Email
                    }
                },
                Subject = "[NoChainSwap] Password Recovery Email",
                Text = textMessage,
                Html = htmlMessage
            };
        }

        private async Task<string> GenerateSlug(IUserModel md)
        {
            string newSlug;
            int c = 0;
            do
            {
                newSlug = await _clients.StringClient.GenerateSlugAsync(!string.IsNullOrEmpty(md.Slug) ? md.Slug : md.Name);
                if (c > 0)
                {
                    newSlug += c.ToString();
                }
                c++;
            } while (md.ExistSlug(md.UserId, newSlug));
            return newSlug;
        }

        private void InsertPhones(UserInsertedInfo user, long userId)
        {
            if (user.Phones != null && user.Phones.Any())
            {
                foreach (var phone in user.Phones)
                {
                    var modelPhone = _factories.PhoneFactory.BuildUserPhoneModel();
                    modelPhone.UserId = userId;
                    modelPhone.Phone = phone.Phone;
                    modelPhone.Insert(_factories.PhoneFactory);
                }
            }
        }

        private void InsertAddresses(UserInsertedInfo user, long userId)
        {
            if (user.Addresses != null && user.Addresses.Any())
            {
                foreach (var addr in user.Addresses)
                {
                    var modelAddr = _factories.AddressFactory.BuildUserAddressModel();
                    modelAddr.UserId = userId;
                    modelAddr.ZipCode = addr.ZipCode;
                    modelAddr.Address = addr.Address;
                    modelAddr.Complement = addr.Complement;
                    modelAddr.Neighborhood = addr.Neighborhood;
                    modelAddr.City = addr.City;
                    modelAddr.State = addr.State;
                    modelAddr.Insert(_factories.AddressFactory);
                }
            }
        }

        private void InsertPhones(UserInfo user)
        {
            if (user.Phones != null && user.Phones.Any())
            {
                foreach (var phone in user.Phones)
                {
                    var modelPhone = _factories.PhoneFactory.BuildUserPhoneModel();
                    modelPhone.UserId = user.UserId;
                    modelPhone.Phone = phone.Phone;
                    modelPhone.Insert(_factories.PhoneFactory);
                }
            }
        }

        private void InsertAddresses(UserInfo user)
        {
            if (user.Addresses != null && user.Addresses.Any())
            {
                foreach (var addr in user.Addresses)
                {
                    var modelAddr = _factories.AddressFactory.BuildUserAddressModel();
                    modelAddr.UserId = user.UserId;
                    modelAddr.ZipCode = addr.ZipCode;
                    modelAddr.Address = addr.Address;
                    modelAddr.Complement = addr.Complement;
                    modelAddr.Neighborhood = addr.Neighborhood;
                    modelAddr.City = addr.City;
                    modelAddr.State = addr.State;
                    modelAddr.Insert(_factories.AddressFactory);
                }
            }
        }

        private void InsertRoles(UserInsertedInfo user, long userId)
        {
            if (user.Roles != null && user.Roles.Any())
            {
                var userModel = _factories.UserFactory.BuildUserModel();
                foreach (var role in user.Roles)
                {
                    userModel.AddRole(userId, role.RoleId);
                }
            }
        }

        private void InsertRoles(UserInfo user)
        {
            if (user.Roles != null && user.Roles.Any())
            {
                var userModel = _factories.UserFactory.BuildUserModel();
                foreach (var role in user.Roles)
                {
                    userModel.AddRole(user.UserId, role.RoleId);
                }
            }
        }

        private void ValidateRoles(UserInsertedInfo user)
        {
            if (user.Roles == null)
            {
                return;
            }

            var roleModel = _factories.RoleFactory.BuildRoleModel();
            var roleIds = user.Roles.Select(role => role.RoleId);
            foreach (var roleId in roleIds)
            {
                if (roleId <= 0)
                {
                    throw new UserValidationException("RoleId is invalid");
                }

                var existingRole = roleModel.GetById(roleId, _factories.RoleFactory);
                if (existingRole == null)
                {
                    throw new UserValidationException($"Role with ID {roleId} does not exist");
                }
            }
        }

        private void ValidateRoles(UserInfo user)
        {
            if (user.Roles == null)
            {
                return;
            }

            var roleModel = _factories.RoleFactory.BuildRoleModel();
            var roleIds = user.Roles.Select(role => role.RoleId);
            foreach (var roleId in roleIds)
            {
                if (roleId <= 0)
                {
                    throw new UserValidationException("RoleId is invalid");
                }

                var existingRole = roleModel.GetById(roleId, _factories.RoleFactory);
                if (existingRole == null)
                {
                    throw new UserValidationException($"Role with ID {roleId} does not exist");
                }
            }
        }

        private async Task ValidatePhones(UserInsertedInfo user)
        {
            if (user.Phones == null)
            {
                return;
            }
            foreach (var phone in user.Phones)
            {
                if (string.IsNullOrEmpty(phone.Phone))
                {
                    throw new UserValidationException("Phone is empty");
                }
                else
                {
                    phone.Phone = await _clients.StringClient.OnlyNumbersAsync(phone.Phone.Trim());
                    if (string.IsNullOrEmpty(phone.Phone))
                    {
                        throw new UserValidationException($"{phone.Phone} is not a valid phone");
                    }
                }
            }
        }

        private async Task ValidatePhones(UserInfo user)
        {
            if (user.Phones == null)
            {
                return;
            }
            foreach (var phone in user.Phones)
            {
                if (string.IsNullOrEmpty(phone.Phone))
                {
                    throw new UserValidationException("Phone is empty");
                }
                else
                {
                    phone.Phone = await _clients.StringClient.OnlyNumbersAsync(phone.Phone.Trim());
                    if (string.IsNullOrEmpty(phone.Phone))
                    {
                        throw new UserValidationException($"{phone.Phone} is not a valid phone");
                    }
                }
            }
        }

        private async Task ValidateAddresses(UserInsertedInfo user)
        {
            if (user.Addresses == null)
            {
                return;
            }

            foreach (var addr in user.Addresses)
            {
                ValidateAddressFields(addr);
                await ValidateAndNormalizeZipCode(addr);
            }
        }

        private async Task ValidateAddresses(UserInfo user)
        {
            if (user.Addresses == null)
            {
                return;
            }

            foreach (var addr in user.Addresses)
            {
                ValidateAddressFields(addr);
                await ValidateAndNormalizeZipCode(addr);
            }
        }

        private static void ValidateAddressFields(UserAddressInfo addr)
        {
            if (string.IsNullOrEmpty(addr.ZipCode))
            {
                throw new UserValidationException("ZipCode is empty");
            }
            if (string.IsNullOrEmpty(addr.Address))
            {
                throw new UserValidationException("Address is empty");
            }
            if (string.IsNullOrEmpty(addr.Complement))
            {
                throw new UserValidationException("Address is empty");
            }
            if (string.IsNullOrEmpty(addr.Neighborhood))
            {
                throw new UserValidationException("Neighborhood is empty");
            }
            if (string.IsNullOrEmpty(addr.City))
            {
                throw new UserValidationException("City is empty");
            }
            if (string.IsNullOrEmpty(addr.State))
            {
                throw new UserValidationException("State is empty");
            }
        }

        private async Task ValidateAndNormalizeZipCode(UserAddressInfo addr)
        {
            if (!string.IsNullOrEmpty(addr.ZipCode))
            {
                addr.ZipCode = await _clients.StringClient.OnlyNumbersAsync(addr.ZipCode);
                if (string.IsNullOrEmpty(addr.ZipCode))
                {
                    throw new UserValidationException($"{addr.ZipCode} is not a valid zip code");
                }
            }
        }

        public async Task<IUserModel> Insert(UserInsertedInfo user)
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    var model = _factories.UserFactory.BuildUserModel();
                    await ValidateUserForInsert(user, model);

                    model.Slug = user.Slug;
                    model.Name = user.Name;
                    model.Email = user.Email;
                    model.BirthDate = user.BirthDate;
                    model.IdDocument = user.IdDocument;
                    model.PixKey = user.PixKey;
                    model.Status = Enums.UserStatus.Active;
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;
                    model.Hash = GetUniqueToken();
                    model.Slug = await GenerateSlug(model);

                    var md = model.Insert(_factories.UserFactory);

                    InsertPhones(user, md.UserId);
                    InsertAddresses(user, md.UserId);
                    InsertRoles(user, md.UserId);

                    md.ChangePassword(md.UserId, user.Password, _factories.UserFactory);

                    transaction.Commit();

                    _logger.LogInformation("User {UserId} inserted successfully with email {Email}", md.UserId, md.Email);

                    return md;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inserting user with email {Email}", user.Email);
                    transaction.Rollback();
                    throw new InvalidOperationException(ex.Message, ex);
                }
            }
        }

        private async Task ValidateUserForInsert(UserInsertedInfo user, IUserModel model)
        {
            if (string.IsNullOrEmpty(user.Name))
            {
                throw new UserValidationException("Name is empty");
            }
            if (string.IsNullOrEmpty(user.Email))
            {
                throw new UserValidationException("Email is empty");
            }
            else
            {
                if (!await _clients.MailClient.IsValidEmailAsync(user.Email))
                {
                    throw new UserValidationException("Email is not valid");
                }
                var userWithEmail = model.GetByEmail(user.Email, _factories.UserFactory);
                if (userWithEmail != null)
                {
                    throw new UserValidationException("User with email already registered");
                }
            }
            if (string.IsNullOrEmpty(user.Password))
            {
                throw new UserValidationException("Password is empty");
            }
            if (!string.IsNullOrEmpty(user.IdDocument))
            {
                user.IdDocument = await _clients.StringClient.OnlyNumbersAsync(user.IdDocument);
                if (!await _clients.DocumentClient.validarCpfOuCnpjAsync(user.IdDocument))
                {
                    throw new UserValidationException($"{user.IdDocument} is not a valid CPF or CNPJ");
                }
            }
            await ValidatePhones(user);
            await ValidateAddresses(user);
            ValidateRoles(user);
        }

        public async Task<IUserModel> Update(UserInfo user)
        {
            using (var transaction = _unitOfWork.BeginTransaction())
            {
                try
                {
                    IUserModel model = null;
                    if (!(user.UserId > 0))
                    {
                        throw new UserValidationException(UserNotFoundMessage);
                    }
                    if (string.IsNullOrEmpty(user.Name))
                    {
                        throw new UserValidationException("Name is empty");
                    }
                    model = _factories.UserFactory.BuildUserModel().GetById(user.UserId, _factories.UserFactory);
                    if (model == null)
                    {
                        throw new UserValidationException("User not exists");
                    }

                    await ValidateUserForUpdate(user, model);

                    model.Slug = user.Slug;
                    model.Name = user.Name;
                    model.Email = user.Email;
                    model.BirthDate = user.BirthDate;
                    model.IdDocument = user.IdDocument;
                    model.PixKey = user.PixKey;
                    model.Status = (NAuth.Domain.Enums.UserStatus)user.Status;
                    model.UpdatedAt = DateTime.Now;
                    model.Slug = await GenerateSlug(model);

                    model.Update(_factories.UserFactory);

                    var modelPhone = _factories.PhoneFactory.BuildUserPhoneModel();
                    modelPhone.DeleteAllByUser(model.UserId);
                    InsertPhones(user);

                    var modelAddr = _factories.AddressFactory.BuildUserAddressModel();
                    modelAddr.DeleteAllByUser(model.UserId);
                    InsertAddresses(user);

                    model.RemoveAllRoles(model.UserId);
                    InsertRoles(user);

                    transaction.Commit();

                    _logger.LogInformation("User {UserId} updated successfully with email {Email}", model.UserId, model.Email);

                    return model;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user {UserId}", user.UserId);
                    transaction.Rollback();
                    throw new InvalidOperationException(ex.Message, ex);
                }
            }
        }

        private async Task ValidateUserForUpdate(UserInfo user, IUserModel model)
        {
            if (string.IsNullOrEmpty(user.Email))
            {
                throw new UserValidationException("Email is empty");
            }
            else
            {
                if (!await _clients.MailClient.IsValidEmailAsync(user.Email))
                {
                    throw new UserValidationException("Email is not valid");
                }
                var userWithEmail = model.GetByEmail(user.Email, _factories.UserFactory);
                if (userWithEmail != null && userWithEmail.UserId != model.UserId)
                {
                    throw new UserValidationException("User with email already registered");
                }
            }
            if (!string.IsNullOrEmpty(user.IdDocument))
            {
                user.IdDocument = await _clients.StringClient.OnlyNumbersAsync(user.IdDocument);
                if (!await _clients.DocumentClient.validarCpfOuCnpjAsync(user.IdDocument))
                {
                    throw new UserValidationException($"{user.IdDocument} is not a valid CPF or CNPJ");
                }
            }
            await ValidatePhones(user);
            await ValidateAddresses(user);
            ValidateRoles(user);
        }

        public IUserModel GetUserByEmail(string email)
        {
            return _factories.UserFactory.BuildUserModel().GetByEmail(email, _factories.UserFactory);
        }

        public IUserModel GetUserByID(long userId)
        {
            return _factories.UserFactory.BuildUserModel().GetById(userId, _factories.UserFactory);
        }

        public UserSessionInfo GetUserInSession(HttpContext httpContext)
        {
            if (httpContext?.User?.Claims == null || !httpContext.User.Claims.Any())
            {
                return null;
            }

            var claims = httpContext.User.Claims.ToList();

            var userInfo = new UserSessionInfo
            {
                UserId = long.TryParse(claims.FirstOrDefault(c => c.Type == "userId")?.Value, out var userId) ? userId : 0,
                Name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
                Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                Hash = claims.FirstOrDefault(c => c.Type == "hash")?.Value,
                IpAddress = claims.FirstOrDefault(c => c.Type == "ipAddress")?.Value,
                UserAgent = claims.FirstOrDefault(c => c.Type == "userAgent")?.Value,
                Fingerprint = claims.FirstOrDefault(c => c.Type == "fingerprint")?.Value,
                IsAdmin = bool.TryParse(claims.FirstOrDefault(c => c.Type == "isAdmin")?.Value, out var isAdmin) && isAdmin,
                Roles = claims.Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList()
            };

            return userInfo;
        }

        public async Task<UserInfo> GetUserInfoFromModel(IUserModel md)
        {
            if (md == null)
                return null;
            return new UserInfo
            {
                UserId = md.UserId,
                Hash = md.Hash,
                Slug = md.Slug,
                ImageUrl = await _clients.FileClient.GetFileUrlAsync(GetBucketName(), md.Image),
                Name = md.Name,
                Email = md.Email,
                IdDocument = md.IdDocument,
                PixKey = md.PixKey,
                BirthDate = md.BirthDate,
                Status = (int)md.Status,
                CreatedAt = md.CreatedAt,
                UpdatedAt = md.UpdatedAt,
                IsAdmin = md.IsAdmin,
                Roles = md.ListRoles(md.UserId, _factories.RoleFactory)
                    .Select(x => new RoleInfo
                    {
                        RoleId = x.RoleId,
                        Slug = x.Slug,
                        Name = x.Name
                    }).ToList(),
                Phones = _factories.PhoneFactory.BuildUserPhoneModel()
                    .ListByUser(md.UserId, _factories.PhoneFactory)
                    .Select(x => new UserPhoneInfo
                    {
                        Phone = x.Phone
                    }).ToList(),
                Addresses = _factories.AddressFactory.BuildUserAddressModel()
                    .ListByUser(md.UserId, _factories.AddressFactory)
                    .Select(x => new UserAddressInfo
                    {
                        ZipCode = x.ZipCode,
                        Address = x.Address,
                        Complement = x.Complement,
                        Neighborhood = x.Neighborhood,
                        City = x.City,
                        State = x.State
                    }).ToList()
            };
        }

        private static string GetUniqueToken()
        {
            using (var crypto = new RNGCryptoServiceProvider())
            {
                int length = 100;
                string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_";
                byte[] data = new byte[length];

                // If chars.Length isn't a power of 2 then there is a bias if we simply use the modulus operator. The first characters of chars will be more probable than the last ones.
                // buffer used if we encounter an unusable random byte. We will regenerate it in this buffer
                byte[] buffer = null;

                // Maximum random number that can be used without introducing a bias
                int maxRandom = byte.MaxValue - (byte.MaxValue + 1) % chars.Length;

                crypto.GetBytes(data);

                char[] result = new char[length];

                for (int i = 0; i < length; i++)
                {
                    byte value = data[i];

                    while (value > maxRandom)
                    {
                        if (buffer == null)
                        {
                            buffer = new byte[1];
                        }

                        crypto.GetBytes(buffer);
                        value = buffer[0];
                    }

                    result[i] = chars[value % chars.Length];
                }

                return new string(result);
            }
        }

        public IUserModel GetByStripeId(string stripeId)
        {
            return _factories.UserFactory.BuildUserModel().GetByStripeId(stripeId, _factories.UserFactory);
        }

        public IUserModel GetBySlug(string slug)
        {
            return _factories.UserFactory.BuildUserModel().GetBySlug(slug, _factories.UserFactory);
        }

        public IList<IUserModel> ListUsers()
        {
            return _factories.UserFactory.BuildUserModel().ListUsers(_factories.UserFactory).ToList();
        }

        public PagedResult<UserInfo> SearchUsers(string searchTerm, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            int totalCount;
            var userModels = _factories.UserFactory.BuildUserModel()
                .SearchUsers(searchTerm, page, pageSize, out totalCount, _factories.UserFactory);

            var userInfoList = userModels.Select(x => GetUserInfoFromModel(x).Result).ToList();

            return new PagedResult<UserInfo>(userInfoList, page, pageSize, totalCount);
        }
    }
}
