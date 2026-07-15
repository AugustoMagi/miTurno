using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Clientes.Dtos;

namespace MiTurno.Application.Features.Clientes;

/// <summary>
/// Muestra los datos de un cliente y su historial de reservas en el negocio del usuario
/// autenticado. Se considera "no encontrado" si el cliente existe pero nunca reservó en este
/// negocio, para no exponer clientes de otros negocios.
/// </summary>
public class ObtenerHistorialClienteUseCase
{
    private readonly IReservaRepository _reservaRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IClienteRepository _clienteRepository;

    public ObtenerHistorialClienteUseCase(
        IReservaRepository reservaRepository,
        IRecursoRepository recursoRepository,
        IClienteRepository clienteRepository)
    {
        _reservaRepository = reservaRepository;
        _recursoRepository = recursoRepository;
        _clienteRepository = clienteRepository;
    }

    public async Task<Result<HistorialClienteResponse>> ExecuteAsync(
        Guid negocioId, Guid clienteId, CancellationToken cancellationToken = default)
    {
        var reservas = (await _reservaRepository.GetByNegocioIdAsync(negocioId, cancellationToken))
            .Where(r => r.ClienteId == clienteId)
            .ToList();
        if (reservas.Count == 0)
            return Result.Failure<HistorialClienteResponse>("Cliente no encontrado.");

        var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);

        var recursos = await _recursoRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        var recursoNombrePorId = recursos.ToDictionary(r => r.Id, r => r.Nombre);

        var historial = reservas
            .OrderByDescending(r => r.Fecha).ThenByDescending(r => r.HoraInicio)
            .Select(r => new ReservaClienteResponse(
                r.Id, recursoNombrePorId[r.RecursoId], r.Fecha, r.HoraInicio, r.HoraFin, r.PrecioTotal, r.Estado))
            .ToList();

        return Result.Success(new HistorialClienteResponse(
            cliente!.Id, cliente.Nombre, cliente.Email, cliente.Telefono, historial));
    }
}
