using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Clientes;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Clientes;

public class ListarClientesUseCaseTests
{
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();

    private readonly ListarClientesUseCase _useCase;

    public ListarClientesUseCaseTests()
    {
        _useCase = new ListarClientesUseCase(_reservaRepository, _clienteRepository);
    }

    [Fact]
    public async Task ExecuteAsync_SinReservas_DevuelveListaVacia()
    {
        var negocioId = Guid.NewGuid();
        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([]);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.Should().BeEmpty();
        await _clienteRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConVariasReservasDelMismoCliente_AgrupaYCuentaTotalDeReservas()
    {
        var negocioId = Guid.NewGuid();
        var recursoId = Guid.NewGuid();
        var cliente = Cliente.Crear("Juan Perez", "juan@test.com");
        var reservaVieja = Reserva.Crear(
            recursoId, cliente.Id, new DateOnly(2026, 1, 10), TimeSpan.FromHours(10), TimeSpan.FromHours(11), 5000m);
        var reservaReciente = Reserva.Crear(
            recursoId, cliente.Id, new DateOnly(2026, 7, 20), TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([reservaVieja, reservaReciente]);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.Should().HaveCount(1);
        result[0].TotalReservas.Should().Be(2);
        result[0].UltimaReserva.Should().Be(new DateOnly(2026, 7, 20));
    }

    [Fact]
    public async Task ExecuteAsync_ConClientesDistintos_OrdenaPorUltimaReservaDescendente()
    {
        var negocioId = Guid.NewGuid();
        var recursoId = Guid.NewGuid();
        var clienteAntiguo = Cliente.Crear("Cliente Antiguo", "antiguo@test.com");
        var clienteReciente = Cliente.Crear("Cliente Reciente", "reciente@test.com");
        var reservaAntigua = Reserva.Crear(
            recursoId, clienteAntiguo.Id, new DateOnly(2026, 1, 10), TimeSpan.FromHours(10), TimeSpan.FromHours(11), 5000m);
        var reservaReciente = Reserva.Crear(
            recursoId, clienteReciente.Id, new DateOnly(2026, 7, 20), TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([reservaAntigua, reservaReciente]);
        _clienteRepository.GetByIdAsync(clienteAntiguo.Id).Returns(clienteAntiguo);
        _clienteRepository.GetByIdAsync(clienteReciente.Id).Returns(clienteReciente);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.Should().HaveCount(2);
        result[0].Nombre.Should().Be("Cliente Reciente");
        result[1].Nombre.Should().Be("Cliente Antiguo");
    }
}
