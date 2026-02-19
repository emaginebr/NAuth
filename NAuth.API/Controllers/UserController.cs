using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NAuth.Domain.Services.Interfaces;
using NAuth.DTO.User;
using zTools.ACL.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NAuth.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private const string NotAuthorizedMessage = "Not Authorized";
        private const string ExceptionOccurredMessage = "An exception occurred: {Message}";
        private const string UserIsEmptyMessage = "User is empty";
        private const string UserNotFoundMessage = "User Not Found";

        private readonly ILogger<UserController> _logger;
        private readonly IUserService _userService;
        private readonly IFileClient _fileClient;

        public UserController(
            ILogger<UserController> logger,
            IUserService userService,
            IFileClient fileClient
        )
        {
            _logger = logger;
            _userService = userService;
            _fileClient = fileClient;
        }

        [Authorize]
        [HttpPost("uploadImageUser")]
        public async Task<ActionResult<string>> UploadImageUser(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogError("No file uploaded");
                    return BadRequest("No file uploaded");
                }
                var userSession = _userService.GetUserInSession(HttpContext);
                if (userSession == null)
                {
                    _logger.LogError(NotAuthorizedMessage);
                    return Unauthorized(NotAuthorizedMessage);
                }

                var fileName = await _fileClient.UploadFileAsync(_userService.GetBucketName(), file);
                _logger.LogInformation("File upload successfully, filename: {@filename}", fileName);
                var fileUrl = await _fileClient.GetFileUrlAsync(_userService.GetBucketName(), fileName);
                return Ok(fileUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("getMe")]
        public async Task<ActionResult<UserInfo>> GetMe()
        {
            try
            {
                var userSession = _userService.GetUserInSession(HttpContext);
                if (userSession == null)
                {
                    _logger.LogError(NotAuthorizedMessage);
                    return Unauthorized(NotAuthorizedMessage);
                }
                var user = _userService.GetUserByID(userSession.UserId);
                if (user == null)
                {
                    _logger.LogError("User Not Found with ID {UserId}", userSession.UserId);
                    return NotFound(UserNotFoundMessage);
                }

                _logger.LogInformation("getMe() = User(UserId: {@ID}, Name: {@name})", user.UserId, user.Name);

                return Ok(await _userService.GetUserInfoFromModel(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("getById/{userId}")]
        public async Task<ActionResult<UserInfo>> GetById(long userId)
        {
            try
            {
                var user = _userService.GetUserByID(userId);
                if (user == null)
                {
                    _logger.LogError("User Not Found with ID {UserId}", userId);
                    return NotFound(UserNotFoundMessage);
                }

                _logger.LogInformation("GetById(userId: {@userId}) = User(UserId: {@ID}, Email: {@email}, Name: {@name})", userId, user.UserId, user.Email, user.Name);

                return Ok(await _userService.GetUserInfoFromModel(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("getByEmail/{email}")]
        public async Task<ActionResult<UserInfo>> GetByEmail(string email)
        {
            try
            {
                var user = _userService.GetUserByEmail(email);
                if (user == null)
                {
                    _logger.LogError("User with email not found {Email}", email);
                    return NotFound("User with email not found");
                }

                _logger.LogInformation("GetByEmail(email: {@email}) = User(UserId: {@ID}, Email: {@email}, Name: {@name})", email, user.UserId, user.Email, user.Name);

                return Ok(await _userService.GetUserInfoFromModel(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("getBySlug/{slug}")]
        public async Task<ActionResult<UserInfo>> GetBySlug(string slug)
        {
            try
            {
                var user = _userService.GetBySlug(slug);
                if (user == null)
                {
                    _logger.LogError("User with slug not found {Slug}", slug);
                    return NotFound("User with slug not found");
                }

                _logger.LogInformation("GetBySlug(slug: {@slug}) = User(UserId: {@ID}, Email: {@email}, Name: {@name})", slug, user.UserId, user.Email, user.Name);

                return Ok(await _userService.GetUserInfoFromModel(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("insert")]
        public async Task<ActionResult<UserInfo>> Insert([FromBody] UserInsertedInfo user)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogError(UserIsEmptyMessage);
                    return BadRequest(UserIsEmptyMessage);
                }
                var newUser = await _userService.Insert(user);

                _logger.LogInformation("User sucessfully inserted (UserId: {@ID}, Email: {@email}, Name: {@name})", newUser.UserId, newUser.Email, newUser.Name);

                return Ok(await _userService.GetUserInfoFromModel(newUser));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPost("update")]
        public async Task<ActionResult<UserInfo>> Update([FromBody] UserInfo user)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogError(UserIsEmptyMessage);
                    return BadRequest(UserIsEmptyMessage);
                }
                var userSession = _userService.GetUserInSession(HttpContext);
                if (userSession == null)
                {
                    _logger.LogError(NotAuthorizedMessage);
                    return Unauthorized(NotAuthorizedMessage);
                }
                if (userSession.UserId != user.UserId && !userSession.IsAdmin)
                {
                    _logger.LogError("Only can update your user ({UserSession} != {UserId})", userSession.UserId, user.UserId);
                    return StatusCode(403, "Only can update your user");
                }

                var updatedUser = await _userService.Update(user);

                _logger.LogInformation("User sucessfully updated (UserId: {@ID}, Email: {@email}, Name: {@name})", updatedUser.UserId, updatedUser.Email, updatedUser.Name);

                return Ok(await _userService.GetUserInfoFromModel(updatedUser));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("loginWithEmail")]
        public async Task<ActionResult<object>> LoginWithEmail([FromBody] LoginParam param)
        {
            try
            {
                var user = _userService.LoginWithEmail(param.Email, param.Password);
                if (user == null)
                {
                    _logger.LogTrace("Email: {Email}, Password: {Password}", param.Email, param.Password);
                    _logger.LogError("Email or password is wrong");
                    return Unauthorized("Email or password is wrong");
                }
                var fingerprint = Request.Headers["X-Device-Fingerprint"].FirstOrDefault();
                var userAgent = Request.Headers["User-Agent"].FirstOrDefault();

                var ipAddr = Request.HttpContext.Connection?.RemoteIpAddress?.ToString();

                if (Request.Headers?.ContainsKey("X-Forwarded-For") == true)
                {
                    ipAddr = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                }
                var token = await _userService.CreateToken(user.UserId, ipAddr, userAgent, fingerprint);

                _logger.LogInformation("Token sucessfully created (Token: {@token})", token);

                return Ok(new
                {
                    token = token,
                    user = await _userService.GetUserInfoFromModel(user)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("hasPassword")]
        public ActionResult<bool> HasPassword()
        {
            try
            {
                var userSession = _userService.GetUserInSession(HttpContext);
                if (userSession == null)
                {
                    _logger.LogError(NotAuthorizedMessage);
                    return Unauthorized(NotAuthorizedMessage);
                }
                var user = _userService.GetUserByID(userSession.UserId);
                if (user == null)
                {
                    _logger.LogError("User with ID not found {UserId}", userSession.UserId);
                    return NotFound(UserNotFoundMessage);
                }

                var hasPassword = _userService.HasPassword(user.UserId);
                _logger.LogInformation("User has password: {@hasPassword}", hasPassword);

                return Ok(hasPassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPost("changePassword")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public ActionResult ChangePassword([FromBody] ChangePasswordParam param)
        {
            try
            {
                var userSession = _userService.GetUserInSession(HttpContext);
                if (userSession == null)
                {
                    _logger.LogError(NotAuthorizedMessage);
                    return Unauthorized(NotAuthorizedMessage);
                }
                var user = _userService.GetUserByID(userSession.UserId);
                if (user == null)
                {
                    _logger.LogError("User with ID not found {UserId}", userSession.UserId);
                    return NotFound(UserNotFoundMessage);
                }

                _userService.ChangePassword(user.UserId, param.OldPassword, param.NewPassword);

                _logger.LogInformation("Password successfully changed, UserId: {@userId}, Email: {@email}, Name: {@name}", user.UserId, user.Email, user.Name);

                return Ok("Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("sendRecoveryMail/{email}")]
        public async Task<ActionResult> SendRecoveryMail(string email)
        {
            try
            {
                var user = _userService.GetUserByEmail(email);
                if (user == null)
                {
                    _logger.LogError("User with email not found {Email}", email);
                    return NotFound("Email not exist");
                }

                await _userService.SendRecoveryEmail(email);
                _logger.LogInformation("Send recovery email, Email: {@email}", email);

                return Ok("Recovery email sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("changePasswordUsingHash")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public ActionResult ChangePasswordUsingHash([FromBody] ChangePasswordUsingHashParam param)
        {
            try
            {
                _userService.ChangePasswordUsingHash(param.RecoveryHash, param.NewPassword);
                _logger.LogInformation("Change password using hash, Hash: {@hash}", param.RecoveryHash);

                return Ok("Password changed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpGet("list")]
        public async Task<ActionResult<System.Collections.Generic.List<UserInfo>>> list()
        {
            try
            {
                var userSession = _userService.GetUserInSession(HttpContext);
                if (userSession == null || !userSession.IsAdmin)
                {
                    _logger.LogError(NotAuthorizedMessage);
                    return Unauthorized(NotAuthorizedMessage);
                }

                var userModels = _userService.ListUsers();
                var userInfos = await Task.WhenAll(userModels.Select(x => _userService.GetUserInfoFromModel(x)));

                _logger.LogInformation("list() successfully");

                return Ok(userInfos.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize]
        [HttpPost("search")]
        public ActionResult<PagedResult<UserInfo>> Search([FromBody] UserSearchParam param)
        {
            try
            {
                var userSession = _userService.GetUserInSession(HttpContext);
                if (userSession == null || !userSession.IsAdmin)
                {
                    _logger.LogError(NotAuthorizedMessage);
                    return Unauthorized(NotAuthorizedMessage);
                }

                var result = _userService.SearchUsers(
                    param.SearchTerm,
                    param.Page,
                    param.PageSize
                );

                _logger.LogInformation(
                    "search(searchTerm: {SearchTerm}, page: {Page}, pageSize: {PageSize}) = {TotalCount} users found",
                    param.SearchTerm,
                    param.Page,
                    param.PageSize,
                    result.TotalCount
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ExceptionOccurredMessage, ex.Message);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
