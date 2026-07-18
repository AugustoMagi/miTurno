using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Estadisticas;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Estadisticas;

public class ObtenerEstadisticasOcupacionUseCaseTests
{
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();

    private readonly ObtenerEstadisticasOcupacionUseCase _useCase;

    public ObtenerEstadisticasOcupacionUseCaseTests()
    {
        _useCase = new ObtenerEstadisticasOcupacionUseCase(_reservaRepository, _recursoRepository);
    }

    [Fact]
    public async Task ExecuteAsync_SinReservas_DevuelveTotalesEnCero()
    {
        var negocioId = Guid.NewGuid();
        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([]);

        var result = await _useCase.ExecuteAsync(negocioId, desde: null, hasta: null);

        result.TotalReservas.Should().Be(0);
        result.IngresosTotales.Should().Be(0m);
        result.ReservasPorEstado.Should().BeEmpty();
        result.OcupacionPorRecurso.Should().BeEmpty();
        await _recursoRepository.DidNotReceive().GetByNegocioIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ExecuteAsync_SoloSumaIngresosDeReservasConfirmadasOCompletadas()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);

        var pendiente = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), new DateOnly(2026, 7, 20), TimeSpan.FromHours(9), TimeSpan.FromHours(10), 1000m);
        var confirmada = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), new DateOnly(2026, 7, 20), TimeSpan.FromHours(10), TimeSpan.FromHours(11), 2000m);
        confirmada.Confirmar();
        var completada = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), new DateOnly(2026, 7, 20), TimeSpan.FromHours(11), TimeSpan.FromHours(12), 3000m);
        completada.Confirmar();
        completada.Completar();
        var cancelada = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), new DateOnly(2026, 7, 20), TimeSpan.FromHours(12), TimeSpan.FromHours(13), 4000m);
        cancelada.Cancelar();

        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([pendiente, confirmada, completada, cancelada]);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recurso]);

        var result = await _useCase.ExecuteAsync(negocioId, desde: null, hasta: null);

        result.TotalReservas.Should().Be(4);
        result.IngresosTotales.Should().Be(5000m);
        result.ReservasPorEstado.Should().ContainSingle(e => e.Estado == EstadoReserva.Pendiente && e.Cantidad == 1);
        result.ReservasPorEstado.Should().ContainSingle(e => e.Estado == EstadoReserva.Confirmada && e.Cantidad == 1);
        result.ReservasPorEstado.Should().ContainSingle(e => e.Estado == EstadoReserva.Completada && e.Cantidad == 1);
        result.ReservasPorEstado.Should().ContainSingle(e => e.Estado == EstadoReserva.Cancelada && e.Cantidad == 1);
    }

    [Fact]
    public async Task ExecuteAsync_ConRangoDeFechas_FiltraReservasFueraDelRango()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);

        var dentroDelRango = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), new DateOnly(2026, 7, 15), TimeSpan.FromHours(9), TimeSpan.FromHours(10), 1000m);
        var antesDelRango = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), new DateOnly(2026, 6, 30), TimeSpan.FromHours(9), TimeSpan.FromHours(10), 1000m);
        var despuesDelRango = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), new DateOnly(2026, 8, 1), TimeSpan.FromHours(9), TimeSpan.FromHours(10), 1000m);

        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([dentroDelRango, antesDelRango, despuesDelRango]);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recurso]);

        var result = await _useCase.ExecuteAsync(
            negocioId, desde: new DateOnly(2026, 7, 1), hasta: new DateOnly(2026, 7, 31));

        result.TotalReservas.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ConVariosRecursos_AgrupaOcupacionPorRecursoOrdenadoDescendente()
    {
        var negocioId = Guid.NewGuid();
        var canchaConMasReservas = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var canchaConMenosReservas = Recurso.Crear(negocioId, "Cancha 2", "Futbol", TimeSpan.FromHours(1), 5000m);

        var reservas = new[]
        {
            Reserva.Crear(canchaConMasReservas.Id, Guid.NewGuid(), new DateOnly(2026, 7, 20), TimeSpan.FromHours(9), TimeSpan.FromHours(10), 1000m),
            Reserva.Crear(canchaConMasReservas.Id, Guid.NewGuid(), new DateOnly(2026, 7, 21), TimeSpan.FromHours(9), TimeSpan.FromHours(10), 1000m),
            Reserva.Crear(canchaConMenosReservas.Id, Guid.NewGuid(), new DateOnly(2026, 7, 20), TimeSpan.FromHours(9), TimeSpan.FromHours(10), 1000m)
        };

        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns(reservas);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([canchaConMasReservas, canchaConMenosReservas]);

        var result = await _useCase.ExecuteAsync(negocioId, desde: null, hasta: null);

        result.OcupacionPorRecurso.Should().HaveCount(2);
        result.OcupacionPorRecurso[0].RecursoId.Should().Be(canchaConMasReservas.Id);
        result.OcupacionPorRecurso[0].RecursoNombre.Should().Be("Cancha 1");
        result.OcupacionPorRecurso[0].TotalReservas.Should().Be(2);
        result.OcupacionPorRecurso[1].RecursoId.Should().Be(canchaConMenosReservas.Id);
        result.OcupacionPorRecurso[1].TotalReservas.Should().Be(1);
    }
}
