using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class NegocioConfiguration : IEntityTypeConfiguration<Negocio>
{
    public void Configure(EntityTypeBuilder<Negocio> builder)
    {
        builder.ToTable("Negocios");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(n => n.Slug).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Descripcion).HasMaxLength(500);
        builder.Property(n => n.Direccion).HasMaxLength(250);
        builder.Property(n => n.Telefono).HasMaxLength(30);
        builder.Property(n => n.Email).HasMaxLength(200).IsRequired();

        // El slug identifica al negocio en el link público de reservas (ej. instagram bio link).
        builder.HasIndex(n => n.Slug).IsUnique();

        // Recursos y Usuarios se exponen como IReadOnlyCollection respaldada por List<T> privada:
        // EF no puede mutarlas vía la propiedad (no tiene Add), debe acceder al campo directamente.
        builder.Navigation(n => n.Recursos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(n => n.Usuarios).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
