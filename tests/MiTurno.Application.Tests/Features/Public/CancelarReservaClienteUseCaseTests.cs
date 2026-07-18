using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Public;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Public;

public class CancelarReservaClienteUseCaseTests
{
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IEmailNotificador _emailNotificador = Substitute.For<IEmailNotificador>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CancelarReservaClienteUseCase _useCase;

    public CancelarReservaClienteUseCaseTests()
    {
        _useCase = new CancelarReservaClienteUseCase(
            _negocioRepository, _recursoRepository, _reservaRepository, _clienteRepository,
            _emailNotificador, _unitOfWork);
    }

    private (Negocio negocio, Recurso recurso, Reserva reserva, Cliente cliente) EscenarioValido()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var recurso = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var cliente = Cliente.Crear("Juan Perez", "juan@test.com");
        var reserva = Reserva.Crear(
            recurso.Id, cliente.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _negocioRepository.GetBySlugAsync(negocio.Slug).Returns(negocio);
        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);

        return (negocio, recurso, reserva, cliente);
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaPendiente_LaCancelaYNotificaAlDuenio()
    {
        var (negocio, _, reserva, cliente) = EscenarioValido();

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id);

        result.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(EstadoReserva.Cancelada);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailNotificador.Received(1).NotificarReservaCanceladaPorClienteAsync(
            Arg.Is<NotificacionNuevaReserva>(n =>
                n!.NegocioEmail == negocio.Email && n.ClienteNombre == cliente.Nombre),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConNegocioInexistente_DevuelveFailure()
    {
        _negocioRepository.GetBySlugAsync("no-existe").Returns((Negocio?)null);

        var result = await _useCase.ExecuteAsync("no-existe", Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Negocio no encontrado.");
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

    [Fact]
    public async Task ExecuteAsync_ConReservaYaCancelada_DevuelveFailureDelDominioSinNotificar()
    {
        var (negocio, _, reserva, _) = EscenarioValido();
        reserva.Cancelar();

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id);

        result.IsFailure.Should().BeTrue();
        await _emailNotificador.DidNotReceive().NotificarReservaCanceladaPorClienteAsync(
            Arg.Any<NotificacionNuevaReserva>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaCompletada_DevuelveFailureDelDominioSinNotificar()
    {
        var (negocio, _, reserva, _) = EscenarioValido();
        reserva.Confirmar();
        reserva.Completar();

        var result = await _useCase.ExecuteAsync(negocio.Slug, reserva.Id);

        result.IsFailure.Should().BeTrue();
        await _emailNotificador.DidNotReceive().NotificarReservaCanceladaPorClienteAsync(
            Arg.Any<NotificacionNuevaReserva>(), Arg.Any<CancellationToken>());
    }
}
