using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class ProcesarNotificacionPagoSuscripcionUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoGateway _pagoGateway = Substitute.For<IPagoGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ProcesarNotificacionPagoSuscripcionUseCase _useCase;

    public ProcesarNotificacionPagoSuscripcionUseCaseTests()
    {
        _plataformaPagoConfiguracion.AccessToken.Returns("PLATAFORMA-TOKEN");
        _useCase = new ProcesarNotificacionPagoSuscripcionUseCase(
            _suscripcionRepository, _plataformaPagoConfiguracion, _pagoGateway, _unitOfWork);
    }

    private (Suscripcion suscripcion, PagoSuscripcion pago) EscenarioConPagoPendiente()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(Guid.NewGuid(), plan);
        var pago = PagoSuscripcion.Registrar(suscripcion.Id, plan.Precio, null);
        suscripcion.RegistrarPago(pago);
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);
        return (suscripcion, pago);
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoAprobado_AprueboElPagoYRenuevaLaSuscripcion()
    {
        var (suscripcion, pago) = EscenarioConPagoPendiente();
        _pagoGateway.ObtenerEstadoPagoAsync("PLATAFORMA-TOKEN", "pago-externo-1")
            .Returns(Result.Success(new EstadoPagoExternoResult("pago-externo-1", EstadoPagoExterno.Aprobado, pago.Id.ToString())));

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        pago.Estado.Should().Be(EstadoPago.Aprobado);
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Activa);
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoRechazado_RechazaElPagoSinTocarLaSuscripcion()
    {
        var (suscripcion, pago) = EscenarioConPagoPendiente();
        var vencimientoOriginal = suscripcion.FechaProximoVencimiento;
        _pagoGateway.ObtenerEstadoPagoAsync("PLATAFORMA-TOKEN", "pago-externo-1")
            .Returns(Result.Success(new EstadoPagoExternoResult("pago-externo-1", EstadoPagoExterno.Rechazado, pago.Id.ToString())));

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        pago.Estado.Should().Be(EstadoPago.Rechazado);
        suscripcion.Estado.Should().Be(EstadoSuscripcion.EnPrueba);
        suscripcion.FechaProximoVencimiento.Should().Be(vencimientoOriginal);
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoPendiente_NoTocaNada()
    {
        var (suscripcion, pago) = EscenarioConPagoPendiente();
        _pagoGateway.ObtenerEstadoPagoAsync("PLATAFORMA-TOKEN", "pago-externo-1")
            .Returns(Result.Success(new EstadoPagoExternoResult("pago-externo-1", EstadoPagoExterno.Pendiente, pago.Id.ToString())));

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        pago.Estado.Should().Be(EstadoPago.Pendiente);
    }

    [Fact]
    public async Task ExecuteAsync_SinPagoPendiente_EsIdempotenteYNoFalla()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(Guid.NewGuid(), plan);
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        await _pagoGateway.DidNotReceive().ObtenerEstadoPagoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConExternalReferenceQueNoCoincide_IgnoraLaNotificacion()
    {
        var (suscripcion, _) = EscenarioConPagoPendiente();
        _pagoGateway.ObtenerEstadoPagoAsync("PLATAFORMA-TOKEN", "pago-externo-1")
            .Returns(Result.Success(new EstadoPagoExternoResult("pago-externo-1", EstadoPagoExterno.Aprobado, Guid.NewGuid().ToString())));

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        suscripcion.Estado.Should().Be(EstadoSuscripcion.EnPrueba);
    }

    [Fact]
    public async Task ExecuteAsync_ConElGatewayFallando_DevuelveFailureParaQueMercadoPagoReintente()
    {
        var (suscripcion, _) = EscenarioConPagoPendiente();
        _pagoGateway.ObtenerEstadoPagoAsync("PLATAFORMA-TOKEN", "pago-externo-1")
            .Returns(Result.Failure<EstadoPagoExternoResult>("Mercado Pago no respondió."));

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "pago-externo-1");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SinPagoExternoId_DevuelveSuccessSinConsultarNada()
    {
        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), null);

        result.IsSuccess.Should().BeTrue();
        await _suscripcionRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
