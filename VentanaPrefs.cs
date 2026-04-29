using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace SistemaGestion
{
    public static class VentanaPrefs
    {
        private class Prefs
        {
            public int    Ancho         { get; set; }
            public int    Alto          { get; set; }
            public int    PosX          { get; set; }
            public int    PosY          { get; set; }
            public string AnchoColumnas { get; set; } = "";
        }

        // ── Asegurar que la tabla existe ─────────────────────────────────────
        private static bool _tablaVerificada = false;
        private static void AsegurarTabla(SqliteConnection c)
        {
            if (_tablaVerificada) return;
            var cmd = c.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PreferenciasVentana (
                    NombreUsuario  TEXT NOT NULL,
                    NombreVentana  TEXT NOT NULL,
                    Datos          TEXT NOT NULL,
                    PRIMARY KEY(NombreUsuario, NombreVentana)
                )";
            cmd.ExecuteNonQuery();
            _tablaVerificada = true;
        }

        // ── Guardar ──────────────────────────────────────────────────────────
        public static void Guardar(Form f)
        {
            if (f == null || f.IsDisposed) return;
            if (f.WindowState == FormWindowState.Minimized) return;

            string ventana = "(null)";
            try
            {
                ventana = f.GetType().Name;
                string usuario = Sesion.NombreUsuario;
                if (string.IsNullOrWhiteSpace(usuario)) usuario = "sistema";

                int w = f.Width, h = f.Height, x = f.Left, y = f.Top;
                if (f.WindowState == FormWindowState.Maximized)
                {
                    w = f.RestoreBounds.Width; h = f.RestoreBounds.Height;
                    x = f.RestoreBounds.Left;  y = f.RestoreBounds.Top;
                }
                if (w < 100 || h < 50) return;

                var sb = new System.Text.StringBuilder();
                foreach (var ctrl in f.Controls.Find("dgv", true))
                    if (ctrl is DataGridView grid)
                        foreach (DataGridViewColumn col in grid.Columns)
                            if (col.Visible && col.Width > 5)
                                sb.Append($"{col.Name}:{col.Width};");

                var json = JsonSerializer.Serialize(new Prefs {
                    Ancho = w, Alto = h, PosX = x, PosY = y,
                    AnchoColumnas = sb.ToString()
                });

                Log($"Guardando {ventana} w={w} h={h} x={x} y={y}");  // usuario omitido del log

                using var c = new SqliteConnection(SecurityService.CadenaConexion);
                c.Open();
                AsegurarTabla(c);

                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO PreferenciasVentana (NombreUsuario, NombreVentana, Datos)
                    VALUES ($u, $v, $d)
                    ON CONFLICT(NombreUsuario, NombreVentana)
                    DO UPDATE SET Datos = excluded.Datos";
                cmd.Parameters.AddWithValue("$u", usuario);
                cmd.Parameters.AddWithValue("$v", ventana);
                cmd.Parameters.AddWithValue("$d", json);
                cmd.ExecuteNonQuery();
                Log($"OK guardado {ventana}");
            }
            catch (Exception ex)
            {
                Log($"ERROR Guardar {ventana}: {ex.Message}");
            }
        }

        private static void Log(string msg)
        {
            // ALTA-2 FIX: El log de VentanaPrefs está deshabilitado en producción.
            // Sólo activo en compilaciones DEBUG para no exponer nombres de usuario
            // ni patrones de uso en texto plano en %AppData%.
#if DEBUG
            try
            {
                string logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SistemaGestion", "ventanaprefs_debug.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                // Límite de 500 KB para evitar crecimiento sin control
                var fi = new System.IO.FileInfo(logPath);
                if (fi.Exists && fi.Length > 500_000)
                    System.IO.File.Delete(logPath);
                // ALTA-2 FIX: No incluir nombre de usuario en el mensaje de log
                System.IO.File.AppendAllText(logPath,
                    $"{DateTime.Now:HH:mm:ss.fff} {msg}\n");
            }
            catch { }
#endif
        }

        // ── Restaurar ────────────────────────────────────────────────────────
        // Aplica las prefs AHORA (no espera evento Load). Funciona tanto si se
        // llama desde el constructor del form como desde dentro de OnLoad.
        public static void Restaurar(Form f)
        {
            if (f == null) return;

            Prefs prefs = null;
            try { prefs = Leer(f); } catch { }

            if (prefs != null && prefs.Ancho > 200 && prefs.Alto > 100)
            {
                f.StartPosition = FormStartPosition.Manual;
                AplicarTamanoPosicion(f, prefs);
            }

            // Guardar al cerrar
            f.FormClosing += (s, e) => Guardar(f);

            // Debounce 800ms al mover o redimensionar
            var vpt = new System.Windows.Forms.Timer { Interval = 800 };
            vpt.Tick          += (s, e) => { vpt.Stop(); Guardar(f); };
            f.SizeChanged     += (s, e) => { if (!f.IsDisposed) { vpt.Stop(); vpt.Start(); } };
            f.LocationChanged += (s, e) => { if (!f.IsDisposed) { vpt.Stop(); vpt.Start(); } };
            f.FormClosed      += (s, e) => { vpt.Stop(); vpt.Dispose(); };
        }

        private static void AplicarTamanoPosicion(Form f, Prefs p)
        {
            try
            {
                f.Width  = p.Ancho;
                f.Height = p.Alto;
                var area = Screen.GetWorkingArea(
                    new System.Drawing.Point(p.PosX, p.PosY));
                f.Left = Math.Max(area.Left, Math.Min(p.PosX, area.Right  - f.Width));
                f.Top  = Math.Max(area.Top,  Math.Min(p.PosY, area.Bottom - f.Height));

                // Aplicar anchos de columna con timer (después de que se rendericen)
                if (!string.IsNullOrWhiteSpace(p.AnchoColumnas))
                {
                    var mapa  = ParsearAnchos(p.AnchoColumnas);
                    var timer = new System.Windows.Forms.Timer { Interval = 50 };
                    timer.Tick += (s2, e2) => {
                        timer.Stop(); timer.Dispose();
                        foreach (var ctrl in f.Controls.Find("dgv", true))
                            if (ctrl is DataGridView dgv) AplicarAnchosDgv(dgv, mapa);
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                Log("ERROR AplicarTamanoPosicion: " + ex.Message);
            }
        }

        // ── AplicarAnchos ────────────────────────────────────────────────────
        public static void AplicarAnchos(Form f, DataGridView dgv)
        {
            if (dgv == null) return;
            try
            {
                var prefs = Leer(f);
                if (prefs == null || string.IsNullOrWhiteSpace(prefs.AnchoColumnas)) return;
                AplicarAnchosDgv(dgv, ParsearAnchos(prefs.AnchoColumnas));
            }
            catch { }
        }

        public static void AplicarAnchos(Form f, Dictionary<string, int> mapa)
        {
            if (mapa == null || mapa.Count == 0) return;
            foreach (var ctrl in f.Controls.Find("dgv", true))
                if (ctrl is DataGridView dgv) AplicarAnchosDgv(dgv, mapa);
        }

        private static void AplicarAnchosDgv(DataGridView dgv, Dictionary<string, int> mapa)
        {
            try
            {
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                foreach (DataGridViewColumn col in dgv.Columns)
                    if (mapa.TryGetValue(col.Name, out int w) && w > 5)
                        col.Width = w;
            }
            catch { }
        }

        public static Dictionary<string, int> ParsearAnchos(string raw)
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw)) return d;
            foreach (var par in raw.Split(';'))
            {
                var kv = par.Split(':');
                if (kv.Length == 2 && int.TryParse(kv[1], out int w) && w > 5)
                    d[kv[0]] = w;
            }
            return d;
        }

        private static Prefs Leer(Form f)
        {
            try
            {
                string ventana = f.GetType().Name;
                string usuario = Sesion.NombreUsuario;
                if (string.IsNullOrWhiteSpace(usuario)) usuario = "sistema";

                using var c = new SqliteConnection(SecurityService.CadenaConexion);
                c.Open();
                AsegurarTabla(c);

                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT Datos FROM PreferenciasVentana
                    WHERE NombreUsuario = $u AND NombreVentana = $v
                    LIMIT 1";
                cmd.Parameters.AddWithValue("$u", usuario);
                cmd.Parameters.AddWithValue("$v", ventana);

                var obj = cmd.ExecuteScalar();
                if (obj == null || obj == DBNull.Value) return null;
                return JsonSerializer.Deserialize<Prefs>(obj.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VentanaPrefs.Leer] {f?.GetType().Name}: {ex.Message}");
                return null;
            }
        }
    }
}
