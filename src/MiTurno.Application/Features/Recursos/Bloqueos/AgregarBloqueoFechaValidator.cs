using FluentValidation;
using MiTurno.Application.Features.Recursos.Bloqueos.Dtos;

namespace MiTurno.Application.Features.Recursos.Bloqueos;

public class AgregarBloqueoFechaValidator : AbstractValidator<AgregarBloqueoFechaRequest>
{
    public AgregarBloqueoFechaValidator()
    {
        RuleFor(x => x.Fecha)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("No se pueden bloquear fechas pasadas.");

        RuleFor(x => x.Motivo).MaximumLength(300);
    }
}
