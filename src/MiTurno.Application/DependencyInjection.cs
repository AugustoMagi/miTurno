using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiTurno.Application.Features.Auth;
using MiTurno.Application.Features.Recursos;
using MiTurno.Application.Features.Recursos.Horarios;

namespace MiTurno.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<RegistrarNegocioUseCase>();
        services.AddScoped<LoginUseCase>();

        services.AddScoped<CrearRecursoUseCase>();
        services.AddScoped<ListarRecursosUseCase>();
        services.AddScoped<ObtenerRecursoUseCase>();
        services.AddScoped<ActualizarRecursoUseCase>();
        services.AddScoped<CambiarEstadoRecursoUseCase>();

        services.AddScoped<AgregarHorarioDisponibleUseCase>();
        services.AddScoped<ListarHorariosDisponiblesUseCase>();
        services.AddScoped<EliminarHorarioDisponibleUseCase>();

        return services;
    }
}
