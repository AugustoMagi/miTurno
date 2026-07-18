using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Public;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Public;

public class ObtenerNegocioPublicoUseCaseTests
{
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();

    private readonly ObtenerNegocioPublicoUseCase _useCase;

    public ObtenerNegocioPublicoUseCaseTests()
    {
        var resolverNegocioPublicoService = new ResolverNegocioPublicoService(_negocioRepository, _suscripcionRepository);
        _useCase = new ObtenerNegocioPublicoUseCase(resolverNegocioPublicoService, _recursoRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ConNegocioActivo_DevuelveSoloSusRecursosActivos()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var recursoActivo = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var recursoInactivo = Recurso.Crear(negocio.Id, "Cancha 2", "Futbol", TimeSpan.FromHours(1), 5000m);
        recursoInactivo.Desactivar();

        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _recursoRepository.GetByNegocioIdAsync(negocio.Id).Returns([recursoActivo, recursoInactivo]);

        var result = await _useCase.ExecuteAsync(negocio.Slug);

        result.IsSuccess.Should().BeTrue();
        result.Value.Recursos.Should().HaveCount(1);
        result.Value.Recursos[0].Id.Should().Be(recursoActivo.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ConNegocioInexistente_DevuelveFailure()
    {
        _negocioRepository.GetBySlugAsync("no-existe").Returns((Negocio?)null);

        var result = await _useCase.ExecuteAsync("no-existe");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Negocio no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConNegocioDesactivado_DevuelveFailure()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        negocio.Desactivar();
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);

        var result = await _useCase.ExecuteAsync(negocio.Slug);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Negocio no encontrado.");
    }
}
