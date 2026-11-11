# 18 — Pruebas .NET y estrategia de testing

## 1. Qué ejecutar según lo que quieras comprobar

Todos los comandos se ejecutan **desde la raíz del repositorio**, donde está
`Start.ps1`. Las rutas de los proyectos incluyen la carpeta `FunkoApiV.2`, que
es la que contiene la solución `FunkoApiV.2.slnx` y el proyecto de tests.

| Necesidad | Comando |
|---|---|
| Suite completa .NET + Bruno | `.\Test-All.ps1` |
| Tests .NET unitarios e integración | `dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj` |
| Solo unitarios | `dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj --filter "FullyQualifiedName~Unit"` |
| Solo integración | `dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj --filter "FullyQualifiedName~Integration"` |
| Tests .NET con cobertura | `dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj --settings .\FunkoApiV.2\coverage.runsettings --collect:"XPlat Code Coverage"` |
| Colección Bruno | `.\Start.ps1 -Bruno` |
| .NET con cobertura + Bruno | Los dos comandos anteriores en ese orden (ver apartado 4.3) |

## 2. Requisitos

- SDK de .NET 10.
- Docker Desktop iniciado para pruebas de integración.
- API en Development y Bruno CLI instalado para la suite completa.
- Cuenta ADMIN de Development para Bruno. `Test-All.ps1` pide el email y la
  contraseña si las variables `FUNKO_ADMIN_EMAIL` y `FUNKO_ADMIN_PASSWORD` no
  están definidas.

Para arrancar dependencias y API en desarrollo en otra terminal:

```powershell
.\Start.ps1
```

El puerto predeterminado es `5798`; se puede cambiar con `-Port`. Si se usa otro
puerto, indícalo también a `Test-All.ps1`:

```powershell
.\Test-All.ps1 -BaseUrl http://localhost:5800
```

`Test-All.ps1` valida la API, ejecuta `dotnet test` y luego Bruno. En Windows,
si la compilación falla con MSB3021/MSB3027 porque `FunkoApiV.2.dll` está en uso,
detén la API con `Ctrl+C` y vuelve a compilar. Para ejecutar después Bruno,
arranca de nuevo la API.

## 3. Estructura real de los tests

```text
FunkoApiV2Test/
├── Unit/          # Controladores, servicios, caché, GraphQL, SignalR,
│                  # infraestructura, mapeadores, seeders, almacenamiento y validadores
├── Integration/   # PostgreSQL, MongoDB, Cassandra y servicios
└── GraphQL/       # Pruebas de integración GraphQL
```

El proyecto usa NUnit, Moq, FluentAssertions y Testcontainers. Las pruebas de
integración arrancan contenedores propios; no necesitan que la API de desarrollo
esté activa, pero sí Docker Desktop.

## 4. Cobertura

### 4.1. Generar el XML

Ejecuta todos los tests .NET con el colector de cobertura y el RunSettings del
repositorio:

```powershell
dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj `
  --settings .\FunkoApiV.2\coverage.runsettings `
  --collect:"XPlat Code Coverage"
```

Cada ejecución crea una carpeta nueva en
`FunkoApiV.2\FunkoApiV2Test\TestResults\<guid>\` con el fichero
`coverage.cobertura.xml`. `TestResults/` está ignorado por git, así que no se
sube al repositorio.

Si solo quieres medir los tests unitarios (más rápido y sin Docker):

```powershell
dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj `
  --filter "FullyQualifiedName~Unit" `
  --settings .\FunkoApiV.2\coverage.runsettings `
  --collect:"XPlat Code Coverage"
```

### 4.2. Informe HTML

Instala la herramienta una sola vez por equipo:

```powershell
dotnet tool install --global dotnet-reportgenerator-globaltool
```

Genera el informe a partir de todos los XML acumulados:

```powershell
reportgenerator `
  -reports:".\FunkoApiV.2\FunkoApiV2Test\TestResults\**\coverage.cobertura.xml" `
  -targetdir:".\FunkoApiV.2\coveragereport" `
  -reporttypes:"Html;TextSummary"
```

Abre `FunkoApiV.2\coveragereport\index.html` en el navegador. El resumen en
texto queda en `Summary.txt`, dentro de esa misma carpeta. `coveragereport/` es
un artefacto generado: no lo commitees.

### 4.3. Cobertura y Bruno en la misma sesión

`Test-All.ps1` ejecuta `dotnet test` y después Bruno, pero **no recoge
cobertura**. Para tener las tres cosas, ejecuta en este orden:

```powershell
# 1. Tests .NET con cobertura
dotnet test .\FunkoApiV.2\FunkoApiV2Test\FunkoApiV2Test.csproj `
  --settings .\FunkoApiV.2\coverage.runsettings `
  --collect:"XPlat Code Coverage"

# 2. Bruno: levanta las bases y la API, ejecuta la colección y apaga la API
.\Start.ps1 -Bruno

# 3. Informe HTML con los XML generados
reportgenerator `
  -reports:".\FunkoApiV.2\FunkoApiV2Test\TestResults\**\coverage.cobertura.xml" `
  -targetdir:".\FunkoApiV.2\coveragereport" `
  -reporttypes:"Html;TextSummary"
```

Si la API ya está arrancada en `http://localhost:5798`, puedes saltarte el paso
2 y lanzar solo la colección:

```powershell
Push-Location .\FunkoApiV.2\Bruno
bru run -r --env-file .\environments\Local.bru
Pop-Location
```

Las pruebas de integración con Testcontainers suman su cobertura, pero tardan
más porque crean contenedores. Para una medida rápida, añade
`--filter "FullyQualifiedName~Unit"` al comando de `dotnet test`.

## 5. Estilo de tests

Los tests siguen el patrón Arrange / Act / Assert y separan comportamientos.
Usa las categorías y convenciones ya presentes en `FunkoApiV2Test`; no supongas
que cada clase o test usa exactamente el mismo patrón si el código existente
marca una excepción.

## 6. Diagnóstico rápido

- **Docker no disponible:** inicia Docker Desktop y repite.
- **Bruno no encontrado:** comprueba `bru --version` o instala Bruno CLI desde
  la página oficial.
- **API no responde:** arráncala con `.\Start.ps1` y comprueba
  `http://localhost:5798/api/funkos?page=0&size=1`.
- **401 en Bruno:** verifica las credenciales de una cuenta ADMIN de Development.
- **DLL bloqueada:** detén la instancia de la API antes de compilar el proyecto.

Para el flujo detallado de la colección y credenciales, consulta
[Testing con Bruno](20-bruno-testing.md). Para el paso a paso general, consulta
[la guía práctica](00-guia-practica.md).
