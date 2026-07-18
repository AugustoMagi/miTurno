using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Clientes;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Clientes;

public class ObtenerHistorialClienteUseCaseTests
{
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();

    private readonly ObtenerHistorialClienteUseCase _useCase;

    public ObtenerHistorialClienteUseCaseTests()
    {
        _useCase = new ObtenerHistorialClienteUseCase(_reservaRepository, _recursoRepository, _clienteRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ConClienteSinReservasEnElNegocio_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([]);

        var result = await _useCase.ExecuteAsync(negocioId, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Cliente no encontrado.");
    }

    [Fact]
    public async Task ExecuteAsync_IgnoraReservasDeOtrosClientesDelMismoNegocio()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var cliente = Cliente.Crear("Juan Perez", "juan@test.com");
        var otroCliente = Cliente.Crear("Otro Cliente", "otro@test.com");
        var reservaDelCliente = Reserva.Crear(
            recurso.Id, cliente.Id, new DateOnly(2026, 7, 20), TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);
        var reservaDeOtro = Reserva.Crear(
            recurso.Id, otroCliente.Id, new DateOnly(2026, 7, 21), TimeSpan.FromHours(10), TimeSpan.FromHours(11), 5000m);

        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([reservaDelCliente, reservaDeOtro]);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recurso]);

        var result = await _useCase.ExecuteAsync(negocioId, cliente.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Reservas.Should().HaveCount(1);
        result.Value.Reservas[0].Id.Should().Be(reservaDelCliente.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ConVariasReservas_LasOrdenaPorFechaYHoraDescendente()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var cliente = Cliente.Crear("Juan Perez", "juan@test.com");
        var reservaTemprano = Reserva.Crear(
            recurso.Id, cliente.Id, new DateOnly(2026, 7, 20), TimeSpan.FromHours(9), TimeSpan.FromHours(10), 5000m);
        var reservaTarde = Reserva.Crear(
            recurso.Id, cliente.Id, new DateOnly(2026, 7, 20), TimeSpan.FromHours(20), TimeSpan.FromHours(21), 5000m);

        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([reservaTemprano, reservaTarde]);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recurso]);

        var result = await _useCase.ExecuteAsync(negocioId, cliente.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Reservas.Should().HaveCount(2);
        result.Value.Reservas[0].Id.Should().Be(reservaTarde.Id);
        result.Value.Reservas[1].Id.Should().Be(reservaTemprano.Id);
        result.Value.Reservas[0].RecursoNombre.Should().Be("Cancha 1");
    }
}
