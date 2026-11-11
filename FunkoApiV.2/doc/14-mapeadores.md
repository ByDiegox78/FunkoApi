# 14 — Mapeadores

## Convención actual

El proyecto utiliza mapeadores manuales en `FunkoApiV.2/Mapper/` para convertir
modelos y DTOs. Por ejemplo, `Mapper/Funko/FunkoMapper.cs` define conversiones
entre `Funko`, solicitudes y DTOs de respuesta. También existen mapeadores para
categorías, proveedores y modelos Cassandra.

Al añadir campos:

1. Actualiza el DTO y el mapeador correspondiente.
2. Revisa consultas con proyección para incluir los datos necesarios.
3. Actualiza pruebas unitarias del mapeo y pruebas de API/servicio que validen
   la respuesta.

No se debe afirmar que AutoMapper es la fuente de verdad: los mapeadores que
usa esta API son manuales. Usa las propiedades y constructores actuales de los
DTOs; los modelos cambian con el código.
