using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Clientes.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Clientes;

/// <summary>
/// Lista los clientes que reservaron al menos una vez en el negocio del usuario autenticado. El
/// Cliente no tiene un FK al Negocio (se identifica por email entre reservas de cualquier negocio),
/// así que el conjunto de "clientes del negocio" se deriva de sus Reservas.
/// </summary>
public class ListarClientesUseCase
{
    private readonly IReservaRepository _reservaRepository;
    private readonly IClienteRepository _clienteRepository;

    public ListarClientesUseCase(IReservaRepository reservaRepository, IClienteRepository clienteRepository)
    {
        _reservaRepository = reservaRepository;
        _clienteRepository = clienteRepository;
    }

    public async Task<IReadOnlyList<ClienteResponse>> ExecuteAsync(
        Guid negocioId, CancellationToken cancellationToken = default)
    {
        var reservas = await _reservaRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (reservas.Count == 0)
            return [];

        var actividadPorCliente = reservas
            .GroupBy(r => r.ClienteId)
            .Select(g => new { ClienteId = g.Key, TotalReservas = g.Count(), UltimaReserva = g.Max(r => r.Fecha) })
            .ToList();

        // Recursos y Clientes son Restrict en el FK de Reservas: nunca se borran mientras
        // tengan reservas asociadas, así que siempre van a aparecer en este diccionario.
        var clientesPorId = new Dictionary<Guid, Cliente>();
        foreach (var actividad in actividadPorCliente)
        {
            var cliente = await _clienteRepository.GetByIdAsync(actividad.ClienteId, cancellationToken);
            clientesPorId[actividad.ClienteId] = cliente!;
        }

        return actividadPorCliente
            .OrderByDescending(a => a.UltimaReserva)
            .Select(a =>
            {
                var cliente = clientesPorId[a.ClienteId];
                return new ClienteResponse(
                    cliente.Id, cliente.Nombre, cliente.Email, cliente.Telefono,
                    a.TotalReservas, a.UltimaReserva);
            })
            .ToList();
    }
}
