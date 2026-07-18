using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Reservas;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Reservas;

public class ListarReservasUseCaseTests
{
    private readonly IReservaRepository _reservaRepository = Substitute.For<IReservaRepository>();
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IClienteRepository _clienteRepository = Substitute.For<IClienteRepository>();

    private readonly ListarReservasUseCase _useCase;

    public ListarReservasUseCaseTests()
    {
        _useCase = new ListarReservasUseCase(_reservaRepository, _recursoRepository, _clienteRepository);
    }

    [Fact]
    public async Task ExecuteAsync_SinReservas_DevuelveListaVaciaSinConsultarRecursosNiClientes()
    {
        var negocioId = Guid.NewGuid();
        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([]);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.Should().BeEmpty();
        await _recursoRepository.DidNotReceive().GetByNegocioIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConReservas_ResuelveNombreDeRecursoYDatosDelCliente()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var cliente = Cliente.Crear("Juan Perez", "juan@test.com");
        var reserva = Reserva.Crear(
            recurso.Id, cliente.Id, new DateOnly(2026, 7, 20), TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([reserva]);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recurso]);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.Should().ContainSingle();
        result[0].RecursoNombre.Should().Be("Cancha 1");
        result[0].ClienteNombre.Should().Be("Juan Perez");
        result[0].ClienteEmail.Should().Be("juan@test.com");
    }

    [Fact]
    public async Task ExecuteAsync_ConVariasReservasDelMismoCliente_ConsultaElClienteUnaSolaVez()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromHours(1), 5000m);
        var cliente = Cliente.Crear("Juan Perez", "juan@test.com");
        var reserva1 = Reserva.Crear(
            recurso.Id, cliente.Id, new DateOnly(2026, 7, 20), TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);
        var reserva2 = Reserva.Crear(
            recurso.Id, cliente.Id, new DateOnly(2026, 7, 21), TimeSpan.FromHours(18), TimeSpan.FromHours(19), 5000m);

        _reservaRepository.GetByNegocioIdAsync(negocioId).Returns([reserva1, reserva2]);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recurso]);
        _clienteRepository.GetByIdAsync(cliente.Id).Returns(cliente);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.Should().HaveCount(2);
        await _clienteRepository.Received(1).GetByIdAsync(cliente.Id, Arg.Any<CancellationToken>());
    }
}
