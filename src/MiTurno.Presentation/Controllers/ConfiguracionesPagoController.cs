using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.ConfiguracionesPago;
using MiTurno.Application.Features.ConfiguracionesPago.Dtos;
using MiTurno.Presentation.Extensions;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/configuracion-pago")]
[Authorize(Roles = "Owner,Empleado")]
public class ConfiguracionesPagoController : ControllerBase
{
    private readonly ConectarConfiguracionPagoUseCase _conectarConfiguracionPagoUseCase;
    private readonly ObtenerConfiguracionPagoUseCase _obtenerConfiguracionPagoUseCase;
    private readonly DesconectarConfiguracionPagoUseCase _desconectarConfiguracionPagoUseCase;
    private readonly IniciarConexionMercadoPagoUseCase _iniciarConexionMercadoPagoUseCase;
    private readonly ProcesarCallbackMercadoPagoUseCase _procesarCallbackMercadoPagoUseCase;
    private readonly IFrontendConfiguracion _frontendConfiguracion;

    public ConfiguracionesPagoController(
        ConectarConfiguracionPagoUseCase conectarConfiguracionPagoUseCase,
        ObtenerConfiguracionPagoUseCase obtenerConfiguracionPagoUseCase,
        DesconectarConfiguracionPagoUseCase desconectarConfiguracionPagoUseCase,
        IniciarConexionMercadoPagoUseCase iniciarConexionMercadoPagoUseCase,
        ProcesarCallbackMercadoPagoUseCase procesarCallbackMercadoPagoUseCase,
        IFrontendConfiguracion frontendConfiguracion)
    {
        _conectarConfiguracionPagoUseCase = conectarConfiguracionPagoUseCase;
        _obtenerConfiguracionPagoUseCase = obtenerConfiguracionPagoUseCase;
        _desconectarConfiguracionPagoUseCase = desconectarConfiguracionPagoUseCase;
        _iniciarConexionMercadoPagoUseCase = iniciarConexionMercadoPagoUseCase;
        _procesarCallbackMercadoPagoUseCase = procesarCallbackMercadoPagoUseCase;
        _frontendConfiguracion = frontendConfiguracion;
    }

    [HttpPost]
    public async Task<IActionResult> Conectar(ConectarConfiguracionPagoRequest request, CancellationToken cancellationToken)
    {
        var result = await _conectarConfiguracionPagoUseCase.ExecuteAsync(User.GetNegocioId(), request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var result = await _obtenerConfiguracionPagoUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpDelete]
    public async Task<IActionResult> Desconectar(CancellationToken cancellationToken)
    {
        var result = await _desconectarConfiguracionPagoUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    /// <summary>Devuelve la URL de Mercado Pago a la que el frontend debe redirigir al dueño para que autorice la conexión.</summary>
    [HttpGet("mercadopago/conectar")]
    public async Task<IActionResult> ConectarMercadoPago(CancellationToken cancellationToken)
    {
        var url = await _iniciarConexionMercadoPagoUseCase.ExecuteAsync(User.GetNegocioId(), cancellationToken);
        return Ok(new { url });
    }

    /// <summary>
    /// Mercado Pago redirige acá al navegador del dueño después de que autoriza (o rechaza) la
    /// conexión — nunca lleva el JWT del panel, así que es anónimo; la identidad del negocio viaja
    /// cifrada dentro de "state". Termina siempre redirigiendo de vuelta al panel, con el resultado
    /// en la query string para que la pantalla de Cobro lo muestre.
    /// </summary>
    [HttpGet("mercadopago/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> CallbackMercadoPago(
        [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var volverAlPanel = $"{_frontendConfiguracion.BaseUrl}/panel/configuracion-pago";

        if (!string.IsNullOrWhiteSpace(error))
            return Redirect($"{volverAlPanel}?mp=error&mensaje={Uri.EscapeDataString("El negocio rechazó la conexión con Mercado Pago.")}");

        var result = await _procesarCallbackMercadoPagoUseCase.ExecuteAsync(code, state, cancellationToken);
        return result.IsSuccess
            ? Redirect($"{volverAlPanel}?mp=conectado")
            : Redirect($"{volverAlPanel}?mp=error&mensaje={Uri.EscapeDataString(result.Error!)}");
    }
}
