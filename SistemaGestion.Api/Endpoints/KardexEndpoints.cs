using SistemaGestion.Api.Auth;

namespace SistemaGestion.Api.Endpoints
{
    public static class KardexEndpoints
    {
        public static void MapKardexEndpoints(this WebApplication app)
        {
            app.MapGet("/api/kardex/b", (string? codigo, string? desde, string? hasta) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                var where = new List<string> { "TipoKardex='PRODUCTO' OR TipoKardex IS NULL" };
                if (!string.IsNullOrWhiteSpace(codigo)) { where.Add("CodigoProducto=$cod"); cmd.Parameters.AddWithValue("$cod", codigo); }
                if (!string.IsNullOrWhiteSpace(desde))  { where.Add("FechaHora>=$des"); cmd.Parameters.AddWithValue("$des", desde); }
                if (!string.IsNullOrWhiteSpace(hasta))  { where.Add("FechaHora<=$has"); cmd.Parameters.AddWithValue("$has", hasta + " 23:59:59"); }
                cmd.CommandText = $"SELECT FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario FROM MovimientosInventario WHERE {string.Join(" AND ", where)} ORDER BY IdMovimiento DESC LIMIT 100";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new { fecha=Db.S(r,0), codigoProducto=Db.S(r,1), tipo=Db.S(r,2),
                        concepto=Db.S(r,3), cantidad=Db.D(r,4), saldoAnt=Db.D(r,5),
                        saldo=Db.D(r,6), referencia=Db.S(r,7), usuario=Db.S(r,8) });
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapGet("/api/kardex/a", (string? codigo, string? desde, string? hasta) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                var where = new List<string> { "TipoKardex='MATERIA_PRIMA'" };
                if (!string.IsNullOrWhiteSpace(codigo)) { where.Add("CodigoProducto=$cod"); cmd.Parameters.AddWithValue("$cod", codigo); }
                if (!string.IsNullOrWhiteSpace(desde))  { where.Add("FechaHora>=$des"); cmd.Parameters.AddWithValue("$des", desde); }
                if (!string.IsNullOrWhiteSpace(hasta))  { where.Add("FechaHora<=$has"); cmd.Parameters.AddWithValue("$has", hasta + " 23:59:59"); }
                cmd.CommandText = $"SELECT FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,CostoUnitario,Referencia,Usuario FROM MovimientosInventario WHERE {string.Join(" AND ", where)} ORDER BY IdMovimiento DESC LIMIT 100";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new { fecha=Db.S(r,0), codigoProducto=Db.S(r,1), tipo=Db.S(r,2),
                        concepto=Db.S(r,3), cantidad=Db.D(r,4), saldoAnt=Db.D(r,5),
                        saldo=Db.D(r,6), costoUnitario=Db.D(r,7), referencia=Db.S(r,8), usuario=Db.S(r,9) });
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapPost("/api/kardex/b/movimiento", (KardexMovRequest req, HttpContext ctx) =>
            {
                if (!TokenService.PuedeEscribirKardex(ctx.User)) return Results.Forbid();
                if (req.Tipo == "AJUSTE" && !TokenService.PuedeAjustarKardex(ctx.User)) return Results.Forbid();
                if (string.IsNullOrWhiteSpace(req.Codigo)) return Results.BadRequest(new { error = "Código requerido." });
                if (req.Cantidad <= 0) return Results.BadRequest(new { error = "Cantidad debe ser mayor que cero." });

                var usuario = TokenService.ObtenerNombreUsuario(ctx.User) + " [API-MOVIL]";
                try
                {
                    using var c = Db.Conn();
                    using var tx = c.BeginTransaction();

                    var cmdS = c.CreateCommand(); cmdS.Transaction = tx;
                    cmdS.CommandText = "SELECT Existencia FROM Inventario WHERE Codigo=$cod";
                    cmdS.Parameters.AddWithValue("$cod", req.Codigo);
                    var objStock = cmdS.ExecuteScalar();
                    if (objStock == null) return Results.BadRequest(new { error = $"Producto '{req.Codigo}' no existe." });
                    decimal sa = (decimal)(double)objStock;

                    decimal ns;
                    if (req.Tipo == "AJUSTE") { ns = req.Cantidad; }
                    else if (req.Tipo == "ENTRADA") { ns = sa + req.Cantidad; }
                    else
                    {
                        ns = sa - req.Cantidad;
                        if (ns < 0) return Results.BadRequest(new { error = $"Stock insuficiente. Disponible: {sa:N2}" });
                    }

                    var tipo = req.Tipo == "AJUSTE" ? (ns > sa ? "ENTRADA" : "SALIDA") : req.Tipo;
                    var cant = req.Tipo == "AJUSTE" ? Math.Abs(ns - sa) : req.Cantidad;

                    var cmdI = c.CreateCommand(); cmdI.Transaction = tx;
                    cmdI.CommandText = @"INSERT INTO MovimientosInventario
                        (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario)
                        VALUES ($fh,$cp,$tm,$co,$ca,$sa,$ns,$re,$us,'PRODUCTO',$cu)";
                    cmdI.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmdI.Parameters.AddWithValue("$cp", req.Codigo);
                    cmdI.Parameters.AddWithValue("$tm", tipo);
                    cmdI.Parameters.AddWithValue("$co", req.Concepto ?? $"{req.Tipo} portal móvil");
                    cmdI.Parameters.AddWithValue("$ca", (double)cant);
                    cmdI.Parameters.AddWithValue("$sa", (double)sa);
                    cmdI.Parameters.AddWithValue("$ns", (double)ns);
                    cmdI.Parameters.AddWithValue("$re", req.Referencia ?? "");
                    cmdI.Parameters.AddWithValue("$us", usuario);
                    cmdI.Parameters.AddWithValue("$cu", (double)req.CostoUnitario);
                    cmdI.ExecuteNonQuery();

                    var cmdU = c.CreateCommand(); cmdU.Transaction = tx;
                    cmdU.CommandText = "UPDATE Inventario SET Existencia=$ns, UltimaActualz=$f WHERE Codigo=$cod";
                    cmdU.Parameters.AddWithValue("$ns", (double)ns);
                    cmdU.Parameters.AddWithValue("$f", DateTime.Today.ToString("yyyy-MM-dd"));
                    cmdU.Parameters.AddWithValue("$cod", req.Codigo);
                    cmdU.ExecuteNonQuery();
                    tx.Commit();

                    AuthEndpoints.RegistrarAuditoria(c, $"KARDEX_B_{req.Tipo}_MOVIL", "MovimientosInventario",
                        $"{req.Codigo} {req.Tipo} {cant:N2} por {usuario}");

                    return Results.Ok(new { mensaje = $"{req.Tipo} registrada.", nuevoSaldo = ns });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[KARDEX-B ERROR] {ex}");
                    return Results.Problem("Error al registrar el movimiento.", statusCode: 500);
                }
            }).RequireAuthorization();

            app.MapPost("/api/kardex/a/movimiento", (KardexAMovRequest req, HttpContext ctx) =>
            {
                if (!TokenService.PuedeEscribirKardex(ctx.User)) return Results.Forbid();
                if (req.Tipo == "AJUSTE" && !TokenService.PuedeAjustarKardex(ctx.User)) return Results.Forbid();
                if (req.IdInsumo <= 0) return Results.BadRequest(new { error = "ID de insumo requerido." });
                if (req.Cantidad <= 0) return Results.BadRequest(new { error = "Cantidad mayor que cero." });

                var usuario = TokenService.ObtenerNombreUsuario(ctx.User) + " [API-MOVIL]";
                try
                {
                    using var c = Db.Conn();
                    using var tx = c.BeginTransaction();

                    var cmdS = c.CreateCommand(); cmdS.Transaction = tx;
                    cmdS.CommandText = "SELECT CodigoInterno,StockActual,CostoPromedio FROM ProductosProveedor WHERE Id=$id";
                    cmdS.Parameters.AddWithValue("$id", req.IdInsumo);
                    using var r = cmdS.ExecuteReader();
                    if (!r.Read()) return Results.BadRequest(new { error = "Insumo no encontrado." });
                    string cod = Db.S(r,0); decimal sa = Db.D(r,1); decimal cpp = Db.D(r,2);
                    r.Close();

                    decimal ns, nuevoCpp = cpp;
                    string tipo;
                    decimal cant;
                    if (req.Tipo == "AJUSTE") { ns = req.Cantidad; cant = Math.Abs(ns-sa); tipo = ns>=sa?"ENTRADA":"SALIDA"; }
                    else if (req.Tipo == "ENTRADA")
                    {
                        ns = sa + req.Cantidad; cant = req.Cantidad;
                        nuevoCpp = sa > 0 && cpp > 0
                            ? (sa * cpp + req.Cantidad * req.CostoUnitario) / ns
                            : req.CostoUnitario > 0 ? req.CostoUnitario : cpp;
                        tipo = "ENTRADA";
                    }
                    else
                    {
                        ns = sa - req.Cantidad; cant = req.Cantidad;
                        if (ns < 0) return Results.BadRequest(new { error = $"Stock insuficiente. Disponible: {sa:N2}" });
                        tipo = "SALIDA";
                    }

                    var cmdI = c.CreateCommand(); cmdI.Transaction = tx;
                    cmdI.CommandText = @"INSERT INTO MovimientosInventario
                        (FechaHora,CodigoProducto,TipoMovimiento,Concepto,Cantidad,SaldoAnterior,NuevoSaldo,Referencia,Usuario,TipoKardex,CostoUnitario)
                        VALUES ($fh,$cp,$tm,$co,$ca,$sa,$ns,$re,$us,'MATERIA_PRIMA',$cu)";
                    cmdI.Parameters.AddWithValue("$fh", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmdI.Parameters.AddWithValue("$cp", cod);
                    cmdI.Parameters.AddWithValue("$tm", tipo);
                    cmdI.Parameters.AddWithValue("$co", req.Concepto ?? $"{req.Tipo} portal móvil");
                    cmdI.Parameters.AddWithValue("$ca", (double)cant);
                    cmdI.Parameters.AddWithValue("$sa", (double)sa);
                    cmdI.Parameters.AddWithValue("$ns", (double)ns);
                    cmdI.Parameters.AddWithValue("$re", req.Referencia ?? "");
                    cmdI.Parameters.AddWithValue("$us", usuario);
                    cmdI.Parameters.AddWithValue("$cu", (double)(req.CostoUnitario > 0 ? req.CostoUnitario : cpp));
                    cmdI.ExecuteNonQuery();

                    var cmdU = c.CreateCommand(); cmdU.Transaction = tx;
                    cmdU.CommandText = "UPDATE ProductosProveedor SET StockActual=$ns,CostoPromedio=$cpp WHERE Id=$id";
                    cmdU.Parameters.AddWithValue("$ns", (double)ns);
                    cmdU.Parameters.AddWithValue("$cpp", (double)nuevoCpp);
                    cmdU.Parameters.AddWithValue("$id", req.IdInsumo);
                    cmdU.ExecuteNonQuery();
                    tx.Commit();

                    AuthEndpoints.RegistrarAuditoria(c, $"KARDEX_A_{req.Tipo}_MOVIL", "MovimientosInventario",
                        $"{cod} {req.Tipo} {cant:N2} por {usuario}");

                    return Results.Ok(new { mensaje = $"{req.Tipo} registrada.", nuevoSaldo = ns, nuevoCpp = nuevoCpp });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[KARDEX-A ERROR] {ex}");
                    return Results.Problem("Error al registrar el movimiento.", statusCode: 500);
                }
            }).RequireAuthorization();
        }
    }

    public record KardexMovRequest(string Codigo, string Tipo, decimal Cantidad,
        string? Concepto, string? Referencia, decimal CostoUnitario = 0);
    public record KardexAMovRequest(int IdInsumo, string Tipo, decimal Cantidad,
        string? Concepto, string? Referencia, decimal CostoUnitario = 0);
}
