using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiTurno.Application.Common.Interfaces;
using MiTurno.Application.Common.Models;

namespace MiTurno.Infrastructure.Notifications;

public class SmtpEmailNotificador : IEmailNotificador
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailNotificador> _logger;

    public SmtpEmailNotificador(IOptions<SmtpSettings> options, ILogger<SmtpEmailNotificador> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public Task NotificarReservaConfirmadaAsync(NotificacionReserva n, CancellationToken cancellationToken = default) =>
        EnviarAsync(
            n.ClienteEmail,
            $"Tu turno en {n.NegocioNombre} fue confirmado",
            $"Hola {n.ClienteNombre},\n\n" +
            $"Tu turno en {n.RecursoNombre} ({n.NegocioNombre}) para el {FormatearFecha(n)} quedó confirmado. ¡Te esperamos!",
            cancellationToken);

    public Task NotificarReservaRechazadaAsync(NotificacionReserva n, CancellationToken cancellationToken = default) =>
        EnviarAsync(
            n.ClienteEmail,
            $"No pudimos confirmar tu turno en {n.NegocioNombre}",
            $"Hola {n.ClienteNombre},\n\n" +
            $"Tu pago para el turno en {n.RecursoNombre} ({n.NegocioNombre}) del {FormatearFecha(n)} no pudo procesarse, " +
            "por lo que la reserva quedó cancelada y el horario volvió a estar disponible. Podés intentar reservarlo de nuevo.",
            cancellationToken);

    public Task NotificarReservaCanceladaAsync(NotificacionReserva n, CancellationToken cancellationToken = default) =>
        EnviarAsync(
            n.ClienteEmail,
            $"Tu turno en {n.NegocioNombre} fue cancelado",
            $"Hola {n.ClienteNombre},\n\n" +
            $"{n.NegocioNombre} canceló tu turno en {n.RecursoNombre} para el {FormatearFecha(n)}. " +
            "Si tenías un pago realizado, el negocio se pondrá en contacto para el reembolso.",
            cancellationToken);

    private static string FormatearFecha(NotificacionReserva n) =>
        $"{n.Fecha:dd/MM/yyyy} de {n.HoraInicio:hh\\:mm} a {n.HoraFin:hh\\:mm}";

    // Envío best-effort: si el SMTP no está configurado (entornos de desarrollo) o falla el envío,
    // se registra en el log en lugar de propagar la excepción, para no malograr una operación de
    // negocio (confirmar/rechazar/cancelar) que ya se persistió correctamente.
    private async Task EnviarAsync(string destinatario, string asunto, string cuerpo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            _logger.LogInformation(
                "SMTP no configurado: se omite el envío. Para: {Destinatario} | Asunto: {Asunto}\n{Cuerpo}",
                destinatario, asunto, cuerpo);
            return;
        }

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = asunto,
                Body = cuerpo,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            mensaje.To.Add(destinatario);

            using var cliente = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.User, _settings.Password)
            };

            await cliente.SendMailAsync(mensaje, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el email a {Destinatario} con asunto '{Asunto}'.", destinatario, asunto);
        }
    }
}
