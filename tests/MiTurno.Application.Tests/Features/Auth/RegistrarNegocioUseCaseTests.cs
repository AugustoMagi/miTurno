using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Features.Auth;
using MiTurno.Application.Features.Auth.Dtos;
using MiTurno.Domain.Entities;
using MiTurno.Domain.Enums;

namespace MiTurno.Application.Tests.Features.Auth;

public class RegistrarNegocioUseCaseTests
{
    private readonly INegocioRepository _negocioRepository = Substitute.For<INegocioRepository>();
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IPlanRepository _planRepository = Substitute.For<IPlanRepository>();
    private readonly ISuscripcionRepository _suscripcionRepository = Substitute.For<ISuscripcionRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly RegistrarNegocioUseCase _useCase;

    public RegistrarNegocioUseCaseTests()
    {
        _useCase = new RegistrarNegocioUseCase(
            new RegistrarNegocioValidator(), _negocioRepository, _usuarioRepository,
            _planRepository, _suscripcionRepository, _passwordHasher, _jwtTokenGenerator, _unitOfWork);

        _negocioRepository.GetBySlugAsync(Arg.Any<string>()).Returns((Negocio?)null);
        _usuarioRepository.GetByEmailAsync(Arg.Any<string>()).Returns((Usuario?)null);
        _planRepository.GetPlanDePruebaAsync().Returns((Plan?)null);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hash-seguro");
        _jwtTokenGenerator.GenerarToken(Arg.Any<Usuario>()).Returns("token-jwt");
    }

    private static RegistrarNegocioRequest RequestValido() => new(
        "Cancha Norte", "cancha-norte", "negocio@test.com", "Ana Owner", "ana@test.com", "Password123");

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_CreaNegocioYUsuarioOwnerYDevuelveToken()
    {
        var result = await _useCase.ExecuteAsync(RequestValido());

        result.IsSuccess.Should().BeTrue();
        result.Value.NegocioSlug.Should().Be("cancha-norte");
        result.Value.Token.Should().Be("token-jwt");
        await _negocioRepository.Received(1).AddAsync(
            Arg.Is<Negocio>(n => n!.Slug == "cancha-norte"), Arg.Any<CancellationToken>());
        await _usuarioRepository.Received(1).AddAsync(
            Arg.Is<Usuario>(u => u!.Rol == RolUsuario.Owner), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConSlugYaUsado_DevuelveFailureSinCrearNada()
    {
        var negocioExistente = Negocio.Crear("Otro", "cancha-norte", "otro@test.com");
        _negocioRepository.GetBySlugAsync("cancha-norte").Returns(negocioExistente);

        var result = await _useCase.ExecuteAsync(RequestValido());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Ya existe un negocio con ese slug.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConEmailDeUsuarioYaRegistrado_DevuelveFailureSinCrearNada()
    {
        var usuarioExistente = Usuario.Crear(Guid.NewGuid(), "Otro", "ana@test.com", "hash", RolUsuario.Owner);
        _usuarioRepository.GetByEmailAsync("ana@test.com").Returns(usuarioExistente);

        var result = await _useCase.ExecuteAsync(RequestValido());

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Ya existe un usuario con ese email.");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("SLUG-CON-MAYUSCULAS")]
    [InlineData("slug con espacios")]
    public async Task ExecuteAsync_ConSlugInvalido_DevuelveFailureDeValidacionSinConsultarRepositorios(string slugInvalido)
    {
        var request = RequestValido() with { Slug = slugInvalido };

        var result = await _useCase.ExecuteAsync(request);

        result.IsFailure.Should().BeTrue();
        await _negocioRepository.DidNotReceive().GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConPasswordCorta_DevuelveFailureDeValidacion()
    {
        var request = RequestValido() with { Password = "1234567" };

        var result = await _useCase.ExecuteAsync(request);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("al menos 8 caracteres");
    }

    [Fact]
    public async Task ExecuteAsync_ConPlanDePruebaConfigurado_IniciaLaSuscripcionEnPrueba()
    {
        var planDePrueba = Plan.Crear("Prueba", 0m, Periodicidad.Mensual, 1, 50);
        planDePrueba.MarcarComoPlanDePrueba();
        _planRepository.GetPlanDePruebaAsync().Returns(planDePrueba);

        var result = await _useCase.ExecuteAsync(RequestValido());

        result.IsSuccess.Should().BeTrue();
        await _suscripcionRepository.Received(1).AddAsync(
            Arg.Is<Suscripcion>(s => s!.PlanId == planDePrueba.Id && s.Estado == EstadoSuscripcion.EnPrueba),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SinPlanDePruebaConfigurado_NoCreaSuscripcionNiFalla()
    {
        var result = await _useCase.ExecuteAsync(RequestValido());

        result.IsSuccess.Should().BeTrue();
        await _suscripcionRepository.DidNotReceive().AddAsync(Arg.Any<Suscripcion>(), Arg.Any<CancellationToken>());
    }
}
