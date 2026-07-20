using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityService.Client;
using IdentityService.Contracts;
using IdentityService.Domain;
using IdentityService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace IdentityService.IntegrationTests.Web;

[Collection(nameof(IntegrationTestCollection))]
public sealed class LocalizationTests(TestApplication app) : ApiTestBase(app)
{
    private UserManager<ApplicationUser> UserManager =>
        Scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    [DockerFact]
    public async Task ValidationErrors_AreReturnedInRussian()
    {
        var login = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(String.Empty, String.Empty));
        var refresh = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Refresh,
            new RefreshRequest(String.Empty));

        login.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        refresh.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var loginProblem = (await login.Content
            .ReadFromJsonAsync<ValidationProblemDetails>())!;
        loginProblem.Title.ShouldBe("Ошибка проверки данных");
        loginProblem.Errors["Email"].ShouldContain("Укажите email.");
        loginProblem.Errors["Password"].ShouldContain("Укажите пароль.");

        var refreshProblem = (await refresh.Content
            .ReadFromJsonAsync<ValidationProblemDetails>())!;
        refreshProblem.Title.ShouldBe("Ошибка проверки данных");
        refreshProblem.Errors["RefreshToken"].ShouldContain("Укажите refresh-токен.");
    }

    [DockerFact]
    public async Task IdentityAndBusinessErrors_AreReturnedInRussian()
    {
        var email = $"localization_{Guid.NewGuid():N}@test.local";
        var weakPassword = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Register,
            new RegisterRequest(email, "password", "Пользователь"));

        weakPassword.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var identityProblem = (await weakPassword.Content
            .ReadFromJsonAsync<ValidationProblemDetails>())!;
        identityProblem.Title.ShouldBe("Ошибка проверки данных");
        identityProblem.Errors[String.Empty]
            .ShouldContain(message => message.Contains("Пароль должен содержать цифру."));
        identityProblem.Errors[String.Empty]
            .ShouldContain(message => message.Contains("Пароль должен содержать заглавную букву."));

        var admin = await CreateAdminTokenAsync();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{ApiRoutes.PrefixV1}/users/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin.AccessToken);
        var missingUser = await HttpClient.SendAsync(request);

        missingUser.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var businessProblem = (await missingUser.Content.ReadFromJsonAsync<ApiProblem>())!;
        businessProblem.Title.ShouldBe("Ресурс не найден");
        businessProblem.Detail.ShouldNotBeNull();
        businessProblem.Detail.ShouldContain("Пользователь с id");
        businessProblem.Detail.ShouldEndWith("не найден.");
        businessProblem.Code.ShouldBe("ApplicationUser.UserNotFound");
    }

    private async Task<TokenResponse> CreateAdminTokenAsync()
    {
        var email = $"localization_admin_{Guid.NewGuid():N}@test.local";
        const string password = "Admin1234";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            DisplayName = "Администратор локализации",
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        (await UserManager.CreateAsync(user, password)).Succeeded.ShouldBeTrue();
        (await UserManager.AddToRoleAsync(user, RoleNames.Admin)).Succeeded.ShouldBeTrue();

        var response = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Auth.Login,
            new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }
}
