using Microsoft.Data.Sqlite;

namespace SistemaGestion.Api
{
    public class InasistenciaEmpleado
    {
        public int      Id               { get; set; }
        public int      IdEmpleado       { get; set; }
        public string   NombreEmpleado   { get; set; } = "";
        public string   TipoEmpleado     { get; set; } = "";
        public DateTime Fecha            { get; set; }
        public bool     Justificada      { get; set; }
        public decimal  MontoDescuentoBs { get; set; }
        public string   Observaciones    { get; set; } = "";
    }

    /// <summary>
    /// Conexión directa a la BD SQLite del sistema de escritorio.
    /// La API lee/escribe la misma BD sin depender del proyecto WinForms.
    /// </summary>
    public static class Db
    {
        private static string? _rutaBD;

        public static void Inicializar(string rutaBD)
        {
            _rutaBD = rutaBD;
            // Verificar que existe
            if (!File.Exists(rutaBD))
                throw new FileNotFoundException($"BD no encontrada: {rutaBD}");

            // Fix defensivo: columnas que pueden faltar en BDs antiguas
            try
            {
                using var c = Conn();
                string[] fixes = {
                    "ALTER TABLE Inventario ADD COLUMN Activo INTEGER NOT NULL DEFAULT 1",
                    "ALTER TABLE Clientes ADD COLUMN Email TEXT DEFAULT ''",
                    "ALTER TABLE Clientes ADD COLUMN Contacto TEXT DEFAULT ''",
                    "ALTER TABLE DetallesFactura ADD COLUMN PrecioUnit REAL DEFAULT 0",
                    "ALTER TABLE Clientes ADD COLUMN PinBot TEXT DEFAULT ''",
                    "ALTER TABLE Usuarios ADD COLUMN PinBotHash TEXT DEFAULT ''",
                    "ALTER TABLE Usuarios ADD COLUMN PinBotIntentos INTEGER NOT NULL DEFAULT 0",
                    "ALTER TABLE Usuarios ADD COLUMN PinBotBloqueadoHasta TEXT",
                    "ALTER TABLE Usuarios ADD COLUMN Roles TEXT DEFAULT ''",
                    "ALTER TABLE RegistroProduccionDia ADD COLUMN TipoMaterial TEXT",
                    "ALTER TABLE DetalleProduccionMolde ADD COLUMN CantPrimera REAL DEFAULT 0",
                    "ALTER TABLE DetalleProduccionMolde ADD COLUMN CantSegunda REAL DEFAULT 0",
                    "ALTER TABLE DetalleProduccionMolde ADD COLUMN CantReciclado REAL DEFAULT 0",
                    "ALTER TABLE DetalleProduccionMolde ADD COLUMN CantDefectuosas REAL DEFAULT 0",
                    // Modelo de pago por tipo de empleado (Obrero/Oficina/Taller/Administrativo/Temporal)
                    "ALTER TABLE Empleados ADD COLUMN FrecuenciaPago TEXT DEFAULT 'Semanal'",
                    "ALTER TABLE Empleados ADD COLUMN SueldoBaseUSD REAL NOT NULL DEFAULT 0",
                    "ALTER TABLE Empleados ADD COLUMN EligibleBonoProd INTEGER NOT NULL DEFAULT 0",
                    "ALTER TABLE Empleados ADD COLUMN EligibleBonoVenta INTEGER NOT NULL DEFAULT 0",
                    // Bono por producto (Bs): pago por unidad producida / vendida, igual para todos
                    "ALTER TABLE Inventario ADD COLUMN BonoProduccionBs REAL NOT NULL DEFAULT 0",
                    "ALTER TABLE Inventario ADD COLUMN BonoVentaBs REAL NOT NULL DEFAULT 0",
                    // Historial fechado de tarifas de bono por producto (para calcular semanas pasadas)
                    "CREATE TABLE IF NOT EXISTS TarifaProductoProduccion (Id INTEGER PRIMARY KEY AUTOINCREMENT, CodigoProducto TEXT NOT NULL, TarifaBs REAL NOT NULL DEFAULT 0, FechaDesde TEXT NOT NULL, FechaHasta TEXT, Usuario TEXT)",
                    "CREATE TABLE IF NOT EXISTS TarifaProductoVenta (Id INTEGER PRIMARY KEY AUTOINCREMENT, CodigoProducto TEXT NOT NULL, TarifaBs REAL NOT NULL DEFAULT 0, FechaDesde TEXT NOT NULL, FechaHasta TEXT, Usuario TEXT)"
                };
                foreach (var sql in fixes)
                {
                    try
                    {
                        using var cmd = c.CreateCommand();
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"[API] Fix aplicado: {sql[..Math.Min(sql.Length, 60)]}");
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
                            Console.WriteLine($"[API] WARN fix: {ex.Message}");
                    }
                }

                // Backfill PrecioUnit con precio actual de inventario donde sea 0
                try
                {
                    using var cmdBf = c.CreateCommand();
                    cmdBf.CommandText = @"
                        UPDATE DetallesFactura
                        SET PrecioUnit = COALESCE(
                            (SELECT i.Precio FROM Inventario i WHERE i.Codigo = DetallesFactura.Codigo), 0)
                        WHERE PrecioUnit = 0 OR PrecioUnit IS NULL";
                    int rows = cmdBf.ExecuteNonQuery();
                    if (rows > 0) Console.WriteLine($"[API] Backfill PrecioUnit: {rows} filas actualizadas");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[API] WARN backfill PrecioUnit: {ex.Message}");
                }

                // Backfill Roles (multi-rol) desde el Rol primario donde este vacio
                try
                {
                    using var cmdBr = c.CreateCommand();
                    cmdBr.CommandText = "UPDATE Usuarios SET Roles=Rol WHERE Roles IS NULL OR Roles=''";
                    int rows = cmdBr.ExecuteNonQuery();
                    if (rows > 0) Console.WriteLine($"[API] Backfill Roles: {rows} filas");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[API] WARN backfill Roles: {ex.Message}");
                }

                // Tabla de participantes de producción (multi-obrero por día)
                try
                {
                    using var cmdPp = c.CreateCommand();
                    cmdPp.CommandText =
                        "CREATE TABLE IF NOT EXISTS ParticipantesProduccionDia (" +
                        "Id INTEGER PRIMARY KEY AUTOINCREMENT, IdRegistro INTEGER NOT NULL, IdEmpleado INTEGER NOT NULL);" +
                        "CREATE INDEX IF NOT EXISTS idx_partprod_reg ON ParticipantesProduccionDia(IdRegistro);";
                    cmdPp.ExecuteNonQuery();
                }
                catch (Exception ex) { Console.WriteLine($"[API] WARN ParticipantesProduccionDia: {ex.Message}"); }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN fixes defensivos: {ex.Message}");
            }
        }

        public static SqliteConnection Conn()
        {
            if (_rutaBD is null)
                throw new InvalidOperationException("BD no inicializada.");

            var conn = new SqliteConnection($"Data Source={_rutaBD};");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=3000; PRAGMA foreign_keys=ON;";
            cmd.ExecuteNonQuery();
            return conn;
        }

        // ── PBKDF2 verificación de PIN (mismo formato que contraseñas: "v2:SALT:HASH") ──
        private const string HASH_PREFIX  = "v2";
        private const int    PBKDF2_ITERS = 600_000;

        /// <summary>Verifica un PIN contra un hash PBKDF2 almacenado.</summary>
        public static bool VerificarPinBot(string pin, string hashAlmacenado)
        {
            if (string.IsNullOrEmpty(hashAlmacenado) || !hashAlmacenado.StartsWith(HASH_PREFIX + ":"))
                return false;

            var partes = hashAlmacenado.Split(':');
            if (partes.Length != 3) return false;

            var salt = Convert.FromHexString(partes[1]);
            using var kdf = new System.Security.Cryptography.Rfc2898DeriveBytes(
                pin, salt, PBKDF2_ITERS,
                System.Security.Cryptography.HashAlgorithmName.SHA256);
            var hashCalculado = kdf.GetBytes(32);

            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(partes[2]), hashCalculado);
        }

        /// <summary>
        /// Marca como "requiere cambio de contraseña" a todos los usuarios
        /// cuyo hash todavía sea SHA-256 legacy (no empieza por "v2:").
        /// La columna RequiereCambioPassword ya existe; este método sólo
        /// la actualiza idempotentemente al arranque.
        /// Devuelve cuántos usuarios fueron marcados.
        /// </summary>
        public static int MarcarHashesLegacyParaMigracion()
        {
            try
            {
                using var c = Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Usuarios
                    SET RequiereCambioPassword = 1
                    WHERE Activo = 1
                      AND PasswordHash IS NOT NULL
                      AND PasswordHash <> ''
                      AND PasswordHash NOT LIKE 'v2:%'
                      AND (RequiereCambioPassword IS NULL OR RequiereCambioPassword = 0)";
                return cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN MarcarHashesLegacy: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Crea la tabla SesionesActivas si no existe. Usada para revocación
        /// de tokens (JTI). Idempotente.
        /// </summary>
        public static void EnsureSesionesActivas()
        {
            try
            {
                using var c = Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SesionesActivas (
                        Jti          TEXT PRIMARY KEY,
                        IdUsuario    INTEGER NOT NULL,
                        NombreUsuario TEXT NOT NULL,
                        IpEmision    TEXT,
                        FechaEmision TEXT NOT NULL,
                        FechaExpira  TEXT NOT NULL,
                        FechaRevoca  TEXT
                    );
                    CREATE INDEX IF NOT EXISTS IX_SesionesActivas_Usuario
                        ON SesionesActivas(IdUsuario);
                    CREATE INDEX IF NOT EXISTS IX_SesionesActivas_Expira
                        ON SesionesActivas(FechaExpira);";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN EnsureSesionesActivas: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea la tabla DetallesGasto si no existe.
        /// Almacena las líneas de detalle de cada egreso (factura de compra).
        /// IdArticulo es FK nullable a ProductosProveedor — nulo cuando el ítem
        /// no está en catálogo y se guarda como texto libre en Descripcion.
        /// EsInventariable = 1 cuando TipoInsumo == 'Materia prima':
        /// al confirmar el egreso el API actualiza stock + CPP en MovimientosInventario.
        /// Idempotente.
        /// </summary>
        public static void EnsureDetallesGasto()
        {
            try
            {
                using var c = Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS DetallesGasto (
                        Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                        IdGasto         INTEGER NOT NULL,
                        Descripcion     TEXT    NOT NULL,
                        IdArticulo      INTEGER,
                        CodigoInterno   TEXT,
                        TipoInsumo      TEXT,
                        EsInventariable INTEGER NOT NULL DEFAULT 0,
                        Cantidad        REAL    NOT NULL DEFAULT 0,
                        PrecioUnitario  REAL    NOT NULL DEFAULT 0,
                        Subtotal        REAL    NOT NULL DEFAULT 0,
                        FOREIGN KEY (IdGasto) REFERENCES Gastos(Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_DetallesGasto_Gasto
                        ON DetallesGasto(IdGasto);
                    CREATE INDEX IF NOT EXISTS IX_DetallesGasto_Articulo
                        ON DetallesGasto(IdArticulo);";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN EnsureDetallesGasto: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea la tabla ConfiguracionPortal (clave/valor) y garantiza el valor
        /// por defecto de vision_backend = ollama. Idempotente.
        /// </summary>
        public static void EnsureConfiguracionPortal()
        {
            try
            {
                using var c = Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS ConfiguracionPortal (
                        Clave              TEXT PRIMARY KEY,
                        Valor              TEXT NOT NULL,
                        FechaActualizacion TEXT
                    );
                    INSERT OR IGNORE INTO ConfiguracionPortal (Clave, Valor, FechaActualizacion)
                    VALUES ('vision_backend', 'ollama', datetime('now','localtime'));";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN EnsureConfiguracionPortal: {ex.Message}");
            }
        }

        /// <summary>Lee un valor de ConfiguracionPortal. Devuelve defaultVal si no existe.</summary>
        public static string GetConfig(string clave, string defaultVal = "")
        {
            try
            {
                using var c = Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT Valor FROM ConfiguracionPortal WHERE Clave = @k;";
                cmd.Parameters.AddWithValue("@k", clave);
                var val = cmd.ExecuteScalar();
                return val is null || val is DBNull ? defaultVal : val.ToString()!;
            }
            catch { return defaultVal; }
        }

        /// <summary>Actualiza o inserta un valor en ConfiguracionPortal.</summary>
        public static void SetConfig(string clave, string valor)
        {
            using var c = Conn();
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ConfiguracionPortal (Clave, Valor, FechaActualizacion)
                VALUES (@k, @v, datetime('now','localtime'))
                ON CONFLICT(Clave) DO UPDATE SET
                    Valor = excluded.Valor,
                    FechaActualizacion = excluded.FechaActualizacion;";
            cmd.Parameters.AddWithValue("@k", clave);
            cmd.Parameters.AddWithValue("@v", valor);
            cmd.ExecuteNonQuery();
        }

        // ── Helpers ───────────────────────────────────────────────────────
        public static string S(SqliteDataReader r, int i) =>
            r.IsDBNull(i) ? "" : r.GetString(i);

        public static decimal D(SqliteDataReader r, int i) =>
            r.IsDBNull(i) ? 0m : (decimal)r.GetDouble(i);

        public static int I(SqliteDataReader r, int i) =>
            r.IsDBNull(i) ? 0 : r.GetInt32(i);

        /// <summary>Lee un INTEGER que puede ser Int64 (COUNT, SUM, MAX, etc.).</summary>
        public static int I2(SqliteDataReader r, int i) =>
            r.IsDBNull(i) ? 0 : (int)r.GetInt64(i);

        // ── Inasistencias ──────────────────────────────────────────────────

        public static List<InasistenciaEmpleado> ObtenerInasistencias(
            int? idEmpleado = null, DateTime? desde = null, DateTime? hasta = null)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            var where = new List<string>();
            if (idEmpleado.HasValue) { where.Add("IdEmpleado=$id"); cmd.Parameters.AddWithValue("$id", idEmpleado.Value); }
            if (desde.HasValue)     { where.Add("Fecha>=$d");       cmd.Parameters.AddWithValue("$d", desde.Value.ToString("yyyy-MM-dd")); }
            if (hasta.HasValue)     { where.Add("Fecha<=$h");       cmd.Parameters.AddWithValue("$h", hasta.Value.ToString("yyyy-MM-dd")); }
            string w = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
            cmd.CommandText = $"SELECT Id,IdEmpleado,NombreEmpleado,TipoEmpleado,Fecha,Justificada,MontoDescuentoBs,Observaciones FROM InasistenciasEmpleado{w} ORDER BY Fecha DESC";
            var list = new List<InasistenciaEmpleado>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new InasistenciaEmpleado {
                    Id               = I(r,0),
                    IdEmpleado       = I(r,1),
                    NombreEmpleado   = S(r,2),
                    TipoEmpleado     = S(r,3),
                    Fecha            = DateTime.TryParse(S(r,4), out var f) ? f : DateTime.MinValue,
                    Justificada      = I(r,5) != 0,
                    MontoDescuentoBs = D(r,6),
                    Observaciones    = S(r,7)
                });
            return list;
        }

        /// <summary>Cache de bono de producción por día (API).</summary>
        private static readonly Dictionary<(int,string), decimal> _cacheBonoDia = new();
        public static void LimpiarCacheBono() => _cacheBonoDia.Clear();

        public static int RegistrarInasistencia(
            int idEmpleado, DateTime fecha, bool justificada,
            string observaciones = "", decimal? tasaOverride = null)
        {
            using var c = Conn();

            // Validar que la fecha caiga dentro de algún período de nómina
            var qp = c.CreateCommand();
            qp.CommandText = "SELECT COUNT(*) FROM PeriodosNomina WHERE $f >= FechaDesde AND $f <= FechaHasta";
            qp.Parameters.AddWithValue("$f", fecha.ToString("yyyy-MM-dd"));
            var periodoCount = (long)(qp.ExecuteScalar() ?? 0L);
            if (periodoCount == 0) return -2; // Fecha fuera de todo período

            // Obtener datos del empleado
            var qe = c.CreateCommand();
            qe.CommandText = "SELECT Nombre||' '||Apellido, TipoEmpleado, SueldoDiarioUSD FROM Empleados WHERE Id=$id";
            qe.Parameters.AddWithValue("$id", idEmpleado);
            string nombre = "", tipo = ""; decimal sueldoDiario = 0;
            using (var re = qe.ExecuteReader())
            {
                if (!re.Read()) return -1;
                nombre = S(re,0); tipo = S(re,1); sueldoDiario = D(re,2);
            }
            // Calcular monto del día (con cache)
            decimal montoBs = 0;
            if (tipo == "Produccion")
            {
                var cacheKey = (idEmpleado, fecha.ToString("yyyy-MM-dd"));
                if (!_cacheBonoDia.TryGetValue(cacheKey, out montoBs))
                {
                    montoBs = CalcularBonoProduccionDia(c, idEmpleado, fecha);
                    _cacheBonoDia[cacheKey] = montoBs;
                }
            }
            else
            {
                decimal tasa = tasaOverride ?? 1;
                if (tasa <= 0) tasa = 1;
                montoBs = sueldoDiario * tasa;
            }
            var cmd = c.CreateCommand();
            cmd.CommandText = @"INSERT INTO InasistenciasEmpleado
                (IdEmpleado,NombreEmpleado,TipoEmpleado,Fecha,Justificada,MontoDescuentoBs,Observaciones)
                VALUES($id,$ne,$te,$f,$j,$m,$o)
                ON CONFLICT(IdEmpleado,Fecha) DO UPDATE SET
                    Justificada=excluded.Justificada,
                    MontoDescuentoBs=excluded.MontoDescuentoBs,
                    Observaciones=excluded.Observaciones";
            cmd.Parameters.AddWithValue("$id", idEmpleado);
            cmd.Parameters.AddWithValue("$ne", nombre);
            cmd.Parameters.AddWithValue("$te", tipo);
            cmd.Parameters.AddWithValue("$f",  fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$j",  justificada ? 1 : 0);
            cmd.Parameters.AddWithValue("$m",  (double)montoBs);
            cmd.Parameters.AddWithValue("$o",  observaciones ?? "");
            cmd.ExecuteNonQuery();
            var qid = c.CreateCommand();
            qid.CommandText = "SELECT Id FROM InasistenciasEmpleado WHERE IdEmpleado=$id AND Fecha=$f";
            qid.Parameters.AddWithValue("$id", idEmpleado);
            qid.Parameters.AddWithValue("$f",  fecha.ToString("yyyy-MM-dd"));
            return (int)(long)(qid.ExecuteScalar() ?? -1L);
        }

        public static bool ActualizarInasistencia(int id, bool justificada, string observaciones)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "UPDATE InasistenciasEmpleado SET Justificada=$j, Observaciones=$o WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$j",  justificada ? 1 : 0);
            cmd.Parameters.AddWithValue("$o",  observaciones ?? "");
            return cmd.ExecuteNonQuery() > 0;
        }

        public static bool EliminarInasistencia(int id)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "DELETE FROM InasistenciasEmpleado WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        public static decimal CalcularDescuentoFaltas(
            int idEmpleado, DateTime desde, DateTime hasta, decimal tasa)
        {
            if (tasa <= 0) tasa = 1;
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT COALESCE(SUM(MontoDescuentoBs),0) FROM InasistenciasEmpleado
                WHERE IdEmpleado=$id AND Justificada=0 AND Fecha >= $d AND Fecha <= $h";
            cmd.Parameters.AddWithValue("$id", idEmpleado);
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            decimal totalBs = (decimal)(double)(cmd.ExecuteScalar() ?? 0.0);
            return Math.Round(totalBs / tasa, 2);
        }

        public static int ContarFaltasInjustificadas(
            int idEmpleado, DateTime desde, DateTime hasta)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT COUNT(*) FROM InasistenciasEmpleado
                WHERE IdEmpleado=$id AND Justificada=0 AND Fecha >= $d AND Fecha <= $h";
            cmd.Parameters.AddWithValue("$id", idEmpleado);
            cmd.Parameters.AddWithValue("$d", desde.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$h", hasta.ToString("yyyy-MM-dd"));
            return (int)(long)(cmd.ExecuteScalar() ?? 0L);
        }

        /// <summary>Calcula el bono de producción de un día específico para un empleado.</summary>
        private static decimal CalcularBonoProduccionDia(SqliteConnection c, int idEmpleado, DateTime fecha)
        {
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT COALESCE(SUM(d.Cantidad * t.TarifaBs),0)
                FROM DetalleProduccionMolde d
                JOIN RegistroProduccionDia r ON r.Id = d.IdRegistro
                JOIN TarifasBonoProduccion t
                  ON t.CodigoProducto = d.CodigoProducto
                 AND t.IdEmpleado     = $id
                 AND r.Fecha >= t.FechaDesde
                 AND (t.FechaHasta IS NULL OR r.Fecha <= t.FechaHasta)
                WHERE r.Fecha = $f
                  AND ( NOT EXISTS (SELECT 1 FROM ParticipantesProduccionDia pp
                                    WHERE pp.IdRegistro = r.Id)
                        OR EXISTS (SELECT 1 FROM ParticipantesProduccionDia p2
                                   WHERE p2.IdRegistro = r.Id AND p2.IdEmpleado = $id) )";
            cmd.Parameters.AddWithValue("$id", idEmpleado);
            cmd.Parameters.AddWithValue("$f",  fecha.ToString("yyyy-MM-dd"));
            try { return (decimal)(double)(cmd.ExecuteScalar() ?? 0.0); }
            catch { return 0; }
        }

        /// <summary>Asegura que la tabla InasistenciasEmpleado exista.</summary>
        public static void EnsureInasistencias()
        {
            try
            {
                using var c = Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS InasistenciasEmpleado (
                        Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                        IdEmpleado       INTEGER NOT NULL,
                        NombreEmpleado   TEXT,
                        TipoEmpleado     TEXT,
                        Fecha            TEXT    NOT NULL,
                        Justificada      INTEGER NOT NULL DEFAULT 0,
                        MontoDescuentoBs REAL    DEFAULT 0,
                        Observaciones    TEXT,
                        FOREIGN KEY(IdEmpleado) REFERENCES Empleados(Id)
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS idx_inas_emp_fecha ON InasistenciasEmpleado(IdEmpleado, Fecha);";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN EnsureInasistencias: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  ConfiguracionSistema — lectura y escritura de parámetros
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Lee un valor de ConfiguracionSistema. Devuelve defaultVal si no existe.
        /// </summary>
        public static string ObtenerConfig(string clave, string defaultVal = "")
        {
            try
            {
                using var c   = Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT Valor FROM ConfiguracionSistema WHERE Clave = $k LIMIT 1";
                cmd.Parameters.AddWithValue("$k", clave);
                var val = cmd.ExecuteScalar();
                return val is string s ? s : defaultVal;
            }
            catch { return defaultVal; }
        }

        /// <summary>
        /// Escribe (upsert) un valor en ConfiguracionSistema.
        /// </summary>
        public static void GuardarConfig(string clave, string valor)
        {
            using var c   = Conn();
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ConfiguracionSistema (Clave, Valor) VALUES ($k, $v)
                ON CONFLICT(Clave) DO UPDATE SET Valor = $v";
            cmd.Parameters.AddWithValue("$k", clave);
            cmd.Parameters.AddWithValue("$v", valor ?? "");
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Siembra los valores legales predeterminados si todavía no existen.
        /// No sobreescribe cambios hechos por el usuario.
        /// </summary>
        public static void EnsureConfigLegal()
        {
            void Seed(string k, string v) { if (ObtenerConfig(k, "") == "") GuardarConfig(k, v); }
            try
            {
                Seed("IVA_Porcentaje",     "16");
                Seed("IVSS_Empleado",      "4");
                Seed("RPVH_Empleado",      "1");
                Seed("IVSS_Patronal",      "9");
                Seed("RPVH_Patronal",      "2");
                Seed("INCES_Patronal",     "2");
                Seed("UT_Bs",              "0");
                Seed("SalarioMinimo_Bs",   "0");
                Seed("DiasCredito_Default","30");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN EnsureConfigLegal: {ex.Message}");
            }
        }

        // ── Cámaras ─────────────────────────────────────────────────────────

        public class CamaraDto
        {
            public int    Id            { get; set; }
            public string Nombre        { get; set; } = "";
            public string Ubicacion     { get; set; } = "";
            public string IpLocal       { get; set; } = "";
            public int    PuertoRtsp    { get; set; } = 554;
            public string UsuarioCamara { get; set; } = "admin";
            public string ClaveCifrada  { get; set; } = "";
            public string RutaStream    { get; set; } = "/Streaming/Channels/101";
            public string Modelo        { get; set; } = "";
            public bool   Activa        { get; set; } = true;
            public int    Orden         { get; set; }

            /// <summary>Descifra la clave AES (SG_CAM_KEY). Solo usar server-side.</summary>
            public string ObtenerClavePlano()
            {
                if (string.IsNullOrEmpty(ClaveCifrada)) return "";
                try
                {
                    // AES-256-CBC: base64( IV[16] + ciphertext )
                    var b64Key = Environment.GetEnvironmentVariable("SG_CAM_KEY",
                                     EnvironmentVariableTarget.Machine)
                              ?? Environment.GetEnvironmentVariable("SG_CAM_KEY")
                              ?? "";
                    if (string.IsNullOrEmpty(b64Key)) return "";
                    var key  = Convert.FromBase64String(b64Key);
                    var blob = Convert.FromBase64String(ClaveCifrada);
                    if (blob.Length < 17) return "";

                    var iv     = new byte[16];
                    var cipher = new byte[blob.Length - 16];
                    Buffer.BlockCopy(blob, 0, iv, 0, 16);
                    Buffer.BlockCopy(blob, 16, cipher, 0, cipher.Length);

                    using var aes = System.Security.Cryptography.Aes.Create();
                    aes.Key = key;
                    aes.IV  = iv;
                    using var dec = aes.CreateDecryptor();
                    var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
                    return System.Text.Encoding.UTF8.GetString(plain);
                }
                catch { return ""; }
            }
        }

        private static CamaraDto LeerCamaraDto(SqliteDataReader r) => new()
        {
            Id            = r.GetInt32(0),
            Nombre        = r.IsDBNull(1) ? "" : r.GetString(1),
            Ubicacion     = r.IsDBNull(2) ? "" : r.GetString(2),
            IpLocal       = r.IsDBNull(3) ? "" : r.GetString(3),
            PuertoRtsp    = r.GetInt32(4),
            UsuarioCamara = r.IsDBNull(5) ? "admin" : r.GetString(5),
            ClaveCifrada  = r.IsDBNull(6) ? "" : r.GetString(6),
            RutaStream    = r.IsDBNull(7) ? "" : r.GetString(7),
            Modelo        = r.IsDBNull(8) ? "" : r.GetString(8),
            Activa        = r.GetInt32(9) == 1,
            Orden         = r.GetInt32(10)
        };

        private const string SQL_CAM_COLS =
            "Id, Nombre, Ubicacion, IpLocal, PuertoRtsp, UsuarioCamara, " +
            "ClaveCifrada, RutaStream, Modelo, Activa, Orden";

        /// <summary>
        /// Cámaras visibles para el usuario.
        /// Admin → todas las activas.  Otros → solo las de PermisosCamara.
        /// </summary>
        public static List<CamaraDto> ObtenerCamarasVisibles(int idUsuario, bool esAdmin)
        {
            var lista = new List<CamaraDto>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            if (esAdmin)
            {
                cmd.CommandText = $"SELECT {SQL_CAM_COLS} FROM Camaras WHERE Activa=1 ORDER BY Orden, Nombre";
            }
            else
            {
                cmd.CommandText = @"
                    SELECT cam.Id, cam.Nombre, cam.Ubicacion, cam.IpLocal, cam.PuertoRtsp,
                           cam.UsuarioCamara, cam.ClaveCifrada, cam.RutaStream, cam.Modelo,
                           cam.Activa, cam.Orden
                    FROM Camaras cam
                    INNER JOIN PermisosCamara p ON p.IdCamara = cam.Id
                    WHERE p.IdUsuario = $u AND cam.Activa = 1
                    ORDER BY cam.Orden, cam.Nombre";
                cmd.Parameters.AddWithValue("$u", idUsuario);
            }
            using var r = cmd.ExecuteReader();
            while (r.Read()) lista.Add(LeerCamaraDto(r));
            return lista;
        }

        public static CamaraDto? ObtenerCamaraPorId(int id)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = $"SELECT {SQL_CAM_COLS} FROM Camaras WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? LeerCamaraDto(r) : null;
        }

        public static List<int> ObtenerPermisosCamara(int idUsuario)
        {
            var ids = new List<int>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT IdCamara FROM PermisosCamara WHERE IdUsuario=$u";
            cmd.Parameters.AddWithValue("$u", idUsuario);
            using var r = cmd.ExecuteReader();
            while (r.Read()) ids.Add(r.GetInt32(0));
            return ids;
        }

        public static int ContarCamarasVisibles(int idUsuario, bool esAdmin)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            if (esAdmin)
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Camaras WHERE Activa = 1";
            }
            else
            {
                cmd.CommandText = @"SELECT COUNT(*) FROM Camaras cam
                    INNER JOIN PermisosCamara p ON p.IdCamara = cam.Id
                    WHERE p.IdUsuario = $u AND cam.Activa = 1";
                cmd.Parameters.AddWithValue("$u", idUsuario);
            }
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
