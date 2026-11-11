# 20 — Pruebas HTTP con Bruno

## 1. Antes de ejecutar

1. Inicia Docker Desktop.
2. Desde la raíz, ejecuta `.\Start.ps1 -Bruno` y deja que el script haga el
   flujo completo: bases, API en 5798, espera y colección.
3. Si prefieres hacerlo por separado, arranca la API con `.\Start.ps1` en una
   terminal y comprueba que responde en `http://localhost:5798`.
4. Instala Bruno CLI si aún no está disponible: `bru --version`.

La colección está en `Bruno/` y el entorno `Local` apunta a
`http://localhost:5798`. Para otro puerto, usa `Test-All.ps1 -BaseUrl` o cambia
la URL del entorno local.

## 2. Ejecutar colección Bruno

La forma recomendada es el flujo completo, que arranca y apaga la API por ti:

```powershell
.\Start.ps1 -Bruno
```

`Run-Bruno.ps1` usa `FUNKO_ADMIN_EMAIL` y `FUNKO_ADMIN_PASSWORD` si están
definidas; si no, aplica los valores de Development (`admin@admin.com` /
`Admin123!`). También puedes fijarlas solo para la sesión actual:

```powershell
$env:FUNKO_ADMIN_EMAIL = "admin@admin.com"
$env:FUNKO_ADMIN_PASSWORD = "Admin123!"
.\Start.ps1 -Bruno
Remove-Item Env:FUNKO_ADMIN_EMAIL, Env:FUNKO_ADMIN_PASSWORD
```

La cuenta ADMIN anterior es una cuenta de muestra que el seeder crea en
Development. Si has cambiado su contraseña, utiliza la nueva. No guardes
contraseñas en archivos `.bru`.

También se puede ejecutar solo la colección con Bruno CLI, con la API ya
arrancada:

```powershell
Push-Location .\Bruno
bru run -r --env-file .\environments\Local.bru
Pop-Location
```

Si `bru` no está instalado, los scripts usan `npx --yes @usebruno/cli`.

> La colección **escribe** datos. No la ejecutes contra Production ni contra una
> base que debas conservar. En un volumen limpio de Production fallarían las
> peticiones que dependen de los datos sembrados en Development.

## 3. Suite completa

Para ejecutar primero .NET y luego Bruno:

```powershell
.\Test-All.ps1
```

Requiere Docker Desktop, la API Development activa en el puerto del parámetro
`BaseUrl` y Bruno CLI (o `npx`). Si no hay credenciales en el entorno, el script
las pide sin escribirlas en la colección. Para un puerto distinto:

```powershell
.\Test-All.ps1 -BaseUrl http://localhost:5800
```

`Test-All.ps1` **no recoge cobertura**. Para tener tests .NET con cobertura y
después Bruno, ejecuta en este orden:

```powershell
dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj `
  --settings .\FunkoApiV.2\coverage.runsettings `
  --collect:"XPlat Code Coverage"

.\Start.ps1 -Bruno

reportgenerator `
  -reports:".\FunkoApiV.2\FunkoApiV2Test\TestResults\**\coverage.cobertura.xml" `
  -targetdir:".\FunkoApiV.2\coveragereport" `
  -reporttypes:"Html;TextSummary"
```

El informe HTML queda en `FunkoApiV.2\coveragereport\index.html`. El detalle de
la cobertura está en [Pruebas .NET y cobertura](18-testing.md).

Las pruebas de integración .NET usan Testcontainers. Si una compilación informa
que `FunkoApiV.2.dll` está bloqueada, detén la instancia de la API con `Ctrl+C`,
vuelve a ejecutar las pruebas y arranca la API de nuevo antes de correr solo
Bruno.

## 4. Contenido de la colección

| Carpeta | Cobertura |
|---|---|
| `0 - Setup` | Inicio de sesión ADMIN, creación de proveedor de prueba y acceso protegido sin token (401). |
| `1 - Categorias` | Listado, alta, actualización, eliminación, validación y errores 404/409. |
| `2 - Funkos` | Listado, lectura protegida, CRUD, filtros, imagen, validación y errores 404. |
| `3 - Proveedores` | Listado, lectura, actualización, validación y error 404. |
| `90 - Teardown` | Eliminación del proveedor de prueba. |

La colección usa IDs guardados por el flujo de setup. Ejecútala completa y
ordenada con `bru run -r`; ejecutar archivos aislados puede fallar si no existen
las variables que producen las peticiones previas. En la ejecución de referencia
actual hay 34 peticiones y 80 comprobaciones; la cantidad puede cambiar al
ampliar la colección.

## 5. Seguridad y limpieza

- Los tokens se obtienen durante el login; no están escritos en la colección.
- La contraseña se pasa mediante variables de proceso.
- `Local.bru` contiene la URL local y variables no secretas.
- Las pruebas crean y borran registros de prueba. No ejecutes la colección
  apuntando a producción ni a una base de datos con datos que deban conservarse.

Para el orden general de trabajo, consulta
[la guía práctica](00-guia-practica.md); para revisar archivos de la colección,
consulta [`Bruno/README.md`](../Bruno/README.md).
