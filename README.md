# MiTurno

## Descripción

MiTurno es una plataforma SaaS que simplifica la reserva de turnos para canchas deportivas y otros negocios que trabajan con horarios.

Actualmente, la mayoría de las reservas se realizan enviando un mensaje por WhatsApp al dueño del establecimiento. Este proceso genera demoras, errores, doble reserva de horarios y obliga al cliente a esperar una respuesta.

Algunos establecimientos utilizan aplicaciones móviles propias, pero obligan al usuario a descargar una aplicación para realizar una única reserva, lo que agrega fricción y disminuye la cantidad de clientes que completan el proceso.

MiTurno busca eliminar esas barreras.

El usuario simplemente ingresa al enlace del negocio (por ejemplo, desde su perfil de Instagram), visualiza los horarios disponibles en tiempo real, selecciona el turno que desea, realiza el pago y recibe la confirmación automáticamente. Todo el proceso puede completarse en menos de un minuto y sin necesidad de instalar ninguna aplicación.

## Objetivos

* Reservar un turno en pocos pasos.
* Evitar conversaciones innecesarias por WhatsApp.
* Mostrar disponibilidad en tiempo real.
* Confirmar automáticamente las reservas una vez realizado el pago.
* Reducir las dobles reservas.
* Facilitar la administración del negocio mediante un panel web.

## Tecnologías

### Backend

* .NET 10
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* JWT Authentication
* Clean Architecture

### Frontend

* React
* TypeScript
* React Router
* Tailwind CSS (o Bootstrap)
* Axios

### Infraestructura

* Docker
* GitHub
* Claude Code como asistente de desarrollo
* Azure o Render para el despliegue
* Cloudflare para dominio y SSL

## Flujo del usuario

1. El usuario ingresa al enlace del negocio.
2. Visualiza las canchas o servicios disponibles.
3. Selecciona la fecha.
4. Elige un horario libre.
5. Ingresa sus datos.
6. Realiza el pago.
7. Recibe la confirmación del turno.

## Funcionalidades principales

### Para el cliente

* Reserva online.
* Pago integrado.
* Confirmación automática.
* Historial de reservas.
* Cancelación según políticas del establecimiento.

### Para el administrador

* Gestión de canchas o recursos.
* Configuración de horarios.
* Bloqueo de fechas.
* Administración de precios.
* Gestión de empleados.
* Estadísticas de ocupación.
* Gestión de clientes.
* Configuración de métodos de pago.

## Características del producto

* Responsive.
* Sin necesidad de instalar aplicaciones.
* Multiempresa (cada negocio tiene su propio espacio).
* Fácil de compartir mediante un simple enlace.
* Preparado para escalar a múltiples tipos de negocios.

## Propuesta de valor

Reservar un turno debería ser tan simple como comprar un producto online.

MiTurno elimina los mensajes de WhatsApp, automatiza la disponibilidad y permite que cualquier cliente reserve y pague un turno en segundos desde cualquier dispositivo.
