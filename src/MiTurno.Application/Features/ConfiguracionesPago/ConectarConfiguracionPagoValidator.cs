using FluentValidation;
using MiTurno.Application.Features.ConfiguracionesPago.Dtos;

namespace MiTurno.Application.Features.ConfiguracionesPago;

public class ConectarConfiguracionPagoValidator : AbstractValidator<ConectarConfiguracionPagoRequest>
{
    public ConectarConfiguracionPagoValidator()
    {
        RuleFor(x => x.Proveedor).IsInEnum();
        RuleFor(x => x.Alias).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AccessToken).MaximumLength(300).When(x => x.AccessToken is not null);
    }
}
