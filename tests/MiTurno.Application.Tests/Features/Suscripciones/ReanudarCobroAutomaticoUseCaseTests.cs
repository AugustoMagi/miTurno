using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class ReanudarCobroAutomaticoUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ReanudarCobroAutomaticoUseCase _useCase;

    public ReanudarCobroAutomaticoUseCaseTests()
    {
        _plataformaPagoConfiguracion.AccessToken.Returns("PLATAFORMA-TOKEN");
        _useCase = new ReanudarCobroAutomaticoUseCase(
            _suscripcionRepository, _plataformaPagoConfiguracion, _pagoRecurrenteGateway, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConCobroAutomaticoPausado_LoReanudaSinCrearUnaPreapprovalNueva()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        suscripcion.PausarCobroAutomatico();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ReanudarPreapprovalAsync("PLATAFORMA-TOKEN", "preapproval-1")
            .Returns(Result.Success());

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        suscripcion.CobroAutomaticoPausado.Should().BeFalse();
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-1");
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Activa);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _pagoRecurrenteGateway.DidNotReceive().CrearPreapprovalAsync(
            Arg.Any<CrearPreapprovalRequest>(), Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task ExecuteAsync_SinCobroAutomaticoAsignado_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsFailure.Should().BeTrue();
        await _pagoRecurrenteGateway.DidNotReceive().ReanudarPreapprovalAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCobroAutomaticoYaActivo_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsFailure.Should().BeTrue();
        await _pagoRecurrenteGateway.DidNotReceive().ReanudarPreapprovalAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConElGatewayFallando_DevuelveFailureSinGuardarNada()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        suscripcion.PausarCobroAutomatico();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ReanudarPreapprovalAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Failure("Mercado Pago no respondió."));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsFailure.Should().BeTrue();
        suscripcion.CobroAutomaticoPausado.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
