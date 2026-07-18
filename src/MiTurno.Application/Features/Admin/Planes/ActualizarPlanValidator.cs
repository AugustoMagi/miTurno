using FluentValidation;
using MiTurno.Application.Features.Admin.Planes.Dtos;

namespace MiTurno.Application.Features.Admin.Planes;

public class ActualizarPlanValidator : AbstractValidator<ActualizarPlanRequest>
{
    public ActualizarPlanValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Precio).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Periodicidad).IsInEnum();
        RuleFor(x => x.LimiteRecursos).GreaterThan(0);
        RuleFor(x => x.LimiteReservasPorMes).GreaterThan(0);
    }
}
