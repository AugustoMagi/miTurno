using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class ObtenerMiSuscripcionUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly IPlataformaPagoConfiguracion _plataformaPagoConfiguracion = Substitute.For<IPlataformaPagoConfiguracion>();
    private readonly IPagoRecurrenteGateway _pagoRecurrenteGateway = Substitute.For<IPagoRecurrenteGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ObtenerMiSuscripcionUseCase _useCase;

    public ObtenerMiSuscripcionUseCaseTests()
    {
        var procesarNotificacionRecurrenteUseCase = new ProcesarNotificacionRecurrenteUseCase(
            _suscripcionRepository, _plataformaPagoConfiguracion, _pagoRecurrenteGateway, _unitOfWork);
        _useCase = new ObtenerMiSuscripcionUseCase(
            _suscripcionRepository, _planRepository, _plataformaPagoConfiguracion, _pagoRecurrenteGateway,
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
    public async Task ExecuteAsync_ConPreapprovalPendienteEnMercadoPago_ReportaCobroAutomaticoInactivo()
    {
        // "pending" es el estado de una Preapproval recién creada mientras el negocio todavía no
        // terminó (o abandonó) el checkout de Mercado Pago: no debe reportarse como si ya estuviera
        // cobrando, aunque el id ya esté guardado localmente desde que se creó.
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        suscripcion.AsignarPreapproval("preapproval-1");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-1")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-1", "pending", null)));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        result.Value.CobroAutomaticoActivo.Should().BeFalse();
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-1");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCambioDePlanPendienteTodaviaPending_MantieneElPlanYCobroVigentes()
    {
        // El caso que rompía la app: cambiar de Pro a Estándar entraba a MP, y si el negocio no
        // pagaba, la card ya mostraba Estándar igual. Mientras la Preapproval nueva siga "pending",
        // el plan y el cobro automático reportados tienen que seguir siendo los de Pro (el vigente).
        var negocioId = Guid.NewGuid();
        var planPro = Plan.Crear("Pro", 8000m, Periodicidad.Mensual, 5, 500);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planPro);
        suscripcion.AsignarPreapproval("preapproval-pro-vigente");
        var planEstandar = Plan.Crear("Estándar", 5000m, Periodicidad.Mensual, 3, 200);
        suscripcion.IniciarCambioDePlanConPago(planEstandar.Id, "preapproval-estandar-pendiente");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-estandar-pendiente")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-estandar-pendiente", "pending", null)));
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-pro-vigente")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-pro-vigente", "authorized", null)));
        _pagoRecurrenteGateway.BuscarUltimoCargoIdAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result.Success<string?>(null));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanId.Should().Be(planPro.Id);
        result.Value.PlanNombre.Should().Be("Pro");
        result.Value.CobroAutomaticoActivo.Should().BeTrue();
        suscripcion.PlanId.Should().Be(planPro.Id);
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-pro-vigente");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _pagoRecurrenteGateway.DidNotReceive().CancelarPreapprovalAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCambioDePlanPendienteAutorizado_ConfirmaElCambioYCancelaLaVieja()
    {
        var negocioId = Guid.NewGuid();
        var planPro = Plan.Crear("Pro", 8000m, Periodicidad.Mensual, 5, 500);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planPro);
        suscripcion.AsignarPreapproval("preapproval-pro-vigente");
        var planEstandar = Plan.Crear("Estándar", 5000m, Periodicidad.Mensual, 3, 200);
        suscripcion.IniciarCambioDePlanConPago(planEstandar.Id, "preapproval-estandar-pendiente");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _planRepository.GetByIdAsync(planEstandar.Id).Returns(planEstandar);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-estandar-pendiente")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-estandar-pendiente", "authorized", null)));
        _pagoRecurrenteGateway.CancelarPreapprovalAsync(Arg.Any<string>(), "preapproval-pro-vigente")
            .Returns(Result.Success());
        _pagoRecurrenteGateway.BuscarUltimoCargoIdAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result.Success<string?>(null));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanId.Should().Be(planEstandar.Id);
        result.Value.PlanNombre.Should().Be("Estándar");
        suscripcion.PlanId.Should().Be(planEstandar.Id);
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-estandar-pendiente");
        suscripcion.PlanPendienteId.Should().BeNull();
        suscripcion.MercadoPagoPreapprovalIdPendiente.Should().BeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _pagoRecurrenteGateway.Received(1).CancelarPreapprovalAsync(
            Arg.Any<string>(), "preapproval-pro-vigente", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCambioDePlanPendienteCancelado_LoDescartaSinTocarElVigente()
    {
        var negocioId = Guid.NewGuid();
        var planPro = Plan.Crear("Pro", 8000m, Periodicidad.Mensual, 5, 500);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, planPro);
        suscripcion.AsignarPreapproval("preapproval-pro-vigente");
        var planEstandar = Plan.Crear("Estándar", 5000m, Periodicidad.Mensual, 3, 200);
        suscripcion.IniciarCambioDePlanConPago(planEstandar.Id, "preapproval-estandar-pendiente");
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-estandar-pendiente")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-estandar-pendiente", "cancelled", null)));
        _pagoRecurrenteGateway.ObtenerPreapprovalAsync(Arg.Any<string>(), "preapproval-pro-vigente")
            .Returns(Result.Success(new PreapprovalEstadoResult("preapproval-pro-vigente", "authorized", null)));
        _pagoRecurrenteGateway.BuscarUltimoCargoIdAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result.Success<string?>(null));

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanId.Should().Be(planPro.Id);
        suscripcion.PlanId.Should().Be(planPro.Id);
        suscripcion.MercadoPagoPreapprovalId.Should().Be("preapproval-pro-vigente");
        suscripcion.PlanPendienteId.Should().BeNull();
        suscripcion.MercadoPagoPreapprovalIdPendiente.Should().BeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _pagoRecurrenteGateway.DidNotReceive().CancelarPreapprovalAsync(
            Arg.Any<string>(), "preapproval-pro-vigente", Arg.Any<CancellationToken>());
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
