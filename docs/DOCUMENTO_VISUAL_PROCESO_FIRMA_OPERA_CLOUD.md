# Documento visual para validación

## Firma OPERA Cloud

**Versión:** 0.1 — 23 de septiembre de 2026  
**Rama:** `documentacionyaprobacion`  
**Estado:** propuesta para revisión y aprobación

Este documento resume visualmente el proceso detallado en `DOCUMENTO_MAESTRO_PROCESO_FIRMA_OPERA_CLOUD.md`. Los diagramas están en Mermaid para que puedan mantenerse junto con el código.

## 1. Flujo operativo de una página

```mermaid
flowchart TD
    A[Colaborador inicia sesión] --> B[Busca reserva por confirmación o apellido]
    B --> C[OHIP consulta OPERA Cloud]
    C --> D{¿Reserva correcta y vigente?}
    D -- No --> B
    D -- Sí --> E[Cargar titular y acompañantes desde OPERA]
    E --> F[Capturar identificación]
    F --> G[OCR propone datos]
    G --> H[Concierge revisa y corrige]
    H --> I{¿Ocupantes coinciden?}
    I -- No --> J[Corregir primero en OPERA]
    J --> E
    I -- Sí --> K[Aceptar autorización de firmas]
    K --> L[Elegir consentimiento promocional opcional]
    L --> M[Capturar firma del titular y las requeridas]
    M --> N[Generar vista previa en la misma pantalla]
    N --> O{¿Datos y PDF correctos?}
    O -- No --> H
    O -- Sí --> P[Concierge envía a OPERA]
    P --> Q[Guardar nueva versión y auditoría local]
    Q --> R[Crear adjunto en OPERA]
    Q --> S[Encolar correo SMTP si corresponde]
```

## 2. Fuente y destino de los datos

```mermaid
flowchart LR
    OP[(OPERA Cloud)] -->|Reserva, titular, habitación y acompañantes| APP[Firma OPERA Cloud]
    ID[Identificación] -->|Imagen y OCR revisado| APP
    FIRMA[Tableta / firma] -->|Trazos y autorizaciones| APP
    APP -->|PDF como nueva versión| DB[(SQL Server local)]
    APP -->|Adjunto mediante OHIP| OP
    DB -->|Cola de salida| SMTP[SMTP institucional]
    SMTP --> CORREO[Correo del huésped]

    classDef pending fill:#fff3cd,stroke:#9a6700,color:#24292f;
    class DB pending;
```

La ubicación definitiva de SQL Server, respaldos y soporte está pendiente. Los datos escritos en la tarjeta no actualizan por sí mismos el perfil de OPERA.

## 3. Regla propuesta para acompañantes

```mermaid
flowchart TD
    A[OPERA devuelve acompañantes] --> B[Aplicación muestra la lista sin edición]
    B --> C{¿Lista correcta?}
    C -- Sí --> D[Continuar con revisión y firmas]
    C -- No --> E[No agregar ni quitar localmente]
    E --> F[Concierge corrige en OPERA según procedimiento]
    F --> G[Recargar la reserva]
    G --> B
    D --> H{¿Faltan firmas requeridas?}
    H -- No --> I[Generar vista previa]
    H -- Sí --> J[Regla pendiente: bloquear o excepción auditada]
```

Por validar con María Guadalupe (Lupita): altas/bajas, menores, edades, capacidad, firmas obligatorias y llegadas posteriores.

## 4. Autorizaciones

```mermaid
flowchart TD
    A[Datos y personas revisados] --> B{¿Acepta autorización para incorporar firmas?}
    B -- No --> C[No permitir captura ni envío]
    B -- Sí --> D[Registrar texto, versión, fecha y usuario]
    D --> E{¿Acepta promocionales?}
    E -- Sí --> F[Registrar consentimiento opcional]
    E -- No --> G[Registrar negativa sin bloquear el proceso]
    F --> H[Capturar firmas]
    G --> H
```

Los textos son una propuesta técnica y requieren aprobación de Legal/Privacidad antes del piloto.

## 5. Secuencia de vista previa y envío

```mermaid
sequenceDiagram
    actor C as Concierge
    participant W as Aplicación web
    participant A as API
    participant D as Base local
    participant O as OPERA/OHIP

    C->>W: Generar vista previa
    W->>A: Datos, firmas y autorizaciones
    A->>A: Validar autorización y firma titular
    A-->>W: PDF temporal
    W-->>C: Mostrar PDF dentro de la pantalla
    C->>W: Confirmar Enviar a OPERA
    W->>A: Solicitud final
    A->>D: Guardar nueva versión y evidencia
    A->>O: Crear adjunto
    O-->>A: Resultado
    A->>D: Registrar auditoría/resultado
    A-->>W: Éxito o error accionable
```

## 6. Ciclo de vida de un documento

```mermaid
stateDiagram-v2
    [*] --> Generado
    Generado --> Enviado: Concierge confirma
    Enviado --> Visible: registro y auditoría
    Visible --> Oculto: usuario autorizado + motivo
    Oculto --> Visible: restauración auditada
    Visible --> NuevaVersion: corrección o reenvío
    NuevaVersion --> Enviado

    note right of Oculto
      Baja lógica solamente.
      No elimina archivo ni adjunto de OPERA.
    end note
```

## 7. Corrección y reenvío

```mermaid
flowchart LR
    V1[Versión 1 local] --> A1[Adjunto 1 en OPERA]
    V1 --> C[Se detecta corrección]
    C --> V2[Versión 2 local]
    V2 --> A2[Adjunto 2 en OPERA]
    A1 -. pendiente .-> R[Definir cómo marcar adjunto sustituido]
    A2 --> ACT[Versión vigente]
```

El comportamiento actual conserva el historial y crea otro adjunto. Operación e Integraciones deben aprobar cómo se identifica la versión vigente en OPERA.

## 8. Decisión de identidad y correo

```mermaid
flowchart TB
    subgraph Acceso de operadores
      U[Colaborador] --> IL[Inicio local actual]
      IL -. objetivo .-> IC[Identidad corporativa individual]
      IC --> ROLES[Roles y auditoría por persona]
    end

    subgraph Envío de correo
      APP[Aplicación] --> ST[SMTP institucional transitorio]
      ST -. objetivo .-> REG[Aplicación registrada / servicio corporativo]
      REG --> CF[Cuenta funcional institucional]
    end

    X[Cuenta personal]:::blocked
    X -. no usar .-> APP

    classDef blocked fill:#f8d7da,stroke:#b02a37,color:#842029;
```

## 9. Semáforo de avance

| Estado | Elementos |
|---|---|
| 🟢 Implementado | Vista previa interna; SMTP activable sin reinicio; autorización de firma; consentimiento promocional separado; acompañantes sin edición; ocultar/restaurar documentos; OCR ampliado; menús no autorizados ocultos |
| 🟡 Requiere UAT | Envío SMTP real; integración OHIP; migración SQL; compilación Angular; tabletas físicas; firma y PDF completos; seguridad y permisos |
| 🟠 Requiere decisión | Acompañantes y menores; firmas faltantes; identificadores; datos a OPERA; correo definitivo; identidad; servidor/BD; versiones de adjuntos |
| 🔴 Bloqueo de piloto | Autorización del tratamiento digital frente a la política 2015; aprobación Legal del formato, textos, retención y documentos admitidos |

## 10. Documentos de identidad

```mermaid
flowchart TD
    A[Documento capturado] --> B{Tipo}
    B -->|INE| C[Permitido por procedimiento, sujeto a revisión]
    B -->|Pasaporte| C
    B -->|Licencia| D[OCR experimental: no autoriza check-in]
    B -->|Residencia| D
    B -->|Visa| D
    C --> E[Validación humana obligatoria]
    D --> F[Solicitar definición formal]
```

## 11. Puertas de aprobación

```mermaid
flowchart LR
    A[Proceso propuesto] --> G1{Política digital aprobada}
    G1 -- No --> X[No iniciar piloto]
    G1 -- Sí --> G2{Legal y formato aprobados}
    G2 -- No --> X
    G2 -- Sí --> G3{Reglas operativas cerradas}
    G3 -- No --> X
    G3 -- Sí --> G4{Infraestructura y seguridad aprobadas}
    G4 -- No --> X
    G4 -- Sí --> G5{UAT integral aprobada}
    G5 -- No --> X
    G5 -- Sí --> P[Piloto controlado]
```

## 12. Validaciones solicitadas a María Guadalupe (Lupita)

1. Confirmar si la aplicación puede agregar/quitar acompañantes o debe reflejar únicamente OPERA.
2. Definir inclusión de menores, edades aplicables y quién firma.
3. Definir si puede enviarse una tarjeta sin todas las firmas de acompañantes.
4. Confirmar el identificador operativo: número de confirmación, CRS, `reservationId`, habitación y vigencia.
5. Confirmar si nacionalidad, ciudad, estado y país deben escribirse en OPERA.
6. Aprobar el tratamiento de correcciones, tarjetas reenviadas y adjuntos anteriores.

## 13. Firma de validación

| Área | Nombre y firma | Decisión | Fecha |
|---|---|---|---|
| María Guadalupe / Operación |  | Aprobar / Condicionar / Rechazar |  |
| Operación Hotelera |  | Aprobar / Condicionar / Rechazar |  |
| Legal / Privacidad |  | Aprobar / Condicionar / Rechazar |  |
| Seguridad de la Información |  | Aprobar / Condicionar / Rechazar |  |
| Infraestructura / DBA |  | Aprobar / Condicionar / Rechazar |  |
| Integraciones OPERA/OHIP |  | Aprobar / Condicionar / Rechazar |  |
| Contraloría |  | Aprobar / Condicionar / Rechazar |  |

La aprobación visual debe acompañarse del documento maestro, donde se describen las condiciones, criterios de aceptación y decisiones D-01 a D-14.
