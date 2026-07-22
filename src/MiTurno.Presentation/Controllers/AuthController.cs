using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Auth;
using MiTurno.Application.Features.Auth.Dtos;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegistrarNegocioUseCase _registrarNegocioUseCase;
    private readonly LoginUseCase _loginUseCase;
    private readonly SolicitarReseteoPasswordUseCase _solicitarReseteoPasswordUseCase;
    private readonly RestablecerPasswordUseCase _restablecerPasswordUseCase;

    public AuthController(
        RegistrarNegocioUseCase registrarNegocioUseCase,
        LoginUseCase loginUseCase,
        SolicitarReseteoPasswordUseCase solicitarReseteoPasswordUseCase,
        RestablecerPasswordUseCase restablecerPasswordUseCase)
    {
        _registrarNegocioUseCase = registrarNegocioUseCase;
        _loginUseCase = loginUseCase;
        _solicitarReseteoPasswordUseCase = solicitarReseteoPasswordUseCase;
        _restablecerPasswordUseCase = restablecerPasswordUseCase;
    }

    [HttpPost("registro")]
    public async Task<IActionResult> Registrar(RegistrarNegocioRequest request, CancellationToken cancellationToken)
    {
        var result = await _registrarNegocioUseCase.ExecuteAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _loginUseCase.ExecuteAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    [HttpPost("solicitar-reseteo-password")]
    public async Task<IActionResult> SolicitarReseteoPassword(
        SolicitarReseteoPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _solicitarReseteoPasswordUseCase.ExecuteAsync(request, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    [HttpPost("restablecer-password")]
    public async Task<IActionResult> RestablecerPassword(
        RestablecerPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _restablecerPasswordUseCase.ExecuteAsync(request, cancellationToken);
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }
}
