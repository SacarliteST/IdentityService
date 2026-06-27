using System.Net;
using System.Net.Http.Json;
using IdentityService.Contracts;
using IdentityService.IntegrationTests.Infrastructure;
using Shouldly;

namespace IdentityService.IntegrationTests.Auth;

[Collection(nameof(IntegrationTestCollection))]
public sealed class RegisterTests(TestApplication app) : ApiTestBase(app)
{
    [DockerFact]
    public async Task Register_ValidRequest_Returns201WithStudentRole()
    {
        var request = new RegisterRequest($"user_{Guid.NewGuid()}@test.com", "Password1!", "Test User");

        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body.ShouldNotBeNull();
        body.AccessToken.ShouldNotBeNullOrEmpty();
        body.RefreshToken.ShouldNotBeNullOrEmpty();
        body.Email.ShouldBe(request.Email);
        body.Roles.ShouldContain(RoleNames.Student);
    }

    [DockerFact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var request = new RegisterRequest($"dup_{Guid.NewGuid()}@test.com", "Password1!", null);

        await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, request);
        var second = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [DockerFact]
    public async Task Register_WeakPassword_Returns422()
    {
        var request = new RegisterRequest($"weak_{Guid.NewGuid()}@test.com", "short", null);

        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [DockerFact]
    public async Task Register_InvalidEmail_Returns422()
    {
        var request = new RegisterRequest("not-an-email", "Password1!", null);

        var response = await HttpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, request);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
