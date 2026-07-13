using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Planes");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Precio).HasPrecision(18, 2);
        builder.Property(p => p.Periodicidad).HasConversion<int>();
    }
}
