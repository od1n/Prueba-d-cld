// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: DB_Camaras.cs  →  raíz del proyecto
//
// Catálogo de cámaras EZVIZ (RTSP local) y matriz de permisos por usuario.
//
// Diseño:
//   - La clave RTSP (codigo de verificacion del sticker) se guarda CIFRADA
//     con DPAPI (SecurityService.DpapiCifrar). Solo se descifra al construir
//     la URL en runtime para conectar. Nunca se guarda en plano.
//   - Admin ve todas las cámaras activas implícitamente, sin necesidad de
//     filas en PermisosCamara.
//   - Resto de usuarios: relación N:N en PermisosCamara. Sin filas → no ven.
//   - RutaStream configurable por cámara: modelos viejos EZVIZ usan /H.264,
//     modelos nuevos /Streaming/Channels/101 (main) o /102 (sub).
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    public class Camara
    {
        public int      Id            { get; set; }
        public string   Nombre        { get; set; } = "";
        public string   Ubicacion     { get; set; } = "";
        public string   IpLocal       { get; set; } = "";
        public int      PuertoRtsp    { get; set; } = 554;
        public string   UsuarioCamara { get; set; } = "admin";
        public string   ClaveCifrada  { get; set; } = "";   // base64 DPAPI
        public string   RutaStream    { get; set; } = "/Streaming/Channels/101";
        public string   Modelo        { get; set; } = "";
        public bool     Activa        { get; set; } = true;
        public int      Orden         { get; set; }
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Devuelve la clave en plano descifrando con DPAPI. Solo usar en
        /// runtime para construir la URL RTSP. No exponer en UI ni logs.
        /// </summary>
        public string ObtenerClavePlano()
        {
            if (string.IsNullOrEmpty(ClaveCifrada)) return "";
            try { return SecurityService.DpapiDescifrar(ClaveCifrada); }
            catch { return ""; }   // perfil Windows distinto o blob corrupto
        }

        /// <summary>
        /// Construye la URL RTSP completa lista para pasarle a LibVLC.
        /// rtsp://usr:pwd@ip:puerto/ruta
        /// El usuario y la clave se URL-encodean por si tienen caracteres
        /// especiales (los códigos de verificación EZVIZ son alfanuméricos
        /// sencillos, pero por seguridad).
        /// </summary>
        public string ConstruirUrlRtsp()
        {
            string pass = ObtenerClavePlano();
            string usr  = Uri.EscapeDataString(UsuarioCamara ?? "admin");
            string pwd  = Uri.EscapeDataString(pass ?? "");
            string ruta = string.IsNullOrEmpty(RutaStream)
                ? "/Streaming/Channels/101"
                : (RutaStream.StartsWith("/") ? RutaStream : "/" + RutaStream);
            return $"rtsp://{usr}:{pwd}@{IpLocal}:{PuertoRtsp}{ruta}";
        }

        /// <summary>URL enmascarada (sin clave) para mostrar en UI / logs.</summary>
        public string ConstruirUrlVisible()
        {
            string ruta = string.IsNullOrEmpty(RutaStream)
                ? "/Streaming/Channels/101"
                : (RutaStream.StartsWith("/") ? RutaStream : "/" + RutaStream);
            return $"rtsp://{UsuarioCamara}:***@{IpLocal}:{PuertoRtsp}{ruta}";
        }
    }

    public static partial class DatabaseService
    {
        // ── MIGRACION v23 ───────────────────────────────────────────────────
        // Llamada desde MigrarEsquema en DB.cs cuando user_version < 23.
        internal static void EjecutarMigracionV23(SqliteConnection conn, SqliteTransaction tx)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Camaras (
                    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre        TEXT    NOT NULL,
                    Ubicacion     TEXT    NOT NULL DEFAULT '',
                    IpLocal       TEXT    NOT NULL,
                    PuertoRtsp    INTEGER NOT NULL DEFAULT 554,
                    UsuarioCamara TEXT    NOT NULL DEFAULT 'admin',
                    ClaveCifrada  TEXT    NOT NULL DEFAULT '',
                    RutaStream    TEXT    NOT NULL DEFAULT '/Streaming/Channels/101',
                    Modelo        TEXT    NOT NULL DEFAULT '',
                    Activa        INTEGER NOT NULL DEFAULT 1,
                    Orden         INTEGER NOT NULL DEFAULT 0,
                    FechaCreacion TEXT    NOT NULL
                );

                CREATE TABLE IF NOT EXISTS PermisosCamara (
                    IdUsuario INTEGER NOT NULL,
                    IdCamara  INTEGER NOT NULL,
                    PRIMARY KEY (IdUsuario, IdCamara),
                    FOREIGN KEY (IdUsuario) REFERENCES Usuarios(Id) ON DELETE CASCADE,
                    FOREIGN KEY (IdCamara)  REFERENCES Camaras(Id)  ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_PermisosCamara_Usuario
                    ON PermisosCamara(IdUsuario);
                CREATE INDEX IF NOT EXISTS idx_Camaras_Activa_Orden
                    ON Camaras(Activa, Orden, Nombre);
            ";
            cmd.ExecuteNonQuery();
        }

        // ── CRUD Camaras ─────────────────────────────────────────────────────

        public static List<Camara> ObtenerTodasCamaras(bool incluirInactivas = false)
        {
            var lista = new List<Camara>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Nombre, Ubicacion, IpLocal, PuertoRtsp, UsuarioCamara,
                       ClaveCifrada, RutaStream, Modelo, Activa, Orden, FechaCreacion
                FROM Camaras
                " + (incluirInactivas ? "" : "WHERE Activa = 1") + @"
                ORDER BY Orden, Nombre";
            using var r = cmd.ExecuteReader();
            while (r.Read()) lista.Add(LeerCamara(r));
            return lista;
        }

        /// <summary>
        /// Cámaras visibles para el usuario. Admin → todas las activas.
        /// Otros → solo las que tengan asignadas en PermisosCamara y activas.
        /// </summary>
        public static List<Camara> ObtenerCamarasVisibles(int idUsuario, bool esAdmin)
        {
            if (esAdmin) return ObtenerTodasCamaras(false);

            var lista = new List<Camara>();
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT cam.Id, cam.Nombre, cam.Ubicacion, cam.IpLocal, cam.PuertoRtsp,
                       cam.UsuarioCamara, cam.ClaveCifrada, cam.RutaStream, cam.Modelo,
                       cam.Activa, cam.Orden, cam.FechaCreacion
                FROM Camaras cam
                INNER JOIN PermisosCamara p ON p.IdCamara = cam.Id
                WHERE p.IdUsuario = $u AND cam.Activa = 1
                ORDER BY cam.Orden, cam.Nombre";
            cmd.Parameters.AddWithValue("$u", idUsuario);
            using var r = cmd.ExecuteReader();
            while (r.Read()) lista.Add(LeerCamara(r));
            return lista;
        }

        /// <summary>Cuenta de cámaras visibles sin cargar las filas.</summary>
        public static int ContarCamarasVisibles(int idUsuario, bool esAdmin)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = esAdmin
                ? "SELECT COUNT(*) FROM Camaras WHERE Activa = 1"
                : @"SELECT COUNT(*) FROM Camaras cam
                    INNER JOIN PermisosCamara p ON p.IdCamara = cam.Id
                    WHERE p.IdUsuario = $u AND cam.Activa = 1";
            if (!esAdmin) cmd.Parameters.AddWithValue("$u", idUsuario);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public static Camara ObtenerCamaraPorId(int id)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Nombre, Ubicacion, IpLocal, PuertoRtsp, UsuarioCamara,
                       ClaveCifrada, RutaStream, Modelo, Activa, Orden, FechaCreacion
                FROM Camaras WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? LeerCamara(r) : null;
        }

        public static int InsertarCamara(Camara cam)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Camaras (Nombre, Ubicacion, IpLocal, PuertoRtsp,
                    UsuarioCamara, ClaveCifrada, RutaStream, Modelo,
                    Activa, Orden, FechaCreacion)
                VALUES ($n, $u, $ip, $p, $usr, $cv, $rs, $mo, $ac, $or, $fc);
                SELECT last_insert_rowid();";
            BindCamara(cmd, cam);
            cmd.Parameters.AddWithValue("$fc",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            int id = Convert.ToInt32(cmd.ExecuteScalar());
            RegistrarAuditoria("CAMARA_NUEVA", "Camaras",
                $"Cámara '{cam.Nombre}' ({cam.IpLocal})");
            return id;
        }

        public static void ActualizarCamara(Camara cam)
        {
            using var c = Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                UPDATE Camaras SET
                    Nombre=$n, Ubicacion=$u, IpLocal=$ip, PuertoRtsp=$p,
                    UsuarioCamara=$usr, ClaveCifrada=$cv, RutaStream=$rs,
                    Modelo=$mo, Activa=$ac, Orden=$or
                WHERE Id=$id";
            BindCamara(cmd, cam);
            cmd.Parameters.AddWithValue("$id", cam.Id);
            cmd.ExecuteNonQuery();
            RegistrarAuditoria("CAMARA_EDITAR", "Camaras",
                $"Cámara '{cam.Nombre}' (Id={cam.Id})");
        }

        public static void EliminarCamara(int idCamara)
        {
            using var c = Conn();
            using var tx = c.BeginTransaction();
            try
            {
                var del1 = c.CreateCommand();
                del1.Transaction = tx;
                del1.CommandText = "DELETE FROM PermisosCamara WHERE IdCamara=$id";
                del1.Parameters.AddWithValue("$id", idCamara);
                del1.ExecuteNonQuery();

                var del2 = c.CreateCommand();
                del2.Transaction = tx;
                del2.CommandText = "DELETE FROM Camaras WHERE Id=$id";
                del2.Parameters.AddWithValue("$id", idCamara);
                del2.ExecuteNonQuery();

                tx.Commit();
                RegistrarAuditoria("CAMARA_ELIMINAR", "Camaras", $"Id={idCamara}");
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ── Permisos por usuario ─────────────────────────────────────────────

        /// <summary>IDs de cámaras asignadas a un usuario.</summary>
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

        /// <summary>
        /// Reemplaza completamente las cámaras asignadas a un usuario por la
        /// lista provista. Operación atómica (transacción).
        /// </summary>
        public static void GuardarPermisosCamara(int idUsuario, IEnumerable<int> idsCamara)
        {
            var ids = new List<int>(idsCamara);
            using var c = Conn();
            using var tx = c.BeginTransaction();
            try
            {
                var del = c.CreateCommand();
                del.Transaction = tx;
                del.CommandText = "DELETE FROM PermisosCamara WHERE IdUsuario=$u";
                del.Parameters.AddWithValue("$u", idUsuario);
                del.ExecuteNonQuery();

                foreach (var idCam in ids)
                {
                    var ins = c.CreateCommand();
                    ins.Transaction = tx;
                    ins.CommandText =
                        "INSERT INTO PermisosCamara (IdUsuario, IdCamara) VALUES ($u, $c)";
                    ins.Parameters.AddWithValue("$u", idUsuario);
                    ins.Parameters.AddWithValue("$c", idCam);
                    ins.ExecuteNonQuery();
                }

                tx.Commit();
                RegistrarAuditoria("CAMARA_PERMISOS", "PermisosCamara",
                    $"Usuario={idUsuario}, cámaras=[{string.Join(",", ids)}]");
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ── Helpers privados ─────────────────────────────────────────────────

        private static Camara LeerCamara(SqliteDataReader r) => new Camara
        {
            Id            = r.GetInt32(0),
            Nombre        = S(r, 1),
            Ubicacion     = S(r, 2),
            IpLocal       = S(r, 3),
            PuertoRtsp    = r.GetInt32(4),
            UsuarioCamara = S(r, 5),
            ClaveCifrada  = S(r, 6),
            RutaStream    = S(r, 7),
            Modelo        = S(r, 8),
            Activa        = r.GetInt32(9) == 1,
            Orden         = r.GetInt32(10),
            FechaCreacion = DateTime.TryParse(S(r, 11), out var fc) ? fc : DateTime.MinValue
        };

        private static void BindCamara(SqliteCommand cmd, Camara cam)
        {
            cmd.Parameters.AddWithValue("$n",   cam.Nombre        ?? "");
            cmd.Parameters.AddWithValue("$u",   cam.Ubicacion     ?? "");
            cmd.Parameters.AddWithValue("$ip",  cam.IpLocal       ?? "");
            cmd.Parameters.AddWithValue("$p",   cam.PuertoRtsp);
            cmd.Parameters.AddWithValue("$usr", cam.UsuarioCamara ?? "admin");
            cmd.Parameters.AddWithValue("$cv",  cam.ClaveCifrada  ?? "");
            cmd.Parameters.AddWithValue("$rs",  cam.RutaStream    ?? "/Streaming/Channels/101");
            cmd.Parameters.AddWithValue("$mo",  cam.Modelo        ?? "");
            cmd.Parameters.AddWithValue("$ac",  cam.Activa ? 1 : 0);
            cmd.Parameters.AddWithValue("$or",  cam.Orden);
        }
    }
}
