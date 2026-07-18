using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Public;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Public;

public class ProcesarNotificacionPagoMercadoPagoUseCaseTests
{
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository = Substitute.For<IConfiguracionPagoRepository>();
    private readonly IPagoGateway _pagoGateway = Substitute.For<IPagoGateway>();
    private readonly IEmailNotificador _emailNotificador = Substitute.For<IEmailNotificador>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ProcesarNotificacionPagoMercadoPagoUseCase _useCase;

    public ProcesarNotificacionPagoMercadoPagoUseCaseTests()
    {
        var confirmarPagoUseCase = new ConfirmarPagoUseCase(
            _negocioRepository, _recursoRepository, _reservaRepository, _clienteRepository,
            _emailNotificador, _unitOfWork);
        var rechazarPagoUseCase = new RechazarPagoUseCase(
            _negocioRepository, _recursoRepository, _reservaRepository, _clienteRepository,
            _emailNotificador, _unitOfWork);

        _useCase = new ProcesarNotificacionPagoMercadoPagoUseCase(
            _negocioRepository, _reservaRepository, _configuracionPagoRepository, _pagoGateway,
            confirmarPagoUseCase, rechazarPagoUseCase);
    }

    private (Negocio negocio, Reserva reserva) EscenarioConMercadoPagoConectado()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var recurso = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var cliente = Cliente.Crear("Juan Perez", "juan@test.com");
        var reserva = Reserva.Crear(
            recurso.Id, cliente.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);
        reserva.AsignarPago(Pago.Registrar(reserva.Id, 5000m));

        var configuracionPago = ConfiguracionPago.Conectar(negocio.Id, ProveedorPago.MercadoPago, "alias.mp", "ACCESS-TOKEN");

        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocio.Id).Returns(configuracionPago);

        return (negocio, reserva);
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoAprobado_ConfirmaLaReserva()
    {
        var (negocio, reserva) = EscenarioConMercadoPagoConectado();
        _pagoGateway.ObtenerEstadoPagoAsync("ACCESS-TOKEN", "pago-externo-1")
            .Returns(Result.Success(new EstadoPagoExternoResult("pago-externo-1", EstadoPagoExterno.Aprobado, reserva.Id.ToString())));

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
        reserva.Pago!.Estado.Should().Be(EstadoPago.Aprobado);
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoRechazado_CancelaLaReserva()
    {
        var (negocio, reserva) = EscenarioConMercadoPagoConectado();
        _pagoGateway.ObtenerEstadoPagoAsync("ACCESS-TOKEN", "pago-externo-1")
            .Returns(Result.Success(new EstadoPagoExternoResult("pago-externo-1", EstadoPagoExterno.Rechazado, reserva.Id.ToString())));

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        reserva.Pago!.Estado.Should().Be(EstadoPago.Rechazado);
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoPendiente_NoTocaLaReserva()
    {
        var (negocio, reserva) = EscenarioConMercadoPagoConectado();
        _pagoGateway.ObtenerEstadoPagoAsync("ACCESS-TOKEN", "pago-externo-1")
            .Returns(Result.Success(new EstadoPagoExternoResult("pago-externo-1", EstadoPagoExterno.Pendiente, reserva.Id.ToString())));

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
        reserva.Pago!.Estado.Should().Be(EstadoPago.Pendiente);
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaYaConfirmadaPorOtraNotificacion_EsIdempotenteYNoFalla()
    {
        var (negocio, reserva) = EscenarioConMercadoPagoConectado();
        reserva.Pago!.Aprobar();
        reserva.Confirmar();

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        await _pagoGateway.DidNotReceive().ObtenerEstadoPagoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConExternalReferenceQueNoCoincide_IgnoraLaNotificacion()
    {
        var (negocio, reserva) = EscenarioConMercadoPagoConectado();
        _pagoGateway.ObtenerEstadoPagoAsync("ACCESS-TOKEN", "pago-externo-1")
            .Returns(Result.Success(new EstadoPagoExternoResult("pago-externo-1", EstadoPagoExterno.Aprobado, Guid.NewGuid().ToString())));

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id, "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task ExecuteAsync_ConElGatewayFallando_DevuelveFailureParaQueMercadoPagoReintente()
    {
        var (negocio, reserva) = EscenarioConMercadoPagoConectado();
        _pagoGateway.ObtenerEstadoPagoAsync("ACCESS-TOKEN", "pago-externo-1")
            .Returns(Result.Failure<EstadoPagoExternoResult>("Mercado Pago no respondió."));

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id, "pago-externo-1");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SinPagoExternoId_DevuelveSuccessSinConsultarNada()
    {
        var result = await _useCase.ExecuteAsync("cancha-norte", Guid.NewGuid(), null);

        result.IsSuccess.Should().BeTrue();
        await _negocioRepository.DidNotReceive().GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConNegocioSinMercadoPagoConectado_DevuelveSuccessSinConsultarElGateway()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocio.Id).Returns((ConfiguracionPago?)null);

        var result = await _useCase.ExecuteAsync(negocio.Slug, Guid.NewGuid(), "pago-externo-1");

        result.IsSuccess.Should().BeTrue();
        await _pagoGateway.DidNotReceive().ObtenerEstadoPagoAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
