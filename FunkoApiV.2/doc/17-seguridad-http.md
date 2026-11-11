# 17 — Seguridad HTTP, documentación e índices

## CORS

La política se configura en `Infrastructure/CorsConfig.cs`. Los orígenes de
`Cors:AllowedOrigins` se usan en todos los entornos; además, solo en Development
se añaden orígenes locales predeterminados (puertos 5500, 5798 y 63342). En
Production se aplican exclusivamente los orígenes configurados, que llegan por
`Cors__AllowedOrigins__0`, `__1`, etc. Configura los orígenes reales:

```text
Cors__AllowedOrigins__0=https://tienda.ejemplo.com
```

No uses `*` como sustituto de una lista de orígenes. SignalR requiere
credenciales de conexión; revisa la política antes de cambiar CORS.

## Límite de peticiones

`Program.cs` configura un límite global por dirección IP, por defecto 120
peticiones por 60 segundos. Se puede ajustar con
`RateLimiting__PermitLimit` y `RateLimiting__WindowSeconds`. Una petición
rechazada recibe HTTP 429 y `Retry-After`.

Si la API está detrás de un proxy inverso, no confíes automáticamente en
cabeceras de IP reenviadas: configura únicamente proxies conocidos antes de
utilizar la IP del cliente para limitar.

## Cabeceras de seguridad

Fuera de Development, el middleware de `Program.cs` añade CSP,
`X-Content-Type-Options`, `X-Frame-Options` y `Referrer-Policy`. La CSP actual
restringe recursos a `'self'`; si se sirve documentación visual en otro entorno,
revisa la política sin debilitarla innecesariamente.

## Swagger y GraphQL

- Swagger UI: `/swagger`, solo en Development.
- Documento OpenAPI: `/openapi/v1.json`, solo en Development.
- GraphQL HTTP: `/graphql`, mapeado en ambos entornos.
- Explorador visual GraphQL: habilitado únicamente en Development.
- GraphQL subscriptions: WebSocket en `/graphql`.
- SignalR: `/hubs/funkos`, requiere autenticación.

## Índices y consultas

`Infrastructure/PostgresSchemaInitializer.cs` crea la extensión `pg_trgm` e
índices para IDs relacionados, precio, stock, nombre, fecha y búsquedas por
trigramas sobre nombres activos. La base debe permitir crear la extensión y los
índices en una inicialización nueva. Los índices parciales excluyen registros
eliminados.

Las consultas paginadas de REST y GraphQL comparten `FunkoRepository.GetPagedAsync`:
`CountAsync` + `Skip`/`Take` y `AsNoTracking`. No hay listados que carguen la
tabla completa. Antes de añadir índices, comprueba los filtros y ordenaciones
existentes y revisa el plan de ejecución con datos representativos. Evita
índices redundantes y cambios de esquema no compatibles con instalaciones que ya
tienen tablas.

## Estado de capacidades solicitadas

- CORS por entorno, rate limiting, cabeceras CSP/seguridad, Swagger, GraphQL y
  SignalR: configurados en el código actual.
- Seeds: solo Development. Production no borra datos y solo crea el admin si no
  existe.
- Redis: caché de aplicación. **Output Cache HTTP no está implementado**; es una
  mejora opcional.
- Emails: el registro intenta enviar un correo de bienvenida sin plantillas.
  SMTP se configura con `Smtp__Host`, `Smtp__Port`, `Smtp__Security`,
  `Smtp__From`, `Smtp__Username` y `Smtp__Password`. Mailpit se usa en
  Development.
- Producción: se arranca con `.\Start.ps1 -Production`; no hay limpieza de datos
  al iniciar. Consulta [Docker](19-docker.md).
