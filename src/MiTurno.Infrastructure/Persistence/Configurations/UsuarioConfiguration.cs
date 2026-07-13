using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiTurno.Domain.Entities;

namespace MiTurno.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(300).IsRequired();
        builder.Property(u => u.Rol).HasConversion<int>();

        builder.HasIndex(u => u.Email).IsUnique();

        // Sin navegación inversa en Usuario: el negocio solo se referencia por Id.
        builder.HasOne<Negocio>()
            .WithMany(n => n.Usuarios)
            .HasForeignKey(u => u.NegocioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
