using FluentValidation;
using MiTurno.Application.Features.Recursos.Horarios.Dtos;

namespace MiTurno.Application.Features.Recursos.Horarios;

public class AgregarHorarioDisponibleValidator : AbstractValidator<AgregarHorarioDisponibleRequest>
{
    public AgregarHorarioDisponibleValidator()
    {
        RuleFor(x => x.DiaSemana).IsInEnum();

        RuleFor(x => x)
            .Must(x => x.HoraInicio < x.HoraFin)
            .WithMessage("La hora de inicio debe ser anterior a la hora de fin.");
    }
}
