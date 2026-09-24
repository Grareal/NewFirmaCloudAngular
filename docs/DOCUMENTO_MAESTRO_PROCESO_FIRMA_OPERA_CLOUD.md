# Documento maestro de proceso, alcance y aprobación

## Firma OPERA Cloud

| Dato | Valor |
|---|---|
| Versión del documento | 0.1 para validación |
| Fecha | 23 de septiembre de 2026 |
| Rama de trabajo | `documentacionyaprobacion` |
| Estado | Propuesta sujeta a aprobación de Operación, Hotel Operations, Legal/Privacidad, Seguridad, Infraestructura y Sistemas |
| Responsable operativo propuesto | Concierge / Recepción |
| Sistema relacionado | OPERA Cloud mediante OHIP |

## 1. Propósito

Definir el proceso funcional y técnico propuesto para identificar la reserva, validar los datos del huésped, integrar a los acompañantes provenientes de OPERA, capturar autorizaciones y firmas, generar la Tarjeta de Registro, mostrar su vista previa, enviarla a OPERA Cloud y conservar evidencia local auditable.

Este documento se entrega para validar **cómo se desarrollará y operará la solución**. Los puntos marcados como “Por decidir” o “Bloqueo” no deben asumirse como aprobados ni liberarse a producción sin respuesta formal del área responsable.

## 2. Resumen ejecutivo

El flujo propuesto inicia con el acceso individual del colaborador y la búsqueda de una reserva en OPERA. La aplicación toma los datos disponibles, presenta los acompañantes sin permitir altas o bajas locales, captura y revisa la identificación, solicita la aceptación legal antes de firmar, genera una vista previa dentro de la misma pantalla y permite que Concierge envíe la tarjeta a OPERA.

La aplicación conserva versiones locales para auditoría. Una versión cargada por error se puede **ocultar o restaurar**, pero no se elimina físicamente. El tratamiento del adjunto en OPERA se controla mediante una política de configuración: conservar todas las versiones, reemplazar el adjunto estable o no subir cuando ya existe.

Se mantienen temporalmente el correo por SMTP institucional y el inicio de sesión local. La definición final debe ser una cuenta funcional institucional para correo y una identidad corporativa individual para cada operador; no se recomienda usar cuentas personales.

## 3. Fuentes normativas revisadas

Se revisaron los siguientes documentos, incorporados en `docs` para trazabilidad:

- `GV-GN-038 Política Manejo de Tarjetas de Registro y Datos Personales en Recepción (1).pdf`.
- `GV-PR-DOH-CON-005 Registro y Movimientos de Huéspedes.pdf`.

### 3.1 Alineación y diferencias

| Tema | Procedimiento vigente | Propuesta de Firma OPERA Cloud | Tratamiento requerido |
|---|---|---|---|
| Acceso | Claves personales e intransferibles | Usuario individual con permisos | Alineado; migrar a identidad corporativa |
| Identificación oficial | INE o pasaporte | OCR reconoce además licencia, residencia y visa | Los tipos adicionales quedan experimentales hasta autorización |
| Acompañantes | Deben coincidir tarjeta y sistema | Se toman directamente de OPERA | Validar reglas de adultos, menores y firmas |
| Datos de residencia | Recepción registra datos | Se capturan para el documento | Definir si también se actualizan en OPERA |
| Tarjeta y datos personales | La política 2015 restringe escaneo, copiado y almacenamiento digital | La solución usa tableta, OCR, PDF y base de datos | **Bloqueo normativo:** requiere excepción o actualización formal |
| Comunicaciones | Medios institucionales | SMTP institucional transitorio | Alineado si no se usa cuenta personal |
| Formato | Debe ser autorizado por Legal y Operación Hotelera | PDF generado por la aplicación | Requiere aprobación de formato y textos legales |

> **Bloqueo antes del piloto:** la política `GV-GN-038` restringe la digitalización y almacenamiento de tarjetas/datos en Recepción. Contraloría, Seguridad, Legal/Privacidad y Operación deben autorizar expresamente el flujo digital o emitir la actualización aplicable.

## 4. Proceso operativo propuesto

### 4.1 Acceso

1. El colaborador ingresa con una cuenta individual.
2. La aplicación valida su rol y registra en auditoría las acciones relevantes.
3. La sesión actual usa autenticación local con cookie segura. La meta es identidad corporativa; este cambio está pendiente de arquitectura y Seguridad.

No debe compartirse una cuenta de operador. La cuenta funcional de correo, en su caso, es distinta de la identidad con la que el colaborador inicia sesión.

### 4.2 Localización de la reserva

1. Concierge busca por número de confirmación o apellido.
2. La aplicación consulta OPERA mediante OHIP.
3. Se selecciona la reserva correcta y se verifica huésped, hotel, vigencia, habitación y estatus.

Actualmente el número de confirmación es el identificador visible principal; `reservationId` es interno. No se recomienda usar únicamente el número de habitación porque puede cambiar y no es único a lo largo del tiempo. La regla definitiva se encuentra en D-04 y D-05.

### 4.3 Revisión de identidad y datos

1. Se carga o captura la identificación.
2. El OCR propone tipo de documento, nombre, número y vigencia cuando puede detectarlos.
3. El operador compara la imagen con la información extraída y corrige lo necesario.
4. La imagen debe estar completa, legible, sin obstrucciones y corresponder a la persona.

El OCR es una ayuda, no una decisión automática. INE y pasaporte son los únicos documentos operativamente autorizados por el procedimiento revisado. Licencia, tarjeta de residencia y visa se muestran como reconocimiento técnico experimental y no habilitan el check-in sin autorización formal.

Los datos capturados llenan la tarjeta; **no actualizan hoy el perfil de OPERA**. La escritura de nacionalidad, ciudad, estado o país requiere decisión funcional, mapeo OHIP y autorización.

### 4.4 Acompañantes

1. La aplicación toma directamente de OPERA los acompañantes asociados a la reserva.
2. La interfaz no permite elegir, agregar ni quitar adultos o menores.
3. Si existe una firma guardada del mismo acompañante, se conserva al actualizar los datos.
4. Concierge compara que los ocupantes mostrados coincidan con la operación real.
5. Si falta o sobra una persona, se corrige primero en OPERA y después se recarga la reserva.

El código de edición local se conserva comentado/oculto para no perder el trabajo previo, pero no forma parte del flujo propuesto. La regla final sobre menores, capacidad, acompañantes no presentes y firmas está pendiente de María Guadalupe (Lupita) y Operación.

### 4.5 Autorizaciones y firmas

Antes de capturar o incorporar firmas, el operador debe confirmar la autorización obligatoria propuesta:

> “Verifico que los datos y firmantes corresponden a la reserva y autorizo que las firmas capturadas se incorporen a la Tarjeta de Registro y al expediente de la estancia.”

El consentimiento promocional se presenta separado y es opcional:

> “Autorizo el envío de promociones y beneficios de Vidanta al correo indicado. Entiendo que es opcional y que no condiciona el registro ni el envío de mis documentos operativos.”

La evidencia local registra aceptación de firma, elección promocional, fecha y versión del texto. Los textos son una **propuesta técnica** y requieren aprobación de Legal/Privacidad, incluyendo finalidad, responsable, conservación y mecanismo de revocación.

La firma del titular es obligatoria para enviar. El sistema todavía permite que existan firmas de acompañantes pendientes; la regla definitiva debe acordarse en D-03.

### 4.6 Vista previa y envío a OPERA

1. Concierge genera la vista previa.
2. El PDF se muestra dentro de la misma pantalla, sin obligar a descargarlo.
3. Se revisan datos, acompañantes, identificaciones, autorizaciones y firmas.
4. Concierge selecciona **Enviar a OPERA**.
5. La API vuelve a generar el documento, valida la autorización y la firma principal, almacena la versión local y crea el adjunto en OPERA.
6. Se muestra el resultado y se registra auditoría.

La descarga queda como opción secundaria. El envío a OPERA es responsabilidad de Concierge y no del huésped.

### 4.7 Correo electrónico

1. La configuración SMTP se administra por hotel.
2. El sistema valida servidor, puerto, seguridad, remitente y destinatario de prueba.
3. Los correos pendientes se procesan solamente cuando el hotel tiene habilitado el envío.
4. Los errores quedan registrados y se reintentan conforme al mecanismo existente.

Se corrigió el procesamiento para que habilitar SMTP desde configuración no requiera reiniciar la aplicación y se agregó un límite de espera a la conexión. Debe ejecutarse una prueba real en UAT con credenciales institucionales.

Decisión recomendada: usar temporalmente SMTP con una cuenta funcional institucional. Como solución objetivo, registrar una aplicación y usar el servicio corporativo autorizado (por ejemplo, Microsoft Graph), sujeto a Seguridad e Infraestructura. No utilizar cuentas personales.

### 4.8 Expediente y documentos

- La aplicación conserva documentos y versiones locales con trazabilidad.
- Los documentos visibles pueden ocultarse indicando un motivo; un administrador autorizado puede incluir ocultos en la consulta y restaurarlos.
- Ocultar es una baja lógica: no borra el archivo, la auditoría ni un adjunto ya enviado a OPERA.
- Una nueva generación siempre crea una versión local. En OPERA se aplica `KeepAllVersions`, `Replace` o `SkipIfExists`, según la configuración del ambiente.
- La eliminación física deberá sujetarse a la política de conservación, protección de datos y capacidades de OPERA.

## 5. Casos operativos que requieren regla explícita

### 5.1 Tarjeta enviada sin todas las firmas de acompañantes

Estado actual: el envío exige la firma del titular, pero permite firmas pendientes de acompañantes. El PDF refleja el estado disponible. Debe decidirse si se bloquea el envío, si se permite una excepción con motivo y autorización, o si la firma de acompañantes no es obligatoria.

Recomendación para aprobación: bloquear cuando falte una firma requerida; permitir excepción solamente con rol, motivo y auditoría.

### 5.2 Acompañante ausente o no registrado

No debe alterarse sólo en la tarjeta. Concierge debe corregir la ocupación en OPERA conforme al procedimiento y volver a cargar la reserva. Debe definirse cómo se tratan menores y personas que llegan posteriormente.

### 5.3 Corrección después de enviar

La tarjeta anterior se conserva. Se corrigen los datos, se genera una nueva versión y se reenvía. Se debe acordar cómo identificar en OPERA cuál es la versión vigente y si el adjunto previo puede ocultarse o clasificarse como sustituido.

### 5.4 Documento cargado por error

Un usuario con permiso de sellado documental lo oculta con un motivo. La acción queda auditada y es reversible. La baja lógica local no elimina documentos externos.

## 6. Alcance implementado en la rama

| Elemento | Estado |
|---|---|
| SMTP configurable por hotel sin reinicio | Implementado; falta prueba real UAT |
| Tiempo límite de conexión SMTP | Implementado |
| Vista previa PDF dentro de la pantalla | Implementado |
| Descarga opcional | Implementado |
| Envío identificado como acción de Concierge | Implementado en interfaz y proceso |
| Acompañantes tomados de OPERA sin edición local | Implementado; regla funcional pendiente |
| Altas/bajas de adultos y menores ocultas | Implementado; código conservado |
| “Soy huésped”, “Operación”, “Validación de firmas” y “Probar operación” ocultos | Implementado |
| Autorización obligatoria para incorporar firmas | Implementado; texto pendiente de Legal |
| Consentimiento promocional separado y opcional | Implementado; texto pendiente de Legal |
| Ocultar/restaurar documentos con motivo y auditoría | Implementado; requiere migración de BD |
| Evidencia de consentimientos en documento local | Implementado; requiere migración de BD |
| OCR ampliado | Implementado técnicamente; tipos extra no autorizados operativamente |
| Ajustes responsive para tableta | Implementados en CSS; falta validación física |
| Autenticación corporativa | No implementada; pendiente de decisión |
| Actualización de datos de perfil en OPERA | No implementada; pendiente de alcance |
| Política de versiones de adjuntos en OPERA | Implementada y configurable; `KeepAllVersions` es el valor predeterminado |

## 7. Matriz de decisiones para aprobación

| ID | Decisión requerida | Responsable sugerido | Recomendación / condición |
|---|---|---|---|
| D-01 | ¿Se agregan o quitan acompañantes desde la aplicación? | Lupita + Operación | No; corregir OPERA y recargar |
| D-02 | ¿Se agregan menores y quién debe firmar? | Lupita + Operación + Legal | Definir por edad, tutela y procedimiento |
| D-03 | ¿Se permite enviar si faltan firmas de acompañantes? | Lupita + Operación + Legal | Bloquear firma requerida o auditar excepción |
| D-04 | Identificador principal: confirmation number, reservationId o CRS | Lupita + Integraciones | Confirmación visible; reservationId interno |
| D-05 | ¿Puede buscarse por habitación y cómo se valida vigencia? | Lupita + Operación | Sólo criterio auxiliar con hotel, fecha y titular |
| D-06 | ¿Se actualizarán nacionalidad, ciudad, estado y país en OPERA? | Lupita + Datos + Integraciones | Aprobar campos, sistema maestro y mapeo OHIP |
| D-07 | Mecanismo definitivo de correo | Seguridad + Infraestructura | SMTP institucional transitorio; aplicación registrada como objetivo |
| D-08 | ¿Cuenta funcional o personal para correo? | Seguridad + Infraestructura | Cuenta funcional institucional, nunca personal |
| D-09 | Mecanismo definitivo de inicio de sesión | Seguridad + Arquitectura | Identidad corporativa individual con roles |
| D-10 | Servidor, base de datos, respaldos y soporte | Infraestructura + DBA | Definir ambientes, SQL Server, cifrado, respaldo, RPO/RTO y monitoreo |
| D-11 | Excepción/actualización a política 2015 para flujo digital | Contraloría + Seguridad + Legal + Operación | **Bloqueo previo al piloto** |
| D-12 | Documentos de identidad permitidos | Operación + Legal + Seguridad | INE/pasaporte hasta autorización expresa |
| D-13 | Manejo de versiones y adjuntos previos en OPERA | Operación + Integraciones | Mantener historial y marcar versión vigente |
| D-14 | Aprobación de textos legales, retención y revocación | Legal/Privacidad | **Bloqueo previo al piloto** |

## 8. Arquitectura, seguridad e infraestructura

- Frontend Angular 21 y API ASP.NET Core 10.
- SQL Server para configuración, auditoría, cola de correo y expediente local.
- Integración con OPERA Cloud mediante OHIP.
- Sesión por cookie `HttpOnly`; autenticación local actual, corporativa como objetivo.
- Permisos para operaciones administrativas y ocultamiento/restauración.
- Secretos SMTP y OHIP deben administrarse fuera del código y cifrarse/protegerse según el ambiente.
- HTTPS obligatorio, acceso de red restringido, bitácora centralizada y monitoreo.
- Respaldos, restauración probada, RPO/RTO, retención, purga y responsables todavía deben definirse.

La ubicación del servidor y de la base de datos **no está aprobada**. No se debe presentar un ambiente provisional como arquitectura final.

## 9. Criterios de aceptación

| ID | Criterio |
|---|---|
| CA-01 | Sólo usuarios autorizados acceden y cada acción queda asociada a una identidad individual. |
| CA-02 | La búsqueda devuelve y permite confirmar inequívocamente la reserva correcta. |
| CA-03 | Los acompañantes mostrados coinciden con OPERA y no pueden modificarse localmente. |
| CA-04 | Las discrepancias de ocupantes se corrigen en el sistema maestro antes de enviar. |
| CA-05 | OCR muestra confianza/advertencias y siempre permite revisión humana. |
| CA-06 | Sólo se aceptan tipos de identificación autorizados por el procedimiento aprobado. |
| CA-07 | No se captura ni incorpora una firma sin autorización obligatoria. |
| CA-08 | El consentimiento promocional es independiente, opcional y auditable. |
| CA-09 | La vista previa se visualiza dentro de la pantalla en escritorio y tableta. |
| CA-10 | El PDF coincide con reserva, acompañantes, datos y firmas revisados. |
| CA-11 | Concierge ejecuta el envío y recibe resultado claro de OPERA. |
| CA-12 | Un reenvío genera una versión identificable sin destruir evidencia previa. |
| CA-13 | Ocultar/restaurar exige permiso y motivo, y no elimina evidencia. |
| CA-14 | SMTP funciona con cuenta institucional y registra fallos sin exponer secretos. |
| CA-15 | La solución funciona en las tabletas objetivo, incluyendo firma y vista previa. |
| CA-16 | La política digital, el formato y los textos legales tienen aprobación documentada. |

## 10. Verificación técnica realizada

- Compilación de la solución .NET: satisfactoria, sin errores.
- Pruebas automatizadas .NET: 6 aprobadas, 0 fallidas.
- Migración creada: `AddDocumentVisibilityAndConsentEvidence`.
- Compilación/pruebas Angular: pendientes porque el equipo de revisión no tiene Node/npm instalado; deben ejecutarse en CI o en un ambiente con la versión aprobada.
- SMTP real, OHIP, tableta física y comportamiento en UAT: pendientes por requerir infraestructura y credenciales.

## 11. Puertas de control antes del piloto

1. Resolver D-11 y documentar la autorización del tratamiento digital.
2. Aprobar formato de tarjeta, avisos y textos de D-14.
3. Cerrar las reglas de acompañantes, menores y firmas D-01 a D-03.
4. Confirmar identificadores y datos que se escribirán en OPERA D-04 a D-06.
5. Aprobar correo, identidad, servidor y base de datos D-07 a D-10.
6. Definir manejo de versiones/adjuntos D-13.
7. Aplicar la migración en un ambiente respaldado.
8. Ejecutar compilación Angular y pruebas de integración, seguridad, tabletas, SMTP y OHIP en UAT.
9. Obtener aceptación formal de los criterios CA-01 a CA-16.

## 12. Registro de aprobación

| Área / responsable | Nombre | Decisión | Fecha | Observaciones |
|---|---|---|---|---|
| María Guadalupe (Lupita) / Operación |  | Aprobar / Rechazar / Condicionar |  | D-01 a D-06, D-13 |
| Operación Hotelera |  | Aprobar / Rechazar / Condicionar |  | Proceso y formato |
| Legal / Privacidad |  | Aprobar / Rechazar / Condicionar |  | D-02, D-03, D-11, D-12, D-14 |
| Seguridad de la Información |  | Aprobar / Rechazar / Condicionar |  | D-07 a D-12 |
| Infraestructura / DBA |  | Aprobar / Rechazar / Condicionar |  | D-07, D-08, D-10 |
| Integraciones OPERA/OHIP |  | Aprobar / Rechazar / Condicionar |  | D-04 a D-06, D-13 |
| Contraloría |  | Aprobar / Rechazar / Condicionar |  | D-11 y controles |

La aprobación de este documento autoriza el proceso y alcance aquí descritos, no libera por sí sola el paso a producción. La liberación requiere evidencia de pruebas, cierre de bloqueos y autorización de cambio conforme al gobierno tecnológico de Vidanta.
