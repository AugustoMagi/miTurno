using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Admin.Planes;
using MiTurno.Application.Features.Admin.Planes.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Admin.Planes;

public class ActualizarPlanUseCaseTests
{
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ActualizarPlanUseCase _useCase;

    public ActualizarPlanUseCaseTests()
    {
        _useCase = new ActualizarPlanUseCase(
            new ActualizarPlanValidator(), _planRepository, _suscripcionRepository,
            _plataformaPagoConfiguracion, _pagoRecurrenteGateway, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanExistente_ActualizaSusDatos()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        _planRepository.GetByIdAsync(plan.Id).Returns(plan);

        var result = await _useCase.ExecuteAsync(
            plan.Id, new ActualizarPlanRequest("Básico Plus", 7000m, Periodicidad.Anual, 5, 500));

        result.IsSuccess.Should().BeTrue();
        result.Value.Nombre.Should().Be("Básico Plus");
        result.Value.Precio.Should().Be(7000m);
        result.Value.Periodicidad.Should().Be(Periodicidad.Anual);
        _planRepository.Received(1).Update(plan);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanInexistente_DevuelveFailure()
    {
        _planRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Plan?)null);

        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), new ActualizarPlanRequest("Básico", 5000m, Periodicidad.Mensual, 3, 200));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Plan no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConCambioDePrecioYSuscripcionesConCobroAutomatico_ActualizaElMontoEnMercadoPago()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        _planRepository.GetByIdAsync(plan.Id).Returns(plan);

        var suscripcion1 = Suscripcion.IniciarPrueba(Guid.NewGuid(), plan);
        suscripcion1.AsignarPreapproval("preapproval-1");
        var suscripcion2 = Suscripcion.IniciarPrueba(Guid.NewGuid(), plan);
        suscripcion2.AsignarPreapproval("preapproval-2");
        _suscripcionRepository.GetConPreapprovalPorPlanIdAsync(plan.Id)
            .Returns(new[] { suscripcion1, suscripcion2 });
        _plataformaPagoConfiguracion.AccessToken.Returns("PLATAFORMA-TOKEN");

        var result = await _useCase.ExecuteAsync(
            plan.Id, new ActualizarPlanRequest("Básico", 7000m, Periodicidad.Mensual, 3, 200));

        result.IsSuccess.Should().BeTrue();
        await _pagoRecurrenteGateway.Received(1).ActualizarMontoPreapprovalAsync(
            "PLATAFORMA-TOKEN", "preapproval-1", 7000m, Arg.Any<CancellationToken>());
        await _pagoRecurrenteGateway.Received(1).ActualizarMontoPreapprovalAsync(
            "PLATAFORMA-TOKEN", "preapproval-2", 7000m, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SinCambioDePrecio_NoConsultaSuscripciones()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        _planRepository.GetByIdAsync(plan.Id).Returns(plan);

        var result = await _useCase.ExecuteAsync(
            plan.Id, new ActualizarPlanRequest("Básico Renombrado", 5000m, Periodicidad.Mensual, 3, 200));

        result.IsSuccess.Should().BeTrue();
        await _suscripcionRepository.DidNotReceive().GetConPreapprovalPorPlanIdAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
