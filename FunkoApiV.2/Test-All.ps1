[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5798"
)

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$testProject = Join-Path $projectRoot "FunkoApiV2Test\FunkoApiV2Test.csproj"
$collectionPath = Join-Path $projectRoot "Bruno"
$environmentFile = Join-Path $collectionPath "environments\Local.bru"
$temporaryEnvironmentFile = Join-Path ([System.IO.Path]::GetTempPath()) "FunkoApi-Bruno-$PID.bru"

foreach ($requiredPath in @($testProject, $collectionPath, $environmentFile)) {
    if (!(Test-Path -LiteralPath $requiredPath)) {
        throw "No se encontró el recurso requerido: $requiredPath"
    }
}

if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontró dotnet en PATH. Instala el SDK definido por el proyecto."
}

$bruno = Get-Command bru -ErrorAction SilentlyContinue

try {
    & docker info --format '{{.ServerVersion}}' 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker no está disponible. Inicia Docker Desktop para ejecutar los tests de integración."
    }
} catch {
    throw "Docker no está disponible. Inicia Docker Desktop para ejecutar los tests de integración."
}

try {
    $health = Invoke-WebRequest -Uri "$($BaseUrl.TrimEnd('/'))/api/funkos?page=0&size=1" -TimeoutSec 5
    if ($health.StatusCode -ne 200) {
        throw "La API respondió con HTTP $($health.StatusCode)."
    }
} catch {
    throw "No se puede acceder a la API en $BaseUrl. Arráncala primero con .\Start-Development.ps1. Detalle: $($_.Exception.Message)"
}

$previousEmail = $env:FUNKO_ADMIN_EMAIL
$previousPassword = $env:FUNKO_ADMIN_PASSWORD
$passwordPointer = [IntPtr]::Zero

try {
    if ([string]::IsNullOrWhiteSpace($env:FUNKO_ADMIN_EMAIL)) {
        $env:FUNKO_ADMIN_EMAIL = Read-Host "Email de una cuenta ADMIN para Bruno"
    }
    if ([string]::IsNullOrWhiteSpace($env:FUNKO_ADMIN_PASSWORD)) {
        $securePassword = Read-Host "Contraseña de la cuenta ADMIN" -AsSecureString
        $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
        $env:FUNKO_ADMIN_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    }
    if ([string]::IsNullOrWhiteSpace($env:FUNKO_ADMIN_EMAIL) -or
        [string]::IsNullOrWhiteSpace($env:FUNKO_ADMIN_PASSWORD)) {
        throw "Se necesitan FUNKO_ADMIN_EMAIL y FUNKO_ADMIN_PASSWORD."
    }

    Write-Host "`nEjecutando tests unitarios y de integración..."
    & dotnet test $testProject -p:UseAppHost=false
    if ($LASTEXITCODE -ne 0) {
        throw "Los tests de .NET han fallado (código $LASTEXITCODE)."
    }

    Write-Host "`nEjecutando la colección Bruno..."
    $brunoEnvironment = [System.IO.File]::ReadAllText($environmentFile)
    if (![System.Text.RegularExpressions.Regex]::IsMatch($brunoEnvironment, "(?m)^\s*baseUrl:\s*\S+")) {
        throw "No se encontró la variable baseUrl en $environmentFile."
    }
    $updatedEnvironment = [System.Text.RegularExpressions.Regex]::Replace(
        $brunoEnvironment,
        "(?m)^\s*baseUrl:\s*\S+",
        "  baseUrl: $($BaseUrl.TrimEnd('/'))")
    [System.IO.File]::WriteAllText(
        $temporaryEnvironmentFile,
        $updatedEnvironment,
        [System.Text.UTF8Encoding]::new($false))

    Push-Location $collectionPath
    try {
        if ($bruno) {
            & $bruno.Source run -r --env-file $temporaryEnvironmentFile
        } else {
            npx --yes @usebruno/cli run -r --env-file $temporaryEnvironmentFile
        }
        if ($LASTEXITCODE -ne 0) {
            throw "Los tests de Bruno han fallado (código $LASTEXITCODE)."
        }
    } finally {
        Pop-Location
    }

    Write-Host "`nTodos los tests de .NET y Bruno han terminado correctamente." -ForegroundColor Green
} finally {
    if ($passwordPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
    if ($null -eq $previousEmail) {
        Remove-Item Env:FUNKO_ADMIN_EMAIL -ErrorAction SilentlyContinue
    } else {
        $env:FUNKO_ADMIN_EMAIL = $previousEmail
    }
    if ($null -eq $previousPassword) {
        Remove-Item Env:FUNKO_ADMIN_PASSWORD -ErrorAction SilentlyContinue
    } else {
        $env:FUNKO_ADMIN_PASSWORD = $previousPassword
    }
    Remove-Item -LiteralPath $temporaryEnvironmentFile -ErrorAction SilentlyContinue
}
