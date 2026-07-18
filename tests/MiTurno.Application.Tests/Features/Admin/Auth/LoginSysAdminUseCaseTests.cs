using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Auth;
using MiTurno.Application.Features.Admin.Auth.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Admin.Auth;

public class LoginSysAdminUseCaseTests
{
    private readonly ISysAdminRepository _sysAdminRepository = Substitute.For<ISysAdminRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();

    private readonly LoginSysAdminUseCase _useCase;

    public LoginSysAdminUseCaseTests()
    {
        _useCase = new LoginSysAdminUseCase(
            new LoginSysAdminValidator(), _sysAdminRepository, _passwordHasher, _jwtTokenGenerator);
    }

    [Fact]
    public async Task ExecuteAsync_ConCredencialesValidas_DevuelveToken()
    {
        var admin = SysAdmin.Crear("Augusto", "admin@miturno.com", "hash-seguro");
        _sysAdminRepository.GetByEmailAsync(admin.Email).Returns(admin);
        _passwordHasher.Verify("Password123", admin.PasswordHash).Returns(true);
        _jwtTokenGenerator.GenerarToken(admin).Returns("token-jwt");

        var result = await _useCase.ExecuteAsync(new LoginSysAdminRequest(admin.Email, "Password123"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("token-jwt");
        result.Value.Email.Should().Be(admin.Email);
    }

    [Fact]
    public async Task ExecuteAsync_ConPasswordIncorrecta_DevuelveFailure()
    {
        var admin = SysAdmin.Crear("Augusto", "admin@miturno.com", "hash-seguro");
        _sysAdminRepository.GetByEmailAsync(admin.Email).Returns(admin);
        _passwordHasher.Verify("mala", admin.PasswordHash).Returns(false);

        var result = await _useCase.ExecuteAsync(new LoginSysAdminRequest(admin.Email, "mala"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email o contraseña incorrectos.");
    }

    [Fact]
    public async Task ExecuteAsync_ConAdminInexistente_DevuelveFailure()
    {
        _sysAdminRepository.GetByEmailAsync("no-existe@miturno.com").Returns((SysAdmin?)null);

        var result = await _useCase.ExecuteAsync(new LoginSysAdminRequest("no-existe@miturno.com", "Password123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email o contraseña incorrectos.");
    }

    [Fact]
    public async Task ExecuteAsync_ConAdminInactivo_DevuelveFailure()
    {
        var admin = SysAdmin.Crear("Augusto", "admin@miturno.com", "hash-seguro");
        admin.Desactivar();
        _sysAdminRepository.GetByEmailAsync(admin.Email).Returns(admin);
        _passwordHasher.Verify("Password123", admin.PasswordHash).Returns(true);

        var result = await _useCase.ExecuteAsync(new LoginSysAdminRequest(admin.Email, "Password123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email o contraseña incorrectos.");
    }
}
