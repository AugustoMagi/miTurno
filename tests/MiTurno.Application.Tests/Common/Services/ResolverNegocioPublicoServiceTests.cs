using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Services;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Common.Services;

public class ResolverNegocioPublicoServiceTests
{
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();

    private readonly ResolverNegocioPublicoService _service;

    public ResolverNegocioPublicoServiceTests()
    {
        _service = new ResolverNegocioPublicoService(_negocioRepository, _suscripcionRepository);
    }

    [Fact]
    public async Task ResolverAsync_ConNegocioInexistente_DevuelveFailure()
    {
        _negocioRepository.GetBySlugAsync("no-existe").Returns((Negocio?)null);

        var result = await _service.ResolverAsync("no-existe");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Negocio no encontrado.");
    }

    [Fact]
    public async Task ResolverAsync_ConNegocioDesactivado_DevuelveFailure()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        negocio.Desactivar();
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);

        var result = await _service.ResolverAsync(negocio.Slug);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Negocio no encontrado.");
    }

    [Fact]
    public async Task ResolverAsync_ConNegocioSinSuscripcionAsignada_PermiteElAcceso()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _suscripcionRepository.GetByNegocioIdAsync(negocio.Id).Returns((Suscripcion?)null);

        var result = await _service.ResolverAsync(negocio.Slug);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(negocio.Id);
    }

    [Fact]
    public async Task ResolverAsync_ConSuscripcionEnPruebaVigente_PermiteElAcceso()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan);
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _suscripcionRepository.GetByNegocioIdAsync(negocio.Id).Returns(suscripcion);

        var result = await _service.ResolverAsync(negocio.Slug);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ResolverAsync_ConSuscripcionVencida_BloqueaElAcceso()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan, diasPrueba: 1);
        suscripcion.MarcarVencida();
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _suscripcionRepository.GetByNegocioIdAsync(negocio.Id).Returns(suscripcion);

        var result = await _service.ResolverAsync(negocio.Slug);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Negocio no encontrado.");
    }

    [Fact]
    public async Task ResolverAsync_ConSuscripcionCancelada_BloqueaElAcceso()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan);
        suscripcion.Cancelar();
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _suscripcionRepository.GetByNegocioIdAsync(negocio.Id).Returns(suscripcion);

        var result = await _service.ResolverAsync(negocio.Slug);

        result.IsFailure.Should().BeTrue();
    }
}
