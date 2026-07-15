using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiTurno.Application.Features.Auth;
using MiTurno.Application.Features.Clientes;
using MiTurno.Application.Features.Recursos;
using MiTurno.Application.Features.Recursos.Bloqueos;
using MiTurno.Application.Features.Recursos.Horarios;
using MiTurno.Application.Features.Public;
using MiTurno.Application.Features.Reservas;

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

        services.AddScoped<AgregarBloqueoFechaUseCase>();
        services.AddScoped<ListarBloqueosFechaUseCase>();
        services.AddScoped<EliminarBloqueoFechaUseCase>();

        services.AddScoped<ObtenerNegocioPublicoUseCase>();
        services.AddScoped<ListarTurnosDisponiblesUseCase>();
        services.AddScoped<CrearReservaUseCase>();
        services.AddScoped<ConfirmarPagoUseCase>();
        services.AddScoped<RechazarPagoUseCase>();

        services.AddScoped<ListarReservasUseCase>();
        services.AddScoped<CancelarReservaUseCase>();

        services.AddScoped<ListarClientesUseCase>();
        services.AddScoped<ObtenerHistorialClienteUseCase>();

        return services;
    }
}
