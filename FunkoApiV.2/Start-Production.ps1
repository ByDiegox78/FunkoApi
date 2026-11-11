#!/usr/bin/env pwsh
#===============================================================================
# Start-Production.ps1
# Construye y levanta FunkoApi en modo Production con Docker Compose.
#
# Uso:
#   .\Start-Production.ps1 -InitEnv     # crea .env.production con secretos aleatorios
#   .\Start-Production.ps1              # levanta todo (compila la imagen si hace falta)
#   .\Start-Production.ps1 -Build       # fuerza la reconstrucción de la imagen
#   .\Start-Production.ps1 -Logs        # sigue los logs del backend
#   .\Start-Production.ps1 -Down        # para los contenedores (NO borra datos)
#===============================================================================

[CmdletBinding()]
param(
    [string]$EnvFile = ".env.production",
    [switch]$InitEnv,
    [switch]$Build,
    [switch]$Logs,
    [switch]$Down
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$apiRoot = $PSScriptRoot
$projectDir = Join-Path $apiRoot "FunkoApiV.2"
$composeFile = Join-Path $projectDir "compose.yaml"
$publicUrl = "http://localhost:5197"
$healthUrl = "$publicUrl/api/funkos?page=0&size=1"

if (!(Test-Path -LiteralPath $composeFile)) {
    throw "No se encontró Docker Compose: $composeFile"
}

if (![System.IO.Path]::IsPathRooted($EnvFile)) {
    $EnvFile = Join-Path $projectDir $EnvFile
}

# ─── 1. Utilidades ────────────────────────────────────────────────────────────
function New-RandomSecret {
    param(
        [int]$Bytes = 32,
        [int]$Length = 28
    )

    $buffer = New-Object byte[] $Bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($buffer)
    $candidate = [Convert]::ToBase64String($buffer) -replace '[^A-Za-z0-9]', ''

    if ($candidate.Length -lt $Length) {
        throw "No se pudo generar un secreto de $Length caracteres."
    }

    return $candidate.Substring(0, $Length)
}

function New-ProductionEnvFile {
    param([string]$Path)

    if (Test-Path -LiteralPath $Path) {
        throw "Ya existe $Path. Bórralo o edítalo a mano si quieres regenerarlo."
    }

    $postgresPassword = New-RandomSecret -Bytes 32 -Length 28
    $jwtKey = New-RandomSecret -Bytes 48 -Length 44
    $adminPassword = "Aa1!$(New-RandomSecret -Bytes 24 -Length 12)"

    $content = @"
# Secretos de producción. NO commitear. Añade .env* a .gitignore.
POSTGRES_USER=postgres
POSTGRES_PASSWORD=$postgresPassword

# Clave de firma del JWT (mínimo 32 caracteres).
JWT_KEY=$jwtKey

# Origen del frontend autorizado por CORS. Cámbialo por tu dominio real.
CORS_ALLOWED_ORIGIN_0=$publicUrl

# Administrador que se aprovisiona al arrancar en Production.
BOOTSTRAP_ADMIN_EMAIL=admin@admin.com
BOOTSTRAP_ADMIN_PASSWORD=$adminPassword
BOOTSTRAP_ADMIN_FIRST_NAME=Admin
BOOTSTRAP_ADMIN_LAST_NAME=Sistema

# SMTP real. Cámbialo por tu proveedor antes de usar el envío de correos.
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_SECURITY=StartTls
SMTP_USERNAME=cambiar
SMTP_PASSWORD=cambiar
SMTP_FROM=noreply@funkos.com
"@

    [System.IO.File]::WriteAllText($Path, $content, [System.Text.UTF8Encoding]::new($false))

    Write-Host ""
    Write-Host "Se creó $Path" -ForegroundColor Green
    Write-Host "  Admin:    admin@admin.com / $adminPassword" -ForegroundColor Yellow
    Write-Host "  Guarda esa contraseña: no se vuelve a mostrar." -ForegroundColor Yellow
    Write-Host "  Revisa SMTP_* y CORS_ALLOWED_ORIGIN_0 antes de abrirlo a Internet." -ForegroundColor Yellow
    Write-Host ""
}

function Read-DotEnvFile {
    param([string]$Path)

    $values = @{}
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith("#")) {
            continue
        }

        $index = $trimmed.IndexOf("=")
        if ($index -lt 1) {
            continue
        }

        $key = $trimmed.Substring(0, $index).Trim()
        $value = $trimmed.Substring($index + 1).Trim()
        $values[$key] = $value
    }

    return $values
}

function Assert-ProductionEnvFile {
    param([string]$Path)

    $values = Read-DotEnvFile -Path $Path

    $required = @(
        "POSTGRES_PASSWORD",
        "JWT_KEY",
        "CORS_ALLOWED_ORIGIN_0",
        "BOOTSTRAP_ADMIN_EMAIL",
        "BOOTSTRAP_ADMIN_PASSWORD",
        "BOOTSTRAP_ADMIN_FIRST_NAME",
        "BOOTSTRAP_ADMIN_LAST_NAME",
        "SMTP_HOST",
        "SMTP_USERNAME",
        "SMTP_PASSWORD",
        "SMTP_FROM"
    )

    $missing = @($required | Where-Object {
            [string]::IsNullOrWhiteSpace($values[$_])
        })

    if ($missing.Count -gt 0) {
        throw "Faltan variables en $Path`: $($missing -join ', '). Usa -InitEnv para generar la plantilla."
    }

    if ($values["JWT_KEY"] -eq "FunkoApi-Development-Only-Key-32-Bytes!" -or $values["JWT_KEY"].Length -lt 32) {
        throw "JWT_KEY debe ser propia y tener al menos 32 caracteres. Ahora mismo es la de desarrollo o es demasiado corta."
    }

    if ($values["POSTGRES_PASSWORD"] -eq "postgres") {
        throw "POSTGRES_PASSWORD sigue siendo 'postgres'. Define una contraseña real en $Path."
    }

    if ($values["BOOTSTRAP_ADMIN_PASSWORD"] -eq "Admin123!") {
        throw "BOOTSTRAP_ADMIN_PASSWORD sigue siendo la de desarrollo. Define una contraseña real en $Path."
    }

    if ($values["SMTP_HOST"] -in @("localhost", "127.0.0.1")) {
        throw "SMTP_HOST no puede ser localhost en Production: dentro del contenedor apuntaría a sí mismo. Configura un SMTP real en $Path."
    }

    return $values
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

# ─── 2. Modo -InitEnv ─────────────────────────────────────────────────────────
if ($InitEnv) {
    New-ProductionEnvFile -Path $EnvFile
    if (!$Build -and !$Logs -and !$Down) {
        Write-Host "Edita el archivo y vuelve a ejecutar .\Start-Production.ps1" -ForegroundColor Cyan
        exit 0
    }
}

if (!(Test-Path -LiteralPath $EnvFile)) {
    throw "No se encontró $EnvFile. Ejecuta primero .\Start-Production.ps1 -InitEnv."
}

$envValues = Assert-ProductionEnvFile -Path $EnvFile

# ─── 3. Modo -Down (nunca borra volúmenes) ────────────────────────────────────
if ($Down) {
    Write-Host "==> Deteniendo los contenedores de producción (los datos se conservan)..." -ForegroundColor Cyan
    Push-Location $projectDir
    try {
        & docker compose --env-file $EnvFile -f $composeFile down
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose down falló (código $LASTEXITCODE)."
        }
    } finally {
        Pop-Location
    }

    Write-Host "Contenedores detenidos. Volúmenes intactos." -ForegroundColor Green
    exit 0
}

# ─── 4. Modo -Logs ────────────────────────────────────────────────────────────
if ($Logs) {
    Push-Location $projectDir
    try {
        & docker compose --env-file $EnvFile -f $composeFile logs -f --tail 200 backend
    } finally {
        Pop-Location
    }
    exit $LASTEXITCODE
}

# ─── 5. Construir y levantar ──────────────────────────────────────────────────
Write-Host "==> Producción: PostgreSQL, MongoDB, Cassandra, Redis y backend" -ForegroundColor Cyan
Write-Host "    Mailpit no se levanta: en Production se usa SMTP real ($($envValues['SMTP_HOST']):$($envValues['SMTP_PORT']))." -ForegroundColor DarkGray
Write-Host "    Aviso: desarrollo y producción comparten los contenedores funkoapi_*." -ForegroundColor DarkYellow
Write-Host "           Para el entorno de desarrollo antes de levantar producción." -ForegroundColor DarkYellow
Write-Host ""

Push-Location $projectDir
try {
    if ($Build) {
        Write-Host "==> Reconstruyendo la imagen del backend..." -ForegroundColor Cyan
        & docker compose --env-file $EnvFile -f $composeFile build --pull backend
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose build falló (código $LASTEXITCODE)."
        }
    }

    Write-Host "==> Levantando bases de datos..." -ForegroundColor Cyan
    & docker compose --env-file $EnvFile -f $composeFile up -d --wait db mongo_db cassandra redis
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose up falló al levantar las bases (código $LASTEXITCODE)."
    }

    # POSTGRES_PASSWORD solo se aplica al crear el volumen; si el volumen ya
    # existía con otra contraseña, el backend fallará con 28P01.
    Write-Host "==> Comprobando credenciales de PostgreSQL..." -ForegroundColor Cyan
    $postgresNetwork = (& docker inspect funkoapi_postgres --format '{{range $k, $v := .NetworkSettings.Networks}}{{$k}}{{end}}' 2>$null)
    if ([string]::IsNullOrWhiteSpace($postgresNetwork)) {
        throw "No se pudo inspeccionar el contenedor funkoapi_postgres. ¿Está arrancado?"
    }

    $postgresUser = if ([string]::IsNullOrWhiteSpace($envValues['POSTGRES_USER'])) { "postgres" } else { $envValues['POSTGRES_USER'] }

    & docker run --rm --network $postgresNetwork `
        -e PGPASSWORD=$($envValues['POSTGRES_PASSWORD']) `
        postgres:16-alpine `
        psql -h db -U $postgresUser -d postgres -tAc "select 1" 2>$null | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "POSTGRES_PASSWORD no coincide con la contraseña guardada en el volumen." -ForegroundColor Red
        Write-Host "POSTGRES_PASSWORD solo se aplica cuando el volumen está vacío." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Para conservar los datos, cambia la contraseña dentro de Postgres:" -ForegroundColor Yellow
        Write-Host '  docker exec -it funkoapi_postgres psql -U postgres -c "ALTER USER postgres WITH PASSWORD ''<la del .env.production>'';"'
        Write-Host "O pon en $EnvFile la misma contraseña que ya usa el volumen." -ForegroundColor Yellow
        throw "Credenciales de PostgreSQL incorrectas. El backend no se ha arrancado."
    }

    Write-Host "    Credenciales correctas." -ForegroundColor Green

    Write-Host "==> Levantando el backend..." -ForegroundColor Cyan
    & docker compose --env-file $EnvFile -f $composeFile up -d --wait
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose up falló (código $LASTEXITCODE). Revisa los logs con .\Start-Production.ps1 -Logs."
    }
} finally {
    Pop-Location
}

# ─── 6. Esperar a que la API responda ─────────────────────────────────────────
Write-Host "==> Esperando a que la API responda en $publicUrl..." -ForegroundColor Cyan
$ready = $false
for ($i = 0; $i -lt 60; $i++) {
    try {
        $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 3
        if ($response.StatusCode -eq 200) {
            $ready = $true
            break
        }
    } catch {
        Start-Sleep -Seconds 2
    }
}

if (!$ready) {
    Write-Host ""
    Write-Host "La API no respondió a tiempo. Últimos logs del backend:" -ForegroundColor Red
    Push-Location $projectDir
    try {
        & docker compose --env-file $EnvFile -f $composeFile logs --tail 60 backend
    } finally {
        Pop-Location
    }
    throw "Revisa los logs con .\Start-Production.ps1 -Logs."
}

Write-Host ""
Write-Host "FunkoApi en producción:" -ForegroundColor Green
Write-Host "  API:            $publicUrl"
Write-Host "  GraphQL:        $publicUrl/graphql  (explorador deshabilitado)"
Write-Host "  Swagger:        deshabilitado en Production"
Write-Host "  Admin:          $($envValues['BOOTSTRAP_ADMIN_EMAIL'])"
Write-Host ""
Write-Host "  Logs:           .\Start-Production.ps1 -Logs"
Write-Host "  Parar:          .\Start-Production.ps1 -Down"
Write-Host ""
