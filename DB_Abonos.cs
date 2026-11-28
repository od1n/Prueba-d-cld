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
            cmd.Parameters.AddWithValue("$nf",  string.IsNullOrWhiteSpace(a.NumeroF) ? (object)DBNull.Value : a.NumeroF.Trim());
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

        private const decimal CXC_TOL = 0.005m;

        private static bool EsEstadoCxCCerrado(string? estado) =>
            estado != null && (
                estado.Equals("Saldada", StringComparison.OrdinalIgnoreCase) ||
                estado.Equals("Pagada",  StringComparison.OrdinalIgnoreCase) ||
                estado.Equals("Pagado",  StringComparison.OrdinalIgnoreCase) ||
                estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase));

        private static string? ExtraerNumeroFacturaCxC(string? concepto)
        {
            if (string.IsNullOrWhiteSpace(concepto)) return null;
            var t = concepto.Trim();
            if (t.StartsWith("Factura ", StringComparison.OrdinalIgnoreCase))
            {
                var n = t.Substring("Factura ".Length).Trim();
                return string.IsNullOrWhiteSpace(n) ? null : n;
            }
            return null;
        }

        private static string ExtraerCodigoClienteCxC(string? cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente)) return "";
            var s = cliente.Trim();
            int idx = s.IndexOf(" - ", StringComparison.Ordinal);
            return idx > 0 ? s.Substring(0, idx).Trim() : s;
        }

        private static bool ExisteFacturaEnTx(SqliteConnection c, SqliteTransaction tx, string? numF)
        {
            if (string.IsNullOrWhiteSpace(numF)) return false;
            var cmd = c.CreateCommand(); cmd.Transaction = tx;
            cmd.CommandText = "SELECT 1 FROM Facturas WHERE NumeroF = $n LIMIT 1";
            cmd.Parameters.AddWithValue("$n", numF.Trim());
            return cmd.ExecuteScalar() != null;
        }

        public static List<CuentaCobrar> ObtenerCuentasPendientesPorCliente(string codigoCliente)
        {
            var list = new List<CuentaCobrar>();
            if (string.IsNullOrWhiteSpace(codigoCliente)) return list;
            using var c = Conn();
            string? nom = null;
            using (var qn = c.CreateCommand())
            {
                qn.CommandText = "SELECT Descripcion FROM Clientes WHERE Codigo = $cod LIMIT 1";
                qn.Parameters.AddWithValue("$cod", codigoCliente.Trim());
                nom = qn.ExecuteScalar()?.ToString();
            }
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Cliente, Concepto, Monto, COALESCE(Pagado,0), FechaEmision, FechaVence, COALESCE(Estado,'Vigente')
                FROM CuentasCobrar
                WHERE (Cliente = $cod OR Cliente = $nom OR Cliente LIKE $pref)
                  AND COALESCE(Estado,'Vigente') NOT IN ('Saldada','Pagada','Pagado','Anulada')
                  AND (Monto - COALESCE(Pagado,0)) > 0.005
                ORDER BY FechaVence ASC, Id ASC";
            cmd.Parameters.AddWithValue("$cod",  codigoCliente.Trim());
            cmd.Parameters.AddWithValue("$nom",  nom ?? codigoCliente.Trim());
            cmd.Parameters.AddWithValue("$pref", codigoCliente.Trim() + " - %");
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new CuentaCobrar
                {
                    Id           = r.GetInt32(0),
                    Cliente      = S(r, 1),
                    Concepto     = S(r, 2),
                    Monto        = D(r, 3),
                    Pagado       = D(r, 4),
                    FechaEmision = DateTime.TryParse(S(r, 5), out var fe) ? fe : DateTime.Today,
                    FechaVence   = DateTime.TryParse(S(r, 6), out var fv) ? fv : DateTime.Today,
                    Estado       = S(r, 7)
                });
            return list;
        }

        /// <summary>
        /// Registra un pago parcial o total sobre una Cuenta por Cobrar y sincroniza
        /// atómicamente: CuentasCobrar + Facturas.Abono + Abonos.
        /// </summary>
        public static (decimal montoAplicado, decimal saldoRestante, bool saldada) RegistrarPagoCxC(
            int idCuenta, decimal monto, string metodoPago = "Efectivo",
            string referencia = "", string observaciones = "", DateTime? fecha = null)
        {
            if (monto <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
            var fechaPago = (fecha ?? DateTime.Today).ToString("yyyy-MM-dd");

            using var c = Conn();
            using var tx = c.BeginTransaction();
            try
            {
                var (aplicado, saldoRest, saldada, concepto, numF, codCli) =
                    AplicarPagoCxCEnTx(c, tx, idCuenta, monto);

                object nfVal = ExisteFacturaEnTx(c, tx, numF) ? numF!.Trim() : (object)DBNull.Value;
                var ins = c.CreateCommand(); ins.Transaction = tx;
                ins.CommandText = @"
                    INSERT INTO Abonos
                        (NumeroF, Fecha, Monto, Cliente, MetodoPago, Referencia,
                         NombreRemitente, RutaComprobante, Observaciones,
                         IdCuentaReceptora, Estado)
                    VALUES ($nf, $fe, $mo, $cl, $mp, $ref, '', '', $obs, 0, 'Confirmado')";
                ins.Parameters.AddWithValue("$nf",  nfVal);
                ins.Parameters.AddWithValue("$fe",  fechaPago);
                ins.Parameters.AddWithValue("$mo",  (double)aplicado);
                ins.Parameters.AddWithValue("$cl",  codCli);
                ins.Parameters.AddWithValue("$mp",  string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago);
                ins.Parameters.AddWithValue("$ref", referencia ?? "");
                ins.Parameters.AddWithValue("$obs", string.IsNullOrWhiteSpace(observaciones) ? $"Pago {concepto}" : observaciones);
                ins.ExecuteNonQuery();

                tx.Commit();
                return (aplicado, saldoRest, saldada);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        private static (decimal aplicado, decimal saldoRest, bool saldada, string concepto, string? numF, string codCli)
            AplicarPagoCxCEnTx(SqliteConnection c, SqliteTransaction tx, int idCuenta, decimal monto)
        {
            string cliCxC, concepto, estAnt, fvStr;
            decimal cMonto, cPagado;
            using (var q = c.CreateCommand())
            {
                q.Transaction = tx;
                q.CommandText = "SELECT Cliente, Concepto, Monto, COALESCE(Pagado,0), COALESCE(Estado,'Vigente'), COALESCE(FechaVence,'') FROM CuentasCobrar WHERE Id = $id";
                q.Parameters.AddWithValue("$id", idCuenta);
                using var r = q.ExecuteReader();
                if (!r.Read()) throw new InvalidOperationException("Cuenta por cobrar no encontrada.");
                cliCxC   = S(r, 0);
                concepto = S(r, 1);
                cMonto   = D(r, 2);
                cPagado  = D(r, 3);
                estAnt   = S(r, 4);
                fvStr    = S(r, 5);
            }
            if (EsEstadoCxCCerrado(estAnt) || (cMonto - cPagado) <= CXC_TOL)
                throw new InvalidOperationException("Esta cuenta ya está saldada o anulada.");

            decimal saldoAnt = Math.Max(0, cMonto - cPagado);
            decimal aplicado = Math.Min(saldoAnt, monto);
            decimal nuevoPag = cPagado + aplicado;
            bool saldada = (cMonto - nuevoPag) <= CXC_TOL;
            string hoy = DateTime.Today.ToString("yyyy-MM-dd");
            string nuevoEst = saldada ? "Saldada"
                : (!string.IsNullOrEmpty(fvStr) && string.CompareOrdinal(fvStr.Substring(0, Math.Min(10, fvStr.Length)), hoy) < 0
                    ? "Vencida" : "Vigente");
            if (saldada) nuevoPag = cMonto;

            using (var u = c.CreateCommand())
            {
                u.Transaction = tx;
                u.CommandText = "UPDATE CuentasCobrar SET Pagado = $p, Estado = $e WHERE Id = $id";
                u.Parameters.AddWithValue("$p", (double)nuevoPag);
                u.Parameters.AddWithValue("$e", nuevoEst);
                u.Parameters.AddWithValue("$id", idCuenta);
                u.ExecuteNonQuery();
            }

            string? numF = ExtraerNumeroFacturaCxC(concepto);
            string codCli = ExtraerCodigoClienteCxC(cliCxC);
            if (!string.IsNullOrWhiteSpace(numF))
            {
                using var qf = c.CreateCommand();
                qf.Transaction = tx;
                qf.CommandText = "SELECT TotalF, COALESCE(Abono,0), COALESCE(Codigo,''), COALESCE(Anulada,0) FROM Facturas WHERE NumeroF = $n";
                qf.Parameters.AddWithValue("$n", numF);
                using var rf = qf.ExecuteReader();
                if (rf.Read())
                {
                    decimal fTot = D(rf, 0), fAb = D(rf, 1);
                    string fCod = S(rf, 2);
                    int fAnul = rf.IsDBNull(3) ? 0 : rf.GetInt32(3);
                    rf.Close();
                    if (!string.IsNullOrWhiteSpace(fCod)) codCli = fCod;
                    if (fAnul == 0 && fAb >= 0)
                    {
                        decimal nuevoAb = saldada ? fTot : Math.Min(fTot, fAb + aplicado);
                        using var uf = c.CreateCommand();
                        uf.Transaction = tx;
                        uf.CommandText = "UPDATE Facturas SET Abono = $a WHERE NumeroF = $n";
                        uf.Parameters.AddWithValue("$a", (double)nuevoAb);
                        uf.Parameters.AddWithValue("$n", numF);
                        uf.ExecuteNonQuery();
                    }
                }
            }

            return (aplicado, Math.Max(0, cMonto - nuevoPag), saldada, concepto, numF, codCli);
        }

        /// <summary>
        /// Registra un abono desde FrmAbonos y opcionalmente lo aplica a una CxC específica
        /// o en orden FIFO sobre las CxC pendientes del cliente (actualizando Facturas.Abono).
        /// </summary>
        public static string RegistrarAbonoYActualizarCxC(Abono a, int? idCuentaCobrar = null, bool aplicarCxC = true)
        {
            using var c = Conn();
            using var tx = c.BeginTransaction();
            try
            {
                string? nfFinal = string.IsNullOrWhiteSpace(a.NumeroF) ? null : a.NumeroF.Trim();
                string obsFinal = a.Observaciones ?? "";
                string resumen = "Abono registrado.";

                if (aplicarCxC && (a.Estado ?? "Confirmado") == "Confirmado" && a.Monto > 0)
                {
                    if (idCuentaCobrar.HasValue && idCuentaCobrar.Value > 0)
                    {
                        var (aplicado, saldoRest, saldada, concepto, numF, _) =
                            AplicarPagoCxCEnTx(c, tx, idCuentaCobrar.Value, a.Monto);
                        nfFinal = numF;
                        resumen = saldada
                            ? $"Abono registrado. {concepto} saldada en su totalidad."
                            : $"Abono registrado en {concepto}. Saldo restante: {saldoRest:C2}.";
                    }
                    else if (!string.IsNullOrWhiteSpace(a.Cliente))
                    {
                        string? nom = null;
                        using (var qn = c.CreateCommand())
                        {
                            qn.Transaction = tx;
                            qn.CommandText = "SELECT Descripcion FROM Clientes WHERE Codigo = $cod LIMIT 1";
                            qn.Parameters.AddWithValue("$cod", a.Cliente.Trim());
                            nom = qn.ExecuteScalar()?.ToString();
                        }
                        var pendientes = new List<(int id, decimal saldo)>();
                        using (var qp = c.CreateCommand())
                        {
                            qp.Transaction = tx;
                            qp.CommandText = @"
                                SELECT Id, Monto - COALESCE(Pagado,0)
                                FROM CuentasCobrar
                                WHERE (Cliente = $cod OR Cliente = $nom OR Cliente LIKE $pref)
                                  AND COALESCE(Estado,'Vigente') NOT IN ('Saldada','Pagada','Pagado','Anulada')
                                  AND (Monto - COALESCE(Pagado,0)) > 0.005
                                ORDER BY FechaVence ASC, Id ASC";
                            qp.Parameters.AddWithValue("$cod",  a.Cliente.Trim());
                            qp.Parameters.AddWithValue("$nom",  nom ?? a.Cliente.Trim());
                            qp.Parameters.AddWithValue("$pref", a.Cliente.Trim() + " - %");
                            using var rp = qp.ExecuteReader();
                            while (rp.Read()) pendientes.Add((rp.GetInt32(0), D(rp, 1)));
                        }

                        decimal restante = a.Monto;
                        var aplicadas = new List<(string concepto, string? numF, decimal aplicado)>();
                        foreach (var p in pendientes)
                        {
                            if (restante <= CXC_TOL) break;
                            var (ap, _, _, conc, numF, _) = AplicarPagoCxCEnTx(c, tx, p.id, Math.Min(restante, p.saldo));
                            aplicadas.Add((conc, numF, ap));
                            restante -= ap;
                        }
                        if (aplicadas.Count == 1) nfFinal = aplicadas[0].numF;
                        if (aplicadas.Count > 0)
                        {
                            string det = "Aplicado a: " + string.Join(", ", aplicadas.Select(x => $"{x.concepto} ({x.aplicado:C2})"));
                            obsFinal = string.IsNullOrWhiteSpace(obsFinal) ? det : $"{obsFinal} | {det}";
                            resumen = $"Abono registrado y aplicado a {aplicadas.Count} cuenta(s) por cobrar.";
                        }
                    }
                }

                object nfVal = ExisteFacturaEnTx(c, tx, nfFinal) ? nfFinal!.Trim() : (object)DBNull.Value;
                using (var cmd = c.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO Abonos
                            (NumeroF, Fecha, Monto, Cliente, MetodoPago, Referencia,
                             NombreRemitente, RutaComprobante, Observaciones,
                             IdCuentaReceptora, Estado)
                        VALUES ($nf, $fe, $mo, $cl, $mp, $ref, $nr, $rc, $obs, $icr, $est)";
                    cmd.Parameters.AddWithValue("$nf",  nfVal);
                    cmd.Parameters.AddWithValue("$fe",  a.Fecha.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("$mo",  (double)a.Monto);
                    cmd.Parameters.AddWithValue("$cl",  a.Cliente ?? "");
                    cmd.Parameters.AddWithValue("$mp",  a.MetodoPago ?? "Efectivo");
                    cmd.Parameters.AddWithValue("$ref", a.Referencia ?? "");
                    cmd.Parameters.AddWithValue("$nr",  a.NombreRemitente ?? "");
                    cmd.Parameters.AddWithValue("$rc",  a.RutaComprobante ?? "");
                    cmd.Parameters.AddWithValue("$obs", obsFinal);
                    cmd.Parameters.AddWithValue("$icr", a.IdCuentaReceptora);
                    cmd.Parameters.AddWithValue("$est", a.Estado ?? "Confirmado");
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                return resumen;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Reconciliación idempotente entre Facturas y CuentasCobrar (misma lógica que la API).
        /// </summary>
        public static void SincronizarCxCYFacturas()
        {
            try
            {
                string hoy = DateTime.Today.ToString("yyyy-MM-dd");
                using var c = Conn();
                using var tx = c.BeginTransaction();

                // 1. CxC de facturas anuladas -> Anulada
                using (var uAnul = c.CreateCommand())
                {
                    uAnul.Transaction = tx;
                    uAnul.CommandText = @"
                        UPDATE CuentasCobrar
                        SET Estado = 'Anulada'
                        WHERE Id IN (
                            SELECT cc.Id
                            FROM CuentasCobrar cc
                            JOIN Facturas f ON LOWER(TRIM(cc.Concepto)) = LOWER('Factura ' || TRIM(f.NumeroF))
                            WHERE (COALESCE(f.Anulada,0) = 1 OR COALESCE(f.Abono,0) < 0)
                              AND COALESCE(cc.Estado,'') != 'Anulada'
                        )";
                    uAnul.ExecuteNonQuery();
                }

                // 2. Reconciliar saldos entre Facturas y CuentasCobrar activas
                var pares = new List<(int id, decimal cMonto, decimal cPag, string cVence, string numF, decimal fTot, decimal fAb)>();
                using (var q = c.CreateCommand())
                {
                    q.Transaction = tx;
                    q.CommandText = @"
                        SELECT cc.Id, cc.Monto, COALESCE(cc.Pagado,0), COALESCE(cc.FechaVence,''),
                               f.NumeroF, COALESCE(f.TotalF,0), COALESCE(f.Abono,0)
                        FROM CuentasCobrar cc
                        JOIN Facturas f ON LOWER(TRIM(cc.Concepto)) = LOWER('Factura ' || TRIM(f.NumeroF))
                        WHERE COALESCE(f.Anulada,0) = 0 AND COALESCE(f.Abono,0) >= 0
                          AND COALESCE(cc.Estado,'') != 'Anulada'";
                    using var r = q.ExecuteReader();
                    while (r.Read())
                        pares.Add((r.GetInt32(0), D(r, 1), D(r, 2), S(r, 3), S(r, 4), D(r, 5), D(r, 6)));
                }

                foreach (var p in pares)
                {
                    decimal saldoF = Math.Max(0, p.fTot - p.fAb);
                    decimal saldoC = Math.Max(0, p.cMonto - p.cPag);
                    if (saldoF < saldoC - CXC_TOL)
                    {
                        decimal nuevoPag = saldoF <= CXC_TOL ? p.cMonto : Math.Max(0, p.cMonto - saldoF);
                        string nuevoEst = saldoF <= CXC_TOL ? "Saldada"
                            : (!string.IsNullOrEmpty(p.cVence) && string.CompareOrdinal(p.cVence.Substring(0, Math.Min(10, p.cVence.Length)), hoy) < 0 ? "Vencida" : "Vigente");
                        using var u = c.CreateCommand(); u.Transaction = tx;
                        u.CommandText = "UPDATE CuentasCobrar SET Pagado = $p, Estado = $e WHERE Id = $id";
                        u.Parameters.AddWithValue("$p", (double)nuevoPag);
                        u.Parameters.AddWithValue("$e", nuevoEst);
                        u.Parameters.AddWithValue("$id", p.id);
                        u.ExecuteNonQuery();
                    }
                    else if (saldoC < saldoF - CXC_TOL)
                    {
                        decimal nuevoAb = saldoC <= CXC_TOL ? p.fTot : Math.Max(0, p.fTot - saldoC);
                        using var u = c.CreateCommand(); u.Transaction = tx;
                        u.CommandText = "UPDATE Facturas SET Abono = $a WHERE NumeroF = $n";
                        u.Parameters.AddWithValue("$a", (double)nuevoAb);
                        u.Parameters.AddWithValue("$n", p.numF);
                        u.ExecuteNonQuery();
                    }
                }

                // 3. Normalizar estados saldados y vencidos
                using (var uS = c.CreateCommand())
                {
                    uS.Transaction = tx;
                    uS.CommandText = @"
                        UPDATE CuentasCobrar SET Pagado = Monto, Estado = 'Saldada'
                        WHERE COALESCE(Estado,'') NOT IN ('Saldada','Anulada')
                          AND Monto > 0 AND (Monto - COALESCE(Pagado,0)) <= 0.005";
                    uS.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch { /* no bloquear la UI si hay algún bloqueo momentáneo */ }
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
        //  PinBot — Operadores (Usuarios autorizados)
        //  Solo roles Admin y Ventas pueden tener PIN asignado.
        //  PIN almacenado como hash PBKDF2 (mismo formato que contraseñas).
        //  Bloqueo tras 5 intentos fallidos por 30 minutos.
        // ════════════════════════════════════════════════════════════════════

        private const int PIN_MAX_INTENTOS  = 5;
        private const int PIN_BLOQUEO_MINS  = 30;
        private const int PIN_LONGITUD      = 6;

        /// <summary>Genera un PIN numérico aleatorio de 6 dígitos, lo hashea y lo guarda.
        /// Devuelve el PIN en texto plano (mostrar UNA SOLA VEZ al Admin).</summary>
        public static string GenerarPinBot(int usuarioId)
        {
            // Generar PIN numérico criptográficamente aleatorio
            var bytes = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            int num = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1_000_000;
            string pin = num.ToString().PadLeft(PIN_LONGITUD, '0');

            string hash = HashPassword(pin);  // PBKDF2

            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Usuarios
                SET PinBotHash = $h, PinBotIntentos = 0, PinBotBloqueadoHasta = NULL
                WHERE Id = $id";
            cmd.Parameters.AddWithValue("$h", hash);
            cmd.Parameters.AddWithValue("$id", usuarioId);
            cmd.ExecuteNonQuery();

            return pin;
        }

        /// <summary>Revoca el PIN de un operador.</summary>
        public static void RevocarPinBot(int usuarioId)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"UPDATE Usuarios
                SET PinBotHash = '', PinBotIntentos = 0, PinBotBloqueadoHasta = NULL
                WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", usuarioId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Valida un PIN de operador. Devuelve el Usuario si es correcto, null si falla.
        /// Implementa bloqueo tras 5 intentos fallidos.
        /// </summary>
        public static (Usuario? operador, string? error) ValidarPinBotOperador(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin) || pin.Length != PIN_LONGITUD || !pin.All(char.IsDigit))
                return (null, "PIN inválido.");

            using var c = Conn();

            // Buscar todos los usuarios con PIN asignado, activos, y con rol Admin o Ventas
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, NombreUsuario, NombreCompleto, Rol, PinBotHash,
                       PinBotIntentos, PinBotBloqueadoHasta
                FROM Usuarios
                WHERE Activo = 1
                  AND COALESCE(PinBotHash,'') != ''
                  AND (Rol = 'Admin' OR Rol = 'Ventas')";

            var candidatos = new List<(int id, string user, string nombre, string rol,
                                       string hash, int intentos, DateTime? bloqueado)>();
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    candidatos.Add((
                        r.GetInt32(0),
                        S(r, 1), S(r, 2), S(r, 3), S(r, 4),
                        r.IsDBNull(5) ? 0 : r.GetInt32(5),
                        r.IsDBNull(6) ? null : DateTime.Parse(r.GetString(6))
                    ));
                }
            }

            if (candidatos.Count == 0)
                return (null, "No hay operadores con PIN configurado.");

            // Verificar PIN contra cada candidato (PBKDF2 - tiempo constante)
            foreach (var op in candidatos)
            {
                // ¿Está bloqueado?
                if (op.bloqueado.HasValue && op.bloqueado.Value > DateTime.Now)
                {
                    var resta = (int)(op.bloqueado.Value - DateTime.Now).TotalMinutes + 1;
                    // No revelar si el PIN es correcto durante el bloqueo
                    continue;
                }

                bool correcto = VerificarPassword(pin, op.hash);

                if (correcto)
                {
                    // Resetear intentos
                    var cmdOk = c.CreateCommand();
                    cmdOk.CommandText = @"UPDATE Usuarios
                        SET PinBotIntentos = 0, PinBotBloqueadoHasta = NULL
                        WHERE Id = $id";
                    cmdOk.Parameters.AddWithValue("$id", op.id);
                    cmdOk.ExecuteNonQuery();

                    return (new Usuario
                    {
                        Id             = op.id,
                        NombreUsuario  = op.user,
                        NombreCompleto = op.nombre,
                        Rol            = op.rol,
                    }, null);
                }
            }

            // PIN incorrecto: incrementar intentos de TODOS los candidatos no bloqueados
            foreach (var op in candidatos)
            {
                if (op.bloqueado.HasValue && op.bloqueado.Value > DateTime.Now)
                    continue;

                int nuevosIntentos = op.intentos + 1;
                var cmdFail = c.CreateCommand();
                if (nuevosIntentos >= PIN_MAX_INTENTOS)
                {
                    var hasta = DateTime.Now.AddMinutes(PIN_BLOQUEO_MINS)
                        .ToString("yyyy-MM-dd HH:mm:ss");
                    cmdFail.CommandText = @"UPDATE Usuarios
                        SET PinBotIntentos = $i, PinBotBloqueadoHasta = $h
                        WHERE Id = $id";
                    cmdFail.Parameters.AddWithValue("$h", hasta);
                }
                else
                {
                    cmdFail.CommandText = @"UPDATE Usuarios
                        SET PinBotIntentos = $i
                        WHERE Id = $id";
                }
                cmdFail.Parameters.AddWithValue("$i", nuevosIntentos);
                cmdFail.Parameters.AddWithValue("$id", op.id);
                cmdFail.ExecuteNonQuery();
            }

            // Verificar si alguno quedó bloqueado
            bool algunoBloqueado = candidatos.Any(op =>
                (op.intentos + 1) >= PIN_MAX_INTENTOS);

            return (null, algunoBloqueado
                ? $"PIN incorrecto. Demasiados intentos, bloqueado por {PIN_BLOQUEO_MINS} minutos."
                : "PIN incorrecto.");
        }

        /// <summary>Verifica si un usuario tiene rol válido para PIN de bot.</summary>
        public static bool PuedeAsignarPinBot(string rol)
            => rol == "Admin" || rol == "Ventas";
    }
}
