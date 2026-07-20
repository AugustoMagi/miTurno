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
    private readonly IPlanRepository _planRepository;
    private readonly ISuscripcionRepository _suscripcionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarNegocioUseCase(
        IValidator<RegistrarNegocioRequest> validator,
        INegocioRepository negocioRepository,
        IUsuarioRepository usuarioRepository,
        IPlanRepository planRepository,
        ISuscripcionRepository suscripcionRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _validator = validator;
        _negocioRepository = negocioRepository;
        _usuarioRepository = usuarioRepository;
        _planRepository = planRepository;
        _suscripcionRepository = suscripcionRepository;
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

            // Si todavía no hay ningún Plan marcado como "de prueba" (ej. el SysAdmin no cargó
            // ninguno aún), el negocio queda sin Suscripcion — el gating público lo trata como
            // acceso permitido, no como bloqueado.
            var planDePrueba = await _planRepository.GetPlanDePruebaAsync(cancellationToken);
            if (planDePrueba is not null)
            {
                var suscripcion = Suscripcion.IniciarPrueba(negocio.Id, planDePrueba);
                await _suscripcionRepository.AddAsync(suscripcion, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var token = _jwtTokenGenerator.GenerarToken(usuario);

            return Result.Success(new RegistrarNegocioResponse(
                usuario.Id, negocio.Id, negocio.Slug, usuario.Nombre, usuario.Email, usuario.Rol.ToString(), token));
        }
        catch (DomainException ex)
        {
            return Result.Failure<RegistrarNegocioResponse>(ex.Message);
        }
    }
}
