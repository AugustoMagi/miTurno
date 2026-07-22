using FluentValidation;
using MiTurno.Application.Features.Auth.Dtos;

namespace MiTurno.Application.Features.Auth;

public class SolicitarReseteoPasswordValidator : AbstractValidator<SolicitarReseteoPasswordRequest>
{
    public SolicitarReseteoPasswordValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
    }
}
