using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class RecursoConfiguration : IEntityTypeConfiguration<Recurso>
{
    public void Configure(EntityTypeBuilder<Recurso> builder)
    {
        builder.ToTable("Recursos");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(r => r.Tipo).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Precio).HasPrecision(18, 2);

        builder.HasOne<Negocio>()
            .WithMany(n => n.Recursos)
            .HasForeignKey(r => r.NegocioId)
            .OnDelete(DeleteBehavior.Cascade);

        // HorariosDisponibles, BloqueosFecha y Reservas son IReadOnlyCollection respaldadas por
        // List<T> privada: EF debe acceder al campo, no hay setter/Add en la propiedad pública.
        builder.Navigation(r => r.HorariosDisponibles).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(r => r.BloqueosFecha).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(r => r.Reservas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
