<#
.SYNOPSIS
    Punto de entrada único de FunkoApi.

.DESCRIPTION
    Envoltorio de los scripts de FunkoApiV.2 para que el comando de arranque sea
    siempre el mismo:
      - Sin parámetros: Start-Development.ps1 (bases + API en 5798).
      - -Bruno:         Run-Bruno.ps1 (bases + API + colección Bruno).
      - -Production:    Start-Production.ps1 (Compose en Production, 5197).

    Se ejecuta desde la raíz del repositorio, un nivel por encima de FunkoApiV.2.

.PARAMETER Port
    Puerto del entorno de desarrollo. Por defecto 5798. Solo se aplica al modo
    desarrollo.

.PARAMETER Bruno
    Levanta el entorno de desarrollo y ejecuta la colección Bruno.

.PARAMETER Production
    Construye y levanta el entorno de producción con Docker Compose.

.PARAMETER ResetData
    Borra los volúmenes de desarrollo antes de arrancar. Pide confirmación.

.PARAMETER InitEnv
    Genera .env.production con secretos aleatorios. Solo con -Production.

.PARAMETER Build
    Reconstruye la imagen del backend. Solo con -Production.

.PARAMETER Logs
    Sigue los logs del backend. Solo con -Production.

.PARAMETER Down
    Detiene los contenedores sin borrar datos. Con -Production para el stack de
    producción; sin -Production, para el de desarrollo.

.PARAMETER EnvFile
    Fichero de entorno de producción. Por defecto .env.production.

.EXAMPLE
    .\Start.ps1
    Desarrollo en http://localhost:5798.

.EXAMPLE
    .\Start.ps1 -Bruno
    Desarrollo + colección Bruno.

.EXAMPLE
    .\Start.ps1 -Port 5800
    Desarrollo en otro puerto.

.EXAMPLE
    .\Start.ps1 -ResetData
    Borra los volúmenes de desarrollo y vuelve a sembrar.

.EXAMPLE
    .\Start.ps1 -Production
    Producción en http://localhost:5197.

.EXAMPLE
    .\Start.ps1 -Production -InitEnv
    Genera .env.production y sale.
#>

[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int]$Port = 5798,

    [switch]$Bruno,
    [switch]$Production,
    [switch]$ResetData,
    [switch]$InitEnv,
    [switch]$Build,
    [switch]$Logs,
    [switch]$Down,

    [string]$EnvFile = ".env.production"
)

$ErrorActionPreference = "Stop"

$scriptsRoot = Join-Path $PSScriptRoot "FunkoApiV.2"

if (!(Test-Path -LiteralPath $scriptsRoot)) {
    throw "No se encontró la carpeta de scripts: $scriptsRoot"
}

function Get-ProjectScript {
    param([string]$Name)

    $path = Join-Path $scriptsRoot $Name
    if (!(Test-Path -LiteralPath $path)) {
        throw "No se encontró el script $Name en $scriptsRoot."
    }

    return $path
}

function Stop-DevelopmentEnvironment {
    $projectDir = Join-Path $scriptsRoot "FunkoApiV.2"
    $composeFile = Join-Path $projectDir "compose.yaml"
    $devEnvFile = Join-Path $projectDir ".env"

    if (!(Test-Path -LiteralPath $composeFile)) {
        throw "No se encontró Docker Compose: $composeFile"
    }

    if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
        throw "No se encontró Docker. Inicia Docker Desktop y vuelve a ejecutar el script."
    }

    if (!(Test-Path -LiteralPath $devEnvFile)) {
        $devEnvFile = Join-Path $scriptsRoot ".env.example"
        if (!(Test-Path -LiteralPath $devEnvFile)) {
            throw "No se encontró .env ni .env.example; no se pueden validar las variables de Compose."
        }
        Write-Host "No hay .env de desarrollo; se usa .env.example solo para validar las variables." -ForegroundColor Yellow
    }

    $apiListening = Get-NetTCPConnection -LocalPort 5798 -State Listen -ErrorAction SilentlyContinue
    if ($apiListening) {
        Write-Host "Aviso: la API sigue escuchando en el puerto 5798. Pulsa Ctrl+C en su terminal." -ForegroundColor Yellow
    }

    Write-Host "==> Deteniendo los contenedores de desarrollo (los datos se conservan)..." -ForegroundColor Cyan
    Push-Location $projectDir
    try {
        & docker compose --env-file $devEnvFile --profile development down
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose down falló (código $LASTEXITCODE)."
        }
    } finally {
        Pop-Location
    }

    Write-Host "Contenedores detenidos. Volúmenes intactos." -ForegroundColor Green
}

$productionOnlyOptions = @($Build, $Logs, $InitEnv | Where-Object { $_ })

if ($Production -and $Bruno) {
    throw "Usa -Production o -Bruno, no ambos."
}

if ($Bruno -and ($productionOnlyOptions.Count -gt 0 -or $Down)) {
    throw "-Bruno no se combina con -Down, -Build, -Logs ni -InitEnv."
}

if (-not $Production -and $productionOnlyOptions.Count -gt 0) {
    throw "-InitEnv, -Build y -Logs solo se aplican con -Production."
}

if ($Production -and $ResetData) {
    throw "-ResetData solo se aplica al entorno de desarrollo; en Production nunca se borran datos."
}

if ($Down -and $ResetData) {
    throw "-ResetData no se combina con -Down: parar los contenedores no borra datos."
}

if ($PSBoundParameters.ContainsKey("Port") -and ($Production -or $Bruno -or $Down)) {
    throw "-Port solo se aplica al arranque del entorno de desarrollo."
}

# --- Producción ---------------------------------------------------------------
if ($Production) {
    $scriptPath = Get-ProjectScript "Start-Production.ps1"

    $scriptArguments = @{ EnvFile = $EnvFile }
    if ($InitEnv) { $scriptArguments["InitEnv"] = $true }
    if ($Build) { $scriptArguments["Build"] = $true }
    if ($Logs) { $scriptArguments["Logs"] = $true }
    if ($Down) { $scriptArguments["Down"] = $true }

    & $scriptPath @scriptArguments
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    exit 0
}

# --- Parar el entorno de desarrollo ------------------------------------------
if ($Down) {
    Stop-DevelopmentEnvironment
    exit 0
}

# --- Desarrollo + Bruno -------------------------------------------------------
if ($Bruno) {
    $scriptPath = Get-ProjectScript "Run-Bruno.ps1"

    & $scriptPath -Environment "Local" -ResetData:$ResetData
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    exit 0
}

# --- Desarrollo ---------------------------------------------------------------
$scriptPath = Get-ProjectScript "Start-Development.ps1"

& $scriptPath -Port $Port -ResetData:$ResetData
if ($LASTEXITCODE) { exit $LASTEXITCODE }
exit 0
