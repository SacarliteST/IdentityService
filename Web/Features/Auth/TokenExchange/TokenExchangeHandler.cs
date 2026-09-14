using System.Security.Claims;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Crypto;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Common.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TokenHasher = IdentityService.Common.Crypto.TokenHasher;

namespace IdentityService.Web.Features.Auth.TokenExchange;

internal sealed class TokenExchangeHandler(
    AppDbContext db,
    ITokenService tokenService,
    ISubjectTokenValidator subjectTokenValidator,
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider) : IRequestHandler<TokenExchangeCommand, Result<TokenExchangeResponse>>
{
    public async Task<Result<TokenExchangeResponse>> Handle(TokenExchangeCommand cmd, CancellationToken ct)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.ClientId == cmd.ClientId, ct);
        if (client is null || !client.IsEnabled || client.ClientSecretHash != TokenHasher.Hash(cmd.ClientSecret))
        {
            return Result<TokenExchangeResponse>.Fail(AuthErrors.InvalidClientCredentials());
        }

        if (!client.CanRequestAudience(cmd.Audience))
        {
            return Result<TokenExchangeResponse>.Fail(AuthErrors.AudienceNotAllowed());
        }

        var principal = await subjectTokenValidator.ValidateAsync(cmd.SubjectToken, ct);
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Result<TokenExchangeResponse>.Fail(AuthErrors.InvalidSubjectToken());
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            return Result<TokenExchangeResponse>.Fail(AuthErrors.InvalidSubjectToken());
        }

        var roles = principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var now = timeProvider.GetUtcNow();
        var exchanged = await tokenService.IssueForAudienceAsync(
            user, roles, cmd.Audience, cmd.SessionId, cmd.SessionExpiresAt, ct);

        var sessionSuffix = cmd.SessionId is null ? "" : $", сессия '{cmd.SessionId}'";
        db.AuditEvents.Add(AuditEvent.Create(
            user.Id,
            user.Id,
            AuditEventTypes.TokenExchanged,
            $"Токен обменян клиентом '{client.ClientId}' на audience '{cmd.Audience}'{sessionSuffix}.",
            now));
        await db.SaveChangesAsync(ct);

        var expiresIn = Math.Max(0, (int)(exchanged.ExpiresAt - now).TotalSeconds);
        return new TokenExchangeResponse(exchanged.AccessToken, expiresIn);
    }
}
