using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.ConfiguracionesPago;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.ConfiguracionesPago;

public class DesconectarConfiguracionPagoUseCaseTests
{
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository = Substitute.For<IConfiguracionPagoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly DesconectarConfiguracionPagoUseCase _useCase;

    public DesconectarConfiguracionPagoUseCaseTests()
    {
        _useCase = new DesconectarConfiguracionPagoUseCase(_configuracionPagoRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_ConConfiguracionActiva_LaDesconectaYPersiste()
    {
        var negocioId = Guid.NewGuid();
        var configuracion = Domain.Entities.ConfiguracionPago.Conectar(negocioId, ProveedorPago.MercadoPago, "alias.mp");
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId).Returns(configuracion);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        configuracion.Activo.Should().BeFalse();
        _configuracionPagoRepository.Received(1).Update(configuracion);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SinConfiguracionActiva_DevuelveFailureSinPersistir()
    {
        var negocioId = Guid.NewGuid();
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId).Returns((Domain.Entities.ConfiguracionPago?)null);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("El negocio no tiene un método de pago conectado.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
