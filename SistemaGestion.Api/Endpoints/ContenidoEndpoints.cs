using Microsoft.Data.Sqlite;
using SistemaGestion.Api.Auth;

// ══════════════════════════════════════════════════════════════════════════
//  ContenidoEndpoints — Estudio de Contenido (redes sociales)
//  Fase 1: CRUD de Marcas, Cuentas, Ideas y Posts + transiciones de estado.
//  Auth: JWT (cookie sg_token). Lectura = cualquier usuario autenticado.
//        Escritura = Marketing/Admin. Aprobación = Admin.
//  La generación con IA (n8n) y la publicación llegan en fases posteriores.
// ══════════════════════════════════════════════════════════════════════════
namespace SistemaGestion.Api.Endpoints
{
    public static class ContenidoEndpoints
    {
        // Estados válidos y transiciones permitidas.
        private static readonly Dictionary<string, string[]> Transiciones = new()
        {
            ["generando"]   = new[] { "borrador", "en_revision", "error" },
            ["borrador"]    = new[] { "en_revision", "archivado" },
            ["en_revision"] = new[] { "aprobado", "borrador", "archivado" },
            ["aprobado"]    = new[] { "programado", "publicado", "borrador", "archivado" },
            ["programado"]  = new[] { "publicado", "aprobado", "archivado" },
            ["publicado"]   = new[] { "archivado" },
            ["error"]       = new[] { "borrador", "archivado" },
            ["archivado"]   = new[] { "borrador" },
        };

        public static void MapContenidoEndpoints(this WebApplication app)
        {
            // ════════════════════════ MARCAS ════════════════════════

            app.MapGet("/api/contenido/marcas", () =>
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Nombre, Handle, Idiomas, VozTono, GuiaEstilo, Paleta,
                           LogoRuta, HashtagsBase, Pilares, Activa, FechaCreacion
                    FROM Marcas ORDER BY Activa DESC, Nombre";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read()) lista.Add(LeerMarca(r));
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapGet("/api/contenido/marcas/{id:int}", (int id) =>
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Nombre, Handle, Idiomas, VozTono, GuiaEstilo, Paleta,
                           LogoRuta, HashtagsBase, Pilares, Activa, FechaCreacion
                    FROM Marcas WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                using var r = cmd.ExecuteReader();
                return r.Read() ? Results.Ok(LeerMarca(r))
                                : Results.NotFound(new { error = "Marca no encontrada." });
            }).RequireAuthorization();

            app.MapPost("/api/contenido/marcas", (HttpContext ctx, ContMarcaReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (string.IsNullOrWhiteSpace(req.Nombre))
                    return Results.BadRequest(new { error = "Nombre requerido." });

                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Marcas (Nombre, Handle, Idiomas, VozTono, GuiaEstilo, Paleta,
                                        LogoRuta, HashtagsBase, Pilares, Activa)
                    VALUES ($n, $h, $i, $v, $g, $pa, $l, $ht, $pi, $ac);
                    SELECT last_insert_rowid();";
                Bind(cmd, "$n", req.Nombre);
                Bind(cmd, "$h", req.Handle);
                Bind(cmd, "$i", string.IsNullOrWhiteSpace(req.Idiomas) ? "es" : req.Idiomas);
                Bind(cmd, "$v", req.VozTono);
                Bind(cmd, "$g", req.GuiaEstilo);
                Bind(cmd, "$pa", req.Paleta);
                Bind(cmd, "$l", req.LogoRuta);
                Bind(cmd, "$ht", req.HashtagsBase);
                Bind(cmd, "$pi", req.Pilares);
                cmd.Parameters.AddWithValue("$ac", req.Activa ?? true ? 1 : 0);
                var id = Convert.ToInt64(cmd.ExecuteScalar());
                return Results.Ok(new { id });
            }).RequireAuthorization();

            app.MapPut("/api/contenido/marcas/{id:int}", (int id, HttpContext ctx, ContMarcaReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Marcas SET
                        Nombre = $n, Handle = $h, Idiomas = $i, VozTono = $v,
                        GuiaEstilo = $g, Paleta = $pa, LogoRuta = $l,
                        HashtagsBase = $ht, Pilares = $pi, Activa = $ac
                    WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                Bind(cmd, "$n", req.Nombre);
                Bind(cmd, "$h", req.Handle);
                Bind(cmd, "$i", string.IsNullOrWhiteSpace(req.Idiomas) ? "es" : req.Idiomas);
                Bind(cmd, "$v", req.VozTono);
                Bind(cmd, "$g", req.GuiaEstilo);
                Bind(cmd, "$pa", req.Paleta);
                Bind(cmd, "$l", req.LogoRuta);
                Bind(cmd, "$ht", req.HashtagsBase);
                Bind(cmd, "$pi", req.Pilares);
                cmd.Parameters.AddWithValue("$ac", req.Activa ?? true ? 1 : 0);
                return cmd.ExecuteNonQuery() > 0
                    ? Results.Ok(new { ok = true })
                    : Results.NotFound(new { error = "Marca no encontrada." });
            }).RequireAuthorization();

            // ════════════════════════ CUENTAS DE RED ════════════════════════

            app.MapGet("/api/contenido/marcas/{id:int}/cuentas", (int id) =>
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, MarcaId, Plataforma, NombreCuenta, IgUserId, PageId,
                           ModoPublicacion, Estado, Activa
                    FROM CuentasRed WHERE MarcaId = $id ORDER BY Plataforma";
                cmd.Parameters.AddWithValue("$id", id);
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new
                    {
                        id = Db.I(r, 0), marcaId = Db.I(r, 1), plataforma = Db.S(r, 2),
                        nombreCuenta = Db.S(r, 3), igUserId = Db.S(r, 4), pageId = Db.S(r, 5),
                        modoPublicacion = Db.S(r, 6), estado = Db.S(r, 7), activa = Db.I(r, 8) == 1
                    });
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapPost("/api/contenido/cuentas", (HttpContext ctx, ContCuentaReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (req.MarcaId <= 0 || string.IsNullOrWhiteSpace(req.Plataforma))
                    return Results.BadRequest(new { error = "MarcaId y plataforma requeridos." });

                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO CuentasRed (MarcaId, Plataforma, NombreCuenta, IgUserId, PageId,
                                            TokenRef, ModoPublicacion, Estado, Activa)
                    VALUES ($m, $p, $nc, $ig, $pg, $tr, $mo, 'desconectada', 1);
                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("$m", req.MarcaId);
                Bind(cmd, "$p", req.Plataforma);
                Bind(cmd, "$nc", req.NombreCuenta);
                Bind(cmd, "$ig", req.IgUserId);
                Bind(cmd, "$pg", req.PageId);
                Bind(cmd, "$tr", req.TokenRef);
                Bind(cmd, "$mo", string.IsNullOrWhiteSpace(req.ModoPublicacion) ? "asistida" : req.ModoPublicacion);
                var id = Convert.ToInt64(cmd.ExecuteScalar());
                return Results.Ok(new { id });
            }).RequireAuthorization();

            // ════════════════════════ IDEAS ════════════════════════

            app.MapGet("/api/contenido/ideas", (int? marca, string? estado, string? pilar) =>
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                var where = new List<string>();
                if (marca.HasValue)                 { where.Add("MarcaId = $m"); cmd.Parameters.AddWithValue("$m", marca.Value); }
                if (!string.IsNullOrWhiteSpace(estado)) { where.Add("Estado = $e"); cmd.Parameters.AddWithValue("$e", estado); }
                if (!string.IsNullOrWhiteSpace(pilar))  { where.Add("Pilar = $p");  cmd.Parameters.AddWithValue("$p", pilar); }
                var w = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
                cmd.CommandText = $@"
                    SELECT Id, MarcaId, Titulo, Descripcion, Pilar, Origen, Estado,
                           RefBaseUrl, FechaCreacion, CreadoPor
                    FROM IdeasContenido{w} ORDER BY FechaCreacion DESC LIMIT 500";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new
                    {
                        id = Db.I(r, 0), marcaId = Db.I(r, 1), titulo = Db.S(r, 2),
                        descripcion = Db.S(r, 3), pilar = Db.S(r, 4), origen = Db.S(r, 5),
                        estado = Db.S(r, 6), refBaseUrl = Db.S(r, 7), fechaCreacion = Db.S(r, 8),
                        creadoPor = Db.I(r, 9)
                    });
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapPost("/api/contenido/ideas", (HttpContext ctx, ContIdeaReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (req.MarcaId <= 0 || string.IsNullOrWhiteSpace(req.Titulo))
                    return Results.BadRequest(new { error = "MarcaId y titulo requeridos." });

                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO IdeasContenido (MarcaId, Titulo, Descripcion, Pilar, Origen, Estado, CreadoPor)
                    VALUES ($m, $t, $d, $p, $o, 'nueva', $u);
                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("$m", req.MarcaId);
                Bind(cmd, "$t", req.Titulo);
                Bind(cmd, "$d", req.Descripcion);
                Bind(cmd, "$p", req.Pilar);
                Bind(cmd, "$o", string.IsNullOrWhiteSpace(req.Origen) ? "manual" : req.Origen);
                cmd.Parameters.AddWithValue("$u", TokenService.ObtenerIdUsuario(ctx.User));
                var id = Convert.ToInt64(cmd.ExecuteScalar());
                return Results.Ok(new { id });
            }).RequireAuthorization();

            app.MapPut("/api/contenido/ideas/{id:int}", (int id, HttpContext ctx, ContIdeaReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    UPDATE IdeasContenido SET
                        Titulo = COALESCE($t, Titulo),
                        Descripcion = COALESCE($d, Descripcion),
                        Pilar = COALESCE($p, Pilar),
                        Estado = COALESCE($e, Estado)
                    WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                Bind(cmd, "$t", req.Titulo);
                Bind(cmd, "$d", req.Descripcion);
                Bind(cmd, "$p", req.Pilar);
                Bind(cmd, "$e", req.Estado);
                return cmd.ExecuteNonQuery() > 0
                    ? Results.Ok(new { ok = true })
                    : Results.NotFound(new { error = "Idea no encontrada." });
            }).RequireAuthorization();

            // ════════════════════════ POSTS ════════════════════════

            app.MapGet("/api/contenido/posts", (int? marca, string? estado, string? desde, string? hasta) =>
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                var where = new List<string>();
                if (marca.HasValue)                     { where.Add("p.MarcaId = $m"); cmd.Parameters.AddWithValue("$m", marca.Value); }
                if (!string.IsNullOrWhiteSpace(estado)) { where.Add("p.Estado = $e"); cmd.Parameters.AddWithValue("$e", estado); }
                if (!string.IsNullOrWhiteSpace(desde))  { where.Add("p.FechaProgramada >= $d"); cmd.Parameters.AddWithValue("$d", desde); }
                if (!string.IsNullOrWhiteSpace(hasta))  { where.Add("p.FechaProgramada <= $h"); cmd.Parameters.AddWithValue("$h", hasta); }
                var w = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
                cmd.CommandText = $@"
                    SELECT p.Id, p.MarcaId, m.Nombre, p.IdeaId, p.Formato, p.Titulo,
                           p.CaptionEs, p.CaptionEn, p.Hashtags, p.PlataformasDestino,
                           p.Estado, p.FechaProgramada, p.FechaCreacion,
                           (SELECT COUNT(*) FROM PostAssets a WHERE a.PostId = p.Id) AS NumAssets
                    FROM Posts p LEFT JOIN Marcas m ON m.Id = p.MarcaId{w}
                    ORDER BY COALESCE(p.FechaProgramada, p.FechaCreacion) DESC LIMIT 500";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read()) lista.Add(LeerPostResumen(r));
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapGet("/api/contenido/posts/{id:int}", (int id) =>
            {
                using var c = Db.Conn();
                object? post = null;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT p.Id, p.MarcaId, m.Nombre, p.IdeaId, p.Formato, p.Titulo,
                               p.CaptionEs, p.CaptionEn, p.Hashtags, p.PromptImagen, p.ModeloUsado,
                               p.PlataformasDestino, p.PrimerComentarioHashtags, p.Estado,
                               p.FechaProgramada, p.CreadoPor, p.AprobadoPor, p.FechaAprobacion,
                               p.FechaCreacion
                        FROM Posts p LEFT JOIN Marcas m ON m.Id = p.MarcaId
                        WHERE p.Id = $id";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    if (r.Read()) post = LeerPostDetalle(r);
                }
                if (post is null) return Results.NotFound(new { error = "Post no encontrado." });

                var assets = new List<object>();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Orden, Tipo, UrlPublica, Origen, ModeloGen, EsFinal
                        FROM PostAssets WHERE PostId = $id ORDER BY Orden";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        assets.Add(new
                        {
                            id = Db.I(r, 0), orden = Db.I(r, 1), tipo = Db.S(r, 2),
                            urlPublica = Db.S(r, 3), origen = Db.S(r, 4),
                            modeloGen = Db.S(r, 5), esFinal = Db.I(r, 6) == 1
                        });
                }
                return Results.Ok(new { post, assets });
            }).RequireAuthorization();

            app.MapPost("/api/contenido/posts", (HttpContext ctx, ContPostCrearReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (req.MarcaId <= 0)
                    return Results.BadRequest(new { error = "MarcaId requerido." });

                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Posts (MarcaId, IdeaId, Formato, Titulo, PlataformasDestino, Estado, CreadoPor)
                    VALUES ($m, $idea, $f, $t, $pl, 'borrador', $u);
                    SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("$m", req.MarcaId);
                if (req.IdeaId.HasValue) cmd.Parameters.AddWithValue("$idea", req.IdeaId.Value);
                else                     cmd.Parameters.AddWithValue("$idea", DBNull.Value);
                Bind(cmd, "$f", string.IsNullOrWhiteSpace(req.Formato) ? "imagen" : req.Formato);
                Bind(cmd, "$t", req.Titulo);
                Bind(cmd, "$pl", string.IsNullOrWhiteSpace(req.PlataformasDestino) ? "instagram" : req.PlataformasDestino);
                cmd.Parameters.AddWithValue("$u", TokenService.ObtenerIdUsuario(ctx.User));
                var id = Convert.ToInt64(cmd.ExecuteScalar());

                // Marcar la idea como en proceso si aplica
                if (req.IdeaId.HasValue)
                {
                    using var upd = c.CreateCommand();
                    upd.CommandText = "UPDATE IdeasContenido SET Estado = 'en_proceso' WHERE Id = $i AND Estado = 'nueva'";
                    upd.Parameters.AddWithValue("$i", req.IdeaId.Value);
                    upd.ExecuteNonQuery();
                }
                return Results.Ok(new { id });
            }).RequireAuthorization();

            app.MapPut("/api/contenido/posts/{id:int}", (int id, HttpContext ctx, ContPostEditarReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Posts SET
                        Titulo = COALESCE($t, Titulo),
                        Formato = COALESCE($f, Formato),
                        CaptionEs = COALESCE($ces, CaptionEs),
                        CaptionEn = COALESCE($cen, CaptionEn),
                        Hashtags = COALESCE($ht, Hashtags),
                        PromptImagen = COALESCE($pi, PromptImagen),
                        PlataformasDestino = COALESCE($pl, PlataformasDestino),
                        PrimerComentarioHashtags = COALESCE($pc, PrimerComentarioHashtags),
                        FechaProgramada = COALESCE($fp, FechaProgramada),
                        FechaActualizacion = datetime('now','localtime')
                    WHERE Id = $id";
                cmd.Parameters.AddWithValue("$id", id);
                Bind(cmd, "$t", req.Titulo);
                Bind(cmd, "$f", req.Formato);
                Bind(cmd, "$ces", req.CaptionEs);
                Bind(cmd, "$cen", req.CaptionEn);
                Bind(cmd, "$ht", req.Hashtags);
                Bind(cmd, "$pi", req.PromptImagen);
                Bind(cmd, "$pl", req.PlataformasDestino);
                if (req.PrimerComentarioHashtags.HasValue)
                    cmd.Parameters.AddWithValue("$pc", req.PrimerComentarioHashtags.Value ? 1 : 0);
                else cmd.Parameters.AddWithValue("$pc", DBNull.Value);
                Bind(cmd, "$fp", req.FechaProgramada);
                return cmd.ExecuteNonQuery() > 0
                    ? Results.Ok(new { ok = true })
                    : Results.NotFound(new { error = "Post no encontrado." });
            }).RequireAuthorization();

            // Cambio de estado genérico (valida transición y rol)
            app.MapPut("/api/contenido/posts/{id:int}/estado", (int id, HttpContext ctx, ContEstadoReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                var nuevo = (req.Estado ?? "").Trim().ToLowerInvariant();
                if (!Transiciones.ContainsKey(nuevo) && nuevo != "en_revision")
                    return Results.BadRequest(new { error = $"Estado inválido: {nuevo}" });

                // Aprobar/publicar exige rol de aprobación
                if ((nuevo == "aprobado" || nuevo == "programado" || nuevo == "publicado")
                    && !TokenService.PuedeAprobarContenido(ctx.User))
                    return Results.Forbid();

                using var c = Db.Conn();
                var actual = EstadoActual(c, id);
                if (actual is null) return Results.NotFound(new { error = "Post no encontrado." });
                if (!EsTransicionValida(actual, nuevo))
                    return Results.BadRequest(new { error = $"Transición no permitida: {actual} → {nuevo}" });

                using var cmd = c.CreateCommand();
                cmd.CommandText = "UPDATE Posts SET Estado = $e, FechaActualizacion = datetime('now','localtime') WHERE Id = $id";
                cmd.Parameters.AddWithValue("$e", nuevo);
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true, estado = nuevo });
            }).RequireAuthorization();

            // Enviar a revisión (valida contenido mínimo)
            app.MapPost("/api/contenido/posts/{id:int}/enviar-revision", (int id, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                // Validaciones mínimas: caption ES no vacío; EN si la marca incluye 'en'
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT p.Estado, COALESCE(p.CaptionEs,''), COALESCE(p.CaptionEn,''),
                               COALESCE(m.Idiomas,'es'),
                               (SELECT COUNT(*) FROM PostAssets a WHERE a.PostId = p.Id AND a.EsFinal = 1)
                        FROM Posts p LEFT JOIN Marcas m ON m.Id = p.MarcaId WHERE p.Id = $id";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) return Results.NotFound(new { error = "Post no encontrado." });
                    var estado = Db.S(r, 0);
                    var capEs = Db.S(r, 1);
                    var capEn = Db.S(r, 2);
                    var idiomas = Db.S(r, 3);
                    var numFinales = Db.I2(r, 4);
                    if (!EsTransicionValida(estado, "en_revision"))
                        return Results.BadRequest(new { error = $"No se puede enviar a revisión desde {estado}." });
                    if (string.IsNullOrWhiteSpace(capEs))
                        return Results.BadRequest(new { error = "Falta el caption en español." });
                    if (idiomas.Contains("en") && string.IsNullOrWhiteSpace(capEn))
                        return Results.BadRequest(new { error = "Esta marca requiere caption en inglés." });
                    if (numFinales < 1)
                        return Results.BadRequest(new { error = "El post necesita al menos una imagen/video final." });
                }
                using (var upd = c.CreateCommand())
                {
                    upd.CommandText = "UPDATE Posts SET Estado='en_revision', FechaActualizacion=datetime('now','localtime') WHERE Id=$id";
                    upd.Parameters.AddWithValue("$id", id);
                    upd.ExecuteNonQuery();
                }
                return Results.Ok(new { ok = true, estado = "en_revision" });
            }).RequireAuthorization();

            // Aprobar (registra auditoría; programa si tiene fecha)
            app.MapPost("/api/contenido/posts/{id:int}/aprobar", (int id, HttpContext ctx) =>
            {
                if (!TokenService.PuedeAprobarContenido(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var actual = EstadoActual(c, id);
                if (actual is null) return Results.NotFound(new { error = "Post no encontrado." });
                if (actual != "en_revision" && actual != "borrador")
                    return Results.BadRequest(new { error = $"No se puede aprobar desde {actual}." });

                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Posts SET
                        Estado = CASE WHEN FechaProgramada IS NOT NULL AND FechaProgramada <> ''
                                      THEN 'programado' ELSE 'aprobado' END,
                        AprobadoPor = $u,
                        FechaAprobacion = datetime('now','localtime'),
                        FechaActualizacion = datetime('now','localtime')
                    WHERE Id = $id";
                cmd.Parameters.AddWithValue("$u", TokenService.ObtenerIdUsuario(ctx.User));
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // Rechazar (vuelve a borrador con nota)
            app.MapPost("/api/contenido/posts/{id:int}/rechazar", (int id, HttpContext ctx, ContRechazoReq req) =>
            {
                if (!TokenService.PuedeAprobarContenido(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Posts SET Estado='borrador', MensajeError=$nota,
                        FechaActualizacion=datetime('now','localtime')
                    WHERE Id=$id AND Estado='en_revision'";
                Bind(cmd, "$nota", req.Nota);
                cmd.Parameters.AddWithValue("$id", id);
                return cmd.ExecuteNonQuery() > 0
                    ? Results.Ok(new { ok = true })
                    : Results.BadRequest(new { error = "Solo se rechazan posts en revisión." });
            }).RequireAuthorization();

            // ════════════════════════ CONFIG (Admin) ════════════════════════
            app.MapGet("/api/contenido/config", (HttpContext ctx) =>
            {
                if (!TokenService.EsAdmin(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT Clave, Valor, Descripcion FROM ContenidoConfig ORDER BY Clave";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new { clave = Db.S(r, 0), valor = Db.S(r, 1), descripcion = Db.S(r, 2) });
                return Results.Ok(lista);
            }).RequireAuthorization();
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static void Bind(SqliteCommand cmd, string name, string? val)
            => cmd.Parameters.AddWithValue(name, (object?)val ?? DBNull.Value);

        private static object LeerMarca(SqliteDataReader r) => new
        {
            id = Db.I(r, 0), nombre = Db.S(r, 1), handle = Db.S(r, 2), idiomas = Db.S(r, 3),
            vozTono = Db.S(r, 4), guiaEstilo = Db.S(r, 5), paleta = Db.S(r, 6),
            logoRuta = Db.S(r, 7), hashtagsBase = Db.S(r, 8), pilares = Db.S(r, 9),
            activa = Db.I(r, 10) == 1, fechaCreacion = Db.S(r, 11)
        };

        private static object LeerPostResumen(SqliteDataReader r) => new
        {
            id = Db.I(r, 0), marcaId = Db.I(r, 1), marca = Db.S(r, 2), ideaId = Db.I(r, 3),
            formato = Db.S(r, 4), titulo = Db.S(r, 5), captionEs = Db.S(r, 6),
            captionEn = Db.S(r, 7), hashtags = Db.S(r, 8), plataformasDestino = Db.S(r, 9),
            estado = Db.S(r, 10), fechaProgramada = Db.S(r, 11), fechaCreacion = Db.S(r, 12),
            numAssets = Db.I2(r, 13)
        };

        private static object LeerPostDetalle(SqliteDataReader r) => new
        {
            id = Db.I(r, 0), marcaId = Db.I(r, 1), marca = Db.S(r, 2), ideaId = Db.I(r, 3),
            formato = Db.S(r, 4), titulo = Db.S(r, 5), captionEs = Db.S(r, 6),
            captionEn = Db.S(r, 7), hashtags = Db.S(r, 8), promptImagen = Db.S(r, 9),
            modeloUsado = Db.S(r, 10), plataformasDestino = Db.S(r, 11),
            primerComentarioHashtags = Db.I(r, 12) == 1, estado = Db.S(r, 13),
            fechaProgramada = Db.S(r, 14), creadoPor = Db.I(r, 15), aprobadoPor = Db.I(r, 16),
            fechaAprobacion = Db.S(r, 17), fechaCreacion = Db.S(r, 18)
        };

        private static string? EstadoActual(SqliteConnection c, int id)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Estado FROM Posts WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            var v = cmd.ExecuteScalar();
            return v is null || v is DBNull ? null : v.ToString();
        }

        private static bool EsTransicionValida(string actual, string nuevo)
            => Transiciones.TryGetValue(actual, out var permitidos)
               && Array.IndexOf(permitidos, nuevo) >= 0;
    }

    // ── DTOs (request bodies) ─────────────────────────────────────────────
    record ContMarcaReq(string Nombre, string? Handle, string? Idiomas, string? VozTono,
        string? GuiaEstilo, string? Paleta, string? LogoRuta, string? HashtagsBase,
        string? Pilares, bool? Activa);
    record ContCuentaReq(int MarcaId, string Plataforma, string? NombreCuenta,
        string? IgUserId, string? PageId, string? TokenRef, string? ModoPublicacion);
    record ContIdeaReq(int MarcaId, string? Titulo, string? Descripcion, string? Pilar,
        string? Origen, string? Estado);
    record ContPostCrearReq(int MarcaId, int? IdeaId, string? Formato, string? Titulo,
        string? PlataformasDestino);
    record ContPostEditarReq(string? Titulo, string? Formato, string? CaptionEs, string? CaptionEn,
        string? Hashtags, string? PromptImagen, string? PlataformasDestino,
        bool? PrimerComentarioHashtags, string? FechaProgramada);
    record ContEstadoReq(string Estado);
    record ContRechazoReq(string? Nota);
}
