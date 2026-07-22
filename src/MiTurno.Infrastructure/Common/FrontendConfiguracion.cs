using Microsoft.Extensions.Options;
using MiTurno.Application.Common.Interfaces;

namespace MiTurno.Infrastructure.Common;

public class FrontendConfiguracion : IFrontendConfiguracion
{
    private readonly FrontendSettings _settings;

    public FrontendConfiguracion(IOptions<FrontendSettings> options)
    {
        _settings = options.Value;
    }

    public string BaseUrl => _settings.BaseUrl;
}
