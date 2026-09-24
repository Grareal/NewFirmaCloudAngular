# ACTA DE APROBACIÓN E INICIO DE PROYECTO

## Firma OPERA Cloud — Digitalización de Registration Cards

| Campo | Información |
|---|---|
| Proyecto | Firma OPERA Cloud |
| Folio | TI-FC-2026-001 |
| Área solicitante | Operación Hotelera / Concierge / Recepción |
| Área responsable | Tecnologías de Información / Desarrollo |
| Líder de proyecto | Por designar |
| Versión | 1.0 |
| Fecha | 24 de septiembre de 2026 |
| Estado | Pendiente de aprobación |

**Propósito.** Formalizar el entendimiento funcional, técnico, operativo y de seguridad del proyecto Firma OPERA Cloud, establecer las condiciones para continuar con UAT y dejar constancia de las aprobaciones requeridas antes de producción.

## 1. Información general

| Campo | Información |
|---|---|
| Patrocinador / responsable de negocio | Operación Hotelera, por designar |
| Product Owner / usuario clave | Concierge / Recepción, por designar |
| Responsable técnico | Tecnologías de Información / Desarrollo |
| Equipo participante | Operación, Desarrollo, Integraciones OPERA, Infraestructura, Seguridad, Legal/Privacidad y DBA |
| Ambiente inicial | UAT |
| Fecha objetivo de piloto | Por acordar después de aprobaciones y UAT |

## 2. Antecedentes y problema

La Tarjeta de Registro requiere consultar la reservación en OPERA Cloud, verificar datos e identidad, identificar al titular y sus acompañantes, capturar autorizaciones y firmas, generar un PDF y conservar evidencia. El manejo manual o fragmentado dificulta la trazabilidad, el control de versiones y la verificación de cuál documento fue enviado a OPERA.

Se identificó además que OPERA entrega acompañantes mediante dos colecciones parcialmente solapadas. La aplicación fue corregida para reconciliar ambas fuentes, evitar duplicados y conservar firmas ya capturadas.

## 3. Objetivo del proyecto

Implementar una aplicación web segura y responsive que permita consultar reservaciones, preparar la Registration Card, capturar firmas táctiles, revisar el documento, versionarlo localmente, enviarlo a OPERA Cloud conforme a una política configurable y mantener expediente y auditoría.

## 4. Alcance

### 4.1 Incluido

- Autenticación, roles y permisos.
- Consulta de reservaciones mediante OHIP.
- Carga de datos del titular y acompañantes desde OPERA.
- Deduplicación de acompañantes por identificador y nombre normalizado.
- Captura táctil de firmas y guardado de avance.
- Vista previa del PDF antes del envío.
- Firma obligatoria del titular y autorización de incorporación de firmas.
- Versionado local, hash documental, expediente y auditoría.
- Envío a OPERA con las políticas `KeepAllVersions`, `Replace` o `SkipIfExists`.
- Correo de documentos mediante configuración SMTP institucional.
- Diseño responsive para PC, tablet y móvil.
- OCR como asistencia sujeta a verificación humana.

### 4.2 Fuera de alcance o pendiente

- Alta o baja de acompañantes desde esta aplicación.
- Actualización del perfil del huésped en OPERA.
- Aplicación móvil nativa.
- Identidad corporativa definitiva, hasta aprobación de Arquitectura y Seguridad.
- Uso productivo sin autorización de Legal/Privacidad y Operación.

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
| RNF-04 | Integridad | No declarar una versión como subida cuando OPERA conservó otra. | Alta |

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

**Política propuesta para aprobación inicial:** `KeepAllVersions`.

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
| Credenciales y permisos OHIP | Integraciones / OPERA | Por validar en UAT | Sí |
| SQL Server, respaldo, RPO/RTO | Infraestructura / DBA | Por definir | Sí |
| Secretos en Key Vault | Seguridad / DevOps | Por configurar | Sí |
| Cuenta institucional de correo | Infraestructura | Por definir | Parcial |
| Usuarios y casos de UAT | Operación | Por designar | Sí |
| Autorización del flujo digital | Legal/Privacidad, Contraloría y Operación | Pendiente | Sí |
| Textos legales y retención | Legal/Privacidad | Pendiente | Sí |

## 10. Riesgos iniciales

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Política vigente restringe digitalización | Alto | Obtener excepción o actualización formal antes del piloto. |
| Reemplazo OHIP no soportado por ambiente | Alto | Probar `Replace` en UAT; mantener `KeepAllVersions` como alternativa segura. |
| Selección errónea de política | Alto | Validación al arranque, banner del ambiente y control de cambios. |
| Datos incompletos de acompañantes | Medio/Alto | Corregir primero en OPERA y recargar la reserva. |
| Exposición de documentos personales | Alto | Permisos, auditoría, cifrado, retención y acceso con motivo. |
| Diferencias responsive en dispositivo real | Medio | Ejecutar UAT en Galaxy Tab A9+ o equipo equivalente. |

## 11. Responsabilidades — RACI simplificado

| Actividad | Operación | Líder proyecto | Desarrollo | Infra/Seguridad | Legal/Privacidad |
|---|---|---|---|---|---|
| Definir flujo y reglas | A/R | C | C | I | C |
| Diseño técnico | C | A | R | C | I |
| Configurar política de adjuntos | A | C | R | R/C | I |
| Pruebas UAT | A/R | C | C | C | I |
| Aprobar textos y tratamiento de datos | C | I | I | C | A/R |
| Despliegue | I | A | R | R | I |

R: Responsable de ejecutar. A: Aprobador final. C: Consultado. I: Informado.

## 12. Cronograma de aprobación y liberación

| Fase | Duración estimada | Resultado |
|---|---|---|
| Cierre técnico y pruebas internas | 1 semana | Versión candidata y evidencia automatizada |
| Validación funcional | 1 semana | Casos y responsables confirmados |
| UAT integrada con OPERA | 1–2 semanas | Evidencia de las tres políticas |
| Revisión Legal/Seguridad | Según responsables | Autorizaciones y condiciones documentadas |
| Piloto controlado | Según ventana | Operación supervisada |
| Producción | Según autorización | Liberación y soporte activo |

## 13. Criterios de aceptación

1. Una persona presente en ambas colecciones de OPERA aparece una sola vez.
2. La firma guardada permanece asociada al mismo firmante.
3. No se generan renglones vacíos a partir del conteo de adultos.
4. El flujo visible contiene solamente Datos, Firmas y Revisión.
5. La vista previa permanece válida al marcar la confirmación final.
6. `KeepAllVersions` genera nombres versionados y no sobrescribe.
7. `Replace` solicita reemplazo con nombre estable.
8. `SkipIfExists` no ejecuta POST cuando el adjunto ya existe.
9. La respuesta y el expediente local distinguen `Uploaded`, `Replaced` y `SkippedExisting`.
10. No existen defectos críticos abiertos y UAT cuenta con evidencia firmada.
11. Legal/Privacidad y Seguridad autorizan el tratamiento y la retención aplicables.

## 14. Control de cambios

Todo cambio posterior que modifique alcance, integraciones, política de adjuntos, textos legales, datos almacenados, retención o criterios de aceptación deberá registrarse. Antes de incorporarlo se comunicará su impacto en esfuerzo, fechas, riesgos, migración y operación.

## 15. Condiciones para autorizar el piloto

La aprobación de esta acta autoriza continuar con UAT y preparación del piloto, pero no constituye por sí sola autorización de producción. Antes del piloto deben completarse:

- Aprobación del formato y textos por Operación y Legal/Privacidad.
- Autorización expresa del flujo digital frente a la política vigente.
- Prueba OHIP de carga y reemplazo en UAT.
- Prueba física responsive en tablet equivalente a Galaxy Tab A9+.
- Ejecución del build y pruebas Angular en un agente con Node/npm.
- Definición de soporte, respaldo, monitoreo, retención y recuperación.

## 16. Aprobación y firmas

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

