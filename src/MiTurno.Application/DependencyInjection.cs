using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Admin.Auth;
using MiTurno.Application.Features.Admin.Facturacion;
using MiTurno.Application.Features.Admin.Negocios;
using MiTurno.Application.Features.Admin.Planes;
using MiTurno.Application.Features.Admin.Suscripciones;
using MiTurno.Application.Features.Auth;
using MiTurno.Application.Features.Clientes;
using MiTurno.Application.Features.ConfiguracionesPago;
using MiTurno.Application.Features.Estadisticas;
using MiTurno.Application.Features.Negocios;
using MiTurno.Application.Features.Recursos;
using MiTurno.Application.Features.Recursos.Bloqueos;
using MiTurno.Application.Features.Recursos.Horarios;
using MiTurno.Application.Features.Perfil;
using MiTurno.Application.Features.Public;
using MiTurno.Application.Features.Reservas;
using MiTurno.Application.Features.Suscripciones;

namespace MiTurno.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<ResolverNegocioPublicoService>();

        services.AddScoped<RegistrarNegocioUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<LoginSysAdminUseCase>();
        services.AddScoped<SolicitarReseteoPasswordUseCase>();
        services.AddScoped<RestablecerPasswordUseCase>();

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
        services.AddScoped<ListarPlanesPublicosUseCase>();
        services.AddScoped<ListarTurnosDisponiblesUseCase>();
        services.AddScoped<CrearReservaUseCase>();
        services.AddScoped<CancelarReservaClienteUseCase>();
        services.AddScoped<ProcesarNotificacionPagoMercadoPagoUseCase>();

        services.AddScoped<ListarReservasUseCase>();
        services.AddScoped<CancelarReservaUseCase>();
        services.AddScoped<ConfirmarPagoUseCase>();
        services.AddScoped<RechazarPagoUseCase>();

        services.AddScoped<ListarClientesUseCase>();
        services.AddScoped<ObtenerHistorialClienteUseCase>();

        services.AddScoped<ConectarConfiguracionPagoUseCase>();
        services.AddScoped<ObtenerConfiguracionPagoUseCase>();
        services.AddScoped<DesconectarConfiguracionPagoUseCase>();
        services.AddScoped<IniciarConexionMercadoPagoUseCase>();
        services.AddScoped<ProcesarCallbackMercadoPagoUseCase>();
        services.AddScoped<RenovarConexionesMercadoPagoUseCase>();

        services.AddScoped<ObtenerEstadisticasOcupacionUseCase>();

        services.AddScoped<ObtenerMiPerfilUseCase>();
        services.AddScoped<ActualizarMiPerfilUseCase>();
        services.AddScoped<CambiarMiPasswordUseCase>();

        services.AddScoped<ObtenerMiNegocioUseCase>();
        services.AddScoped<ActualizarMiNegocioUseCase>();

        services.AddScoped<CrearPlanUseCase>();
        services.AddScoped<ActualizarPlanUseCase>();
        services.AddScoped<ListarPlanesUseCase>();
        services.AddScoped<DesactivarPlanUseCase>();
        services.AddScoped<EliminarPlanUseCase>();
        services.AddScoped<MarcarPlanDePruebaUseCase>();
        services.AddScoped<DesmarcarPlanDePruebaUseCase>();

        services.AddScoped<ListarSuscripcionesUseCase>();
        services.AddScoped<CambiarPlanSuscripcionUseCase>();
        services.AddScoped<RenovarSuscripcionManualUseCase>();
        services.AddScoped<CancelarSuscripcionUseCase>();

        services.AddScoped<ObtenerFacturacionPlataformaUseCase>();

        services.AddScoped<ListarNegociosUseCase>();
        services.AddScoped<ObtenerNegocioDetalleUseCase>();
        services.AddScoped<CambiarEstadoNegocioUseCase>();

        services.AddScoped<ObtenerMiSuscripcionUseCase>();
        services.AddScoped<CambiarPlanMiSuscripcionUseCase>();
        services.AddScoped<CancelarMiSuscripcionUseCase>();
        services.AddScoped<IniciarSuscripcionMercadoPagoUseCase>();
        services.AddScoped<ProcesarNotificacionRecurrenteUseCase>();
        services.AddScoped<NotificarSuscripcionesPorVencerUseCase>();

        return services;
    }
}
