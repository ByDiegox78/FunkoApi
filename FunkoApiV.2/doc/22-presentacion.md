# 22 — Guion de presentación y demostración

Guion breve para presentar FunkoApi en clase. Está pensado para ejecutarse en
Development desde Windows y complementa los apuntes técnicos: arquitectura por
capas, REST, validación, persistencia, autenticación, pruebas y comunicación en
tiempo real.

## Idea principal

FunkoApi es una API para gestionar un catálogo de Funkos. Ofrece tres formas de
interacción —REST, GraphQL y SignalR— y distribuye los datos según su uso:
PostgreSQL para Funkos, categorías e Identity; MongoDB para proveedores;
Cassandra para consultas preparadas; Redis para caché. En desarrollo, los
correos de bienvenida se capturan con Mailpit, sin enviarlos a destinatarios
reales.

## Preparación antes de la exposición

1. Inicia Docker Desktop y, desde la raíz del repositorio, ejecuta
   `.\Start.ps1`.
2. Espera a que aparezca `Now listening on: http://localhost:5798`.
3. Comprueba en otra pestaña Swagger (`/swagger`), GraphQL (`/graphql`), el
   cliente SignalR (`/`) y Mailpit (`http://localhost:8025`).
4. Inicia sesión con la cuenta ADMIN local descrita en
   [la guía práctica](00-guia-practica.md#3-cuentas-de-desarrollo) y comprueba
   que Swagger acepta el token.
5. Si se va a mostrar una mutación o una notificación en SignalR, prepara un
   Funko de prueba y ensaya el recorrido completo antes de la exposición.

**No uses Production para la exposición:** no carga datos de ejemplo ni cuentas
de demostración, y Swagger y el explorador GraphQL están deshabilitados.
Production conserva los datos y solo crea el admin configurado. Consulta
[Docker](19-docker.md) para el detalle.

## Orden sugerido (6–8 minutos)

### 1. Problema y objetivo

> “Queríamos construir una API de catálogo que no dependiera de una sola forma
> de consulta, que separase responsabilidades y que se pudiera probar y ejecutar
> de forma reproducible en desarrollo.”

Muestra el README y resume las entidades principales: Funkos, categorías,
proveedores y usuarios.

### 2. Arquitectura y tecnologías

Explica el recorrido habitual de una petición:

`Cliente → controlador/API → servicio de negocio → repositorio → persistencia`

Los controladores exponen contratos HTTP y DTOs; los servicios coordinan reglas
de negocio y validaciones; los repositorios encapsulan el acceso a datos. La
solución combina PostgreSQL, MongoDB, Cassandra y Redis para responsabilidades
distintas. Señala en el código solo un ejemplo concreto de cada capa, sin
intentar recorrer todas las carpetas.

### 3. REST y autenticación con Swagger

1. Abre <http://localhost:5798/swagger>.
2. Ejecuta `POST /api/auth/login` con la cuenta ADMIN de Development.
3. Copia el campo `token`, pulsa **Authorize** y pega el token sin añadir
   `Bearer`.
4. Ejecuta una lectura paginada de Funkos, por ejemplo
   `GET /api/funkos?page=0&size=5`.
5. Si se quiere enseñar escritura, crea o actualiza un registro de prueba con
   una operación marcada como protegida por `ADMIN`.

Explica que Swagger describe rutas REST, parámetros, cuerpos, respuestas y
autenticación; no es el explorador de GraphQL ni una herramienta para conectarse
al hub SignalR.

### 4. GraphQL

Abre <http://localhost:5798/graphql> y ejecuta una consulta de lectura de
[los ejemplos GraphQL](21-GRAPHQL-EJEMPLOS.md). Comenta que el cliente
selecciona los campos que necesita. Si hay tiempo, muestra una suscripción
WebSocket y provoca el evento mediante una mutación autorizada en otra pestaña.

El explorador visual solo está habilitado en Development. Las operaciones
GraphQL HTTP y WebSocket usan `/graphql`; SignalR es un canal distinto en
`/hubs/funkos`.

### 5. SignalR

1. Abre <http://localhost:5798/>. Esta página de demostración solo se sirve en
   Development.
2. Inicia sesión con ADMIN y pulsa **Conectar**.
3. Deja esa pestaña abierta y crea un Funko de prueba desde Swagger.
4. Comprueba que la notificación `FunkoCreado` aparece en la página.

La página carga el cliente SignalR desde una CDN, por lo que la demo necesita
conexión a Internet. Si no hay red disponible, enseña la prueba unitaria de
`FunkosHub` o una captura preparada en vez de depender de una conexión en vivo.

### 6. Email y entorno de desarrollo

Muestra <http://localhost:8025> y explica que el email de bienvenida se captura
localmente con Mailpit: no se necesita una plantilla ni se envía el mensaje al
exterior. En Development se cargan datos y cuentas de ejemplo al arrancar. Si
SMTP falla durante el registro, el fallo se registra y no cancela la creación
de la cuenta.

### 7. Calidad y cierre

Menciona FluentValidation, respuestas HTTP coherentes, autorización por roles,
paginación e índices PostgreSQL. Enseña la colección Bruno o el resultado de
`.\Test-All.ps1` como evidencia de pruebas automatizadas.

> “El proyecto permite comparar REST, GraphQL y comunicación en tiempo real
> sobre un mismo dominio, manteniendo las reglas y el acceso a datos en capas
> separadas.”

## Qué no conviene afirmar

- **No afirmar que está listo para producción sin revisar la configuración.**
  Production conserva los datos y crea el admin de `BootstrapAdmin__*` si no
  existe, pero necesita `.env.production` con secretos propios, CORS real y SMTP
  válido. El arranque se hace con `.\Start.ps1 -Production`.
- **No afirmar que GraphQL o SignalR aparecen en Swagger.** Swagger describe
  las acciones REST; sus protocolos se prueban por separado.
- **No afirmar que existe Output Cache HTTP.** Redis se usa como caché de
  aplicación; Output Cache sigue siendo opcional y no está configurado.
- **No llamar a esto “un único contenedor”.** Hay un único comando de arranque,
  pero Compose coordina la API y varios servicios en contenedores distintos.
- **No depender de la suscripción `onStockBajo` para recibir eventos:** está
  declarada, pero actualmente no se publica ese evento.

## Plan de respaldo

- Si falla una base de datos o Docker tarda en arrancar, no cambies a Production
  a mitad de la demo: no tiene datos de ejemplo ni Swagger. Revisa
  `docker compose ps` y los logs de los servicios.
- Si Swagger no carga, comprueba `/swagger/v1/swagger.json` y la salida de la
  terminal de la API.
- Si falla una operación de escritura, vuelve a una consulta de lectura ya
  preparada; no uses registros importantes como datos de prueba.
- Guarda capturas del flujo y del resultado de tests para poder explicar el
  proyecto aunque el entorno del aula no tenga conexión o Docker disponible.

## Referencias del proyecto

- [Guía práctica de arranque y cuentas de desarrollo](00-guia-practica.md)
- [Documentación REST y buenas prácticas](06-rest-best-practices.md)
- [Autenticación JWT](15-jwt-authentication.md)
- [Seguridad HTTP, Swagger e índices](17-seguridad-http.md)
- [Testing .NET](18-testing.md) y [Bruno](20-bruno-testing.md)
- [Ejemplos GraphQL](21-GRAPHQL-EJEMPLOS.md)
