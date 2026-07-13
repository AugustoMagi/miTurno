using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class HorarioDisponibleConfiguration : IEntityTypeConfiguration<HorarioDisponible>
{
    public void Configure(EntityTypeBuilder<HorarioDisponible> builder)
    {
        builder.ToTable("HorariosDisponibles");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.DiaSemana).HasConversion<int>();

        builder.HasOne<Recurso>()
            .WithMany(r => r.HorariosDisponibles)
            .HasForeignKey(h => h.RecursoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
