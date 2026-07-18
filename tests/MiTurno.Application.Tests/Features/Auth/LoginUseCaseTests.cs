using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Auth;
using MiTurno.Application.Features.Auth.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Auth;

public class LoginUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

    private readonly LoginUseCase _useCase;

    public LoginUseCaseTests()
    {
        _useCase = new LoginUseCase(
            new LoginValidator(), _usuarioRepository, _passwordHasher, _jwtTokenGenerator);
    }

    [Fact]
    public async Task ExecuteAsync_ConCredencialesValidas_DevuelveTokenYDatosDeUsuario()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana Owner", "ana@test.com", "hash-seguro", RolUsuario.Owner);
        _usuarioRepository.GetByEmailAsync("ana@test.com").Returns(usuario);
        _passwordHasher.Verify("Password123", usuario.PasswordHash).Returns(true);
        _jwtTokenGenerator.GenerarToken(usuario).Returns("token-jwt");

        var result = await _useCase.ExecuteAsync(new LoginRequest("ana@test.com", "Password123"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("token-jwt");
        result.Value.UsuarioId.Should().Be(usuario.Id);
        result.Value.Rol.Should().Be(RolUsuario.Owner.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_ConEmailInexistente_DevuelveMensajeGenericoSinRevelarQueNoExiste()
    {
        _usuarioRepository.GetByEmailAsync(Arg.Any<string>()).Returns((Usuario?)null);

        var result = await _useCase.ExecuteAsync(new LoginRequest("no-existe@test.com", "Password123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email o contraseña incorrectos.");
    }

    [Fact]
    public async Task ExecuteAsync_ConPasswordIncorrecta_DevuelveMismoMensajeGenericoQueEmailInexistente()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana Owner", "ana@test.com", "hash-seguro", RolUsuario.Owner);
        _usuarioRepository.GetByEmailAsync("ana@test.com").Returns(usuario);
        _passwordHasher.Verify("password-incorrecta", usuario.PasswordHash).Returns(false);

        var result = await _useCase.ExecuteAsync(new LoginRequest("ana@test.com", "password-incorrecta"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email o contraseña incorrectos.");
    }

    [Fact]
    public async Task ExecuteAsync_ConUsuarioInactivo_DevuelveFailureAunConPasswordCorrecta()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana Owner", "ana@test.com", "hash-seguro", RolUsuario.Owner);
        usuario.Desactivar();
        _usuarioRepository.GetByEmailAsync("ana@test.com").Returns(usuario);
        _passwordHasher.Verify("Password123", usuario.PasswordHash).Returns(true);

        var result = await _useCase.ExecuteAsync(new LoginRequest("ana@test.com", "Password123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email o contraseña incorrectos.");
        _jwtTokenGenerator.DidNotReceive().GenerarToken(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task ExecuteAsync_ConEmailInvalido_DevuelveFailureDeValidacionSinConsultarRepositorio()
    {
        var result = await _useCase.ExecuteAsync(new LoginRequest("no-es-un-email", "Password123"));

        result.IsFailure.Should().BeTrue();
        await _usuarioRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
