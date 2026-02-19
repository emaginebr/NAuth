# NAuth

[![NuGet](https://img.shields.io/nuget/v/NAuth.svg)](https://www.nuget.org/packages/NAuth/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NAuth.svg)](https://www.nuget.org/packages/NAuth/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Unified library for the [NAuth](https://github.com/landim32/NAuth.API) authentication and authorization ecosystem. Contains Data Transfer Objects (DTOs), HTTP client implementations (ACL), authentication handlers, and JWT token processing for user management, roles, and authentication.

> **Part of the NAuth ecosystem** — see [NAuth.API](https://github.com/landim32/NAuth.API) for the main project and full documentation.

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package NAuth
```

Or via Package Manager Console:

```powershell
Install-Package NAuth
```

> **Migration from NAuth.DTO / NAuth.ACL**: This package replaces both `NAuth.DTO` and `NAuth.ACL`. Simply swap the package references — all namespaces (`NAuth.DTO.*`, `NAuth.ACL.*`) remain the same, so no code changes are needed.

## Features

### Data Transfer Objects (DTOs)

- **User DTOs**: Complete user data models for authentication and management
- **Role DTOs**: Role and permission data structures
- **Authentication Models**: Login parameters, token results, and password management
- **Settings**: Configuration objects for NAuth services
- **Converters**: Custom JSON converters for proper serialization
- **Pagination**: Generic paged result container

### Access Control Layer (ACL)

- **HTTP Clients**: Ready-to-use HTTP clients for NAuth API
- **User Management**: Complete user CRUD operations via `IUserClient`
- **Role Management**: Role and permission handling via `IRoleClient`
- **Authentication Handler**: JWT validation middleware for ASP.NET Core
- **Session Management**: Extract user info from JWT claims

## Quick Start

### 1. Configure Services

Add NAuth to your ASP.NET Core application:

```csharp
using NAuth.ACL;
using NAuth.ACL.Interfaces;
using NAuth.DTO.Settings;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Configure NAuth settings
        services.Configure<NAuthSetting>(options =>
        {
            options.ApiUrl = "https://your-nauth-api.com/api";
            options.JwtSecret = "your-jwt-secret-key";
            options.BucketName = "user-images";
        });

        // Add HttpClient
        services.AddHttpClient();

        // Register NAuth clients
        services.AddScoped<IUserClient, UserClient>();
        services.AddScoped<IRoleClient, RoleClient>();

        // Add authentication with NAuth handler
        services.AddAuthentication("NAuth")
            .AddScheme<AuthenticationSchemeOptions, NAuthHandler>("NAuth", options => { });
    }
}
```

### 2. Configure appsettings.json

```json
{
  "NAuthSetting": {
    "ApiUrl": "https://your-nauth-api.com/api",
    "JwtSecret": "your-super-secret-jwt-key-min-64-characters",
    "BucketName": "nauth-user-images"
  }
}
```

## Core DTOs

### UserInfo

Complete user information including profile, roles, addresses, and phones.

```csharp
public class UserInfo
{
    public long UserId { get; set; }
    public string Slug { get; set; }
    public string ImageUrl { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Hash { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime? BirthDate { get; set; }
    public string IdDocument { get; set; }
    public string PixKey { get; set; }
    public string Password { get; set; }
    public int Status { get; set; }
    public IList<RoleInfo> Roles { get; set; }
    public IList<UserPhoneInfo> Phones { get; set; }
    public IList<UserAddressInfo> Addresses { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### UserInsertedInfo

Simplified model for user registration (without system-generated fields).

```csharp
public class UserInsertedInfo
{
    public string Slug { get; set; }
    public string ImageUrl { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime? BirthDate { get; set; }
    public string IdDocument { get; set; }
    public string PixKey { get; set; }
    public string Password { get; set; }
    public IList<RoleInfo> Roles { get; set; }
    public IList<UserPhoneInfo> Phones { get; set; }
    public IList<UserAddressInfo> Addresses { get; set; }
}
```

### Authentication Models

```csharp
// Login credentials
public class LoginParam
{
    public string Email { get; set; }
    public string Password { get; set; }
}

// Authentication result with JWT token
public class UserTokenResult
{
    public string Token { get; set; }
    public UserInfo User { get; set; }
}
```

### Password Management

```csharp
// Change password with current password verification
public class ChangePasswordParam
{
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
}

// Reset password using recovery hash
public class ChangePasswordUsingHashParam
{
    public string RecoveryHash { get; set; }
    public string NewPassword { get; set; }
}
```

### Role, Search and Pagination

```csharp
public class RoleInfo
{
    public long RoleId { get; set; }
    public string Slug { get; set; }
    public string Name { get; set; }
}

public class UserSearchParam
{
    public string SearchTerm { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PagedResult<T>
{
    public IList<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }
}
```

### Settings

```csharp
public class NAuthSetting
{
    public string ApiUrl { get; set; }
    public string JwtSecret { get; set; }
    public string BucketName { get; set; }
}
```

## User Client

### IUserClient Interface

Complete user management operations:

```csharp
public interface IUserClient
{
    // Session Management
    UserSessionInfo? GetUserInSession(HttpContext httpContext);

    // User Retrieval
    Task<UserInfo?> GetMeAsync(string token);
    Task<UserInfo?> GetByIdAsync(long userId, string token);
    Task<UserInfo?> GetByTokenAsync(string token);
    Task<UserInfo?> GetByEmailAsync(string email);
    Task<UserInfo?> GetBySlugAsync(string slug);
    Task<IList<UserInfo>> ListAsync(int take);

    // User Management
    Task<UserInfo?> InsertAsync(UserInsertedInfo user);
    Task<UserInfo?> UpdateAsync(UserInfo user, string token);

    // Authentication
    Task<UserTokenResult?> LoginWithEmailAsync(LoginParam param);

    // Password Management
    Task<bool> HasPasswordAsync(string token);
    Task<bool> ChangePasswordAsync(ChangePasswordParam param, string token);
    Task<bool> SendRecoveryMailAsync(string email);
    Task<bool> ChangePasswordUsingHashAsync(ChangePasswordUsingHashParam param);

    // File Upload
    Task<string> UploadImageUserAsync(Stream fileStream, string fileName, string token);
}
```

### Usage Examples

#### User Registration

```csharp
using NAuth.ACL.Interfaces;
using NAuth.DTO.User;

public class UserService
{
    private readonly IUserClient _userClient;

    public UserService(IUserClient userClient)
    {
        _userClient = userClient;
    }

    public async Task<UserInfo> RegisterUserAsync()
    {
        var newUser = new UserInsertedInfo
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Password = "SecureP@ssw0rd",
            BirthDate = new DateTime(1990, 1, 1),
            IdDocument = "12345678901",
            Roles = new List<RoleInfo>
            {
                new RoleInfo { RoleId = 1 }
            }
        };

        var result = await _userClient.InsertAsync(newUser);
        return result;
    }
}
```

#### User Login

```csharp
public async Task<UserTokenResult> LoginAsync(string email, string password)
{
    var loginParam = new LoginParam
    {
        Email = email,
        Password = password
    };

    var result = await _userClient.LoginWithEmailAsync(loginParam);

    if (result != null)
    {
        Console.WriteLine($"Logged in as: {result.User.Name}");
        Console.WriteLine($"Token: {result.Token}");
    }

    return result;
}
```

#### Password Management

```csharp
// Change password (authenticated user)
var changeParam = new ChangePasswordParam
{
    OldPassword = "OldPassword123",
    NewPassword = "NewSecureP@ssw0rd"
};
var success = await _userClient.ChangePasswordAsync(changeParam, token);

// Send recovery email
await _userClient.SendRecoveryMailAsync("user@example.com");

// Reset password using hash from email
var resetParam = new ChangePasswordUsingHashParam
{
    RecoveryHash = "abc123def456",
    NewPassword = "NewP@ssw0rd123"
};
await _userClient.ChangePasswordUsingHashAsync(resetParam);
```

#### Get User from Session

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IUserClient _userClient;

    public ProfileController(IUserClient userClient)
    {
        _userClient = userClient;
    }

    [HttpGet]
    [Authorize]
    public IActionResult GetProfile()
    {
        var user = _userClient.GetUserInSession(HttpContext);
        return user == null ? Unauthorized() : Ok(user);
    }
}
```

## Role Client

### IRoleClient Interface

```csharp
public interface IRoleClient
{
    Task<IList<RoleInfo>> ListAsync();
    Task<RoleInfo?> GetByIdAsync(long roleId);
    Task<RoleInfo?> GetBySlugAsync(string slug);
}
```

### Usage Example

```csharp
using NAuth.ACL.Interfaces;
using NAuth.DTO.User;

public class RoleService
{
    private readonly IRoleClient _roleClient;

    public RoleService(IRoleClient roleClient)
    {
        _roleClient = roleClient;
    }

    public async Task<IList<RoleInfo>> GetAllRolesAsync()
    {
        return await _roleClient.ListAsync();
    }
}
```

## Authentication Handler

Custom JWT authentication handler for ASP.NET Core middleware:

```csharp
[ApiController]
[Route("api/[controller]")]
public class SecureController : ControllerBase
{
    [HttpGet]
    [Authorize] // Uses NAuthHandler automatically
    public IActionResult GetSecureData()
    {
        var userId = User.FindFirst("userId")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var isAdmin = User.FindFirst("isAdmin")?.Value;

        return Ok(new { UserId = userId, Email = email, IsAdmin = isAdmin });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "admin")]
    public IActionResult GetAdminData()
    {
        return Ok("Admin only data");
    }
}
```

## JSON Serialization

All DTOs are decorated with JSON attributes for seamless serialization with both `Newtonsoft.Json` and `System.Text.Json`. The library includes `NullableDateTimeConverter` for proper nullable DateTime handling:

```csharp
[JsonConverter(typeof(NullableDateTimeConverter))]
public DateTime? BirthDate { get; set; }
```

## Advanced Configuration

### Custom HTTP Client Configuration

```csharp
services.AddHttpClient<IUserClient, UserClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
});
```

### Retry Policy with Polly

```csharp
services.AddHttpClient<IUserClient, UserClient>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
```

## Error Handling

All client methods may throw exceptions. Implement proper error handling:

```csharp
try
{
    var user = await _userClient.GetMeAsync(token);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Network error occurred");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error retrieving user");
}
```

## Dependencies

- **Newtonsoft.Json** (13.0.3)
- **Microsoft.AspNetCore.Authentication** (2.3.0)
- **System.IdentityModel.Tokens.Jwt** (8.15.0)
- **zTools** (latest)

## Best Practices

1. **Token Storage**: Store JWT tokens securely (HttpOnly cookies or secure storage)
2. **Token Refresh**: Implement token refresh before expiration
3. **Error Handling**: Always wrap API calls in try-catch blocks
4. **Logging**: Use structured logging for debugging
5. **Dependency Injection**: Always use DI for client instances
6. **Configuration**: Use strongly-typed configuration with `IOptions<NAuthSetting>`

## NAuth Ecosystem

| Project | Description |
|---------|-------------|
| **[NAuth.API](https://github.com/landim32/NAuth.API)** | Central REST API backend (main project) |
| **NAuth** | Unified DTOs + ACL client library (NuGet) |
| **[NAuth.React](https://github.com/landim32/NAuth.React)** | React component library (NPM) |
| **[NAuth.App](https://github.com/landim32/NAuth.APP)** | Frontend web application |

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](https://opensource.org/licenses/MIT) file for details.

## Links

- [NuGet Package](https://www.nuget.org/packages/NAuth/)
- [GitHub Repository](https://github.com/landim32/NAuth.API)

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/landim32/NAuth.API).

---

Made with love by [Rodrigo Landim](https://github.com/landim32) at Emagine
