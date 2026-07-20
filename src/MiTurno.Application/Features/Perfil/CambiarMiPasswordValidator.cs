using FluentValidation;
using MiTurno.Application.Features.Perfil.Dtos;

namespace MiTurno.Application.Features.Perfil;

public class CambiarMiPasswordValidator : AbstractValidator<CambiarMiPasswordRequest>
{
    public CambiarMiPasswordValidator()
    {
        RuleFor(x => x.PasswordActual).NotEmpty();
        RuleFor(x => x.PasswordNueva)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.");
    }
}
