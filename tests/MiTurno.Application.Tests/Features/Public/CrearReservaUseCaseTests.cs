using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Public;
using MiTurno.Application.Features.Public.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Public;

public class CrearReservaUseCaseTests
{
    private const string WebhookBaseUrl = "https://miturno.test";

    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository = Substitute.For<IConfiguracionPagoRepository>();
    private readonly IPagoGateway _pagoGateway = Substitute.For<IPagoGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly CrearReservaUseCase _useCase;

    private static readonly DateOnly FechaFutura = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

    public CrearReservaUseCaseTests()
    {
        _clock.Now.Returns(DateTime.UtcNow);
        var resolverNegocioPublicoService = new ResolverNegocioPublicoService(_negocioRepository, _suscripcionRepository);
        _useCase = new CrearReservaUseCase(
            new CrearReservaValidator(), resolverNegocioPublicoService, _recursoRepository, _reservaRepository,
            _clienteRepository, _configuracionPagoRepository, _pagoGateway, _unitOfWork, _clock);
    }

    private (Negocio negocio, Recurso recurso) EscenarioValido()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var recurso = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        recurso.AgregarHorarioDisponible(HorarioDisponible.Crear(
            recurso.Id, FechaFutura.DayOfWeek, TimeSpan.FromHours(8), TimeSpan.FromHours(22)));

        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, FechaFutura).Returns([]);

        return (negocio, recurso);
    }

    private static CrearReservaRequest RequestValido() => new(
        FechaFutura, TimeSpan.FromHours(18), "Juan Perez", "juan@test.com", "1122334455");

    [Fact]
    public async Task ExecuteAsync_ConClienteNuevo_LoCreaYReservaElTurnoConPagoPendiente()
    {
        var (negocio, recurso) = EscenarioValido();
        _clienteRepository.GetByEmailAsync("juan@test.com").Returns((Cliente?)null);

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.HoraInicio.Should().Be(TimeSpan.FromHours(18));
        result.Value.HoraFin.Should().Be(TimeSpan.FromHours(19));
        result.Value.PrecioTotal.Should().Be(5000m);
        await _clienteRepository.Received(1).AddAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
        await _reservaRepository.Received(1).AddAsync(Arg.Any<Reserva>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConClienteExistente_ActualizaSusDatosDeContactoSinCrearUnoNuevo()
    {
        var (negocio, recurso) = EscenarioValido();
        var clienteExistente = Cliente.Crear("Juan P.", "juan@test.com", "000");
        _clienteRepository.GetByEmailAsync("juan@test.com").Returns(clienteExistente);

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        clienteExistente.Nombre.Should().Be("Juan Perez");
        clienteExistente.Telefono.Should().Be("1122334455");
        _clienteRepository.Received(1).Update(clienteExistente);
        await _clienteRepository.DidNotReceive().AddAsync(Arg.Any<Cliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConFechaPasada_DevuelveFailureSinConsultarNegocio()
    {
        var request = RequestValido() with { Fecha = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) };

        var result = await _useCase.ExecuteAsync("cancha-norte", Guid.NewGuid(), request, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("No se pueden reservar turnos en fechas pasadas.");
        await _negocioRepository.DidNotReceive().GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConHorarioDeHoyYaPasado_DevuelveFailureSinConsultarNegocio()
    {
        var ahora = new DateTime(2026, 3, 10, 19, 30, 0, DateTimeKind.Utc);
        _clock.Now.Returns(ahora);
        var request = RequestValido() with { Fecha = DateOnly.FromDateTime(ahora), HoraInicio = TimeSpan.FromHours(18) };

        var result = await _useCase.ExecuteAsync("cancha-norte", Guid.NewGuid(), request, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("No se pueden reservar turnos que ya pasaron.");
        await _negocioRepository.DidNotReceive().GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConHorarioDeHoyAunNoPasado_PermiteLaReserva()
    {
        var ahora = new DateTime(2026, 3, 10, 19, 30, 0, DateTimeKind.Utc);
        var hoy = DateOnly.FromDateTime(ahora);
        _clock.Now.Returns(ahora);

        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var recurso = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        recurso.AgregarHorarioDisponible(HorarioDisponible.Crear(
            recurso.Id, hoy.DayOfWeek, TimeSpan.FromHours(18), TimeSpan.FromHours(21)));
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, hoy).Returns([]);
        _clienteRepository.GetByEmailAsync("juan@test.com").Returns((Cliente?)null);

        var request = RequestValido() with { Fecha = hoy, HoraInicio = TimeSpan.FromHours(20) };
        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, request, WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConNegocioInactivo_DevuelveFailure()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        negocio.Desactivar();
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);

        var result = await _useCase.ExecuteAsync(negocio.Slug, Guid.NewGuid(), RequestValido(), WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Negocio no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDesactivado_DevuelveFailure()
    {
        var (negocio, recurso) = EscenarioValido();
        recurso.Desactivar();

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConFechaBloqueada_DevuelveFailure()
    {
        var (negocio, recurso) = EscenarioValido();
        recurso.AgregarBloqueoFecha(BloqueoFecha.Crear(recurso.Id, FechaFutura, "Feriado"));

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("El recurso no tiene disponibilidad ese día.");
    }

    [Fact]
    public async Task ExecuteAsync_ConHorarioFueraDeLosDisponibles_DevuelveFailure()
    {
        var (negocio, recurso) = EscenarioValido();
        var request = RequestValido() with { HoraInicio = TimeSpan.FromHours(23) };

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, request, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("El horario seleccionado no está disponible.");
    }

    [Fact]
    public async Task ExecuteAsync_ConTurnoYaReservado_DevuelveFailure()
    {
        var (negocio, recurso) = EscenarioValido();
        var reservaExistente = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), FechaFutura, TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, FechaFutura).Returns([reservaExistente]);

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("El turno seleccionado ya fue reservado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConTurnoSuperpuestoAUnaReservaCancelada_PermiteLaNuevaReserva()
    {
        var (negocio, recurso) = EscenarioValido();
        var reservaCancelada = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), FechaFutura, TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);
        reservaCancelada.Cancelar();
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, FechaFutura).Returns([reservaCancelada]);
        _clienteRepository.GetByEmailAsync("juan@test.com").Returns((Cliente?)null);

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConEmailDeClienteInvalido_DevuelveFailureDeValidacionSinConsultarNegocio()
    {
        var request = RequestValido() with { ClienteEmail = "no-es-un-email" };

        var result = await _useCase.ExecuteAsync("cancha-norte", Guid.NewGuid(), request, WebhookBaseUrl);

        result.IsFailure.Should().BeTrue();
        await _negocioRepository.DidNotReceive().GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SinMercadoPagoConectado_NoLlamaAlGatewayYLinkPagoEsNulo()
    {
        var (negocio, recurso) = EscenarioValido();
        _clienteRepository.GetByEmailAsync("juan@test.com").Returns((Cliente?)null);
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocio.Id).Returns((ConfiguracionPago?)null);

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkPago.Should().BeNull();
        result.Value.AliasPago.Should().BeNull();
        await _pagoGateway.DidNotReceive().CrearPreferenciaAsync(Arg.Any<CrearPreferenciaPagoRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConAliasPeroSinAccessToken_NoLlamaAlGatewayYDevuelveElAliasParaTransferenciaManual()
    {
        var (negocio, recurso) = EscenarioValido();
        _clienteRepository.GetByEmailAsync("juan@test.com").Returns((Cliente?)null);
        var configuracionPago = ConfiguracionPago.Conectar(negocio.Id, ProveedorPago.MercadoPago, "alias.mp");
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocio.Id).Returns(configuracionPago);

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkPago.Should().BeNull();
        result.Value.AliasPago.Should().Be("alias.mp");
        await _pagoGateway.DidNotReceive().CrearPreferenciaAsync(Arg.Any<CrearPreferenciaPagoRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConMercadoPagoConectadoYAccessToken_CreaLaPreferenciaYDevuelveElLinkPago()
    {
        var (negocio, recurso) = EscenarioValido();
        _clienteRepository.GetByEmailAsync("juan@test.com").Returns((Cliente?)null);
        var configuracionPago = ConfiguracionPago.Conectar(negocio.Id, ProveedorPago.MercadoPago, "alias.mp", "TEST-ACCESS-TOKEN");
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocio.Id).Returns(configuracionPago);
        _pagoGateway.CrearPreferenciaAsync(Arg.Any<CrearPreferenciaPagoRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PreferenciaPagoResult("pref-1", "https://mp.test/pagar/pref-1")));

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkPago.Should().Be("https://mp.test/pagar/pref-1");
        result.Value.AliasPago.Should().BeNull();
        await _pagoGateway.Received(1).CrearPreferenciaAsync(
            Arg.Is<CrearPreferenciaPagoRequest>(r =>
                r!.AccessToken == "TEST-ACCESS-TOKEN" &&
                r.NotificationUrl.StartsWith(WebhookBaseUrl) &&
                r.NotificationUrl.Contains($"{negocio.Slug}/reservas/") &&
                r.NotificationUrl.EndsWith("/pago/webhook/mercadopago")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConGatewayQueFalla_LaReservaSeCreaIgualYLinkPagoEsNulo()
    {
        var (negocio, recurso) = EscenarioValido();
        _clienteRepository.GetByEmailAsync("juan@test.com").Returns((Cliente?)null);
        var configuracionPago = ConfiguracionPago.Conectar(negocio.Id, ProveedorPago.MercadoPago, "alias.mp", "TEST-ACCESS-TOKEN");
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocio.Id).Returns(configuracionPago);
        _pagoGateway.CrearPreferenciaAsync(Arg.Any<CrearPreferenciaPagoRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PreferenciaPagoResult>("Mercado Pago no respondió."));

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, RequestValido(), WebhookBaseUrl);

        result.IsSuccess.Should().BeTrue();
        result.Value.LinkPago.Should().BeNull();
        result.Value.AliasPago.Should().Be("alias.mp");
        await _reservaRepository.Received(1).AddAsync(Arg.Any<Reserva>(), Arg.Any<CancellationToken>());
    }
}
