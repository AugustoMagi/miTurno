using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class PagoSuscripcionConfiguration : IEntityTypeConfiguration<PagoSuscripcion>
{
    public void Configure(EntityTypeBuilder<PagoSuscripcion> builder)
    {
        builder.ToTable("PagosSuscripcion");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Monto).HasPrecision(18, 2);
        builder.Property(p => p.Estado).HasConversion<int>();
        builder.Property(p => p.TransaccionExternalId).HasMaxLength(100);

        // Registro financiero: se protege de borrado en cascada al eliminar la suscripción.
        builder.HasOne<Suscripcion>()
            .WithMany(s => s.Pagos)
            .HasForeignKey(p => p.SuscripcionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
