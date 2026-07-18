using Microsoft.Extensions.Options;
using MiTurno.Application.Common.Interfaces;

namespace MiTurno.Infrastructure.Pagos;

public class PlataformaPagoConfiguracion : IPlataformaPagoConfiguracion
{
    private readonly MercadoPagoPlataformaSettings _settings;

    public PlataformaPagoConfiguracion(IOptions<MercadoPagoPlataformaSettings> options)
    {
        _settings = options.Value;
    }

    public string AccessToken => _settings.AccessToken;
}
