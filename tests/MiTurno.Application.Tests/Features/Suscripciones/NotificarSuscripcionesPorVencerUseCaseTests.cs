using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Suscripciones;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Suscripciones;

public class NotificarSuscripcionesPorVencerUseCaseTests
{
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IEmailNotificador _emailNotificador = Substitute.For<IEmailNotificador>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly NotificarSuscripcionesPorVencerUseCase _useCase;

    public NotificarSuscripcionesPorVencerUseCaseTests()
    {
        _useCase = new NotificarSuscripcionesPorVencerUseCase(
            _suscripcionRepository, _negocioRepository, _emailNotificador, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConSuscripcionesPorVencer_NotificaYMarcaElFlagComoEnviado()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var negocio = Negocio.Crear("Cancha 1", "cancha-1", "dueno@test.com");
        var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, plan);
        _suscripcionRepository.GetPendientesDeNotificarVencimientoAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([suscripcion]);
        _negocioRepository.GetByIdAsync(negocio.Id).Returns(negocio);

        var result = await _useCase.ExecuteAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        suscripcion.NotificacionVencimientoEnviada.Should().BeTrue();
        _suscripcionRepository.Received(1).Update(suscripcion);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailNotificador.Received(1).NotificarSuscripcionPorVencerAsync(
            Arg.Is<NotificacionSuscripcionPorVencer>(n => n!.NegocioEmail == "dueno@test.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SinSuscripcionesPorVencer_NoNotificaNiGuardaCambios()
    {
        _suscripcionRepository.GetPendientesDeNotificarVencimientoAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await _useCase.ExecuteAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        await _emailNotificador.DidNotReceive().NotificarSuscripcionPorVencerAsync(
            Arg.Any<NotificacionSuscripcionPorVencer>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConNegocioInexistente_LaSalteaSinRomper()
    {
        var plan = Plan.Crear("Básico", 5000m, Periodicidad.Mensual, 3, 200);
        var suscripcion = Suscripcion.IniciarPrueba(Guid.NewGuid(), plan);
        _suscripcionRepository.GetPendientesDeNotificarVencimientoAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([suscripcion]);
        _negocioRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Negocio?)null);

        var result = await _useCase.ExecuteAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
        suscripcion.NotificacionVencimientoEnviada.Should().BeFalse();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
