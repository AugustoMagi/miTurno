namespace MiTurno.Application.Features.Auth.Dtos;

public record RegistrarNegocioResponse(
    Guid UsuarioId,
    Guid NegocioId,
    string NegocioSlug,
    string Nombre,
    string Email,
    string Rol,
    string Token);
