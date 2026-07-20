using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Perfil;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Perfil;

public class ObtenerMiPerfilUseCaseTests
{
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();

    private readonly ObtenerMiPerfilUseCase _useCase;

    public ObtenerMiPerfilUseCaseTests()
    {
        _useCase = new ObtenerMiPerfilUseCase(_usuarioRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ConUsuarioExistente_DevuelveSusDatos()
    {
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana Owner", "ana@test.com", "hash", RolUsuario.Owner);
        _usuarioRepository.GetByIdAsync(usuario.Id).Returns(usuario);

        var result = await _useCase.ExecuteAsync(usuario.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Ana Owner");
        result.Value.Email.Should().Be("ana@test.com");
        result.Value.Rol.Should().Be("Owner");
    }

    [Fact]
    public async Task ExecuteAsync_ConUsuarioInexistente_DevuelveFailure()
    {
        _usuarioRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Usuario?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }
}
