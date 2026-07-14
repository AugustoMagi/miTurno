using FluentValidation;
using MiTurno.Application.Features.Public.Dtos;

namespace MiTurno.Application.Features.Public;

public class CrearReservaValidator : AbstractValidator<CrearReservaRequest>
{
    public CrearReservaValidator()
    {
        RuleFor(x => x.ClienteNombre).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ClienteEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.ClienteTelefono).MaximumLength(30);
    }
}
