namespace MiTurno.Application.Features.Auth.Dtos;

public record RestablecerPasswordRequest(string Token, string PasswordNueva);
