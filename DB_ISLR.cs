#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  MODELOS — ISLR Estimado (Tarifa 3, Persona Juridica)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parametros de configuracion para el calculo de ISLR de un ejercicio fiscal.
    /// </summary>
    public class ConfigISLR
    {
        public int     Id              { get; set; }
        public int     Anio            { get; set; }
        public decimal ValorUT         { get; set; }   // Valor de la Unidad Tributaria en Bs
        public decimal PctRebaja       { get; set; } = 10m; // % rebaja Art. 57 (actividad productiva)
        public decimal ISLRAnioAnterior { get; set; }  // ISLR pagado el año anterior (para anticipos)
        public decimal RetencionesAnio  { get; set; }  // Total retenciones recibidas en el año
        public string  Observaciones   { get; set; } = "";
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Retencion de ISLR recibida de un cliente agente de retencion.
    /// </summary>
    public class RetencionISLR
    {
        public int      Id          { get; set; }
        public int      Anio        { get; set; }
        public DateTime Fecha       { get; set; }
        public string   Cliente     { get; set; } = "";
        public string   NumFactura  { get; set; } = "";
        public decimal  MontoBase   { get; set; }   // monto de la factura base
        public decimal  PctRetencion{ get; set; }   // % retenido (tipicamente 3%)
        public decimal  MontoRetenido { get; set; } // MontoBase * PctRetencion / 100
        public string   Comprobante { get; set; } = "";
        public string   Observaciones{ get; set; } = "";
    }

    /// <summary>
    /// Resultado del calculo de ISLR Estimado para un ejercicio.
    /// Todos los montos en USD salvo que se indique "Bs" o "UT".
    /// </summary>
    public class ResultadoISLR
    {
        // ── Ingresos y costos (calculados desde la BD) ──────────────────
        public decimal IngresosBrutos      { get; set; }  // Total ventas (Facturas)
        public decimal CostosCompras       { get; set; }  // Total compras de insumos/mercancias
        public decimal CostosGastos        { get; set; }  // Gastos operativos + egresos
        public decimal CostosNomina        { get; set; }  // Total nomina del año
        public decimal TotalCostos         => CostosCompras + CostosGastos + CostosNomina;
        public decimal EnriquecimientoNeto => IngresosBrutos - TotalCostos;

        // ── Conversion a UT ─────────────────────────────────────────────
        public decimal ValorUT             { get; set; }
        public decimal EnriquecimientoBS   => EnriquecimientoNeto * TasaBCV;
        public decimal TasaBCV             { get; set; }  // tasa del ultimo dia del ejercicio
        public decimal EnriquecimientoUT   => ValorUT > 0 ? EnriquecimientoBS / ValorUT : 0;

        // ── Calculo de impuesto (Tarifa 3) ──────────────────────────────
        // Art. 52 LISR: 15% hasta 2000 UT | 22% de 2001 a 3000 UT | 34% mas de 3000 UT
        public decimal ImpuestoUT          { get; set; }  // resultado del calculo por tramos en UT
        public decimal ImpuestoBS          => ImpuestoUT * ValorUT;
        public decimal ImpuestoUSD         => TasaBCV > 0 ? ImpuestoBS / TasaBCV : 0;

        // ── Ajustes ─────────────────────────────────────────────────────
        public decimal PctRebaja           { get; set; }  // % rebaja Art. 57
        public decimal RebajaBS            => ImpuestoBS * PctRebaja / 100m;
        public decimal RetencionesBS       { get; set; }  // total retenciones recibidas en Bs
        public decimal ISLRNetoBS          => Math.Max(0, ImpuestoBS - RebajaBS - RetencionesBS);
        public decimal ISLRNetoUSD         => TasaBCV > 0 ? ISLRNetoBS / TasaBCV : 0;

        // ── Anticipos ───────────────────────────────────────────────────
        // Anticipo mensual = (ISLR año anterior - retenciones año anterior) / 12
        public decimal ISLRAnioAnteriorBS  { get; set; }
        public decimal RetencionesAntBS    { get; set; }
        public decimal AnticipoMensualBS   =>
            Math.Max(0, (ISLRAnioAnteriorBS - RetencionesAntBS)) / 12m;

        // ── Periodo ─────────────────────────────────────────────────────
        public int     Anio                { get; set; }
        public DateTime FechaCalculo       { get; set; } = DateTime.Now;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DATABASE SERVICE — ISLR (Migracion v24)
    // ════════════════════════════════════════════════════════════════════════
    public static partial class DatabaseService
    {
        // En DB.cs: cambiar SCHEMA_VERSION = 24
        // En MigrarEsquema(): agregar
        //   if (versionActual < 24) MigrarV24(conn, tx);
        public static void MigrarV24(SqliteConnection conn, SqliteTransaction tx)
        {
            _ultimoSqlMigracion = "[v24] inicio MigrarV24 — ISLR";
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ISLRConfig (
                    Id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    Anio                INTEGER NOT NULL UNIQUE,
                    ValorUT             REAL    NOT NULL DEFAULT 0,
                    PctRebaja           REAL    NOT NULL DEFAULT 10,
                    ISLRAnioAnterior    REAL    NOT NULL DEFAULT 0,
                    RetencionesAnio     REAL    NOT NULL DEFAULT 0,
                    Observaciones       TEXT    DEFAULT '',
                    FechaActualizacion  TEXT    NOT NULL
                );

                CREATE TABLE IF NOT EXISTS RetencionesISLR (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    Anio         INTEGER NOT NULL,
                    Fecha        TEXT    NOT NULL,
                    Cliente      TEXT    NOT NULL DEFAULT '',
                    NumFactura   TEXT    DEFAULT '',
                    MontoBase    REAL    NOT NULL DEFAULT 0,
                    PctRetencion REAL    NOT NULL DEFAULT 3,
                    MontoRetenido REAL   NOT NULL DEFAULT 0,
                    Comprobante  TEXT    DEFAULT '',
                    Observaciones TEXT   DEFAULT ''
                );
                CREATE INDEX IF NOT EXISTS idx_ret_islr_anio ON RetencionesISLR(Anio);
            ";
            cmd.ExecuteNonQuery();
        }

        // ── ConfigISLR ───────────────────────────────────────────────────────

        public static ConfigISLR ObtenerConfigISLR(int anio)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM ISLRConfig WHERE Anio=$a";
            cmd.Parameters.AddWithValue("$a", anio);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return new ConfigISLR { Anio = anio, ValorUT = 0 };
            return new ConfigISLR
            {
                Id               = r.GetInt32(0),
                Anio             = r.GetInt32(1),
                ValorUT          = D(r, 2),
                PctRebaja        = D(r, 3),
                ISLRAnioAnterior = D(r, 4),
                RetencionesAnio  = D(r, 5),
                Observaciones    = S(r, 6),
                FechaActualizacion = DateTime.TryParse(S(r, 7), out var fa) ? fa : DateTime.Now
            };
        }

        public static void GuardarConfigISLR(ConfigISLR cfg)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ISLRConfig
                    (Anio, ValorUT, PctRebaja, ISLRAnioAnterior, RetencionesAnio, Observaciones, FechaActualizacion)
                VALUES ($an, $ut, $rb, $ia, $ra, $ob, $fa)
                ON CONFLICT(Anio) DO UPDATE SET
                    ValorUT=$ut, PctRebaja=$rb, ISLRAnioAnterior=$ia,
                    RetencionesAnio=$ra, Observaciones=$ob, FechaActualizacion=$fa";
            cmd.Parameters.AddWithValue("$an", cfg.Anio);
            cmd.Parameters.AddWithValue("$ut", (double)cfg.ValorUT);
            cmd.Parameters.AddWithValue("$rb", (double)cfg.PctRebaja);
            cmd.Parameters.AddWithValue("$ia", (double)cfg.ISLRAnioAnterior);
            cmd.Parameters.AddWithValue("$ra", (double)cfg.RetencionesAnio);
            cmd.Parameters.AddWithValue("$ob", cfg.Observaciones ?? "");
            cmd.Parameters.AddWithValue("$fa", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        // ── RetencionesISLR ──────────────────────────────────────────────────

        public static List<RetencionISLR> ObtenerRetencionesISLR(int anio)
        {
            var list = new List<RetencionISLR>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT * FROM RetencionesISLR WHERE Anio=$a ORDER BY Fecha DESC";
            cmd.Parameters.AddWithValue("$a", anio);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new RetencionISLR
                {
                    Id           = r.GetInt32(0),
                    Anio         = r.GetInt32(1),
                    Fecha        = DateTime.TryParse(S(r,2), out var f) ? f : DateTime.Today,
                    Cliente      = S(r, 3),
                    NumFactura   = S(r, 4),
                    MontoBase    = D(r, 5),
                    PctRetencion = D(r, 6),
                    MontoRetenido= D(r, 7),
                    Comprobante  = S(r, 8),
                    Observaciones= S(r, 9)
                });
            return list;
        }

        public static void GuardarRetencionISLR(RetencionISLR ret)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            if (ret.Id == 0)
                cmd.CommandText = @"
                    INSERT INTO RetencionesISLR
                        (Anio,Fecha,Cliente,NumFactura,MontoBase,PctRetencion,MontoRetenido,Comprobante,Observaciones)
                    VALUES ($an,$fe,$cl,$nf,$mb,$pc,$mr,$co,$ob)";
            else
            {
                cmd.CommandText = @"
                    UPDATE RetencionesISLR SET
                        Anio=$an, Fecha=$fe, Cliente=$cl, NumFactura=$nf,
                        MontoBase=$mb, PctRetencion=$pc, MontoRetenido=$mr,
                        Comprobante=$co, Observaciones=$ob
                    WHERE Id=$id";
                cmd.Parameters.AddWithValue("$id", ret.Id);
            }
            cmd.Parameters.AddWithValue("$an", ret.Anio);
            cmd.Parameters.AddWithValue("$fe", ret.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cl", ret.Cliente ?? "");
            cmd.Parameters.AddWithValue("$nf", ret.NumFactura ?? "");
            cmd.Parameters.AddWithValue("$mb", (double)ret.MontoBase);
            cmd.Parameters.AddWithValue("$pc", (double)ret.PctRetencion);
            cmd.Parameters.AddWithValue("$mr", (double)ret.MontoRetenido);
            cmd.Parameters.AddWithValue("$co", ret.Comprobante ?? "");
            cmd.Parameters.AddWithValue("$ob", ret.Observaciones ?? "");
            cmd.ExecuteNonQuery();
        }

        public static void EliminarRetencionISLR(int id)
        {
            using var c = Conn();
            Exec(c, "DELETE FROM RetencionesISLR WHERE Id=$id", ("$id", id));
        }

        // ── Calculo ISLR Estimado ────────────────────────────────────────────

        /// <summary>
        /// Calcula el ISLR estimado para el ejercicio fiscal indicado.
        /// Extrae ingresos, costos y nomina de la BD y aplica Tarifa 3 (persona juridica).
        /// </summary>
        public static ResultadoISLR CalcularISLR(int anio, ConfigISLR cfg)
        {
            using var c = Conn();
            var desde = $"{anio}-01-01";
            var hasta = $"{anio}-12-31";

            // Ingresos brutos: facturas activas del ejercicio
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(SUM(TotalF),0) FROM Facturas WHERE Fecha>=$d AND Fecha<=$h AND Estado!='Anulada'";
            cmd.Parameters.AddWithValue("$d", desde);
            cmd.Parameters.AddWithValue("$h", hasta);
            decimal ingresos = D2(cmd.ExecuteScalar());

            // Costos: compras del ejercicio
            cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(SUM(Total),0) FROM Compras WHERE Fecha>=$d AND Fecha<=$h";
            cmd.Parameters.AddWithValue("$d", desde);
            cmd.Parameters.AddWithValue("$h", hasta);
            decimal compras = D2(cmd.ExecuteScalar());

            // Costos: egresos + gastos del ejercicio
            cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(SUM(Monto),0) FROM Egresos WHERE Fecha>=$d AND Fecha<=$h AND Estado!='Anulado'";
            cmd.Parameters.AddWithValue("$d", desde);
            cmd.Parameters.AddWithValue("$h", hasta);
            decimal egresos = D2(cmd.ExecuteScalar());

            cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(SUM(Monto),0) FROM Gastos WHERE Fecha>=$d AND Fecha<=$h";
            cmd.Parameters.AddWithValue("$d", desde);
            cmd.Parameters.AddWithValue("$h", hasta);
            decimal gastos = D2(cmd.ExecuteScalar());

            // Costos: nomina del ejercicio (neto total pagado)
            cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(d.TotalDevengado),0)
                FROM DetallesNomina d
                JOIN PeriodosNomina p ON d.IdPeriodo=p.Id
                WHERE p.FechaInicio>=$d AND p.FechaInicio<=$h";
            cmd.Parameters.AddWithValue("$d", desde);
            cmd.Parameters.AddWithValue("$h", hasta);
            decimal nomina = D2(cmd.ExecuteScalar());

            // Tasa BCV del ultimo dia del ejercicio (o la mas reciente disponible)
            cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT TasaBCV FROM TasaCambio
                WHERE Fecha<=$h ORDER BY Fecha DESC LIMIT 1";
            cmd.Parameters.AddWithValue("$h", hasta);
            decimal tasaBCV = D2(cmd.ExecuteScalar());
            if (tasaBCV <= 0) tasaBCV = 1;

            // Retenciones recibidas en el año (en USD, convertir a Bs)
            decimal retencionesBs = 0;
            var rets = ObtenerRetencionesISLR(anio);
            foreach (var r in rets) retencionesBs += r.MontoRetenido;
            // Las retenciones ya estan en Bs (se registran en Bs como lo emite el comprobante)

            // ── Calculo Tarifa 3 ──────────────────────────────────────────
            var res = new ResultadoISLR
            {
                Anio             = anio,
                IngresosBrutos   = ingresos,
                CostosCompras    = compras,
                CostosGastos     = egresos + gastos,
                CostosNomina     = nomina,
                ValorUT          = cfg.ValorUT,
                TasaBCV          = tasaBCV,
                PctRebaja        = cfg.PctRebaja,
                RetencionesBS    = retencionesBs,
                ISLRAnioAnteriorBS = cfg.ISLRAnioAnterior,
                RetencionesAntBS   = cfg.RetencionesAnio
            };

            // Enriquecimiento neto en UT
            decimal enUT = res.EnriquecimientoUT;

            // Tarifa 3 (Art. 52 LISR)
            decimal impUT = 0;
            if (enUT > 0)
            {
                if (enUT <= 2000m)
                    impUT = enUT * 0.15m;
                else if (enUT <= 3000m)
                    impUT = 2000m * 0.15m + (enUT - 2000m) * 0.22m;
                else
                    impUT = 2000m * 0.15m + 1000m * 0.22m + (enUT - 3000m) * 0.34m;
            }
            res.ImpuestoUT = impUT;

            return res;
        }

        private static decimal D2(object? val) =>
            val == null || val == DBNull.Value ? 0m : (decimal)(double)val;
    }
}
