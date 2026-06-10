# Rotación de claves — Acciones pendientes

## Prioridad Alta

### 1. Clave API Anthropic (digitalizador/.env)
- **Clave**: `sk-ant-api03-7ovN...` (en `digitalizador/.env` en OLIMPO y CHAOS)
- **Acción**: Ir a https://console.anthropic.com/settings/keys → revocar la clave actual → generar una nueva → actualizar `digitalizador/.env` en OLIMPO (`C:\SistemaGestion\digitalizador\.env`)
- **Verificar**: Reiniciar el digitalizador y probar extracción de documento

### 2. Token Telegram + OpenRouter (ehp-bot/.env)
- **Clave Telegram**: Revocar con @BotFather → /revoke → generar nuevo token
- **Clave OpenRouter**: Ir al dashboard de OpenRouter → revocar → generar nueva
- **Acción**: Actualizar `ehp-bot/.env` en todas las máquinas donde corre

### 3. Credenciales Gmail (ehp-bot/.env)
- **Cuenta**: hatopascher@gmail.com con app password
- **Acción**: Ir a https://myaccount.google.com/apppasswords → revocar app password actual → generar nueva → actualizar en `ehp-bot/.env`

## Prioridad Media

### 4. Verificar permisos de archivos .env
- En OLIMPO: `icacls C:\SistemaGestion\digitalizador\.env` — solo el usuario PC debe tener acceso
- En SERVO: verificar que no haya copias de .env innecesarias

## Estado
- [ ] Clave Anthropic rotada
- [ ] Token Telegram rotado
- [ ] Clave OpenRouter rotada
- [ ] App password Gmail rotada
- [ ] Permisos .env verificados
