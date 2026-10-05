# Revisión de mantenibilidad

## Diagnóstico

La separación Domain / Application / Infrastructure / API es razonable para las integraciones externas que sí cambian (OPERA, OCR, correo, PDF y persistencia). El problema no es Clean Architecture en sí, sino aplicar ceremonias de manera irregular y concentrar demasiadas responsabilidades en algunos archivos.

Los puntos más importantes encontrados son:

1. `Program.cs` mezcla composición, seguridad, migraciones, datos semilla y diagnóstico del ambiente. Es fácil de ejecutar, pero difícil de recorrer y modificar.
2. Algunos controladores son demasiado grandes: `ReservationsController` ronda 400 líneas y combina consulta, mapeo, validación y flujos operativos.
3. Hay servicios extensos de integración, especialmente `OperaAccompanyingGuestService`, con transporte HTTP, JSON, reglas y mapeo en una sola clase.
4. Coexisten un cliente Angular manual y otro generado desde OpenAPI. Eso permite que los contratos se desvíen; el OCR ya mostraba esa clase de problema.
5. El análisis estático produce muchas advertencias repetitivas. Las más relevantes son uso de `System.Drawing` solo compatible con Windows dentro de una imagen Docker Linux, `CancellationToken` fuera de la última posición y conversiones dependientes de la cultura.
6. Hay comentarios que describen implementación temporal (`POC`, “revisar”, “dependiendo cuál”) en vez de explicar decisiones. También hay cadenas con problemas históricos de codificación en algunos archivos/documentos.

## Simplificaciones ya aplicadas

- Las referencias LEADTOOLS absolutas y duplicadas se reemplazaron por un paquete NuGet oficial.
- Se eliminaron el endpoint `passport-debug` y el lector singleton duplicado; el flujo normal ya reconoce pasaporte.
- El selector OCR dejó de usar `IServiceProvider` como service locator. Ahora recibe tres dependencias explícitas y cabe en una clase pequeña.
- Se eliminó una interfaz de fábrica con una sola implementación que no representaba un límite arquitectónico.
- La configuración de licencias está fuera del código y produce errores concretos.
- El motor elegido se conserva también al generar el PDF.

## Siguiente refactor recomendado

Conviene hacerlo por rebanadas funcionales, sin crear más proyectos ni interfaces:

1. Extraer de `Program.cs` métodos de extensión pequeños: `AddOperaServices`, `AddOcrServices`, `AddSecurity` y `SeedReferenceData`. Mantenerlos en la API.
2. Dividir `ReservationsController` por intención de usuario, no por capas técnicas: búsqueda/consulta, acompañantes y generación de tarjeta.
3. Convertir los DTO y mapeos privados de OPERA en archivos cercanos al servicio únicamente cuando se reutilicen o superen unas decenas de líneas.
4. Elegir el cliente OpenAPI generado como única ruta y retirar gradualmente los métodos duplicados de `ApiService`.
5. Sustituir el preprocesamiento `System.Drawing` por una opción multiplataforma antes de declarar soportado el Docker Linux. Hasta entonces, documentar Tesseract preprocesado como Windows-only.
6. Agregar pruebas de contrato para `/api/ocr/parse` y `/api/ocr/identity-pdf`; las pruebas actuales cubren el parser, no la selección ni configuración de proveedores.

La regla práctica sugerida es: una interfaz solo para un límite real o varias implementaciones; una clase por flujo que tenga una razón clara para cambiar; y comentarios para explicar el “por qué”, no para narrar el código.
