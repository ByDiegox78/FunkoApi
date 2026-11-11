# 07 — Patrón repositorio

## Responsabilidad

Los servicios dependen de interfaces de repositorio y no manipulan directamente
las consultas de almacenamiento. Las interfaces e implementaciones están
organizadas por agregado en `Repository/Funko`, `Repository/Categoria`,
`Repository/Proveedor` y `Repository/Cassandra`.

| Almacén | Uso |
|---|---|
| PostgreSQL / EF Core | Funkos, categorías e Identity |
| MongoDB | Proveedores |
| Cassandra | Modelos de lectura agregados para consultas |

## Reglas al modificar repositorios

- Mantén la lógica HTTP fuera del repositorio.
- Mantén las reglas de negocio entre varias entidades en el servicio.
- Proyecta/página en la consulta para evitar cargar resultados completos.
- Respeta filtros de baja lógica y relaciones existentes.
- Propaga errores de infraestructura de forma explícita; no los transformes en
  un resultado de éxito vacío.
- Añade pruebas de repositorio/integración cuando cambie el comportamiento de
  almacenamiento.

Los nombres y tipos de retorno reales se definen en `IFunkoRepository`,
`ICategoriaRepository`, `IProveedorRepository` e `ICassandraRepository`; usa
esas interfaces en lugar de firmas copiadas de ejemplos previos.
