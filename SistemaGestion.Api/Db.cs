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

        // ── Helpers ───────────────────────────────────────────────────────
        public static string S(SqliteDataReader r, int i) =>
            r.IsDBNull(i) ? "" : r.GetString(i);

        public static decimal D(SqliteDataReader r, int i) =>
            r.IsDBNull(i) ? 0m : (decimal)r.GetDouble(i);

        public static int I(SqliteDataReader r, int i) =>
            r.IsDBNull(i) ? 0 : r.GetInt32(i);

        /// <summary>Ruta de la BD — %AppData%\SistemaGestion\sistema_gestion.db</summary>
        public static string RutaBD => _rutaBD ?? "";
    }
}
