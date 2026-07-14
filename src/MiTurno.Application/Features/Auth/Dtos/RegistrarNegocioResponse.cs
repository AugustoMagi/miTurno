namespace MiTurno.Application.Features.Auth.Dtos;

public record RegistrarNegocioResponse(
    Guid NegocioId,
    string NegocioSlug,
    Guid UsuarioId,
    string Token);
