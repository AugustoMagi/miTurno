using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Public.Dtos;

namespace MiTurno.Application.Features.Public;

/// <summary>
/// Resuelve el negocio a partir del slug de su link público (ej. el de Instagram) y devuelve
/// sus recursos activos, para que el cliente elija cuál reservar.
/// </summary>
public class ObtenerNegocioPublicoUseCase
{
    private readonly ResolverNegocioPublicoService _resolverNegocioPublicoService;
    private readonly IRecursoRepository _recursoRepository;

    public ObtenerNegocioPublicoUseCase(
        ResolverNegocioPublicoService resolverNegocioPublicoService, IRecursoRepository recursoRepository)
    {
        _resolverNegocioPublicoService = resolverNegocioPublicoService;
        _recursoRepository = recursoRepository;
    }

    public async Task<Result<NegocioPublicoResponse>> ExecuteAsync(string slug, CancellationToken cancellationToken = default)
    {
        var negocioResult = await _resolverNegocioPublicoService.ResolverAsync(slug, cancellationToken);
        if (negocioResult.IsFailure)
            return Result.Failure<NegocioPublicoResponse>(negocioResult.Error!);
        var negocio = negocioResult.Value;

        var recursos = await _recursoRepository.GetByNegocioIdAsync(negocio.Id, cancellationToken);

        var recursosActivos = recursos
            .Where(r => r.Activo)
            .Select(r => new RecursoPublicoResponse(r.Id, r.Nombre, r.Tipo, (int)r.DuracionTurno.TotalMinutes, r.Precio))
            .ToList();

        return Result.Success(new NegocioPublicoResponse(
            negocio.Id, negocio.Nombre, negocio.Slug, negocio.Descripcion, negocio.Direccion, recursosActivos));
    }
}
