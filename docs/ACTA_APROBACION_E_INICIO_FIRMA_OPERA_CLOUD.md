# ACTA DE APROBACIÓN E INICIO DE PROYECTO

## Firma OPERA Cloud — Digitalización de Registration Cards

| Campo | Información |
| Proyecto | Firma OPERA Cloud |
| Área solicitante | Operación Hotelera / Concierge / Recepción |
| Área responsable | Tecnologías de Información / Desarrollo |
| Líder de proyecto | Yesenia Castañon Jimenez  |
| Versión | 1.0 |
| Fecha | 24 de septiembre de 2026 |
| Estado | Pendiente de aprobación |

**Propósito.** Formalizar el entendimiento funcional, técnico, operativo y de seguridad del proyecto Firma OPERA Cloud, establecer las condiciones para continuar y dejar constancia de las aprobaciones requeridas antes de producción.

## 1. Información general

| Campo | Información |
|---|---|
| Product Owner / usuario clave | Sergio Rodriguez Diaz |
| Responsable técnico | Tecnologías de Información / Desarrollo |
| Equipo participante | Operación, Desarrollo, Infraestructura, Legal y Calidad |
| Ambiente inicial | UAT |
| Fecha objetivo de piloto | Por acordar después de aprobaciones y UAT |

## 2. Antecedentes y problema

La Tarjeta de Registro requiere consultar la reservación en OPERA Cloud, verificar datos e identidad, identificar al titular y sus acompañantes, capturar autorizaciones y firmas, generar un PDF y conservar evidencia. Puesto que el manejo manual  dificulta la trazabilidad, el control de versiones y la verificación de cuál documento fue enviado a OPERA.


## 3. Objetivo del proyecto

Implementar una aplicación web segura  que permita consultar reservaciones, preparar la Registration Card, capturar firmas táctiles, revisar el documento, versionarlo localmente, enviarlo a OPERA Cloud conforme a una política configurable y mantener expediente y auditoría.

## 4. Alcance

### 4.1 Incluido

- Autenticación, roles y permisos.
- Consulta de reservaciones mediante OHIP.
- Carga de datos del titular y acompañantes desde OPERA.
- Captura táctil de firmas y guardado de avance.
- Vista previa del PDF antes del envío.
- Firma obligatoria del titular y autorización de incorporación de firmas.
- Versionado local, hash documental, expediente y auditoría.
- Envío a OPERA con las políticas `KeepAllVersions`, `Replace` o `SkipIfExists`.
- Correo de documentos mediante configuración SMTP institucional.
- Diseño responsive para PC, tablet y móvil.
- OCR como asistencia sujeta a verificación humana. (Falta de ver implementacion)

### 4.2 Fuera de alcance o pendiente

- Alta o baja de acompañantes desde esta aplicación.
- Actualización del perfil del huésped en OPERA.
- Uso productivo sin autorización de Legal/Privacidad y Operación.
- Diseño de tarjeta de registro para adaptabilidad
- Legalidad y procedimiento de calidad añadiendo la informacion de la tarjeta digital

## 5. Requerimientos iniciales

| ID | Tipo | Requerimiento | Prioridad |
|---|---|---|---|
| RF-01 | Funcional | Consultar una reservación vigente en OPERA Cloud. | Alta |
| RF-02 | Funcional | Mostrar una sola vez a cada acompañante retornado por OPERA. | Alta |
| RF-03 | Funcional | Capturar, conservar y asociar cada firma a su persona. | Alta |
| RF-04 | Funcional | Generar y revisar el PDF antes de enviarlo. | Alta |
| RF-05 | Funcional | Crear una versión local inmutable por cada generación final. | Alta |
| RF-06 | Funcional | Aplicar la política configurada de adjuntos de OPERA. | Alta |
| RF-07 | Funcional | Informar si el adjunto fue subido, reemplazado u omitido. | Alta |
| RNF-01 | Seguridad | No almacenar secretos directamente en el código fuente. | Alta |
| RNF-02 | Auditoría | Registrar versiones, hashes, usuario, consentimientos y resultado de envío. | Alta |
| RNF-03 | UX | Operar correctamente en PC, Galaxy Tab A9+ aproximada y móvil. | Alta |

## 6. Arquitectura y tecnologías

- Frontend: Angular 21.
- Backend: ASP.NET Core Web API 10.
- Base de datos: SQL Server con Entity Framework Core.
- Integración: OPERA Cloud mediante OHIP.
- Documentos: PDFsharp y QuestPDF.
- OCR: Tesseract on-premise, sujeto a validación de plataforma.
- Seguridad: cookie HttpOnly, permisos, Azure Key Vault/variables de entorno y HTTPS.
- Entrega: Git, GitHub Actions/Azure DevOps, Docker cuando aplique.

## 7. Política de Registration Cards en OPERA

La variable `OperaCloud__RegistrationCardAttachmentPolicy` determina el comportamiento:

| Valor | Comportamiento | Nombre en OPERA |
|---|---|---|
| `KeepAllVersions` | Conserva cada documento como adjunto independiente. Es el valor predeterminado y recomendado para auditoría. | `REGCARD{confirmación}SIGNED-V{n}.pdf` |
| `Replace` | Mantiene un nombre estable y solicita a OPERA reemplazar el adjunto existente. | `REGCARD{confirmación}SIGNED.pdf` |
| `SkipIfExists` | Si el nombre estable ya existe, no sube la nueva versión y registra el resultado localmente. | `REGCARD{confirmación}SIGNED.pdf` |

La configuración se valida al iniciar el servicio. Cualquier cambio requiere reinicio controlado y evidencia de prueba en UAT.


## 8. Entregables

| # | Entregable | Criterio general |
|---|---|---|
| 1 | Código fuente | Versionado y compilable en repositorio autorizado. |
| 2 | API e integración OHIP | Consulta, PDF, adjuntos y resultados controlados. |
| 3 | Frontend Angular | Flujo Datos → Firmas → Revisión, responsive y accesible. |
| 4 | Persistencia | Versiones, firmas, expediente, auditoría y consentimientos. |
| 5 | Configuración | Variables documentadas sin secretos. |
| 6 | Pruebas | Pruebas automatizadas y evidencia UAT. |
| 7 | Documentación | Arquitectura, operación, seguridad y actas. |
| 8 | Despliegue | Versión autorizada en el ambiente definido. |

## 9. Dependencias y prerrequisitos

| Dependencia | Responsable | Estado | ¿Bloquea producción? |
|---|---|---|---|
| Credenciales y permisos OHIP | Integraciones / OPERA | Por validar en UAT | No |
| SQL Server, respaldo, RPO/RTO | Infraestructura / DBA | Por definir | Sí |
| Cuenta institucional de correo | Infraestructura | Por definir | Parcial |
| Usuarios y casos de UAT | Operación | Por designar | Sí |
| Autorización del flujo digital | Legal/Privacidad, Contraloría y Operación | Pendiente | Sí |
| Textos legales y retención | Legal/Privacidad | Pendiente | Sí |


## 10. Responsabilidades — RACI simplificado

| Actividad | Operación | Líder proyecto | Desarrollo | Infra/Seguridad | Legal/Privacidad |
|---|---|---|---|---|---|
| Definir flujo y reglas | A/R | C | C | I | C |
| Diseño técnico | C | A | R | C | I |
| Configurar política de adjuntos | A | C | R | R/C | I |
| Pruebas UAT | A/R | C | C | C | I |
| Aprobar textos y tratamiento de datos | C | I | I | C | A/R |
| Despliegue | I | A | R | R | I |

R: Responsable de ejecutar. A: Aprobador final. C: Consultado. I: Informado.



## 11. Criterios de aceptación

1. Una persona presente en ambas colecciones de OPERA aparece una sola vez.
2. La firma guardada permanece asociada al mismo firmante.
3. No se generan renglones vacíos a partir del conteo de adultos.
4. El flujo visible contiene solamente Datos, Firmas y Revisión.
5. Legal/Privacidad y Seguridad autorizan el tratamiento y la retención aplicables.

## 12. Control de cambios

Todo cambio posterior que modifique alcance, integraciones, política de adjuntos, textos legales, datos almacenados, retención o criterios de aceptación deberá registrarse. Antes de incorporarlo se comunicará su impacto en esfuerzo, fechas, riesgos, migración y operación.

## 13. Condiciones para autorizar el piloto

La aprobación de esta acta autoriza continuar con UAT y preparación del piloto, pero no constituye por sí sola autorización de producción. Antes del piloto deben completarse:

- Aprobación del formato y textos por Operación y Legal/Privacidad.
- Autorización expresa del flujo digital frente a la política vigente.
- Definicion de si en el proceso se utilizara el ocr para la subida de documentos al archivo mediante la app (Uso de licencia con inteligencia artificial o proveedor)
- Revisar los codigos promocionales de tsw, administracion y homologacion de estos


## 14. Aprobación y firmas

Mediante la firma del presente documento, las partes declaran haber revisado el objetivo, alcance, arquitectura, política de versiones, dependencias, riesgos y criterios generales, y manifiestan su decisión respecto al inicio de UAT y preparación del piloto.

| Rol | Nombre | Decisión | Firma | Fecha |
|---|---|---|---|---|
| Solicitante / Product Owner |  | Aprobado / Rechazado |  |  |
| Responsable de Operación Hotelera |  | Aprobado / Rechazado |  |  |
| Líder del proyecto |  | Aceptado |  |  |
| Responsable TI / Desarrollo |  | Aceptado |  |  |
| Seguridad de la Información |  | Autorizado / Condicionado |  |  |
| Legal / Privacidad |  | Autorizado / Condicionado |  |  |

### Aceptación final / UAT

| Resultado | Nombre / Rol | Firma | Fecha |
|---|---|---|---|
| Aceptado |  |  |  |
| Aceptado con observaciones |  |  |  |
| Rechazado / requiere correcciones |  |  |  |

**Observaciones de cierre:**

________________________________________________________________________________

________________________________________________________________________________

