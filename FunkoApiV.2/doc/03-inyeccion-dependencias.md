# 03 — Inyección de dependencias

## Composición actual

El registro está repartido entre `Program.cs` y métodos de extensión de
`Infrastructure/`:

| Extensión / archivo | Responsabilidad |
|---|---|
| `IdentityConfig` | Identity, JWT y políticas `RequireAdmin` / `RequireUser` |
| `GraphQLConfig` | esquema GraphQL y endpoint visual condicionado por entorno |
| `SignalRConfig` | SignalR |
| `CorsConfig` | política CORS |
| `CassandraConfig` | sesión y servicios Cassandra |
| `DependencerProvider.AddDependencies` | descubrimiento de servicios/repositorios y decoradores |

`AddDependencies` usa Scrutor para escanear las clases marcadas con
`ITransientService` e `IScopedService`, y registra los decoradores
`CachedFunkoService`, `CachedCategoriaService` y `CachedProveedorService`.
Redis se registra explícitamente desde `Program.cs`.

## Ciclos de vida

- `Transient`: tipos marcados con `ITransientService`.
- `Scoped`: tipos marcados con `IScopedService`.
- Redis: `ICacheService` se registra como singleton.

Al añadir una clase, comprueba qué interfaz marcador implementa y si necesita
decorador; no añadas registros duplicados sin revisar el escaneo existente.

## Comprobar la configuración

La fuente de verdad es `FunkoApiV.2/Program.cs` y las extensiones
`FunkoApiV.2/Infrastructure/*.cs`. Los nombres de ejemplo antiguos como
`DatabaseConfig` o `ServicesConfig` no corresponden necesariamente a clases
actuales.
