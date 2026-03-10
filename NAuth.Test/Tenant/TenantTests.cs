using Microsoft.Extensions.Configuration;
using Moq;
using NAuth.API.Services;
using NAuth.Infra.Interfaces;
using Xunit;

namespace NAuth.Test.Tenant
{
    public class TenantContextTests
    {
        [Fact]
        public void MockedTenantContext_ShouldReturnFixedTenantId()
        {
            var mock = new Mock<ITenantContext>();
            mock.Setup(x => x.TenantId).Returns("test-tenant-001");

            Assert.Equal("test-tenant-001", mock.Object.TenantId);
        }

        [Fact]
        public void MockedTenantContext_DifferentTenants_ShouldBeIsolated()
        {
            var tenantA = new Mock<ITenantContext>();
            tenantA.Setup(x => x.TenantId).Returns("tenant-a");

            var tenantB = new Mock<ITenantContext>();
            tenantB.Setup(x => x.TenantId).Returns("tenant-b");

            Assert.NotEqual(tenantA.Object.TenantId, tenantB.Object.TenantId);
        }
    }

    public class TenantResolverTests
    {
        private static IConfiguration BuildConfiguration(Dictionary<string, string> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        [Fact]
        public void TenantResolver_ShouldResolveTenantId_FromConfiguration()
        {
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                { "Tenant:DefaultTenantId", "tenant-a" },
                { "Tenants:tenant-a:ConnectionString", "Server=srv1;Database=TenantA_DB;" },
                { "Tenants:tenant-a:JwtSecret", "secret-key-tenant-a-at-least-64-chars-long-for-hmac-sha256-testing" }
            });

            var resolver = new TenantResolver(config);

            Assert.Equal("tenant-a", resolver.TenantId);
            Assert.Equal("Server=srv1;Database=TenantA_DB;", resolver.ConnectionString);
            Assert.Equal("secret-key-tenant-a-at-least-64-chars-long-for-hmac-sha256-testing", resolver.JwtSecret);
        }

        [Fact]
        public void TenantResolver_MissingTenantId_ShouldThrow()
        {
            var config = BuildConfiguration(new Dictionary<string, string>());

            var resolver = new TenantResolver(config);

            Assert.Throws<InvalidOperationException>(() => resolver.TenantId);
        }

        [Fact]
        public void TenantResolver_MissingConnectionString_ShouldThrow()
        {
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                { "Tenant:DefaultTenantId", "tenant-x" }
            });

            var resolver = new TenantResolver(config);

            Assert.Throws<InvalidOperationException>(() => resolver.ConnectionString);
        }

        [Fact]
        public void TenantResolver_MissingJwtSecret_ShouldThrow()
        {
            var config = BuildConfiguration(new Dictionary<string, string>
            {
                { "Tenant:DefaultTenantId", "tenant-x" },
                { "Tenants:tenant-x:ConnectionString", "Server=srv1;" }
            });

            var resolver = new TenantResolver(config);

            Assert.Throws<InvalidOperationException>(() => resolver.JwtSecret);
        }
    }

    public class TenantHeaderHandlerTests
    {
        [Fact]
        public async Task SendAsync_ShouldAddTenantHeader()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Tenant:DefaultTenantId", "tenant-test" }
                })
                .Build();

            var handler = new TenantHeaderHandler(config)
            {
                InnerHandler = new TestDelegatingHandler()
            };

            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");

            await client.SendAsync(request);

            Assert.True(request.Headers.Contains("X-Tenant-Id"));
            Assert.Equal("tenant-test", request.Headers.GetValues("X-Tenant-Id").First());
        }

        [Fact]
        public async Task SendAsync_MissingConfig_ShouldNotAddHeader()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>())
                .Build();

            var handler = new TenantHeaderHandler(config)
            {
                InnerHandler = new TestDelegatingHandler()
            };

            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/test");

            await client.SendAsync(request);

            Assert.False(request.Headers.Contains("X-Tenant-Id"));
        }

        private class TestDelegatingHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
            }
        }
    }
}
