using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class CancelarMiSuscripcionUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CancelarMiSuscripcionUseCase _useCase;

    public CancelarMiSuscripcionUseCaseTests()
    {
        _useCase = new CancelarMiSuscripcionUseCase(_suscripcionRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionAsignada_LaCancela()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Cancelada);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripcionAsignada_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsFailure.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
