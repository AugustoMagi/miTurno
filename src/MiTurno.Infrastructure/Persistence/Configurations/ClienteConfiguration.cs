using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Telefono).HasMaxLength(30);

        // Sin login: el cliente se busca y se le acumula historial por Email entre reservas.
        builder.HasIndex(c => c.Email);

        builder.Navigation(c => c.Reservas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
