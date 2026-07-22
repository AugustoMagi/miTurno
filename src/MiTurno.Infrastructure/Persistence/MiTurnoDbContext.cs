using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence;

public class MiTurnoDbContext : DbContext
{
    private readonly IDataProtector _accessTokenProtector;
    private readonly IDataProtector _refreshTokenProtector;

    public MiTurnoDbContext(DbContextOptions<MiTurnoDbContext> options, IDataProtectionProvider dataProtectionProvider)
        : base(options)
    {
        _accessTokenProtector = dataProtectionProvider.CreateProtector("MiTurno.ConfiguracionPago.AccessToken");
        _refreshTokenProtector = dataProtectionProvider.CreateProtector("MiTurno.ConfiguracionPago.RefreshToken");
    }

    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<SysAdmin> SysAdmins => Set<SysAdmin>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<Suscripcion> Suscripciones => Set<Suscripcion>();
    public DbSet<PagoSuscripcion> PagosSuscripcion => Set<PagoSuscripcion>();
    public DbSet<ConfiguracionPago> ConfiguracionesPago => Set<ConfiguracionPago>();
    public DbSet<Recurso> Recursos => Set<Recurso>();
    public DbSet<HorarioDisponible> HorariosDisponibles => Set<HorarioDisponible>();
    public DbSet<BloqueoFecha> BloqueosFecha => Set<BloqueoFecha>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Pago> Pagos => Set<Pago>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiTurnoDbContext).Assembly);

        // AccessToken/RefreshToken son credenciales reales (a diferencia del Alias, que es solo un
        // dato de exhibición), así que se cifran en la columna con Data Protection en vez de
        // guardarse en texto plano. Cada una con su propio protector (purpose string distinto), como
        // recomienda Data Protection para separar datos de distinta sensibilidad/uso.
        var accessTokenConverter = new ValueConverter<string?, string?>(
            v => v == null ? null : _accessTokenProtector.Protect(v),
            v => v == null ? null : _accessTokenProtector.Unprotect(v));

        var refreshTokenConverter = new ValueConverter<string?, string?>(
            v => v == null ? null : _refreshTokenProtector.Protect(v),
            v => v == null ? null : _refreshTokenProtector.Unprotect(v));

        modelBuilder.Entity<ConfiguracionPago>()
            .Property(c => c.AccessToken)
            .HasConversion(accessTokenConverter);

        modelBuilder.Entity<ConfiguracionPago>()
            .Property(c => c.RefreshToken)
            .HasConversion(refreshTokenConverter);
    }
}
