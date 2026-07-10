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

    public void ActualizarDatos(string nombre, string? descripcion, string? direccion, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del negocio es obligatorio.");

        Nombre = nombre;
        Descripcion = descripcion;
        Direccion = direccion;
        Telefono = telefono;
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
