# 12 — Servicios de negocio

## Responsabilidades

Los servicios coordinan validación, reglas de negocio, repositorios, mapeo,
caché y notificaciones. Sus interfaces e implementaciones están bajo
`FunkoApiV.2/Service/`. Los controladores no deben consultar directamente los
repositorios.

Ejemplos de reglas gestionadas por servicios:

- Verificar que la categoría y el proveedor existen antes de asociarlos a un
  Funko.
- No borrar una categoría que todavía tenga Funkos asociados (409).
- Validar solicitudes mediante FluentValidation.
- Guardar imágenes y publicar notificaciones relacionadas con cambios.
- Devolver errores tipados, que el controlador traduce a HTTP.

## Decoradores

`DependencerProvider.AddDependencies` decora los servicios de Funkos,
categorías y proveedores con implementaciones cacheadas. Al añadir una regla
que cambia datos, revisa qué claves de caché y eventos SignalR/GraphQL deben
actualizarse. No añadas efectos secundarios silenciosos ante fallos.

## Cómo cambiar un servicio

1. Revisa la interfaz y la implementación actuales.
2. Identifica los repositorios y validadores existentes que ya cubren la regla.
3. Devuelve un error de dominio para fallos esperados; reserva excepciones para
   fallos inesperados.
4. Añade tests unitarios del servicio y, si toca persistencia, tests de
   integración.

`Service/Email/EmailService.cs` envía un correo de bienvenida al registrarse un
usuario. En Development se conecta a Mailpit (`localhost:1025`); abre
<http://localhost:8025> para consultar los mensajes capturados. Los ajustes
SMTP se enlazan desde la sección `Smtp`; producción debe proporcionar sus
propias variables de entorno y no reutilizar credenciales de desarrollo.
