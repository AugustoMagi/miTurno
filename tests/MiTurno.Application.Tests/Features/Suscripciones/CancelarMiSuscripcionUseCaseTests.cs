using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class CancelarMiSuscripcionUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CancelarMiSuscripcionUseCase _useCase;

    public CancelarMiSuscripcionUseCaseTests()
    {
        _useCase = new CancelarMiSuscripcionUseCase(
            _suscripcionRepository, _plataformaPagoConfiguracion, _pagoRecurrenteGateway, _unitOfWork);
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

    [Fact]
    public async Task ExecuteAsync_ConCobroAutomaticoActivo_PausaLaPreapprovalYCancelaLaSuscripcion()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _plataformaPagoConfiguracion.AccessToken.Returns("PLATAFORMA-TOKEN");
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync("PLATAFORMA-TOKEN", "preapproval-1")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-1", "authorized", null)));
        _pagoRecurrenteGateway.PausarPreapprovalAsync("PLATAFORMA-TOKEN", "preapproval-1")
            .Returns(Result.Success());

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Cancelada);
        // No se suelta el id: hay que poder reanudar sin volver a pasar por el checkout de MP.
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-1");
        suscripcion.CobroAutomaticoPausado.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConCobroAutomaticoYFallaAlPausarEnMercadoPago_NoCancelaLocalmente()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-1", "authorized", null)));
        _pagoRecurrenteGateway.PausarPreapprovalAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Failure("Mercado Pago no respondió."));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsFailure.Should().BeTrue();
        suscripcion.Estado.Should().NotBe(EstadoSuscripcion.Cancelada);
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-1");
        suscripcion.CobroAutomaticoPausado.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPreapprovalYaPausadaEnMercadoPago_CancelaLocalmenteSinReintentarPausar()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-1", "paused", null)));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Cancelada);
        suscripcion.CobroAutomaticoPausado.Should().BeTrue();
        await _pagoRecurrenteGateway.DidNotReceive().PausarPreapprovalAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPreapprovalYaCanceladaEnMercadoPago_LaSueltaSinPausar()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-1", "cancelled", null)));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Cancelada);
        suscripcion.MercadoPagoPreapprovalId.Should().BeNull();
        suscripcion.CobroAutomaticoPausado.Should().BeFalse();
        await _pagoRecurrenteGateway.DidNotReceive().PausarPreapprovalAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
