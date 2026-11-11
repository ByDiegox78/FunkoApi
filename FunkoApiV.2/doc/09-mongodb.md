# 09 — MongoDB y proveedores

## Uso actual

MongoDB almacena proveedores en la colección `proveedor`. La entidad y el
contexto se encuentran en `FunkoApiV.2/Models/Proveedor.cs` y los componentes
de `Repository/Proveedor/`. El contexto se configura con
`MongoDB.EntityFrameworkCore` y los valores `ConnectionStrings:Mongo` y
`DatabaseSettings:Name`.

El proveedor se relaciona con un Funko mediante un ObjectId serializado como
cadena de 24 caracteres hexadecimales. No hay transacción distribuida entre
MongoDB y PostgreSQL; el servicio valida la existencia antes de crear o
actualizar un Funko.

## Operaciones y precauciones

El servicio de proveedores permite altas, lecturas, actualizaciones y baja
lógica. Al añadir operaciones, invalida las claves de caché relacionadas.
Production **no** limpia la colección `proveedor` al iniciar: los datos se
conservan en el volumen de MongoDB. Solo Development inserta proveedores de
ejemplo.

Para el modelo exacto y los filtros, usa el código actual en
`Models/Proveedor.cs`, `Repository/Proveedor/ProveedorRepository.cs` y
`Service/Proveedor/ProveedorService.cs`.
