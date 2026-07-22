using FluentValidation;
using MiTurno.Application.Features.Negocios.Dtos;

namespace MiTurno.Application.Features.Negocios;

public class ActualizarMiNegocioValidator : AbstractValidator<ActualizarMiNegocioRequest>
{
    public ActualizarMiNegocioValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Descripcion).MaximumLength(500).When(x => x.Descripcion is not null);
        RuleFor(x => x.Direccion).MaximumLength(200).When(x => x.Direccion is not null);
        RuleFor(x => x.Telefono).MaximumLength(30).When(x => x.Telefono is not null);
    }
}
