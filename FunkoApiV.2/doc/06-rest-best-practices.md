# 06 — REST: contrato y buenas prácticas

## Recursos y formatos de ID

- Funko: `long` incremental en PostgreSQL.
- Categoría: cadena de 11 caracteres.
- Proveedor: ObjectId Mongo de 24 caracteres hexadecimales.

Las rutas y permisos exactos están documentados en los controladores de
`FunkoApiV.2/Controller/` y en Swagger.

La lectura de Funkos/categorías es pública, mientras que las acciones del
controlador de Proveedores requieren `ADMIN`, también sus lecturas.

## Métodos y respuestas

| Método | Uso | Respuesta habitual |
|---|---|---|
| GET | Listar / consultar | 200 |
| POST | Crear | 201 |
| PUT | Actualizar | 200 |
| PATCH | Reemplazar imagen | 200 |
| DELETE | Borrado lógico o eliminación según entidad | 204 |

Una categoría con Funkos asociados no se puede borrar y devuelve 409. Las
solicitudes inválidas devuelven 400. La API utiliza paginación en Funkos y
categorías; los límites y nombres de query parameters están en
`Dto/Funko/FunkoFilterRequest.cs` y `Dto/Categoria/`.

## Filtros, orden y coste

El repositorio construye consultas filtradas en PostgreSQL; los filtros de
proveedor cruzan datos Mongo/Cassandra según la operación. Evita cargar todas
las filas si la API ofrece página y tamaño. Los índices y la inicialización de
esquema se describen en [Seguridad HTTP](17-seguridad-http.md).

## Eliminación

Funkos y categorías utilizan `IsDeleted` y consultas que excluyen eliminados por
defecto. Proveedores también disponen de una operación de baja lógica propia.
Consulta [Borrado lógico](13-borrado-logico.md) antes de cambiar esta semántica.

## Imágenes

La API guarda referencias/rutas, no el binario de imagen dentro de PostgreSQL.
Las acciones de carga reciben `multipart/form-data`; Swagger permite explorar
el contrato en Development.
