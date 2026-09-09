using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Auth.TokenExchange;

internal sealed class TokenExchangeRequestValidator : AbstractValidator<TokenExchangeRequest>
{
    public const string ExpectedGrantType = "urn:ietf:params:oauth:grant-type:token-exchange";

    public TokenExchangeRequestValidator()
    {
        RuleFor(x => x.GrantType)
            .Equal(ExpectedGrantType)
            .WithMessage($"grantType должен быть '{ExpectedGrantType}'.");
        RuleFor(x => x.SubjectToken)
            .NotEmpty()
            .WithMessage("Укажите subjectToken.");
        RuleFor(x => x.Audience)
            .NotEmpty()
            .WithMessage("Укажите audience.");
    }
}
