using FluentValidation;
using MiTurno.Application.Features.ConfiguracionesPago.Dtos;

namespace MiTurno.Application.Features.ConfiguracionesPago;

public class ConectarConfiguracionPagoValidator : AbstractValidator<ConectarConfiguracionPagoRequest>
{
    public ConectarConfiguracionPagoValidator()
    {
        RuleFor(x => x.Proveedor).IsInEnum();
        RuleFor(x => x.Alias).NotEmpty().MaximumLength(200);
    }
}
