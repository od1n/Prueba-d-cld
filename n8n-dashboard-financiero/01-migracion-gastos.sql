-- ============================================================
-- Migración + carga de gastos  (ejecutar UNA sola vez)
-- Base: PostgreSQL (credencial n8n "Postgres account")
-- Es idempotente: se puede correr de nuevo sin duplicar.
-- ============================================================

-- 1) Ampliar la tabla gastos con campos financieros
ALTER TABLE gastos ADD COLUMN IF NOT EXISTS tipo          text    DEFAULT 'unico';       -- 'recurrente' | 'unico'
ALTER TABLE gastos ADD COLUMN IF NOT EXISTS periodicidad  text;                          -- 'mensual' | 'anual' | 'consumo'
ALTER TABLE gastos ADD COLUMN IF NOT EXISTS proximo_cobro date;                          -- para recurrentes
ALTER TABLE gastos ADD COLUMN IF NOT EXISTS ambito        text    DEFAULT 'general';     -- este|general|motoapp|ehp|personal
ALTER TABLE gastos ADD COLUMN IF NOT EXISTS activo        boolean DEFAULT true;

-- 1b) Permitir monto nulo (p. ej. Hetzner: recurrente aún sin primera factura)
ALTER TABLE gastos ALTER COLUMN monto DROP NOT NULL;

-- 2) Reclasificar las 2 filas que ya existían
UPDATE gastos
   SET tipo='recurrente', periodicidad='mensual', proximo_cobro=DATE '2026-09-11',
       ambito='este', categoria='Plataforma'
 WHERE descripcion ILIKE '%Freelancer Plus%';

-- La recarga Anthropic de $25 (10/08) se ajusta a la compra de créditos de $20.20 (con imp.)
UPDATE gastos
   SET tipo='unico', ambito='general', categoria='IA/API', monto=20.20, moneda='USD',
       descripcion='Créditos API Anthropic ($20 + 1% imp.)'
 WHERE descripcion ILIKE '%Recarga de saldo%' OR descripcion ILIKE '%API de Anthropic%';

-- 3) Cargar el resto de los gastos (solo si no existen ya, por descripción)
INSERT INTO gastos (categoria, descripcion, monto, moneda, tipo, periodicidad, proximo_cobro, ambito, notas, created_at)
SELECT * FROM (VALUES
  ('IA/API',          'Anthropic Max plan 5x',                     106.35::numeric, 'USD', 'recurrente', 'mensual',  DATE '2026-09-21', 'general', 'Base $100 + 6,35% imp. Se usa en todos los proyectos', TIMESTAMP '2026-08-21'),
  ('Infraestructura', 'Vercel — Neon Postgres (MotoApp)',            8.87::numeric, 'USD', 'recurrente', 'consumo',  DATE '2026-09-03', 'motoapp', 'Por consumo, varía cada mes (jul $18.02, ago $8.87)',  TIMESTAMP '2026-08-03'),
  ('Suscripción',     'Google One — AI Plus 400 GB',                 2.49::numeric, 'USD', 'recurrente', 'mensual',  DATE '2026-09-10', 'general', 'Almacenamiento 400 GB',                                TIMESTAMP '2026-07-10'),
  ('Suscripción',     'Paramount+ (Standard)',                       5.99::numeric, 'USD', 'recurrente', 'mensual',  DATE '2026-09-09', 'personal','Entretenimiento — personal',                           TIMESTAMP '2026-08-09'),
  ('Infraestructura', 'Hetzner CX23 — VPS (n8n)',                    NULL::numeric, 'USD', 'recurrente', 'mensual',  DATE '2026-08-28', 'este',    'Aún sin primera factura; monto por confirmar (~€6-7). Factura el 28', TIMESTAMP '2026-08-01'),
  ('Dominio',         'Cloudflare — dominio hato-pascher.com',      22.25::numeric, 'USD', 'recurrente', 'anual',    DATE '2027-08-15', 'ehp',     'Registro anual; dominio de SistemaGestion/EHP (otro proyecto)',      TIMESTAMP '2026-08-15'),
  ('Plataforma',      '100 Connects Upwork (top-up)',               15.00::numeric, 'USD', 'unico',      NULL,       NULL,              'este',    'Compra puntual, aparte de la suscripción Plus',        TIMESTAMP '2026-08-24'),
  ('IA/API',          'Créditos API Anthropic ($5 + 1% imp.)',       5.05::numeric, 'USD', 'unico',      NULL,       NULL,              'general', 'Compra puntual de créditos',                           TIMESTAMP '2026-08-10')
) AS v(categoria, descripcion, monto, moneda, tipo, periodicidad, proximo_cobro, ambito, notas, created_at)
WHERE NOT EXISTS (SELECT 1 FROM gastos g WHERE g.descripcion = v.descripcion);

-- Verificación rápida (opcional):
-- SELECT tipo, ambito, descripcion, monto, periodicidad, proximo_cobro FROM gastos ORDER BY tipo, proximo_cobro;
