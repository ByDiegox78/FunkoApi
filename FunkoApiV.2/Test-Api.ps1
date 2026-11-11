#!/usr/bin/env pwsh
#===============================================================================
# Test-Api.ps1
# Prueba los endpoints de FunkoApi usando Invoke-WebRequest (PowerShell nativo).
# No requiere instalar Bruno CLI. Útil para validar rápidamente la API.
#===============================================================================

[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5798",
    [switch]$SkipSetup
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$ErrorActionPreference = "Stop"
$Passed = 0
$Failed = 0

# ─── Función auxiliar para hacer requests ───
function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Path,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [string]$ContentType = "application/json"
    )

    $url = "$BaseUrl$Path"
    $splat = @{
        Method = $Method
        Uri    = $url
        Headers = $Headers
        ErrorAction = "SilentlyContinue"
    }

    if ($Body -ne $null) {
        $splat["Body"] = $Body | ConvertTo-Json -Depth 10
        $splat["ContentType"] = $ContentType
    }

    try {
        $response = Invoke-WebRequest @splat
        return [PSCustomObject]@{
            StatusCode = $response.StatusCode
            StatusDescription = $response.StatusDescription
            Content = $response.Content
            Headers = $response.Headers
        }
    } catch {
        if ($_.Exception.Response -ne $null) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = [System.IO.StreamReader]::new($stream)
            $content = $reader.ReadToEnd()
            return [PSCustomObject]@{
                StatusCode = $statusCode
                StatusDescription = $_.Exception.Response.StatusDescription
                Content = $content
                Headers = $null
            }
        } else {
            Write-Warning "Error de conexión a ${url}: $($_.Exception.Message)"
            return [PSCustomObject]@{
                StatusCode = 0
                StatusDescription = "Connection Error"
                Content = $null
                Headers = $null
            }
        }
    }
}

# ─── Función para validar un test ───
function Test-Endpoint {
    param(
        [string]$Name,
        [int]$ExpectedStatus,
        [scriptblock]$Condition
    )

    $result = & $Condition
    $statusOk = $result.StatusCode -eq $ExpectedStatus

    if ($statusOk -and (& $Condition).StatusCode -eq $ExpectedStatus) {
        # Ejecutar condición solo una vez si es posible
        $actual = & $Condition
    } else {
        $actual = & $Condition
    }

    # Re-ejecutar para obtener el resultado real (simplificado)
    $resp = & $Condition

    $condResult = & $Condition
    # La condición ya se ejecutó arriba, aquí verificamos
    # Pero para no ejecutar 3 veces, guardamos en variable
}

# Mejor: función que recibe el response y valida
function Validate-Test {
    param(
        [string]$Name,
        [int]$ExpectedStatus,
        [PSCustomObject]$Response,
        [scriptblock]$ExtraChecks = $null
    )

    $script:Passed++
    $script:Failed++ # placeholder, se ajusta abajo

    $ok = $true
    $msgs = @()

    if ($Response.StatusCode -ne $ExpectedStatus) {
        $ok = $false
        $msgs += "Esperaba status $ExpectedStatus, obtuvo $($Response.StatusCode)"
    }

    if ($ExtraChecks -ne $null) {
        try {
            & $ExtraChecks -Response $Response | ForEach-Object { $msgs += $_; $ok = $false }
        } catch {
            $msgs += $_
            $ok = $false
        }
    }

    if ($ok) {
        $script:Passed++
        Write-Host "  ✅ $Name" -ForegroundColor Green
    } else {
        $script:Failed++
        Write-Host "  ❌ $Name" -ForegroundColor Red
        $msgs | ForEach-Object { Write-Host "      $_" -ForegroundColor Red }
        Write-Host "      Response: $($Response.Content)" -ForegroundColor DarkGray
    }
}

# ─── Variables de entorno (simulando Local.bru) ───
$script:funkoId = "1"
$script:categoriaId = "VQGCKyvzIM4"
$script:proveedorId = "672f1a8b3c9d4e5f6a7b8c9d"
$script:funkoIdCreado = $null
$script:categoriaIdCreada = $null

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "🧪 FunkoApi - Pruebas de API (Invoke-WebRequest)" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl"
Write-Host ""

# ─── Verificar que la API esté corriendo ───
Write-Host "🔍 Verificando que la API esté disponible..." -ForegroundColor Cyan
$health = Invoke-ApiRequest -Method Get -Path "/api/funkos?page=0&size=1"
if ($health.StatusCode -eq 0) {
    Write-Host "❌ No se puede conectar a $BaseUrl. ¿Está la API ejecutándose?" -ForegroundColor Red
    Write-Host "   Ejecuta: dotnet run --project FunkoApiV.2\FunkoApiV.2.csproj" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ API disponible (status $($health.StatusCode))`n" -ForegroundColor Green

# =========================================================================
# FUNKOS
# =========================================================================
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📦 FUNKOS" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# 1. Listar funkos
$resp = Invoke-ApiRequest -Method Get -Path "/api/funkos?page=0&size=10"
Validate-Test -Name "Listar funkos (200)" -ExpectedStatus 200 -Response $resp

# 2. Obtener funko por id
$resp = Invoke-ApiRequest -Method Get -Path "/api/funkos/$script:funkoId"
Validate-Test -Name "Obtener funko por id (200)" -ExpectedStatus 200 -Response $resp

# 3. Obtener funko inexistente (404)
$resp = Invoke-ApiRequest -Method Get -Path "/api/funkos/99999"
Validate-Test -Name "Obtener funko inexistente (404)" -ExpectedStatus 404 -Response $resp

# 4. Crear funko (201)
$createBody = @{
    name = "Funko Test PowerShell"
    precio = 15.99
    categoriaId = $script:categoriaId
    proveedorId = $script:proveedorId
    stock = 5
    imagen = "/uploads/funkos/test.png"
}
$resp = Invoke-ApiRequest -Method Post -Path "/api/funkos" -Body $createBody
Validate-Test -Name "Crear funko (201)" -ExpectedStatus 201 -Response $resp -ExtraChecks {
    param($Response)
    $errors = @()
    if ($Response.Content) {
        $body = $Response.Content | ConvertFrom-Json
        if ($body.id) {
            $script:funkoIdCreado = $body.id
        } else {
            $errors += "No se obtuvo id en la respuesta"
        }
    } else {
        $errors += "Respuesta vacía"
    }
    $errors
}

# 5. Buscar funkos por categoría
$resp = Invoke-ApiRequest -Method Get -Path "/api/funkos?categoriaId=$script:categoriaId&page=0&size=10"
Validate-Test -Name "Buscar funkos por categoría (200)" -ExpectedStatus 200 -Response $resp

# 6. Buscar funkos por precio
$resp = Invoke-ApiRequest -Method Get -Path "/api/funkos?precioMin=10&precioMax=20&page=0&size=10"
Validate-Test -Name "Buscar funkos por precio (200)" -ExpectedStatus 200 -Response $resp

# 7. Actualizar funko (200)
if ($script:funkoIdCreado) {
    $updateBody = @{
        name = "Funko Actualizado PS"
        precio = 18.50
        categoriaId = $script:categoriaId
        proveedorId = $script:proveedorId
        stock = 7
        imagen = "/uploads/funkos/updated.png"
    }
    $resp = Invoke-ApiRequest -Method Put -Path "/api/funkos/$script:funkoIdCreado" -Body $updateBody
    Validate-Test -Name "Actualizar funko (200)" -ExpectedStatus 200 -Response $resp

    # 8. Eliminar funko (204)
    $resp = Invoke-ApiRequest -Method Delete -Path "/api/funkos/$script:funkoIdCreado"
    Validate-Test -Name "Eliminar funko (204)" -ExpectedStatus 204 -Response $resp
}

# 9. Crear funko inválido (400)
$badBody = @{
    name = ""
    precio = -5
    categoriaId = ""
    proveedorId = "invalido"
    stock = -1
    imagen = ""
}
$resp = Invoke-ApiRequest -Method Post -Path "/api/funkos" -Body $badBody
Validate-Test -Name "Crear funko inválido (400)" -ExpectedStatus 400 -Response $resp

# 10. Actualizar funko inexistente (404)
$resp = Invoke-ApiRequest -Method Put -Path "/api/funkos/99999" -Body $createBody
Validate-Test -Name "Actualizar funko inexistente (404)" -ExpectedStatus 404 -Response $resp

# 11. Eliminar funko inexistente (404)
$resp = Invoke-ApiRequest -Method Delete -Path "/api/funkos/99999"
Validate-Test -Name "Eliminar funko inexistente (404)" -ExpectedStatus 404 -Response $resp

Write-Host ""

# =========================================================================
# CATEGORÍAS
# =========================================================================
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "🏷️  CATEGORÍAS" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# 1. Listar categorías
$resp = Invoke-ApiRequest -Method Get -Path "/api/categorias?page=0&size=10"
Validate-Test -Name "Listar categorías (200)" -ExpectedStatus 200 -Response $resp

# 2. Obtener categoría por id
$resp = Invoke-ApiRequest -Method Get -Path "/api/categorias/$script:categoriaId"
Validate-Test -Name "Obtener categoría por id (200)" -ExpectedStatus 200 -Response $resp

# 3. Obtener categoría inexistente (404)
$resp = Invoke-ApiRequest -Method Get -Path "/api/categorias/NoExiste001"
Validate-Test -Name "Obtener categoría inexistente (404)" -ExpectedStatus 404 -Response $resp

# 4. Crear categoría (201)
$catBody = @{
    nombre = "Categoria Test PS"
    descripcion = "Creada por PowerShell"
}
$resp = Invoke-ApiRequest -Method Post -Path "/api/categorias" -Body $catBody
Validate-Test -Name "Crear categoría (201)" -ExpectedStatus 201 -Response $resp -ExtraChecks {
    param($Response)
    $errors = @()
    if ($Response.Content) {
        $body = $Response.Content | ConvertFrom-Json
        if ($body.id) {
            $script:categoriaIdCreada = $body.id
        } else {
            $errors += "No se obtuvo id en la respuesta"
        }
    } else {
        $errors += "Respuesta vacía"
    }
    $errors
}

# 5. Crear categoría inválida (400)
$badCat = @{
    nombre = ""
    descripcion = "Sin nombre"
}
$resp = Invoke-ApiRequest -Method Post -Path "/api/categorias" -Body $badCat
Validate-Test -Name "Crear categoría inválida (400)" -ExpectedStatus 400 -Response $resp

# 6. Actualizar categoría (200)
if ($script:categoriaIdCreada) {
    $updateCat = @{
        nombre = "Categoria Actualizada PS"
        descripcion = "Actualizada por PowerShell"
    }
    $resp = Invoke-ApiRequest -Method Put -Path "/api/categorias/$script:categoriaIdCreada" -Body $updateCat
    Validate-Test -Name "Actualizar categoría (200)" -ExpectedStatus 200 -Response $resp

    # 7. Eliminar categoría inexistente (404)
    $resp = Invoke-ApiRequest -Method Delete -Path "/api/categorias/NoExiste001"
    Validate-Test -Name "Eliminar categoría inexistente (404)" -ExpectedStatus 404 -Response $resp

    # 8. Eliminar categoría creada (204)
    $resp = Invoke-ApiRequest -Method Delete -Path "/api/categorias/$script:categoriaIdCreada"
    Validate-Test -Name "Eliminar categoría creada (204)" -ExpectedStatus 204 -Response $resp
}

# 9. Actualizar categoría inválida (400)
$badUpdate = @{
    nombre = ""
    descripcion = "Sin nombre"
}
$resp = Invoke-ApiRequest -Method Put -Path "/api/categorias/$script:categoriaId" -Body $badUpdate
Validate-Test -Name "Actualizar categoría inválida (400)" -ExpectedStatus 400 -Response $resp

Write-Host ""

# =========================================================================
# RESUMEN
# =========================================================================
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "📊 RESUMEN" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ Pasaron:  $script:Passed" -ForegroundColor Green
Write-Host "❌ Fallaron: $script:Failed" -ForegroundColor Red
Write-Host ""

if ($script:Failed -eq 0) {
    Write-Host "🎉 ¡Todas las pruebas pasaron!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "⚠️  Algunas pruebas fallaron. Revisa los errores arriba." -ForegroundColor Yellow
    exit 1
}
