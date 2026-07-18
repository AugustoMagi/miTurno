using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos.Bloqueos;
using MiTurno.Application.Features.Recursos.Bloqueos.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos.Bloqueos;

public class AgregarBloqueoFechaUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly AgregarBloqueoFechaUseCase _useCase;

    private static readonly DateOnly FechaFutura = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7);

    public AgregarBloqueoFechaUseCaseTests()
    {
        _useCase = new AgregarBloqueoFechaUseCase(
            new AgregarBloqueoFechaValidator(), _recursoRepository, _reservaRepository, _clienteRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_SinReservasEseDia_AgregaElBloqueoSinReservasAfectadas()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, FechaFutura).Returns([]);

        var result = await _useCase.ExecuteAsync(
            negocioId, recurso.Id, new AgregarBloqueoFechaRequest(FechaFutura, "Feriado"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Motivo.Should().Be("Feriado");
        result.Value.ReservasAfectadas.Should().BeEmpty();
        recurso.BloqueosFecha.Should().ContainSingle(b => b.Fecha == FechaFutura);
        _recursoRepository.Received(1).Update(recurso);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConReservaActivaEseDia_LaDevuelveComoAfectadaSinCancelarla()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var cliente = Cliente.Crear("Juan Perez", "juan@test.com");
        var reserva = Reserva.Crear(
            recurso.Id, cliente.Id, FechaFutura, TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, FechaFutura).Returns([reserva]);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);

        var result = await _useCase.ExecuteAsync(
            negocioId, recurso.Id, new AgregarBloqueoFechaRequest(FechaFutura, "Feriado"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ReservasAfectadas.Should().ContainSingle(r => r.Id == reserva.Id && r.ClienteNombre == "Juan Perez");
        reserva.Estado.Should().Be(Domain.Enums.EstadoReserva.Pendiente);
    }

    [Fact]
    public async Task ExecuteAsync_IgnoraReservasCanceladasComoAfectadas()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var reservaCancelada = Reserva.Crear(
            recurso.Id, Guid.NewGuid(), FechaFutura, TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);
        reservaCancelada.Cancelar();

        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);
        _reservaRepository.GetByRecursoYFechaAsync(recurso.Id, FechaFutura).Returns([reservaCancelada]);

        var result = await _useCase.ExecuteAsync(
            negocioId, recurso.Id, new AgregarBloqueoFechaRequest(FechaFutura, "Feriado"));

        result.IsSuccess.Should().BeTrue();
        result.Value.ReservasAfectadas.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ConFechaYaBloqueada_DevuelveFailureDelDominio()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        recurso.AgregarBloqueoFecha(BloqueoFecha.Crear(recurso.Id, FechaFutura));
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(
            negocioId, recurso.Id, new AgregarBloqueoFechaRequest(FechaFutura, "Feriado"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Ya existe un bloqueo para esa fecha.");
    }

    [Fact]
    public async Task ExecuteAsync_ConFechaPasada_DevuelveFailureDeValidacionSinConsultarRecurso()
    {
        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new AgregarBloqueoFechaRequest(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1), null));

        result.IsFailure.Should().BeTrue();
        await _recursoRepository.DidNotReceive().GetConHorariosYBloqueosAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailure()
    {
        var recurso = Recurso.Crear(Guid.NewGuid(), "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        _recursoRepository.GetConHorariosYBloqueosAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), recurso.Id, new AgregarBloqueoFechaRequest(FechaFutura, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
    }
}
