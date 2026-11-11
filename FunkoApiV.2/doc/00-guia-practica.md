# 00 — Guía práctica: arrancar, probar y desplegar

Esta es la guía operativa principal. Los comandos están escritos para
PowerShell en Windows y se ejecutan **desde la raíz del repositorio**, donde
está `Start.ps1`. Para exponer el proyecto en clase, usa el
[guion de presentación](22-presentacion.md).

## 1. Scripts disponibles

Todos los scripts se lanzan desde la raíz. `Start.ps1` es el comando único y
delega en los demás según el modo.

| Comando | Qué hace |
|---|---|
| `.\Start.ps1` | Desarrollo: bases de datos + API en `http://localhost:5798` |
| `.\Start.ps1 -Port 5800` | Desarrollo en otro puerto |
| `.\Start.ps1 -ResetData` | Borra los volúmenes de desarrollo y vuelve a sembrar |
| `.\Start.ps1 -Bruno` | Desarrollo + ejecución completa de la colección Bruno |
| `.\Start.ps1 -Production` | Producción: bases + backend en `http://localhost:5197` |
| `.\Start.ps1 -Production -Build` | Reconstruye la imagen antes de arrancar |
| `.\Start.ps1 -Production -Logs` | Sigue los logs del backend |
| `.\Start.ps1 -Down` | Detiene los contenedores de desarrollo sin borrar datos |
| `.\Start.ps1 -Production -Down` | Detiene los contenedores de producción sin borrar datos |
| `.\Start.ps1 -Production -InitEnv` | Crea `.env.production` con secretos aleatorios |
| `.\Test-All.ps1` | `dotnet test` + colección Bruno |

`Start.ps1` rechaza combinaciones incoherentes, como `-Production -Bruno`,
`-Production -ResetData` o `-Down -ResetData`: producción nunca borra datos y
parar contenedores tampoco.

## 2. Primera vez en este equipo

Necesitas:

- SDK de .NET 10.
- Docker Desktop iniciado.
- Bruno CLI solo si vas a lanzar la colección Bruno (`bru --version`). Si no
  está instalado, `Test-All.ps1` y `Run-Bruno.ps1` intentan usar
  `npx --yes @usebruno/cli`.

Comprueba Docker y arranca:

```powershell
docker info
.\Start.ps1
```

En el primer arranque, `Start-Development.ps1` crea `FunkoApiV.2\FunkoApiV.2\.env`
copiando `.env.example`. Ese fichero contiene valores locales de ejemplo, no
secretos de producción. No lo subas al repositorio.

## 3. Trabajo diario en desarrollo

```powershell
.\Start.ps1
```

El script:

1. Comprueba que Docker responde.
2. Crea `.env` desde `.env.example` si falta.
3. Levanta PostgreSQL, MongoDB, Cassandra, Redis y Mailpit.
4. Arranca la API con `ASPNETCORE_ENVIRONMENT=Development` en el puerto 5798.

La API queda en primer plano: no cierres esa terminal. Para detenerla, pulsa
`Ctrl+C`; los contenedores de datos siguen levantados.

Cuando aparezca `Now listening on: http://localhost:5798`:

| Para... | Dirección |
|---|---|
| Swagger / REST | <http://localhost:5798/swagger> |
| Explorador visual de GraphQL | <http://localhost:5798/graphql> |
| Consultar funkos directamente | <http://localhost:5798/api/funkos?page=0&size=10> |
| Cliente de demostración SignalR | <http://localhost:5798/> |
| Correos capturados (Mailpit) | <http://localhost:8025> |

El hub SignalR (`/hubs/funkos`) requiere un cliente SignalR autenticado, no se
abre como una página normal. En Development, la raíz sirve la demo.

### Cambiar el puerto

```powershell
.\Start.ps1 -Port 5800
```

Usa la misma URL para Swagger, GraphQL y Bruno. Para la suite completa:

```powershell
.\Test-All.ps1 -BaseUrl http://localhost:5800
```

### Volver a sembrar los datos

```powershell
.\Start.ps1 -ResetData
```

Pide confirmación, borra los volúmenes de desarrollo y deja que los seeders
vuelvan a insertar los datos de ejemplo.

## 4. Cuentas de desarrollo

En Development, `IdentitySeeder.cs` crea estas cuentas si todavía no existen:

| Rol | Email | Contraseña inicial |
|---|---|---|
| ADMIN | `admin@admin.com` | `Admin123!` |
| USER | `user@user.com` | `User123!` |

Son credenciales locales. Si cambias la contraseña de una cuenta ya creada, el
seeder no la restablece. Para obtener un JWT, envía `POST /api/auth/login` y
usa el token como `Authorization: Bearer <token>`.

## 5. Probar GraphQL

1. Arranca la API en Development.
2. Abre <http://localhost:5798/graphql>.
3. Ejecuta una consulta de [los ejemplos](21-GRAPHQL-EJEMPLOS.md).
4. Para una mutación, inicia sesión y añade en **Headers**:
   `Authorization: Bearer <JWT>`.

`funkos` está paginada y admite filtros, igual que la API REST:

```graphql
query {
  funkos(page: 1, size: 10, categoriaId: "VQGCKyvzIM4") {
    items { id name precio stock categoria }
    totalCount
    page
    pageSize
    hasNextPage
  }
}
```

Las mutaciones requieren rol `ADMIN`. El explorador visual y Swagger solo se
habilitan en Development; el endpoint `POST /graphql` sigue funcionando en
Production. Las suscripciones usan WebSocket en `/graphql` y SignalR usa
`/hubs/funkos`: son protocolos distintos.

## 6. Ejecutar pruebas

Todos los comandos de esta sección se ejecutan desde la raíz del repositorio,
donde está `Start.ps1`.

| Quiero... | Comando |
|---|---|
| Todos los tests .NET | `dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj` |
| Solo unitarios | `dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj --filter "FullyQualifiedName~Unit"` |
| Solo integración | `dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj --filter "FullyQualifiedName~Integration"` |
| .NET con cobertura | `dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj --settings .\FunkoApiV.2\coverage.runsettings --collect:"XPlat Code Coverage"` |
| .NET + Bruno | `.\Test-All.ps1` |
| Solo Bruno | `.\Start.ps1 -Bruno` |
| Informe HTML de cobertura | `reportgenerator -reports:".\FunkoApiV.2\FunkoApiV2Test\TestResults\**\coverage.cobertura.xml" -targetdir:".\FunkoApiV.2\coveragereport" -reporttypes:"Html;TextSummary"` |

### Suite completa (.NET + Bruno)

Deja Docker Desktop iniciado. Desde la raíz:

```powershell
.\Test-All.ps1
```

Ejecuta `dotnet test` (incluidas las pruebas de integración con Testcontainers)
y después toda la colección Bruno. Si faltan credenciales ADMIN, las pide sin
guardarlas en el repositorio.

### Solo pruebas .NET

```powershell
dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj
```

Solo unitarias:

```powershell
dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj --filter "FullyQualifiedName~Unit"
```

Las de integración crean sus propios contenedores y necesitan Docker.

### Cobertura de código

Los tests .NET pueden medir cobertura sin cambiar el proyecto. Genera el XML:

```powershell
dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj `
  --settings .\FunkoApiV.2\coverage.runsettings `
  --collect:"XPlat Code Coverage"
```

Cada ejecución deja `coverage.cobertura.xml` en una carpeta nueva de
`FunkoApiV.2\FunkoApiV2Test\TestResults\`. Después genera el informe HTML:

```powershell
reportgenerator `
  -reports:".\FunkoApiV.2\FunkoApiV2Test\TestResults\**\coverage.cobertura.xml" `
  -targetdir:".\FunkoApiV.2\coveragereport" `
  -reporttypes:"Html;TextSummary"
```

Abre `FunkoApiV.2\coveragereport\index.html`. Si `reportgenerator` no existe en
el equipo, instálalo una vez:

```powershell
dotnet tool install --global dotnet-reportgenerator-globaltool
```

Si quieres cobertura y Bruno en la misma sesión, ejecuta el comando de cobertura,
luego `.\Start.ps1 -Bruno` y, al terminar, `reportgenerator`. El detalle está en
[Pruebas .NET y cobertura](18-testing.md).

### Solo Bruno

```powershell
$env:FUNKO_ADMIN_EMAIL = "admin@admin.com"
$env:FUNKO_ADMIN_PASSWORD = "Admin123!"
.\Start.ps1 -Bruno
Remove-Item Env:FUNKO_ADMIN_EMAIL, Env:FUNKO_ADMIN_PASSWORD
```

`-Bruno` levanta las bases, arranca la API en 5798, espera a que responda,
ejecuta la colección y apaga la API que él mismo inició. La colección escribe
datos: no la apuntes a producción ni a una base que debas conservar. Tampoco
pongas contraseñas en `Bruno/environments/Local.bru` ni en documentación
compartida.

## 7. Entornos y datos

| Entorno | Al arrancar |
|---|---|
| Development | Aplica el esquema y carga datos de ejemplo en PostgreSQL, MongoDB, Cassandra e Identity. Los seeders son idempotentes: reiniciar no duplica. |
| Production | Aplica el esquema y **solo aprovisiona el administrador** definido por `BootstrapAdmin__*` si no existe. **No borra datos.** |

Diferencias que conviene tener claras:

- **Datos:** en Development hay funkos, categorías y proveedores de ejemplo; en
  Production la base empieza vacía y solo se crea el admin. No se ejecuta
  ningún limpiador al arrancar.
- **CORS:** en Development se añaden orígenes locales; en Production solo se
  permiten los de `Cors__AllowedOrigins__*`.
- **Swagger y explorador GraphQL:** disponibles en Development. En Production
  devuelven 404, pero `POST /graphql` sigue operativo.
- **Errores:** en Production no se incluye el detalle de las excepciones.
- **Rate limit:** 120 peticiones por minuto y por IP en todos los entornos.
- **Correo:** en Development se captura en Mailpit; en Production se usa el
  SMTP real configurado en `.env.production`.

## 8. Producción con un solo comando

Producción usa su propio fichero de secretos, `.env.production` (no lo
commitees), y el Compose de `FunkoApiV.2\FunkoApiV.2\compose.yaml`.

### Primera vez

```powershell
.\Start.ps1 -Production -InitEnv
```

Genera `.env.production` con contraseñas aleatorias para PostgreSQL, JWT y el
admin. Después revisa y ajusta:

- `CORS_ALLOWED_ORIGIN_0`: origen real del frontend.
- `SMTP_*`: proveedor de correo real. Los valores de ejemplo no envían correo.
- `BOOTSTRAP_ADMIN_*`: cuenta administradora inicial.

### Arrancar

```powershell
.\Start.ps1 -Production
```

El script levanta primero las bases, comprueba las credenciales de PostgreSQL y
solo entonces arranca el backend. Si la contraseña no coincide con la que ya
tiene el volumen, aborta con un mensaje claro en lugar de dejar el contenedor en
bucle.

| Servicio | URL / puerto |
|---|---|
| API | <http://localhost:5197> |
| Swagger | no publicado en Production (404) |
| GraphQL | `POST /graphql`; explorador no publicado |
| PostgreSQL | `127.0.0.1:5432` |
| MongoDB | `127.0.0.1:27017` |
| Cassandra | `127.0.0.1:9043` |
| Redis | `127.0.0.1:6379` |

### Reconstruir tras cambiar código C#

```powershell
.\Start.ps1 -Production -Build
```

La imagen no se reconstruye sola: si cambias código y no usas `-Build`, seguirás
ejecutando la versión anterior.

### Logs y parada

```powershell
.\Start.ps1 -Production -Logs
.\Start.ps1 -Production -Down
```

`-Down` no borra volúmenes. No uses `docker compose down -v` salvo que quieras
perder los datos.

### Si aparece `28P01: password authentication failed`

`POSTGRES_PASSWORD` solo se aplica cuando el volumen de PostgreSQL está vacío.
Si el volumen ya existía con otra contraseña, el backend no puede conectar.
Para conservar los datos, cambia la contraseña dentro de Postgres:

```powershell
$password = (Select-String -Path '.\FunkoApiV.2\FunkoApiV.2\.env.production' `
  -Pattern '^POSTGRES_PASSWORD=(.+)$').Matches[0].Groups[1].Value
docker exec funkoapi_postgres psql -U postgres -d postgres `
  -c "ALTER USER postgres WITH PASSWORD '$password';"
.\Start.ps1 -Production
```

La alternativa es poner en `.env.production` la contraseña que ya usa el
volumen. Borrar el volumen (`down -v`) aplica la nueva, pero pierde los datos.

## 9. Errores habituales

### `compose.yaml` no encontrado

Ejecuta los scripts desde la raíz. El Compose está en
`FunkoApiV.2\FunkoApiV.2\compose.yaml`: el proyecto de la API es la carpeta
interna, por eso `FunkoApiV.2` aparece dos veces.

### `POSTGRES_PASSWORD` o `JWT_KEY` no definidos

En desarrollo, `Start-Development.ps1` crea `FunkoApiV.2\FunkoApiV.2\.env` desde la
plantilla. En producción, usa `.\Start.ps1 -Production -InitEnv` y revisa
`.env.production`. Compose valida las variables de todos los servicios aunque
solo levantes las bases.

### El puerto 5798 ya está ocupado

```powershell
Get-NetTCPConnection -LocalPort 5798 -State Listen |
  Select-Object LocalAddress, LocalPort, OwningProcess
Get-Process -Id <PID>
```

Detén tu instancia con `Ctrl+C` o usa `-Port`.

### Error MSB3021/MSB3027: DLL bloqueada

La API sigue ejecutándose y Windows no puede reemplazar la DLL. Detén la
instancia con `Ctrl+C` y vuelve a compilar o a lanzar los tests.

### Swagger muestra error al cargar operaciones

Confirma que abriste la URL de la instancia actual y que estás en Development.
La definición OpenAPI está en `/swagger/v1/swagger.json`.

### El login devuelve 500 nada más desplegar

Suele ser una imagen antigua. Reconstruye con `.\Start.ps1 -Production -Build`.

### El admin de producción no usa la contraseña del `.env.production`

Si `admin@admin.com` ya existía (por ejemplo, porque el volumen se creó en
Development), el bootstrap no cambia su contraseña: solo comprueba que tenga rol
`ADMIN`. Usa la contraseña anterior (`Admin123!` en desarrollo) o crea otro
administrador.

## 10. Apagar servicios

`Ctrl+C` detiene la API. Para parar además los contenedores de desarrollo:

```powershell
.\Start.ps1 -Down
```

Los volúmenes se conservan: al volver a arrancar, los datos siguen ahí. No uses
`docker compose down -v` salvo que quieras borrarlos.

Para el stack de producción:

```powershell
.\Start.ps1 -Production -Down
```

Para el detalle de Compose, consulta [Docker](19-docker.md).

## Mapa de apuntes técnicos

| Documento | Tema |
|---|---|
| [01](01-configuracion-proyectos-dotnet.md) | Solución, proyectos .NET y configuración |
| [02](02-arquitectura-pipeline-http.md) | Arquitectura y pipeline HTTP |
| [03](03-inyeccion-dependencias.md) | Inyección de dependencias |
| [04](04-controladores-rest.md) | Controladores REST y respuestas |
| [05](05-validacion-cascada.md) | Validación de solicitudes |
| [06](06-rest-best-practices.md) | Contrato y buenas prácticas REST |
| [07](07-repository-pattern.md) | Repositorios |
| [08](08-ef-core-postgresql.md) | EF Core y PostgreSQL |
| [09](09-mongodb.md) | MongoDB y proveedores |
| [10](10-redis-caching.md) | Redis y caché de aplicación |
| [11](11-patron-result.md) | Result y errores de dominio |
| [12](12-servicios-negocio.md) | Servicios de negocio |
| [13](13-borrado-logico.md) | Borrado lógico |
| [14](14-mapeadores.md) | Mapeadores |
| [15](15-jwt-authentication.md) | Autenticación JWT |
| [16](16-autorizacion-roles.md) | Autorización por roles |
| [17](17-seguridad-http.md) | CORS, límites, CSP, Swagger e índices |
| [18](18-testing.md) | Pruebas .NET y cobertura |
| [19](19-docker.md) | Docker, desarrollo y producción |
| [20](20-bruno-testing.md) | Colección Bruno |
| [21](21-GRAPHQL-EJEMPLOS.md) | Ejemplos GraphQL |
| [22](22-presentacion.md) | Guion de exposición y demo |

Las páginas técnicas describen el estado actual y enlazan con las clases
correspondientes. Para ejecutar la aplicación o las pruebas, empieza por las
secciones anteriores.
