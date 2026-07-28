using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Recursos;
using MiTurno.Application.Features.Recursos.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Recursos;

public class CrearRecursoUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CrearRecursoUseCase _useCase;

    public CrearRecursoUseCaseTests()
    {
        var validarLimiteRecursosService = new ValidarLimiteRecursosService(_recursoRepository, _suscripcionRepository);
        _useCase = new CrearRecursoUseCase(new CrearRecursoValidator(), _recursoRepository, validarLimiteRecursosService, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripcionAsignada_CreaElRecursoSinLimite()
    {
        var negocioId = Guid.NewGuid();
        var request = new CrearRecursoRequest("Cancha 1", "Futbol", 60, 5000m);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);

        var result = await _useCase.ExecuteAsync(negocioId, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.NegocioId.Should().Be(negocioId);
        result.Value.Nombre.Should().Be("Cancha 1");
        result.Value.DuracionTurnoMinutos.Should().Be(60);
        await _recursoRepository.Received(1).AddAsync(Arg.Any<Recurso>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConCupoDisponibleEnElPlan_CreaElRecurso()
    {
        var negocioId = Guid.NewGuid();
        var request = new CrearRecursoRequest("Cancha 2", "Futbol", 60, 5000m);
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, limiteRecursos: 3, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m)
        ]);

        var result = await _useCase.ExecuteAsync(negocioId, request);

        result.IsSuccess.Should().BeTrue();
        await _recursoRepository.Received(1).AddAsync(Arg.Any<Recurso>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConElLimiteDeCanchasDelPlanAlcanzado_DevuelveFailureSinCrearNada()
    {
        var negocioId = Guid.NewGuid();
        var request = new CrearRecursoRequest("Cancha 2", "Futbol", 60, 5000m);
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, limiteRecursos: 1, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m)
        ]);

        var result = await _useCase.ExecuteAsync(negocioId, request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Básico");
        await _recursoRepository.DidNotReceive().AddAsync(Arg.Any<Recurso>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConUnRecursoDesactivadoLiberandoCupo_CreaElRecurso()
    {
        var negocioId = Guid.NewGuid();
        var request = new CrearRecursoRequest("Cancha 2", "Futbol", 60, 5000m);
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, limiteRecursos: 1, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        var recursoDesactivado = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        recursoDesactivado.Desactivar();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recursoDesactivado]);

        var result = await _useCase.ExecuteAsync(negocioId, request);

        result.IsSuccess.Should().BeTrue();
        await _recursoRepository.Received(1).AddAsync(Arg.Any<Recurso>(), Arg.Any<CancellationToken>());
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
