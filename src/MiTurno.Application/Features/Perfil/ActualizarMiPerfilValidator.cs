using FluentValidation;
using MiTurno.Application.Features.Perfil.Dtos;

namespace MiTurno.Application.Features.Perfil;

public class ActualizarMiPerfilValidator : AbstractValidator<ActualizarMiPerfilRequest>
{
    public ActualizarMiPerfilValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
    }
}
