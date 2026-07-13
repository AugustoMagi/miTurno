using Microsoft.EntityFrameworkCore;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence;

public class MiTurnoDbContext : DbContext
{
    public MiTurnoDbContext(DbContextOptions<MiTurnoDbContext> options) : base(options)
    {
    }

    public DbSet<Negocio> Negocios => Set<Negocio>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
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
    }
}
