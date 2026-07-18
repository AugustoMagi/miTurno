using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Suscripciones;

public class CancelarSuscripcionUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CancelarSuscripcionUseCase _useCase;

    public CancelarSuscripcionUseCaseTests()
    {
        _useCase = new CancelarSuscripcionUseCase(_suscripcionRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionExistente_LaCancela()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan);
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);

        var result = await _useCase.ExecuteAsync(suscripcion.Id);

        result.IsSuccess.Should().BeTrue();
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Cancelada);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionInexistente_DevuelveFailure()
    {
        _suscripcionRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Suscripcion?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Suscripción no encontrada.");
    }
}
