SELECT id, categoria, descripcion, monto, moneda, notas, created_at,
       tipo, periodicidad, proximo_cobro, ambito, activo
FROM gastos
WHERE COALESCE(descripcion,'') NOT ILIKE '%(ignorar)%'
  AND COALESCE(notas,'')       NOT ILIKE '%(ignorar)%'
  AND COALESCE(activo, true) = true
ORDER BY created_at DESC, id DESC;
