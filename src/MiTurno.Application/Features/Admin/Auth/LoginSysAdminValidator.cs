using FluentValidation;
using MiTurno.Application.Features.Admin.Auth.Dtos;

namespace MiTurno.Application.Features.Admin.Auth;

public class LoginSysAdminValidator : AbstractValidator<LoginSysAdminRequest>
{
    public LoginSysAdminValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
