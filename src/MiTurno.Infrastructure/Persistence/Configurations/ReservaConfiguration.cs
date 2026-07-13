using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.ToTable("Reservas");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Estado).HasConversion<int>();
        builder.Property(r => r.PrecioTotal).HasPrecision(18, 2);

        // Consulta habitual: reservas de un recurso en una fecha determinada.
        builder.HasIndex(r => new { r.RecursoId, r.Fecha });

        // Registro transaccional: nunca se borra en cascada al eliminar el Recurso o el Cliente
        // (se desactivan/soft-delete en su lugar) para no perder el historial de reservas.
        builder.HasOne<Recurso>()
            .WithMany(rec => rec.Reservas)
            .HasForeignKey(r => r.RecursoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Cliente>()
            .WithMany(c => c.Reservas)
            .HasForeignKey(r => r.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
