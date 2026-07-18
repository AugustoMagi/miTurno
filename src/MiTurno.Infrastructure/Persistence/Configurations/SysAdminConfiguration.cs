using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class SysAdminConfiguration : IEntityTypeConfiguration<SysAdmin>
{
    public void Configure(EntityTypeBuilder<SysAdmin> builder)
    {
        builder.ToTable("SysAdmins");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(a => a.Email).HasMaxLength(200).IsRequired();
        builder.Property(a => a.PasswordHash).HasMaxLength(300).IsRequired();

        builder.HasIndex(a => a.Email).IsUnique();
    }
}
