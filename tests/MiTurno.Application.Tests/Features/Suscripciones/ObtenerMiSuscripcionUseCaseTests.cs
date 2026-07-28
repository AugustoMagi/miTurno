using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class ObtenerMiSuscripcionUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ObtenerMiSuscripcionUseCase _useCase;

    public ObtenerMiSuscripcionUseCaseTests()
    {
        var procesarNotificacionRecurrenteUseCase = new ProcesarNotificacionRecurrenteUseCase(
            _suscripcionRepository, _plataformaPagoConfiguracion, _pagoRecurrenteGateway, _unitOfWork);
        _useCase = new ObtenerMiSuscripcionUseCase(
            _suscripcionRepository, _plataformaPagoConfiguracion, _pagoRecurrenteGateway,
            procesarNotificacionRecurrenteUseCase, _unitOfWork);
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
        result.Value.PlanId.Should().Be(plan.Id);
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

    [Fact]
    public async Task ExecuteAsync_ConPreapprovalVivoEnMercadoPago_MantieneCobroAutomaticoActivo()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-1", "authorized", null)));
        _pagoRecurrenteGateway.BuscarUltimoCargoIdAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success<string?>(null));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CobroAutomaticoActivo.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoYaAutorizadoEnMercadoPagoPeroNoReflejadoLocalmente_ReconciliaYActivaLaSuscripcion()
    {
        // Reproduce el incidente real: Mercado Pago ya autorizó y cobró (el webhook nunca llegó o
        // falló), y la suscripción se había quedado en Cancelada con el preapproval igual asignado.
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        suscripcion.Cancelar();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _suscripcionRepository.GetByIdAsync(suscripcion.Id).Returns(suscripcion);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-1", "authorized", null)));
        _pagoRecurrenteGateway.BuscarUltimoCargoIdAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success<string?>("cargo-1"));
        _pagoRecurrenteGateway.ObtenerCargoRecurrenteAsync(Arg.Any<string>(), "cargo-1")
            .Returns(Result.Success(new CargoRecurrenteResult("cargo-1", "preapproval-1", 5000m, EstadoPagoExterno.Aprobado)));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(EstadoSuscripcion.Activa);
        suscripcion.Pagos.Should().ContainSingle(p => p.TransaccionExternalId == "cargo-1");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPreapprovalAutorizadoPeroSinNingunCargoTodavia_NoReconciliaNiRompe()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        suscripcion.Cancelar();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-1", "authorized", null)));
        _pagoRecurrenteGateway.BuscarUltimoCargoIdAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success<string?>(null));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(EstadoSuscripcion.Cancelada);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPreapprovalCanceladoEnMercadoPago_LoSueltaYNoQuedaComoActivo()
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
        result.Value.CobroAutomaticoActivo.Should().BeFalse();
        suscripcion.MercadoPagoPreapprovalId.Should().BeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
