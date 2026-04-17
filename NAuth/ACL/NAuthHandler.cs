using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NAuth.ACL.Interfaces;
using NAuth.DTO.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace NAuth.ACL
{
    public class NAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly NAuthSetting _nauthSetting;
        private readonly ITenantSecretProvider? _tenantSecretProvider;

        public NAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IOptions<NAuthSetting> nauthSetting,
            ITenantSecretProvider? tenantSecretProvider = null)
            : base(options, logger, encoder, clock)
        {
            _nauthSetting = nauthSetting.Value;
            _tenantSecretProvider = tenantSecretProvider;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Logger.LogInformation("Starting authentication process for request path: {Path}", Request.Path);

            if (!Request.Headers.ContainsKey("Authorization"))
            {
                Logger.LogWarning("Authentication failed: Missing Authorization Header for path {Path}", Request.Path);
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
            }

            try
            {
                var authHeaderValue = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeaderValue))
                {
                    Logger.LogWarning("Authentication failed: Authorization header is missing for path {Path}", Request.Path);
                    return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
                }
                if (string.IsNullOrWhiteSpace(authHeaderValue))
                {
                    Logger.LogWarning("Authentication failed: Authorization header has no token for path {Path}", Request.Path);
                    return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Token"));
                }

                var authHeader = AuthenticationHeaderValue.Parse(authHeaderValue);
                var token = authHeader.Parameter;
                if (string.IsNullOrWhiteSpace(token))
                {
                    Logger.LogWarning("Authentication failed: Authorization scheme present but token is empty for path {Path}", Request.Path);
                    return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Token"));
                }

                var jwtSecret = ResolveJwtSecret(token);
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    Logger.LogError("Authentication failed: NAuth:JwtSecret is not configured");
                    return Task.FromResult(AuthenticateResult.Fail("Missing JWT Secret"));
                }

                Logger.LogDebug("Starting JWT token validation");
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(jwtSecret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "NAuth",
                    ValidateAudience = true,
                    ValidAudience = "NAuth.API",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                Logger.LogDebug("Token validation completed successfully");

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    Logger.LogWarning("Authentication failed: Invalid token format or algorithm");
                    return Task.FromResult(AuthenticateResult.Fail("Invalid token format"));
                }

                var userIdClaim = principal.FindFirst("userId") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out long userId))
                {
                    Logger.LogWarning("Authentication failed: Invalid or missing user ID in token claims");
                    return Task.FromResult(AuthenticateResult.Fail("Invalid user ID in token"));
                }

                Logger.LogDebug("Creating authentication ticket for user {UserId}", userId);
                var identity = new ClaimsIdentity(principal.Claims, Scheme.Name);
                var claimsPrincipal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);

                Logger.LogInformation("JWT token validated successfully for user {UserId}", userId);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (SecurityTokenExpiredException ex)
            {
                Logger.LogWarning(ex, "Token has expired");
                return Task.FromResult(AuthenticateResult.Fail("Token has expired"));
            }
            catch (SecurityTokenException ex)
            {
                Logger.LogWarning(ex, "Invalid token");
                return Task.FromResult(AuthenticateResult.Fail($"Invalid token: {ex.Message}"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error validating token");
                return Task.FromResult(AuthenticateResult.Fail($"Error validating token: {ex.Message}"));
            }
        }

        private string? ResolveJwtSecret(string? token)
        {
            if (_tenantSecretProvider == null || string.IsNullOrEmpty(token))
                return _nauthSetting.JwtSecret;

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwt = handler.ReadJwtToken(token);
                    var tenantClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tenant_id");

                    if (tenantClaim != null && !string.IsNullOrEmpty(tenantClaim.Value))
                    {
                        var secret = _tenantSecretProvider.GetJwtSecret(tenantClaim.Value);
                        if (!string.IsNullOrEmpty(secret))
                            return secret;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to resolve tenant-specific JWT secret, falling back to default");
            }

            return _nauthSetting.JwtSecret;
        }
    }
}
