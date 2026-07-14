namespace MiTurno.Application.Features.Auth.Dtos;

public record LoginResponse(
    Guid UsuarioId,
    Guid NegocioId,
    string Nombre,
    string Email,
    string Rol,
    string Token);
