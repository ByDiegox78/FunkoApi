# 04 — Controladores REST

## Rutas principales

| Recurso | Ruta | Nota |
|---|---|---|
| Funkos | `/api/funkos` | ID `long`; filtros y paginación en query |
| Categorías | `/api/categorias` | ID string de 11 caracteres |
| Proveedores | `/api/Proveedor` | ID Mongo ObjectId de 24 caracteres hex |
| Autenticación | `/api/auth` | Registro, login y usuario actual |

El código real está en `FunkoApiV.2/Controller/`. Los métodos declaran códigos
de respuesta para Swagger y delegan la lógica a servicios.

## Autenticación

Las lecturas de listas de Funkos y categorías son públicas; la lectura de un
Funko por ID y todas las acciones actuales de Proveedor requieren `ADMIN`.
Las escrituras principales también requieren ese rol. La carga de imagen está
marcada como pública en el controlador actual. Revisa los atributos
`[Authorize]` de cada acción antes de asumir permisos.

## Respuestas y validación

- Creación correcta: HTTP 201 cuando corresponde.
- Lectura/actualización correcta: HTTP 200.
- Borrado correcto: HTTP 204.
- Entrada no válida: HTTP 400 con `ApiErrorResponse`.
- Errores de negocio se convierten en estados HTTP desde
  `Error/DomainErrorExtensions.cs`.
- Subida de archivo utiliza `multipart/form-data`; los parámetros `IFormFile`
  no se marcan conjuntamente con `[FromForm]` en la acción para permitir la
  generación correcta del documento Swagger.

La referencia de ejecución es Swagger en Development:
<http://localhost:5798/swagger>. Para levantar la API, sigue
[la guía práctica](00-guia-practica.md).
