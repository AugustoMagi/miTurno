using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class ObtenerMiSuscripcionUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();

    private readonly ObtenerMiSuscripcionUseCase _useCase;

    public ObtenerMiSuscripcionUseCaseTests()
    {
        _useCase = new ObtenerMiSuscripcionUseCase(_suscripcionRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionAsignada_DevuelveSusDatos()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanNombre.Should().Be("Básico");
        result.Value.Estado.Should().Be(EstadoSuscripcion.EnPrueba);
        result.Value.EstaActiva.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripcionAsignada_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsFailure.Should().BeTrue();
    }
}
