using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NAuth.API.Services;

/// <summary>
/// DelegatingHandler that automatically injects the X-Tenant-Id header
/// into all outbound HTTP requests made by HttpClients.
/// Reads TenantId from Tenant:DefaultTenantId in appsettings.
/// </summary>
public class TenantHeaderHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;

    public TenantHeaderHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tenantId = _configuration["Tenant:DefaultTenantId"];
        if (!string.IsNullOrEmpty(tenantId))
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);

        return await base.SendAsync(request, cancellationToken);
    }
}
