using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NAuth.Infra.Context;
using NAuth.Infra.Interfaces;

namespace NAuth.API.Services;

/// <summary>
/// Creates NAuthContext instances configured with the current tenant's ConnectionString.
/// Resolves the ConnectionString dynamically from Tenants:{tenantId}:ConnectionString.
/// </summary>
public class TenantDbContextFactory
{
    private readonly ITenantContext _tenantContext;
    private readonly IConfiguration _configuration;

    public TenantDbContextFactory(ITenantContext tenantContext, IConfiguration configuration)
    {
        _tenantContext = tenantContext;
        _configuration = configuration;
    }

    public NAuthContext CreateDbContext()
    {
        var tenantId = _tenantContext.TenantId;
        var connectionString = _configuration[$"Tenants:{tenantId}:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"ConnectionString not found for tenant '{tenantId}'. Expected key: Tenants:{tenantId}:ConnectionString");

        var optionsBuilder = new DbContextOptionsBuilder<NAuthContext>();
        optionsBuilder.UseLazyLoadingProxies().UseNpgsql(connectionString);

        return new NAuthContext(optionsBuilder.Options);
    }
}
