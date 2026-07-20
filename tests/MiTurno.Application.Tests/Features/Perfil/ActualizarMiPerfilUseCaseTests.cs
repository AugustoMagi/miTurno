using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Perfil;
using MiTurno.Application.Features.Perfil.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Perfil;

public class ActualizarMiPerfilUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ActualizarMiPerfilUseCase _useCase;

    public ActualizarMiPerfilUseCaseTests()
    {
        _useCase = new ActualizarMiPerfilUseCase(new ActualizarMiPerfilValidator(), _usuarioRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_ActualizaNombreYEmail()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana Owner", "ana@test.com", "hash", RolUsuario.Owner);
        _usuarioRepository.GetByIdAsync(usuario.Id).Returns(usuario);
        _usuarioRepository.GetByEmailAsync("ana.nueva@test.com").Returns((Usuario?)null);

        var result = await _useCase.ExecuteAsync(
            usuario.Id, new ActualizarMiPerfilRequest("Ana Actualizada", "ana.nueva@test.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Ana Actualizada");
        result.Value.Email.Should().Be("ana.nueva@test.com");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConElMismoEmailQueYaTenia_PermiteActualizar()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana Owner", "ana@test.com", "hash", RolUsuario.Owner);
        _usuarioRepository.GetByIdAsync(usuario.Id).Returns(usuario);
        _usuarioRepository.GetByEmailAsync("ana@test.com").Returns(usuario);

        var result = await _useCase.ExecuteAsync(usuario.Id, new ActualizarMiPerfilRequest("Ana Nuevo Nombre", "ana@test.com"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConEmailUsadoPorOtroUsuario_DevuelveFailure()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana Owner", "ana@test.com", "hash", RolUsuario.Owner);
        var otroUsuario = Usuario.Crear(Guid.NewGuid(), "Otro", "otro@test.com", "hash", RolUsuario.Owner);
        _usuarioRepository.GetByIdAsync(usuario.Id).Returns(usuario);
        _usuarioRepository.GetByEmailAsync("otro@test.com").Returns(otroUsuario);

        var result = await _useCase.ExecuteAsync(usuario.Id, new ActualizarMiPerfilRequest("Ana Owner", "otro@test.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Ya existe un usuario con ese email.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConNombreVacio_DevuelveFailureDeValidacion()
    {
        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), new ActualizarMiPerfilRequest("", "ana@test.com"));

        result.IsFailure.Should().BeTrue();
        await _usuarioRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
