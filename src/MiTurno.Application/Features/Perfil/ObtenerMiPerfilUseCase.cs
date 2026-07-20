using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Perfil.Dtos;

namespace MiTurno.Application.Features.Perfil;

public class ObtenerMiPerfilUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;

    public ObtenerMiPerfilUseCase(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<Result<MiPerfilResponse>> ExecuteAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
            return Result.Failure<MiPerfilResponse>("Usuario no encontrado.");

        return Result.Success(usuario.ToMiPerfilResponse());
    }
}
