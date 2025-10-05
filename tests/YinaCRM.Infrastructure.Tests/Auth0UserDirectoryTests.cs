using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Yina.Common.Abstractions.Results;
using YinaCRM.Infrastructure.Abstractions.Auth;
using YinaCRM.Infrastructure.Auth.Auth0;

namespace YinaCRM.Infrastructure.Tests;

public class Auth0UserDirectoryTests
{
    [Fact]
    public async Task EnsureUserAsync_AddsTenantToMetadata_WhenMissing()
    {
        var patchCalled = false;
        var handler = new TestHttpMessageHandler(request =>
        {
            if (request.Method == HttpMethod.Get)
            {
                var appMetadata = new JsonObject
                {
                    ["crm"] = new JsonObject
                    {
                        ["tenants"] = new JsonArray(),
                    },
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        user_id = "auth0|user-123",
                        email_verified = true,
                        app_metadata = appMetadata,
                    }),
                };
            }

            patchCalled = true;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { }),
            };
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://auth0.local"),
        };

        var tokenSource = new StubTokenSource(Result.Success("token"));
        var directory = new Auth0UserDirectory(httpClient, tokenSource, NullLogger<Auth0UserDirectory>.Instance);

        var principal = new AuthenticatedPrincipal(
            SubjectId: "auth0|user-123",
            TenantId: "tenant-1",
            Email: "user@example.com",
            DisplayName: "User",
            Roles: ImmutableArray.Create("admin"),
            Scopes: ImmutableArray.Create("crm:read"),
            CustomClaims: ImmutableDictionary<string, string>.Empty);

        var result = await directory.EnsureUserAsync(principal);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.True(patchCalled);
        Assert.Equal("auth0|user-123", result.Value.InternalUserId);
    }

    [Fact]
    public async Task EnsureUserAsync_ReturnsFailure_WhenUserMissing()
    {
        var handler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://auth0.local") };
        var tokenSource = new StubTokenSource(Result.Success("token"));
        var directory = new Auth0UserDirectory(httpClient, tokenSource, NullLogger<Auth0UserDirectory>.Instance);

        var principal = new AuthenticatedPrincipal(
            SubjectId: "auth0|missing",
            TenantId: "tenant-1",
            Email: "user@example.com",
            DisplayName: "User",
            Roles: ImmutableArray<string>.Empty,
            Scopes: ImmutableArray<string>.Empty,
            CustomClaims: ImmutableDictionary<string, string>.Empty);

        var result = await directory.EnsureUserAsync(principal);
        Assert.True(result.IsFailure);
    }

    private sealed class StubTokenSource : IAuth0ManagementTokenSource
    {
        private readonly Result<string> _result;

        public StubTokenSource(Result<string> result)
        {
            _result = result;
        }

        public Task<Result<string>> GetTokenAsync(CancellationToken cancellationToken) => Task.FromResult(_result);
    }
}
