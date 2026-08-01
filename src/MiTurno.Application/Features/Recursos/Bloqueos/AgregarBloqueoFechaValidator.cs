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

        RuleFor(x => x)
            .Must(x => (x.HoraInicio is null) == (x.HoraFin is null))
            .WithMessage("Si cargás un horario, completá tanto el inicio como el fin.");

        RuleFor(x => x)
            .Must(x => x.HoraInicio < x.HoraFin)
            .When(x => x.HoraInicio is not null && x.HoraFin is not null)
            .WithMessage("El horario \"desde\" debe ser anterior al \"hasta\".");
    }
}
