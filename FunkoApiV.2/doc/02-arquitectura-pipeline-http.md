# 02 — Arquitectura y pipeline HTTP

## Arquitectura actual

La API separa controladores, servicios, repositorios e infraestructura. Los
controladores traducen HTTP; los servicios aplican validaciones y reglas de
negocio; los repositorios trabajan con PostgreSQL, MongoDB o Cassandra. Redis
se integra mediante servicios decoradores de caché.

Código principal:

- `Controller/`: endpoints REST.
- `Service/`: reglas de negocio y decoradores.
- `Repository/`: acceso a datos.
- `Infrastructure/`: composición de servicios, seguridad, esquemas e
  inicialización.
- `Program.cs`: registro de dependencias, inicialización de datos y pipeline.

## Inicio de la aplicación

`Program.Main` configura controladores y errores de modelo, Identity/JWT,
validadores, GraphQL, SignalR, CORS, Swagger, rate limiting, contextos de datos,
Cassandra, Redis y dependencias Scrutor. Al construir la aplicación:

1. Swagger UI se activa solo en Development.
2. Inicializa el esquema PostgreSQL/Cassandra.
3. En Development ejecuta los seeders; en Production solo garantiza los roles y
   aprovisiona el admin configurado si no existe. No borra datos.
4. Configura cabeceras de seguridad fuera de Development.
5. Habilita redirección HTTPS, archivos estáticos, CORS, rate limiting,
   autenticación, autorización, WebSockets, controladores, SignalR y GraphQL.

El orden y las condiciones exactas viven en `FunkoApiV.2/Program.cs`; este
documento no debe usarse como sustituto del código.

## Errores HTTP

Los errores de negocio usan `DomainError` y `DomainErrorExtensions`. Los errores
de validación automática de `[ApiController]` se transforman en `ApiErrorResponse`
con mensaje y errores por campo. Una excepción no controlada se procesa por el
middleware global registrado en `Program.cs`.

## Entorno de trabajo

Arranca dependencias y API con `.\Start.ps1` desde la raíz. Para pasos y
direcciones, consulta [la guía práctica](00-guia-practica.md).
