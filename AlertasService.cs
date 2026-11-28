using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    /// <summary>
    /// Contenedor con las tres listas de alertas.
    /// </summary>
    public class AlertasData
    {
        public List<Inventario>   StockBajo         { get; set; } = new();
        public List<CuentaCobrar> CxCVencidas       { get; set; } = new();
        public List<CuentaCobrar> CxCProximasVencer { get; set; } = new();
        public List<ProgramaMant> MantVencido       { get; set; } = new();
        // Fase 2: egresos con precio pendiente (nota de entrega sin montos) que el
        // administrador debe completar.
        public List<Egreso>       PreciosPendientes { get; set; } = new();

        public bool TieneAlertas =>
            StockBajo.Count > 0 || CxCVencidas.Count > 0 || CxCProximasVencer.Count > 0
            || MantVencido.Count > 0 || PreciosPendientes.Count > 0;

        public int TotalAlertas =>
            StockBajo.Count + CxCVencidas.Count + CxCProximasVencer.Count
            + MantVencido.Count + PreciosPendientes.Count;

        /// <summary>
        /// True si hay al menos una alerta que no fue silenciada hoy.
        /// Usa el archivo de silenciadas del dia para decidir.
        /// </summary>
        public bool TieneAlertasNoSilenciadas
        {
            get
            {
                string ruta = System.IO.Path.Combine(
                    SecurityService.CarpetaDatos,
                    $"alertas_silenciadas_{DateTime.Today:yyyyMMdd}.txt");

                var silenciadas = new System.Collections.Generic.HashSet<string>();
                if (System.IO.File.Exists(ruta))
                    foreach (var l in System.IO.File.ReadAllLines(ruta))
                        if (!string.IsNullOrWhiteSpace(l)) silenciadas.Add(l.Trim());

                if (StockBajo.Count         > 0 && !silenciadas.Contains("stock_bajo"))        return true;
                if (CxCVencidas.Count       > 0 && !silenciadas.Contains("cxc_vencidas"))      return true;
                if (CxCProximasVencer.Count > 0 && !silenciadas.Contains("cxc_por_vencer"))    return true;
                if (MantVencido.Count       > 0 && !silenciadas.Contains("mantenimiento"))     return true;
                if (PreciosPendientes.Count > 0 && !silenciadas.Contains("precios_pendientes"))return true;
                return false;
            }
        }
    }

    /// <summary>
    /// Consulta la BD y devuelve las alertas activas.
    /// </summary>
    public static class AlertasService
    {
        public static AlertasData ObtenerAlertas()
        {
            var data = new AlertasData();

            using var conn = new SqliteConnection(SecurityService.CadenaConexion);
            conn.Open();

            // ------------------------------------------------------------------
            // 1. PRODUCTOS BAJO STOCK MINIMO
            //    Existencia < ExistenMin  (y ExistenMin > 0 para ignorar sin minimo definido)
            // ------------------------------------------------------------------
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Codigo, Descripcion, Departamento, Unidad,
                           Existencia, ExistenMin, ExistenMax,
                           CostoActual, Proveedores, CodProveedor
                    FROM Inventario
                    WHERE ExistenMin > 0
                      AND Existencia < ExistenMin
                    ORDER BY (Existencia / ExistenMin) ASC";

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    data.StockBajo.Add(new Inventario
                    {
                        Codigo       = r["Codigo"]?.ToString() ?? "",
                        Descripcion  = r["Descripcion"]?.ToString() ?? "",
                        Departamento = r["Departamento"]?.ToString() ?? "",
                        Unidad       = r["Unidad"]?.ToString() ?? "",
                        Existencia   = Convert.ToDecimal(r["Existencia"]),
                        ExistenMin   = Convert.ToDecimal(r["ExistenMin"]),
                        ExistenMax   = Convert.ToDecimal(r["ExistenMax"]),
                        CostoActual  = Convert.ToDecimal(r["CostoActual"]),
                        Proveedores  = r["Proveedores"]?.ToString() ?? "",
                        CodProveedor = r["CodProveedor"]?.ToString() ?? ""
                    });
                }
            }

            // ------------------------------------------------------------------
            // 2. CUENTAS POR COBRAR VENCIDAS
            //    FechaVence < hoy  Y  Estado no cerrado  Y  Saldo > 0
            // ------------------------------------------------------------------
            DatabaseService.SincronizarCxCYFacturas();
            string hoy = DateTime.Today.ToString("yyyy-MM-dd");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Id, Cliente, Concepto, Monto, COALESCE(Pagado,0) AS Pagado,
                           FechaEmision, FechaVence, Estado
                    FROM CuentasCobrar
                    WHERE COALESCE(Estado,'Vigente') NOT IN ('Pagada','Saldada','Pagado','Anulada')
                      AND substr(FechaVence,1,10) < $hoy
                      AND (Monto - COALESCE(Pagado,0)) > 0.005
                    ORDER BY FechaVence ASC";
                cmd.Parameters.AddWithValue("$hoy", hoy);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    data.CxCVencidas.Add(new CuentaCobrar
                    {
                        Id           = Convert.ToInt32(r["Id"]),
                        Cliente      = r["Cliente"]?.ToString() ?? "",
                        Concepto     = r["Concepto"]?.ToString() ?? "",
                        Monto        = Convert.ToDecimal(r["Monto"]),
                        Pagado       = Convert.ToDecimal(r["Pagado"]),
                        FechaEmision = Convert.ToDateTime(r["FechaEmision"]),
                        FechaVence   = Convert.ToDateTime(r["FechaVence"]),
                        Estado       = r["Estado"]?.ToString() ?? ""
                    });
                }
            }

            // ------------------------------------------------------------------
            // 3. CUENTAS POR COBRAR PRÓXIMAS A VENCER (dentro de 3 días)
            // ------------------------------------------------------------------
            string en3dias = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Id, Cliente, Concepto, Monto, COALESCE(Pagado,0) AS Pagado,
                           FechaEmision, FechaVence, Estado
                    FROM CuentasCobrar
                    WHERE COALESCE(Estado,'Vigente') NOT IN ('Pagada','Saldada','Pagado','Anulada')
                      AND substr(FechaVence,1,10) >= $hoy
                      AND substr(FechaVence,1,10) <= $en3
                      AND (Monto - COALESCE(Pagado,0)) > 0.005
                    ORDER BY FechaVence ASC";
                cmd.Parameters.AddWithValue("$hoy", hoy);
                cmd.Parameters.AddWithValue("$en3", en3dias);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    data.CxCProximasVencer.Add(new CuentaCobrar
                    {
                        Id           = Convert.ToInt32(r["Id"]),
                        Cliente      = r["Cliente"]?.ToString() ?? "",
                        Concepto     = r["Concepto"]?.ToString() ?? "",
                        Monto        = Convert.ToDecimal(r["Monto"]),
                        Pagado       = Convert.ToDecimal(r["Pagado"]),
                        FechaEmision = Convert.ToDateTime(r["FechaEmision"]),
                        FechaVence   = Convert.ToDateTime(r["FechaVence"]),
                        Estado       = r["Estado"]?.ToString() ?? ""
                    });
                }
            }

            // ------------------------------------------------------------------
            // 4. PROGRAMAS DE MANTENIMIENTO VENCIDOS O URGENTES
            //    DiasRestantes = (UltimoMant + FrecuenciaDias) - Hoy
            //    Vencido: DiasRestantes < 0
            //    Urgente: DiasRestantes entre 0 y 7
            // ------------------------------------------------------------------
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Id, CodigoMolde, DescMolde, TipoMant,
                           FrecuenciaDias, UltimoMant, Activo
                    FROM ProgramasMantenimiento
                    WHERE Activo = 1
                    ORDER BY UltimoMant ASC";

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var prog = new ProgramaMant
                    {
                        Id            = Convert.ToInt32(r["Id"]),
                        CodigoMolde   = r["CodigoMolde"]?.ToString() ?? "",
                        DescMolde     = r["DescMolde"]?.ToString() ?? "",
                        TipoMant      = r["TipoMant"]?.ToString() ?? "",
                        FrecuenciaDias= Convert.ToInt32(r["FrecuenciaDias"]),
                        UltimoMant    = Convert.ToDateTime(r["UltimoMant"]),
                        Activo        = Convert.ToInt32(r["Activo"]) == 1
                    };

                    // Solo incluir vencidos (< 0) y urgentes (<= 7)
                    if (prog.DiasRestantes <= 7)
                        data.MantVencido.Add(prog);
                }
            }

            // ------------------------------------------------------------------
            // 5. EGRESOS CON PRECIO PENDIENTE (nota de entrega sin montos)
            //    El admin debe completar el precio. Se envuelve en try/catch por
            //    si la columna PrecioPendiente aún no existe en una BD vieja.
            // ------------------------------------------------------------------
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT g.Id, g.Fecha, g.Concepto, COALESCE(g.Proveedor,''),
                           COALESCE(g.Cantidad,0), g.IdInsumo, COALESCE(c.Nombre,'')
                    FROM Gastos g
                    LEFT JOIN CategoriasGasto c ON c.Id = g.IdCategoria
                    WHERE COALESCE(g.PrecioPendiente,0) = 1 AND g.Estado != 'Anulado'
                    ORDER BY g.Fecha ASC, g.Id ASC";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    data.PreciosPendientes.Add(new Egreso
                    {
                        Id              = Convert.ToInt32(r.GetInt32(0)),
                        Fecha           = DateTime.TryParse(r.GetString(1), out var fp) ? fp : DateTime.Today,
                        Concepto        = r.IsDBNull(2) ? "" : r.GetString(2),
                        Proveedor       = r.IsDBNull(3) ? "" : r.GetString(3),
                        Cantidad        = (decimal)r.GetDouble(4),
                        IdInsumo        = r.IsDBNull(5) ? (int?)null : r.GetInt32(5),
                        NombreCategoria = r.IsDBNull(6) ? "" : r.GetString(6),
                        PrecioPendiente = true
                    });
                }
            }
            catch { /* columna PrecioPendiente inexistente en BD vieja: ignorar */ }

            return data;
        }
    }
}
