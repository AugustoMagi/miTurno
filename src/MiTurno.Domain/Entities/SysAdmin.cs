using MiTurno.Domain.Common;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

/// <summary>
/// Cuenta de plataforma de MiTurno (no de un Negocio): fija los precios de los Plan y
/// gestiona las Suscripcion de los negocios. Sin relación con Negocio/Usuario.
/// </summary>
public class SysAdmin : BaseEntity
{
    public string Nombre { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool Activo { get; private set; }

    private SysAdmin() { }

    public static SysAdmin Crear(string nombre, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El email del administrador es obligatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("La contraseña es obligatoria.");

        return new SysAdmin
        {
            Nombre = nombre,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            Activo = true
        };
    }

    public void Desactivar()
    {
        Activo = false;
        MarcarActualizado();
    }
}
