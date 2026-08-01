using MiTurno.Domain.Common;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

public class Negocio : BaseEntity
{
    public string Nombre { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public string? Direccion { get; private set; }
    public string? Telefono { get; private set; }
    public string Email { get; private set; } = null!;
    public bool Activo { get; private set; }

    /// <summary>
    /// Cuántas horas antes del inicio de un turno deja de poder reservarse (ej. 24 = "con un día de
    /// anticipación", 6 = "con 6 horas de anticipación"). 0 significa sin restricción: se puede
    /// reservar hasta el último momento, como era el comportamiento antes de esta configuración.
    /// </summary>
    public int AnticipacionMinimaHoras { get; private set; }

    private readonly List<Recurso> _recursos = [];
    public IReadOnlyCollection<Recurso> Recursos => _recursos.AsReadOnly();

    private readonly List<Usuario> _usuarios = [];
    public IReadOnlyCollection<Usuario> Usuarios => _usuarios.AsReadOnly();

    private Negocio() { }

    public static Negocio Crear(string nombre, string slug, string email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del negocio es obligatorio.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("El slug del negocio es obligatorio.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El email del negocio es obligatorio.");

        return new Negocio
        {
            Nombre = nombre,
            Slug = slug.ToLowerInvariant(),
            Email = email.ToLowerInvariant(),
            Activo = true
        };
    }

    public void ActualizarDatos(
        string nombre, string? descripcion, string? direccion, string? telefono, int anticipacionMinimaHoras)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del negocio es obligatorio.");
        if (anticipacionMinimaHoras < 0)
            throw new DomainException("La anticipación mínima no puede ser negativa.");

        Nombre = nombre;
        Descripcion = descripcion;
        Direccion = direccion;
        Telefono = telefono;
        AnticipacionMinimaHoras = anticipacionMinimaHoras;
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
