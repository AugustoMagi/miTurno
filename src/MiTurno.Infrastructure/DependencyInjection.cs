using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Infrastructure.Auth;
using MiTurno.Infrastructure.BackgroundJobs;
using MiTurno.Infrastructure.Common;
using MiTurno.Infrastructure.Notifications;
using MiTurno.Infrastructure.Pagos;
using MiTurno.Infrastructure.Persistence;
using MiTurno.Infrastructure.Persistence.Repositories;

namespace MiTurno.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no está configurada.");

        services.AddDbContext<MiTurnoDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<INegocioRepository, NegocioRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ISysAdminRepository, SysAdminRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<ISuscripcionRepository, SuscripcionRepository>();
        services.AddScoped<IConfiguracionPagoRepository, ConfiguracionPagoRepository>();
        services.AddScoped<IRecursoRepository, RecursoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IReservaRepository, ReservaRepository>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
        services.AddScoped<IEmailNotificador, SmtpEmailNotificador>();

        services.AddHttpClient<IPagoGateway, MercadoPagoGateway>();
        services.Configure<MercadoPagoPlataformaSettings>(configuration.GetSection(MercadoPagoPlataformaSettings.SectionName));
        services.AddScoped<IPlataformaPagoConfiguracion, PlataformaPagoConfiguracion>();

        services.AddDataProtection();

        services.AddHostedService<SuscripcionVencimientoBackgroundService>();

        return services;
    }
}
