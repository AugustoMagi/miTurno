using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Services;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Common.Services;

public class ValidarLimiteRecursosServiceTests
{
    private readonly IRecursoRepository _recursoRepository = Substitute.For<IRecursoRepository>();
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();

    private readonly ValidarLimiteRecursosService _service;

    public ValidarLimiteRecursosServiceTests()
    {
        _service = new ValidarLimiteRecursosService(_recursoRepository, _suscripcionRepository);
    }

    [Fact]
    public async Task ValidarAsync_SinSuscripcionAsignada_DevuelveSuccess()
    {
        var negocioId = Guid.NewGuid();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns((Suscripcion?)null);

        var result = await _service.ValidarAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarAsync_ConRecursosActivosPorDebajoDelLimite_DevuelveSuccess()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, limiteRecursos: 2, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m)
        ]);

        var result = await _service.ValidarAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarAsync_ConRecursosActivosEnElLimite_DevuelveFailureConElNombreDelPlan()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, limiteRecursos: 1, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m)
        ]);

        var result = await _service.ValidarAsync(negocioId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Básico");
    }

    [Fact]
    public async Task ValidarAsync_ConRecursosDesactivadosSinContarParaElLimite_DevuelveSuccess()
    {
        var negocioId = Guid.NewGuid();
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, limiteRecursos: 1, limiteReservasPorMes: 200);
        var suscripcion = Suscripcion.IniciarPrueba(negocioId, plan);
        var recursoDesactivado = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        recursoDesactivado.Desactivar();
        _suscripcionRepository.GetByNegocioIdAsync(negocioId).Returns(suscripcion);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([recursoDesactivado]);

        var result = await _service.ValidarAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarCambioDePlanAsync_ConMasCanchasActivasQueElLimiteDelPlanNuevo_DevuelveFailureConElNombreDelPlanNuevo()
    {
        var negocioId = Guid.NewGuid();
        var planNuevo = Plan.Crear("Estándar", 5000m, Periodicidad.Mensual, limiteRecursos: 2, limiteReservasPorMes: 200);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m),
            Recurso.Crear(negocioId, "Cancha 2", "Futbol", TimeSpan.FromMinutes(60), 5000m),
            Recurso.Crear(negocioId, "Cancha 3", "Futbol", TimeSpan.FromMinutes(60), 5000m),
        ]);

        var result = await _service.ValidarCambioDePlanAsync(negocioId, planNuevo);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Estándar");
        result.Error.Should().Contain("3");
    }

    [Fact]
    public async Task ValidarCambioDePlanAsync_ConCanchasActivasEnElLimiteDelPlanNuevo_DevuelveSuccess()
    {
        var negocioId = Guid.NewGuid();
        var planNuevo = Plan.Crear("Estándar", 5000m, Periodicidad.Mensual, limiteRecursos: 2, limiteReservasPorMes: 200);
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m),
            Recurso.Crear(negocioId, "Cancha 2", "Futbol", TimeSpan.FromMinutes(60), 5000m),
        ]);

        var result = await _service.ValidarCambioDePlanAsync(negocioId, planNuevo);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarCambioDePlanAsync_ConCanchasDesactivadasSinContarParaElLimite_DevuelveSuccess()
    {
        var negocioId = Guid.NewGuid();
        var planNuevo = Plan.Crear("Estándar", 5000m, Periodicidad.Mensual, limiteRecursos: 1, limiteReservasPorMes: 200);
        var recursoDesactivado = Recurso.Crear(negocioId, "Cancha 1", "Futbol", TimeSpan.FromMinutes(60), 5000m);
        recursoDesactivado.Desactivar();
        _recursoRepository.GetByNegocioIdAsync(negocioId).Returns([
            recursoDesactivado,
            Recurso.Crear(negocioId, "Cancha 2", "Futbol", TimeSpan.FromMinutes(60), 5000m),
        ]);

        var result = await _service.ValidarCambioDePlanAsync(negocioId, planNuevo);

        result.IsSuccess.Should().BeTrue();
    }
}
