using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class BloqueoFechaConfiguration : IEntityTypeConfiguration<BloqueoFecha>
{
    public void Configure(EntityTypeBuilder<BloqueoFecha> builder)
    {
        builder.ToTable("BloqueosFecha");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Motivo).HasMaxLength(300);

        // Ya no es único por fecha: un recurso puede tener varios bloqueos horarios (no
        // superpuestos) el mismo día, además del caso de siempre de un único bloqueo de día
        // completo. Esa regla la aplica Recurso.AgregarBloqueoFecha, no una constraint de BD.
        builder.HasIndex(b => new { b.RecursoId, b.Fecha });

        builder.HasOne<Recurso>()
            .WithMany(r => r.BloqueosFecha)
            .HasForeignKey(b => b.RecursoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
