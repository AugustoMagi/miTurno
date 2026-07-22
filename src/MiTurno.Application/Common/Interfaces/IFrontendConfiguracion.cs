namespace MiTurno.Application.Common.Interfaces;

/// <summary>URL base del frontend (sin barra final), para armar links que un email o una redirección le mandan de vuelta al usuario (ej. reseteo de contraseña, callback OAuth de Mercado Pago).</summary>
public interface IFrontendConfiguracion
{
    string BaseUrl { get; }
}
