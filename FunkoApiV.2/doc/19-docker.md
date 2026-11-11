# 19 — Docker: desarrollo y producción

Los scripts están en `FunkoApiV.2\` y el proyecto de la API, con `compose.yaml`
y `.env`, en `FunkoApiV.2\FunkoApiV.2\`. Todos los comandos de esta guía se
ejecutan desde la **raíz del repositorio**.

## 1. Servicios del Compose

| Servicio | Uso | Puerto del host |
|---|---|---|
| `db` | PostgreSQL: Funkos, categorías e Identity | 5432 |
| `mongo_db` | MongoDB: proveedores | 27017 |
| `cassandra` | Cassandra: vistas de consulta | 9043 → 9042 |
| `redis` | Caché de aplicación | 6379 |
| `mailpit` | SMTP y bandeja local (perfil `development`) | 1025 / 8025 |
| `backend` | API ASP.NET Core | 5197 → 8080 |

El Compose levanta varios contenedores, pero cada entorno se arranca con **un
solo comando**. No es una imagen única con la API y las bases dentro.

## 2. Desarrollo local

Docker ejecuta los almacenes y `dotnet` ejecuta la API en el host. Así puedes
usar Hot Reload y el depurador sin reconstruir la imagen.

```powershell
.\Start.ps1
```

`Start-Development.ps1`:

1. Comprueba Docker.
2. Crea `FunkoApiV.2\FunkoApiV.2\.env` desde `.env.example` si falta.
3. Levanta `db`, `mongo_db`, `cassandra`, `redis` y Mailpit con `--wait`.
4. Arranca la API en Development en el puerto 5798.

La API queda en primer plano; deténla con `Ctrl+C`. Mailpit está en
<http://localhost:8025>. Para parar también los contenedores:

```powershell
.\Start.ps1 -Down
```

Los volúmenes se conservan.

Si solo quieres los almacenes:

```powershell
docker compose --env-file .\FunkoApiV.2\FunkoApiV.2\.env `
  --profile development -f .\FunkoApiV.2\FunkoApiV.2\compose.yaml `
  up -d db mongo_db cassandra redis mailpit

docker compose --env-file .\FunkoApiV.2\FunkoApiV.2\.env `
  --profile development -f .\FunkoApiV.2\FunkoApiV.2\compose.yaml `
  stop db mongo_db cassandra redis mailpit
```

Compose valida las variables de todos los servicios, incluido `backend`, aunque
solo levantes las bases. Por eso necesita que exista `FunkoApiV.2\FunkoApiV.2\.env`.

## 3. Producción con un solo comando

Producción usa `.env.production`, con secretos propios, y el mismo
`compose.yaml`. El servicio `backend` se construye con el `Dockerfile` y define
`ASPNETCORE_ENVIRONMENT=Production`.

### Primera vez

```powershell
.\Start.ps1 -Production -InitEnv
```

Genera `.env.production` con contraseñas aleatorias para PostgreSQL, JWT y el
admin. Revisa después:

- `CORS_ALLOWED_ORIGIN_0`: origen real del frontend.
- `SMTP_*`: proveedor real; los valores de ejemplo son marcadores.
- `BOOTSTRAP_ADMIN_*`: administrador inicial.

### Arrancar, reconstruir, logs y parar

```powershell
.\Start.ps1 -Production          # arranca todo
.\Start.ps1 -Production -Build   # reconstruye la imagen antes de arrancar
.\Start.ps1 -Production -Logs    # sigue los logs del backend
.\Start.ps1 -Production -Down    # para sin borrar volúmenes
```

`Start-Production.ps1` levanta primero las bases, comprueba las credenciales de
PostgreSQL y solo después arranca el backend. Si la contraseña no coincide con
la que ya tiene el volumen, aborta con un mensaje accionable.

### Qué hace Production al arrancar

- Aplica el esquema (tablas e índices).
- Crea los roles `ADMIN` y `USER` si faltan.
- Aprovisiona el admin de `BootstrapAdmin__*` **solo si no existe**.
- **No borra datos** y no carga datos de ejemplo.

> Un reinicio del backend conserva los datos. Los volúmenes
> `funkoapiv2_*` son persistentes. `-Down` no los elimina.

### Diferencias visibles

- API en `http://localhost:5197`.
- Swagger devuelve 404; el explorador visual de GraphQL también.
- `POST /graphql` sigue disponible para clientes GraphQL.
- El registro crea usuarios `USER`; no crea administradores.
- Los correos se envían por el SMTP configurado, no por Mailpit.

## 4. Qué hacen los entornos al iniciar

La selección se basa en `IWebHostEnvironment` en `Program.cs`:

- **Development:** inicializa esquemas y ejecuta `DbSeeder`, `CassandraSeeder` e
  `IdentitySeeder`. Los seeders comprueban si ya hay datos antes de insertar, así
  que reiniciar no duplica ni borra.
- **Production:** inicializa esquemas y ejecuta
  `IdentitySeeder.EnsureProductionAdminAsync`. No inserta datos de ejemplo ni
  vacía tablas, colecciones o caché.

El antiguo `ProductionDataCleaner` no se invoca desde el arranque. Si quieres
limpiar datos, hazlo de forma explícita sobre una copia o un volumen de pruebas,
nunca como efecto colateral de arrancar la API.

## 5. Configuración de conexión

- `FunkoApiV.2\FunkoApiV.2\appsettings.json`: valores compartidos.
- `FunkoApiV.2\FunkoApiV.2\appsettings.Development.json`: `localhost`, Cassandra
  en 9043 y clave JWT local.
- `FunkoApiV.2\FunkoApiV.2\appsettings.Production.json`: hostnames Docker (`db`,
  `mongo_db`, `redis`, `cassandra`).
- `FunkoApiV.2\FunkoApiV.2\.env`: variables de desarrollo.
- `FunkoApiV.2\FunkoApiV.2\.env.production`: secretos de producción. No lo
  commitees; añádelo a `.gitignore` si todavía no está excluido.

Las variables de entorno prevalecen sobre JSON. Compose transforma `JWT_KEY` en
`Jwt__Key`, usa `POSTGRES_PASSWORD` para PostgreSQL y el backend, y convierte
`SMTP_*` en `Smtp__*`.

## 6. Problemas frecuentes

### `28P01: password authentication failed`

`POSTGRES_PASSWORD` solo se aplica cuando el volumen está vacío. Para conservar
los datos, cambia la contraseña dentro de Postgres:

```powershell
$password = (Select-String -Path '.\FunkoApiV.2\FunkoApiV.2\.env.production' `
  -Pattern '^POSTGRES_PASSWORD=(.+)$').Matches[0].Groups[1].Value
docker exec funkoapi_postgres psql -U postgres -d postgres `
  -c "ALTER USER postgres WITH PASSWORD '$password';"
.\Start.ps1 -Production
```

También puedes poner en `.env.production` la contraseña que ya usa el volumen.
`down -v` aplica la nueva, pero borra los datos.

### El login devuelve 500 tras desplegar

Suele ser una imagen antigua. Reconstruye con
`.\Start.ps1 -Production -Build`.

### Variables requeridas ausentes

En desarrollo, `Start-Development.ps1` crea `FunkoApiV.2\FunkoApiV.2\.env`. En producción,
usa `-InitEnv` y revisa `.env.production`.

### Puerto ocupado

Comprueba el proceso antes de detenerlo:

```powershell
Get-NetTCPConnection -LocalPort 5197 -State Listen
```

### Docker no responde

Inicia Docker Desktop. Las pruebas de integración también lo necesitan, aunque
crean sus propios contenedores con Testcontainers.

### `compose.yaml` no encontrado

Está en `.\FunkoApiV.2\FunkoApiV.2\compose.yaml`; ejecuta los scripts desde la raíz.
La carpeta interna es el proyecto de la API, por eso `FunkoApiV.2` aparece dos
veces en la ruta.

## 7. Comandos Compose equivalentes

Si necesitas depurar a mano:

```powershell
docker compose --env-file .\FunkoApiV.2\FunkoApiV.2\.env.production `
  -f .\FunkoApiV.2\FunkoApiV.2\compose.yaml up -d --wait

docker compose --env-file .\FunkoApiV.2\FunkoApiV.2\.env.production `
  -f .\FunkoApiV.2\FunkoApiV.2\compose.yaml logs -f backend

docker compose --env-file .\FunkoApiV.2\FunkoApiV.2\.env.production `
  -f .\FunkoApiV.2\FunkoApiV.2\compose.yaml down
```

Para el paso a paso general, consulta [la guía práctica](00-guia-practica.md).
