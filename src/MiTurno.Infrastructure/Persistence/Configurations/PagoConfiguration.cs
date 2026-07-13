using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pagos");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Monto).HasPrecision(18, 2);
        builder.Property(p => p.Estado).HasConversion<int>();
        builder.Property(p => p.TransaccionExternalId).HasMaxLength(100);

        // Relación 1:1 (Reserva.Pago es opcional): la unicidad del FK es lo que hace que
        // sea "uno a uno" y no "uno a muchos" a nivel de base de datos.
        builder.HasIndex(p => p.ReservaId).IsUnique();

        // El pago pertenece enteramente a su reserva: no tiene sentido sin ella.
        builder.HasOne<Reserva>()
            .WithOne(r => r.Pago)
            .HasForeignKey<Pago>(p => p.ReservaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
