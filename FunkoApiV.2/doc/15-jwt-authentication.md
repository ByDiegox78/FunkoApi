# 15 — Autenticación JWT

## Inicio de sesión

La autenticación está en `Controller/Auth/AuthController.cs` y
`Service/Auth/AuthService.cs`. En Development, inicia sesión mediante
`POST /api/auth/login` con email y contraseña para obtener un JWT. La cuenta de
ejemplo ADMIN se crea desde `Seeders/IdentitySeeder.cs`; consulta
[la guía práctica](00-guia-practica.md). No uses las credenciales de ejemplo en
producción.

Envía el token a los endpoints protegidos con:

```text
Authorization: Bearer <JWT>
```

Swagger tiene una opción Bearer para autorizar peticiones autenticadas. En
GraphQL, introduce el mismo header en el panel **Headers**.

## Configuración

`Infrastructure/IdentityConfig.cs` registra Identity, autenticación bearer,
validación del token y políticas de roles. `Config/JwtSettings.cs` define el
modelo de configuración. Development dispone de una clave local en
`appsettings.Development.json`; en Production configura una clave privada
mediante variable de entorno/secreto, no en JSON compartido.

## Registro y usuarios Production

El endpoint de registro crea un usuario con rol `USER`; no permite que el
solicitante se asigne `ADMIN`. Production no borra usuarios: conserva los
existentes, garantiza las definiciones de roles y crea el admin de
`BootstrapAdmin__*` solo si no existe. Aprovisiona esa cuenta de forma segura
antes de habilitar operaciones que la requieran.

Ver también [autorización por roles](16-autorizacion-roles.md) y
[seguridad HTTP](17-seguridad-http.md).
