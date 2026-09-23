# Firma OPERA Cloud — Angular + ASP.NET Core

Solución migrada a Angular 21 y ASP.NET Core 10 con arquitectura por capas. Blazor,
tablet, estación y SignalR ya no forman parte del código de ejecución.

## Arranque local

1. Configure las variables descritas en `docs/CONFIGURACION_SEGURA.md`.
2. API: `dotnet run --project src/FirmaOperaCloud.Api --launch-profile http`.
3. Angular: en `web-angular`, ejecute `npm ci` y después `npm start`.
4. Abra `http://localhost:4200`. El proxy envía `/api` a ASP.NET.

`ng serve` y `npm start` cargan automáticamente `web-angular/proxy.conf.json`
desde `angular.json`. La API debe estar disponible en `http://127.0.0.1:5016`.
Reinicie Angular después de cambiar el proxy. Para UAT (puerto 5017), utilice
una configuración de proxy que apunte expresamente a ese ambiente.

## Documentación para cambios

Consulte [`docs/GUIA_ARQUITECTURA_Y_CAMBIOS.md`](docs/GUIA_ARQUITECTURA_Y_CAMBIOS.md)
para entender la función de cada proyecto, el flujo OpenAPI y qué archivos deben
modificarse al cambiar pantallas, menús, rutas, campos, permisos, PDF u OPERA.

Para la validación del proceso y alcance de Firma OPERA Cloud, consulte:

- [`docs/DOCUMENTO_MAESTRO_PROCESO_FIRMA_OPERA_CLOUD.md`](docs/DOCUMENTO_MAESTRO_PROCESO_FIRMA_OPERA_CLOUD.md), con alcance, decisiones pendientes y criterios de aceptación.
- [`docs/DOCUMENTO_VISUAL_PROCESO_FIRMA_OPERA_CLOUD.md`](docs/DOCUMENTO_VISUAL_PROCESO_FIRMA_OPERA_CLOUD.md), con los diagramas editables del proceso.

En PowerShell, si la política de ejecución bloquea `npm.ps1`, use `npm.cmd ci`
y `npm.cmd start`; no es necesario cambiar la política del equipo.

Validación del frontend, desde `web-angular`: `npm.cmd test -- --watch=false`
y `npm.cmd run build`. Las pruebas usan respuestas HTTP simuladas y no escriben en OPERA.

Para un paquete de un solo origen, `dotnet publish src/FirmaOperaCloud.Api -c Release`
compila Angular y lo incorpora a `wwwroot` del artefacto publicado.

Las migraciones no se aplican automáticamente salvo que
`Database__ApplyMigrations=true`. En UAT y producción deben ejecutarse como una
etapa controlada y respaldada del despliegue.

## Cliente Angular generado desde OpenAPI

Con la API de desarrollo ejecutándose en el puerto 5016, use
`npm run api:generate` dentro de `web-angular`. El generador lee el contrato Swagger y
crea tipos y servicios en `src/app/core/generated`; así, un cambio incompatible del
backend se detecta al compilar Angular. El cliente manual actual se conserva durante
la transición y debe sustituirse gradualmente por los servicios generados.
