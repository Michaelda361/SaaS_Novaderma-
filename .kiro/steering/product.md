# Product: NovaHub — Gestión de Talento

NovaHub is a talent management web application for organizations. It allows HR teams to manage:

- **Colaboradores** (employees) — personal info, area, cargo, supervisor hierarchy
- **Capacitaciones** (training sessions) — scheduling, enrollment, resources
- **Certificados** (certifications) — issuance, expiry tracking, alerts for upcoming expirations
- **Áreas** (departments) — organizational units with an assigned jefe (head)
- **Cargos** (job positions) — roles scoped to areas
- **Inscripciones** (enrollments) — linking colaboradores to capacitaciones with grades
- **Recursos** (resources) — materials attached to capacitaciones

The app is secured via Azure Entra ID (MSAL). All API endpoints require a valid Bearer token. The UI is in Spanish.
