using MiTurno.Domain.Common;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

public class Usuario : BaseEntity
{
    public Guid NegocioId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public RolUsuario Rol { get; private set; }
    public bool Activo { get; private set; }

    private Usuario() { }

    public static Usuario Crear(Guid negocioId, string nombre, string email, string passwordHash, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del usuario es obligatorio.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El email del usuario es obligatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("La contraseña es obligatoria.");

        return new Usuario
        {
            NegocioId = negocioId,
            Nombre = nombre,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Rol = rol,
            Activo = true
        };
    }

    public void CambiarPassword(string nuevoPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(nuevoPasswordHash))
            throw new DomainException("La contraseña es obligatoria.");

        PasswordHash = nuevoPasswordHash;
        MarcarActualizado();
    }

    public void Desactivar()
    {
        Activo = false;
        MarcarActualizado();
    }

    public void Activar()
    {
        Activo = true;
        MarcarActualizado();
    }
}
