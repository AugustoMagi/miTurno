using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Suscripciones;

public class ListarSuscripcionesUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();

    private readonly ListarSuscripcionesUseCase _useCase;

    public ListarSuscripcionesUseCaseTests()
    {
        _useCase = new ListarSuscripcionesUseCase(_suscripcionRepository, _negocioRepository);
    }

    [Fact]
    public async Task ExecuteAsync_DevuelveLasSuscripcionesConDatosDelNegocio()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan);

        _suscripcionRepository.GetAllAsync().Returns([suscripcion]);
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);

        var result = await _useCase.ExecuteAsync();

        result.Should().HaveCount(1);
        result[0].NegocioNombre.Should().Be("Cancha Norte");
        result[0].PlanNombre.Should().Be("Básico");
        result[0].Estado.Should().Be(EstadoSuscripcion.EnPrueba);
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripciones_DevuelveListaVacia()
    {
        _suscripcionRepository.GetAllAsync().Returns([]);

        var result = await _useCase.ExecuteAsync();

        result.Should().BeEmpty();
    }
}
