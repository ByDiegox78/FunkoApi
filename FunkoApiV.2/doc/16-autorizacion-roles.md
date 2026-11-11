# 16 — Autorización por roles

## Roles actuales

Identity usa los nombres exactos `ADMIN` y `USER` (en mayúsculas). Las
políticas `RequireAdmin` y `RequireUser` se registran en
`Infrastructure/IdentityConfig.cs`. Los controladores también pueden declarar
directamente `[Authorize(Roles = "ADMIN")]`.

## Regla de acceso

- Lecturas de colecciones de Funkos/categorías: públicas.
- Proveedores: todas las acciones del controlador requieren `ADMIN`, también
  listados y lecturas individuales.
- Lectura de Funko por ID: requiere `ADMIN`.
- Escrituras de Funkos y categorías y todas las acciones de proveedores:
  requieren `ADMIN`.
- Hub SignalR `/hubs/funkos`: requiere usuario autenticado.
- Consultas GraphQL de lectura: públicas; mutaciones protegidas según el resolver.

Los atributos de cada endpoint y las políticas del resolver son la fuente de
verdad. No presupongas que todos los GET son públicos.

## Respuestas

- Sin token válido: `401 Unauthorized`.
- Token válido sin el rol exigido: `403 Forbidden`.

El registro asigna `USER`. En Production, el arranque elimina usuarios y no hay
un ADMIN de ejemplo; planifica su provisión segura.
