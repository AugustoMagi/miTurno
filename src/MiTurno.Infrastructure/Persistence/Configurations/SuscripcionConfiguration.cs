using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class SuscripcionConfiguration : IEntityTypeConfiguration<Suscripcion>
{
    public void Configure(EntityTypeBuilder<Suscripcion> builder)
    {
        builder.ToTable("Suscripciones");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Estado).HasConversion<int>();

        // Propiedad calculada (Estado + FechaProximoVencimiento), no tiene columna propia.
        builder.Ignore(s => s.EstaActiva);

        // Sin navegación inversa: la suscripción tiene datos financieros (pagos), no se borra en
        // cascada al eliminar el negocio para evitar perder historial de facturación.
        builder.HasOne<Negocio>()
            .WithMany()
            .HasForeignKey(s => s.NegocioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(s => s.Pagos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
