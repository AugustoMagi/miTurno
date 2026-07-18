using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Admin.Auth.Dtos;

namespace MiTurno.Application.Features.Admin.Auth;

public class LoginSysAdminUseCase
{
    private readonly IValidator<LoginSysAdminRequest> _validator;
    private readonly ISysAdminRepository _sysAdminRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginSysAdminUseCase(
        IValidator<LoginSysAdminRequest> validator,
        ISysAdminRepository sysAdminRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _validator = validator;
        _sysAdminRepository = sysAdminRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<LoginSysAdminResponse>> ExecuteAsync(
        LoginSysAdminRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<LoginSysAdminResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var admin = await _sysAdminRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Mismo mensaje para "no existe" y "contraseña incorrecta", igual que LoginUseCase.
        if (admin is null || !admin.Activo || !_passwordHasher.Verify(request.Password, admin.PasswordHash))
            return Result.Failure<LoginSysAdminResponse>("Email o contraseña incorrectos.");

        var token = _jwtTokenGenerator.GenerarToken(admin);

        return Result.Success(new LoginSysAdminResponse(admin.Id, admin.Nombre, admin.Email, token));
    }
}
