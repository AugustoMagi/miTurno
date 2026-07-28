using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Services;
using MiTurno.Application.Features.Recursos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Recursos;

public class CambiarEstadoRecursoUseCaseTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly CambiarEstadoRecursoUseCase _useCase;

    public CambiarEstadoRecursoUseCaseTests()
    {
        var validarLimiteRecursosService = new ValidarLimiteRecursosService(_recursoRepository, _suscripcionRepository);
        _useCase = new CambiarEstadoRecursoUseCase(_recursoRepository, validarLimiteRecursosService, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConActivarFalse_DesactivaElRecurso()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, activar: false);

        result.IsSuccess.Should().BeTrue();
        recurso.Activo.Should().BeFalse();
        _recursoRepository.Received(1).Update(recurso);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConActivarTrue_ReactivaElRecurso()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        recurso.Desactivar();
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, activar: true);

        result.IsSuccess.Should().BeTrue();
        recurso.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConActivarTrue_ConElLimiteDeCanchasDelPlanAlcanzado_DevuelveFailureSinReactivar()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 2", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        recurso.Desactivar();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, limiteRecursos: 1, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m), recurso
        ]);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, activar: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Básico");
        recurso.Activo.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConActivarTrue_ConCupoDisponibleEnElPlan_ReactivaElRecurso()
    {
        var negocioId = Guid.NewGuid();
        var recurso = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        recurso.Desactivar();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, limiteRecursos: 3, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recurso]);

        var result = await _useCase.ExecuteAsync(negocioId, recurso.Id, activar: true);

        result.IsSuccess.Should().BeTrue();
        recurso.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ConRecursoDeOtroNegocio_DevuelveFailureSinModificar()
    {
        var recurso = Recurso.Crear(Guid.NewGuid(), "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        _recursoRepository.GetByIdAsync(recurso.Id).Returns(recurso);

        var result = await _useCase.ExecuteAsync(Guid.NewGuid(), recurso.Id, activar: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Recurso no encontrado.");
        recurso.Activo.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
