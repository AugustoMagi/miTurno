using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Recursos;
using MiTurno.Application.Features.Recursos.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Recursos;

public class CrearRecursoUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CrearRecursoUseCase _useCase;

    public CrearRecursoUseCaseTests()
    {
        _useCase = new CrearRecursoUseCase(new CrearRecursoValidator(), _recursoRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_CreaElRecursoParaElNegocio()
    {
        var negocioId = Guid.NewGuid();
        var request = new CrearRecursoRequest("Cancha 1", "Futbol", 60, 5000m);

        var result = await _useCase.ExecuteAsync(negocioId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.NegocioId.Should().Be(negocioId);
        result.Value.Nombre.Should().Be("Cancha 1");
        result.Value.DuracionTurnoMinutos.Should().Be(60);
        await _recursoRepository.Received(1).AddAsync(Arg.Any<Recurso>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConNombreVacio_DevuelveFailureDeValidacionSinCrearNada()
    {
        var request = new CrearRecursoRequest("", "Futbol", 60, 5000m);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
        await _recursoRepository.DidNotReceive().AddAsync(Arg.Any<Recurso>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConDuracionCero_DevuelveFailureDeValidacion()
    {
        var request = new CrearRecursoRequest("Cancha 1", "Futbol", 0, 5000m);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConPrecioNegativo_DevuelveFailureDeValidacion()
    {
        var request = new CrearRecursoRequest("Cancha 1", "Futbol", 60, -100m);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
    }
}
