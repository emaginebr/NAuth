# NAuth

[![NuGet](https://img.shields.io/nuget/v/NAuth.svg)](https://www.nuget.org/packages/NAuth/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NAuth.svg)](https://www.nuget.org/packages/NAuth/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Unified library for the [NAuth](https://github.com/emaginebr/NAuth) authentication and authorization ecosystem. Contains Data Transfer Objects (DTOs), HTTP client implementations (ACL), authentication handlers, JWT token processing, and **multi-tenant support** for user management, roles, and authentication.

> **Part of the NAuth ecosystem** — see [NAuth.API](https://github.com/emaginebr/NAuth) for the main project and full documentation.

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
- **Settings**: Configuration objects for NAuth services (including tenant settings)
- **Converters**: Custom JSON converters for proper serialization
- **Pagination**: Generic paged result container

### Access Control Layer (ACL)

- **HTTP Clients**: Ready-to-use HTTP clients for NAuth API
- **User Management**: Complete user CRUD operations via `IUserClient`
- **Role Management**: Role and permission handling via `IRoleClient`
- **Authentication Handler**: JWT validation middleware for ASP.NET Core
- **Session Management**: Extract user info from JWT claims

### Multi-Tenant Support

- **Automatic Tenant Header Injection**: `TenantDelegatingHandler` adds `X-Tenant-Id` to all HTTP requests
- **Pluggable Tenant Resolution**: `ITenantProvider` interface for custom tenant identification strategies
- **Per-Tenant JWT Secrets**: `ITenantSecretProvider` allows different JWT secrets per tenant
- **Built-in Settings Provider**: `SettingsTenantProvider` reads tenant ID from `NAuthSetting.TenantId`
- **Simplified DI Registration**: `AddNAuth()` and `AddNAuthAuthentication()` extension methods

## Quick Start

### 1. Configure Services

Add NAuth to your ASP.NET Core application using the new extension methods:

```csharp
using NAuth.ACL;
using NAuth.DTO.Settings;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Configure NAuth settings
        services.Configure<NAuthSetting>(Configuration.GetSection("NAuthSetting"));

        // Register all NAuth services (clients, tenant provider, delegating handler)
        services.AddNAuth();

        // Add NAuth authentication handler
        services.AddNAuthAuthentication();
    }
}
```

The `AddNAuth()` method registers:
- `ITenantProvider` (defaults to `SettingsTenantProvider`)
- `TenantDelegatingHandler` for automatic `X-Tenant-Id` header injection
- `IUserClient` / `IRoleClient` HTTP clients with the tenant handler in the pipeline

### 2. Configure appsettings.json

```json
{
  "NAuthSetting": {
    "ApiUrl": "https://your-nauth-api.com/api",
    "JwtSecret": "your-super-secret-jwt-key-min-64-characters",
    "BucketName": "nauth-user-images",
    "TenantId": "your-tenant-id"
  }
}
```

## Multi-Tenant

NAuth supports multi-tenant architectures where a single NAuth.API instance serves multiple tenants with isolated data.

### How It Works

1. **Outgoing requests**: The `TenantDelegatingHandler` automatically injects the `X-Tenant-Id` HTTP header into every request made by `UserClient` and `RoleClient`.
2. **Incoming requests**: The `NAuthHandler` resolves the JWT secret per tenant by reading the `tenant_id` claim from the token and delegating to `ITenantSecretProvider`.
3. **Fallback**: If no tenant-specific secret is found, the default `JwtSecret` from `NAuthSetting` is used.

### Option 1: Simple (Single Tenant via Settings)

Use the default `SettingsTenantProvider`, which reads the tenant ID from configuration:

```csharp
services.Configure<NAuthSetting>(Configuration.GetSection("NAuthSetting"));
services.AddNAuth(); // Uses SettingsTenantProvider by default
services.AddNAuthAuthentication();
```

```json
{
  "NAuthSetting": {
    "ApiUrl": "https://your-nauth-api.com/api",
    "JwtSecret": "your-jwt-secret-key-min-64-characters",
    "TenantId": "tenant-abc"
  }
}
```

### Option 2: Custom Tenant Provider

Implement `ITenantProvider` for dynamic tenant resolution (e.g., from request headers, subdomains, or route data):

```csharp
using NAuth.ACL.Interfaces;

public class HeaderTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetTenantId()
    {
        return _httpContextAccessor.HttpContext?
            .Request.Headers["X-Tenant-Id"].FirstOrDefault();
    }
}
```

Register with the generic overload:

```csharp
services.AddHttpContextAccessor();
services.AddNAuth<HeaderTenantProvider>();
services.AddNAuthAuthentication();
```

### Option 3: Per-Tenant JWT Secrets

Implement `ITenantSecretProvider` so the authentication handler can validate tokens signed with different secrets per tenant:

```csharp
using NAuth.ACL.Interfaces;

public class DatabaseTenantSecretProvider : ITenantSecretProvider
{
    private readonly ITenantRepository _tenantRepo;

    public DatabaseTenantSecretProvider(ITenantRepository tenantRepo)
    {
        _tenantRepo = tenantRepo;
    }

    public string? GetJwtSecret(string tenantId)
    {
        var tenant = _tenantRepo.GetById(tenantId);
        return tenant?.JwtSecret;
    }
}
```

Register it alongside your tenant provider:

```csharp
services.AddNAuth<HeaderTenantProvider>();
services.AddNAuthAuthentication();
services.AddScoped<ITenantSecretProvider, DatabaseTenantSecretProvider>();
```

The `NAuthHandler` will:
1. Read the `tenant_id` claim from the JWT token (without validating first)
2. Call `ITenantSecretProvider.GetJwtSecret(tenantId)` to get the tenant-specific secret
3. Validate the token using the resolved secret
4. Fall back to `NAuthSetting.JwtSecret` if no tenant secret is found

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
    public string TenantId { get; set; }
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
    Task<UserInfo?> UpdateAsync(UserUpdatedInfo user, string token);

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

Custom JWT authentication handler for ASP.NET Core middleware with multi-tenant support:

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
- **Microsoft.Extensions.Configuration.Abstractions** (9.0.8)
- **Microsoft.Extensions.Http** (9.0.8)
- **System.IdentityModel.Tokens.Jwt** (8.15.0)
- **zTools** (latest)

## Best Practices

1. **Token Storage**: Store JWT tokens securely (HttpOnly cookies or secure storage)
2. **Token Refresh**: Implement token refresh before expiration
3. **Error Handling**: Always wrap API calls in try-catch blocks
4. **Logging**: Use structured logging for debugging
5. **Dependency Injection**: Always use DI for client instances — prefer `AddNAuth()` over manual registration
6. **Configuration**: Use strongly-typed configuration with `IOptions<NAuthSetting>`
7. **Multi-Tenant**: Use `AddNAuth<T>()` with a custom `ITenantProvider` for dynamic tenant resolution in multi-tenant scenarios

## NAuth Ecosystem

| Project | Type | Package | Description |
|---------|------|---------|-------------|
| **[NAuth.API](https://github.com/emaginebr/NAuth)** | .NET | — | Central REST API backend (main project) |
| **NAuth** | .NET | [![NuGet](https://img.shields.io/nuget/v/NAuth.svg)](https://www.nuget.org/packages/NAuth/) | Unified DTOs + ACL client library (this package) |
| **[nauth-react](https://github.com/emaginebr/nauth-react)** | NPM | [![npm](https://img.shields.io/npm/v/nauth-react.svg)](https://www.npmjs.com/package/nauth-react) | React component library (login, register, user management) |

### Dependency graph

```
nauth-react (NPM)
  └─ NAuth.API (HTTP)
       └─ NAuth (NuGet - DTOs + ACL)
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](https://opensource.org/licenses/MIT) file for details.

## Links

- [NuGet Package](https://www.nuget.org/packages/NAuth/)
- [GitHub Repository](https://github.com/emaginebr/NAuth)

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/emaginebr/NAuth).

---

Made with love by [Rodrigo Landim Carneiro](https://github.com/landim32) at Emagine
