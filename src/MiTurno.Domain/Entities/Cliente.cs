using MiTurno.Domain.Common;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

/// <summary>
/// Representa al cliente final que reserva turnos. No tiene credenciales de acceso:
/// se identifica y se le acumula historial por Email entre reservas.
/// </summary>
public class Cliente : BaseEntity
{
    public string Nombre { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Telefono { get; private set; }

    private readonly List<Reserva> _reservas = [];
    public IReadOnlyCollection<Reserva> Reservas => _reservas.AsReadOnly();

    private Cliente() { }

    public static Cliente Crear(string nombre, string email, string? telefono = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El email del cliente es obligatorio.");

        return new Cliente
        {
            Nombre = nombre,
            Email = email.ToLowerInvariant(),
            Telefono = telefono
        };
    }

    public void ActualizarDatosContacto(string nombre, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del cliente es obligatorio.");

        Nombre = nombre;
        Telefono = telefono;
        MarcarActualizado();
    }
}
