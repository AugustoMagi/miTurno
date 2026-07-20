using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Suscripciones;
using MiTurno.Application.Features.Admin.Suscripciones.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Suscripciones;

public class RenovarSuscripcionManualUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RenovarSuscripcionManualUseCase _useCase;

    public RenovarSuscripcionManualUseCaseTests()
    {
        _useCase = new RenovarSuscripcionManualUseCase(_suscripcionRepository, _negocioRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConFechaPosteriorALaActual_RenuevaLaSuscripcion()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan);
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);

        var nuevoVencimiento = suscripcion.FechaProximoVencimiento.AddMonths(1);
        var result = await _useCase.ExecuteAsync(suscripcion.Id, new RenovarSuscripcionManualRequest(nuevoVencimiento));

        result.IsSuccess.Should().BeTrue();
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Activa);
        suscripcion.FechaProximoVencimiento.Should().Be(nuevoVencimiento);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConFechaAnteriorALaActual_DevuelveFailureDelDominio()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan);
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);

        var fechaAnterior = suscripcion.FechaProximoVencimiento.AddDays(-1);
        var result = await _useCase.ExecuteAsync(suscripcion.Id, new RenovarSuscripcionManualRequest(fechaAnterior));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionCancelada_LaReactivaEnVezDeFallar()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan);
        suscripcion.Cancelar();
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);

        var nuevoVencimiento = suscripcion.FechaProximoVencimiento.AddMonths(1);
        var result = await _useCase.ExecuteAsync(suscripcion.Id, new RenovarSuscripcionManualRequest(nuevoVencimiento));

        result.IsSuccess.Should().BeTrue();
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Activa);
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionInexistente_DevuelveFailure()
    {
        _suscripcionRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Suscripcion?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), new RenovarSuscripcionManualRequest(DateTime.UtcNow));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Suscripción no encontrada.");
    }
}
