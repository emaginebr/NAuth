using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace NAuth.API.Middlewares;

/// <summary>
/// Middleware that reads the X-Tenant-Id header and stores it in HttpContext.Items.
/// Runs before authentication so that non-authenticated endpoints can resolve the tenant.
/// For authenticated endpoints, TenantContext will use the JWT tenant_id claim instead.
/// </summary>
public class TenantMiddleware
{
    private const string TenantHeaderName = "X-Tenant-Id";
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(TenantHeaderName, out var tenantHeader))
        {
            var tenantId = tenantHeader.ToString();
            if (!string.IsNullOrWhiteSpace(tenantId))
                context.Items["TenantId"] = tenantId;
        }

        await _next(context);
    }
}
