using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Auth.Dtos;

namespace MiTurno.Application.Features.Auth;

public class LoginUseCase
{
    private readonly IValidator<LoginRequest> _validator;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUseCase(
        IValidator<LoginRequest> validator,
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _validator = validator;
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginResponse>> ExecuteAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<LoginResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var usuario = await _usuarioRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Mismo mensaje para "no existe" y "contraseña incorrecta": evita revelar
        // a un atacante si un email está registrado (enumeración de usuarios).
        if (usuario is null || !usuario.Activo || !_passwordHasher.Verify(request.Password, usuario.PasswordHash))
            return Result.Failure<LoginResponse>("Email o contraseña incorrectos.");

        var token = _jwtTokenGenerator.GenerarToken(usuario);

        return Result.Success(new LoginResponse(
            usuario.Id, usuario.NegocioId, usuario.Nombre, usuario.Email, usuario.Rol.ToString(), token));
    }
}
