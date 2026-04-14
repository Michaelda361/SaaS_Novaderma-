---
inclusion: always
---

# Product: NovaHub — Gestión de Talento

NovaHub es una aplicación web de gestión de talento para organizaciones. Permite a los equipos de RRHH gestionar:

- **Colaboradores** — información personal, área, cargo, jerarquía de supervisores, rol en la app
- **Capacitaciones** — programación, inscripciones, recursos adjuntos, cuestionarios de evaluación
- **Certificados** — emisión, seguimiento de vencimientos, alertas de próximos vencimientos
- **Áreas** — unidades organizacionales con jefe asignado
- **Cargos** — puestos de trabajo scoped a un área
- **Inscripciones** — vinculación de colaboradores a capacitaciones con calificaciones
- **Recursos** — materiales adjuntos a capacitaciones (video, documento, presentación, enlace)
- **Cuestionarios** — evaluaciones con preguntas de opción múltiple asociadas a capacitaciones
- **Control Documental** — documentos organizacionales con flujo de aprobación y propuestas de cambio, almacenados en SharePoint
- **Cartas Laborales** — generación de cartas en PDF desde plantillas HTML o DOCX, con flujo de aprobación del admin

## Roles de usuario

| Rol | Descripción | Acceso |
|---|---|---|
| Admin | Administrador del sistema | Acceso total a todos los módulos |
| Jefe | Jefe de área | Gestión de su área, revisión de propuestas documentales |
| Colaborador | Empleado regular | Acceso restringido: ver sus datos, solicitar cartas, proponer cambios en documentos |

El rol se almacena en la entidad Colaborador (campo Rol, enum RolUsuario).
En producción se valida contra Azure Entra ID. En desarrollo se usa DevAuthHandler.

## Idioma
La UI está completamente en español. Mensajes de error, labels, toasts y textos de la interfaz deben estar en español.

## Seguridad
La app está asegurada via Azure Entra ID (MSAL). Todos los endpoints API requieren Bearer token válido.
En Development se usa DevAuthHandler con header X-Dev-User como alternativa.
