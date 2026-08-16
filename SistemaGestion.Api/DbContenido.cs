using Microsoft.Data.Sqlite;

namespace SistemaGestion.Api
{
    // ══════════════════════════════════════════════════════════════════════
    //  DbContenido — Estudio de Contenido (módulo de redes sociales)
    //
    //  Sigue el patrón de la API: método Ensure*() idempotente llamado al
    //  arranque (Program.cs), CREATE TABLE IF NOT EXISTS, sin PRAGMA
    //  user_version (ese lo maneja el WinForms). El acceso a datos se hace
    //  con Db.Conn() y los helpers Db.S/D/I desde los endpoints (mismo estilo
    //  que WhatsAppEndpoints).
    //
    //  Tablas: Marcas, CuentasRed, IdeasContenido, PromptsPlantilla,
    //          Posts, PostAssets, Publicaciones, ContenidoConfig.
    // ══════════════════════════════════════════════════════════════════════
    public static class DbContenido
    {
        /// <summary>
        /// Crea las tablas del módulo Estudio de Contenido si no existen y
        /// siembra las dos marcas base. Idempotente. Se llama en el arranque.
        /// </summary>
        public static void EnsureContenido()
        {
            try
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Marcas (
                        Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nombre        TEXT    NOT NULL,
                        Handle        TEXT,
                        Idiomas       TEXT    NOT NULL DEFAULT 'es',
                        VozTono       TEXT,
                        GuiaEstilo    TEXT,
                        Paleta        TEXT,
                        LogoRuta      TEXT,
                        HashtagsBase  TEXT,
                        Pilares       TEXT,
                        Activa        INTEGER NOT NULL DEFAULT 1,
                        FechaCreacion TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
                    );

                    CREATE TABLE IF NOT EXISTS CuentasRed (
                        Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                        MarcaId         INTEGER NOT NULL,
                        Plataforma      TEXT    NOT NULL,
                        NombreCuenta    TEXT,
                        IgUserId        TEXT,
                        PageId          TEXT,
                        TokenRef        TEXT,
                        ModoPublicacion TEXT    NOT NULL DEFAULT 'asistida',
                        Estado          TEXT    NOT NULL DEFAULT 'desconectada',
                        Activa          INTEGER NOT NULL DEFAULT 1,
                        FOREIGN KEY (MarcaId) REFERENCES Marcas(Id)
                    );

                    CREATE TABLE IF NOT EXISTS IdeasContenido (
                        Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                        MarcaId       INTEGER NOT NULL,
                        Titulo        TEXT    NOT NULL,
                        Descripcion   TEXT,
                        Pilar         TEXT,
                        Origen        TEXT    NOT NULL DEFAULT 'manual',
                        Estado        TEXT    NOT NULL DEFAULT 'nueva',
                        RefBaseUrl    TEXT,
                        FechaCreacion TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                        CreadoPor     INTEGER,
                        FOREIGN KEY (MarcaId) REFERENCES Marcas(Id)
                    );

                    CREATE TABLE IF NOT EXISTS PromptsPlantilla (
                        Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                        MarcaId       INTEGER,
                        Nombre        TEXT    NOT NULL,
                        Tipo          TEXT    NOT NULL,
                        Categoria     TEXT,
                        Texto         TEXT    NOT NULL,
                        Veces         INTEGER NOT NULL DEFAULT 0,
                        FechaCreacion TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
                    );

                    CREATE TABLE IF NOT EXISTS Posts (
                        Id                       INTEGER PRIMARY KEY AUTOINCREMENT,
                        MarcaId                  INTEGER NOT NULL,
                        IdeaId                   INTEGER,
                        Formato                  TEXT    NOT NULL DEFAULT 'imagen',
                        Titulo                   TEXT,
                        CaptionEs                TEXT,
                        CaptionEn                TEXT,
                        Hashtags                 TEXT,
                        PromptImagen             TEXT,
                        ModeloUsado              TEXT,
                        PlataformasDestino       TEXT    NOT NULL DEFAULT 'instagram',
                        PrimerComentarioHashtags INTEGER NOT NULL DEFAULT 0,
                        Estado                   TEXT    NOT NULL DEFAULT 'borrador',
                        FechaProgramada          TEXT,
                        JobId                    TEXT,
                        MensajeError             TEXT,
                        CreadoPor                INTEGER,
                        AprobadoPor              INTEGER,
                        FechaAprobacion          TEXT,
                        FechaCreacion            TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                        FechaActualizacion       TEXT,
                        FOREIGN KEY (MarcaId) REFERENCES Marcas(Id)
                    );

                    CREATE TABLE IF NOT EXISTS PostAssets (
                        Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                        PostId        INTEGER NOT NULL,
                        Orden         INTEGER NOT NULL DEFAULT 1,
                        Tipo          TEXT    NOT NULL DEFAULT 'imagen',
                        RutaArchivo   TEXT    NOT NULL,
                        UrlPublica    TEXT,
                        Ancho         INTEGER,
                        Alto          INTEGER,
                        Origen        TEXT    NOT NULL DEFAULT 'ia',
                        ModeloGen     TEXT,
                        PromptGen     TEXT,
                        EsFinal       INTEGER NOT NULL DEFAULT 0,
                        FechaCreacion TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
                        FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS Publicaciones (
                        Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                        PostId           INTEGER NOT NULL,
                        CuentaRedId      INTEGER,
                        Plataforma       TEXT    NOT NULL,
                        Estado           TEXT    NOT NULL DEFAULT 'pendiente',
                        IdExterno        TEXT,
                        Permalink        TEXT,
                        RespuestaApi     TEXT,
                        FechaIntento     TEXT,
                        FechaPublicacion TEXT,
                        FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE
                    );

                    CREATE TABLE IF NOT EXISTS ContenidoConfig (
                        Clave       TEXT PRIMARY KEY,
                        Valor       TEXT NOT NULL,
                        Descripcion TEXT
                    );

                    CREATE INDEX IF NOT EXISTS IX_Posts_Estado     ON Posts(Estado);
                    CREATE INDEX IF NOT EXISTS IX_Posts_Marca      ON Posts(MarcaId);
                    CREATE INDEX IF NOT EXISTS IX_Posts_Programada  ON Posts(FechaProgramada);
                    CREATE INDEX IF NOT EXISTS IX_Ideas_MarcaEstado ON IdeasContenido(MarcaId, Estado);
                    CREATE INDEX IF NOT EXISTS IX_Assets_Post       ON PostAssets(PostId);
                    CREATE INDEX IF NOT EXISTS IX_Pub_Post          ON Publicaciones(PostId);";
                cmd.ExecuteNonQuery();

                SembrarMarcasBase(c);
                SembrarConfigBase(c);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN EnsureContenido: {ex.Message}");
            }
        }

        /// <summary>Siembra las 2 marcas base solo si la tabla está vacía.</summary>
        private static void SembrarMarcasBase(SqliteConnection c)
        {
            using (var chk = c.CreateCommand())
            {
                chk.CommandText = "SELECT COUNT(*) FROM Marcas";
                if (Convert.ToInt64(chk.ExecuteScalar() ?? 0L) > 0) return;
            }

            void Insertar(string nombre, string handle, string idiomas, string voz, string pilares, string hashtags)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Marcas (Nombre, Handle, Idiomas, VozTono, Pilares, HashtagsBase, Activa)
                    VALUES ($n, $h, $i, $v, $p, $ht, 1)";
                cmd.Parameters.AddWithValue("$n", nombre);
                cmd.Parameters.AddWithValue("$h", handle);
                cmd.Parameters.AddWithValue("$i", idiomas);
                cmd.Parameters.AddWithValue("$v", voz);
                cmd.Parameters.AddWithValue("$p", pilares);
                cmd.Parameters.AddWithValue("$ht", hashtags);
                cmd.ExecuteNonQuery();
            }

            const string pilares = "[\"producto\",\"detras\",\"educativo\",\"testimonio\",\"historia\",\"estacional\"]";

            Insertar(
                "EHP Saddles", "ehpsaddles", "es,en",
                "Artesanal y tecnico. Orgullo 'hecho en Venezuela desde 1983'. Transmite confianza y durabilidad. Publico internacional (jinetes, talabarteros). Frases cortas, sin prometer precios.",
                pilares,
                "#EHPSaddles #saddletree #arboldemontura #polietileno #hechoenvenezuela");

            Insertar(
                "El Hato Pascher Plasticos", "hatopascher", "es",
                "Cercano, practico e industrial. Enfoque en servicio, calidad y disponibilidad para clientes locales en Venezuela. Espanol neutro.",
                pilares,
                "#plasticos #villadecura #aragua #industria #hatopascher");
        }

        /// <summary>Siembra la configuración de motores de IA (referencias, no claves).</summary>
        private static void SembrarConfigBase(SqliteConnection c)
        {
            void Seed(string k, string v, string desc)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = "INSERT OR IGNORE INTO ContenidoConfig (Clave, Valor, Descripcion) VALUES ($k, $v, $d)";
                cmd.Parameters.AddWithValue("$k", k);
                cmd.Parameters.AddWithValue("$v", v);
                cmd.Parameters.AddWithValue("$d", desc);
                cmd.ExecuteNonQuery();
            }
            Seed("motor.texto.primario",  "gemini",   "LLM ideas/captions/hashtags — Gemini (cuenta de PAGO disponible)");
            Seed("motor.texto.fallback",  "openai",   "Respaldo de texto — OpenAI (cuenta de PAGO disponible)");
            Seed("motor.imagen.primario", "gemini-nano-banana", "Imagen — Gemini Nano Banana (cuenta de PAGO disponible)");
            Seed("motor.imagen.fallback", "gpt-image", "Respaldo imagen — OpenAI GPT Image 2 (de PAGO)");
            Seed("motor.imagen.libre",    "z-image-turbo", "Opcion gratuita/abierta — Alibaba Z-Image-Turbo");
            Seed("motor.video.primario",  "",         "Video — definir al activar (Seedance/Wan/Kling via API)");
        }
    }
}
