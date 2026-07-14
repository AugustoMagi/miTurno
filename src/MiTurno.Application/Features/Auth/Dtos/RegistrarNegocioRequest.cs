namespace MiTurno.Application.Features.Auth.Dtos;

/// <summary>Alta de un negocio nuevo junto con su primer usuario (el dueño).</summary>
public record RegistrarNegocioRequest(
    string NombreNegocio,
    string Slug,
    string EmailNegocio,
    string NombreUsuario,
    string EmailUsuario,
    string Password);
