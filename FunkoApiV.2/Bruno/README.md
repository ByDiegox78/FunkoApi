# Colección Bruno — FunkoApi

Colección de pruebas HTTP automatizadas para los endpoints REST de FunkoApi.

## Requisitos

- API ejecutándose en Development.
- `Bruno/environments/Local.bru` apunta por defecto a `http://localhost:5798`.
- Bruno CLI (`bru --version`).
- Variables `FUNKO_ADMIN_EMAIL` y `FUNKO_ADMIN_PASSWORD` para ejecutar la
  colección desde CLI.

Arranca la API y sus dependencias desde la raíz del proyecto:

```powershell
.\Start.ps1
```

O ejecuta el flujo completo de Bruno, que arranca la API, espera y lanza la
colección:

```powershell
.\Start.ps1 -Bruno
```

## Ejecutar

Desde la raíz, después de definir credenciales ADMIN para la sesión:

```powershell
$env:FUNKO_ADMIN_EMAIL = "admin@admin.com"
$env:FUNKO_ADMIN_PASSWORD = "Admin123!"
.\Start.ps1 -Bruno
Remove-Item Env:FUNKO_ADMIN_EMAIL, Env:FUNKO_ADMIN_PASSWORD
```

O ejecuta directamente Bruno CLI:

```powershell
Push-Location .\Bruno
bru run -r --env-file .\environments\Local.bru
Pop-Location
```

La cuenta de ejemplo corresponde al seeder Development. Utiliza la contraseña
actual si la cambiaste y nunca ejecutes estas pruebas contra producción.

Para ejecutar también los tests .NET, usa `.\Test-All.ps1` desde la raíz. Más
detalles: [`doc/20-bruno-testing.md`](../doc/20-bruno-testing.md).

## Flujo y variables

La colección está numerada para que Bruno ejecute setup antes del resto:

1. `0 - Setup`: login, captura del JWT y creación de proveedor de prueba.
2. `1 - Categorias`: CRUD, validaciones y respuestas de conflicto/no encontrado.
3. `2 - Funkos`: CRUD, filtros, carga de imagen y validaciones.
4. `3 - Proveedores`: lecturas y actualización del proveedor de prueba.
5. `90 - Teardown`: elimina el proveedor de prueba.

El JWT y los IDs creados se guardan como variables durante la ejecución. No
escribas tokens ni contraseñas en archivos de entorno `.bru`.
