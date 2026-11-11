# 05 — Validación de solicitudes

## Capas de validación actuales

1. ASP.NET Core valida el model binding y las anotaciones del DTO.
2. `Program.cs` convierte fallos del estado del modelo en HTTP 400 con
   `ApiErrorResponse`, mensaje común y errores agrupados por campo.
3. FluentValidation aplica reglas de cada solicitud; los validators se
   registran desde el ensamblado de la API.
4. Los servicios verifican reglas de negocio y relaciones que requieren leer
   datos, como existencia de categoría y proveedor.
5. Las restricciones del almacén protegen los datos persistidos.

## Validadores

- `Validator/FunkoCreateValidator.cs`: nombre, precio, stock, IDs relacionados e imagen.
- `Validator/FunkoUpdateValidator.cs`: reglas equivalentes para actualizar.
- `Validator/Categoria/CategoriaValidator.cs`: reglas de categorías.
- `Validator/Proveedor/ProveedorValidator.cs`: nombre y dirección.

Consulta estas clases antes de documentar un límite: las reglas pueden cambiar.
Por ejemplo, los nombres y direcciones de proveedor tienen un máximo actual de
20 caracteres.

## Añadir o cambiar una regla

1. Localiza el DTO y su validator.
2. Decide si es una regla de forma (validator) o una regla de negocio
   dependiente de datos (servicio).
3. Devuelve un mensaje claro y coherente con los demás errores.
4. Añade o actualiza el test de `FunkoApiV2Test/Unit/Validators/`.
5. Valida también el contrato HTTP si afecta el status o el cuerpo de respuesta.

Las reglas efectivas están en los validators y DTOs actuales; los ejemplos
antiguos de este apunte no deben copiarse a ciegas.
