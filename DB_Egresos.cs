// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: DB_Egresos.cs  →  raíz del proyecto
//
// CRUD para el módulo de Egresos (Gastos + Categorías).
//
// Diseño:
//   - Tabla unificada Gastos para TODOS los egresos (materia prima + ops).
//   - Multi-moneda: TipoTasa decide cómo se calcula MontoUSD/MontoBs.
//   - Snapshot de IdTasaCambio (igual patrón que Facturas) para que reportes
//     históricos no cambien si se modifica una tasa antigua.
//   - Validación de permisos por rol al guardar/anular.
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    public class CategoriaGasto
    {
        public int    Id              { get; set; }
        public string Nombre          { get; set; } = "";
        public string Color           { get; set; } = "#1A4A8A";
        public bool   Activa          { get; set; } = true;
        public int    Orden           { get; set; }
        public bool   EsMateriaPrima  { get; set; }
    }

    public class Egreso
    {
        public int      Id              { get; set; }
        public DateTime Fecha           { get; set; }
        public int      IdCategoria     { get; set; }
        public string   NombreCategoria { get; set; } = "";  // join
        public string   Concepto        { get; set; } = "";
        public string   Proveedor       { get; set; } = "";
        public string   CodigoProveedor { get; set; } = "";  // enlace al directorio de Proveedores
        // Materia prima (opcional)
        public int?     IdInsumo        { get; set; }
        public decimal  Cantidad        { get; set; }
        // Multi-moneda
        public string   MonedaOriginal  { get; set; } = "USD";   // 'USD' | 'BS'
        public string   TipoTasa        { get; set; } = "USD_DIRECTO";
                                            // 'USD_DIRECTO' | 'BCV' | 'PARALELO' | 'PERSONALIZADA'
        public decimal  TasaUsada       { get; set; }
        public int?     IdTasaCambio    { get; set; }
        public decimal  MontoOriginal   { get; set; }
        public decimal  MontoUSD        { get; set; }
        public decimal  MontoBs         { get; set; }
        // Pago / CxP: cuánto se abonó (en USD). Si PagadoUSD < MontoUSD el saldo
        // genera automáticamente una Cuenta por Pagar. Por defecto se paga completo.
        public decimal  PagadoUSD       { get; set; }
        public decimal  SaldoUSD        => Math.Max(0m, MontoUSD - PagadoUSD);
        // Estado / CxP
        public string   Estado          { get; set; } = "Pagado";  // 'Pendiente' | 'Pagado' | 'Anulado'
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaPago        { get; set; }
        // Comprobante
        public string   RutaComprobante { get; set; } = "";
        public string   Usuario         { get; set; } = "";
        public DateTime FechaRegistro   { get; set; }
        public string   Observaciones   { get; set; } = "";
    }

    public static partial class DatabaseService
    {
        // ── CRUD CategoriasGasto ────────────────────────────────────────────
        public static List<CategoriaGasto> ObtenerCategoriasGasto(bool incluirInactivas = false)
        {
            var list = new List<CategoriaGasto>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id, Nombre, COALESCE(Color,'#1A4A8A'), Activa, " +
                              "Orden, EsMateriaPrima FROM CategoriasGasto " +
                              (incluirInactivas ? "" : "WHERE Activa = 1 ") +
                              "ORDER BY Orden, Nombre";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new CategoriaGasto
                {
                    Id              = r.GetInt32(0),
                    Nombre          = r.GetString(1),
                    Color           = r.GetString(2),
                    Activa          = r.GetInt32(3) == 1,
                    Orden           = r.GetInt32(4),
                    EsMateriaPrima  = r.GetInt32(5) == 1
                });
            return list;
        }

        public static void GuardarCategoriaGasto(CategoriaGasto cat)
        {
            string rol = Sesion.RolesEfectivos;
            if (!Sesion.EsAdmin && !PermisosRol.Permite(rol, "EgresosCategorias"))
                throw new UnauthorizedAccessException(
                    "Tu rol no tiene permiso para gestionar categorías de gasto.");

            using var c = Conn();
            var cmd = c.CreateCommand();
            if (cat.Id == 0)
                cmd.CommandText = @"INSERT INTO CategoriasGasto
                    (Nombre, Color, Activa, Orden, EsMateriaPrima)
                    VALUES ($n, $c, $a, $o, $mp)";
            else
            {
                cmd.CommandText = @"UPDATE CategoriasGasto SET
                    Nombre = $n, Color = $c, Activa = $a, Orden = $o,
                    EsMateriaPrima = $mp
                    WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", cat.Id);
            }
            cmd.Parameters.AddWithValue("$n",  cat.Nombre);
            cmd.Parameters.AddWithValue("$c",  cat.Color ?? "#1A4A8A");
            cmd.Parameters.AddWithValue("$a",  cat.Activa ? 1 : 0);
            cmd.Parameters.AddWithValue("$o",  cat.Orden);
            cmd.Parameters.AddWithValue("$mp", cat.EsMateriaPrima ? 1 : 0);
            cmd.ExecuteNonQuery();

            RegistrarAuditoria(cat.Id == 0 ? "CATGASTO_CREADA" : "CATGASTO_MODIFICADA",
                "CategoriasGasto", cat.Nombre);
        }

        // ── CRUD Gastos ─────────────────────────────────────────────────────
        /// <summary>
        /// Inserta o actualiza un gasto. Calcula MontoUSD y MontoBs según
        /// MonedaOriginal + TipoTasa. Captura snapshot de IdTasaCambio si aplica.
        /// Valida permisos por rol antes de guardar.
        /// </summary>
        public static int GuardarEgreso(Egreso g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (g.IdCategoria == 0) throw new Exception("Categoría requerida.");
            if (string.IsNullOrWhiteSpace(g.Concepto)) throw new Exception("Concepto requerido.");
            if (g.MontoOriginal <= 0) throw new Exception("El monto debe ser mayor que cero.");

            string rol = Sesion.RolesEfectivos;
            if (!Sesion.EsAdmin && !PermisosRol.Permite(rol, "EgresosRegistrar"))
                throw new UnauthorizedAccessException(
                    "Tu rol (" + rol + ") no tiene permiso para registrar egresos.");

            CalcularMontos(g);

            // ── Pago / estado a partir de lo abonado (PagadoUSD) ────────────────
            // Compatibilidad: si el caller marcó 'Pagado' sin abono explícito, se
            // asume pago completo. Si PagadoUSD < MontoUSD el egreso queda Pendiente
            // y el saldo generará una Cuenta por Pagar (más abajo).
            if (g.Estado != "Anulado")
            {
                if (g.Estado == "Pagado" && g.PagadoUSD <= 0) g.PagadoUSD = g.MontoUSD;
                if (g.PagadoUSD < 0) g.PagadoUSD = 0;
                if (g.PagadoUSD >= g.MontoUSD - 0.01m)
                {
                    g.PagadoUSD = g.MontoUSD;
                    g.Estado = "Pagado";
                    if (g.FechaPago == null) g.FechaPago = DateTime.Today;
                    g.FechaVencimiento = null;
                }
                else
                {
                    g.Estado = "Pendiente";
                    if (g.FechaPago == null && g.PagadoUSD > 0) g.FechaPago = DateTime.Today;
                }
            }

            // Capturar snapshot de IdTasaCambio si la conversión usó BCV o Paralelo.
            if (g.IdTasaCambio == null && g.TipoTasa is "BCV" or "PARALELO")
            {
                try
                {
                    var t = ObtenerTasaPorFecha(g.Fecha);
                    if (t != null) g.IdTasaCambio = t.Id;
                }
                catch { }
            }

            if (g.FechaRegistro == default) g.FechaRegistro = DateTime.Now;
            if (string.IsNullOrEmpty(g.Usuario)) g.Usuario = Sesion.NombreUsuario ?? "Admin";

            using var c = Conn();
            var cmd = c.CreateCommand();

            bool esNuevo = g.Id == 0;
            if (esNuevo)
            {
                cmd.CommandText = @"INSERT INTO Gastos
                    (Fecha, IdCategoria, Concepto, Proveedor, CodigoProveedor, IdInsumo, Cantidad,
                     MonedaOriginal, TipoTasa, TasaUsada, IdTasaCambio,
                     MontoOriginal, MontoUSD, MontoBs, PagadoUSD,
                     Estado, FechaVencimiento, FechaPago, RutaComprobante,
                     Usuario, FechaRegistro, Observaciones)
                    VALUES
                    ($f, $cat, $con, $prov, $cprov, $iins, $cant,
                     $mon, $tt, $tu, $idt,
                     $mo, $musd, $mbs, $pag,
                     $es, $fv, $fp, $rc,
                     $us, $fr, $obs);
                    SELECT last_insert_rowid();";
            }
            else
            {
                cmd.CommandText = @"UPDATE Gastos SET
                    Fecha=$f, IdCategoria=$cat, Concepto=$con, Proveedor=$prov, CodigoProveedor=$cprov,
                    IdInsumo=$iins, Cantidad=$cant,
                    MonedaOriginal=$mon, TipoTasa=$tt, TasaUsada=$tu, IdTasaCambio=$idt,
                    MontoOriginal=$mo, MontoUSD=$musd, MontoBs=$mbs, PagadoUSD=$pag,
                    Estado=$es, FechaVencimiento=$fv, FechaPago=$fp, RutaComprobante=$rc,
                    Observaciones=$obs
                    WHERE Id=$id";
                cmd.Parameters.AddWithValue("$id", g.Id);
            }

            cmd.Parameters.AddWithValue("$f",   g.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cat", g.IdCategoria);
            cmd.Parameters.AddWithValue("$con", g.Concepto);
            cmd.Parameters.AddWithValue("$prov",g.Proveedor ?? "");
            cmd.Parameters.AddWithValue("$cprov",g.CodigoProveedor ?? "");
            cmd.Parameters.AddWithValue("$iins",(object?)g.IdInsumo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$cant",(double)g.Cantidad);
            cmd.Parameters.AddWithValue("$mon", g.MonedaOriginal);
            cmd.Parameters.AddWithValue("$tt",  g.TipoTasa);
            cmd.Parameters.AddWithValue("$tu",  (double)g.TasaUsada);
            cmd.Parameters.AddWithValue("$idt", (object?)g.IdTasaCambio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$mo",  (double)g.MontoOriginal);
            cmd.Parameters.AddWithValue("$musd",(double)g.MontoUSD);
            cmd.Parameters.AddWithValue("$mbs", (double)g.MontoBs);
            cmd.Parameters.AddWithValue("$pag", (double)g.PagadoUSD);
            cmd.Parameters.AddWithValue("$es",  g.Estado);
            cmd.Parameters.AddWithValue("$fv",  (object?)g.FechaVencimiento?.ToString("yyyy-MM-dd") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$fp",  (object?)g.FechaPago?.ToString("yyyy-MM-dd")        ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$rc",  g.RutaComprobante ?? "");
            if (esNuevo)
            {
                cmd.Parameters.AddWithValue("$us",  g.Usuario);
                cmd.Parameters.AddWithValue("$fr",  g.FechaRegistro.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            cmd.Parameters.AddWithValue("$obs", g.Observaciones ?? "");

            int idResultante = g.Id;
            if (esNuevo)
                idResultante = Convert.ToInt32(cmd.ExecuteScalar());
            else
                cmd.ExecuteNonQuery();

            RegistrarAuditoria(esNuevo ? "GASTO_REGISTRADO" : "GASTO_MODIFICADO", "Gastos",
                $"#{idResultante} {g.Fecha:dd/MM/yyyy} cat={g.IdCategoria} " +
                $"USD={g.MontoUSD:N2} ({g.TipoTasa}) {g.Concepto}");

            // ── Integración Materia Prima → Kardex + Stock ──────────────────
            // Si el egreso es nuevo, tiene IdInsumo y Cantidad > 0, registramos
            // automáticamente la entrada en el Kardex A (insumos), lo que
            // actualiza el StockActual y recalcula el CPP del insumo.
            // Esto evita doble entrada (gasto + compra) — un solo registro
            // dispara ambos lados.
            if (esNuevo && g.IdInsumo.HasValue && g.IdInsumo.Value > 0 && g.Cantidad > 0)
            {
                try
                {
                    var pp = ObtenerProductoProveedorPorId(g.IdInsumo.Value);
                    if (pp != null && !string.IsNullOrEmpty(pp.CodigoInterno))
                    {
                        decimal precioUnit = g.Cantidad > 0
                            ? Math.Round(g.MontoUSD / g.Cantidad, 4) : 0m;
                        RegistrarEntradaInsumo(
                            codigoInsumo: pp.CodigoInterno,
                            cantidad: g.Cantidad,
                            costoUnitario: precioUnit,
                            concepto: "Compra MP — egreso #" + idResultante + " — " + g.Concepto,
                            referencia: "Gasto#" + idResultante,
                            usuario: g.Usuario);
                    }
                }
                catch (Exception ex)
                {
                    // No abortamos el guardado del egreso; solo dejamos auditoría
                    // del problema con el kardex.
                    RegistrarAuditoria("GASTO_KARDEX_FALLO", "Gastos",
                        "Egreso#" + idResultante + " no actualizó kardex: " + ex.Message);
                }
            }

            // ── Egreso a crédito → Cuenta por Pagar por el saldo ────────────────
            // Si quedó saldo pendiente (PagadoUSD < MontoUSD) se crea/actualiza una
            // CxP ligada a este egreso; si se pagó completo y existía una, se salda.
            try
            {
                SincronizarCxPDesdeEgreso(
                    idEgreso: idResultante,
                    proveedor: g.Proveedor ?? "",
                    codigoProveedor: g.CodigoProveedor ?? "",
                    concepto:  g.Concepto,
                    numRef:    "Egreso #" + idResultante,
                    montoUSD:  g.MontoUSD,
                    pagadoUSD: g.PagadoUSD,
                    emision:   g.Fecha,
                    vence:     g.FechaVencimiento ?? g.Fecha.AddDays(30));
            }
            catch (Exception ex)
            {
                RegistrarAuditoria("GASTO_CXP_FALLO", "Gastos",
                    "Egreso#" + idResultante + " no sincronizó CxP: " + ex.Message);
            }

            return idResultante;
        }

        /// <summary>
        /// Calcula MontoUSD y MontoBs según moneda original y tipo de tasa.
        /// Reglas:
        ///   - USD_DIRECTO: MontoUSD = MontoOriginal (no se requiere tasa).
        ///   - BCV / PARALELO / PERSONALIZADA: TasaUsada > 0; si MonedaOriginal=BS,
        ///     MontoUSD = MontoOriginal / TasaUsada y MontoBs = MontoOriginal.
        ///     Si MonedaOriginal=USD, MontoBs = MontoOriginal * TasaUsada.
        /// </summary>
        public static void CalcularMontos(Egreso g)
        {
            if (g.TipoTasa == "USD_DIRECTO")
            {
                g.MonedaOriginal = "USD";
                g.TasaUsada = 0;
                g.MontoUSD = g.MontoOriginal;
                g.MontoBs  = 0;
                return;
            }
            if (g.TasaUsada <= 0)
                throw new Exception("La tasa debe ser mayor que cero para tipo " + g.TipoTasa + ".");

            if (g.MonedaOriginal == "BS")
            {
                g.MontoBs  = g.MontoOriginal;
                g.MontoUSD = Math.Round(g.MontoOriginal / g.TasaUsada, 4);
            }
            else  // USD
            {
                g.MontoUSD = g.MontoOriginal;
                g.MontoBs  = Math.Round(g.MontoOriginal * g.TasaUsada, 2);
            }
        }

        public static void AnularEgreso(int id, string motivo)
        {
            string rol = Sesion.RolesEfectivos;
            if (!Sesion.EsAdmin && !PermisosRol.Permite(rol, "EgresosAnular"))
                throw new UnauthorizedAccessException(
                    "Tu rol no tiene permiso para anular egresos.");

            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Gastos SET Estado='Anulado', " +
                              "Observaciones=COALESCE(Observaciones,'') || ' | ANULADO: ' || $m " +
                              "WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$m",  motivo ?? "");
            cmd.ExecuteNonQuery();

            RegistrarAuditoria("GASTO_ANULADO", "Gastos", "#" + id + " — " + motivo);
        }

        public static void MarcarEgresoPagado(int id, DateTime fechaPago)
        {
            string rol = Sesion.RolesEfectivos;
            if (!Sesion.EsAdmin && !PermisosRol.Permite(rol, "EgresosRegistrar"))
                throw new UnauthorizedAccessException(
                    "Tu rol no tiene permiso para marcar pagos.");

            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Gastos SET Estado='Pagado', FechaPago=$fp WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$fp", fechaPago.ToString("yyyy-MM-dd"));
            cmd.ExecuteNonQuery();

            RegistrarAuditoria("GASTO_PAGADO", "Gastos",
                "#" + id + " pagado el " + fechaPago.ToString("dd/MM/yyyy"));
        }

        // ── Listados ────────────────────────────────────────────────────────
        public static List<Egreso> ObtenerEgresosPorPeriodo(
            DateTime desde, DateTime hasta,
            int? idCategoria = null,
            string? estado   = null,
            bool soloMateriaPrima = false)
        {
            var list = new List<Egreso>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            var sb = new System.Text.StringBuilder();
            sb.Append(@"
                SELECT g.Id, g.Fecha, g.IdCategoria, COALESCE(c.Nombre,''),
                       g.Concepto, COALESCE(g.Proveedor,''),
                       g.IdInsumo, COALESCE(g.Cantidad, 0),
                       g.MonedaOriginal, g.TipoTasa, g.TasaUsada, g.IdTasaCambio,
                       g.MontoOriginal, g.MontoUSD, g.MontoBs,
                       g.Estado, g.FechaVencimiento, g.FechaPago,
                       COALESCE(g.RutaComprobante,''),
                       g.Usuario, g.FechaRegistro, COALESCE(g.Observaciones,''),
                       COALESCE(g.PagadoUSD,0), COALESCE(g.CodigoProveedor,'')
                FROM Gastos g
                LEFT JOIN CategoriasGasto c ON c.Id = g.IdCategoria
                WHERE g.Fecha >= $d AND g.Fecha <= $h ");
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            if (idCategoria.HasValue) {
                sb.Append(" AND g.IdCategoria=$cat");
                cmd.Parameters.AddWithValue("$cat", idCategoria.Value);
            }
            if (!string.IsNullOrEmpty(estado)) {
                sb.Append(" AND g.Estado=$es");
                cmd.Parameters.AddWithValue("$es", estado);
            }
            if (soloMateriaPrima) {
                sb.Append(" AND c.EsMateriaPrima=1");
            }
            sb.Append(" ORDER BY g.Fecha DESC, g.Id DESC LIMIT 1000");
            cmd.CommandText = sb.ToString();

            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(BindEgreso(r));
            return list;
        }

        public static List<Egreso> ObtenerEgresosPorPagar()
        {
            var list = new List<Egreso>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT g.Id, g.Fecha, g.IdCategoria, COALESCE(c.Nombre,''),
                       g.Concepto, COALESCE(g.Proveedor,''),
                       g.IdInsumo, COALESCE(g.Cantidad, 0),
                       g.MonedaOriginal, g.TipoTasa, g.TasaUsada, g.IdTasaCambio,
                       g.MontoOriginal, g.MontoUSD, g.MontoBs,
                       g.Estado, g.FechaVencimiento, g.FechaPago,
                       COALESCE(g.RutaComprobante,''),
                       g.Usuario, g.FechaRegistro, COALESCE(g.Observaciones,''),
                       COALESCE(g.PagadoUSD,0), COALESCE(g.CodigoProveedor,'')
                FROM Gastos g
                LEFT JOIN CategoriasGasto c ON c.Id = g.IdCategoria
                WHERE g.Estado = 'Pendiente'
                ORDER BY g.FechaVencimiento ASC NULLS LAST, g.Fecha ASC";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(BindEgreso(r));
            return list;
        }

        /// <summary>
        /// Resumen de egresos del periodo agrupado por categoría.
        /// Devuelve (CategoriaId, Nombre, Color, TotalUSD, Cantidad).
        /// </summary>
        public static List<(int Id, string Nombre, string Color, decimal TotalUSD, int Cantidad)>
            ResumenEgresosPorCategoria(DateTime desde, DateTime hasta)
        {
            var list = new List<(int, string, string, decimal, int)>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT c.Id, c.Nombre, COALESCE(c.Color, '#888780'),
                       COALESCE(SUM(g.MontoUSD), 0), COUNT(g.Id)
                FROM CategoriasGasto c
                LEFT JOIN Gastos g
                       ON g.IdCategoria = c.Id
                      AND g.Fecha >= $d AND g.Fecha <= $h
                      AND g.Estado != 'Anulado'
                GROUP BY c.Id, c.Nombre, c.Color
                ORDER BY SUM(g.MontoUSD) DESC";
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetInt32(0), r.GetString(1), r.GetString(2),
                          (decimal)r.GetDouble(3), r.GetInt32(4)));
            return list;
        }

        /// <summary>
        /// Total de egresos en USD para el período (excluye anulados).
        /// </summary>
        public static decimal TotalEgresosUSDPorPeriodo(DateTime desde, DateTime hasta)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(MontoUSD), 0) FROM Gastos
                WHERE Fecha >= $d AND Fecha <= $h AND Estado != 'Anulado'";
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            return (decimal)Convert.ToDouble(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Para el reporte de P&L: egresos por mes desglosados en
        /// (MateriaPrima, GastosOperativos). Excluye anulados.
        /// </summary>
        public static List<(int Ano, int Mes, decimal MateriaPrima, decimal Operativos)>
            ObtenerEgresosMensualesPorPeriodo(DateTime desde, DateTime hasta)
        {
            var list = new List<(int, int, decimal, decimal)>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT CAST(strftime('%Y', g.Fecha) AS INTEGER) AS Ano,
                       CAST(strftime('%m', g.Fecha) AS INTEGER) AS Mes,
                       COALESCE(SUM(CASE WHEN c.EsMateriaPrima = 1 THEN g.MontoUSD ELSE 0 END), 0) AS MP,
                       COALESCE(SUM(CASE WHEN c.EsMateriaPrima = 0 THEN g.MontoUSD ELSE 0 END), 0) AS Op
                FROM Gastos g
                LEFT JOIN CategoriasGasto c ON c.Id = g.IdCategoria
                WHERE g.Fecha >= $d AND g.Fecha <= $h AND g.Estado != 'Anulado'
                GROUP BY Ano, Mes
                ORDER BY Ano, Mes";
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetInt32(0), r.GetInt32(1),
                    (decimal)r.GetDouble(2), (decimal)r.GetDouble(3)));
            return list;
        }

        /// <summary>
        /// Para el reporte de P&L: ventas en USD por mes en el período.
        /// </summary>
        public static List<(int Ano, int Mes, decimal VentasUSD)>
            ObtenerVentasMensualesUSDPorPeriodo(DateTime desde, DateTime hasta)
        {
            var list = new List<(int, int, decimal)>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT CAST(strftime('%Y', Fecha) AS INTEGER) AS Ano,
                       CAST(strftime('%m', Fecha) AS INTEGER) AS Mes,
                       COALESCE(SUM(TotalF), 0) AS Total
                FROM Facturas
                WHERE Fecha >= $d AND Fecha <= $h
                GROUP BY Ano, Mes
                ORDER BY Ano, Mes";
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetInt32(0), r.GetInt32(1), (decimal)r.GetDouble(2)));
            return list;
        }

        /// <summary>
        /// Análisis de diferencia cambiaria: cuántos USD se "gastaron" usando
        /// tasa paralelo vs si se hubiera usado tasa BCV en el mismo egreso.
        /// Devuelve totales por tipo de tasa para el período.
        /// </summary>
        public static (decimal TotalBcvUSD, decimal TotalParaleloUSD,
                       decimal TotalUsdDirectoUSD, decimal TotalPersonalizadaUSD,
                       decimal DiferenciaCambiaria)
            AnalisisDiferenciaCambiaria(DateTime desde, DateTime hasta)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    COALESCE(SUM(CASE WHEN TipoTasa = 'BCV'           THEN MontoUSD ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN TipoTasa = 'PARALELO'      THEN MontoUSD ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN TipoTasa = 'USD_DIRECTO'   THEN MontoUSD ELSE 0 END), 0),
                    COALESCE(SUM(CASE WHEN TipoTasa = 'PERSONALIZADA' THEN MontoUSD ELSE 0 END), 0)
                FROM Gastos
                WHERE Fecha >= $d AND Fecha <= $h AND Estado != 'Anulado'";
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            decimal bcv = 0, par = 0, usd = 0, pers = 0;
            if (r.Read())
            {
                bcv  = (decimal)r.GetDouble(0);
                par  = (decimal)r.GetDouble(1);
                usd  = (decimal)r.GetDouble(2);
                pers = (decimal)r.GetDouble(3);
            }
            // Diferencia cambiaria: cuánto MÁS pagaste en USD por usar
            // paralelo en lugar de BCV (asumiendo que el negocio cobra en BCV).
            // Es indicativo: si compraste $100 a paralelo cuando podías a BCV
            // (que es más bajo), pagaste más Bs por el mismo bien.
            decimal diferencia = par > 0 && bcv > 0 ? (par - bcv) : 0;
            return (bcv, par, usd, pers, diferencia);
        }

        private static Egreso BindEgreso(SqliteDataReader r) => new Egreso
        {
            Id              = r.GetInt32(0),
            Fecha           = DateTime.Parse(r.GetString(1)),
            IdCategoria     = r.GetInt32(2),
            NombreCategoria = r.GetString(3),
            Concepto        = r.GetString(4),
            Proveedor       = r.GetString(5),
            IdInsumo        = r.IsDBNull(6) ? (int?)null : r.GetInt32(6),
            Cantidad        = (decimal)r.GetDouble(7),
            MonedaOriginal  = r.GetString(8),
            TipoTasa        = r.GetString(9),
            TasaUsada       = (decimal)r.GetDouble(10),
            IdTasaCambio    = r.IsDBNull(11) ? (int?)null : r.GetInt32(11),
            MontoOriginal   = (decimal)r.GetDouble(12),
            MontoUSD        = (decimal)r.GetDouble(13),
            MontoBs         = (decimal)r.GetDouble(14),
            Estado          = r.GetString(15),
            FechaVencimiento = r.IsDBNull(16) ? (DateTime?)null : DateTime.Parse(r.GetString(16)),
            FechaPago        = r.IsDBNull(17) ? (DateTime?)null : DateTime.Parse(r.GetString(17)),
            RutaComprobante = r.GetString(18),
            Usuario         = r.GetString(19),
            FechaRegistro   = DateTime.TryParse(r.GetString(20), out var fr) ? fr : DateTime.MinValue,
            Observaciones   = r.GetString(21),
            PagadoUSD       = r.FieldCount > 22 && !r.IsDBNull(22) ? (decimal)r.GetDouble(22) : 0m,
            CodigoProveedor = r.FieldCount > 23 && !r.IsDBNull(23) ? r.GetString(23) : ""
        };
    }
}
