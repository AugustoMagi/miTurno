using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Negocios.Dtos;

namespace MiTurno.Application.Features.Admin.Negocios;

/// <summary>Todos los negocios de la plataforma, para que el SysAdmin los navegue.</summary>
public class ListarNegociosUseCase
{
    private readonly INegocioRepository _negocioRepository;

    public ListarNegociosUseCase(INegocioRepository negocioRepository)
    {
        _negocioRepository = negocioRepository;
    }

    public async Task<IReadOnlyList<NegocioAdminResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var negocios = await _negocioRepository.GetAllAsync(cancellationToken);
        return negocios.Select(n => n.ToResponse()).ToList();
    }
}
