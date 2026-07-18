using FluentValidation;
using MiTurno.Application.Features.Admin.Planes.Dtos;

namespace MiTurno.Application.Features.Admin.Planes;

public class CrearPlanValidator : AbstractValidator<CrearPlanRequest>
{
    public CrearPlanValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Precio).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Periodicidad).IsInEnum();
        RuleFor(x => x.LimiteRecursos).GreaterThan(0);
        RuleFor(x => x.LimiteReservasPorMes).GreaterThan(0);
    }
}
