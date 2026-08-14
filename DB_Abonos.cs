#nullable enable
// DB_Abonos.cs — Migración v28 + CRUD para Abonos extendidos,
// ComprobantesFactura y CuentasReceptoras
// Partial class de DatabaseService (mismo patrón que DB_CuentasPagar.cs)

using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaGestion
{
    // ─── Modelos ─────────────────────────────────────────────────────────────

    public class CuentaReceptora
    {
        public int    Id          { get; set; }
        public string Nombre      { get; set; } = "";  // "Cuenta Zelle", "BDV Personal", etc.
        public string Banco       { get; set; } = "";   // "Zelle", "BDV", "Provincial", "Banesco"
        public string Tipo        { get; set; } = "";   // "Personal" o "Empresa"
        public string Titular     { get; set; } = "";   // "Jacques Pascher" o "El Hato Pascher C.A."
        public string Documento   { get; set; } = "";   // "10784445" o "J-30571871-7"
        public bool   Activa      { get; set; } = true;
    }

    public class ComprobanteFactura
    {
        public int      Id            { get; set; }
        public string   NumeroF       { get; set; } = "";
        public string   MetodoPago    { get; set; } = "";  // Zelle, TransfNac, PagoMovil
        public decimal  Monto         { get; set; }
        public string   Referencia    { get; set; } = "";
        public string   RutaImagen    { get; set; } = "";
        public int      IdCuentaReceptora { get; set; }
        public DateTime Fecha         { get; set; }
        public string   Observaciones { get; set; } = "";
    }

    // ─── Partial class ──────────────────────────────────────────────────────

    public static partial class DatabaseService
    {
        /// <summary>Migración v28: Abonos extendidos + ComprobantesFactura + CuentasReceptoras + PinBot</summary>
        public static void MigrarV28(SqliteConnection conn, SqliteTransaction tx)
        {
            // ── 1. Columnas nuevas en Abonos ─────────────────────────────────
            AlterTableSafe(conn, tx, "ALTER TABLE Abonos ADD COLUMN MetodoPago TEXT DEFAULT 'Efectivo'");
            AlterTableSafe(conn, tx, "ALTER TABLE Abonos ADD COLUMN Referencia TEXT DEFAULT ''");
            AlterTableSafe(conn, tx, "ALTER TABLE Abonos ADD COLUMN NombreRemitente TEXT DEFAULT ''");
            AlterTableSafe(conn, tx, "ALTER TABLE Abonos ADD COLUMN RutaComprobante TEXT DEFAULT ''");
            AlterTableSafe(conn, tx, "ALTER TABLE Abonos ADD COLUMN Observaciones TEXT DEFAULT ''");
            AlterTableSafe(conn, tx, "ALTER TABLE Abonos ADD COLUMN IdCuentaReceptora INTEGER DEFAULT 0");
            AlterTableSafe(conn, tx, "ALTER TABLE Abonos ADD COLUMN Estado TEXT DEFAULT 'Confirmado'");

            // ── 2. ComprobantesFactura ────────────────────────────────────────
            EjecutarSql(conn, tx, @"
                CREATE TABLE IF NOT EXISTS ComprobantesFactura (
                    Id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                    NumeroF            TEXT NOT NULL,
                    MetodoPago         TEXT NOT NULL,
                    Monto              REAL DEFAULT 0,
                    Referencia         TEXT DEFAULT '',
                    RutaImagen         TEXT DEFAULT '',
                    IdCuentaReceptora  INTEGER DEFAULT 0,
                    Fecha              TEXT NOT NULL,
                    Observaciones      TEXT DEFAULT '',
                    FOREIGN KEY(NumeroF) REFERENCES Facturas(NumeroF)
                );
                CREATE INDEX IF NOT EXISTS idx_compfact_nf ON ComprobantesFactura(NumeroF);
            ");

            // ── 3. CuentasReceptoras ─────────────────────────────────────────
            EjecutarSql(conn, tx, @"
                CREATE TABLE IF NOT EXISTS CuentasReceptoras (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre    TEXT NOT NULL,
                    Banco     TEXT NOT NULL,
                    Tipo      TEXT NOT NULL,
                    Titular   TEXT NOT NULL,
                    Documento TEXT NOT NULL,
                    Activa    INTEGER DEFAULT 1
                );
            ");

            // Semilla: las 5 cuentas de la empresa
            // Solo insertar si la tabla está vacía (idempotente)
            var cmdCount = conn.CreateCommand();
            cmdCount.Transaction = tx;
            cmdCount.CommandText = "SELECT COUNT(*) FROM CuentasReceptoras";
            long count = (long)cmdCount.ExecuteScalar()!;
            if (count == 0)
            {
                EjecutarSql(conn, tx, @"
                    INSERT INTO CuentasReceptoras (Nombre, Banco, Tipo, Titular, Documento) VALUES
                    ('Cuenta Zelle',        'Zelle',      'Personal', 'Jacques Pascher',       '10784445'),
                    ('BDV Personal',        'BDV',        'Personal', 'Jacques Pascher',       '10784445'),
                    ('BDV Empresas',        'BDV',        'Empresa',  'El Hato Pascher C.A.',  'J-30571871-7'),
                    ('Provincial Personal', 'Provincial', 'Personal', 'Jacques Pascher',       '10784445'),
                    ('Banesco Empresa',     'Banesco',    'Empresa',  'El Hato Pascher C.A.',  'J-30571871-7');
                ");
            }

            // ── 4. PinBot en Clientes ────────────────────────────────────────
            AlterTableSafe(conn, tx, "ALTER TABLE Clientes ADD COLUMN PinBot TEXT DEFAULT ''");

            // ── 5. Índice para búsqueda de abonos por cliente ────────────────
            EjecutarSql(conn, tx, @"
                CREATE INDEX IF NOT EXISTS idx_abonos_cliente ON Abonos(Cliente);
                CREATE INDEX IF NOT EXISTS idx_abonos_fecha   ON Abonos(Fecha);
            ");

            SetSchemaVersion(conn, tx, 28);
        }

        // ════════════════════════════════════════════════════════════════════
        //  CRUD — CuentasReceptoras
        // ════════════════════════════════════════════════════════════════════

        public static List<CuentaReceptora> ObtenerCuentasReceptoras(bool soloActivas = true)
        {
            var list = new List<CuentaReceptora>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = soloActivas
                ? "SELECT Id,Nombre,Banco,Tipo,Titular,Documento,Activa FROM CuentasReceptoras WHERE Activa=1 ORDER BY Nombre"
                : "SELECT Id,Nombre,Banco,Tipo,Titular,Documento,Activa FROM CuentasReceptoras ORDER BY Nombre";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new CuentaReceptora
                {
                    Id        = r.GetInt32(0),
                    Nombre    = S(r, 1),
                    Banco     = S(r, 2),
                    Tipo      = S(r, 3),
                    Titular   = S(r, 4),
                    Documento = S(r, 5),
                    Activa    = r.GetInt32(6) == 1
                });
            return list;
        }

        public static CuentaReceptora? IdentificarCuentaPorDatos(string titular, string documento)
        {
            // Busca la cuenta receptora que coincida con titular o documento
            // extraídos del comprobante (OCR)
            var cuentas = ObtenerCuentasReceptoras();
            if (string.IsNullOrWhiteSpace(titular) && string.IsNullOrWhiteSpace(documento))
                return null;

            // Primero intentar por documento (más confiable)
            if (!string.IsNullOrWhiteSpace(documento))
            {
                var limpio = documento.Replace("-", "").Replace(" ", "").ToUpper();
                var match = cuentas.FirstOrDefault(ct =>
                    ct.Documento.Replace("-", "").Replace(" ", "").ToUpper().Contains(limpio) ||
                    limpio.Contains(ct.Documento.Replace("-", "").Replace(" ", "").ToUpper()));
                if (match != null) return match;
            }

            // Luego por nombre del titular
            if (!string.IsNullOrWhiteSpace(titular))
            {
                var tit = titular.ToUpper().Trim();
                var match = cuentas.FirstOrDefault(ct =>
                    tit.Contains(ct.Titular.ToUpper()) || ct.Titular.ToUpper().Contains(tit));
                if (match != null) return match;
            }

            return null;
        }

        // ════════════════════════════════════════════════════════════════════
        //  CRUD — Abonos (extendido)
        // ════════════════════════════════════════════════════════════════════

        public static void InsertarAbonoExtendido(Abono a)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Abonos
                    (NumeroF, Fecha, Monto, Cliente, MetodoPago, Referencia,
                     NombreRemitente, RutaComprobante, Observaciones,
                     IdCuentaReceptora, Estado)
                VALUES
                    ($nf, $fe, $mo, $cl, $mp, $ref,
                     $nr, $rc, $obs, $icr, $est)";
            cmd.Parameters.AddWithValue("$nf",  a.NumeroF ?? "");
            cmd.Parameters.AddWithValue("$fe",  a.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$mo",  (double)a.Monto);
            cmd.Parameters.AddWithValue("$cl",  a.Cliente ?? "");
            cmd.Parameters.AddWithValue("$mp",  a.MetodoPago ?? "Efectivo");
            cmd.Parameters.AddWithValue("$ref", a.Referencia ?? "");
            cmd.Parameters.AddWithValue("$nr",  a.NombreRemitente ?? "");
            cmd.Parameters.AddWithValue("$rc",  a.RutaComprobante ?? "");
            cmd.Parameters.AddWithValue("$obs", a.Observaciones ?? "");
            cmd.Parameters.AddWithValue("$icr", a.IdCuentaReceptora);
            cmd.Parameters.AddWithValue("$est", a.Estado ?? "Confirmado");
            cmd.ExecuteNonQuery();
        }

        public static List<Abono> ObtenerAbonosExtendido(string? cliente = null,
            DateTime? desde = null, DateTime? hasta = null)
        {
            var list = new List<Abono>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            var where = new List<string>();
            if (!string.IsNullOrEmpty(cliente))
            {
                where.Add("Cliente = $cl");
                cmd.Parameters.AddWithValue("$cl", cliente);
            }
            if (desde.HasValue)
            {
                where.Add("Fecha >= $desde");
                cmd.Parameters.AddWithValue("$desde", desde.Value.ToString("yyyy-MM-dd"));
            }
            if (hasta.HasValue)
            {
                where.Add("Fecha <= $hasta");
                cmd.Parameters.AddWithValue("$hasta", hasta.Value.ToString("yyyy-MM-dd"));
            }
            string filtro = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
            cmd.CommandText = $@"
                SELECT NumAbono, NumeroF, Fecha, Monto, Cliente,
                       COALESCE(MetodoPago,'Efectivo'), COALESCE(Referencia,''),
                       COALESCE(NombreRemitente,''), COALESCE(RutaComprobante,''),
                       COALESCE(Observaciones,''), COALESCE(IdCuentaReceptora,0),
                       COALESCE(Estado,'Confirmado')
                FROM Abonos {filtro}
                ORDER BY Fecha DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Abono
                {
                    NumAbono          = r.GetInt32(0),
                    NumeroF           = S(r, 1),
                    Fecha             = DateTime.Parse(r.GetString(2)),
                    Monto             = D(r, 3),
                    Cliente           = S(r, 4),
                    MetodoPago        = S(r, 5),
                    Referencia        = S(r, 6),
                    NombreRemitente   = S(r, 7),
                    RutaComprobante   = S(r, 8),
                    Observaciones     = S(r, 9),
                    IdCuentaReceptora = r.GetInt32(10),
                    Estado            = S(r, 11)
                });
            return list;
        }

        /// <summary>Cambia el estado de un abono (Pendiente → Confirmado / Rechazado).</summary>
        public static void ActualizarEstadoAbono(int numAbono, string nuevoEstado)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Abonos SET Estado = $est WHERE NumAbono = $id";
            cmd.Parameters.AddWithValue("$est", nuevoEstado);
            cmd.Parameters.AddWithValue("$id", numAbono);
            cmd.ExecuteNonQuery();
        }

        // ════════════════════════════════════════════════════════════════════
        //  CRUD — ComprobantesFactura
        // ════════════════════════════════════════════════════════════════════

        public static void InsertarComprobanteFactura(ComprobanteFactura cf)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ComprobantesFactura
                    (NumeroF, MetodoPago, Monto, Referencia, RutaImagen,
                     IdCuentaReceptora, Fecha, Observaciones)
                VALUES
                    ($nf, $mp, $mo, $ref, $ri, $icr, $fe, $obs)";
            cmd.Parameters.AddWithValue("$nf",  cf.NumeroF);
            cmd.Parameters.AddWithValue("$mp",  cf.MetodoPago);
            cmd.Parameters.AddWithValue("$mo",  (double)cf.Monto);
            cmd.Parameters.AddWithValue("$ref", cf.Referencia ?? "");
            cmd.Parameters.AddWithValue("$ri",  cf.RutaImagen ?? "");
            cmd.Parameters.AddWithValue("$icr", cf.IdCuentaReceptora);
            cmd.Parameters.AddWithValue("$fe",  cf.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$obs", cf.Observaciones ?? "");
            cmd.ExecuteNonQuery();
        }

        public static List<ComprobanteFactura> ObtenerComprobantesFactura(string numeroF)
        {
            var list = new List<ComprobanteFactura>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, NumeroF, MetodoPago, Monto, Referencia, RutaImagen,
                       IdCuentaReceptora, Fecha, COALESCE(Observaciones,'')
                FROM ComprobantesFactura
                WHERE NumeroF = $nf
                ORDER BY Fecha";
            cmd.Parameters.AddWithValue("$nf", numeroF);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ComprobanteFactura
                {
                    Id                = r.GetInt32(0),
                    NumeroF           = S(r, 1),
                    MetodoPago        = S(r, 2),
                    Monto             = D(r, 3),
                    Referencia        = S(r, 4),
                    RutaImagen        = S(r, 5),
                    IdCuentaReceptora = r.GetInt32(6),
                    Fecha             = DateTime.Parse(r.GetString(7)),
                    Observaciones     = S(r, 8)
                });
            return list;
        }

        public static void EliminarComprobanteFactura(int id)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM ComprobantesFactura WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        // ════════════════════════════════════════════════════════════════════
        //  Estado de cuenta (reporte)
        // ════════════════════════════════════════════════════════════════════

        public static (decimal saldoFavor, List<Abono> abonos, decimal totalAbonos)
            ObtenerEstadoCuenta(string codigoCliente)
        {
            var abonos = ObtenerAbonosExtendido(cliente: codigoCliente);
            decimal totalAbonos = abonos.Where(a => a.Estado == "Confirmado").Sum(a => a.Monto);

            // Saldo a favor = abonos confirmados - facturas pagadas con esos abonos
            // (simplificado: AFavor del cliente)
            decimal saldoFavor = 0;
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(AFavor, 0) FROM Clientes WHERE Codigo = $cod";
            cmd.Parameters.AddWithValue("$cod", codigoCliente);
            var result = cmd.ExecuteScalar();
            if (result != null && result != DBNull.Value)
                saldoFavor = Convert.ToDecimal(result);

            return (saldoFavor, abonos, totalAbonos);
        }

        // ════════════════════════════════════════════════════════════════════
        //  PinBot — Clientes
        // ════════════════════════════════════════════════════════════════════

        public static string? ObtenerPinBot(string codigoCliente)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(PinBot,'') FROM Clientes WHERE Codigo = $cod";
            cmd.Parameters.AddWithValue("$cod", codigoCliente);
            var r = cmd.ExecuteScalar();
            var pin = r?.ToString() ?? "";
            return string.IsNullOrEmpty(pin) ? null : pin;
        }

        public static void GuardarPinBot(string codigoCliente, string pin)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE Clientes SET PinBot = $pin WHERE Codigo = $cod";
            cmd.Parameters.AddWithValue("$pin", pin ?? "");
            cmd.Parameters.AddWithValue("$cod", codigoCliente);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Valida un PIN de bot y devuelve el código del cliente, o null si no coincide.</summary>
        public static string? ValidarPinBot(string pin, string? telefonoONombre = null)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            // Buscar por PIN exacto (puede haber más de un cliente con el mismo PIN,
            // en ese caso usar teléfono como desambiguación)
            cmd.CommandText = @"
                SELECT Codigo, Descripcion, Telefono, Telefono2
                FROM Clientes
                WHERE PinBot = $pin AND PinBot != ''";
            cmd.Parameters.AddWithValue("$pin", pin);
            using var r = cmd.ExecuteReader();
            var candidatos = new List<(string cod, string nombre, string tel1, string tel2)>();
            while (r.Read())
                candidatos.Add((S(r, 0), S(r, 1), S(r, 2), S(r, 3)));

            if (candidatos.Count == 0) return null;
            if (candidatos.Count == 1) return candidatos[0].cod;

            // Desambiguar por teléfono si hay múltiples
            if (!string.IsNullOrEmpty(telefonoONombre))
            {
                var limpio = telefonoONombre.Replace("+", "").Replace(" ", "").Replace("-", "");
                var match = candidatos.FirstOrDefault(c =>
                    c.tel1.Replace("+", "").Replace(" ", "").Replace("-", "").Contains(limpio) ||
                    c.tel2.Replace("+", "").Replace(" ", "").Replace("-", "").Contains(limpio) ||
                    c.nombre.ToUpper().Contains(telefonoONombre.ToUpper()));
                if (match.cod != null) return match.cod;
            }

            // Si no se puede desambiguar, devolver el primero
            return candidatos[0].cod;
        }
    }
}
