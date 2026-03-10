namespace NAuth.Infra.Interfaces;

/// <summary>
/// Provides the current tenant identifier for the request scope.
/// In the API: resolved from JWT claim (authenticated) or X-Tenant-Id header (non-authenticated).
/// </summary>
public interface ITenantContext
{
    string TenantId { get; }
}
