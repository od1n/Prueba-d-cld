using Microsoft.Data.Sqlite;
using SistemaGestion.Api.Auth;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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

            // ════════════════ PUBLICACION ASISTIDA (Fase 4a) ════════════════

            // Descargar un ZIP con las imagenes del post + caption.txt (texto listo para pegar).
            app.MapGet("/api/contenido/posts/{id:int}/paquete.zip", (int id) =>
            {
                using var c = Db.Conn();
                string titulo = "", capEs = "", capEn = "", hashtags = "";
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT COALESCE(Titulo,''), COALESCE(CaptionEs,''), COALESCE(CaptionEn,''), COALESCE(Hashtags,'') FROM Posts WHERE Id=$id";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) return Results.NotFound();
                    titulo = Db.S(r, 0); capEs = Db.S(r, 1); capEn = Db.S(r, 2); hashtags = Db.S(r, 3);
                }
                var rutas = new List<string>();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT RutaArchivo FROM PostAssets WHERE PostId=$id ORDER BY Orden";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) rutas.Add(Db.S(r, 0));
                }
                using var ms = new MemoryStream();
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    var sb = new System.Text.StringBuilder();
                    if (!string.IsNullOrWhiteSpace(titulo)) { sb.AppendLine(titulo); sb.AppendLine(); }
                    sb.AppendLine("== Caption (ES) =="); sb.AppendLine(capEs);
                    if (!string.IsNullOrWhiteSpace(capEn)) { sb.AppendLine(); sb.AppendLine("== Caption (EN) =="); sb.AppendLine(capEn); }
                    sb.AppendLine(); sb.AppendLine("== Hashtags =="); sb.AppendLine(hashtags);
                    var entry = zip.CreateEntry("caption.txt");
                    using (var w = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false))) w.Write(sb.ToString());
                    int n = 0;
                    foreach (var ruta in rutas)
                    {
                        if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta)) continue;
                        n++;
                        var ext = Path.GetExtension(ruta);
                        var e = zip.CreateEntry($"imagen-{n:00}{ext}");
                        using var zs = e.Open();
                        using var fs = File.OpenRead(ruta);
                        fs.CopyTo(zs);
                    }
                }
                return Results.File(ms.ToArray(), "application/zip", $"post-{id}.zip");
            }).RequireAuthorization();

            // Marcar como publicado (registro manual: el operador ya publico en las redes).
            app.MapPost("/api/contenido/posts/{id:int}/marcar-publicado", (int id, HttpContext ctx, ContPublicarReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                string estado = "", plataformas = "";
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT Estado, COALESCE(PlataformasDestino,'instagram') FROM Posts WHERE Id=$id";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) return Results.NotFound(new { error = "Post no encontrado." });
                    estado = Db.S(r, 0); plataformas = Db.S(r, 1);
                }
                if (estado is not ("aprobado" or "programado" or "publicado"))
                    return Results.BadRequest(new { error = $"Solo se marca publicado un post aprobado o programado (actual: {estado})." });

                var lista = (req.Plataformas != null && req.Plataformas.Length > 0)
                    ? req.Plataformas
                    : plataformas.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var pl in lista)
                {
                    using var ins = c.CreateCommand();
                    ins.CommandText = @"INSERT INTO Publicaciones (PostId, Plataforma, Estado, Permalink, FechaPublicacion)
                                        VALUES ($p, $pl, 'publicado', $link, datetime('now','localtime'))";
                    ins.Parameters.AddWithValue("$p", id);
                    ins.Parameters.AddWithValue("$pl", pl);
                    ins.Parameters.AddWithValue("$link", (object?)req.Permalink ?? DBNull.Value);
                    ins.ExecuteNonQuery();
                }
                using (var upd = c.CreateCommand())
                {
                    upd.CommandText = "UPDATE Posts SET Estado='publicado', FechaActualizacion=datetime('now','localtime') WHERE Id=$id";
                    upd.Parameters.AddWithValue("$id", id);
                    upd.ExecuteNonQuery();
                }
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // ════════════════ PUBLICACION AUTOMATICA (Fase 4b — Meta Graph API) ════════════════

            // Imagen PUBLICA por token (sin login) para que Meta pueda descargarla al publicar.
            app.MapGet("/api/contenido/pub/{token}/{idx:int}", (string token, int idx) =>
            {
                if (string.IsNullOrWhiteSpace(token) || token.Length < 16) return Results.NotFound();
                using var c = Db.Conn();
                int postId;
                using (var q = c.CreateCommand())
                {
                    q.CommandText = "SELECT Id FROM Posts WHERE PublicToken=$t";
                    q.Parameters.AddWithValue("$t", token);
                    var v = q.ExecuteScalar();
                    if (v is null || v is DBNull) return Results.NotFound();
                    postId = Convert.ToInt32(v);
                }
                string ruta;
                using (var q = c.CreateCommand())
                {
                    q.CommandText = "SELECT RutaArchivo FROM PostAssets WHERE PostId=$p ORDER BY Orden LIMIT 1 OFFSET $o";
                    q.Parameters.AddWithValue("$p", postId);
                    q.Parameters.AddWithValue("$o", idx);
                    ruta = q.ExecuteScalar()?.ToString() ?? "";
                }
                if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta)) return Results.NotFound();
                var ext = Path.GetExtension(ruta).ToLowerInvariant();
                var mime = ext is ".jpg" or ".jpeg" ? "image/jpeg" : ext == ".png" ? "image/png" : "application/octet-stream";
                return Results.File(File.ReadAllBytes(ruta), mime);
            });

            // Configurar una cuenta de red (Page/IG/token) — solo Admin.
            app.MapPut("/api/contenido/cuentas/{id:int}", (int id, HttpContext ctx, ContCuentaEditarReq req) =>
            {
                if (!TokenService.EsAdmin(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"UPDATE CuentasRed SET
                        NombreCuenta = COALESCE($nc, NombreCuenta),
                        IgUserId     = COALESCE($ig, IgUserId),
                        PageId       = COALESCE($pg, PageId),
                        TokenRef     = COALESCE($tk, TokenRef),
                        ModoPublicacion = COALESCE($mo, ModoPublicacion),
                        Estado = CASE WHEN TRIM(COALESCE($tk, TokenRef, '')) <> '' THEN 'conectada' ELSE Estado END
                    WHERE Id = $id";
                Bind(cmd, "$nc", req.NombreCuenta);
                Bind(cmd, "$ig", req.IgUserId);
                Bind(cmd, "$pg", req.PageId);
                Bind(cmd, "$tk", req.Token);
                Bind(cmd, "$mo", req.ModoPublicacion);
                cmd.Parameters.AddWithValue("$id", id);
                return cmd.ExecuteNonQuery() > 0
                    ? Results.Ok(new { ok = true })
                    : Results.NotFound(new { error = "Cuenta no encontrada." });
            }).RequireAuthorization();

            // Publicar AHORA en las redes via Graph API.
            app.MapPost("/api/contenido/posts/{id:int}/publicar", async (int id, HttpContext ctx, ContPublicarAutoReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                int marcaId; string estado, capEs, hashtags;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = @"SELECT MarcaId, Estado, COALESCE(CaptionEs,''), COALESCE(Hashtags,'')
                                        FROM Posts WHERE Id=$id";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) return Results.NotFound(new { error = "Post no encontrado." });
                    marcaId = Db.I(r, 0); estado = Db.S(r, 1); capEs = Db.S(r, 2); hashtags = Db.S(r, 3);
                }
                if (estado is not ("aprobado" or "programado" or "publicado"))
                    return Results.BadRequest(new { error = $"Solo se publica un post aprobado (actual: {estado})." });

                int nAssets;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM PostAssets WHERE PostId=$id";
                    cmd.Parameters.AddWithValue("$id", id);
                    nAssets = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                }
                if (nAssets == 0) return Results.BadRequest(new { error = "El post no tiene imagen para publicar." });

                var pubToken = AsegurarPublicToken(c, id);
                var baseUrl = CallbackUrl("");
                var urls = Enumerable.Range(0, nAssets).Select(i => $"{baseUrl}/api/contenido/pub/{pubToken}/{i}").ToList();
                var caption = string.IsNullOrWhiteSpace(hashtags) ? capEs : (capEs + "\n\n" + hashtags);

                var plataformas = (req.Plataformas != null && req.Plataformas.Length > 0)
                    ? req.Plataformas : new[] { "instagram" };
                var resultados = new List<object>();
                bool algunaOk = false;
                foreach (var plRaw in plataformas)
                {
                    var pl = plRaw.Trim().ToLowerInvariant();
                    var (igId, pageId, tkAcc) = LeerCuenta(c, marcaId, pl);
                    if (string.IsNullOrWhiteSpace(tkAcc))
                    { resultados.Add(new { plataforma = pl, ok = false, error = "Falta configurar la cuenta y el token en Cuentas." }); continue; }
                    try
                    {
                        var (extId, permalink) = pl == "facebook"
                            ? await PublicarFacebook(pageId, tkAcc, urls, caption)
                            : await PublicarInstagram(igId, tkAcc, urls, caption);
                        RegistrarPublicacion(c, id, pl, extId, permalink);
                        resultados.Add(new { plataforma = pl, ok = true, permalink });
                        algunaOk = true;
                    }
                    catch (Exception ex)
                    { resultados.Add(new { plataforma = pl, ok = false, error = ex.Message }); }
                }
                if (algunaOk)
                {
                    using var upd = c.CreateCommand();
                    upd.CommandText = "UPDATE Posts SET Estado='publicado', FechaActualizacion=datetime('now','localtime') WHERE Id=$id";
                    upd.Parameters.AddWithValue("$id", id);
                    upd.ExecuteNonQuery();
                }
                return Results.Ok(new { ok = algunaOk, resultados });
            }).RequireAuthorization();

            // ════════════════════════ MÉTRICAS (Insights) ════════════════════════
            // Métricas de un post publicado (por plataforma). Requiere token de la cuenta.
            app.MapGet("/api/contenido/posts/{id:int}/metricas", async (int id, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                int marcaId;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT MarcaId FROM Posts WHERE Id=$id";
                    cmd.Parameters.AddWithValue("$id", id);
                    var o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return Results.NotFound(new { error = "Post no encontrado." });
                    marcaId = Convert.ToInt32(o);
                }
                var pubs = new List<(string pl, string ext, string link)>();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT Plataforma, COALESCE(IdExterno,''), COALESCE(Permalink,'') FROM Publicaciones WHERE PostId=$id AND Estado='publicado'";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) pubs.Add((Db.S(r, 0), Db.S(r, 1), Db.S(r, 2)));
                }
                var salida = new List<object>();
                foreach (var (pl, ext, link) in pubs)
                {
                    var pll = pl.ToLowerInvariant();
                    var (igId, pageId, tk) = LeerCuenta(c, marcaId, pll);
                    if (string.IsNullOrWhiteSpace(tk))
                    { salida.Add(new { plataforma = pll, idExterno = ext, permalink = link, ok = false, error = "Falta el token de la cuenta (pendiente de verificación de Meta)." }); continue; }
                    if (string.IsNullOrWhiteSpace(ext))
                    { salida.Add(new { plataforma = pll, idExterno = ext, permalink = link, ok = false, error = "Sin ID del post. Vincúlalo con el enlace publicado." }); continue; }
                    try
                    {
                        object m = pll == "facebook" ? await LeerMetricasFB(tk, ext) : await LeerMetricasIG(tk, ext);
                        salida.Add(new { plataforma = pll, idExterno = ext, permalink = link, ok = true, metricas = m });
                    }
                    catch (Exception ex)
                    { salida.Add(new { plataforma = pll, idExterno = ext, permalink = link, ok = false, error = ex.Message }); }
                }
                return Results.Ok(new { postId = id, plataformas = salida });
            }).RequireAuthorization();

            // Lista los últimos medios de la cuenta (para vincular posts publicados a mano)
            app.MapGet("/api/contenido/marcas/{marcaId:int}/medios-recientes", async (int marcaId, string? plataforma, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                var pl = (plataforma ?? "instagram").ToLowerInvariant();
                using var c = Db.Conn();
                var (igId, pageId, tk) = LeerCuenta(c, marcaId, pl);
                if (string.IsNullOrWhiteSpace(tk)) return Results.BadRequest(new { error = "Falta el token de la cuenta (pendiente de verificación de Meta)." });
                try
                {
                    var lista = new List<object>();
                    if (pl == "facebook")
                    {
                        var el = await GraphGet($"{pageId}/published_posts", "fields=id,message,created_time,permalink_url,full_picture&limit=25", tk);
                        if (el.TryGetProperty("data", out var arr))
                            foreach (var m in arr.EnumerateArray())
                                lista.Add(new
                                {
                                    id = m.TryGetProperty("id", out var i) ? i.GetString() : "",
                                    texto = m.TryGetProperty("message", out var msg) ? msg.GetString() : "",
                                    fecha = m.TryGetProperty("created_time", out var ct) ? ct.GetString() : "",
                                    enlace = m.TryGetProperty("permalink_url", out var pu) ? pu.GetString() : "",
                                    imagen = m.TryGetProperty("full_picture", out var fp) ? fp.GetString() : ""
                                });
                    }
                    else
                    {
                        var el = await GraphGet($"{igId}/media", "fields=id,caption,media_type,permalink,timestamp,thumbnail_url,media_url&limit=25", tk);
                        if (el.TryGetProperty("data", out var arr))
                            foreach (var m in arr.EnumerateArray())
                                lista.Add(new
                                {
                                    id = m.TryGetProperty("id", out var i) ? i.GetString() : "",
                                    texto = m.TryGetProperty("caption", out var cap) ? cap.GetString() : "",
                                    fecha = m.TryGetProperty("timestamp", out var ts) ? ts.GetString() : "",
                                    enlace = m.TryGetProperty("permalink", out var pk) ? pk.GetString() : "",
                                    imagen = m.TryGetProperty("thumbnail_url", out var th) ? th.GetString()
                                           : (m.TryGetProperty("media_url", out var mu) ? mu.GetString() : "")
                                });
                    }
                    return Results.Ok(lista);
                }
                catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
            }).RequireAuthorization();

            // Vincula un post publicado a mano con su ID externo (para poder seguir sus métricas)
            app.MapPost("/api/contenido/posts/{id:int}/vincular", (int id, HttpContext ctx, ContVincularReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (req == null || string.IsNullOrWhiteSpace(req.Plataforma) || string.IsNullOrWhiteSpace(req.IdExterno))
                    return Results.BadRequest(new { error = "Falta plataforma o ID del post." });
                using var c = Db.Conn();
                RegistrarPublicacion(c, id, req.Plataforma.ToLowerInvariant(), req.IdExterno.Trim(), (req.Enlace ?? "").Trim());
                using (var upd = c.CreateCommand())
                {
                    upd.CommandText = "UPDATE Posts SET Estado='publicado', FechaActualizacion=datetime('now','localtime') WHERE Id=$id AND Estado<>'publicado'";
                    upd.Parameters.AddWithValue("$id", id);
                    upd.ExecuteNonQuery();
                }
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // Resumen de la cuenta (seguidores, nº de publicaciones)
            app.MapGet("/api/contenido/marcas/{marcaId:int}/metricas-cuenta", async (int marcaId, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var res = new List<object>();
                foreach (var pl in new[] { "instagram", "facebook" })
                {
                    var (igId, pageId, tk) = LeerCuenta(c, marcaId, pl);
                    if (string.IsNullOrWhiteSpace(tk)) continue;
                    try
                    {
                        if (pl == "instagram")
                        {
                            var el = await GraphGet(igId, "fields=username,followers_count,media_count", tk);
                            res.Add(new
                            {
                                plataforma = "instagram",
                                usuario = el.TryGetProperty("username", out var u) ? u.GetString() : "",
                                seguidores = el.TryGetProperty("followers_count", out var f) && f.ValueKind == System.Text.Json.JsonValueKind.Number ? f.GetInt64() : 0,
                                publicaciones = el.TryGetProperty("media_count", out var mc) && mc.ValueKind == System.Text.Json.JsonValueKind.Number ? mc.GetInt64() : 0
                            });
                        }
                        else
                        {
                            var el = await GraphGet(pageId, "fields=name,fan_count,followers_count", tk);
                            long seg = el.TryGetProperty("followers_count", out var fo) && fo.ValueKind == System.Text.Json.JsonValueKind.Number ? fo.GetInt64()
                                     : (el.TryGetProperty("fan_count", out var fc) && fc.ValueKind == System.Text.Json.JsonValueKind.Number ? fc.GetInt64() : 0);
                            res.Add(new
                            {
                                plataforma = "facebook",
                                usuario = el.TryGetProperty("name", out var n) ? n.GetString() : "",
                                seguidores = seg,
                                publicaciones = (long?)null
                            });
                        }
                    }
                    catch { }
                }
                return Results.Ok(res);
            }).RequireAuthorization();

            // ════════════════════════ VIDEO (reels) ═════════════════════════
            // Genera un reel 9:16 a partir de las imágenes del post. tipo='armado' (ffmpeg)
            // funciona ya; tipo='ia' (Veo/Kling/…) queda para cuando se configure un motor.
            app.MapPost("/api/contenido/posts/{id:int}/video", async (int id, HttpContext ctx, ContVideoReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                var tipo = (req?.Tipo ?? "armado").Trim().ToLowerInvariant();
                if (tipo == "ia")
                    return Results.BadRequest(new { error = "El video con IA (Veo/Kling/Seedance) estará disponible al configurar un motor. Por ahora usa 'Armado'." });

                using var c = Db.Conn();
                var imgs = new List<string>();
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT RutaArchivo FROM PostAssets WHERE PostId=$id AND (Tipo IS NULL OR Tipo='imagen') ORDER BY Orden";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) { var p = Db.S(r, 0); if (!string.IsNullOrWhiteSpace(p) && File.Exists(p)) imgs.Add(p); }
                }
                if (imgs.Count == 0) return Results.BadRequest(new { error = "El post no tiene imágenes para armar el video." });

                double seg = (req?.SegundosPorImagen is > 0 and <= 10) ? req!.SegundosPorImagen!.Value : 3.0;
                var dir = Path.Combine(RutaContenidoDir(), id.ToString());
                Directory.CreateDirectory(dir);
                var outPath = Path.Combine(dir, $"reel-{Guid.NewGuid():N}.mp4");

                var (ok, err) = await GenerarReelArmadoAsync(imgs, seg, req?.Gancho ?? "", outPath);
                if (!ok) return Results.Json(new { error = err }, statusCode: 500);

                int orden;
                using (var mx = c.CreateCommand())
                {
                    mx.CommandText = "SELECT COALESCE(MAX(Orden),0) FROM PostAssets WHERE PostId=$id";
                    mx.Parameters.AddWithValue("$id", id);
                    orden = Convert.ToInt32(mx.ExecuteScalar() ?? 0) + 1;
                }
                using (var ins = c.CreateCommand())
                {
                    ins.CommandText = @"INSERT INTO PostAssets (PostId, Orden, Tipo, RutaArchivo, Origen, ModeloGen, PromptGen, EsFinal)
                                        VALUES ($p,$o,'video',$rt,'video','armado-ffmpeg',$g,1)";
                    ins.Parameters.AddWithValue("$p", id);
                    ins.Parameters.AddWithValue("$o", orden);
                    ins.Parameters.AddWithValue("$rt", outPath);
                    ins.Parameters.AddWithValue("$g", (object?)(req?.Gancho) ?? DBNull.Value);
                    ins.ExecuteNonQuery();
                }
                int assetId;
                using (var q = c.CreateCommand()) { q.CommandText = "SELECT last_insert_rowid()"; assetId = Convert.ToInt32(q.ExecuteScalar() ?? 0); }
                return Results.Ok(new { ok = true, assetId });
            }).RequireAuthorization();

            // ════════════════════ BANDEJA (mensajes / comentarios) ══════════════
            // Webhook de Meta (sin auth). GET = verificacion; POST = eventos entrantes.
            app.MapGet("/api/contenido/webhook/meta", (HttpContext ctx) =>
            {
                var q = ctx.Request.Query;
                var verify = Environment.GetEnvironmentVariable("SG_META_VERIFY_TOKEN") ?? "hatopascher-verify";
                if (q["hub.mode"] == "subscribe" && q["hub.verify_token"] == verify)
                    return Results.Text(q["hub.challenge"].ToString());
                return Results.StatusCode(403);
            });

            app.MapPost("/api/contenido/webhook/meta", async (HttpContext ctx) =>
            {
                string body;
                using (var sr = new StreamReader(ctx.Request.Body)) body = await sr.ReadToEndAsync();
                try { await ProcesarWebhookMetaAsync(body); } catch { }
                return Results.Ok(new { received = true }); // Meta exige 200 rapido
            });

            // Simular un mensaje entrante (para probar la logica SIN Meta). Rol Redes.
            app.MapPost("/api/contenido/marcas/{marcaId:int}/mensajes/simular", async (int marcaId, HttpContext ctx, ContSimularMsgReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (req == null || string.IsNullOrWhiteSpace(req.Texto)) return Results.BadRequest(new { error = "Falta el texto." });
                var canal = string.IsNullOrWhiteSpace(req.Canal) ? "ig_comment" : req.Canal.Trim().ToLowerInvariant();
                await GuardarYProcesarAsync(marcaId, canal, "sim-" + Guid.NewGuid().ToString("N"),
                    req.AutorNombre ?? "Cliente (simulado)", "", req.Texto, req.PostExternoId ?? "", "");
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // Listar mensajes de una marca (bandeja). Rol Redes.
            app.MapGet("/api/contenido/marcas/{marcaId:int}/mensajes", (int marcaId, string? estado, string? canal, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                var sql = @"SELECT Id,Canal,COALESCE(AutorNombre,''),COALESCE(Texto,''),Estado,RequiereHumano,
                                   COALESCE(RespuestaIa,''),RespuestaEnviada,COALESCE(Permalink,''),COALESCE(AvisarEn,''),
                                   Avisado,COALESCE(RecibidoEn,''),COALESCE(NotaInterna,'')
                            FROM MensajesRed WHERE MarcaId=$m";
                cmd.Parameters.AddWithValue("$m", marcaId);
                if (!string.IsNullOrWhiteSpace(estado)) { sql += " AND Estado=$e"; cmd.Parameters.AddWithValue("$e", estado); }
                if (!string.IsNullOrWhiteSpace(canal)) { sql += " AND Canal=$c"; cmd.Parameters.AddWithValue("$c", canal); }
                sql += " ORDER BY (Estado='escalado') DESC, Id DESC LIMIT 300";
                cmd.CommandText = sql;
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new
                    {
                        id = Db.I(r, 0), canal = Db.S(r, 1), autor = Db.S(r, 2), texto = Db.S(r, 3), estado = Db.S(r, 4),
                        requiereHumano = Db.I(r, 5) == 1, respuestaIa = Db.S(r, 6), respuestaEnviada = Db.I(r, 7) == 1,
                        permalink = Db.S(r, 8), avisarEn = Db.S(r, 9), avisado = Db.I(r, 10) == 1,
                        recibidoEn = Db.S(r, 11), nota = Db.S(r, 12)
                    });
                return Results.Ok(lista);
            }).RequireAuthorization();

            // Contador de avisos pendientes (para el badge). Rol Redes.
            app.MapGet("/api/contenido/marcas/{marcaId:int}/avisos", (int marcaId, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT COUNT(*) FROM MensajesRed WHERE MarcaId=$m AND Estado='escalado' AND Avisado=0
                                    AND (AvisarEn IS NULL OR AvisarEn <= datetime('now','localtime'))";
                cmd.Parameters.AddWithValue("$m", marcaId);
                var n = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                return Results.Ok(new { pendientes = n });
            }).RequireAuthorization();

            // Responder manualmente un mensaje (envía por el canal si hay token). Rol Redes.
            app.MapPost("/api/contenido/mensajes/{id:int}/responder", async (int id, HttpContext ctx, ContResponderMsgReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (req == null || string.IsNullOrWhiteSpace(req.Texto)) return Results.BadRequest(new { error = "Falta el texto." });
                using var c = Db.Conn();
                int marcaId; string canal, externoId, autorId;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT MarcaId,Canal,COALESCE(ExternoId,''),COALESCE(AutorId,'') FROM MensajesRed WHERE Id=$id";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) return Results.NotFound(new { error = "Mensaje no encontrado." });
                    marcaId = Db.I(r, 0); canal = Db.S(r, 1); externoId = Db.S(r, 2); autorId = Db.S(r, 3);
                }
                bool enviado = await EnviarRespuestaCanal(c, marcaId, canal, externoId, autorId, req.Texto);
                using (var upd = c.CreateCommand())
                {
                    upd.CommandText = "UPDATE MensajesRed SET Estado='respondido',RespuestaEnviada=$re,Avisado=1,NotaInterna=$n,ActualizadoEn=datetime('now','localtime') WHERE Id=$id";
                    upd.Parameters.AddWithValue("$re", enviado ? 1 : 0);
                    upd.Parameters.AddWithValue("$n", "Respuesta manual: " + req.Texto);
                    upd.Parameters.AddWithValue("$id", id);
                    upd.ExecuteNonQuery();
                }
                return Results.Ok(new { ok = true, enviado });
            }).RequireAuthorization();

            // Cambiar estado (cerrar, marcar respondido, etc.). Rol Redes.
            app.MapPost("/api/contenido/mensajes/{id:int}/estado", (int id, HttpContext ctx, ContEstadoMsgReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                var est = (req?.Estado ?? "").Trim().ToLowerInvariant();
                if (est is not ("nuevo" or "escalado" or "respondido" or "cerrado" or "auto_respondido"))
                    return Results.BadRequest(new { error = "Estado inválido." });
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "UPDATE MensajesRed SET Estado=$e,Avisado=1,ActualizadoEn=datetime('now','localtime') WHERE Id=$id";
                cmd.Parameters.AddWithValue("$e", est);
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // Configuración de la bandeja (horario, base de conocimiento, plantilla). Rol Redes.
            app.MapGet("/api/contenido/marcas/{marcaId:int}/bandeja-config", (int marcaId, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cfg = LeerBandejaConfig(c, marcaId);
                return Results.Ok(new
                {
                    horarioInicio = cfg.ini, horarioFin = cfg.fin, diasLaborales = cfg.dias,
                    baseConocimiento = cfg.kb, plantillaAcuse = cfg.acuse, autoRespuesta = cfg.auto,
                    palabrasHumano = cfg.palabras, activa = cfg.activa
                });
            }).RequireAuthorization();

            app.MapPut("/api/contenido/marcas/{marcaId:int}/bandeja-config", (int marcaId, HttpContext ctx, ContBandejaConfigReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                LeerBandejaConfig(c, marcaId); // asegura que exista la fila
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"UPDATE BandejaConfig SET
                        HorarioInicio=COALESCE($hi,HorarioInicio),
                        HorarioFin=COALESCE($hf,HorarioFin),
                        DiasLaborales=COALESCE($dl,DiasLaborales),
                        BaseConocimiento=COALESCE($bc,BaseConocimiento),
                        PlantillaAcuse=COALESCE($pa,PlantillaAcuse),
                        AutoRespuesta=COALESCE($ar,AutoRespuesta),
                        PalabrasHumano=COALESCE($ph,PalabrasHumano),
                        Activa=COALESCE($ac,Activa)
                        WHERE MarcaId=$m";
                cmd.Parameters.AddWithValue("$hi", (object?)req.HorarioInicio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$hf", (object?)req.HorarioFin ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$dl", (object?)req.DiasLaborales ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$bc", (object?)req.BaseConocimiento ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$pa", (object?)req.PlantillaAcuse ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$ar", req.AutoRespuesta.HasValue ? (object)(req.AutoRespuesta.Value ? 1 : 0) : DBNull.Value);
                cmd.Parameters.AddWithValue("$ph", (object?)req.PalabrasHumano ?? DBNull.Value);
                cmd.Parameters.AddWithValue("$ac", req.Activa.HasValue ? (object)(req.Activa.Value ? 1 : 0) : DBNull.Value);
                cmd.Parameters.AddWithValue("$m", marcaId);
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true });
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

            // ════════════════════ GENERACIÓN CON IA (n8n) ════════════════════

            // Dispara WF1 (generar ideas). El resultado llega por /hook/ideas.
            app.MapPost("/api/contenido/ideas/generar", async (HttpContext ctx, GenerarIdeasReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (req.MarcaId <= 0) return Results.BadRequest(new { error = "MarcaId requerido." });

                var marca = LeerMarcaDict(req.MarcaId);
                if (marca is null) return Results.NotFound(new { error = "Marca no encontrada." });

                var payload = new
                {
                    jobId = Guid.NewGuid().ToString("N"),
                    tipo = "generar_ideas",
                    marca,
                    pilares = req.Pilares ?? Array.Empty<string>(),
                    cantidad = req.Cantidad is > 0 and <= 15 ? req.Cantidad!.Value : 5,
                    usarBase = req.UsarBase ?? true,
                    callbackUrl = CallbackUrl("/api/contenido/hook/ideas")
                };
                var (ok, err) = await PostearN8n("contenido-ideas", payload);
                return ok ? Results.Json(new { ok = true }, statusCode: 202)
                          : Results.Problem(err ?? "No se pudo contactar a n8n.", statusCode: 502);
            }).RequireAuthorization();

            // Callback de WF1: inserta las ideas generadas (auth X-Internal-Token).
            app.MapPost("/api/contenido/hook/ideas", (HttpContext ctx, HookIdeasReq req) =>
            {
                if (!TokenValidoInterno(ctx)) return Results.Json(new { error = "No autorizado." }, statusCode: 401);
                if (req.MarcaId <= 0 || req.Ideas is null) return Results.BadRequest(new { error = "Datos inválidos." });

                int n = 0;
                using var c = Db.Conn();
                foreach (var idea in req.Ideas)
                {
                    if (string.IsNullOrWhiteSpace(idea.Titulo)) continue;
                    using var cmd = c.CreateCommand();
                    cmd.CommandText = @"INSERT INTO IdeasContenido (MarcaId, Titulo, Descripcion, Pilar, Origen, Estado)
                                        VALUES ($m, $t, $d, $p, 'ia', 'nueva')";
                    cmd.Parameters.AddWithValue("$m", req.MarcaId);
                    cmd.Parameters.AddWithValue("$t", idea.Titulo!.Trim());
                    cmd.Parameters.AddWithValue("$d", (object?)idea.Descripcion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$p", (object?)idea.Pilar ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                    n++;
                }
                return Results.Ok(new { insertadas = n });
            });

            // Dispara WF2 (generar post completo). El resultado llega por /hook/resultado.
            app.MapPost("/api/contenido/posts/{id:int}/generar", async (int id, HttpContext ctx, GenerarPostReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();

                using var c = Db.Conn();
                string ideaTitulo = "", ideaDesc = "", ideaPilar = "", formato = "imagen";
                int marcaId = 0;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = @"SELECT p.MarcaId, p.Formato, i.Titulo, i.Descripcion, i.Pilar
                                        FROM Posts p LEFT JOIN IdeasContenido i ON i.Id = p.IdeaId
                                        WHERE p.Id = $id";
                    cmd.Parameters.AddWithValue("$id", id);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) return Results.NotFound(new { error = "Post no encontrado." });
                    marcaId = Db.I(r, 0);
                    formato = string.IsNullOrWhiteSpace(Db.S(r, 1)) ? "imagen" : Db.S(r, 1);
                    ideaTitulo = Db.S(r, 2); ideaDesc = Db.S(r, 3); ideaPilar = Db.S(r, 4);
                }
                var marca = LeerMarcaDict(marcaId);
                if (marca is null) return Results.NotFound(new { error = "Marca no encontrada." });

                var jobId = Guid.NewGuid().ToString("N");
                var promptImagen = string.IsNullOrWhiteSpace(req.PromptImagen)
                    ? $"{ideaTitulo}. {ideaDesc}".Trim() : req.PromptImagen!;
                var fmt = string.IsNullOrWhiteSpace(req.Formato) ? formato : req.Formato!;
                var modo = (req.Modo ?? "todo").Trim().ToLowerInvariant();
                if (modo is not ("texto" or "imagen" or "todo" or "ajuste")) modo = "todo";
                if (modo == "ajuste" && string.IsNullOrWhiteSpace(req.PromptImagen))
                    return Results.BadRequest(new { error = "Falta la instrucción del ajuste (qué cambiar)." });

                using (var upd = c.CreateCommand())
                {
                    upd.CommandText = @"UPDATE Posts SET Estado='generando', JobId=$j, PromptImagen=$pi, Formato=$f,
                                        ModoGen=$modo, MensajeError=NULL, FechaActualizacion=datetime('now','localtime') WHERE Id=$id";
                    upd.Parameters.AddWithValue("$j", jobId);
                    upd.Parameters.AddWithValue("$pi", promptImagen);
                    upd.Parameters.AddWithValue("$f", fmt);
                    upd.Parameters.AddWithValue("$modo", modo);
                    upd.Parameters.AddWithValue("$id", id);
                    upd.ExecuteNonQuery();
                }

                // Referencias + nota para la IA de imagen. Tres casos:
                //  - texto: no lleva imagenes.
                //  - ajuste: edita la imagen ACTUAL (base) aplicando solo el cambio pedido, conservando el resto.
                //  - imagen/todo: genera con referencias PRODUCTO (base real) + CREATIVIDAD (inspiracion).
                var referencias = new List<object>();
                var notaRefs = "";
                if (modo == "ajuste")
                {
                    // Localizar la imagen base a editar (la elegida, o la ultima del post).
                    string? rutaBase = null;
                    using (var cmd = c.CreateCommand())
                    {
                        if (req.AssetIdBase is int aid)
                        {
                            cmd.CommandText = "SELECT RutaArchivo FROM PostAssets WHERE Id=$a AND PostId=$id";
                            cmd.Parameters.AddWithValue("$a", aid);
                            cmd.Parameters.AddWithValue("$id", id);
                        }
                        else
                        {
                            cmd.CommandText = "SELECT RutaArchivo FROM PostAssets WHERE PostId=$id ORDER BY Orden DESC, Id DESC LIMIT 1";
                            cmd.Parameters.AddWithValue("$id", id);
                        }
                        rutaBase = cmd.ExecuteScalar() as string;
                    }
                    if (string.IsNullOrWhiteSpace(rutaBase) || !File.Exists(rutaBase))
                        return Results.BadRequest(new { error = "No encontré la imagen base para ajustar." });
                    try
                    {
                        var jpg = RedimensionarJpeg(File.ReadAllBytes(rutaBase), 1024, 85);
                        referencias.Add(new { mime = "image/jpeg", data = Convert.ToBase64String(jpg) });
                    }
                    catch { return Results.Problem("No se pudo leer la imagen base para el ajuste."); }
                    notaRefs = "EDICION DE IMAGEN: la PRIMERA imagen adjunta es la BASE. Aplica UNICAMENTE el cambio solicitado y CONSERVA todo lo demas EXACTAMENTE igual (misma composicion, encuadre, fondo, iluminacion, y las demas partes del producto). No regeneres desde cero ni cambies nada que no se haya pedido. Cambio solicitado: ";
                }
                else if (modo != "texto")
                {
                    var refsProducto    = LeerReferenciasParaPayload(marcaId, 4, req.Referencias, "producto");
                    var refsCreatividad = (req.ReferenciasCreatividad != null && req.ReferenciasCreatividad.Length > 0)
                        ? LeerReferenciasParaPayload(marcaId, 3, req.ReferenciasCreatividad, "creatividad")
                        : new List<object>();
                    referencias.AddRange(refsProducto);
                    referencias.AddRange(refsCreatividad);
                    if (refsProducto.Count > 0)
                        notaRefs = "Las primeras imagenes de referencia son los PRODUCTOS REALES de la marca: fustes de plastico color BLANCO HUESO. Reproduce FIELMENTE su forma y su color blanco hueso tal como se ven en las fotos; NUNCA cambies el color ni el material del producto. Cualquier fuste de OTRO color es de la competencia o de mala calidad y NO debe copiarse. ";
                    if (refsCreatividad.Count > 0)
                        notaRefs += $"Las ultimas {refsCreatividad.Count} imagen(es) son SOLO inspiracion de estilo/composicion o comparacion; NO copies de ellas el color ni la forma del producto. ";
                }

                var payload = new
                {
                    jobId, postId = id, tipo = "generar_post", marca, referencias, notaRefs, modo,
                    idea = new { titulo = ideaTitulo, descripcion = ideaDesc, pilar = ideaPilar },
                    formato = fmt, promptImagen,
                    nImagenes = req.NImagenes is > 0 and <= 6 ? req.NImagenes!.Value : 1,
                    callbackUrl = CallbackUrl("/api/contenido/hook/resultado")
                };
                var (ok, err) = await PostearN8n("contenido-post", payload);
                if (!ok)
                {
                    using var e = c.CreateCommand();
                    e.CommandText = "UPDATE Posts SET Estado='borrador', MensajeError=$e WHERE Id=$id";
                    e.Parameters.AddWithValue("$e", err ?? "No se pudo contactar a n8n.");
                    e.Parameters.AddWithValue("$id", id);
                    e.ExecuteNonQuery();
                    return Results.Problem(err ?? "No se pudo contactar a n8n.", statusCode: 502);
                }
                return Results.Json(new { ok = true, jobId }, statusCode: 202);
            }).RequireAuthorization();

            // Callback de WF2: guarda caption, hashtags y assets (imagen base64).
            app.MapPost("/api/contenido/hook/resultado", async (HttpContext ctx, HookResultadoReq req) =>
            {
                if (!TokenValidoInterno(ctx)) return Results.Json(new { error = "No autorizado." }, statusCode: 401);
                if (req.PostId <= 0) return Results.BadRequest(new { error = "postId requerido." });

                using var c = Db.Conn();
                string jobActual = "", modo = "todo";
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = "SELECT COALESCE(JobId,''), COALESCE(NULLIF(ModoGen,''),'todo') FROM Posts WHERE Id=$id";
                    cmd.Parameters.AddWithValue("$id", req.PostId);
                    using var r = cmd.ExecuteReader();
                    if (!r.Read()) return Results.NotFound(new { error = "Post no encontrado." });
                    jobActual = Db.S(r, 0);
                    modo = Db.S(r, 1);
                }
                bool aplicarTexto = modo != "imagen" && modo != "ajuste";
                bool aplicarImagen = modo != "texto";
                if (!string.IsNullOrEmpty(req.JobId) && !string.IsNullOrEmpty(jobActual) && req.JobId != jobActual)
                    return Results.BadRequest(new { error = "jobId no coincide." });

                if (req.Estado == "error")
                {
                    using var e = c.CreateCommand();
                    e.CommandText = "UPDATE Posts SET Estado='borrador', MensajeError=$m WHERE Id=$id";
                    e.Parameters.AddWithValue("$m", req.Error ?? "Error en la generación.");
                    e.Parameters.AddWithValue("$id", req.PostId);
                    e.ExecuteNonQuery();
                    return Results.Ok(new { ok = true });
                }

                var dir = Path.Combine(RutaContenidoDir(), req.PostId.ToString());
                Directory.CreateDirectory(dir);
                int orden = 0;
                string? primerB64 = null, primerMime = null;
                if (req.Assets != null && aplicarImagen)
                {
                    if (modo == "ajuste")
                    {
                        // Ajuste: conservar las imagenes existentes y agregar la version nueva al final.
                        using var mx = c.CreateCommand();
                        mx.CommandText = "SELECT COALESCE(MAX(Orden),0) FROM PostAssets WHERE PostId=$id";
                        mx.Parameters.AddWithValue("$id", req.PostId);
                        orden = Convert.ToInt32(mx.ExecuteScalar() ?? 0);
                    }
                    else
                    {
                        using var del = c.CreateCommand();
                        del.CommandText = "DELETE FROM PostAssets WHERE PostId=$id AND Origen='ia'";
                        del.Parameters.AddWithValue("$id", req.PostId);
                        del.ExecuteNonQuery();
                    }
                    foreach (var a in req.Assets)
                    {
                        if (string.IsNullOrWhiteSpace(a.DataBase64)) continue;
                        orden++;
                        var ext = (a.Mime ?? "image/png").Contains("jpeg") ? "jpg" : "png";
                        var file = $"{orden}-{Guid.NewGuid():N}.{ext}";
                        var full = Path.Combine(dir, file);
                        try { await File.WriteAllBytesAsync(full, Convert.FromBase64String(LimpiarBase64(a.DataBase64!))); }
                        catch { orden--; continue; }
                        if (primerB64 == null && (a.Tipo ?? "imagen") == "imagen") { primerB64 = a.DataBase64; primerMime = a.Mime ?? "image/png"; }
                        using var cmd = c.CreateCommand();
                        cmd.CommandText = @"INSERT INTO PostAssets (PostId, Orden, Tipo, RutaArchivo, Origen, ModeloGen, PromptGen, EsFinal)
                                            VALUES ($p, $o, $t, $rt, 'ia', $mo, $pr, 1)";
                        cmd.Parameters.AddWithValue("$p", req.PostId);
                        cmd.Parameters.AddWithValue("$o", orden);
                        cmd.Parameters.AddWithValue("$t", a.Tipo ?? "imagen");
                        cmd.Parameters.AddWithValue("$rt", full);
                        cmd.Parameters.AddWithValue("$mo", (object?)a.Modelo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("$pr", (object?)a.Prompt ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Verificacion de material: la marca es fuste PLASTICO; nunca madera.
                string? advertencia = null;
                if (modo != "ajuste" && primerB64 != null && !string.IsNullOrEmpty(GeminiKey()))
                {
                    try
                    {
                        if (await DetectaMaderaAsync(primerB64, primerMime ?? "image/png"))
                            advertencia = "\u26a0 Verificacion IA: la imagen podria mostrar MADERA (la marca es fuste de plastico HDPE). Regenera o revisa antes de aprobar.";
                    }
                    catch { }
                }

                using (var upd = c.CreateCommand())
                {
                    upd.CommandText = @"UPDATE Posts SET
                            CaptionEs = COALESCE($ces, CaptionEs),
                            CaptionEn = COALESCE($cen, CaptionEn),
                            Hashtags  = COALESCE($ht, Hashtags),
                            ModeloUsado = COALESCE($mo, ModeloUsado),
                            Estado = 'borrador', MensajeError = $err, ModoGen = NULL,
                            FechaActualizacion = datetime('now','localtime')
                        WHERE Id=$id";
                    upd.Parameters.AddWithValue("$ces", aplicarTexto ? ((object?)req.Caption?.Es ?? DBNull.Value) : DBNull.Value);
                    upd.Parameters.AddWithValue("$cen", aplicarTexto ? ((object?)req.Caption?.En ?? DBNull.Value) : DBNull.Value);
                    upd.Parameters.AddWithValue("$ht", aplicarTexto ? ((object?)(req.Hashtags != null ? string.Join(" ", req.Hashtags) : null) ?? DBNull.Value) : DBNull.Value);
                    upd.Parameters.AddWithValue("$mo", aplicarImagen ? ((object?)req.Modelo ?? DBNull.Value) : DBNull.Value);
                    upd.Parameters.AddWithValue("$err", (object?)advertencia ?? DBNull.Value);
                    upd.Parameters.AddWithValue("$id", req.PostId);
                    upd.ExecuteNonQuery();
                }
                return Results.Ok(new { ok = true, assets = orden });
            });

            // Servir un asset (imagen/video) para vista previa — protegido por el gate de rol.
            app.MapGet("/api/contenido/posts/{id:int}/asset/{assetId:int}", (int id, int assetId) =>
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT RutaArchivo FROM PostAssets WHERE Id=$a AND PostId=$p";
                cmd.Parameters.AddWithValue("$a", assetId);
                cmd.Parameters.AddWithValue("$p", id);
                var v = cmd.ExecuteScalar();
                var ruta = v?.ToString() ?? "";
                if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta)) return Results.NotFound();
                var ext = Path.GetExtension(ruta).ToLowerInvariant();
                var mime = ext is ".jpg" or ".jpeg" ? "image/jpeg" : ext == ".mp4" ? "video/mp4" : "image/png";
                return Results.File(File.ReadAllBytes(ruta), mime);
            }).RequireAuthorization();

            // ════════════════ REFERENCIAS DE MARCA (fotos reales del producto) ════════════════
            //  Se guardan en {contenido}/marca-{id}/ref y se usan como base en la generacion.

            app.MapGet("/api/contenido/marcas/{id:int}/referencias", (int id, string? categoria) =>
            {
                var dir = RutaRefsDir(id, categoria);
                var files = Directory.GetFiles(dir).Select(Path.GetFileName).OrderBy(x => x).ToList();
                return Results.Ok(files);
            }).RequireAuthorization();

            app.MapPost("/api/contenido/marcas/{id:int}/referencias", async (int id, HttpContext ctx, IFormFile file, string? categoria) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (file is null || file.Length == 0) return Results.BadRequest(new { error = "Archivo vacio." });
                if (file.Length > 8 * 1024 * 1024) return Results.BadRequest(new { error = "Maximo 8 MB por imagen." });
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp"))
                    return Results.BadRequest(new { error = "Solo PNG, JPG o WEBP." });
                var dir = RutaRefsDir(id, categoria);
                var name = (DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")).Substring(0, 20) + ext;
                await using (var fs = File.Create(Path.Combine(dir, name)))
                    await file.CopyToAsync(fs);
                return Results.Ok(new { name });
            }).RequireAuthorization().DisableAntiforgery();

            app.MapGet("/api/contenido/marcas/{id:int}/referencias/{name}", (int id, string name, string? categoria) =>
            {
                if (!NombreRefSeguro(name)) return Results.NotFound();
                var full = Path.Combine(RutaRefsDir(id, categoria), name);
                if (!File.Exists(full)) return Results.NotFound();
                var ext = Path.GetExtension(full).ToLowerInvariant();
                var mime = ext is ".jpg" or ".jpeg" ? "image/jpeg" : ext == ".webp" ? "image/webp" : "image/png";
                return Results.File(File.ReadAllBytes(full), mime);
            }).RequireAuthorization();

            app.MapDelete("/api/contenido/marcas/{id:int}/referencias/{name}", (int id, HttpContext ctx, string name, string? categoria) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (!NombreRefSeguro(name)) return Results.NotFound();
                var full = Path.Combine(RutaRefsDir(id, categoria), name);
                if (File.Exists(full)) File.Delete(full);
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // Mover una referencia de un banco al otro (producto <-> creatividad).
            app.MapPost("/api/contenido/marcas/{id:int}/referencias/{name}/mover", (int id, HttpContext ctx, string name, string? desde, string? hacia) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (!NombreRefSeguro(name)) return Results.NotFound();
                var catDesde = (desde ?? "").Trim().ToLowerInvariant() == "creatividad" ? "creatividad" : "producto";
                var catHacia = (hacia ?? "").Trim().ToLowerInvariant() == "creatividad" ? "creatividad" : "producto";
                if (catDesde == catHacia) return Results.Ok(new { ok = true });
                var src = Path.Combine(RutaRefsDir(id, catDesde), name);
                if (!File.Exists(src)) return Results.NotFound();
                var dst = Path.Combine(RutaRefsDir(id, catHacia), name);
                try
                {
                    if (File.Exists(dst)) File.Delete(dst);
                    File.Move(src, dst);
                }
                catch (Exception ex) { return Results.Problem(ex.Message); }
                return Results.Ok(new { ok = true, categoria = catHacia });
            }).RequireAuthorization();

            // ════════════════ ASSETS DEL POST (carrusel / subida manual) ════════════════

            // Subir una imagen manual como asset del post (para carrusel o reemplazo).
            app.MapPost("/api/contenido/posts/{id:int}/assets", async (int id, HttpContext ctx, IFormFile file) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (file is null || file.Length == 0) return Results.BadRequest(new { error = "Archivo vacio." });
                if (file.Length > 12 * 1024 * 1024) return Results.BadRequest(new { error = "Maximo 12 MB." });
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp"))
                    return Results.BadRequest(new { error = "Solo PNG, JPG o WEBP." });

                using var c = Db.Conn();
                int orden;
                using (var q = c.CreateCommand())
                {
                    q.CommandText = "SELECT COUNT(*), COALESCE(MAX(Orden),0) FROM PostAssets WHERE PostId=$p";
                    q.Parameters.AddWithValue("$p", id);
                    using var chkPost = c.CreateCommand();
                    chkPost.CommandText = "SELECT 1 FROM Posts WHERE Id=$p";
                    chkPost.Parameters.AddWithValue("$p", id);
                    if (chkPost.ExecuteScalar() is null) return Results.NotFound(new { error = "Post no encontrado." });
                    using var r = q.ExecuteReader();
                    r.Read();
                    orden = Db.I2(r, 1) + 1;
                }
                var dir = Path.Combine(RutaContenidoDir(), id.ToString());
                Directory.CreateDirectory(dir);
                var outExt = ext == ".png" ? "png" : ext == ".webp" ? "webp" : "jpg";
                var name = $"{orden}-{Guid.NewGuid():N}.{outExt}";
                var full = Path.Combine(dir, name);
                await using (var fs = File.Create(full)) await file.CopyToAsync(fs);

                long newId;
                using (var cmd = c.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO PostAssets (PostId, Orden, Tipo, RutaArchivo, Origen, EsFinal)
                                        VALUES ($p, $o, 'imagen', $rt, 'manual', 1);
                                        SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("$p", id);
                    cmd.Parameters.AddWithValue("$o", orden);
                    cmd.Parameters.AddWithValue("$rt", full);
                    newId = Convert.ToInt64(cmd.ExecuteScalar());
                }
                return Results.Ok(new { id = newId, orden });
            }).RequireAuthorization().DisableAntiforgery();

            // Eliminar un asset (imagen) del post.
            app.MapDelete("/api/contenido/posts/{id:int}/assets/{assetId:int}", (int id, int assetId, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                string ruta;
                using (var q = c.CreateCommand())
                {
                    q.CommandText = "SELECT RutaArchivo FROM PostAssets WHERE Id=$a AND PostId=$p";
                    q.Parameters.AddWithValue("$a", assetId);
                    q.Parameters.AddWithValue("$p", id);
                    var v = q.ExecuteScalar();
                    if (v is null) return Results.NotFound();
                    ruta = v.ToString() ?? "";
                }
                using (var del = c.CreateCommand())
                {
                    del.CommandText = "DELETE FROM PostAssets WHERE Id=$a AND PostId=$p";
                    del.Parameters.AddWithValue("$a", assetId);
                    del.Parameters.AddWithValue("$p", id);
                    del.ExecuteNonQuery();
                }
                try { if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta)) File.Delete(ruta); } catch { }
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // Reordenar los assets del post (nuevo orden por lista de ids).
            app.MapPut("/api/contenido/posts/{id:int}/assets/orden", (int id, HttpContext ctx, ContOrdenReq req) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                if (req.Ids is null || req.Ids.Length == 0) return Results.BadRequest(new { error = "Ids requeridos." });
                using var c = Db.Conn();
                int orden = 0;
                foreach (var aid in req.Ids)
                {
                    orden++;
                    using var cmd = c.CreateCommand();
                    cmd.CommandText = "UPDATE PostAssets SET Orden=$o WHERE Id=$a AND PostId=$p";
                    cmd.Parameters.AddWithValue("$o", orden);
                    cmd.Parameters.AddWithValue("$a", aid);
                    cmd.Parameters.AddWithValue("$p", id);
                    cmd.ExecuteNonQuery();
                }
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // ════════════════ LIMPIEZA / BORRADO (solo Admin) ════════════════

            // Eliminar UN post (con sus assets en disco).
            app.MapDelete("/api/contenido/posts/{id:int}", (int id, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                BorrarAssetsDeDisco(new[] { id });
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"DELETE FROM PostAssets WHERE PostId=$id;
                                    DELETE FROM Publicaciones WHERE PostId=$id;
                                    DELETE FROM Posts WHERE Id=$id;";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // Vaciar TODOS los posts de una marca (con sus assets en disco).
            app.MapDelete("/api/contenido/marcas/{id:int}/posts", (int id, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var ids = new List<int>();
                using (var q = c.CreateCommand())
                {
                    q.CommandText = "SELECT Id FROM Posts WHERE MarcaId=$m";
                    q.Parameters.AddWithValue("$m", id);
                    using var r = q.ExecuteReader();
                    while (r.Read()) ids.Add(Db.I(r, 0));
                }
                BorrarAssetsDeDisco(ids);
                using var cmd = c.CreateCommand();
                cmd.CommandText = @"DELETE FROM PostAssets WHERE PostId IN (SELECT Id FROM Posts WHERE MarcaId=$m);
                                    DELETE FROM Publicaciones WHERE PostId IN (SELECT Id FROM Posts WHERE MarcaId=$m);
                                    DELETE FROM Posts WHERE MarcaId=$m;";
                cmd.Parameters.AddWithValue("$m", id);
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true, posts = ids.Count });
            }).RequireAuthorization();

            // Vaciar TODAS las ideas de una marca.
            app.MapDelete("/api/contenido/marcas/{id:int}/ideas", (int id, HttpContext ctx) =>
            {
                if (!TokenService.EsRedes(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "DELETE FROM IdeasContenido WHERE MarcaId=$m";
                cmd.Parameters.AddWithValue("$m", id);
                var n = cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true, ideas = n });
            }).RequireAuthorization();
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static void Bind(SqliteCommand cmd, string name, string? val)
            => cmd.Parameters.AddWithValue(name, (object?)val ?? DBNull.Value);

        private static void BorrarAssetsDeDisco(IEnumerable<int> postIds)
        {
            foreach (var pid in postIds)
            {
                try
                {
                    var dir = Path.Combine(RutaContenidoDir(), pid.ToString());
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                }
                catch { }
            }
        }

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

        // ── Helpers de generación IA (n8n) ───────────────────────────────────
        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

        private static bool TokenValidoInterno(HttpContext ctx)
        {
            var token = Environment.GetEnvironmentVariable("SG_INTERNAL_TOKEN") ?? "";
            if (string.IsNullOrEmpty(token)) return false;
            var h = ctx.Request.Headers["X-Internal-Token"].FirstOrDefault();
            return !string.IsNullOrEmpty(h) && string.Equals(h, token, StringComparison.Ordinal);
        }

        private static string CallbackUrl(string path)
        {
            var host = Environment.GetEnvironmentVariable("SG_PUBLIC_HOST");
            var baseUrl = !string.IsNullOrWhiteSpace(host)
                ? $"https://{host}"
                : (Environment.GetEnvironmentVariable("SG_CALLBACK_BASE") ?? "https://api-sg.hato-pascher.com");
            return baseUrl.TrimEnd('/') + path;
        }

        private static string RutaContenidoDir()
        {
            var env = Environment.GetEnvironmentVariable("SG_CONTENIDO_DIR");
            if (!string.IsNullOrWhiteSpace(env)) return env;
            var db = Environment.GetEnvironmentVariable("SG_DB_PATH", EnvironmentVariableTarget.Machine)
                     ?? Environment.GetEnvironmentVariable("SG_DB_PATH");
            var baseDir = !string.IsNullOrWhiteSpace(db) ? Path.GetDirectoryName(db)! : AppContext.BaseDirectory;
            return Path.Combine(baseDir, "contenido");
        }

        private static string LimpiarBase64(string s)
        {
            var i = s.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
            return i >= 0 ? s.Substring(i + 7) : s;
        }

        private static async Task<(bool ok, string? err)> PostearN8n(string webhookPath, object payload)
        {
            var baseUrl = Environment.GetEnvironmentVariable("SG_N8N_WEBHOOK_BASE");
            if (string.IsNullOrWhiteSpace(baseUrl))
                return (false, "SG_N8N_WEBHOOK_BASE no configurada.");
            var url = baseUrl.TrimEnd('/') + "/webhook/" + webhookPath;
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                using var reqMsg = new HttpRequestMessage(HttpMethod.Post, url)
                { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };
                var token = Environment.GetEnvironmentVariable("SG_INTERNAL_TOKEN") ?? "";
                if (!string.IsNullOrEmpty(token)) reqMsg.Headers.Add("X-Internal-Token", token);
                var resp = await _http.SendAsync(reqMsg);
                return resp.IsSuccessStatusCode ? (true, null) : (false, $"n8n respondió {(int)resp.StatusCode}.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        // ── Video (reel armado con ffmpeg) ──────────────────────────────────
        // Escapa una ruta de Windows para usarla dentro de un filtro de ffmpeg
        // (drawtext fontfile/textfile): C:\a\b  ->  C\:/a/b
        private static string FfPath(string p) => p.Replace("\\", "/").Replace(":", "\\:");

        private static async Task<(bool ok, string err)> CorrerFfmpegAsync(List<string> args, int timeoutMs = 180000)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                foreach (var a in args) psi.ArgumentList.Add(a);
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) return (false, "No se pudo iniciar ffmpeg.");
                var errTask = p.StandardError.ReadToEndAsync();
                if (!p.WaitForExit(timeoutMs)) { try { p.Kill(true); } catch { } return (false, "ffmpeg tardó demasiado (timeout)."); }
                var err = await errTask;
                if (p.ExitCode != 0)
                    return (false, "ffmpeg error: " + (err.Length > 600 ? err.Substring(err.Length - 600) : err));
                return (true, "");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return (false, "ffmpeg no está instalado en el servidor. Instálalo (por ej. 'winget install Gyan.FFmpeg') y reinicia la API.");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        // Arma un reel 9:16 (1080x1920) con Ken Burns por imagen + texto de gancho opcional.
        private static async Task<(bool ok, string err)> GenerarReelArmadoAsync(List<string> imgs, double segPorImg, string gancho, string outPath)
        {
            const int fps = 30;
            int framesPerImg = Math.Max(15, (int)Math.Round(segPorImg * fps));
            double totalDur = imgs.Count * segPorImg;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string kb = $"scale=2160:3840:force_original_aspect_ratio=increase,crop=2160:3840,zoompan=z='min(zoom+0.0012,1.18)':d={framesPerImg}:x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':s=1080x1920:fps={fps},setsar=1";

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < imgs.Count; i++) sb.Append($"[{i}:v]{kb}[v{i}];");
            for (int i = 0; i < imgs.Count; i++) sb.Append($"[v{i}]");
            sb.Append($"concat=n={imgs.Count}:v=1:a=0[vc];");

            string lastLabel = "vc";
            string? hookFile = null;
            string? fontReal = new[] { @"C:\Windows\Fonts\arialbd.ttf", @"C:\Windows\Fonts\segoeuib.ttf", @"C:\Windows\Fonts\arial.ttf", @"C:\Windows\Fonts\segoeui.ttf" }
                .FirstOrDefault(File.Exists);
            if (!string.IsNullOrWhiteSpace(gancho) && fontReal != null)
            {
                hookFile = Path.Combine(Path.GetDirectoryName(outPath)!, $"hook-{Guid.NewGuid():N}.txt");
                await File.WriteAllTextAsync(hookFile, gancho.Trim(), new System.Text.UTF8Encoding(false));
                var font = FfPath(fontReal);
                var hf = FfPath(hookFile);
                double hookDur = Math.Min(totalDur, 3.0);
                sb.Append($"[vc]drawtext=fontfile={font}:textfile={hf}:fontcolor=white:fontsize=72:borderw=6:bordercolor=black@0.85:x=(w-tw)/2:y=170:line_spacing=12:enable='lt(t,{hookDur.ToString("0.###", inv)})'[vout];");
                lastLabel = "vout";
            }
            var filter = sb.ToString().TrimEnd(';');

            var args = new List<string> { "-y", "-loglevel", "error" };
            foreach (var im in imgs) { args.Add("-i"); args.Add(im); }
            args.Add("-filter_complex"); args.Add(filter);
            args.Add("-map"); args.Add($"[{lastLabel}]");
            args.Add("-r"); args.Add(fps.ToString());
            args.Add("-c:v"); args.Add("libx264");
            args.Add("-preset"); args.Add("veryfast");
            args.Add("-pix_fmt"); args.Add("yuv420p");
            args.Add("-t"); args.Add(totalDur.ToString("0.###", inv));
            args.Add(outPath);

            var res = await CorrerFfmpegAsync(args);
            if (hookFile != null) { try { File.Delete(hookFile); } catch { } }
            return res;
        }

        // ── Bandeja: configuración, horario, clasificación IA y escalado ─────
        private static (string ini, string fin, string dias, string kb, string acuse, bool auto, string palabras, bool activa)
            LeerBandejaConfig(SqliteConnection c, int marcaId)
        {
            using (var chk = c.CreateCommand())
            {
                chk.CommandText = @"SELECT HorarioInicio,HorarioFin,DiasLaborales,COALESCE(BaseConocimiento,''),
                                           PlantillaAcuse,AutoRespuesta,COALESCE(PalabrasHumano,''),Activa
                                    FROM BandejaConfig WHERE MarcaId=$m";
                chk.Parameters.AddWithValue("$m", marcaId);
                using var r = chk.ExecuteReader();
                if (r.Read())
                    return (Db.S(r, 0), Db.S(r, 1), Db.S(r, 2), Db.S(r, 3), Db.S(r, 4), Db.I(r, 5) == 1, Db.S(r, 6), Db.I(r, 7) == 1);
            }
            using (var ins = c.CreateCommand())
            {
                ins.CommandText = "INSERT OR IGNORE INTO BandejaConfig (MarcaId) VALUES ($m)";
                ins.Parameters.AddWithValue("$m", marcaId);
                ins.ExecuteNonQuery();
            }
            return ("08:00", "17:00", "1,2,3,4,5", "",
                "Gracias por escribirnos. Te responderemos personalmente en cuanto inicie nuestro horario de atencion.",
                true, "reclamo,queja,devolucion,demanda,abogado,mayorista,distribuidor,factura,garantia", true);
        }

        private static bool EnHorario(string ini, string fin, string dias, DateTime now)
        {
            try
            {
                int dow = (int)now.DayOfWeek;          // 0=Dom..6=Sab
                int diaIso = dow == 0 ? 7 : dow;       // 1=Lun..7=Dom
                var setDias = (dias ?? "1,2,3,4,5")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => int.TryParse(x, out var d) ? d : -1).ToHashSet();
                if (!setDias.Contains(diaIso)) return false;
                if (TimeSpan.TryParse(ini, out var ti) && TimeSpan.TryParse(fin, out var tf))
                    return now.TimeOfDay >= ti && now.TimeOfDay <= tf;
                return true;
            }
            catch { return true; }
        }

        private static DateTime ProximoInicioHorario(string ini, string dias, DateTime now)
        {
            try
            {
                if (!TimeSpan.TryParse(ini, out var ti)) ti = new TimeSpan(8, 0, 0);
                var setDias = (dias ?? "1,2,3,4,5")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => int.TryParse(x, out var d) ? d : -1).ToHashSet();
                for (int i = 0; i <= 7; i++)
                {
                    var cand = now.Date.AddDays(i).Add(ti);
                    int dow = (int)cand.DayOfWeek; int diaIso = dow == 0 ? 7 : dow;
                    if (setDias.Contains(diaIso) && cand > now) return cand;
                }
                return now.AddHours(12);
            }
            catch { return now.AddHours(12); }
        }

        // Devuelve (puede, respuesta). puede=true => la IA respondió con seguridad usando la base.
        private static async Task<(bool puede, string respuesta)> ClasificarMensajeAsync(string kb, string palabrasHumano, string texto)
        {
            var low = (texto ?? "").ToLowerInvariant();
            foreach (var p in (palabrasHumano ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (p.Length > 0 && low.Contains(p.ToLowerInvariant())) return (false, "");

            var key = GeminiKey();
            if (string.IsNullOrEmpty(key)) return (false, "");
            var prompt =
                "Eres el asistente de atencion al cliente de una marca. Responde SOLO con la base de conocimiento:\n" +
                "=== BASE ===\n" + (string.IsNullOrWhiteSpace(kb) ? "(sin base de conocimiento)" : kb) + "\n=== FIN BASE ===\n" +
                "Mensaje del cliente: \"" + (texto ?? "") + "\"\n" +
                "Si puedes responder de forma correcta, breve y amable USANDO SOLO la base, hazlo. " +
                "Si requiere una persona (precio no listado, reclamo, negociacion, hacer un pedido, o algo que NO esta en la base), NO inventes. " +
                "Responde UNICAMENTE en JSON: {\"puede\":true|false,\"respuesta\":\"...\"}. Si puede=false deja respuesta vacia.";
            var body = new { contents = new[] { new { parts = new object[] { new { text = prompt } } } } };
            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, url)
            { Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json") };
            reqMsg.Headers.Add("x-goog-api-key", key);
            try
            {
                var resp = await _http.SendAsync(reqMsg);
                if (!resp.IsSuccessStatusCode) return (false, "");
                var txt = await resp.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(txt);
                var ans = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
                int a = ans.IndexOf('{'), b = ans.LastIndexOf('}');
                if (a >= 0 && b > a) ans = ans.Substring(a, b - a + 1);
                using var j = System.Text.Json.JsonDocument.Parse(ans);
                bool puede = j.RootElement.TryGetProperty("puede", out var pp) && pp.ValueKind == System.Text.Json.JsonValueKind.True;
                string r = j.RootElement.TryGetProperty("respuesta", out var rr) ? (rr.GetString() ?? "") : "";
                if (puede && string.IsNullOrWhiteSpace(r)) puede = false;
                return (puede, r);
            }
            catch { return (false, ""); }
        }

        private static async Task<bool> GraphPostRawJson(string path, string json, string token)
        {
            try
            {
                var url = $"{GraphBase}{path}?access_token={Uri.EscapeDataString(token)}";
                using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var resp = await _http.PostAsync(url, content);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // Envía una respuesta por el canal correcto (comentario o DM). false si no hay token o falla.
        private static async Task<bool> EnviarRespuestaCanal(SqliteConnection c, int marcaId, string canal, string externoId, string autorId, string texto)
        {
            var plRed = canal.StartsWith("ig") ? "instagram" : "facebook";
            var (igId, pageId, tk) = LeerCuenta(c, marcaId, plRed);
            if (string.IsNullOrWhiteSpace(tk)) return false;
            try
            {
                if (canal == "ig_comment" && !string.IsNullOrEmpty(externoId))
                { await GraphPost($"{externoId}/replies", new() { ["message"] = texto, ["access_token"] = tk }); return true; }
                if (canal == "fb_comment" && !string.IsNullOrEmpty(externoId))
                { await GraphPost($"{externoId}/comments", new() { ["message"] = texto, ["access_token"] = tk }); return true; }
                if (canal == "ig_dm" && !string.IsNullOrEmpty(autorId))
                    return await GraphPostRawJson($"{igId}/messages",
                        $"{{\"recipient\":{{\"id\":\"{autorId}\"}},\"message\":{{\"text\":{System.Text.Json.JsonSerializer.Serialize(texto)}}}}}", tk);
                if (canal == "fb_msg" && !string.IsNullOrEmpty(autorId))
                    return await GraphPostRawJson($"{pageId}/messages",
                        $"{{\"recipient\":{{\"id\":\"{autorId}\"}},\"message\":{{\"text\":{System.Text.Json.JsonSerializer.Serialize(texto)}}}}}", tk);
            }
            catch { return false; }
            return false;
        }

        // Núcleo: decide auto-respuesta vs escalado según horario y capacidad de la IA.
        private static async Task ProcesarMensajeAsync(int mensajeId)
        {
            using var c = Db.Conn();
            int marcaId; string canal, texto, externoId, autorId;
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "SELECT MarcaId,Canal,COALESCE(Texto,''),COALESCE(ExternoId,''),COALESCE(AutorId,'') FROM MensajesRed WHERE Id=$id";
                cmd.Parameters.AddWithValue("$id", mensajeId);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return;
                marcaId = Db.I(r, 0); canal = Db.S(r, 1); texto = Db.S(r, 2); externoId = Db.S(r, 3); autorId = Db.S(r, 4);
            }
            var cfg = LeerBandejaConfig(c, marcaId);
            var now = DateTime.Now;
            bool enHorario = EnHorario(cfg.ini, cfg.fin, cfg.dias, now);

            bool puede = false; string respuesta = "";
            if (cfg.auto && cfg.activa)
            {
                var res = await ClasificarMensajeAsync(cfg.kb, cfg.palabras, texto);
                puede = res.puede; respuesta = res.respuesta;
            }

            string estado; int requiereHumano; string? avisarEn; int respEnviada = 0; string respIa = respuesta;
            if (puede)
            {
                respEnviada = await EnviarRespuestaCanal(c, marcaId, canal, externoId, autorId, respuesta) ? 1 : 0;
                estado = "auto_respondido"; requiereHumano = 0; avisarEn = null;
            }
            else
            {
                requiereHumano = 1;
                if (enHorario) { estado = "escalado"; avisarEn = now.ToString("yyyy-MM-dd HH:mm:ss"); }
                else
                {
                    await EnviarRespuestaCanal(c, marcaId, canal, externoId, autorId, cfg.acuse); // acuse al cliente
                    estado = "escalado";
                    avisarEn = ProximoInicioHorario(cfg.ini, cfg.dias, now).ToString("yyyy-MM-dd HH:mm:ss");
                    respIa = cfg.acuse;
                }
            }
            using (var upd = c.CreateCommand())
            {
                upd.CommandText = @"UPDATE MensajesRed SET Estado=$e,RequiereHumano=$rh,RespuestaIa=$ri,RespuestaEnviada=$re,
                                    AvisarEn=$av,Avisado=0,ActualizadoEn=datetime('now','localtime') WHERE Id=$id";
                upd.Parameters.AddWithValue("$e", estado);
                upd.Parameters.AddWithValue("$rh", requiereHumano);
                upd.Parameters.AddWithValue("$ri", (object?)respIa ?? DBNull.Value);
                upd.Parameters.AddWithValue("$re", respEnviada);
                upd.Parameters.AddWithValue("$av", (object?)avisarEn ?? DBNull.Value);
                upd.Parameters.AddWithValue("$id", mensajeId);
                upd.ExecuteNonQuery();
            }
        }

        // Inserta (idempotente por Canal+ExternoId) y procesa si es nuevo.
        private static async Task GuardarYProcesarAsync(int marcaId, string canal, string externoId,
            string autorNombre, string autorId, string texto, string postExternoId, string permalink)
        {
            if (string.IsNullOrWhiteSpace(texto)) return;
            int id; string estado;
            using (var c = Db.Conn())
            {
                using (var ins = c.CreateCommand())
                {
                    ins.CommandText = @"INSERT OR IGNORE INTO MensajesRed
                        (MarcaId,Canal,ExternoId,PostExternoId,AutorNombre,AutorId,Texto,Estado,Permalink,RecibidoEn,ActualizadoEn)
                        VALUES ($m,$c,$e,$pe,$an,$ai,$t,'nuevo',$pl,datetime('now','localtime'),datetime('now','localtime'))";
                    ins.Parameters.AddWithValue("$m", marcaId);
                    ins.Parameters.AddWithValue("$c", canal);
                    ins.Parameters.AddWithValue("$e", (object?)externoId ?? DBNull.Value);
                    ins.Parameters.AddWithValue("$pe", (object?)postExternoId ?? DBNull.Value);
                    ins.Parameters.AddWithValue("$an", (object?)autorNombre ?? DBNull.Value);
                    ins.Parameters.AddWithValue("$ai", (object?)autorId ?? DBNull.Value);
                    ins.Parameters.AddWithValue("$t", texto);
                    ins.Parameters.AddWithValue("$pl", (object?)permalink ?? DBNull.Value);
                    ins.ExecuteNonQuery();
                }
                using (var q = c.CreateCommand())
                {
                    q.CommandText = "SELECT Id,Estado FROM MensajesRed WHERE Canal=$c AND ExternoId=$e";
                    q.Parameters.AddWithValue("$c", canal);
                    q.Parameters.AddWithValue("$e", (object?)externoId ?? DBNull.Value);
                    using var r = q.ExecuteReader();
                    if (!r.Read()) return;
                    id = Db.I(r, 0); estado = Db.S(r, 1);
                }
            }
            if (estado == "nuevo") await ProcesarMensajeAsync(id);
        }

        private static int MarcaPorCuentaExterna(string extId)
        {
            if (!string.IsNullOrWhiteSpace(extId))
            {
                using var c = Db.Conn();
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT MarcaId FROM CuentasRed WHERE PageId=$x OR IgUserId=$x LIMIT 1";
                cmd.Parameters.AddWithValue("$x", extId);
                var o = cmd.ExecuteScalar();
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o);
            }
            using (var c = Db.Conn())
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT Id FROM Marcas WHERE Activa=1 ORDER BY Id LIMIT 1";
                var o = cmd.ExecuteScalar();
                return (o != null && o != DBNull.Value) ? Convert.ToInt32(o) : 1;
            }
        }

        // Parser tolerante del webhook de Meta (comentarios IG/FB y mensajes IG-DM/Messenger).
        private static async Task ProcesarWebhookMetaAsync(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return;
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;
            string obj = root.TryGetProperty("object", out var o) ? (o.GetString() ?? "") : "";
            if (!root.TryGetProperty("entry", out var entries) || entries.ValueKind != System.Text.Json.JsonValueKind.Array) return;

            foreach (var entry in entries.EnumerateArray())
            {
                string entryId = entry.TryGetProperty("id", out var eid) ? (eid.GetString() ?? "") : "";
                int marcaId = MarcaPorCuentaExterna(entryId);

                if (entry.TryGetProperty("changes", out var changes) && changes.ValueKind == System.Text.Json.JsonValueKind.Array)
                    foreach (var ch in changes.EnumerateArray())
                    {
                        var field = ch.TryGetProperty("field", out var f) ? (f.GetString() ?? "") : "";
                        if (!ch.TryGetProperty("value", out var v)) continue;
                        bool esComentario = field == "comments" ||
                            (field == "feed" && v.TryGetProperty("item", out var it) && it.GetString() == "comment");
                        if (!esComentario) continue;
                        string cid = v.TryGetProperty("comment_id", out var c1) ? (c1.GetString() ?? "")
                                   : (v.TryGetProperty("id", out var c2) ? (c2.GetString() ?? "") : "");
                        string texto = v.TryGetProperty("message", out var m1) ? (m1.GetString() ?? "")
                                     : (v.TryGetProperty("text", out var m2) ? (m2.GetString() ?? "") : "");
                        string autorN = "", autorI = "";
                        if (v.TryGetProperty("from", out var fr))
                        {
                            if (fr.TryGetProperty("name", out var fn)) autorN = fn.GetString() ?? "";
                            if (fr.TryGetProperty("username", out var fu)) autorN = fu.GetString() ?? autorN;
                            if (fr.TryGetProperty("id", out var fi)) autorI = fi.GetString() ?? "";
                        }
                        string postExt = v.TryGetProperty("post_id", out var pid) ? (pid.GetString() ?? "")
                                       : (v.TryGetProperty("media", out var md) && md.TryGetProperty("id", out var mi) ? (mi.GetString() ?? "") : "");
                        string canal = obj == "instagram" ? "ig_comment" : "fb_comment";
                        await GuardarYProcesarAsync(marcaId, canal, cid, autorN, autorI, texto, postExt, "");
                    }

                if (entry.TryGetProperty("messaging", out var msgs) && msgs.ValueKind == System.Text.Json.JsonValueKind.Array)
                    foreach (var mg in msgs.EnumerateArray())
                    {
                        string autorI = mg.TryGetProperty("sender", out var sn) && sn.TryGetProperty("id", out var si) ? (si.GetString() ?? "") : "";
                        string texto = mg.TryGetProperty("message", out var mm) && mm.TryGetProperty("text", out var mt) ? (mt.GetString() ?? "") : "";
                        string mid = mg.TryGetProperty("message", out var mm2) && mm2.TryGetProperty("mid", out var midp) ? (midp.GetString() ?? "") : "";
                        if (string.IsNullOrWhiteSpace(texto)) continue;
                        string canal = obj == "instagram" ? "ig_dm" : "fb_msg";
                        string ext = string.IsNullOrEmpty(mid) ? ("msg-" + autorI + "-" + texto.Length) : mid;
                        await GuardarYProcesarAsync(marcaId, canal, ext, "", autorI, texto, "", "");
                    }
            }
        }

        private static string GeminiKey() =>
            Environment.GetEnvironmentVariable("SG_GEMINI_KEY", EnvironmentVariableTarget.Machine)
            ?? Environment.GetEnvironmentVariable("SG_GEMINI_KEY") ?? "";

        // Gemini Vision: ¿la imagen muestra un fuste/arbol de montura de MADERA? true = si.
        private static async Task<bool> DetectaMaderaAsync(string base64, string mime)
        {
            var key = GeminiKey();
            if (string.IsNullOrEmpty(key)) return false;
            var data = LimpiarBase64(base64);
            var body = new
            {
                contents = new[] { new { parts = new object[] {
                    new { text = "Analiza la imagen de una marca que fabrica fustes/arboles de montura de PLASTICO (polietileno). " +
                                 "El arbol/fuste (estructura base de la silla) o el taller que se ve, ¿es principalmente de MADERA? " +
                                 "Responde UNICAMENTE 'SI' o 'NO'. Ante la duda o si no hay madera clara, responde 'NO'." },
                    new { inlineData = new { mimeType = string.IsNullOrEmpty(mime) ? "image/png" : mime, data } }
                } } }
            };
            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, url)
            { Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json") };
            reqMsg.Headers.Add("x-goog-api-key", key);
            var resp = await _http.SendAsync(reqMsg);
            if (!resp.IsSuccessStatusCode) return false;
            var txt = await resp.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(txt);
            var ans = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
            ans = ans.Trim().ToUpperInvariant();
            return ans.StartsWith("SI") || ans.StartsWith("YES");
        }

        private static string RutaRefsDir(int marcaId, string? categoria = null)
        {
            var baseDir = Path.Combine(RutaContenidoDir(), $"marca-{marcaId}", "ref");
            var cat = (categoria ?? "").Trim().ToLowerInvariant();
            var d = cat == "creatividad" ? Path.Combine(baseDir, "creatividad") : baseDir; // producto = raiz
            Directory.CreateDirectory(d);
            return d;
        }

        private static bool NombreRefSeguro(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains("..")) return false;
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            var ext = Path.GetExtension(name).ToLowerInvariant();
            return ext is ".png" or ".jpg" or ".jpeg" or ".webp";
        }

        /// <summary>
        /// Lee hasta max imagenes de referencia, las REDIMENSIONA a ~1024 px JPEG
        /// (para que el payload a n8n sea liviano) y las devuelve como {mime, data(base64)}.
        /// </summary>
        private static List<object> LeerReferenciasParaPayload(int marcaId, int max, string[]? seleccion = null, string? categoria = null)
        {
            var list = new List<object>();
            try
            {
                var dir = RutaRefsDir(marcaId, categoria);
                IEnumerable<string> archivos = (seleccion != null && seleccion.Length > 0)
                    ? seleccion.Where(NombreRefSeguro).Select(n => Path.Combine(dir, n)).Where(File.Exists)
                    : Directory.GetFiles(dir).OrderBy(x => x);
                foreach (var f in archivos.Take(max))
                {
                    var ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp")) continue;
                    try
                    {
                        var jpg = RedimensionarJpeg(File.ReadAllBytes(f), 1024, 80);
                        list.Add(new { mime = "image/jpeg", data = Convert.ToBase64String(jpg) });
                    }
                    catch { }
                }
            }
            catch { }
            return list;
        }

        /// <summary>Redimensiona una imagen a un lado maximo y la re-codifica como JPEG.</summary>
        private static byte[] RedimensionarJpeg(byte[] datos, int maxLado, int calidad)
        {
            using var image = Image.Load(datos);
            int mayor = Math.Max(image.Width, image.Height);
            if (mayor > maxLado)
            {
                double r = (double)maxLado / mayor;
                image.Mutate(x => x.Resize((int)Math.Round(image.Width * r), (int)Math.Round(image.Height * r)));
            }
            using var ms = new MemoryStream();
            image.Save(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = calidad });
            return ms.ToArray();
        }

        private static string AsegurarPublicToken(SqliteConnection c, int postId)
        {
            using (var q = c.CreateCommand())
            {
                q.CommandText = "SELECT PublicToken FROM Posts WHERE Id=$id";
                q.Parameters.AddWithValue("$id", postId);
                var v = q.ExecuteScalar()?.ToString();
                if (!string.IsNullOrWhiteSpace(v)) return v!;
            }
            var tok = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            using (var u = c.CreateCommand())
            {
                u.CommandText = "UPDATE Posts SET PublicToken=$t WHERE Id=$id";
                u.Parameters.AddWithValue("$t", tok);
                u.Parameters.AddWithValue("$id", postId);
                u.ExecuteNonQuery();
            }
            return tok;
        }

        private static (string igId, string pageId, string token) LeerCuenta(SqliteConnection c, int marcaId, string plataforma)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT COALESCE(IgUserId,''), COALESCE(PageId,''), COALESCE(TokenRef,'')
                                FROM CuentasRed WHERE MarcaId=$m AND LOWER(Plataforma)=$p AND Activa=1 LIMIT 1";
            cmd.Parameters.AddWithValue("$m", marcaId);
            cmd.Parameters.AddWithValue("$p", plataforma.ToLowerInvariant());
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return ("", "", "");
            return (Db.S(r, 0), Db.S(r, 1), Db.S(r, 2));
        }

        private static void RegistrarPublicacion(SqliteConnection c, int postId, string plataforma, string idExterno, string permalink)
        {
            using var ins = c.CreateCommand();
            // Reemplaza cualquier registro previo de esa plataforma para el post (evita duplicados al re-vincular)
            ins.CommandText = @"DELETE FROM Publicaciones WHERE PostId=$p AND LOWER(Plataforma)=LOWER($pl);
                                INSERT INTO Publicaciones (PostId, Plataforma, Estado, IdExterno, Permalink, FechaPublicacion)
                                VALUES ($p, $pl, 'publicado', $ext, $link, datetime('now','localtime'))";
            ins.Parameters.AddWithValue("$p", postId);
            ins.Parameters.AddWithValue("$pl", plataforma);
            ins.Parameters.AddWithValue("$ext", string.IsNullOrWhiteSpace(idExterno) ? (object)DBNull.Value : idExterno);
            ins.Parameters.AddWithValue("$link", (object?)permalink ?? DBNull.Value);
            ins.ExecuteNonQuery();
        }

        private const string GraphBase = "https://graph.facebook.com/v21.0/";

        private static async Task<string> GraphPost(string path, Dictionary<string, string> form)
        {
            using var content = new FormUrlEncodedContent(form);
            var resp = await _http.PostAsync(GraphBase + path, content);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Graph {(int)resp.StatusCode}: {body}");
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("id", out var idp)) return idp.GetString() ?? "";
            if (doc.RootElement.TryGetProperty("post_id", out var pp)) return pp.GetString() ?? "";
            return "";
        }

        // POST que devuelve el JSON completo (para leer varios campos, p.ej. post_id de Facebook)
        private static async Task<System.Text.Json.JsonElement> GraphPostDoc(string path, Dictionary<string, string> form)
        {
            using var content = new FormUrlEncodedContent(form);
            var resp = await _http.PostAsync(GraphBase + path, content);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Graph {(int)resp.StatusCode}: {body}");
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }

        // GET a la Graph API; query sin el access_token (se agrega aquí)
        private static async Task<System.Text.Json.JsonElement> GraphGet(string path, string query, string token)
        {
            var url = $"{GraphBase}{path}?{query}{(string.IsNullOrEmpty(query) ? "" : "&")}access_token={Uri.EscapeDataString(token)}";
            var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) throw new Exception($"Graph {(int)resp.StatusCode}: {body}");
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }

        // Convierte la respuesta de /insights (data[].name + values[0].value) a un diccionario
        private static Dictionary<string, long> ParseInsights(System.Text.Json.JsonElement el)
        {
            var d = new Dictionary<string, long>();
            if (el.TryGetProperty("data", out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
                foreach (var m in arr.EnumerateArray())
                {
                    var name = m.TryGetProperty("name", out var nm) ? (nm.GetString() ?? "") : "";
                    if (string.IsNullOrEmpty(name)) continue;
                    if (m.TryGetProperty("values", out var vals) && vals.ValueKind == System.Text.Json.JsonValueKind.Array)
                        foreach (var val in vals.EnumerateArray())
                        {
                            if (val.TryGetProperty("value", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.Number)
                                d[name] = v.GetInt64();
                            break; // sólo el primer valor
                        }
                }
            return d;
        }

        // Métricas de una publicación de Instagram (por media id)
        private static async Task<object> LeerMetricasIG(string token, string mediaId)
        {
            long likes = 0, comentarios = 0, alcance = 0, guardados = 0, interacciones = 0;
            string tipo = "", permalink = "";
            try
            {
                var f = await GraphGet(mediaId, "fields=like_count,comments_count,media_type,permalink", token);
                if (f.TryGetProperty("like_count", out var lc) && lc.ValueKind == System.Text.Json.JsonValueKind.Number) likes = lc.GetInt64();
                if (f.TryGetProperty("comments_count", out var cc) && cc.ValueKind == System.Text.Json.JsonValueKind.Number) comentarios = cc.GetInt64();
                if (f.TryGetProperty("media_type", out var mt)) tipo = mt.GetString() ?? "";
                if (f.TryGetProperty("permalink", out var pk)) permalink = pk.GetString() ?? "";
            }
            catch { }
            try
            {
                var ins = await GraphGet($"{mediaId}/insights", "metric=reach,saved,total_interactions", token);
                var d = ParseInsights(ins);
                d.TryGetValue("reach", out alcance);
                d.TryGetValue("saved", out guardados);
                d.TryGetValue("total_interactions", out interacciones);
            }
            catch
            {
                try
                {
                    var ins2 = await GraphGet($"{mediaId}/insights", "metric=reach,saved", token);
                    var d2 = ParseInsights(ins2);
                    d2.TryGetValue("reach", out alcance);
                    d2.TryGetValue("saved", out guardados);
                }
                catch { }
            }
            return new { alcance, likes, comentarios, guardados, interacciones, tipo, permalink };
        }

        // Métricas de una publicación de Facebook (por post id)
        private static async Task<object> LeerMetricasFB(string token, string postId)
        {
            long likes = 0, comentarios = 0, compartidos = 0, impresiones = 0, alcance = 0;
            try
            {
                var f = await GraphGet(postId, "fields=likes.summary(true).limit(0),comments.summary(true).limit(0),shares", token);
                if (f.TryGetProperty("likes", out var lk) && lk.TryGetProperty("summary", out var ls) && ls.TryGetProperty("total_count", out var ltc)) likes = ltc.GetInt64();
                if (f.TryGetProperty("comments", out var cm) && cm.TryGetProperty("summary", out var cs) && cs.TryGetProperty("total_count", out var ctc)) comentarios = ctc.GetInt64();
                if (f.TryGetProperty("shares", out var sh) && sh.TryGetProperty("count", out var scc)) compartidos = scc.GetInt64();
            }
            catch { }
            try
            {
                var ins = await GraphGet($"{postId}/insights", "metric=post_impressions,post_impressions_unique", token);
                var d = ParseInsights(ins);
                d.TryGetValue("post_impressions", out impresiones);
                d.TryGetValue("post_impressions_unique", out alcance);
            }
            catch { }
            return new { alcance, impresiones, likes, comentarios, compartidos };
        }

        private static async Task<(string id, string permalink)> PublicarInstagram(string igId, string token, List<string> urls, string caption)
        {
            if (string.IsNullOrWhiteSpace(igId)) throw new Exception("Falta el ID de la cuenta de Instagram Business.");
            string creationId;
            if (urls.Count == 1)
            {
                creationId = await GraphPost($"{igId}/media", new() { ["image_url"] = urls[0], ["caption"] = caption, ["access_token"] = token });
            }
            else
            {
                var children = new List<string>();
                foreach (var u in urls)
                {
                    var cid = await GraphPost($"{igId}/media", new() { ["image_url"] = u, ["is_carousel_item"] = "true", ["access_token"] = token });
                    children.Add(cid);
                }
                creationId = await GraphPost($"{igId}/media", new() { ["media_type"] = "CAROUSEL", ["children"] = string.Join(",", children), ["caption"] = caption, ["access_token"] = token });
            }
            var mediaId = await GraphPost($"{igId}/media_publish", new() { ["creation_id"] = creationId, ["access_token"] = token });
            string permalink = $"IG {mediaId}";
            try
            {
                var resp = await _http.GetAsync($"{GraphBase}{mediaId}?fields=permalink&access_token={Uri.EscapeDataString(token)}");
                var body = await resp.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("permalink", out var pl)) permalink = pl.GetString() ?? permalink;
            }
            catch { }
            return (mediaId, permalink);
        }

        private static async Task<(string id, string permalink)> PublicarFacebook(string pageId, string token, List<string> urls, string caption)
        {
            if (string.IsNullOrWhiteSpace(pageId)) throw new Exception("Falta el ID de la Pagina de Facebook.");
            if (urls.Count == 1)
            {
                // /photos devuelve id (foto) y post_id (historia de la página): para métricas usamos post_id
                var el = await GraphPostDoc($"{pageId}/photos", new() { ["url"] = urls[0], ["caption"] = caption, ["access_token"] = token });
                string pid = el.TryGetProperty("post_id", out var pp) ? (pp.GetString() ?? "")
                           : (el.TryGetProperty("id", out var ii) ? (ii.GetString() ?? "") : "");
                return (pid, $"https://www.facebook.com/{pid}");
            }
            var media = new List<string>();
            foreach (var u in urls)
            {
                var fbid = await GraphPost($"{pageId}/photos", new() { ["url"] = u, ["published"] = "false", ["access_token"] = token });
                media.Add(fbid);
            }
            var attached = "[" + string.Join(",", media.Select(m => $"{{\"media_fbid\":\"{m}\"}}")) + "]";
            var postId = await GraphPost($"{pageId}/feed", new() { ["message"] = caption, ["attached_media"] = attached, ["access_token"] = token });
            return (postId, $"https://www.facebook.com/{postId}");
        }

        private static object? LeerMarcaDict(int marcaId)
        {
            using var c = Db.Conn();
            using var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT Id, Nombre, Handle, Idiomas, VozTono, GuiaEstilo, HashtagsBase, Pilares
                                FROM Marcas WHERE Id=$id";
            cmd.Parameters.AddWithValue("$id", marcaId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new
            {
                id = Db.I(r, 0), nombre = Db.S(r, 1), handle = Db.S(r, 2),
                idiomas = Db.S(r, 3), voz = Db.S(r, 4), guiaEstilo = Db.S(r, 5),
                hashtagsBase = Db.S(r, 6), pilares = Db.S(r, 7)
            };
        }
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
    record ContOrdenReq(int[]? Ids);
    record ContPublicarReq(string[]? Plataformas, string? Permalink);
    record ContCuentaEditarReq(string? NombreCuenta, string? IgUserId, string? PageId, string? Token, string? ModoPublicacion);
    record ContVincularReq(string Plataforma, string IdExterno, string? Enlace);
    record ContSimularMsgReq(string? Canal, string? AutorNombre, string Texto, string? PostExternoId);
    record ContResponderMsgReq(string Texto);
    record ContEstadoMsgReq(string Estado);
    record ContBandejaConfigReq(string? HorarioInicio, string? HorarioFin, string? DiasLaborales,
        string? BaseConocimiento, string? PlantillaAcuse, bool? AutoRespuesta, string? PalabrasHumano, bool? Activa);
    record ContVideoReq(string? Tipo, double? SegundosPorImagen, string? Gancho, string? Motor);
    record ContPublicarAutoReq(string[]? Plataformas);

    // DTOs de generación IA
    record GenerarIdeasReq(int MarcaId, string[]? Pilares, int? Cantidad, bool? UsarBase);
    record GenerarPostReq(string? Formato, string? PromptImagen, int? NImagenes, string? Modo, string[]? Referencias, string[]? ReferenciasCreatividad, int? AssetIdBase);
    record HookIdeaItem(string? Titulo, string? Descripcion, string? Pilar);
    record HookIdeasReq(int MarcaId, HookIdeaItem[]? Ideas);
    record CaptionDto(string? Es, string? En);
    record HookAssetItem(int? Orden, string? Tipo, string? DataBase64, string? Mime, string? Modelo, string? Prompt);
    record HookResultadoReq(string? JobId, int PostId, string? Estado, CaptionDto? Caption,
        string[]? Hashtags, HookAssetItem[]? Assets, string? Modelo, string? Error);
}
