using MiTurno.Domain.Common;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Domain.Entities;

public class Plan : BaseEntity
{
    public string Nombre { get; private set; } = null!;
    public decimal Precio { get; private set; }
    public Periodicidad Periodicidad { get; private set; }
    public int LimiteRecursos { get; private set; }
    public int LimiteReservasPorMes { get; private set; }
    public bool Activo { get; private set; }
    public bool EsPlanDePrueba { get; private set; }

    private Plan() { }

    public static Plan Crear(string nombre, decimal precio, Periodicidad periodicidad, int limiteRecursos, int limiteReservasPorMes)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del plan es obligatorio.");
        if (precio < 0)
            throw new DomainException("El precio del plan no puede ser negativo.");
        if (limiteRecursos <= 0)
            throw new DomainException("El límite de recursos debe ser mayor a cero.");
        if (limiteReservasPorMes <= 0)
            throw new DomainException("El límite de reservas mensuales debe ser mayor a cero.");

        return new Plan
        {
            Nombre = nombre,
            Precio = precio,
            Periodicidad = periodicidad,
            LimiteRecursos = limiteRecursos,
            LimiteReservasPorMes = limiteReservasPorMes,
            Activo = true
        };
    }

    public void Desactivar()
    {
        Activo = false;
        MarcarActualizado();
    }

    public void Actualizar(string nombre, decimal precio, Periodicidad periodicidad, int limiteRecursos, int limiteReservasPorMes)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del plan es obligatorio.");
        if (precio < 0)
            throw new DomainException("El precio del plan no puede ser negativo.");
        if (limiteRecursos <= 0)
            throw new DomainException("El límite de recursos debe ser mayor a cero.");
        if (limiteReservasPorMes <= 0)
            throw new DomainException("El límite de reservas mensuales debe ser mayor a cero.");

        Nombre = nombre;
        Precio = precio;
        Periodicidad = periodicidad;
        LimiteRecursos = limiteRecursos;
        LimiteReservasPorMes = limiteReservasPorMes;
        MarcarActualizado();
    }

    public void MarcarComoPlanDePrueba()
    {
        EsPlanDePrueba = true;
        MarcarActualizado();
    }

    public void DesmarcarComoPlanDePrueba()
    {
        EsPlanDePrueba = false;
        MarcarActualizado();
    }
}
