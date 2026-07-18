using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.ConfiguracionesPago;
using MiTurno.Application.Features.ConfiguracionesPago.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.ConfiguracionesPago;

public class ConectarConfiguracionPagoUseCaseTests
{
    private readonly IConfiguracionPagoRepository _configuracionPagoRepository = Substitute.For<IConfiguracionPagoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ConectarConfiguracionPagoUseCase _useCase;

    public ConectarConfiguracionPagoUseCaseTests()
    {
        _useCase = new ConectarConfiguracionPagoUseCase(
            new ConectarConfiguracionPagoValidator(), _configuracionPagoRepository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_SinConfiguracionPrevia_ConectaYDevuelveLaNueva()
    {
        var negocioId = Guid.NewGuid();
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId).Returns((Domain.Entities.ConfiguracionPago?)null);

        var result = await _useCase.ExecuteAsync(
            negocioId, new ConectarConfiguracionPagoRequest(ProveedorPago.MercadoPago, "alias.mp"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Proveedor.Should().Be(ProveedorPago.MercadoPago);
        result.Value.Alias.Should().Be("alias.mp");
        await _configuracionPagoRepository.Received(1).AddAsync(
            Arg.Any<Domain.Entities.ConfiguracionPago>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConConfiguracionActivaPrevia_LaDesconectaAntesDeConectarLaNueva()
    {
        var negocioId = Guid.NewGuid();
        var anterior = Domain.Entities.ConfiguracionPago.Conectar(negocioId, ProveedorPago.Stripe, "link-viejo");
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId).Returns(anterior);

        var result = await _useCase.ExecuteAsync(
            negocioId, new ConectarConfiguracionPagoRequest(ProveedorPago.MercadoPago, "alias.mp"));

        result.IsSuccess.Should().BeTrue();
        anterior.Activo.Should().BeFalse();
        _configuracionPagoRepository.Received(1).Update(anterior);
    }

    [Fact]
    public async Task ExecuteAsync_ConAliasVacio_DevuelveFailureDeValidacionSinConsultarRepositorio()
    {
        var result = await _useCase.ExecuteAsync(
            Guid.NewGuid(), new ConectarConfiguracionPagoRequest(ProveedorPago.MercadoPago, ""));

        result.IsFailure.Should().BeTrue();
        await _configuracionPagoRepository.DidNotReceive().GetActivaByNegocioIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConAccessToken_LoGuardaYNoLoExponeEnLaRespuesta()
    {
        var negocioId = Guid.NewGuid();
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId).Returns((Domain.Entities.ConfiguracionPago?)null);

        var result = await _useCase.ExecuteAsync(
            negocioId, new ConectarConfiguracionPagoRequest(ProveedorPago.MercadoPago, "alias.mp", "TEST-ACCESS-TOKEN"));

        result.IsSuccess.Should().BeTrue();
        result.Value.TieneAccessToken.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_SinAccessToken_TieneAccessTokenEsFalse()
    {
        var negocioId = Guid.NewGuid();
        _configuracionPagoRepository.GetActivaByNegocioIdAsync(negocioId).Returns((Domain.Entities.ConfiguracionPago?)null);

        var result = await _useCase.ExecuteAsync(
            negocioId, new ConectarConfiguracionPagoRequest(ProveedorPago.MercadoPago, "alias.mp"));

        result.IsSuccess.Should().BeTrue();
        result.Value.TieneAccessToken.Should().BeFalse();
    }
}
