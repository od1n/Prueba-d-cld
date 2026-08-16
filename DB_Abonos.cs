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
