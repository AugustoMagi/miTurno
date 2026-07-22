using FluentValidation;
using MiTurno.Application.Features.Auth.Dtos;

namespace MiTurno.Application.Features.Auth;

public class RestablecerPasswordValidator : AbstractValidator<RestablecerPasswordRequest>
{
    public RestablecerPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.PasswordNueva)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.");
    }
}
