namespace MiTurno.Application.Features.Perfil.Dtos;

public record MiPerfilResponse(
    Guid Id,
    string Nombre,
    string Email,
    string Rol);
