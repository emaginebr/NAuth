using System;
using Microsoft.AspNetCore.Http;
using NAuth.Infra.Interfaces;

namespace NAuth.API.Services;

/// <summary>
/// Scoped service that resolves the TenantId for the current HTTP request.
/// Authenticated requests: reads from JWT tenant_id claim.
/// Non-authenticated requests: reads from HttpContext.Items["TenantId"] (set by TenantMiddleware from X-Tenant-Id header).
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                throw new InvalidOperationException(
                    "HttpContext is not available. TenantContext must be used within an HTTP request scope.");

            // Authenticated: resolve from JWT claim
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = httpContext.User.FindFirst("tenant_id");
                if (tenantClaim != null && !string.IsNullOrWhiteSpace(tenantClaim.Value))
                    return tenantClaim.Value;
            }

            // Non-authenticated: resolve from HttpContext.Items (set by TenantMiddleware from X-Tenant-Id header)
            if (httpContext.Items.TryGetValue("TenantId", out var tenantId)
                && tenantId is string tid
                && !string.IsNullOrWhiteSpace(tid))
            {
                return tid;
            }

            throw new InvalidOperationException(
                "Unable to resolve TenantId. Ensure the request includes a valid JWT with tenant_id claim or the X-Tenant-Id header.");
        }
    }
}
