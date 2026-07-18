using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.ConfiguracionesPago;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.ConfiguracionesPago;

public class ObtenerConfiguracionPagoUseCaseTests
{
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository = Substitute.For<IConfiguracionPagoRepository>();

    private readonly ObtenerConfiguracionPagoUseCase _useCase;

    public ObtenerConfiguracionPagoUseCaseTests()
    {
        _useCase = new ObtenerConfiguracionPagoUseCase(_configuracionPagoRepository);
    }

    [Fact]
    public async Task ExecuteAsync_ConConfiguracionActiva_DevuelveSusDatos()
    {
        var negocioId = Guid.NewGuid();
        var configuracion = Domain.Entities.ConfiguracionPago.Conectar(negocioId, ProveedorPago.MercadoPago, "alias.mp");
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId).Returns(configuracion);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Proveedor.Should().Be(ProveedorPago.MercadoPago);
        result.Value.Alias.Should().Be("alias.mp");
    }

    [Fact]
    public async Task ExecuteAsync_SinConfiguracionActiva_DevuelveFailure()
    {
        var negocioId = Guid.NewGuid();
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId).Returns((Domain.Entities.ConfiguracionPago?)null);

        var result = await _useCase.ExecuteAsync(negocioId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("El negocio no tiene un método de pago conectado.");
    }
}
