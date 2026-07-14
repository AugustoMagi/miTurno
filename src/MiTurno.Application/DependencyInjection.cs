using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiTurno.Application.Features.Auth;

namespace MiTurno.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<RegistrarNegocioUseCase>();
        services.AddScoped<LoginUseCase>();

        return services;
    }
}
