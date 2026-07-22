using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Negocios.Dtos;

namespace MiTurno.Application.Features.Negocios;

/// <summary>Datos propios del negocio del usuario autenticado (nombre, link público, contacto), a diferencia de PerfilController que es sobre el Usuario.</summary>
public class ObtenerMiNegocioUseCase
{
    private readonly INegocioRepository _negocioRepository;

    public ObtenerMiNegocioUseCase(INegocioRepository negocioRepository)
    {
        _negocioRepository = negocioRepository;
    }

    public async Task<Result<MiNegocioResponse>> ExecuteAsync(Guid negocioId, CancellationToken cancellationToken = default)
    {
        var negocio = await _negocioRepository.GetByIdAsync(negocioId, cancellationToken);
        if (negocio is null)
            return Result.Failure<MiNegocioResponse>("Negocio no encontrado.");

        return Result.Success(negocio.ToMiNegocioResponse());
    }
}
