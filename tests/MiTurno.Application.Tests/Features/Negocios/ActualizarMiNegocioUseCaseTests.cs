using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Negocios;
using MiTurno.Application.Features.Negocios.Dtos;
using MiTurno.Domain.Entities;

namespace MiTurno.Application.Tests.Features.Negocios;

public class ActualizarMiNegocioUseCaseTests
{
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ActualizarMiNegocioUseCase _useCase;

    public ActualizarMiNegocioUseCaseTests()
    {
        _useCase = new ActualizarMiNegocioUseCase(new ActualizarMiNegocioValidator(), _negocioRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_ActualizaLaAnticipacionMinimaYDevuelveLaRespuesta()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);
        var request = new ActualizarMiNegocioRequest("Cancha Norte", null, null, null, AnticipacionMinimaHoras: 24);

        var result = await _useCase.ExecuteAsync(negocio.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.AnticipacionMinimaHoras.Should().Be(24);
        negocio.AnticipacionMinimaHoras.Should().Be(24);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConAnticipacionMinimaNegativa_DevuelveFailureDeValidacionSinGuardar()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);
        var request = new ActualizarMiNegocioRequest("Cancha Norte", null, null, null, AnticipacionMinimaHoras: -1);

        var result = await _useCase.ExecuteAsync(negocio.Id, request);

        result.IsFailure.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConAnticipacionMinimaPorEncimaDelMaximo_DevuelveFailureDeValidacion()
    {
        var negocio = Negocio.Crear("Cancha Norte", "cancha-norte", "negocio@test.com");
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);
        var request = new ActualizarMiNegocioRequest("Cancha Norte", null, null, null, AnticipacionMinimaHoras: 721);

        var result = await _useCase.ExecuteAsync(negocio.Id, request);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConNegocioInexistente_DevuelveFailure()
    {
        _negocioRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Negocio?)null);
        var request = new ActualizarMiNegocioRequest("Cancha Norte", null, null, null, AnticipacionMinimaHoras: 0);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Negocio no encontrado.");
    }
}
