using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Perfil;
using MiTurno.Application.Features.Perfil.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Perfil;

public class CambiarMiPasswordUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CambiarMiPasswordUseCase _useCase;

    public CambiarMiPasswordUseCaseTests()
    {
        _useCase = new CambiarMiPasswordUseCase(
            new CambiarMiPasswordValidator(), _usuarioRepository, _passwordHasher, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConPasswordActualCorrecta_CambiaLaPassword()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana Owner", "ana@test.com", "hash-viejo", RolUsuario.Owner);
        _usuarioRepository.GetByIdAsync(usuario.Id).Returns(usuario);
        _passwordHasher.Verify("actual123", "hash-viejo").Returns(true);
        _passwordHasher.Hash("nueva12345").Returns("hash-nuevo");

        var result = await _useCase.ExecuteAsync(usuario.Id, new CambiarMiPasswordRequest("actual123", "nueva12345"));

        result.IsSuccess.Should().BeTrue();
        usuario.PasswordHash.Should().Be("hash-nuevo");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPasswordActualIncorrecta_DevuelveFailureSinGuardar()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana Owner", "ana@test.com", "hash-viejo", RolUsuario.Owner);
        _usuarioRepository.GetByIdAsync(usuario.Id).Returns(usuario);
        _passwordHasher.Verify("incorrecta", "hash-viejo").Returns(false);

        var result = await _useCase.ExecuteAsync(usuario.Id, new CambiarMiPasswordRequest("incorrecta", "nueva12345"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("La contraseña actual es incorrecta.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPasswordNuevaCorta_DevuelveFailureDeValidacion()
    {
        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), new CambiarMiPasswordRequest("actual123", "corta"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("al menos 8 caracteres");
        await _usuarioRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
