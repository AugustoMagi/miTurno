namespace MiTurno.Infrastructure.Notifications;

public class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "MiTurno";
    public bool EnableSsl { get; set; } = true;
}
