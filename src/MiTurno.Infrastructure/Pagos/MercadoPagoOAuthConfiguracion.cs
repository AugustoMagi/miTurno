using Microsoft.Extensions.Options;
using MiTurno.Application.Common.Interfaces;

namespace MiTurno.Infrastructure.Pagos;

public class MercadoPagoOAuthConfiguracion : IMercadoPagoOAuthConfiguracion
{
    private readonly MercadoPagoOAuthSettings _settings;

    public MercadoPagoOAuthConfiguracion(IOptions<MercadoPagoOAuthSettings> options)
    {
        _settings = options.Value;
    }

    public string ClientId => _settings.ClientId;
    public string ClientSecret => _settings.ClientSecret;
    public string RedirectUri => _settings.RedirectUri;
}
