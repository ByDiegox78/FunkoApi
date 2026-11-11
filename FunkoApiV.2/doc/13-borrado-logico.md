# 13 — Borrado lógico y reglas de eliminación

## Datos de negocio

- **Funko:** el endpoint DELETE marca el registro como eliminado; las consultas
  normales excluyen los eliminados.
- **Categoría:** se marca como eliminada solo cuando no tiene Funkos asociados.
  Si existe alguno, la API responde `409 Conflict`.
- **Proveedor:** usa operaciones de baja lógica propias del servicio MongoDB y
  expone una consulta separada para proveedores eliminados.

Los detalles de filtrado están en los repositorios y servicios. Antes de
modificar `IsDeleted`, comprueba filtros, caché y consultas cruzadas en
PostgreSQL/MongoDB/Cassandra.

## Datos de Production

Production **no borra datos al arrancar**. `Program.cs` solo aplica el esquema y
aprovisiona el administrador definido en `BootstrapAdmin__*` si no existe. No se
ejecuta ningún limpiador de tablas, colecciones o caché.

Si necesitas vaciar datos para una demostración, hazlo de forma explícita y
consciente, por ejemplo recreando el volumen de PostgreSQL (`down -v`) o
ejecutando SQL a mano sobre una copia. Nunca lo dejes como efecto colateral del
arranque.

Los volúmenes Docker `funkoapiv2_*` son persistentes. `.\Start.ps1 -Production
-Down` para los contenedores sin borrar los datos. Consulta
[Docker](19-docker.md) antes de desplegar.
