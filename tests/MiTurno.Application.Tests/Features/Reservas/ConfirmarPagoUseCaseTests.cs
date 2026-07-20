using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Reservas;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Reservas;

public class ConfirmarPagoUseCaseTests
{
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IEmailNotificador _emailNotificador = Substitute.For<IEmailNotificador>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ConfirmarPagoUseCase _useCase;

    public ConfirmarPagoUseCaseTests()
    {
        _useCase = new ConfirmarPagoUseCase(
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
        reserva.AsignarPago(Pago.Registrar(reserva.Id, 5000m));

        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);
        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);

        return (negocio, recurso, reserva, cliente);
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoPendiente_LoApruebaConfirmaLaReservaYNotifica()
    {
        var (negocio, _, reserva, cliente) = EscenarioValido();

        var result = await _useCase.ExecuteAsync(negocio.Id, reserva.Id);

        result.IsSuccess.Should().BeTrue();
        reserva.Pago!.Estado.Should().Be(EstadoPago.Aprobado);
        reserva.Estado.Should().Be(EstadoReserva.Confirmada);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailNotificador.Received(1).NotificarReservaConfirmadaAsync(
            Arg.Is<NotificacionReserva>(n => n!.ClienteEmail == cliente.Email), Arg.Any<CancellationToken>());
        await _emailNotificador.Received(1).NotificarNuevaReservaAlDuenioAsync(
            Arg.Is<NotificacionNuevaReserva>(n => n!.NegocioEmail == negocio.Email), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaInexistente_DevuelveFailureSinTocarOtrosRepositorios()
    {
        var reservaId = Guid.NewGuid();
        _reservaRepository.GetByIdAsync(reservaId).Returns((Reserva?)null);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), reservaId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Reserva no encontrada.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaSinPagoAsociado_DevuelveFailure()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        var recurso = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var reserva = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocio.Id, reserva.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("La reserva no tiene un pago asociado.");
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailureComoSiNoExistiera()
    {
        var negocioDueno = Guid.NewGuid();
        var otroNegocio = Negocio.Crear("Otro", "otro-negocio", "otro@test.com");
        var recurso = Recurso.Crear(otroNegocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var reserva = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioDueno, reserva.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Reserva no encontrada.");
    }

    [Fact]
    public async Task ExecuteAsync_ConPagoYaAprobado_DevuelveFailureDelDominioSinNotificar()
    {
        var (negocio, _, reserva, _) = EscenarioValido();
        reserva.Pago!.Aprobar();

        var result = await _useCase.ExecuteAsync(negocio.Id, reserva.Id);

        result.IsFailure.Should().BeTrue();
        await _emailNotificador.DidNotReceive().NotificarReservaConfirmadaAsync(
            Arg.Any<NotificacionReserva>(), Arg.Any<CancellationToken>());
        await _emailNotificador.DidNotReceive().NotificarNuevaReservaAlDuenioAsync(
            Arg.Any<NotificacionNuevaReserva>(), Arg.Any<CancellationToken>());
    }
}
