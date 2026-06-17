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

        public bool TieneAlertas =>
            StockBajo.Count > 0 || CxCVencidas.Count > 0 || CxCProximasVencer.Count > 0 || MantVencido.Count > 0;

        public int TotalAlertas =>
            StockBajo.Count + CxCVencidas.Count + CxCProximasVencer.Count + MantVencido.Count;

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
            //    FechaVence < hoy  Y  Estado != 'Pagada'  Y  Saldo > 0
            // ------------------------------------------------------------------
            string hoy = DateTime.Today.ToString("yyyy-MM-dd");
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT Id, Cliente, Concepto, Monto, Pagado,
                           FechaEmision, FechaVence, Estado
                    FROM CuentasCobrar
                    WHERE Estado != 'Pagada'
                      AND FechaVence < $hoy
                      AND (Monto - Pagado) > 0
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
                    SELECT Id, Cliente, Concepto, Monto, Pagado,
                           FechaEmision, FechaVence, Estado
                    FROM CuentasCobrar
                    WHERE Estado NOT IN ('Pagada','Saldada')
                      AND FechaVence >= $hoy
                      AND FechaVence <= $en3
                      AND (Monto - Pagado) > 0
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

            return data;
        }
    }
}
