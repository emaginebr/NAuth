namespace NAuth.API.Services;

/// <summary>
/// Resolves tenant configuration from appsettings.
/// TenantId is read from Tenant:DefaultTenantId.
/// ConnectionString and JwtSecret are read from Tenants:{tenantId}.
/// </summary>
public interface ITenantResolver
{
    string TenantId { get; }
    string ConnectionString { get; }
    string JwtSecret { get; }
}
