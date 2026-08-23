# SistemaGestion — Documentación del sistema completo

Documentación de diseño y referencia de **todo SistemaGestion**: el ERP de EL HATO PASCHER C.A. (RIF J-30571871-7, Villa de Cura, Aragua), incluyendo la app de escritorio (WinForms), el portal web (API + SPA), el digitalizador OCR, la integración de cámaras/CCTV (EZVIZ), el bot de WhatsApp y el módulo de redes sociales (Estudio de Contenido).

> Estos documentos describen el sistema **como está desarrollado y funcionando** a la fecha (agosto 2026). Para el detalle específico del módulo de redes sociales existe además el set en [`docs/redes-sociales/`](../redes-sociales/00-README.md); aquí se resume y se enmarca dentro del sistema completo.

## Alcance

| Incluye | No incluye |
|---|---|
| App de escritorio WinForms (.NET 8) | Código fuente línea por línea (ver el repo) |
| Portal web: API ASP.NET 8 Minimal API + SPA vanilla JS | Credenciales y secretos (viven en variables de entorno) |
| Base de datos SQLite única | Manual de usuario final (ver `manual/`) |
| Digitalizador OCR (Python) | |
| Cámaras EZVIZ / CCTV | |
| Bot de WhatsApp (Evolution API + n8n) | |
| Módulo de redes sociales (Estudio de Contenido) | |
| Infraestructura, deploy y seguridad | |

## Documentos

| # | Documento | Qué responde |
|---|---|---|
| 1 | [PRD](01-PRD.md) | Qué es el sistema y para qué: visión, usuarios, módulos, alcance, requisitos funcionales, estado |
| 2 | [TRD](02-TRD.md) | Cómo está construido: arquitectura, stack, máquinas, integraciones, seguridad, deploy |
| 3 | [UI/UX](03-UIUX.md) | Las dos interfaces (escritorio y web): navegación, pantallas, patrones |
| 4 | [App Flow](04-AppFlow.md) | Flujos end-to-end de los procesos clave del negocio |
| 5 | [Backend](05-Backend.md) | Base de datos (tablas), API (endpoints por módulo), servicios e integraciones |
| 6 | [Plan / Estado](06-Plan.md) | Qué está hecho, versiones, pendientes y hoja de ruta |
| 7 | [Web EHP](07-EHP-Web.md) | La web pública www.ehp-saddles.com (proyecto aparte, repo propio) |
| 8 | [Repos y carpetas](08-Repos-y-Carpetas.md) | Cuál es el repo real, qué carpetas sobran y plan de limpieza |

## Empresa

- **Razón social:** EL HATO PASCHER C.A.
- **RIF:** J-30571871-7
- **Rubro:** fabricación de plásticos (incluye fustes/árboles de montura de HDPE bajo la marca EHP Saddles)
- **Dirección:** Ctra. Villa de Cura - San Francisco de Asís, Sector Los Colorados, Villa de Cura, Estado Aragua
- **Teléfonos:** 0244 386.10.13 / 386.33.04

## Máquinas (resumen)

| Máquina | Rol | Notas |
|---|---|---|
| **CHAOS** | Desarrollo | Único que hace `git push`. No tiene BD de producción ni impresora |
| **OLIMPO** | Producción / admin (192.168.68.132) | BD única, app de escritorio, API, digitalizador, cámaras, túnel Cloudflare |
| **SERVO** | Oficina / usuario | Solo API web (impresión de facturas). BD por red SMB a OLIMPO |

## Convenciones de estos documentos

- Español neutro.
- Los comandos siempre indican en qué máquina se ejecutan (CHAOS / OLIMPO / SERVO).
- Los secretos nunca se escriben en el repo; van en variables de entorno *Machine scope*.
