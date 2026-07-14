using FluentValidation;
using MiTurno.Application.Features.Auth.Dtos;

namespace MiTurno.Application.Features.Auth;

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
