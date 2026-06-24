using IdentityService.Domain;
using Shouldly;

namespace IdentityService.IntegrationTests.Domain;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ReturnsTrue()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(7), Now);

        token.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsActive_WhenExpired_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddSeconds(-1), Now);

        token.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_AfterRevoke_ReturnsFalse()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(7), Now);
        token.Revoke(Now);

        token.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_SetsRevokedAtAndReplacedByHash()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", Now.AddDays(7), Now);
        token.Revoke(Now, "newHash");

        token.RevokedAt.ShouldBe(Now);
        token.ReplacedByTokenHash.ShouldBe("newHash");
    }
}
