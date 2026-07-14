using FluentValidation;
using MiTurno.Application.Features.Auth.Dtos;

namespace MiTurno.Application.Features.Auth;

public class RegistrarNegocioValidator : AbstractValidator<RegistrarNegocioRequest>
{
    public RegistrarNegocioValidator()
    {
        RuleFor(x => x.NombreNegocio).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("El slug solo puede contener minúsculas, números y guiones.");

        RuleFor(x => x.EmailNegocio).NotEmpty().EmailAddress().MaximumLength(200);

        RuleFor(x => x.NombreUsuario).NotEmpty().MaximumLength(150);
        RuleFor(x => x.EmailUsuario).NotEmpty().EmailAddress().MaximumLength(200);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("La contraseña debe tener al menos 8 caracteres.");
    }
}
