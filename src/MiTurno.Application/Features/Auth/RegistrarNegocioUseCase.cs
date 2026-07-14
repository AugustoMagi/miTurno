using FluentValidation;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;
using MiTurno.Application.Features.Auth.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;
using MiTurno.Domain.Exceptions;

namespace MiTurno.Application.Features.Auth;

/// <summary>
/// Da de alta un negocio junto con su primer usuario (rol Owner) y devuelve un token
/// ya autenticado, para no forzar un login aparte inmediatamente después de registrarse.
/// </summary>
public class RegistrarNegocioUseCase
{
    private readonly IValidator<RegistrarNegocioRequest> _validator;
    private readonly INegocioRepository _negocioRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarNegocioUseCase(
        IValidator<RegistrarNegocioRequest> validator,
        INegocioRepository negocioRepository,
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _negocioRepository = negocioRepository;
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RegistrarNegocioResponse>> ExecuteAsync(
        RegistrarNegocioRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<RegistrarNegocioResponse>(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        if (await _negocioRepository.GetBySlugAsync(request.Slug, cancellationToken) is not null)
            return Result.Failure<RegistrarNegocioResponse>("Ya existe un negocio con ese slug.");

        if (await _usuarioRepository.GetByEmailAsync(request.EmailUsuario, cancellationToken) is not null)
            return Result.Failure<RegistrarNegocioResponse>("Ya existe un usuario con ese email.");

        try
        {
            var negocio = Negocio.Crear(request.NombreNegocio, request.Slug, request.EmailNegocio);
            var passwordHash = _passwordHasher.Hash(request.Password);
            var usuario = Usuario.Crear(
                negocio.Id, request.NombreUsuario, request.EmailUsuario, passwordHash, RolUsuario.Owner);

            await _negocioRepository.AddAsync(negocio, cancellationToken);
            await _usuarioRepository.AddAsync(usuario, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var token = _jwtTokenGenerator.GenerarToken(usuario);

            return Result.Success(new RegistrarNegocioResponse(negocio.Id, negocio.Slug, usuario.Id, token));
        }
        catch (DomainException ex)
        {
            return Result.Failure<RegistrarNegocioResponse>(ex.Message);
        }
    }
}
