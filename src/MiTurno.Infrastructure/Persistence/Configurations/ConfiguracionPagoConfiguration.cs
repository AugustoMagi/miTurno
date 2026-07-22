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

        builder.Property(c => c.Alias).IsRequired().HasMaxLength(200);

        builder.Property(c => c.AccessToken).HasMaxLength(500);
        builder.Property(c => c.RefreshToken).HasMaxLength(500);

        builder.HasOne<Negocio>()
            .WithMany()
            .HasForeignKey(c => c.NegocioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
