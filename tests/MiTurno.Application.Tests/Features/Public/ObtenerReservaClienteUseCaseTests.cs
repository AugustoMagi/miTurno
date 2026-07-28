using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Public;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Public;

public class ObtenerReservaClienteUseCaseTests
{
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();

    private readonly ObtenerReservaClienteUseCase _useCase;

    public ObtenerReservaClienteUseCaseTests()
    {
        _useCase = new ObtenerReservaClienteUseCase(_negocioRepository, _recursoRepository, _reservaRepository);
    }

    private (Negocio negocio, Recurso recurso, Reserva reserva) EscenarioValido()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var recurso = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var reserva = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        return (negocio, recurso, reserva);
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaPendiente_DevuelveSuElEstadoActual()
    {
        var (negocio, _, reserva) = EscenarioValido();

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(reserva.Id);
        result.Value.Estado.Should().Be(EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaConfirmada_DevuelveElEstadoConfirmada()
    {
        var (negocio, _, reserva) = EscenarioValido();
        reserva.Confirmar();

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Estado.Should().Be(EstadoReserva.Confirmada);
    }

    [Fact]
    public async Task ExecuteAsync_ConNegocioInexistente_DevuelveFailure()
    {
        _negocioRepository.GetBySlugAsync("no-existe").Returns((Negocio?)null);

        var result = await _useCase.ExecuteAsync("no-existe", Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Reserva no encontrada.");
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaInexistente_DevuelveFailure()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _reservaRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Reserva?)null);

        var result = await _useCase.ExecuteAsync(negocio.Slug, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Reserva no encontrada.");
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailureComoSiNoExistiera()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var otroNegocio = Negocio.Crear("Otro", "otro-negocio", "otro@test.com");
        var recurso = Recurso.Crear(otroNegocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var reserva = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Reserva no encontrada.");
    }
}
