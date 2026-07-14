using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Reservas.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Reservas;

/// <summary>Lista las reservas de todos los recursos del negocio del usuario autenticado.</summary>
public class ListarReservasUseCase
{
    private readonly IReservaRepository _reservaRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IClienteRepository _clienteRepository;

    public ListarReservasUseCase(
        IReservaRepository reservaRepository,
        IRecursoRepository recursoRepository,
        IClienteRepository clienteRepository)
    {
        _reservaRepository = reservaRepository;
        _recursoRepository = recursoRepository;
        _clienteRepository = clienteRepository;
    }

    public async Task<IReadOnlyList<ReservaOwnerResponse>> ExecuteAsync(
        Guid negocioId, CancellationToken cancellationToken = default)
    {
        var reservas = await _reservaRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        if (reservas.Count == 0)
            return [];

        var recursos = await _recursoRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        var recursoNombrePorId = recursos.ToDictionary(r => r.Id, r => r.Nombre);

        // Recursos y Clientes son Restrict en el FK de Reservas: nunca se borran mientras
        // tengan reservas asociadas, así que siempre van a aparecer en estos diccionarios.
        var clientesPorId = new Dictionary<Guid, Cliente>();
        foreach (var clienteId in reservas.Select(r => r.ClienteId).Distinct())
        {
            var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);
            clientesPorId[clienteId] = cliente!;
        }

        return reservas
            .Select(r =>
            {
                var cliente = clientesPorId[r.ClienteId];
                return new ReservaOwnerResponse(
                    r.Id,
                    r.RecursoId,
                    recursoNombrePorId[r.RecursoId],
                    r.ClienteId,
                    cliente.Nombre,
                    cliente.Email,
                    r.Fecha,
                    r.HoraInicio,
                    r.HoraFin,
                    r.PrecioTotal,
                    r.Estado);
            })
            .ToList();
    }
}
