using Microsoft.Data.Sqlite;

namespace SistemaGestion.Api.Endpoints
{
    public static class ComprasEndpoints
    {
        private static string ProximoNumeroCompra()
        {
            string prefix = "C-";
            using var c = Db.Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT CompraN FROM Compras WHERE CompraN LIKE $p ORDER BY CompraN DESC LIMIT 1";
            cmd.Parameters.AddWithValue("$p", prefix + "%");
            var last = cmd.ExecuteScalar()?.ToString();
            int next = 1;
            if (!string.IsNullOrEmpty(last) && last.Length > prefix.Length)
                if (int.TryParse(last.Substring(prefix.Length), out int n)) next = n + 1;
            return $"{prefix}{next:D4}";
        }

        public static void MapComprasEndpoints(this WebApplication app)
        {
            // ── GET proveedores (para dropdown) ──────────────────────────
            app.MapGet("/api/proveedores", (string? q) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Codigo,Descripcion,Telefono1,CiRif
                    FROM Proveedores ORDER BY Descripcion LIMIT 200";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string cod=Db.S(r,0), nom=Db.S(r,1);
                    if (!string.IsNullOrWhiteSpace(q) &&
                        !cod.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                        !nom.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
                    lista.Add(new { codigo=cod, nombre=nom, telefono=Db.S(r,2), rif=Db.S(r,3) });
                }
                return Results.Ok(lista);
            }).RequireAuthorization();

            // ── GET lista compras ────────────────────────────────────────
            app.MapGet("/api/compras", (string? q, string? desde, string? hasta) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT c.CompraN,c.FechaC,c.CodigoP,p.Descripcion,
                    c.TotalC,c.AbonoC,c.FormaDePago
                    FROM Compras c LEFT JOIN Proveedores p ON p.Codigo=c.CodigoP
                    ORDER BY c.FechaC DESC, c.CompraN DESC LIMIT 100";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string num=Db.S(r,0), fecha=Db.S(r,1), nomP=Db.S(r,3);
                    if (!string.IsNullOrWhiteSpace(q) &&
                        !num.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                        !nomP.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrWhiteSpace(desde) && string.Compare(fecha, desde) < 0) continue;
                    if (!string.IsNullOrWhiteSpace(hasta) && string.Compare(fecha, hasta) > 0) continue;
                    decimal total=Db.D(r,4), abono=Db.D(r,5);
                    lista.Add(new { compraN=num, fecha, codigoP=Db.S(r,2), proveedor=nomP,
                        total, abono, saldo=total-abono, formaDePago=Db.S(r,6), pagada=abono>=total });
                }
                return Results.Ok(lista);
            }).RequireAuthorization();

            // ── GET detalle compra ────────────────────────────────────────
            app.MapGet("/api/compras/{id}", (string id) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT c.CompraN,c.FechaC,c.CodigoP,p.Descripcion,
                    c.TotalC,c.AbonoC,c.IvaC,c.FormaDePago,c.Observaciones,c.DescuentoC,c.rowid
                    FROM Compras c LEFT JOIN Proveedores p ON p.Codigo=c.CodigoP
                    WHERE c.CompraN=$n";
                cmd.Parameters.AddWithValue("$n", id);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return Results.NotFound(new { error = "Compra no encontrada." });
                decimal total=Db.D(r,4), abono=Db.D(r,5);
                int rowid=Db.I(r,10);
                var compra = new { compraN=Db.S(r,0), fecha=Db.S(r,1), codigoP=Db.S(r,2),
                    proveedor=Db.S(r,3), total, abono, saldo=total-abono, iva=Db.D(r,6),
                    formaDePago=Db.S(r,7), observaciones=Db.S(r,8), descuento=Db.D(r,9), pagada=abono>=total };
                r.Close();

                var lineas = new List<object>();
                var cmd2 = c.CreateCommand();
                cmd2.CommandText = @"SELECT dc.CodigoI,COALESCE(i.Descripcion,dc.CodigoI),
                    dc.Cantidad,dc.PrecioC
                    FROM DetallesCompra dc LEFT JOIN Inventario i ON i.Codigo=dc.CodigoI
                    WHERE dc.IdCompra=$id";
                cmd2.Parameters.AddWithValue("$id", rowid);
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read())
                {
                    decimal cant=Db.D(r2,2), precio=Db.D(r2,3);
                    lineas.Add(new { codigo=Db.S(r2,0), descripcion=Db.S(r2,1),
                        cantidad=cant, precioUnit=precio, subtotal=cant*precio });
                }
                return Results.Ok(new { compra, lineas });
            }).RequireAuthorization();

            // ── POST crear compra ─────────────────────────────────────────
            app.MapPost("/api/compras", (CompraRequest req, HttpContext ctx) =>
            {
                if (req.Items == null || req.Items.Count == 0)
                    return Results.BadRequest(new { error = "La compra debe tener al menos un ítem." });

                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    string numC = string.IsNullOrWhiteSpace(req.CompraN)
                        ? ProximoNumeroCompra() : req.CompraN.Trim().ToUpper();
                    string fecha = string.IsNullOrWhiteSpace(req.Fecha)
                        ? DateTime.Today.ToString("yyyy-MM-dd") : req.Fecha;

                    // Verificar duplicado
                    var chk = c.CreateCommand(); chk.Transaction = tx;
                    chk.CommandText = "SELECT COUNT(*) FROM Compras WHERE CompraN=$n";
                    chk.Parameters.AddWithValue("$n", numC);
                    if ((long)(chk.ExecuteScalar() ?? 0L) > 0)
                        return Results.Conflict(new { error = $"La compra {numC} ya existe." });

                    // Calcular totales
                    decimal subTotal = 0;
                    var itemsCalc = new List<(string cod, decimal cant, decimal precio)>();
                    foreach (var it in req.Items)
                    {
                        if (string.IsNullOrWhiteSpace(it.Codigo) || it.Cantidad <= 0 || it.PrecioUnit <= 0) continue;
                        subTotal += it.Cantidad * it.PrecioUnit;
                        itemsCalc.Add((it.Codigo.Trim(), it.Cantidad, it.PrecioUnit));
                    }
                    if (itemsCalc.Count == 0)
                        return Results.BadRequest(new { error = "Sin ítems válidos (precio y cantidad requeridos)." });

                    decimal descMonto = subTotal * (req.Descuento / 100m);
                    decimal baseIva   = subTotal - descMonto;
                    decimal ivaMonto  = baseIva * (req.PctIva / 100m);
                    decimal total     = baseIva + ivaMonto;

                    // Insertar cabecera
                    var ins = c.CreateCommand(); ins.Transaction = tx;
                    ins.CommandText = @"INSERT INTO Compras
                        (CompraN,FechaC,CodigoP,DescuentoC,FormaDePago,AbonoC,IvaC,DiasDeCred,TotalC,Observaciones)
                        VALUES($n,$f,$p,$desc,$fp,$ab,$iva,$dc,$tot,$obs)";
                    ins.Parameters.AddWithValue("$n",   numC);
                    ins.Parameters.AddWithValue("$f",   fecha);
                    ins.Parameters.AddWithValue("$p",   req.CodigoProveedor ?? "");
                    ins.Parameters.AddWithValue("$desc",(double)req.Descuento);
                    ins.Parameters.AddWithValue("$fp",  req.FormaDePago ?? "Contado");
                    ins.Parameters.AddWithValue("$ab",  (double)(req.FormaDePago=="Contado"?total:0));
                    ins.Parameters.AddWithValue("$iva", (double)ivaMonto);
                    ins.Parameters.AddWithValue("$dc",  req.DiasCredito);
                    ins.Parameters.AddWithValue("$tot", (double)total);
                    ins.Parameters.AddWithValue("$obs", req.Observaciones ?? "");
                    ins.ExecuteNonQuery();

                    // rowid
                    var rid = c.CreateCommand(); rid.Transaction = tx;
                    rid.CommandText = "SELECT rowid FROM Compras WHERE CompraN=$n";
                    rid.Parameters.AddWithValue("$n", numC);
                    int rowid = (int)(long)(rid.ExecuteScalar() ?? 0L);

                    string usuario = ctx.User?.FindFirst(
                        "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value ?? "portal";

                    // Insertar líneas + actualizar stock
                    foreach (var (cod, cant, precio) in itemsCalc)
                    {
                        var dl = c.CreateCommand(); dl.Transaction = tx;
                        dl.CommandText = @"INSERT OR REPLACE INTO DetallesCompra(IdCompra,CodigoI,Cantidad,PrecioC)
                            VALUES($id,$cod,$cant,$p)";
                        dl.Parameters.AddWithValue("$id",  rowid);
                        dl.Parameters.AddWithValue("$cod", cod);
                        dl.Parameters.AddWithValue("$cant",(double)cant);
                        dl.Parameters.AddWithValue("$p",   (double)precio);
                        dl.ExecuteNonQuery();

                        // Sumar al stock
                        var st = c.CreateCommand(); st.Transaction = tx;
                        st.CommandText = @"UPDATE Inventario
                            SET Existencia=Existencia+$q, CostoActual=$p,
                                UltimaActualz=date('now','localtime')
                            WHERE Codigo=$c";
                        st.Parameters.AddWithValue("$q",(double)cant);
                        st.Parameters.AddWithValue("$p",(double)precio);
                        st.Parameters.AddWithValue("$c",cod);
                        st.ExecuteNonQuery();

                        // Movimiento kardex
                        var mv = c.CreateCommand(); mv.Transaction = tx;
                        mv.CommandText = @"INSERT INTO MovimientosInventario
                            (CodigoProducto,FechaHora,TipoMovimiento,Concepto,Cantidad,NuevoSaldo,Usuario)
                            SELECT $cod,datetime('now','localtime'),'ENTRADA',$conc,$cant,
                                Existencia,$usr FROM Inventario WHERE Codigo=$cod";
                        mv.Parameters.AddWithValue("$cod",  cod);
                        mv.Parameters.AddWithValue("$conc", $"Compra {numC}");
                        mv.Parameters.AddWithValue("$cant", (double)cant);
                        mv.Parameters.AddWithValue("$usr",  usuario);
                        mv.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return Results.Created($"/api/compras/{Uri.EscapeDataString(numC)}",
                        new { compraN=numC, total, iva=ivaMonto });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem(ex.Message);
                }
            }).RequireAuthorization();

            // ── GET página imprimible ─────────────────────────────────────
            app.MapGet("/api/compras/{id}/imprimir", async (string id, HttpContext ctx) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT c.CompraN,c.FechaC,c.CodigoP,p.Descripcion,
                    c.TotalC,c.AbonoC,c.IvaC,c.FormaDePago,c.Observaciones,c.DescuentoC,c.rowid
                    FROM Compras c LEFT JOIN Proveedores p ON p.Codigo=c.CodigoP
                    WHERE c.CompraN=$n";
                cmd.Parameters.AddWithValue("$n", id);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) { ctx.Response.StatusCode=404; return; }
                string numC=Db.S(r,0),fecha=Db.S(r,1),nomP=Db.S(r,3),fp=Db.S(r,7),obs=Db.S(r,8);
                decimal total=Db.D(r,4),abono=Db.D(r,5),iva=Db.D(r,6),desc=Db.D(r,9);
                int rowid=Db.I(r,10);
                r.Close();

                var lineas = new List<(string cod,string desc2,decimal cant,decimal precio)>();
                var cmd2=c.CreateCommand();
                cmd2.CommandText=@"SELECT dc.CodigoI,COALESCE(i.Descripcion,dc.CodigoI),dc.Cantidad,dc.PrecioC
                    FROM DetallesCompra dc LEFT JOIN Inventario i ON i.Codigo=dc.CodigoI
                    WHERE dc.IdCompra=$id";
                cmd2.Parameters.AddWithValue("$id",rowid);
                using var r2=cmd2.ExecuteReader();
                while(r2.Read()) lineas.Add((Db.S(r2,0),Db.S(r2,1),Db.D(r2,2),Db.D(r2,3)));

                string filas=string.Join("",lineas.Select(l=>$"""
                    <tr><td>{H(l.cod)}</td><td>{H(l.desc2)}</td>
                    <td class="r">{l.cant:N2}</td><td class="r">{l.precio:N2}</td>
                    <td class="r">{l.cant*l.precio:N2}</td></tr>
                """));

                string html=$"""
<!DOCTYPE html><html lang="es"><head>
<meta charset="UTF-8"><title>Compra {H(numC)}</title>
<style>
*{{box-sizing:border-box;margin:0;padding:0;font-family:'Segoe UI',Arial,sans-serif;font-size:11pt}}
body{{padding:20mm 15mm;color:#111}}
.titulo{{font-size:20pt;font-weight:700;margin-bottom:2mm}}
.sub{{color:#555;margin-bottom:6mm;font-size:10pt}}
.grid2{{display:grid;grid-template-columns:1fr 1fr;gap:4mm;margin-bottom:6mm}}
.campo{{margin-bottom:2mm}}.campo label{{font-weight:600;font-size:9pt;text-transform:uppercase;color:#555}}
.campo p{{font-size:11pt}}
table{{width:100%;border-collapse:collapse;margin-bottom:6mm}}
thead tr{{background:#1A4A8A;color:#fff}}
th,td{{padding:2mm 3mm;text-align:left;border-bottom:1px solid #ddd}}
.r{{text-align:right}}.total-row td{{font-weight:700;font-size:12pt;border-top:2px solid #111}}
.estado{{display:inline-block;padding:1mm 4mm;border-radius:3mm;font-weight:700;font-size:10pt;
    background:{(abono>=total?"#d4edda":"#fff3cd")};color:{(abono>=total?"#155724":"#856404")}}}
@media print{{body{{padding:10mm}}.no-print{{display:none!important}}}}
</style></head><body>
<button class="no-print" onclick="window.print()"
  style="margin-bottom:8mm;padding:2mm 8mm;background:#1A4A8A;color:#fff;border:none;
         border-radius:4px;cursor:pointer;font-size:11pt">🖨 Imprimir</button>
<div class="titulo">ORDEN DE COMPRA</div>
<div class="sub">Sistema de Gestión — Portal Interno</div>
<div class="grid2">
  <div>
    <div class="campo"><label>N° Documento</label><p style="font-size:14pt;font-weight:700">{H(numC)}</p></div>
    <div class="campo"><label>Fecha</label><p>{H(fecha)}</p></div>
    <div class="campo"><label>Forma de pago</label><p>{H(fp)}</p></div>
  </div>
  <div>
    <div class="campo"><label>Proveedor</label><p>{H(nomP)}</p></div>
    <div class="campo"><label>Estado</label><p><span class="estado">{(abono>=total?"Pagada":"Pendiente")}</span></p></div>
  </div>
</div>
<table>
  <thead><tr><th>Código</th><th>Descripción</th><th class="r">Cant.</th><th class="r">P.Unit (USD)</th><th class="r">Subtotal (USD)</th></tr></thead>
  <tbody>{filas}</tbody>
  <tfoot>
    <tr><td colspan='4' class='r'>IVA</td><td class='r'>{iva:N2}</td></tr>
    <tr class='total-row'><td colspan='4' class='r'>TOTAL USD</td><td class='r'>{total:N2}</td></tr>
  </tfoot>
</table>
{(string.IsNullOrWhiteSpace(obs)?""
    :$"<div style='border:1px solid #ddd;padding:3mm;border-radius:2mm;font-size:10pt;color:#555'><strong>Observaciones:</strong> {H(obs)}</div>")}
</body></html>
""";
                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.WriteAsync(html);
            }).RequireAuthorization();
        }

        private static string H(string? s) => System.Web.HttpUtility.HtmlEncode(s ?? "");
    }

    public record CompraItemRequest(string Codigo, decimal Cantidad, decimal PrecioUnit);
    public record CompraRequest(
        string?  CompraN,
        string?  Fecha,
        string?  CodigoProveedor,
        string   FormaDePago,
        decimal  Descuento,
        decimal  PctIva,
        int      DiasCredito,
        string?  Observaciones,
        List<CompraItemRequest> Items);
}
