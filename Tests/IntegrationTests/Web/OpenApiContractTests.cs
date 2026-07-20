using System.Net;
using System.Text.Json;
using IdentityService.IntegrationTests.Infrastructure;
using Shouldly;

namespace IdentityService.IntegrationTests.Web;

[Collection(nameof(IntegrationTestCollection))]
public sealed class OpenApiContractTests(TestApplication app) : ApiTestBase(app)
{
    [DockerFact]
    public async Task Swagger_ContainsStableIdentityAdminContract()
    {
        var response = await HttpClient.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var schemas = root.GetProperty("components").GetProperty("schemas");

        var rolesPath = root.GetProperty("paths").GetProperty("/api/v1/users/{id}/roles");
        rolesPath.TryGetProperty("put", out _).ShouldBeTrue();
        rolesPath.TryGetProperty("post", out _).ShouldBeFalse();

        var roleSchema = schemas.GetProperty("UserRole");
        roleSchema.GetProperty("type").GetString().ShouldBe("string");
        roleSchema.GetProperty("enum")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ShouldBe(["Admin", "Teacher", "Student"], ignoreOrder: true);

        AssertRequired(schemas.GetProperty("LoginRequest"), "email", "password");
        AssertRequired(
            schemas.GetProperty("TokenResponse"),
            "accessToken",
            "accessExpiresAt",
            "refreshToken",
            "refreshExpiresAt",
            "userId",
            "email",
            "roles");
        AssertRequired(schemas.GetProperty("UpdateUserRolesRequest"), "roles");
        AssertRequired(schemas.GetProperty("CreateUserRequest"), "email", "password", "roles");
        AssertRequired(schemas.GetProperty("ValidationProblemDetails"), "errors");

        var loginValidationSchema = root
            .GetProperty("paths")
            .GetProperty("/api/v1/auth/login")
            .GetProperty("post")
            .GetProperty("responses")
            .GetProperty("422")
            .GetProperty("content")
            .GetProperty("application/problem+json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        loginValidationSchema.ShouldBe("#/components/schemas/ValidationProblemDetails");
    }

    private static void AssertRequired(JsonElement schema, params string[] expectedProperties)
    {
        var required = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToHashSet(StringComparer.Ordinal);

        foreach (var property in expectedProperties)
        {
            required.ShouldContain(property);
        }
    }
}
