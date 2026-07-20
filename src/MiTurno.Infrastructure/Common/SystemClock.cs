using MiTurno.Application.Common.Interfaces;

namespace MiTurno.Infrastructure.Common;

public class SystemClock : IClock
{
    private static readonly TimeSpan ArgentinaOffset = TimeSpan.FromHours(-3);

    public DateTime Now => DateTime.UtcNow + ArgentinaOffset;
}
