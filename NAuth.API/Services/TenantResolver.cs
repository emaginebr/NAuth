using System;
using Microsoft.Extensions.Configuration;

namespace NAuth.API.Services;

/// <summary>
/// Resolves tenant configuration from IConfiguration.
/// Reads TenantId from Tenant:DefaultTenantId and tenant-specific settings from Tenants:{tenantId}.
/// </summary>
public class TenantResolver : ITenantResolver
{
    private readonly IConfiguration _configuration;

    public TenantResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string TenantId
    {
        get
        {
            var tenantId = _configuration["Tenant:DefaultTenantId"];
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new InvalidOperationException(
                    "Tenant:DefaultTenantId is not configured in appsettings.json.");
            return tenantId;
        }
    }

    public string ConnectionString
    {
        get
        {
            var connStr = _configuration[$"Tenants:{TenantId}:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connStr))
                throw new InvalidOperationException(
                    $"ConnectionString not found for tenant '{TenantId}'. Expected key: Tenants:{TenantId}:ConnectionString");
            return connStr;
        }
    }

    public string JwtSecret
    {
        get
        {
            var secret = _configuration[$"Tenants:{TenantId}:JwtSecret"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException(
                    $"JwtSecret not found for tenant '{TenantId}'. Expected key: Tenants:{TenantId}:JwtSecret");
            return secret;
        }
    }
}
