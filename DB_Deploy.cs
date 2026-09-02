using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  MODELO — Tasa de cambio diaria (BCV + Paralelo)
    // ════════════════════════════════════════════════════════════════════════
    public class TasaCambio
    {
        public int      Id           { get; set; }
        public DateTime Fecha        { get; set; }
        public decimal  TasaBCV      { get; set; }
        public decimal  TasaParalelo { get; set; }
        public string   Usuario      { get; set; }
        // Origen del registro (auditoría):
        //   "AUTO_API"  — obtenido de dolarapi.com / bcv.org.ve sin edición
        //   "MANUAL"    — ingresado a mano por un usuario
        //   "EDITADO"   — auto-API pero el usuario modificó el valor antes de guardar
        //   "IMPORTADO" — futuro: importación masiva (CSV, Excel)
        public string   Origen       { get; set; } = "MANUAL";
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public string Descripcion =>
            $"{Fecha:dd/MM/yyyy}  BCV: {TasaBCV:N2}  |  Paralelo: {TasaParalelo:N2}";
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DATABASE SERVICE — Migración v16 + TasaCambio + ConfiguracionSistema
    // ════════════════════════════════════════════════════════════════════════
    public static partial class DatabaseService
    {
        // ════════════════════════════════════════════════════════════════
        //  MIGRACIÓN v16
        //  - Índices de rendimiento (resuelve reporte de ventas lento)
        //  - Tabla TasaCambio (BCV + Paralelo por día)
        //  - Tabla ConfiguracionSistema (clave-valor)
        //
        //  En DB.cs: SCHEMA_VERSION = 16
        //  En MigrarEsquema() antes de tx.Commit():
        //    if (versionActual < 16) MigrarV16(conn, tx);
        // ════════════════════════════════════════════════════════════════
        public static void MigrarV16(SqliteConnection conn, SqliteTransaction tx)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                CREATE INDEX IF NOT EXISTS idx_detfact_codigo
                    ON DetallesFactura(Codigo);
                CREATE INDEX IF NOT EXISTS idx_detfact_factura
                    ON DetallesFactura(IdFactura);
                CREATE INDEX IF NOT EXISTS idx_fact_fecha
                    ON Facturas(Fecha);
                CREATE INDEX IF NOT EXISTS idx_fact_fecha_cliente
                    ON Facturas(Fecha, Codigo);
                CREATE INDEX IF NOT EXISTS idx_cxc_estado
                    ON CuentasCobrar(Estado);
                CREATE INDEX IF NOT EXISTS idx_cxc_vence
                    ON CuentasCobrar(FechaVence);
                CREATE INDEX IF NOT EXISTS idx_mov_fecha_tipo
                    ON MovimientosInventario(FechaHora, TipoKardex);
                CREATE INDEX IF NOT EXISTS idx_detnomina_periodo
                    ON DetallesNomina(IdPeriodo);

                CREATE TABLE IF NOT EXISTS TasaCambio (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha        TEXT    NOT NULL UNIQUE,
                    TasaBCV      REAL    NOT NULL DEFAULT 0,
                    TasaParalelo REAL    NOT NULL DEFAULT 0,
                    Usuario      TEXT    NOT NULL DEFAULT 'Sistema'
                );
                CREATE INDEX IF NOT EXISTS idx_tasa_fecha
                    ON TasaCambio(Fecha DESC);

                CREATE TABLE IF NOT EXISTS ConfiguracionSistema (
                    Clave TEXT PRIMARY KEY,
                    Valor TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();

            SetSchemaVersion(conn, tx, 16);
        }

        // ════════════════════════════════════════════════════════════════
        //  CRUD — TasaCambio
        // ════════════════════════════════════════════════════════════════

        // Columnas estandarizadas para todas las queries de TasaCambio.
        // Si las columnas Origen/FechaRegistro no existen en BDs sin migrar,
        // COALESCE/NULL las degrada elegantemente en BindTasa.
        private const string TASA_COLUMNAS =
            "Id, Fecha, TasaBCV, TasaParalelo, Usuario, " +
            "COALESCE(Origen, 'MANUAL') AS Origen, FechaRegistro";

        public static TasaCambio ObtenerTasaHoy()
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT " + TASA_COLUMNAS +
                " FROM TasaCambio ORDER BY Fecha DESC LIMIT 1";
            using var r = cmd.ExecuteReader();
            return r.Read() ? BindTasa(r) : null;
        }

        public static TasaCambio ObtenerTasaPorFecha(DateTime fecha)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT " + TASA_COLUMNAS +
                " FROM TasaCambio WHERE Fecha <= $f ORDER BY Fecha DESC LIMIT 1";
            cmd.Parameters.AddWithValue("$f", fecha.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            return r.Read() ? BindTasa(r) : null;
        }

        public static List<TasaCambio> ObtenerHistorialTasas(int ultimos = 60)
        {
            var list = new List<TasaCambio>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT " + TASA_COLUMNAS +
                " FROM TasaCambio ORDER BY Fecha DESC LIMIT $n";
            cmd.Parameters.AddWithValue("$n", ultimos);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(BindTasa(r));
            return list;
        }

        /// <summary>
        /// Devuelve las facturas que usaron una tasa específica (por IdTasaCambio).
        /// Útil para trazabilidad: "qué facturas se vieron afectadas por esta tasa".
        /// </summary>
        public static List<(string NumeroF, string Fecha, decimal Total, string Codigo)>
            ObtenerFacturasQueUsaronTasa(int idTasa)
        {
            var list = new List<(string, string, decimal, string)>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT NumeroF, Fecha, COALESCE(TotalF, 0), COALESCE(Codigo, '')
                FROM Facturas
                WHERE IdTasaCambio = $id
                ORDER BY Fecha DESC, NumeroF DESC
                LIMIT 500";
            cmd.Parameters.AddWithValue("$id", idTasa);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetString(0), r.GetString(1),
                    (decimal)r.GetDouble(2), r.GetString(3)));
            return list;
        }

        /// <summary>
        /// Guarda o sobrescribe la tasa del día indicado.
        ///
        /// Seguridad y auditoría:
        ///   • Verifica permisos según el rol de la sesión actual:
        ///     - Si NO existe tasa para esa fecha → requiere "TasaIngreso".
        ///     - Si YA existe (sobrescritura) → requiere "TasaModificar".
        ///   • Valida BCV &gt; 0 y rangos razonables (evita typo de cero o exabytes).
        ///   • Audita el valor ANTERIOR cuando se sobrescribe (TASA_MODIFICADA),
        ///     o registra TASA_INGRESADA si es nueva. Origen se guarda en cada caso.
        ///   • FechaRegistro se llena con DateTime.Now (auditoría temporal).
        /// </summary>
        public static void GuardarTasa(TasaCambio t)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            if (t.TasaBCV <= 0)
                throw new Exception("La tasa BCV debe ser mayor que cero.");
            if (t.TasaBCV > 1_000_000m || t.TasaParalelo > 1_000_000m)
                throw new Exception("Las tasas exceden el rango razonable. Verifica los valores.");
            if (t.Fecha > DateTime.Today.AddDays(1))
                throw new Exception("No se puede registrar tasa para fecha futura.");

            // ── Permisos ────────────────────────────────────────────────────
            var anterior = ObtenerTasaPorFechaExacta(t.Fecha);
            string permisoNecesario = anterior == null ? "TasaIngreso" : "TasaModificar";
            string rol = Sesion.RolesEfectivos;
            if (!string.IsNullOrEmpty(rol) && !PermisosRol.Permite(rol, permisoNecesario))
            {
                RegistrarAuditoria("TASA_DENEGADA", "TasaCambio",
                    "Rol " + rol + " intentó " + permisoNecesario + " " +
                    t.Fecha.ToString("dd/MM/yyyy") + " — DENEGADO");
                throw new UnauthorizedAccessException(
                    "Tu rol (" + rol + ") no tiene permiso para " +
                    (anterior == null ? "ingresar" : "modificar") +
                    " tasas de cambio. Contacta a un administrador.");
            }

            string usuario = t.Usuario;
            if (string.IsNullOrEmpty(usuario)) usuario = Sesion.NombreUsuario ?? "Admin";
            string origen = string.IsNullOrEmpty(t.Origen) ? "MANUAL" : t.Origen;
            string fechaRegStr = (t.FechaRegistro == default
                ? DateTime.Now : t.FechaRegistro).ToString("yyyy-MM-dd HH:mm:ss");

            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TasaCambio (Fecha, TasaBCV, TasaParalelo, Usuario, Origen, FechaRegistro)
                VALUES ($f, $bcv, $par, $us, $or, $fr)
                ON CONFLICT(Fecha) DO UPDATE SET
                    TasaBCV       = $bcv,
                    TasaParalelo  = $par,
                    Usuario       = $us,
                    Origen        = $or,
                    FechaRegistro = $fr";
            cmd.Parameters.AddWithValue("$f",   t.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$bcv", (double)t.TasaBCV);
            cmd.Parameters.AddWithValue("$par", (double)t.TasaParalelo);
            cmd.Parameters.AddWithValue("$us",  usuario);
            cmd.Parameters.AddWithValue("$or",  origen);
            cmd.Parameters.AddWithValue("$fr",  fechaRegStr);
            cmd.ExecuteNonQuery();

            // ── Auditoría detallada ─────────────────────────────────────────
            if (anterior == null)
            {
                RegistrarAuditoria("TASA_INGRESADA", "TasaCambio",
                    string.Format("{0:dd/MM/yyyy} BCV={1:N4} PAR={2:N4} Origen={3} Usuario={4}",
                        t.Fecha, t.TasaBCV, t.TasaParalelo, origen, usuario));
            }
            else
            {
                RegistrarAuditoria("TASA_MODIFICADA", "TasaCambio",
                    string.Format(
                        "{0:dd/MM/yyyy}: BCV {1:N4}->{2:N4}  PAR {3:N4}->{4:N4}  " +
                        "Origen={5} (antes {6}) Usuario={7} (antes {8})",
                        t.Fecha, anterior.TasaBCV, t.TasaBCV,
                        anterior.TasaParalelo, t.TasaParalelo,
                        origen, anterior.Origen ?? "MANUAL",
                        usuario, anterior.Usuario ?? ""));
            }
        }

        /// <summary>
        /// Devuelve la tasa para la fecha EXACTA, o null si no existe.
        /// (Distinto de ObtenerTasaPorFecha que devuelve la última ≤ fecha.)
        /// </summary>
        public static TasaCambio ObtenerTasaPorFechaExacta(DateTime fecha)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Fecha, TasaBCV, TasaParalelo, Usuario,
                       COALESCE(Origen, 'MANUAL'), FechaRegistro
                FROM TasaCambio WHERE Fecha = $f LIMIT 1";
            cmd.Parameters.AddWithValue("$f", fecha.ToString("yyyy-MM-dd"));
            using var r = cmd.ExecuteReader();
            return r.Read() ? BindTasa(r) : null;
        }

        private static TasaCambio BindTasa(SqliteDataReader r) => new TasaCambio
        {
            Id           = r.GetInt32(0),
            Fecha        = DateTime.Parse(r.GetString(1)),
            TasaBCV      = r.IsDBNull(2) ? 0m : (decimal)r.GetDouble(2),
            TasaParalelo = r.IsDBNull(3) ? 0m : (decimal)r.GetDouble(3),
            Usuario      = r.IsDBNull(4) ? ""  : r.GetString(4),
            Origen       = r.FieldCount > 5 && !r.IsDBNull(5) ? r.GetString(5) : "MANUAL",
            FechaRegistro = r.FieldCount > 6 && !r.IsDBNull(6) &&
                            DateTime.TryParse(r.GetString(6), out var fr)
                            ? fr : default
        };

        // ════════════════════════════════════════════════════════════════
        //  CRUD — ConfiguracionSistema
        // ════════════════════════════════════════════════════════════════

        public static string ObtenerConfig(string clave, string defaultVal = "")
        {
            try
            {
                using var c = Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT Valor FROM ConfiguracionSistema WHERE Clave=$k";
                cmd.Parameters.AddWithValue("$k", clave);
                var obj = cmd.ExecuteScalar();
                return obj == null ? defaultVal : obj.ToString();
            }
            catch { return defaultVal; }
        }

        public static void GuardarConfig(string clave, string valor)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ConfiguracionSistema (Clave, Valor) VALUES ($k, $v)
                ON CONFLICT(Clave) DO UPDATE SET Valor = $v";
            cmd.Parameters.AddWithValue("$k", clave);
            cmd.Parameters.AddWithValue("$v", valor ?? "");
            cmd.ExecuteNonQuery();
        }
    }
}


// ── Parcial adicional en DatabaseService ─────────────────────────────────
namespace SistemaGestion
{
    public static partial class DatabaseService
    {
        // ════════════════════════════════════════════════════════════════
        //  MIGRACIÓN v17 — columna Activo en Inventario
        //  Permite archivar productos con historial sin borrarlos.
        //
        //  En DB.cs: SCHEMA_VERSION = 17
        //  En MigrarEsquema() antes de tx.Commit():
        //    if (versionActual < 17) MigrarV17(conn, tx);
        // ════════════════════════════════════════════════════════════════
        public static void MigrarV17(SqliteConnection conn, SqliteTransaction tx)
        {
            // Agrega columna Activo; todos los existentes quedan activos (default 1)
            AlterTableSafe(conn, tx,
                "ALTER TABLE Inventario ADD COLUMN Activo INTEGER NOT NULL DEFAULT 1");
            SetSchemaVersion(conn, tx, 17);
        }

        // ════════════════════════════════════════════════════════════════════
        //  MIGRACIÓN v18 — Seguridad de autenticación (PBKDF2 + Rate Limiting)
        // ════════════════════════════════════════════════════════════════════
        /// <summary>
        /// v18: Agrega columnas de seguridad a la tabla Usuarios:
        ///   - RequiereCambioPassword: fuerza cambio en primer login (admin=1, resto=0)
        ///   - IntentosHoy: contador de intentos fallidos (rate limiting persistente)
        ///   - BloqueadoHasta: timestamp hasta el que el usuario está bloqueado
        ///
        /// Los hashes SHA-256 existentes (legacy) son compatibles hacia atrás:
        /// ValidarLogin() los acepta y los migra a PBKDF2 automáticamente en el
        /// siguiente login exitoso de cada usuario.
        /// </summary>
        public static void MigrarV18(SqliteConnection conn, SqliteTransaction tx)
        {
            // Columnas nuevas en Usuarios
            AlterTableSafe(conn, tx,
                "ALTER TABLE Usuarios ADD COLUMN RequiereCambioPassword INTEGER NOT NULL DEFAULT 0");
            AlterTableSafe(conn, tx,
                "ALTER TABLE Usuarios ADD COLUMN IntentosHoy INTEGER NOT NULL DEFAULT 0");
            AlterTableSafe(conn, tx,
                "ALTER TABLE Usuarios ADD COLUMN BloqueadoHasta TEXT");

            // Forzar cambio de contraseña para el usuario admin
            // (sus credenciales por defecto admin/admin123 son un riesgo)
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Usuarios SET RequiereCambioPassword=1 WHERE Rol='Admin'";
            cmd.ExecuteNonQuery();

            // Índice para acelerar la consulta de bloqueo en ValidarLogin
            var cmdIdx = conn.CreateCommand();
            cmdIdx.Transaction = tx;
            cmdIdx.CommandText = @"CREATE INDEX IF NOT EXISTS idx_usuarios_bloqueado
                ON Usuarios(BloqueadoHasta) WHERE BloqueadoHasta IS NOT NULL";
            cmdIdx.ExecuteNonQuery();

            SetSchemaVersion(conn, tx, 18);
        }

        // ════════════════════════════════════════════════════════════════
        //  EnsureConfigLegal — siembra valores legales predeterminados
        //  Solo escribe si la clave no existe (no sobreescribe cambios del usuario).
        // ════════════════════════════════════════════════════════════════
        public static void EnsureConfigLegal()
        {
            void Seed(string k, string v) { if (ObtenerConfig(k, "") == "") GuardarConfig(k, v); }
            Seed("IVA_Porcentaje",    "16");
            Seed("IVSS_Empleado",     "4");
            Seed("RPVH_Empleado",     "1");
            Seed("IVSS_Patronal",     "9");
            Seed("RPVH_Patronal",     "2");
            Seed("INCES_Patronal",    "2");
            Seed("UT_Bs",             "0");
            Seed("SalarioMinimo_Bs",  "0");
            Seed("DiasCredito_Default","30");
        }
    }
}
