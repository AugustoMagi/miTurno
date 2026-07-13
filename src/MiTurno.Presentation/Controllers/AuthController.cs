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

    public AuthController(RegistrarNegocioUseCase registrarNegocioUseCase, LoginUseCase loginUseCase)
    {
        _registrarNegocioUseCase = registrarNegocioUseCase;
        _loginUseCase = loginUseCase;
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
}
