namespace MiTurno.Application.Features.Admin.Auth.Dtos;

public record LoginSysAdminResponse(Guid SysAdminId, string Nombre, string Email, string Token);
