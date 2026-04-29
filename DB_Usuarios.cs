using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  DB_Usuarios.cs — solo contiene MigrarV15
    //
    //  Usuario, Sesion, PermisosRol y todo el CRUD de usuarios
    //  están en DB.cs para evitar duplicados.
    // ════════════════════════════════════════════════════════════════════════
    public static partial class DatabaseService
    {
        public static void MigrarV15(SqliteConnection conn, SqliteTransaction tx)
        {
            AlterTableSafe(conn, tx,
                "ALTER TABLE Usuarios ADD COLUMN Roles TEXT DEFAULT ''");

            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText =
                "UPDATE Usuarios SET Roles = Rol WHERE Roles IS NULL OR Roles = ''";
            cmd.ExecuteNonQuery();

            SetSchemaVersion(conn, tx, 15);
        }
    }
}
