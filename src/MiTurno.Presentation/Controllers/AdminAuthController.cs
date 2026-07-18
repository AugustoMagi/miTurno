using Microsoft.AspNetCore.Mvc;
using MiTurno.Application.Features.Admin.Auth;
using MiTurno.Application.Features.Admin.Auth.Dtos;

namespace MiTurno.Presentation.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly LoginSysAdminUseCase _loginSysAdminUseCase;

    public AdminAuthController(LoginSysAdminUseCase loginSysAdminUseCase)
    {
        _loginSysAdminUseCase = loginSysAdminUseCase;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginSysAdminRequest request, CancellationToken cancellationToken)
    {
        var result = await _loginSysAdminUseCase.ExecuteAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }
}
