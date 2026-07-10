# MiTurno

## Descripción

MiTurno es un SaaS para gestionar reservas de turnos.

La primera versión está enfocada en canchas deportivas.

El objetivo es que un usuario pueda ingresar desde un enlace (por ejemplo el link de Instagram de una cancha), seleccionar un horario disponible, pagar y confirmar automáticamente su reserva.

## Stack

Backend
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Clean Architecture
- JWT

Frontend
- React
- TypeScript
- React Router
- Tailwind CSS

## Arquitectura

Backend

Presentation
Application
Domain
Infrastructure

No utilizar lógica de negocio dentro de Controllers.

Los Controllers únicamente llaman a los casos de uso.

Toda la lógica pertenece a Application.

Infrastructure implementa repositorios.

Domain no depende de ningún otro proyecto.

## Convenciones

Siempre utilizar:

- async/await
- DTOs
- Repository Pattern
- Dependency Injection
- FluentValidation
- Result Pattern

Nunca devolver entidades directamente.

Siempre devolver DTOs.

Todo código debe estar documentado cuando sea complejo.

## Estilo

Priorizar código limpio.

Evitar duplicación.

Aplicar SOLID.

Aplicar Clean Architecture.

Si una decisión rompe la arquitectura, proponer una alternativa.

Cada vez que escribas código:

✔ explicar qué hace

✔ explicar por qué

✔ mencionar posibles problemas

✔ indicar cómo probarlo
Analizá todo el proyecto.

Explicame la arquitectura.

Encontrá código duplicado.

Detectá code smells.

Proponé mejoras.