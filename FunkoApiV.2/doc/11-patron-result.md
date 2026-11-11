# 11 — Patrón Result y errores de dominio

## Uso actual

Los servicios devuelven resultados `CSharpFunctionalExtensions` y errores de
dominio definidos en `FunkoApiV.2/Error/DomainError.cs` y
`DomainErrors.cs`. Los tipos incluyen `NotFound`, `ValidationError`, `Conflict`,
`BusinessRuleError`, `InternalError` y `UnauthorizedError`.

Los controladores convierten errores de dominio a HTTP mediante
`DomainErrorExtensions.TopHttpResult<T>()`:

| Error | HTTP |
|---|---:|
| `NotFound` | 404 |
| `ValidationError` | 400 |
| `Conflict` | 409 |
| `BusinessRuleError` | 422 |
| `UnauthorizedError` | 401 |
| Otro error | 500 |

El cuerpo REST usa `ApiErrorResponse`. No uses resultados de éxito como
fallback de errores ni captures excepciones sin registrarlas. Errores de
infraestructura inesperados deben llegar al manejo global de excepciones.

Al añadir una clase de error, actualiza el mapeo HTTP y las pruebas relacionadas.
