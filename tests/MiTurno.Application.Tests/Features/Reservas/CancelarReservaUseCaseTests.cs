using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Reservas;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Reservas;

public class CancelarReservaUseCaseTests
{
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IEmailNotificador _emailNotificador = Substitute.For<IEmailNotificador>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CancelarReservaUseCase _useCase;

    public CancelarReservaUseCaseTests()
    {
        _useCase = new CancelarReservaUseCase(
            _reservaRepository, _recursoRepository, _negocioRepository, _clienteRepository,
            _emailNotificador, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaValidaDelNegocio_LaCancelaYNotificaAlCliente()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "dueno@test.com");
        var recurso = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var cliente = Cliente.Crear("Juan Perez", "juan@test.com");
        var reserva = Reserva.Crear(
            recurso.Id, cliente.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);

        var result = await _useCase.ExecuteAsync(negocio.Id, reserva.Id);

        result.IsSuccess.Should().BeTrue();
        reserva.Estado.Should().Be(Domain.Enums.EstadoReserva.Cancelada);
        _reservaRepository.Received(1).Update(reserva);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailNotificador.Received(1).NotificarReservaCanceladaAsync(
            Arg.Is<NotificacionReserva>(n => n!.ClienteEmail == cliente.Email),
            Arg.Any<CancellationToken>());
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
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailureComoSiNoExistiera()
    {
        var negocioDueno = Guid.NewGuid();
        var otroNegocio = Negocio.Crear("Otro negocio", "otro-negocio", "otro@test.com");
        var recurso = Recurso.Crear(otroNegocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var reserva = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioDueno, reserva.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Reserva no encontrada.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaYaCancelada_DevuelveFailureDelDominioSinNotificar()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "dueno@test.com");
        var recurso = Recurso.Crear(negocio.Id, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var reserva = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);
        reserva.Cancelar();

        _reservaRepository.GetByIdAsync(reserva.Id).Returns(reserva);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocio.Id, reserva.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("La reserva ya está cancelada.");
        await _emailNotificador.DidNotReceive().NotificarReservaCanceladaAsync(
            Arg.Any<NotificacionReserva>(), Arg.Any<CancellationToken>());
    }
}
