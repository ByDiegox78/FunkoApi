#!/usr/bin/env pwsh
#===============================================================================
# Run-Bruno.ps1
# Arranca las bases con Docker, levanta la API en Development (5798) y ejecuta
# la colección Bruno. Se puede lanzar desde FunkoApiV.2 o desde la raíz.
# Uso: .\Run-Bruno.ps1 [-Environment Local] [-ResetData]
#===============================================================================

[CmdletBinding()]
param(
    [string]$Environment = "Local",
    [switch]$ResetData
)

$ErrorActionPreference = "Stop"

$apiRoot = $PSScriptRoot
$projectDir = Join-Path $apiRoot "FunkoApiV.2"
$brunoDir = Join-Path $apiRoot "Bruno"
$envFile = Join-Path $projectDir ".env"
$exampleEnvFile = Join-Path $apiRoot ".env.example"
$brunoEnvFile = Join-Path $brunoDir "environments\$Environment.bru"
$baseUrl = "http://localhost:5798"
$healthUrl = "$baseUrl/api/funkos?page=0&size=1"
$apiOutLog = Join-Path $env:TEMP "funkoapi-run-bruno.log"
$apiErrLog = Join-Path $env:TEMP "funkoapi-run-bruno.err.log"
$temporaryEnvFile = Join-Path ([System.IO.Path]::GetTempPath()) "FunkoApi-Bruno-$PID.bru"

$brunoExit = 1
$apiProcess = $null
$startedApi = $false

# ─── 1. Crear .env desde la plantilla si no existe ───
if (!(Test-Path -LiteralPath $envFile)) {
    if (!(Test-Path -LiteralPath $exampleEnvFile)) {
        throw "No se encontró $envFile ni la plantilla $exampleEnvFile."
    }
    Copy-Item -LiteralPath $exampleEnvFile -Destination $envFile
    Write-Host "Se creó .env desde .env.example." -ForegroundColor Yellow
}

# ─── 2. Borrar volúmenes de desarrollo si se pide ───
if ($ResetData) {
    $confirm = Read-Host "Esto borrará los volúmenes de desarrollo (Postgres, Mongo, Cassandra y Redis). Escribe SI para continuar"
    if ($confirm -ne "SI") {
        throw "Cancelado por el usuario."
    }
    Write-Host "==> Borrando volúmenes de desarrollo..." -ForegroundColor Yellow
    Push-Location $projectDir
    try {
        docker compose --env-file $envFile down -v
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose down -v falló (código $LASTEXITCODE)."
        }
    } finally {
        Pop-Location
    }
}

# ─── 3. Levantar las bases de datos ───
Write-Host "==> Levantando PostgreSQL, MongoDB, Cassandra, Redis y Mailpit..." -ForegroundColor Cyan
Push-Location $projectDir
try {
    docker compose --profile development --env-file $envFile up -d --wait db mongo_db cassandra redis mailpit
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose up falló (código $LASTEXITCODE)."
    }
} finally {
    Pop-Location
}

# ─── 4. Reutilizar la API si ya está levantada; si no, arrancarla en el host ───
$alreadyRunning = $false
try {
    $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 3
    if ($response.StatusCode -eq 200) {
        $alreadyRunning = $true
    }
} catch { }

if (-not $alreadyRunning) {
    $portInUse = Get-NetTCPConnection -LocalPort 5798 -State Listen -ErrorAction SilentlyContinue
    if ($portInUse) {
        throw "El puerto 5798 está ocupado por otro proceso. Deténlo antes de ejecutar este script."
    }

    Write-Host "==> Iniciando la API en Development ($baseUrl)..." -ForegroundColor Cyan
    $previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
    try {
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $apiProcess = Start-Process -FilePath "dotnet" `
            -ArgumentList @("run", "--project", ".\FunkoApiV.2.csproj", "--no-launch-profile", "--urls", $baseUrl) `
            -WorkingDirectory $projectDir `
            -RedirectStandardOutput $apiOutLog `
            -RedirectStandardError $apiErrLog `
            -PassThru -WindowStyle Hidden
        $startedApi = $true
    } finally {
        if ($null -eq $previousEnvironment) {
            Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
        } else {
            $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
        }
    }
} else {
    Write-Host "==> La API ya responde en $baseUrl; se reutiliza." -ForegroundColor Yellow
}

try {
    if ($startedApi) {
        Write-Host "==> Esperando a que la API responda..." -ForegroundColor Cyan
        $ready = $false
        for ($i = 0; $i -lt 60; $i++) {
            if ($apiProcess.HasExited) {
                throw "La API terminó antes de arrancar. Revisa $apiOutLog y $apiErrLog."
            }
            try {
                $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 2
                if ($response.StatusCode -eq 200) {
                    $ready = $true
                    break
                }
            } catch { }
            Start-Sleep -Seconds 2
        }
        if (-not $ready) {
            throw "La API no respondió a tiempo. Revisa $apiOutLog."
        }
        Write-Host "API lista." -ForegroundColor Green
    }

    # ─── 5. Credenciales de Bruno (por defecto, las del seeder de Development) ───
    if ([string]::IsNullOrWhiteSpace($env:FUNKO_ADMIN_EMAIL)) {
        $env:FUNKO_ADMIN_EMAIL = "admin@admin.com"
    }
    if ([string]::IsNullOrWhiteSpace($env:FUNKO_ADMIN_PASSWORD)) {
        $env:FUNKO_ADMIN_PASSWORD = "Admin123!"
    }

    # ─── 6. Ejecutar Bruno con una copia temporal del entorno ───
    if (!(Test-Path -LiteralPath $brunoEnvFile)) {
        throw "No se encontró el entorno de Bruno: $brunoEnvFile"
    }
    $brunoEnvironment = [System.IO.File]::ReadAllText($brunoEnvFile)
    $updatedEnvironment = [System.Text.RegularExpressions.Regex]::Replace(
        $brunoEnvironment,
        "(?m)^\s*baseUrl:\s*\S+",
        "  baseUrl: $baseUrl")
    [System.IO.File]::WriteAllText(
        $temporaryEnvFile,
        $updatedEnvironment,
        [System.Text.UTF8Encoding]::new($false))

    Write-Host "==> Ejecutando la colección Bruno..." -ForegroundColor Cyan
    Push-Location $brunoDir
    try {
        npx --yes @usebruno/cli run -r --env-file $temporaryEnvFile
        $brunoExit = $LASTEXITCODE
    } finally {
        Pop-Location
    }
} finally {
    Remove-Item -LiteralPath $temporaryEnvFile -ErrorAction SilentlyContinue
    if ($startedApi -and $apiProcess -and -not $apiProcess.HasExited) {
        & taskkill /PID $apiProcess.Id /T /F | Out-Null
        Write-Host "API detenida. Los contenedores siguen en marcha." -ForegroundColor Yellow
    }
}

if ($brunoExit -ne 0) {
    Write-Host "Bruno reportó fallos (código $brunoExit)." -ForegroundColor Red
    exit $brunoExit
}

Write-Host "Todo OK." -ForegroundColor Green
