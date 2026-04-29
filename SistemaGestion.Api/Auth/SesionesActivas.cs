using Microsoft.Data.Sqlite;

namespace SistemaGestion.Api.Auth
{
    /// <summary>
    /// Gestiona los JWT activos en la tabla SesionesActivas.
    /// Permite revocación al logout, al cambio de contraseña, o de forma
    /// administrativa. La validación se aplica en el middleware después
    /// de UseAuthentication.
    /// </summary>
    public static class SesionesActivas
    {
        public static void Registrar(int idUsuario, string nombreUsuario,
            string jti, string? ip, DateTime expiraUtc)
        {
            try
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO SesionesActivas
                        (Jti, IdUsuario, NombreUsuario, IpEmision, FechaEmision, FechaExpira)
                    VALUES ($jti, $id, $nu, $ip, $em, $ex)";
                cmd.Parameters.AddWithValue("$jti", jti);
                cmd.Parameters.AddWithValue("$id",  idUsuario);
                cmd.Parameters.AddWithValue("$nu",  nombreUsuario);
                cmd.Parameters.AddWithValue("$ip",  (object?)ip ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$em",  DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$ex",  expiraUtc.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN Sesiones.Registrar: {ex.Message}");
            }
        }

        /// <summary>
        /// True si el jti está registrado, no expirado y no revocado.
        /// </summary>
        public static bool EsValido(string jti)
        {
            if (string.IsNullOrEmpty(jti)) return false;
            try
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT FechaRevoca, FechaExpira FROM SesionesActivas WHERE Jti=$jti";
                cmd.Parameters.AddWithValue("$jti", jti);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return false;       // jti no existe = revocado/desconocido
                if (!r.IsDBNull(0)) return false;  // FechaRevoca poblada = revocado
                if (DateTime.TryParse(r.GetString(1), out var exp) && exp < DateTime.UtcNow)
                    return false;                  // expirado
                return true;
            }
            catch
            {
                // Falla cerrada: si la BD no responde, denegamos.
                return false;
            }
        }

        public static void Revocar(string jti)
        {
            if (string.IsNullOrEmpty(jti)) return;
            try
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    UPDATE SesionesActivas
                    SET FechaRevoca=$fr
                    WHERE Jti=$jti AND FechaRevoca IS NULL";
                cmd.Parameters.AddWithValue("$fr",  DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$jti", jti);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN Sesiones.Revocar: {ex.Message}");
            }
        }

        /// <summary>
        /// Revoca TODAS las sesiones activas de un usuario. Llamar tras
        /// cambio de contraseña o desactivación de cuenta.
        /// </summary>
        public static int RevocarTodasDeUsuario(int idUsuario)
        {
            try
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    UPDATE SesionesActivas
                    SET FechaRevoca=$fr
                    WHERE IdUsuario=$id AND FechaRevoca IS NULL";
                cmd.Parameters.AddWithValue("$fr", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$id", idUsuario);
                return cmd.ExecuteNonQuery();
            }
            catch { return 0; }
        }

        /// <summary>
        /// Borra sesiones expiradas hace más de 24h. Llamar periódicamente
        /// (al arranque y/o desde un timer).
        /// </summary>
        public static int LimpiarExpiradas()
        {
            try
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    DELETE FROM SesionesActivas
                    WHERE FechaExpira < $cutoff";
                cmd.Parameters.AddWithValue("$cutoff",
                    DateTime.UtcNow.AddHours(-24).ToString("yyyy-MM-dd HH:mm:ss"));
                return cmd.ExecuteNonQuery();
            }
            catch { return 0; }
        }
    }
}
