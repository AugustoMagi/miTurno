using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Public;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Public;

public class ListarTurnosDisponiblesUseCaseTests
{
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly ListarTurnosDisponiblesUseCase _useCase;

    private static readonly DateOnly FechaFutura = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

    public ListarTurnosDisponiblesUseCaseTests()
    {
        _clock.Now.Returns(DateTime.UtcNow);
        var resolverNegocioPublicoService = new ResolverNegocioPublicoService(_negocioRepository, _suscripcionRepository);
        _useCase = new ListarTurnosDisponiblesUseCase(resolverNegocioPublicoService, _recursoRepository, _reservaRepository, _clock);
    }

    private (Negocio negocio, Recurso recurso) EscenarioValido()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var recurso = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        recurso.AgregarHorarioDisponible(HorarioDisponible.Crear(
            recurso.Id, FechaFutura.DayOfWeek, TimeSpan.FromHours(18), TimeSpan.FromHours(21)));

        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, FechaFutura).Returns([]);

        return (negocio, recurso);
    }

    [Fact]
    public async Task ExecuteAsync_ConVentanaDeTresHorasYTurnosDeUnaHora_DevuelveTresTurnosLibres()
    {
        var (negocio, recurso) = EscenarioValido();

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, FechaFutura);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].HoraInicio.Should().Be(TimeSpan.FromHours(18));
        result.Value[2].HoraInicio.Should().Be(TimeSpan.FromHours(20));
    }

    [Fact]
    public async Task ExecuteAsync_ConUnTurnoYaReservado_LoExcluyeDelResultado()
    {
        var (negocio, recurso) = EscenarioValido();
        var reservaExistente = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), FechaFutura, TimeSpan.FromHours(19), TimeSpan.FromHours(20), 5000m);
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, FechaFutura).Returns([reservaExistente]);

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, FechaFutura);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().NotContain(t => t.HoraInicio == TimeSpan.FromHours(19));
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaCancelada_LaIgnoraYDejaElTurnoLibre()
    {
        var (negocio, recurso) = EscenarioValido();
        var reservaCancelada = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), FechaFutura, TimeSpan.FromHours(19), TimeSpan.FromHours(20), 5000m);
        reservaCancelada.Cancelar();
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, FechaFutura).Returns([reservaCancelada]);

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, FechaFutura);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExecuteAsync_ConFechaBloqueada_DevuelveListaVacia()
    {
        var (negocio, recurso) = EscenarioValido();
        recurso.AgregarBloqueoFecha(BloqueoFecha.Crear(recurso.Id, FechaFutura, "Feriado"));

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, FechaFutura);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ConFechaDeHoyYTurnosYaPasados_LosExcluyeYDejaSoloLosFuturos()
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

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, hoy);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].HoraInicio.Should().Be(TimeSpan.FromHours(20));
    }

    [Fact]
    public async Task ExecuteAsync_ConFechaPasada_DevuelveFailure()
    {
        var result = await _useCase.ExecuteAsync(
            "cancha-norte", Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("No se pueden consultar turnos de fechas pasadas.");
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailure()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var otroNegocio = Negocio.Crear("Otro", "otro-negocio", "otro@test.com");
        var recurso = Recurso.Crear(otroNegocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);

        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocio.Slug, recurso.Id, FechaFutura);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
    }
}
