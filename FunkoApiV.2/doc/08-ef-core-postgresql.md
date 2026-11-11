# 08 — EF Core y PostgreSQL

## Qué almacena PostgreSQL

PostgreSQL contiene Funkos, categorías y tablas de ASP.NET Core Identity. El
contexto está en `FunkoApiV.2/Entity/AppDbContext.cs`; la conexión se obtiene de
`ConnectionStrings:Postgre` mediante `FunkoConfig`.

## Inicialización actual

En cada inicio, `Program.cs` ejecuta `EnsureCreatedAsync` y
`PostgresSchemaInitializer.EnsureSchemaAsync`. El inicializador crea tablas,
extensión `pg_trgm` e índices si no existen. No se aplica aquí el flujo habitual
de `dotnet ef migrations`; `CREATE TABLE IF NOT EXISTS` tampoco actualiza una
tabla antigua que ya exista con una definición incompatible.

Los índices actuales cubren claves relacionadas, filtros/ordenaciones de
Funkos y búsquedas por texto mediante trigramas. El usuario PostgreSQL necesita
permisos para crear los objetos/extensión requeridos en una instalación nueva.

## Cambio de esquema

Antes de cambiar una entidad:

1. Revisa `AppDbContext` y `PostgresSchemaInitializer`.
2. Considera tanto una base vacía como una que ya tenga tablas.
3. Actualiza el esquema de forma compatible con la estrategia actual; no asumas
   que una migración se ejecutará automáticamente.
4. Añade o actualiza tests de modelo/repositorio.
5. Si un esquema persistente no coincide, planifica su migración de manera
   controlada; no borres volúmenes como método de actualización rutinario.

En Production no se borran datos al arrancar: solo se aplica el esquema y se
aprovisiona el admin si no existe. Lee [Docker](19-docker.md) antes de iniciar
ese entorno.
