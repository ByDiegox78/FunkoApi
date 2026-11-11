# 01 — Configuración de la solución .NET

## Archivos que mandan

- `FunkoApiV.2.slnx`: solución con el proyecto web `FunkoApiV.2` y el proyecto
  de tests `FunkoApiV2Test`.
- `FunkoApiV.2/FunkoApiV.2.csproj` y
  `FunkoApiV2Test/FunkoApiV2Test.csproj`: framework objetivo y paquetes NuGet.
- `FunkoApiV.2/Properties/launchSettings.json`: perfiles locales Development,
  HTTP `5798` y HTTPS `7259`.
- `.env.example`: plantilla de variables para Docker Compose; no contiene
  credenciales válidas para producción.

El repositorio apunta a `net10.0`. No hay un `global.json` ni archivos
`Directory.Packages.props`/`Directory.Build.props` que fijen una versión única
del SDK o centralicen paquetes. Por eso, no uses versiones copiadas de apuntes
anteriores: consulta los `.csproj` actuales.

## Proyectos

| Proyecto | Responsabilidad |
|---|---|
| `FunkoApiV.2` | API ASP.NET Core, servicios, repositorios e infraestructura |
| `FunkoApiV2Test` | Tests NUnit unitarios, de integración y GraphQL |

Para restaurar y compilar desde la raíz:

```powershell
dotnet restore .\FunkoApiV.2.slnx
dotnet build .\FunkoApiV.2.slnx
```

Para arrancar con las dependencias correctas, utiliza
[`Start.ps1`](../../Start.ps1) desde la raíz (o `Start-Development.ps1` para solo
desarrollo) en lugar de suponer que `dotnet run` levantará PostgreSQL, MongoDB,
Cassandra y Redis automáticamente.
