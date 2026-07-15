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

        // Un mismo recurso no puede tener dos bloqueos para la misma fecha.
        builder.HasIndex(b => new { b.RecursoId, b.Fecha }).IsUnique();

        builder.HasOne<Recurso>()
            .WithMany(r => r.BloqueosFecha)
            .HasForeignKey(b => b.RecursoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
