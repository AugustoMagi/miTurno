namespace MiTurno.Application.Features.Negocios.Dtos;

/// <summary>
/// Solo los datos que Negocio.ActualizarDatos permite tocar: el Email es la credencial de contacto
/// registrada y el Slug es el link público ya compartido, ninguno se edita desde acá.
/// </summary>
public record ActualizarMiNegocioRequest(string Nombre, string? Descripcion, string? Direccion, string? Telefono);
