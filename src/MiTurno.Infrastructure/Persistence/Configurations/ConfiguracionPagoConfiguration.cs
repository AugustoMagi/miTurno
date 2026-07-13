using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class ConfiguracionPagoConfiguration : IEntityTypeConfiguration<ConfiguracionPago>
{
    public void Configure(EntityTypeBuilder<ConfiguracionPago> builder)
    {
        builder.ToTable("ConfiguracionesPago");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Proveedor).HasConversion<int>();

        // Puede ser un blob de OAuth (tokens/refresh token en JSON); sin límite de longitud fijo.
        builder.Property(c => c.CredencialesOAuth).IsRequired();

        builder.HasOne<Negocio>()
            .WithMany()
            .HasForeignKey(c => c.NegocioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
