# FunkoApi

API para gestionar Funkos, categorías, proveedores y usuarios, desarrollada con
ASP.NET Core/.NET 10. Combina REST, GraphQL y SignalR con PostgreSQL, MongoDB,
Cassandra y Redis. Esta página resume el proyecto; el recorrido completo está en
la [guía práctica](doc/00-guia-practica.md) y el
[guion de presentación](doc/22-presentacion.md).

## Arranque rápido

Los comandos se ejecutan **desde la raíz del repositorio**, donde está
`Start.ps1` (un nivel por encima de esta carpeta).

```powershell
.\Start.ps1              # Desarrollo: bases + API en http://localhost:5798
.\Start.ps1 -Bruno       # Desarrollo + colección Bruno
.\Start.ps1 -Production  # Producción: bases + backend en http://localhost:5197
```

| Comando | Uso |
|---|---|
| `.\Start.ps1` | Desarrollo en 5798 |
| `.\Start.ps1 -Port 5800` | Desarrollo en otro puerto |
| `.\Start.ps1 -ResetData` | Borra volúmenes y vuelve a sembrar |
| `.\Start.ps1 -Bruno` | Flujo completo de desarrollo + Bruno |
| `.\Start.ps1 -Down` | Detiene los contenedores de desarrollo sin borrar datos |
| `.\Start.ps1 -Production` | Producción en 5197 |
| `.\Start.ps1 -Production -Build` | Reconstruye la imagen |
| `.\Start.ps1 -Production -Logs` | Sigue los logs del backend |
| `.\Start.ps1 -Production -Down` | Detiene los contenedores de producción sin borrar datos |
| `.\Start.ps1 -Production -InitEnv` | Genera `.env.production` con secretos |
| `.\Test-All.ps1` | `dotnet test` + Bruno |

Cuando la API esté escuchando:

- Swagger: <http://localhost:5798/swagger> (solo Development)
- GraphQL: <http://localhost:5798/graphql> (explorador solo Development)
- Demo SignalR: <http://localhost:5798/> (requiere cuenta ADMIN)
- Mailpit: <http://localhost:8025>

## Pruebas y cobertura

Desde la raíz del repositorio:

```powershell
.\Start.ps1 -Bruno            # solo Bruno: bases + API + colección

.\Test-All.ps1                # todos los tests .NET + Bruno

dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj `
  --settings .\FunkoApiV.2\coverage.runsettings `
  --collect:"XPlat Code Coverage"

reportgenerator `
  -reports:".\FunkoApiV.2\FunkoApiV2Test\TestResults\**\coverage.cobertura.xml" `
  -targetdir:".\FunkoApiV.2\coveragereport" `
  -reporttypes:"Html;TextSummary"
```

El informe queda en `FunkoApiV.2\coveragereport\index.html`. El paso a paso,
los filtros por tipo de test y los requisitos están en
[la guía de pruebas .NET](doc/18-testing.md) y en
[la guía de Bruno](doc/20-bruno-testing.md).

## Qué incluye el proyecto

- Arquitectura por capas: controladores, servicios, repositorios, DTOs,
  validación y patrón `Result`.
- REST para Funkos, categorías, proveedores y autenticación.
- GraphQL con consultas paginadas y filtradas, mutaciones, cargas y
  suscripciones. El explorador visual solo se habilita en Development.
- SignalR en el hub autenticado `/hubs/funkos`.
- PostgreSQL para Funkos, categorías e Identity; MongoDB para proveedores;
  Cassandra para consultas agregadas; Redis como caché de aplicación.
- Correo de bienvenida al registrar usuarios, sin plantillas: asunto y cuerpo se
  construyen en código. En Development se captura con Mailpit.
- JWT, roles, borrado lógico, rate limiting (120 peticiones/minuto por IP),
  CORS por entorno y cabeceras CSP/seguridad en Production.
- Índices PostgreSQL para filtros, ordenaciones y búsquedas por trigramas.
- 286 tests .NET y una colección Bruno de 34 peticiones y 80 comprobaciones.
- Docker Compose con un único comando de arranque por entorno.

**Output Cache HTTP no está implementado.** Redis se usa como caché de
aplicación (lecturas por id); no es lo mismo que Output Cache.

## Entornos y datos

- **Development:** aplica el esquema y carga datos de ejemplo en PostgreSQL,
  MongoDB, Cassandra e Identity. Los seeders son idempotentes: reiniciar no
  duplica ni borra. Crea las cuentas `admin@admin.com` / `Admin123!` y
  `user@user.com` / `User123!`.
- **Production:** aplica el esquema y solo aprovisiona el administrador
  configurado en `BootstrapAdmin__*` si no existe. **No borra datos** y no carga
  datos de ejemplo. Usa `.env.production` con secretos propios.

Diferencias prácticas:

| Aspecto | Development | Production |
|---|---|---|
| Datos de ejemplo | Sí | No |
| Borrado al arrancar | No | No |
| Swagger / explorador GraphQL | Sí | 404 |
| `POST /graphql` | Sí | Sí |
| CORS | Orígenes locales + configuración | Solo configuración |
| Correo | Mailpit | SMTP real |
| Detalle de excepciones | Sí | No |

El primer arranque de Production necesita `.\Start.ps1 -Production -InitEnv`
para generar `.env.production`. El script valida las credenciales de PostgreSQL
antes de arrancar el backend y nunca borra volúmenes.

## Estructura

```text
FunkoApi/
├── Start.ps1              # Comando único: desarrollo, Bruno o producción
├── FunkoApiV.2/
│   ├── FunkoApiV.2/       # API, controladores, servicios, repositorios e infraestructura
│   ├── FunkoApiV2Test/    # Tests unitarios y de integración
│   ├── Bruno/             # Colección de pruebas HTTP
│   ├── doc/               # Guías operativas y apuntes técnicos
│   ├── Start-Development.ps1
│   ├── Start-Production.ps1
│   ├── Run-Bruno.ps1
│   ├── Test-All.ps1
│   └── README.md
└── README.md
```

## Documentación

- [00 — Guía práctica: scripts, entornos, pruebas y problemas](doc/00-guia-practica.md)
- [22 — Guion de presentación y demo](doc/22-presentacion.md)
- [17 — Seguridad HTTP, Swagger, GraphQL e índices](doc/17-seguridad-http.md)
- [18 — Tests .NET y cobertura](doc/18-testing.md)
- [19 — Docker, desarrollo y producción](doc/19-docker.md)
- [20 — Bruno](doc/20-bruno-testing.md)
- [21 — Ejemplos GraphQL](doc/21-GRAPHQL-EJEMPLOS.md)

## Tecnología y versiones

Los `PackageReference` y el framework objetivo están en
`FunkoApiV.2/FunkoApiV.2.csproj` y
`FunkoApiV2Test/FunkoApiV2Test.csproj`. Consulta esos archivos como fuente de
verdad.

## Atribución

El proyecto utiliza como referencia material de José Luis González Sánchez
(`joseluisgs`) y sus repositorios docentes, citados en los apuntes
correspondientes. Respeta las licencias y atribuciones del material original si
reutilizas contenido.
