# Guía práctica de arquitectura y cambios

Esta guía explica cómo está organizado FirmaOperaCloud, qué función cumple OpenAPI
y qué archivos deben modificarse para realizar los cambios más comunes sin repartir
una misma responsabilidad por lugares incorrectos.

## 1. Qué tipo de aplicación es

FirmaOperaCloud es un monolito web compuesto por dos aplicaciones que se publican
juntas:

- Angular presenta las pantallas y captura la información del usuario.
- ASP.NET Core autentica, autoriza y publica la API HTTP.
- SQL Server conserva usuarios, tarjetas, firmas, documentos y auditoría.
- Infrastructure conecta la aplicación con OPERA Cloud, OCR, PDF y correo.

En desarrollo, Angular y ASP.NET se ejecutan por separado:

```text
Navegador -> Angular :4200 -> proxy /api -> ASP.NET :5016
```

Al publicar, ASP.NET incluye el resultado compilado de Angular en `wwwroot` y ambos
se entregan desde el mismo origen:

```text
Navegador -> ASP.NET -> archivos Angular
                     -> /api/*
```

No es una arquitectura de microservicios. Para el tamaño y los flujos actuales,
mantener un solo despliegue es más sencillo y apropiado.

## 2. Proyectos del backend

### `FirmaOperaCloud.Domain`

Contiene los conceptos centrales del negocio y las entidades persistidas, por
ejemplo reservaciones, tarjetas, firmas, plantillas, usuarios y auditoría.

Idealmente, también debe contener las reglas que siempre deben cumplirse, sin
depender de HTTP, SQL Server, OPERA o Angular.

### `FirmaOperaCloud.Application`

Actualmente contiene contratos como `IReservationService`, `IOcrService` e
`IAuditService`. Su función prevista es coordinar casos de uso, por ejemplo:

- consultar una reservación;
- generar y firmar una tarjeta;
- sellar un expediente;
- procesar una identificación;
- agregar un acompañante en OPERA.

Esta capa todavía está incompleta: varios de esos flujos se coordinan directamente
en los controladores. Cuando se extraiga lógica de un controlador, debe colocarse
aquí como un caso de uso organizado por funcionalidad.

### `FirmaOperaCloud.Infrastructure`

Contiene implementaciones dependientes de tecnología:

- `Persistence`: Entity Framework Core y SQL Server;
- `Opera`: autenticación y peticiones a OPERA Cloud;
- `Ocr`: Tesseract y lectura de identificaciones;
- `Pdf`: generación y llenado de documentos PDF.

Una regla útil es: si mañana se puede sustituir SQL Server, Tesseract u OPERA por
otro proveedor, el código específico del proveedor debe estar aquí.

### `FirmaOperaCloud.Api`

Es el punto de entrada. Contiene:

- controladores y contratos HTTP;
- autenticación, cookies, CSRF y políticas de autorización;
- middleware de auditoría;
- registro de dependencias;
- configuración del arranque;
- servicios que todavía no han sido trasladados a Application o Infrastructure.

Un controlador debería recibir la petición, validar el contrato HTTP, llamar un
caso de uso y convertir el resultado en una respuesta. No debería concentrar un
flujo de negocio completo.

### `FirmaOperaCloud.Shared`

Actualmente sólo contiene `OperaCloudOptions`. No debe convertirse en una carpeta
general para código que no se sabe dónde colocar. En una simplificación futura,
esas opciones pueden trasladarse a Infrastructure o API y eliminar este proyecto.

## 3. Organización del frontend Angular

Las carpetas principales son:

```text
web-angular/src/app/
|-- core/       autenticación, cliente HTTP y modelos compartidos
|-- features/   pantallas organizadas por funcionalidad
|-- shared/     componentes visuales reutilizados
|-- app.routes.ts
|-- app.html
`-- app.css
```

Cada carpeta de `features` corresponde a una capacidad visible, como tarjetas,
expedientes, OCR, plantillas o usuarios. Esta organización es correcta y debe
conservarse.

Cuando una funcionalidad crezca, puede dividirse internamente:

```text
features/tarjeta/
|-- pages/
|-- components/
|-- data-access/
`-- models/
```

No hace falta crear estas subcarpetas para una pantalla pequeña. Se justifican
cuando un archivo mezcla una página completa, formularios, llamadas HTTP y varios
componentes visuales.

## 4. Qué es OpenAPI y cómo se usa aquí

OpenAPI es un formato estándar que describe una API. No está relacionado con
OpenAI ni requiere inteligencia artificial.

ASP.NET y Swashbuckle examinan los controladores y generan un documento parecido a:

```json
{
  "path": "/api/reservations/{confirmationNumber}",
  "method": "GET",
  "response": "Reservation[]"
}
```

Swagger UI presenta ese documento como una página interactiva. En desarrollo suele
estar disponible en:

```text
http://localhost:5016/swagger
```

El documento JSON normalmente se encuentra en:

```text
http://localhost:5016/swagger/v1/swagger.json
```

`ng-openapi-gen` lee ese JSON y genera funciones y tipos TypeScript dentro de:

```text
web-angular/src/app/core/generated/
```

El flujo es:

```text
Controladores y DTO de ASP.NET
              |
              v
        documento OpenAPI
              |
              v
 cliente TypeScript generado
              |
              v
            Angular
```

### Regla principal

No se deben editar manualmente los archivos de `core/generated`. Cada generación
puede reemplazarlos por completo. El origen de esos archivos son los controladores,
los DTO y la configuración OpenAPI del backend.

### Cómo regenerar el cliente

1. Ejecutar la API con el perfil que escucha en el puerto 5016.
2. Entrar en `web-angular`.
3. Ejecutar:

```powershell
npm run api:generate
```

4. Revisar los cambios generados.
5. Compilar Angular y corregir usos incompatibles.

La dirección del documento se configura en `web-angular/ng-openapi-gen.json`.

### Estado de transición actual

La aplicación todavía consume la API principalmente mediante:

```text
web-angular/src/app/core/api.service.ts
web-angular/src/app/core/models.ts
```

Estos dos archivos son manuales. Al mismo tiempo existe el cliente generado. Hasta
terminar la transición, un cambio de contrato puede requerir actualizar el backend,
regenerar OpenAPI y ajustar también el modelo o método manual que siga usando la
pantalla.

El destino recomendado es que cada funcionalidad consuma el cliente generado, de
forma directa o mediante un adaptador pequeño propio de la funcionalidad. Cuando
ninguna pantalla use un método manual, éste puede retirarse de `ApiService`.

## 5. Mapa rápido: qué debo cambiar y dónde

| Quiero cambiar | Lugar principal | Otros lugares que debo revisar |
|---|---|---|
| Texto, controles o distribución de una pantalla | `web-angular/src/app/features/<funcionalidad>/` | CSS global sólo si el estilo se reutiliza |
| Colores y apariencia general | `web-angular/src/vidanta-design.css` | `src/styles.css` y `app.css` |
| Nombre u orden de una opción del menú | `web-angular/src/app/app.html` | Permiso asociado y ruta |
| URL de una pantalla | `web-angular/src/app/app.routes.ts` | Enlaces `routerLink` y redirecciones |
| Orden de resultados | Consulta del backend si debe ser universal | Componente Angular si sólo es presentación local |
| Campo visible que ya existe en la respuesta | Componente de la funcionalidad | Modelo TypeScript manual durante la transición |
| Campo nuevo calculado | Caso de uso/DTO del backend | OpenAPI y pantalla Angular |
| Campo nuevo persistido | Entidad Domain y `DbContext` | Migración, caso de uso, DTO, OpenAPI y Angular |
| Endpoint nuevo | Controlador API y caso de uso Application | Registro DI, autorización, OpenAPI y Angular |
| Dato obtenido de OPERA | `Infrastructure/Opera` | Contrato Application, DTO API y Angular |
| Diseño de un PDF generado por código | `Infrastructure/Pdf` | Caso de uso que lo invoca |
| Plantilla PDF configurable | Pantalla `features/plantillas` y API de plantillas | Entidades y persistencia si cambia su estructura |
| Permiso nuevo | Backend `ViewPermissions` y `Program.cs` | Modelo Angular, ruta y menú |
| Variable de conexión con OPERA | `OperaCloudOptions` y variables de entorno | `.env.example`, Compose y documentación |
| Regla de negocio | Domain o caso de uso Application | Pruebas y respuesta HTTP correspondiente |

## 6. Cambios de interfaz visual

Aquí “interfaz” significa la pantalla que ve el usuario.

### Cambiar una pantalla existente

Primero se localiza su ruta en `web-angular/src/app/app.routes.ts`. La propiedad
`loadComponent` indica el archivo de la funcionalidad.

Ejemplo:

```typescript
{
  path: 'plantillas-pdf',
  loadComponent: () => import('./features/plantillas/plantillas')
}
```

La pantalla se cambia en:

```text
web-angular/src/app/features/plantillas/plantillas.ts
```

Algunas pantallas tienen `templateUrl` y un HTML separado. Otras tienen una plantilla
inline mediante ``template: `...` ``. Si una plantilla inline empieza a dificultar
la lectura, debe moverse a un archivo `.html` en la misma carpeta y cambiarse a:

```typescript
templateUrl: './nombre.html'
```

La lógica de carga, guardado y estado permanece en el archivo `.ts`. Los bloques
visuales reutilizables se convierten en componentes pequeños.

### Cambiar el menú o su orden

El menú lateral vive en:

```text
web-angular/src/app/app.html
```

El orden de los enlaces en ese archivo es el orden que verá el usuario. Mover un
enlace no mueve ni modifica la ruta correspondiente.

Cada enlace suele estar protegido con `has('permiso')`. Ese control oculta opciones,
pero la seguridad real siempre debe mantenerse también en el backend.

### Cambiar el orden de las rutas

Las rutas están en:

```text
web-angular/src/app/app.routes.ts
```

Las redirecciones y rutas específicas deben declararse antes de la ruta comodín:

```typescript
{ path: '**', ... }
```

El comodín debe permanecer al final, porque captura cualquier dirección no
reconocida.

### Cambiar estilos

- Estilo exclusivo de una pantalla: archivo CSS del componente, si existe.
- Diseño general y estructura lateral: `web-angular/src/styles.css`.
- Variables, paleta y sistema visual Vidanta: `web-angular/src/vidanta-design.css`.
- Estructura del componente raíz: `web-angular/src/app/app.css`.

No conviene poner en el CSS global una regla que sólo necesita una pantalla.

## 7. Cambios en el orden de datos

Hay dos clases de orden y deben distinguirse.

### Orden funcional

Si todos los consumidores deben recibir los elementos en el mismo orden, el orden
pertenece al backend. Ejemplos:

- auditorías por fecha;
- versiones de documentos;
- plantillas publicadas;
- promociones por prioridad.

Debe aplicarse en la consulta o caso de uso mediante `OrderBy`/`ThenBy`. Así la API
entrega un resultado consistente a Angular y a cualquier consumidor futuro.

### Orden únicamente visual

Si el usuario cambia temporalmente una tabla entre nombre, fecha o estado, puede
ordenarse en el componente Angular. Ese orden no debe alterar la base de datos.

Si el orden se debe conservar entre sesiones o afectar a otros usuarios, necesita
un campo persistido, un endpoint para actualizarlo y una migración si el campo no
existe.

## 8. Agregar o modificar un campo

### Campo sólo visual

Si el dato ya llega desde la API, se modifica únicamente la pantalla. Por ejemplo,
mostrar `guest.email` debajo del nombre no exige una migración ni un endpoint nuevo.

### Campo calculado por el backend

Ejemplo: agregar `canBeSealed` a la respuesta de un expediente.

1. Calcularlo en el caso de uso o servicio correspondiente.
2. Agregarlo al DTO de respuesta de la API.
3. Regenerar el cliente OpenAPI.
4. Mostrarlo en Angular.
5. Actualizar temporalmente el modelo manual si esa pantalla aún usa `core/models.ts`.

### Campo persistido

Ejemplo: guardar una observación adicional de la firma.

1. Agregar la propiedad a la entidad en `FirmaOperaCloud.Domain/Entities`.
2. Configurar longitud, nulabilidad, índice o relación en
   `FirmaOperaCloudDbContext`.
3. Crear una migración de Entity Framework.
4. Incorporar la propiedad al caso de uso.
5. Agregarla al DTO de entrada o salida; no exponer automáticamente toda la entidad.
6. Regenerar el cliente Angular.
7. Actualizar formulario, validación y presentación.
8. Agregar pruebas para la regla o persistencia relevante.

Una migración debe revisarse antes de ejecutarla en UAT o producción. No se debe
editar una migración ya aplicada para representar un cambio nuevo.

## 9. Agregar o modificar una operación de API

Una operación completa atraviesa estas partes:

```text
Angular
  -> contrato HTTP en API
  -> caso de uso en Application
  -> regla de Domain
  -> implementación en Infrastructure
  -> SQL Server u OPERA
```

No todas las operaciones utilizan todas las capas. Una consulta simple puede no
necesitar una regla de Domain. Una operación que firma, sella o modifica OPERA sí
debe tener un caso de uso explícito.

Procedimiento recomendado:

1. Definir la petición y la respuesta esperadas.
2. Implementar o modificar el caso de uso en Application.
3. Crear el adaptador tecnológico en Infrastructure cuando corresponda.
4. Mantener el controlador pequeño y asignarle una política de autorización.
5. Registrar la implementación en `Program.cs`.
6. Verificar la operación desde Swagger.
7. Regenerar el cliente OpenAPI.
8. Consumirlo desde la funcionalidad Angular.
9. Probar tanto el resultado correcto como los errores importantes.

## 10. Cambiar información proveniente de OPERA

El navegador nunca debe llamar directamente a OPERA ni conocer sus credenciales.

Los cambios relacionados con OPERA se realizan en:

```text
src/FirmaOperaCloud.Infrastructure/Opera/
```

- Autenticación y renovación de tokens: `OperaTokenService`.
- Reservaciones: `OperaReservationService` y sus modelos internos.
- Tarjeta oficial: `OperaRegistrationCardService`.
- Acompañantes: `OperaAccompanyingGuestService`.
- Catálogos UDF: `OperaUdfCatalog`.

Si OPERA cambia el nombre o estructura de un campo, primero se ajusta su DTO interno
y el mapeo al modelo de la aplicación. No se debe propagar directamente el JSON de
OPERA hasta Angular, porque eso acoplaría toda la aplicación al proveedor.

Las escrituras en OPERA requieren más cuidado que las consultas: autorización,
validación previa, control de concurrencia, auditoría y pruebas en UAT.

## 11. Cambiar permisos y accesos

Un permiso aparece tanto en backend como en frontend.

Backend:

1. Constante en `FirmaOperaCloud.Domain/Entities/AppUser.cs`, dentro de
   `ViewPermissions`.
2. Inclusión en `ViewPermissions.All`.
3. Política correspondiente en `FirmaOperaCloud.Api/Program.cs`.
4. `[Authorize]` en el controlador o endpoint.

Frontend:

1. Constante temporal en `web-angular/src/app/core/models.ts`.
2. `data: { permission: ... }` en `app.routes.ts`.
3. Condición `has('permiso')` en `app.html` si hay una opción de menú.

Ocultar un botón o enlace no protege una operación. El backend debe rechazar la
petición aunque alguien la envíe sin usar Angular.

## 12. Cambiar una interfaz de programación

Aquí “interfaz” significa una interfaz C# como `IReservationService`.

Estas interfaces viven en:

```text
src/FirmaOperaCloud.Application/Contracts/
```

Si cambia un método:

1. Modificar el contrato en Application.
2. Actualizar su implementación en Infrastructure.
3. Actualizar los casos de uso o controladores consumidores.
4. Actualizar el registro de dependencias en `Program.cs` si cambia el tipo.
5. Ajustar pruebas y dobles de prueba.

No se debe crear una interfaz para cada clase. Se justifica principalmente en
límites tecnológicos o sustituibles: OPERA, persistencia, OCR, correo, PDF, firma,
almacenamiento y tiempo externo.

## 13. Dónde no colocar código

- No colocar consultas SQL o Entity Framework en Angular.
- No llamar OPERA desde Angular.
- No guardar secretos en `appsettings.json`, TypeScript o archivos versionados.
- No editar `core/generated` manualmente.
- No colocar reglas de negocio dentro de una plantilla HTML.
- No colocar lógica dependiente de ASP.NET dentro de Domain.
- No utilizar `Shared` como depósito de utilidades sin una responsabilidad clara.
- No devolver entidades persistidas sólo por comodidad si el endpoint necesita un
  contrato diferente.

## 14. Comprobación después de un cambio

Para un cambio únicamente visual:

```powershell
cd web-angular
npm test -- --watch=false
npm run build
```

Para un cambio del backend:

```powershell
dotnet test FirmaOperaCloud.slnx
dotnet build FirmaOperaCloud.slnx -c Release
```

Para un cambio de contrato HTTP, además:

```powershell
cd web-angular
npm run api:generate
npm test -- --watch=false
npm run build
```

También se debe revisar el diff de Git para confirmar que sólo cambiaron los
archivos previstos y que la regeneración OpenAPI no introdujo eliminaciones
inesperadas.

## 15. Regla para decidir rápidamente

Antes de mover o agregar código, se puede usar esta pregunta:

```text
¿Es presentación?             -> Angular
¿Es contrato HTTP?            -> API
¿Coordina una operación?      -> Application
¿Es una regla permanente?     -> Domain
¿Depende de una tecnología?   -> Infrastructure
¿Es código generado?          -> No editar; cambiar su origen y regenerar
```

La dirección recomendada para evolucionar el proyecto es conservar el monolito,
organizar Application por casos de uso y dividir las funcionalidades grandes de
Angular. Esto mejora la comprensión sin exigir una reescritura completa.
