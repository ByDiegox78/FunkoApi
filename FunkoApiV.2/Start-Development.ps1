[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int]$Port = 5798,
    [switch]$ResetData
)

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$apiDirectory = Join-Path $projectRoot "FunkoApiV.2"
$composeFile = Join-Path $apiDirectory "compose.yaml"
$envFile = Join-Path $apiDirectory ".env"
$exampleEnvFile = Join-Path $projectRoot ".env.example"

if (!(Test-Path -LiteralPath $composeFile)) {
    throw "No se encontró Docker Compose: $composeFile"
}

if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "No se encontró Docker. Instala Docker Desktop y vuelve a ejecutar el script."
}

try {
    & docker info --format '{{.ServerVersion}}' 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker no está disponible."
    }
} catch {
    throw "Docker no está disponible. Inicia Docker Desktop y vuelve a ejecutar el script."
}

if (!(Test-Path -LiteralPath $envFile)) {
    if (!(Test-Path -LiteralPath $exampleEnvFile)) {
        throw "No se encontró $envFile ni la plantilla $exampleEnvFile."
    }

    Copy-Item -LiteralPath $exampleEnvFile -Destination $envFile
    Write-Host "Se creó .env con los valores de desarrollo de .env.example." -ForegroundColor Yellow
}

if ($ResetData) {
    $confirm = Read-Host "Esto borrará los volúmenes de desarrollo (Postgres, Mongo, Cassandra y Redis). Escribe SI para continuar"
    if ($confirm -ne "SI") {
        throw "Cancelado por el usuario."
    }

    Write-Host "==> Borrando volúmenes de desarrollo..." -ForegroundColor Yellow
    & docker compose --env-file $envFile -f $composeFile down -v
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose no pudo borrar los volúmenes (código $LASTEXITCODE)."
    }
}

$portInUse = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if ($portInUse) {
    throw "El puerto $Port ya está ocupado. Detén la aplicación que lo usa o ejecuta .\Start-Development.ps1 -Port 5197."
}

Write-Host "==> Iniciando PostgreSQL, MongoDB, Cassandra, Redis y Mailpit..." -ForegroundColor Cyan
& docker compose --profile development --env-file $envFile -f $composeFile up -d --wait db mongo_db cassandra redis mailpit
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose no pudo iniciar los servicios (código $LASTEXITCODE)."
}

Write-Host ""
Write-Host "Servicios listos:" -ForegroundColor Green
Write-Host "  Swagger:        http://localhost:$Port/swagger"
Write-Host "  GraphQL:        http://localhost:$Port/graphql"
Write-Host "  Demo SignalR:   http://localhost:$Port/"
Write-Host "  Mailpit:        http://localhost:8025"
Write-Host "  Admin:          admin@admin.com / Admin123!"
Write-Host ""

$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
try {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    Push-Location $apiDirectory
    try {
        Write-Host "==> Iniciando la API en desarrollo en http://localhost:$Port" -ForegroundColor Cyan
        & dotnet run --project .\FunkoApiV.2.csproj --no-launch-profile --urls "http://localhost:$Port"
        if ($LASTEXITCODE -ne 0) {
            throw "La API terminó con código $LASTEXITCODE."
        }
    } finally {
        Pop-Location
    }
} finally {
    if ($null -eq $previousEnvironment) {
        Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    } else {
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    }
}
