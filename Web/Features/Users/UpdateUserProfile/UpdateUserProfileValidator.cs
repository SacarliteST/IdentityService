using FluentValidation;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.UpdateUserProfile;

internal sealed class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileValidator()
    {
        RuleFor(request => request)
            .Must(request =>
                !String.IsNullOrWhiteSpace(request.FirstName) || !String.IsNullOrWhiteSpace(request.LastName))
            .WithName("FirstName")
            .WithMessage("Укажите имя или фамилию.");
        RuleFor(request => request.FirstName).ValidPersonNamePart("Имя");
        RuleFor(request => request.LastName).ValidPersonNamePart("Фамилия");
        RuleFor(request => request.MiddleName).ValidPersonNamePart("Отчество");
    }
}
