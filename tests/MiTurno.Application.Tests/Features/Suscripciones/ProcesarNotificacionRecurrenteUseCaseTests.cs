using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class ProcesarNotificacionRecurrenteUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ProcesarNotificacionRecurrenteUseCase _useCase;

    public ProcesarNotificacionRecurrenteUseCaseTests()
    {
        _plataformaPagoConfiguracion.AccessToken.Returns("PLATAFORMA-TOKEN");
        _useCase = new ProcesarNotificacionRecurrenteUseCase(
            _suscripcionRepository, _plataformaPagoConfiguracion, _pagoRecurrenteGateway, _unitOfWork);
    }

    private Suscripcion EscenarioConPreapprovalActiva()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(Guid.NewGuid(), plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);
        return suscripcion;
    }

    [Fact]
    public async Task ExecuteAsync_ConCargoAprobado_RegistraElPagoYRenuevaElVencimiento()
    {
        var suscripcion = EscenarioConPreapprovalActiva();
        var vencimientoOriginal = suscripcion.FechaProximoVencimiento;
        _pagoRecurrenteGateway.ObtenerCargoRecurrenteAsync("PLATAFORMA-TOKEN", "cargo-1")
            .Returns(Result.Success(new CargoRecurrenteResult("cargo-1", "preapproval-1", 5000m, EstadoPagoExterno.Aprobado)));

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "subscription_authorized_payment", "cargo-1");

        result.IsSuccess.Should().BeTrue();
        suscripcion.Pagos.Should().ContainSingle(p => p.TransaccionExternalId == "cargo-1" && p.Estado == EstadoPago.Aprobado);
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Activa);
        suscripcion.FechaProximoVencimiento.Should().BeAfter(vencimientoOriginal);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCargoYaRegistrado_EsIdempotenteYNoLoDuplica()
    {
        var suscripcion = EscenarioConPreapprovalActiva();
        var pagoExistente = PagoSuscripcion.Registrar(suscripcion.Id, 5000m, "cargo-1");
        suscripcion.RegistrarPago(pagoExistente);

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "subscription_authorized_payment", "cargo-1");

        result.IsSuccess.Should().BeTrue();
        suscripcion.Pagos.Should().HaveCount(1);
        await _pagoRecurrenteGateway.DidNotReceive().ObtenerCargoRecurrenteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPreapprovalQueNoCoincide_IgnoraLaNotificacion()
    {
        var suscripcion = EscenarioConPreapprovalActiva();
        _pagoRecurrenteGateway.ObtenerCargoRecurrenteAsync("PLATAFORMA-TOKEN", "cargo-1")
            .Returns(Result.Success(new CargoRecurrenteResult("cargo-1", "otra-preapproval", 5000m, EstadoPagoExterno.Aprobado)));

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "subscription_authorized_payment", "cargo-1");

        result.IsSuccess.Should().BeTrue();
        suscripcion.Pagos.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ConCargoRechazado_NoRenuevaLaSuscripcion()
    {
        var suscripcion = EscenarioConPreapprovalActiva();
        var vencimientoOriginal = suscripcion.FechaProximoVencimiento;
        _pagoRecurrenteGateway.ObtenerCargoRecurrenteAsync("PLATAFORMA-TOKEN", "cargo-1")
            .Returns(Result.Success(new CargoRecurrenteResult("cargo-1", "preapproval-1", 5000m, EstadoPagoExterno.Rechazado)));

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "subscription_authorized_payment", "cargo-1");

        result.IsSuccess.Should().BeTrue();
        suscripcion.Pagos.Should().BeEmpty();
        suscripcion.FechaProximoVencimiento.Should().Be(vencimientoOriginal);
    }

    [Fact]
    public async Task ExecuteAsync_TipoPreapprovalConEstadoCancelado_CancelaLaSuscripcionLocal()
    {
        var suscripcion = EscenarioConPreapprovalActiva();
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync("PLATAFORMA-TOKEN", "preapproval-1")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-1", "cancelled", suscripcion.Id.ToString())));

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "subscription_preapproval", "preapproval-1");

        result.IsSuccess.Should().BeTrue();
        suscripcion.Estado.Should().Be(EstadoSuscripcion.Cancelada);
        suscripcion.MercadoPagoPreapprovalId.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripcionConPreapproval_DevuelveSuccessSinConsultarNada()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(Guid.NewGuid(), plan);
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);

        var result = await _useCase.ExecuteAsync(suscripcion.Id, "subscription_authorized_payment", "cargo-1");

        result.IsSuccess.Should().BeTrue();
        await _pagoRecurrenteGateway.DidNotReceive().ObtenerCargoRecurrenteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SinExternoId_DevuelveSuccessSinConsultarNada()
    {
        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), "subscription_authorized_payment", null);

        result.IsSuccess.Should().BeTrue();
        await _suscripcionRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
