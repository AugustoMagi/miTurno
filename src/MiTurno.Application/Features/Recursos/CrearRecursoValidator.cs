using FluentValidation;
using MiTurno.Application.Features.Recursos.Dtos;

namespace MiTurno.Application.Features.Recursos;

public class CrearRecursoValidator : AbstractValidator<CrearRecursoRequest>
{
    public CrearRecursoValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Tipo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DuracionTurnoMinutos).GreaterThan(0);
        RuleFor(x => x.Precio).GreaterThanOrEqualTo(0);
    }
}
