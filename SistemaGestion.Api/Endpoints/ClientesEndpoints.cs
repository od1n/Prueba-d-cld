using SistemaGestion.Api.Auth;

namespace SistemaGestion.Api.Endpoints
{
    public static class ClientesEndpoints
    {
        public static void MapClientesEndpoints(this WebApplication app)
        {
            app.MapGet("/api/clientes", (HttpContext ctx, string? q) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT Codigo,Descripcion,CiRif,Telefono,Ciudad FROM Clientes ORDER BY Descripcion LIMIT 100";
                var lista = new List<object>();
                // Aplicar filtro en SQL para búsqueda por código, nombre y RIF
                if (!string.IsNullOrWhiteSpace(q))
                {
                    // Primero exacto por código, luego parcial por nombre y RIF
                    // ORDER BY prioridad: código exacto > código parcial > nombre > RIF
                    cmd.CommandText = @"SELECT Codigo,Descripcion,CiRif,Telefono,Ciudad,
                        CASE
                            WHEN Codigo = $qExact THEN 0
                            WHEN Codigo LIKE $qStart THEN 1
                            WHEN Descripcion LIKE $q THEN 2
                            ELSE 3
                        END AS _prio
                        FROM Clientes
                        WHERE Codigo LIKE $q OR Descripcion LIKE $q OR CiRif LIKE $q
                        ORDER BY _prio, Descripcion LIMIT 50";
                    cmd.Parameters.AddWithValue("$q",      $"%{q}%");
                    cmd.Parameters.AddWithValue("$qExact", q.Trim());
                    cmd.Parameters.AddWithValue("$qStart", $"{q.Trim()}%");
                }
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new { codigo=Db.S(r,0), nombre=Db.S(r,1), ciRif=Db.S(r,2),
                        telefono=Db.S(r,3), ciudad=Db.S(r,4) });
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapGet("/api/clientes/{codigo}/cxc", (HttpContext ctx, string codigo) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();

                var cmdC = c.CreateCommand();
                cmdC.CommandText = "SELECT Codigo,Descripcion,CiRif,Telefono FROM Clientes WHERE Codigo=$cod";
                cmdC.Parameters.AddWithValue("$cod", codigo);
                using var rc = cmdC.ExecuteReader();
                if (!rc.Read()) return Results.NotFound(new { error = "Cliente no encontrado." });
                var cliente = new { codigo=Db.S(rc,0), nombre=Db.S(rc,1), ciRif=Db.S(rc,2), telefono=Db.S(rc,3) };
                var nombreCliente = cliente.nombre;
                rc.Close();

                var hoy = DateTime.Today.ToString("yyyy-MM-dd");
                var cmd2 = c.CreateCommand();
                cmd2.CommandText = @"SELECT Id,Cliente,Concepto,Monto,Pagado,FechaEmision,FechaVence,Estado
                    FROM CuentasCobrar WHERE (Cliente=$cod OR Cliente=$nom) ORDER BY FechaVence";
                cmd2.Parameters.AddWithValue("$cod", codigo);
                cmd2.Parameters.AddWithValue("$nom", nombreCliente);
                var cuentas = new List<object>();
                decimal saldoTotal = 0, vencido = 0;
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read())
                {
                    decimal monto=Db.D(r2,3); decimal pagado=Db.D(r2,4);
                    decimal saldo=monto-pagado; string estado=Db.S(r2,7);
                    string fv=Db.S(r2,6);
                    bool esVenc = estado!="Pagada" && string.Compare(fv,hoy)<0 && saldo>0;
                    if (estado!="Pagada") { saldoTotal+=saldo; if(esVenc) vencido+=saldo; }
                    cuentas.Add(new { id=Db.I(r2,0), concepto=Db.S(r2,2), monto, pagado, saldo,
                        estado, emision=Db.S(r2,5), vence=fv, estaVencida=esVenc });
                }
                return Results.Ok(new { cliente, saldoTotal, vencido, cuentas });
            }).RequireAuthorization();
        }
    }

    public static class FacturasEndpoints
    {
        // ── Número siguiente (formato NE-XXXX) ────────────────────────────
        private static string ProximoNumero()
        {
            string prefix = "NE-";
            using var c = Db.Conn();
            var cmd = c.CreateCommand();
            cmd.CommandText = @"SELECT NumeroF FROM Facturas
                WHERE NumeroF LIKE $p ORDER BY NumeroF DESC LIMIT 1";
            cmd.Parameters.AddWithValue("$p", prefix + "%");
            var last = cmd.ExecuteScalar()?.ToString();
            int next = 1;
            if (!string.IsNullOrEmpty(last) && last.Length > prefix.Length)
                if (int.TryParse(last.Substring(prefix.Length), out int n)) next = n + 1;
            return $"{prefix}{next:D4}";
        }

        public static void MapFacturasEndpoints(this WebApplication app)
        {
            // ── GET lista ────────────────────────────────────────────────
            app.MapGet("/api/facturas", (string? q, string? desde, string? hasta) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT NumeroF,Fecha,Codigo,TotalF,Abono,FormaDePago
                    FROM Facturas
                    WHERE (Observaciones IS NULL OR Observaciones NOT LIKE 'ANULADA%')
                    ORDER BY Fecha DESC, NumeroF DESC LIMIT 100";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string numF = Db.S(r,0), fecha = Db.S(r,1), cl = Db.S(r,2);
                    if (!string.IsNullOrWhiteSpace(q) &&
                        !cl.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                        !numF.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrWhiteSpace(desde) && string.Compare(fecha, desde) < 0) continue;
                    if (!string.IsNullOrWhiteSpace(hasta) && string.Compare(fecha, hasta) > 0) continue;
                    decimal total=Db.D(r,3), abono=Db.D(r,4);
                    lista.Add(new { numeroF=numF, fecha, cliente=cl,
                        total, abono, saldo=total-abono, formaDePago=Db.S(r,5), pagada=abono>=total });
                }
                return Results.Ok(lista);
            }).RequireAuthorization();

            // ── GET detalle ───────────────────────────────────────────────
            app.MapGet("/api/facturas/{numero}", (string numero) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT NumeroF,Fecha,Codigo,TotalF,Abono,IvaF,
                    FormaDePago,Observaciones,rowid,Descuento,DiasCreditoF
                    FROM Facturas WHERE NumeroF=$n";
                cmd.Parameters.AddWithValue("$n", numero);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return Results.NotFound(new { error = "Factura no encontrada." });
                decimal total=Db.D(r,3), abono=Db.D(r,4);
                int rowid=Db.I(r,8);
                var fact = new { numeroF=Db.S(r,0), fecha=Db.S(r,1), cliente=Db.S(r,2),
                    total, abono, saldo=total-abono, iva=Db.D(r,5),
                    formaDePago=Db.S(r,6), observaciones=Db.S(r,7), pagada=abono>=total,
                    descuento=Db.D(r,9), diasCredito=Db.I(r,10) };
                r.Close();

                var lineas = new List<object>();
                var cmd2 = c.CreateCommand();
                cmd2.CommandText = @"SELECT df.Codigo,COALESCE(i.Descripcion,df.Codigo),
                    df.Cantidad,i.Precio
                    FROM DetallesFactura df
                    LEFT JOIN Inventario i ON i.Codigo=df.Codigo
                    WHERE df.IdFactura=$id";
                cmd2.Parameters.AddWithValue("$id", rowid);
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read())
                {
                    decimal cant=Db.D(r2,2), precio=Db.D(r2,3);
                    lineas.Add(new { codigoItem=Db.S(r2,0), descripcion=Db.S(r2,1),
                        cantidad=cant, precioUnit=precio, subtotal=cant*precio });
                }

                // Nombre del cliente
                string nombreCliente = fact.cliente;
                try {
                    var cc = Db.Conn();
                    var cc2 = cc.CreateCommand();
                    cc2.CommandText = "SELECT Descripcion FROM Clientes WHERE Codigo=$c";
                    cc2.Parameters.AddWithValue("$c", fact.cliente);
                    var nom = cc2.ExecuteScalar()?.ToString();
                    if (!string.IsNullOrEmpty(nom)) nombreCliente = nom;
                    cc.Close();
                } catch {}

                return Results.Ok(new { factura=fact, lineas, nombreCliente });
            }).RequireAuthorization();

            // ── POST crear ───────────────────────────────────────────────
            app.MapPost("/api/facturas", (FacturaRequest req, HttpContext ctx) =>
            {
                if (req.Items == null || req.Items.Count == 0)
                    return Results.BadRequest(new { error = "La factura debe tener al menos un ítem." });

                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    string numF = string.IsNullOrWhiteSpace(req.NumeroF)
                        ? ProximoNumero() : req.NumeroF.Trim().ToUpper();
                    string fecha = string.IsNullOrWhiteSpace(req.Fecha)
                        ? DateTime.Today.ToString("yyyy-MM-dd") : req.Fecha;

                    // Verificar que no exista
                    var chk = c.CreateCommand(); chk.Transaction = tx;
                    chk.CommandText = "SELECT COUNT(*) FROM Facturas WHERE NumeroF=$n";
                    chk.Parameters.AddWithValue("$n", numF);
                    if ((long)(chk.ExecuteScalar()??0L) > 0)
                        return Results.Conflict(new { error = $"La factura {numF} ya existe." });

                    // Calcular total desde inventario
                    decimal subTotal = 0;
                    var itemsCalc = new List<(string cod, decimal cant, decimal precio)>();
                    foreach (var it in req.Items)
                    {
                        if (string.IsNullOrWhiteSpace(it.Codigo) || it.Cantidad <= 0) continue;
                        var pi = c.CreateCommand(); pi.Transaction = tx;
                        pi.CommandText = "SELECT Precio FROM Inventario WHERE Codigo=$c";
                        pi.Parameters.AddWithValue("$c", it.Codigo.Trim());
                        decimal precio = it.PrecioUnit > 0 ? it.PrecioUnit
                            : (decimal)(double)(pi.ExecuteScalar() ?? 0.0);
                        subTotal += it.Cantidad * precio;
                        itemsCalc.Add((it.Codigo.Trim(), it.Cantidad, precio));
                    }
                    if (itemsCalc.Count == 0)
                        return Results.BadRequest(new { error = "Sin ítems válidos." });

                    decimal descMonto = subTotal * (req.Descuento / 100m);
                    decimal baseIva   = subTotal - descMonto;
                    decimal ivaMonto  = baseIva * (req.PctIva / 100m);
                    decimal total     = baseIva + ivaMonto;

                    // Insertar cabecera
                    var ins = c.CreateCommand(); ins.Transaction = tx;
                    ins.CommandText = @"INSERT INTO Facturas
                        (NumeroF,Fecha,Codigo,Descuento,FormaDePago,Abono,IvaF,DiasCreditoF,TotalF,Observaciones)
                        VALUES($n,$f,$cl,$desc,$fp,$ab,$iva,$dc,$tot,$obs)";
                    ins.Parameters.AddWithValue("$n",   numF);
                    ins.Parameters.AddWithValue("$f",   fecha);
                    ins.Parameters.AddWithValue("$cl",  req.CodigoCliente ?? "");
                    ins.Parameters.AddWithValue("$desc",(double)req.Descuento);
                    ins.Parameters.AddWithValue("$fp",  req.FormaDePago ?? "Contado");
                    ins.Parameters.AddWithValue("$ab",  (double)(req.FormaDePago=="Contado"?total:0));
                    ins.Parameters.AddWithValue("$iva", (double)ivaMonto);
                    ins.Parameters.AddWithValue("$dc",  req.DiasCredito);
                    ins.Parameters.AddWithValue("$tot", (double)total);
                    ins.Parameters.AddWithValue("$obs", req.Observaciones ?? "");
                    ins.ExecuteNonQuery();

                    // Obtener rowid
                    var rid = c.CreateCommand(); rid.Transaction = tx;
                    rid.CommandText = "SELECT rowid FROM Facturas WHERE NumeroF=$n";
                    rid.Parameters.AddWithValue("$n", numF);
                    int rowid = (int)(long)(rid.ExecuteScalar() ?? 0L);

                    // Insertar líneas + actualizar stock
                    foreach (var (cod, cant, precio) in itemsCalc)
                    {
                        var dl = c.CreateCommand(); dl.Transaction = tx;
                        dl.CommandText = @"INSERT OR REPLACE INTO DetallesFactura(IdFactura,Codigo,Cantidad)
                            VALUES($id,$cod,$cant)";
                        dl.Parameters.AddWithValue("$id",  rowid);
                        dl.Parameters.AddWithValue("$cod", cod);
                        dl.Parameters.AddWithValue("$cant",(double)cant);
                        dl.ExecuteNonQuery();

                        // Descontar stock
                        var st = c.CreateCommand(); st.Transaction = tx;
                        st.CommandText = @"UPDATE Inventario SET Existencia=MAX(0,Existencia-$q),
                            UltimaActualz=date('now','localtime') WHERE Codigo=$c";
                        st.Parameters.AddWithValue("$q",(double)cant);
                        st.Parameters.AddWithValue("$c",cod);
                        st.ExecuteNonQuery();

                        // Movimiento kardex
                        var mv = c.CreateCommand(); mv.Transaction = tx;
                        mv.CommandText = @"INSERT INTO MovimientosInventario
                            (CodigoProducto,FechaHora,TipoMovimiento,Concepto,Cantidad,NuevoSaldo,Usuario)
                            SELECT $cod,datetime('now','localtime'),'SALIDA',$conc,$cant,
                                MAX(0,Existencia),$usr FROM Inventario WHERE Codigo=$cod";
                        mv.Parameters.AddWithValue("$cod",  cod);
                        mv.Parameters.AddWithValue("$conc", $"Factura {numF}");
                        mv.Parameters.AddWithValue("$cant", (double)cant);
                        mv.Parameters.AddWithValue("$usr",
                            ctx.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value ?? "portal");
                        mv.ExecuteNonQuery();
                    }

                    tx.Commit();
                    return Results.Created($"/api/facturas/{Uri.EscapeDataString(numF)}",
                        new { numeroF=numF, total, iva=ivaMonto });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem(ex.Message);
                }
            }).RequireAuthorization();

            // ── POST pago parcial / abono ─────────────────────────────────
            app.MapPost("/api/facturas/{numero}/pago", (string numero, AbonoRequest req) =>
            {
                if (req.Monto <= 0) return Results.BadRequest(new { error = "Monto inválido." });
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT TotalF,Abono FROM Facturas WHERE NumeroF=$n";
                cmd.Parameters.AddWithValue("$n", numero);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return Results.NotFound(new { error = "Factura no encontrada." });
                decimal total=Db.D(r,0), abonoAnt=Db.D(r,1);
                r.Close();
                decimal nuevoAbono = Math.Min(total, abonoAnt + req.Monto);
                var upd = c.CreateCommand();
                upd.CommandText = "UPDATE Facturas SET Abono=$a WHERE NumeroF=$n";
                upd.Parameters.AddWithValue("$a",(double)nuevoAbono);
                upd.Parameters.AddWithValue("$n",numero);
                upd.ExecuteNonQuery();
                return Results.Ok(new { abono=nuevoAbono, saldo=total-nuevoAbono, pagada=nuevoAbono>=total });
            }).RequireAuthorization();


            // ── GET página imprimible ─────────────────────────────────────
            app.MapGet("/api/facturas/{numero}/imprimir", async (string numero, HttpContext ctx) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT NumeroF,Fecha,Codigo,TotalF,Abono,IvaF,
                    FormaDePago,Observaciones,rowid,Descuento
                    FROM Facturas WHERE NumeroF=$n";
                cmd.Parameters.AddWithValue("$n", numero);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) { ctx.Response.StatusCode=404; return; }
                decimal total=Db.D(r,3), abono=Db.D(r,4), iva=Db.D(r,5), desc=Db.D(r,9);
                int rowid=Db.I(r,8);
                string numF=Db.S(r,0), fecha=Db.S(r,1), codCli=Db.S(r,2),
                       fp=Db.S(r,6), obs=Db.S(r,7);
                r.Close();

                string nomCli = codCli;
                try {
                    var cc=c.CreateCommand();
                    cc.CommandText="SELECT Descripcion FROM Clientes WHERE Codigo=$c";
                    cc.Parameters.AddWithValue("$c",codCli);
                    nomCli = cc.ExecuteScalar()?.ToString() ?? codCli;
                } catch {}

                var cmdL = c.CreateCommand();
                cmdL.CommandText = @"SELECT df.Codigo, COALESCE(i.Descripcion,df.Codigo),
                    COALESCE(i.Precio,0), df.Cantidad
                    FROM DetallesFactura df
                    LEFT JOIN Inventario i ON i.Codigo=df.Codigo
                    WHERE df.IdFactura=$id";
                cmdL.Parameters.AddWithValue("$id", rowid);
                var filas = new System.Text.StringBuilder();
                using var rl = cmdL.ExecuteReader();
                while (rl.Read()) {
                    decimal pU=Db.D(rl,2), cnt=Db.D(rl,3);
                    filas.Append("<tr><td>" + H(Db.S(rl,0)) + "</td><td>" + H(Db.S(rl,1)) +
                        "</td><td class='r'>" + cnt.ToString("N2") +
                        "</td><td class='r'>" + pU.ToString("N2") +
                        "</td><td class='r'>" + (pU*cnt).ToString("N2") + "</td></tr>");
                }
                string estado = abono >= total ? "Pagada" : "Pendiente";
                string bg = abono >= total ? "#d4edda" : "#fff3cd";
                string fg = abono >= total ? "#155724" : "#856404";

                string html =
                    "<!DOCTYPE html><html lang='es'><head>" +
                    "<meta charset='UTF-8'><title>Factura " + H(numF) + "</title>" +
                    "<style>" +
                    "*{box-sizing:border-box;margin:0;padding:0;font-family:'Segoe UI',Arial,sans-serif;font-size:11pt}" +
                    "body{padding:20mm 15mm;color:#111}" +
                    ".titulo{font-size:20pt;font-weight:700;margin-bottom:2mm}" +
                    ".sub{color:#555;margin-bottom:6mm;font-size:10pt}" +
                    ".grid2{display:grid;grid-template-columns:1fr 1fr;gap:4mm;margin-bottom:6mm}" +
                    ".campo{margin-bottom:2mm}.campo label{font-weight:600;font-size:9pt;text-transform:uppercase;color:#555}" +
                    ".campo p{font-size:11pt}" +
                    "table{width:100%;border-collapse:collapse;margin-bottom:6mm}" +
                    "thead tr{background:#1A4A8A;color:#fff}" +
                    "th,td{padding:2mm 3mm;text-align:left;border-bottom:1px solid #ddd}" +
                    ".r{text-align:right}.total-row td{font-weight:700;border-top:2px solid #111}" +
                    ".estado{display:inline-block;padding:1mm 4mm;border-radius:3mm;font-weight:700}" +
                    "@media print{body{padding:10mm}.no-print{display:none!important}}" +
                    "</style></head><body>" +
                    "<button class='no-print' onclick='window.print()' " +
                    "style='margin-bottom:8mm;padding:2mm 8mm;background:#1A4A8A;color:#fff;" +
                    "border:none;border-radius:4px;cursor:pointer;font-size:11pt'>&#128424; Imprimir</button>" +
                    "<div class='titulo'>NOTA DE ENTREGA</div>" +
                    "<div class='sub'>Sistema de Gestión — Portal Interno</div>" +
                    "<div class='grid2'><div>" +
                    "<div class='campo'><label>N° Documento</label><p style='font-size:14pt;font-weight:700'>" + H(numF) + "</p></div>" +
                    "<div class='campo'><label>Fecha</label><p>" + H(fecha) + "</p></div>" +
                    "<div class='campo'><label>Forma de pago</label><p>" + H(fp) + "</p></div>" +
                    "</div><div>" +
                    "<div class='campo'><label>Cliente</label><p>" + H(nomCli) + "</p></div>" +
                    "<div class='campo'><label>Estado</label><p>" +
                    "<span class='estado' style='background:" + bg + ";color:" + fg + "'>" + estado + "</span></p></div>" +
                    "</div></div>" +
                    "<table><thead><tr><th>Código</th><th>Descripción</th>" +
                    "<th class='r'>Cant.</th><th class='r'>P.Unit</th><th class='r'>Subtotal</th></tr></thead>" +
                    "<tbody>" + filas + "</tbody>" +
                    "<tfoot>" +
                    (desc > 0 ? "<tr><td colspan='4' class='r'>Descuento</td><td class='r'>-" + desc.ToString("N2") + "</td></tr>" : "") +
                    "<tr><td colspan='4' class='r'>IVA</td><td class='r'>" + iva.ToString("N2") + "</td></tr>" +
                    "<tr class='total-row'><td colspan='4' class='r'>TOTAL</td><td class='r'>" + total.ToString("N2") + "</td></tr>" +
                    "</tfoot></table>" +
                    (string.IsNullOrWhiteSpace(obs) ? "" :
                        "<div style='border:1px solid #ddd;padding:3mm;border-radius:2mm;font-size:10pt;color:#555;margin-top:4mm'>" +
                        "<strong>Observaciones:</strong> " + H(obs) + "</div>") +
                    "</body></html>";

                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.WriteAsync(html);
            }).RequireAuthorization();
        }

        private static string H(string? s) => System.Web.HttpUtility.HtmlEncode(s ?? "");
    }

    public record FacturaItemRequest(string Codigo, decimal Cantidad, decimal PrecioUnit);
    public record FacturaRequest(
        string?  NumeroF,
        string?  Fecha,
        string?  CodigoCliente,
        string   FormaDePago,
        decimal  PctIva,
        decimal  Descuento,
        int      DiasCredito,
        string?  Observaciones,
        List<FacturaItemRequest>? Items);
    public record AbonoRequest(decimal Monto, string? Fecha);
}
