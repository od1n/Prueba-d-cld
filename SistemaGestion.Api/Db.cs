using Microsoft.Data.Sqlite;

namespace SistemaGestion.Api
{
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
                    "ALTER TABLE Clientes ADD COLUMN Contacto TEXT DEFAULT ''"
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
    }
}
