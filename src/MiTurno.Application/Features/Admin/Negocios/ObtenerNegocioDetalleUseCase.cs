using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Admin.Negocios.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Features.Admin.Negocios;

/// <summary>
/// Detalle de un negocio para el SysAdmin: sus recursos, cada uno con los horarios que ofrece y
/// todas sus reservas (con estado), para poder auditar pendientes/canceladas/completadas sin
/// depender de que el dueño le pase esa información.
/// </summary>
public class ObtenerNegocioDetalleUseCase
{
    private readonly INegocioRepository _negocioRepository;
    private readonly IRecursoRepository _recursoRepository;
    private readonly IReservaRepository _reservaRepository;
    private readonly IClienteRepository _clienteRepository;

    public ObtenerNegocioDetalleUseCase(
        INegocioRepository negocioRepository,
        IRecursoRepository recursoRepository,
        IReservaRepository reservaRepository,
        IClienteRepository clienteRepository)
    {
        _negocioRepository = negocioRepository;
        _recursoRepository = recursoRepository;
        _reservaRepository = reservaRepository;
        _clienteRepository = clienteRepository;
    }

    public async Task<Result<NegocioDetalleAdminResponse>> ExecuteAsync(
        Guid negocioId, CancellationToken cancellationToken = default)
    {
        var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
        if (negocio is null)
            return Result.Failure<NegocioDetalleAdminResponse>("Negocio no encontrado.");

        var recursos = await _recursoRepository.GetByNegocioIdAsync(negocioId, cancellationToken);
        var reservas = await _reservaRepository.GetByNegocioIdAsync(negocioId, cancellationToken);

        // Cliente es Restrict en el FK de Reservas: nunca se borra mientras tenga reservas asociadas.
        var clientesPorId = new Dictionary<Guid, Cliente>();
        foreach (var clienteId in reservas.Select(r => r.ClienteId).Distinct())
        {
            var cliente = await _clienteRepository.GetByIdAsync(clienteId, cancellationToken);
            clientesPorId[clienteId] = cliente!;
        }

        var recursosResponse = new List<RecursoDetalleAdminResponse>();
        foreach (var recurso in recursos)
        {
            var recursoConHorarios = await _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id, cancellationToken);
            var horarios = recursoConHorarios!.HorariosDisponibles
                .Select(h => new HorarioAdminResponse(h.Id, h.DiaSemana, h.HoraInicio, h.HoraFin))
                .OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
                .ToList();

            var reservasDelRecurso = reservas
                .Where(r => r.RecursoId == recurso.Id)
                .Select(r =>
                {
                    var cliente = clientesPorId[r.ClienteId];
                    return new ReservaAdminResponse(
                        r.Id, r.Fecha, r.HoraInicio, r.HoraFin, cliente.Nombre, cliente.Email, r.Estado, r.PrecioTotal);
                })
                .OrderByDescending(r => r.Fecha).ThenBy(r => r.HoraInicio)
                .ToList();

            recursosResponse.Add(new RecursoDetalleAdminResponse(
                recurso.Id,
                recurso.Nombre,
                recurso.Tipo,
                (int)recurso.DuracionTurno.TotalMinutes,
                recurso.Precio,
                recurso.Activo,
                horarios,
                reservasDelRecurso));
        }

        return Result.Success(new NegocioDetalleAdminResponse(
            negocio.Id, negocio.Nombre, negocio.Slug, negocio.Email, negocio.Activo, recursosResponse));
    }
}
