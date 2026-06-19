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
                cmd.CommandText = "SELECT Codigo,Descripcion,CiRif,Telefono,Ciudad,Direccion,Email,Contacto FROM Clientes ORDER BY Descripcion LIMIT 100";
                var lista = new List<object>();
                // Aplicar filtro en SQL para búsqueda por código, nombre y RIF
                if (!string.IsNullOrWhiteSpace(q))
                {
                    // Primero exacto por código, luego parcial por nombre y RIF
                    // ORDER BY prioridad: código exacto > código parcial > nombre > RIF
                    cmd.CommandText = @"SELECT Codigo,Descripcion,CiRif,Telefono,Ciudad,Direccion,Email,Contacto,
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
                        telefono=Db.S(r,3), ciudad=Db.S(r,4), direccion=Db.S(r,5),
                        email=Db.S(r,6), contacto=Db.S(r,7) });
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

            // ── POST crear cliente ───────────────────────────────────────
            app.MapPost("/api/clientes", (HttpContext ctx, ClienteRequest req) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                if (string.IsNullOrWhiteSpace(req.Codigo) || string.IsNullOrWhiteSpace(req.Descripcion))
                    return Results.BadRequest(new { error = "Código y nombre son obligatorios." });

                using var c = Db.Conn();
                // Verificar que no exista
                var chk = c.CreateCommand();
                chk.CommandText = "SELECT COUNT(*) FROM Clientes WHERE Codigo=$cod";
                chk.Parameters.AddWithValue("$cod", req.Codigo.Trim());
                if ((long)(chk.ExecuteScalar() ?? 0L) > 0)
                    return Results.Conflict(new { error = $"Ya existe un cliente con código '{req.Codigo.Trim()}'." });

                var cmd = c.CreateCommand();
                cmd.CommandText = @"INSERT INTO Clientes (Codigo,Descripcion,CiRif,Telefono,Ciudad,Direccion,Email,Contacto)
                    VALUES($cod,$desc,$rif,$tel,$ciu,$dir,$email,$cont)";
                cmd.Parameters.AddWithValue("$cod",   req.Codigo.Trim());
                cmd.Parameters.AddWithValue("$desc",  req.Descripcion.Trim());
                cmd.Parameters.AddWithValue("$rif",   req.CiRif?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$tel",   req.Telefono?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$ciu",   req.Ciudad?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$dir",   req.Direccion?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$email", req.Email?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$cont",  req.Contacto?.Trim() ?? "");
                cmd.ExecuteNonQuery();
                return Results.Created($"/api/clientes/{Uri.EscapeDataString(req.Codigo.Trim())}",
                    new { codigo = req.Codigo.Trim(), nombre = req.Descripcion.Trim() });
            }).RequireAuthorization();

            // ── PUT actualizar cliente ────────────────────────────────────
            app.MapPut("/api/clientes/{codigo}", (HttpContext ctx, string codigo, ClienteRequest req) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                if (string.IsNullOrWhiteSpace(req.Descripcion))
                    return Results.BadRequest(new { error = "El nombre es obligatorio." });

                using var c = Db.Conn();
                var chk = c.CreateCommand();
                chk.CommandText = "SELECT COUNT(*) FROM Clientes WHERE Codigo=$cod";
                chk.Parameters.AddWithValue("$cod", codigo);
                if ((long)(chk.ExecuteScalar() ?? 0L) == 0)
                    return Results.NotFound(new { error = "Cliente no encontrado." });

                var cmd = c.CreateCommand();
                cmd.CommandText = @"UPDATE Clientes SET Descripcion=$desc, CiRif=$rif, Telefono=$tel,
                    Ciudad=$ciu, Direccion=$dir, Email=$email, Contacto=$cont
                    WHERE Codigo=$cod";
                cmd.Parameters.AddWithValue("$cod",   codigo);
                cmd.Parameters.AddWithValue("$desc",  req.Descripcion.Trim());
                cmd.Parameters.AddWithValue("$rif",   req.CiRif?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$tel",   req.Telefono?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$ciu",   req.Ciudad?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$dir",   req.Direccion?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$email", req.Email?.Trim() ?? "");
                cmd.Parameters.AddWithValue("$cont",  req.Contacto?.Trim() ?? "");
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // ── DELETE eliminar cliente ───────────────────────────────────
            app.MapDelete("/api/clientes/{codigo}", (HttpContext ctx, string codigo) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var chk = c.CreateCommand();
                chk.CommandText = "SELECT COUNT(*) FROM Clientes WHERE Codigo=$cod";
                chk.Parameters.AddWithValue("$cod", codigo);
                if ((long)(chk.ExecuteScalar() ?? 0L) == 0)
                    return Results.NotFound(new { error = "Cliente no encontrado." });

                var cmd = c.CreateCommand();
                cmd.CommandText = "DELETE FROM Clientes WHERE Codigo=$cod";
                cmd.Parameters.AddWithValue("$cod", codigo);
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // ── POST importar CSV ────────────────────────────────────────
            app.MapPost("/api/clientes/importar-csv", (HttpContext ctx, ImportCsvRequest req) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                if (req.Clientes == null || req.Clientes.Count == 0)
                    return Results.BadRequest(new { error = "No hay clientes para importar." });

                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                int importados = 0, errores = 0;
                var detallesErrores = new List<string>();
                try
                {
                    foreach (var cl in req.Clientes)
                    {
                        if (string.IsNullOrWhiteSpace(cl.Codigo) || string.IsNullOrWhiteSpace(cl.Descripcion))
                        {
                            errores++;
                            detallesErrores.Add($"Fila sin código o nombre: '{cl.Codigo}'");
                            continue;
                        }
                        try
                        {
                            var cmd = c.CreateCommand(); cmd.Transaction = tx;
                            cmd.CommandText = @"INSERT OR REPLACE INTO Clientes
                                (Codigo,Descripcion,CiRif,Telefono,Ciudad,Direccion,Email,Contacto)
                                VALUES($cod,$desc,$rif,$tel,$ciu,$dir,$email,$cont)";
                            cmd.Parameters.AddWithValue("$cod",   cl.Codigo.Trim());
                            cmd.Parameters.AddWithValue("$desc",  cl.Descripcion.Trim());
                            cmd.Parameters.AddWithValue("$rif",   cl.CiRif?.Trim() ?? "");
                            cmd.Parameters.AddWithValue("$tel",   cl.Telefono?.Trim() ?? "");
                            cmd.Parameters.AddWithValue("$ciu",   cl.Ciudad?.Trim() ?? "");
                            cmd.Parameters.AddWithValue("$dir",   cl.Direccion?.Trim() ?? "");
                            cmd.Parameters.AddWithValue("$email", cl.Email?.Trim() ?? "");
                            cmd.Parameters.AddWithValue("$cont",  cl.Contacto?.Trim() ?? "");
                            cmd.ExecuteNonQuery();
                            importados++;
                        }
                        catch
                        {
                            errores++;
                            detallesErrores.Add($"Error insertando '{cl.Codigo}'");
                        }
                    }
                    tx.Commit();
                    return Results.Ok(new { importados, errores, detallesErrores });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem(ex.Message);
                }
            }).RequireAuthorization();
        }
    }

    public record ClienteRequest(
        string? Codigo,
        string? Descripcion,
        string? CiRif,
        string? Telefono,
        string? Ciudad,
        string? Direccion,
        string? Email,
        string? Contacto);

    public record ImportClienteCsv(
        string? Codigo,
        string? Descripcion,
        string? CiRif,
        string? Telefono,
        string? Ciudad,
        string? Direccion,
        string? Email,
        string? Contacto);

    public record ImportCsvRequest(List<ImportClienteCsv> Clientes);

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
                    string tipoDoc = req.PctIva > 0 ? "FacturaConIVA" : "NotaEntrega";
                    ins.CommandText = @"INSERT INTO Facturas
                        (NumeroF,Fecha,Codigo,Descuento,FormaDePago,Abono,IvaF,DiasCreditoF,TotalF,Observaciones,TipoDocumento)
                        VALUES($n,$f,$cl,$desc,$fp,$ab,$iva,$dc,$tot,$obs,$tipo)";
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
                    ins.Parameters.AddWithValue("$tipo", tipoDoc);
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

                    // Auto-crear CxC si es factura a crédito
                    bool esCreditoApi = (req.FormaDePago ?? "").StartsWith("Credito", StringComparison.OrdinalIgnoreCase);
                    bool avisoMora = false;
                    if (esCreditoApi && total > 0)
                    {
                        // Calcular días de crédito desde el campo o desde el valor de FormaDePago ("Credito 30")
                        int diasCredApi = req.DiasCredito > 0 ? req.DiasCredito : 30;
                        if (req.DiasCredito <= 0)
                        {
                            var parts = (req.FormaDePago ?? "").Split(' ');
                            if (parts.Length > 1 && int.TryParse(parts[1], out int d)) diasCredApi = d;
                        }
                        DateTime fechaFact = DateTime.TryParse(fecha, out var ff) ? ff : DateTime.Today;

                        using var c2 = Db.Conn();
                        var cxc = c2.CreateCommand();
                        cxc.CommandText = @"INSERT INTO CuentasCobrar (Cliente,Concepto,Monto,Pagado,FechaEmision,FechaVence,Estado)
                            VALUES ($cl,$co,$m,0,$fe,$fv,'Vigente')";
                        cxc.Parameters.AddWithValue("$cl", req.CodigoCliente ?? "");
                        cxc.Parameters.AddWithValue("$co", $"Factura {numF}");
                        cxc.Parameters.AddWithValue("$m",  (double)total);
                        cxc.Parameters.AddWithValue("$fe", fechaFact.ToString("yyyy-MM-dd"));
                        cxc.Parameters.AddWithValue("$fv", fechaFact.AddDays(diasCredApi).ToString("yyyy-MM-dd"));
                        cxc.ExecuteNonQuery();

                        // Comprobar morosidad del cliente (solo advertencia, no bloquea)
                        using var c3 = Db.Conn();
                        var chkMora = c3.CreateCommand();
                        chkMora.CommandText = @"SELECT COUNT(*), COALESCE(SUM(Monto-Pagado),0)
                            FROM CuentasCobrar
                            WHERE Cliente LIKE $cod
                              AND Estado NOT IN ('Pagada','Saldada')
                              AND FechaVence < $hoy
                              AND (Monto-Pagado) > 0";
                        chkMora.Parameters.AddWithValue("$cod", $"%{req.CodigoCliente ?? ""}%");
                        chkMora.Parameters.AddWithValue("$hoy", DateTime.Today.ToString("yyyy-MM-dd"));
                        using var rm = chkMora.ExecuteReader();
                        if (rm.Read() && !rm.IsDBNull(0) && Convert.ToInt32(rm[0]) > 0)
                            avisoMora = true;
                    }

                    return Results.Created($"/api/facturas/{Uri.EscapeDataString(numF)}",
                        new { numeroF=numF, total, iva=ivaMonto,
                              cxcCreada=esCreditoApi && total > 0,
                              avisoMorosidad=avisoMora });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem(ex.Message);
                }
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirFacturas)).RequireAuthorization();

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
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirFacturas)).RequireAuthorization();


            // ── GET página imprimible ─────────────────────────────────────
            // Detecta TipoDocumento para renderizar factura fiscal (con IVA,
            // tamaño carta) o nota de entrega (sin IVA, media carta).
            app.MapGet("/api/facturas/{numero}/imprimir", async (string numero, HttpContext ctx) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT NumeroF,Fecha,Codigo,TotalF,Abono,IvaF,
                    FormaDePago,Observaciones,rowid,Descuento,
                    COALESCE(TipoDocumento,'FacturaConIVA'),
                    COALESCE(TasaBCVUsada,0)
                    FROM Facturas WHERE NumeroF=$n";
                cmd.Parameters.AddWithValue("$n", numero);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) { ctx.Response.StatusCode=404; return; }
                decimal total=Db.D(r,3), abono=Db.D(r,4), iva=Db.D(r,5), desc=Db.D(r,9),
                        tasaBcv=Db.D(r,11);
                int rowid=Db.I(r,8);
                string numF=Db.S(r,0), fecha=Db.S(r,1), codCli=Db.S(r,2),
                       fp=Db.S(r,6), obs=Db.S(r,7), tipoDoc=Db.S(r,10);
                r.Close();

                // Datos del cliente
                string nomCli=codCli, rifCli="", dirCli="", telCli="", ciudadCli="";
                try {
                    var cc=c.CreateCommand();
                    cc.CommandText="SELECT Descripcion,COALESCE(CiRif,''),COALESCE(Direccion,''),COALESCE(Telefono,''),COALESCE(Ciudad,'') FROM Clientes WHERE Codigo=$c";
                    cc.Parameters.AddWithValue("$c",codCli);
                    using var rc=cc.ExecuteReader();
                    if(rc.Read()){nomCli=Db.S(rc,0);rifCli=Db.S(rc,1);dirCli=Db.S(rc,2);telCli=Db.S(rc,3);ciudadCli=Db.S(rc,4);}
                } catch {}
                string dirCompleta = string.IsNullOrWhiteSpace(ciudadCli) ? dirCli : dirCli + ", " + ciudadCli;

                // Items
                var cmdL = c.CreateCommand();
                cmdL.CommandText = @"SELECT df.Codigo, COALESCE(i.Descripcion,df.Codigo),
                    COALESCE(i.Precio,0), df.Cantidad
                    FROM DetallesFactura df
                    LEFT JOIN Inventario i ON i.Codigo=df.Codigo
                    WHERE df.IdFactura=$id";
                cmdL.Parameters.AddWithValue("$id", rowid);
                var filasFactura = new System.Text.StringBuilder();
                var filasNota = new System.Text.StringBuilder();
                int nItems = 0;
                using var rl = cmdL.ExecuteReader();
                while (rl.Read()) {
                    nItems++;
                    string codItem=Db.S(rl,0), descItem=Db.S(rl,1);
                    decimal pU=Db.D(rl,2), cnt=Db.D(rl,3), sub=pU*cnt;
                    filasFactura.Append($"<tr><td class='c'>{H(cnt.ToString("N0"))}</td><td>{H(codItem)}</td><td>{H(descItem)}</td><td class='r'>{pU.ToString("N2")}</td><td class='r'>{sub.ToString("N2")}</td></tr>");
                    filasNota.Append($"<tr><td class='c'>{H(cnt.ToString("N0"))}</td><td>{H(descItem)}</td><td class='r'>{pU.ToString("N2")}</td><td class='r'>{sub.ToString("N2")}</td></tr>");
                }
                // Filas vacías para llenar la tabla a altura fija
                // Nota de entrega: ~18 filas visibles (198mm content - ~75mm header/footer = ~123mm / 5mm por fila)
                int filasVaciasNota = Math.Max(0, 18 - nItems);
                for (int i = 0; i < filasVaciasNota; i++)
                    filasNota.Append("<tr><td>&nbsp;</td><td></td><td></td><td></td></tr>");
                // Factura fiscal: 11 filas máx (63mm items-section, 11×5mm+6mm thead=61mm)
                int filasVaciasFact = Math.Max(0, 11 - nItems);
                for (int i = 0; i < filasVaciasFact; i++)
                    filasFactura.Append("<tr><td>&nbsp;</td><td></td><td></td><td></td><td></td></tr>");

                // Calcular totales
                decimal subtotalSinDesc = total - iva + (total - iva) * desc / (100 - desc);
                decimal montoDesc = desc > 0 ? subtotalSinDesc * desc / 100 : 0;
                decimal baseImponible = subtotalSinDesc - montoDesc;
                decimal totalBs = tasaBcv > 0 ? total * tasaBcv : 0;

                // Parsear fecha
                string fechaDia="", fechaMes="", fechaAno="";
                if(DateTime.TryParse(fecha, out var dtFecha)){
                    fechaDia=dtFecha.ToString("dd"); fechaMes=dtFecha.ToString("MM"); fechaAno=dtFecha.ToString("yyyy");
                }

                // Forma de pago checkboxes
                string fpLower = (fp ?? "").ToLower();
                string chkEfectivo = fpLower.Contains("efectivo") ? "X" : "&nbsp;";
                string chkCheque = fpLower.Contains("cheque") ? "X" : "&nbsp;";
                string chkTarjC = fpLower.Contains("credito") || fpLower.Contains("crédito") ? "X" : "&nbsp;";
                string chkTransf = fpLower.Contains("transf") ? "X" : "&nbsp;";
                string chkTarjD = fpLower.Contains("debito") || fpLower.Contains("débito") ? "X" : "&nbsp;";

                // Condicion de pago
                string condPago = "Contado";
                if(fp != null && fp.Contains("Crédito", StringComparison.OrdinalIgnoreCase))
                    condPago = fp;
                else if(fp != null) condPago = fp;

                string html;
                if (tipoDoc == "NotaEntrega")
                {
                    // ═══════════════════════════════════════════════════════
                    //  NOTA DE ENTREGA — Diseño COMPLETO para papel normal
                    //  140×210mm — NO usa formato preimpreso
                    // ═══════════════════════════════════════════════════════
                    html =
                    "<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'>" +
                    "<title>Nota de Entrega " + H(numF) + "</title><style>" +
                    "@page{size:140mm 210mm;margin:6mm}" +
                    "*{box-sizing:border-box;margin:0;padding:0;font-family:Arial,sans-serif;font-size:8pt}" +
                    "html,body{height:100%;margin:0}" +
                    "body{width:128mm;margin:0 auto;color:#111}" +
                    ".page-wrap{display:flex;flex-direction:column;min-height:198mm}" +
                    ".empresa{text-align:center;margin-bottom:2mm}" +
                    ".empresa h1{font-size:11pt;font-weight:900;color:#C00;margin-bottom:0.5mm}" +
                    ".empresa .dir{font-size:6pt;color:#333}" +
                    ".empresa .rif{font-size:7pt;font-weight:700}" +
                    ".doc-info{border:1px solid #333;padding:2mm;margin-bottom:1mm;display:grid;grid-template-columns:1fr auto}" +
                    ".doc-titulo{font-size:8pt;font-weight:700;color:#1E3A8A}" +
                    ".doc-num{font-size:12pt;font-weight:900;color:#C00}" +
                    ".doc-fecha{font-size:9pt;text-align:right}" +
                    ".cli{border:1px solid #333;padding:1.5mm 2mm;font-size:8pt;margin-bottom:1mm}" +
                    ".cli b{font-size:7pt;color:#555}" +
                    ".items-section{flex:1;display:flex;flex-direction:column}" +
                    ".items-section table{flex:1}" +
                    "table{width:100%;border-collapse:collapse;border:1px solid #333;margin-bottom:2mm}" +
                    "thead{background:#eee}" +
                    "th{font-size:7pt;padding:1.5mm 2mm;border:1px solid #333;text-transform:uppercase}" +
                    "td{padding:1.5mm 2mm;border-bottom:1px solid #ccc;font-size:8pt;height:5mm}" +
                    ".c{text-align:center}.r{text-align:right}" +
                    ".pie{display:grid;grid-template-columns:1fr 1fr;gap:2mm;font-size:8pt;margin-top:2mm}" +
                    ".pie-izq{border-top:1px solid #333;padding-top:2mm;text-align:center;font-size:7pt}" +
                    ".totales{border:1px solid #333}" +
                    ".totales tr td{padding:1mm 2mm;border-bottom:1px solid #ccc;font-size:8pt}" +
                    ".totales tr:last-child td{font-weight:700;border-top:2px solid #333}" +
                    ".no-print{margin-bottom:4mm;padding:2mm 6mm;background:#C00;color:#fff;border:none;border-radius:3px;cursor:pointer;font-size:9pt}" +
                    "@media print{.no-print{display:none!important}}" +
                    "</style></head><body>" +
                    "<button class='no-print' onclick='window.print()'>Imprimir</button>" +
                    "<div class='page-wrap'>" +

                    // Encabezado empresa (se imprime completo)
                    "<div class='empresa'>" +
                    "<h1>EL HATO PASCHER C.A.</h1>" +
                    "<div class='dir'>Ctra. Villa de Cura - San Francisco de As&iacute;s, Local N&deg; S/N, Sector Los Colorados - Villa de Cura - Estado Aragua</div>" +
                    "<div class='rif'>RIF: J-30571871-7 &nbsp;&nbsp; Telfs: 0244 386.10.13 - 386.33.04</div>" +
                    "</div>" +

                    // Número y fecha
                    "<div class='doc-info'>" +
                    "<div><span class='doc-titulo'>NOTA DE ENTREGA</span><br>" +
                    "<span class='doc-num'>N&deg; " + H(numF) + "</span></div>" +
                    "<div class='doc-fecha'><b style='font-size:6pt'>FECHA</b><br>" + H(fechaDia) + "/" + H(fechaMes) + "/" + H(fechaAno) + "</div>" +
                    "</div>" +

                    // Datos cliente (todo visible)
                    "<div class='cli'><b>CLIENTE:</b> " + H(nomCli) + "</div>" +
                    "<div class='cli'><b>RIF:</b> " + H(rifCli) + " &nbsp;&nbsp; <b>DIRECCI&Oacute;N:</b> " + H(dirCompleta) + "</div>" +
                    "<div class='cli'><b>TEL&Eacute;FONO:</b> " + H(telCli) + " &nbsp;&nbsp; <b>COND. PAGO:</b> " + H(condPago) + "</div>" +

                    // Tabla items
                    "<div class='items-section'>" +
                    "<table><thead><tr><th style='width:12mm'>Cant.</th><th>Descripci&oacute;n</th><th style='width:22mm'>P. Unit.</th><th style='width:22mm'>Total</th></tr></thead>" +
                    "<tbody>" + filasNota + "</tbody></table>" +
                    "</div>" +

                    // Pie: firma + totales
                    "<div class='pie'>" +
                    "<div class='pie-izq'><div style='margin-top:8mm'>Firma y Sello</div></div>" +
                    "<table class='totales'>" +
                    "<tr><td class='r'>SUB-TOTAL</td><td class='r'>" + baseImponible.ToString("N2") + "</td></tr>" +
                    (desc > 0 ? "<tr><td class='r'>DESCUENTO</td><td class='r'>-" + montoDesc.ToString("N2") + "</td></tr>" : "") +
                    "<tr><td class='r'><b>TOTAL</b></td><td class='r'><b>" + total.ToString("N2") + "</b></td></tr>" +
                    "</table></div>" +
                    "</div>" + // cierre page-wrap
                    "</body></html>";
                }
                else
                {
                    // ═══════════════════════════════════════════════════════
                    //  FACTURA FISCAL — 154mm × 305mm (15.4×30.5cm), con IVA
                    // ═══════════════════════════════════════════════════════
                    html =
                    "<!DOCTYPE html><html lang='es'><head><meta charset='UTF-8'>" +
                    "<title>Factura " + H(numF) + "</title><style>" +
                    "@page{size:154mm 305mm;margin:0}" +
                    "*{box-sizing:border-box;margin:0;padding:0;font-family:Arial,sans-serif;font-size:8.5pt}" +
                    "html,body{margin:0;padding:0}" +
                    "body{color:#111}" +
                    ".page-wrap{width:134mm;margin:0 auto;padding-top:4mm}" +
                    ".empresa{height:26mm;text-align:center;overflow:hidden}" +
                    ".empresa h1{font-size:12pt;color:#C00;font-weight:900;letter-spacing:1px}" +
                    ".empresa .dir{font-size:7pt;color:#333;margin:0.5mm 0}" +
                    ".empresa .rif{font-size:8pt;font-weight:700}" +
                    ".doc-info{height:13mm;margin-top:1mm;display:grid;grid-template-columns:auto 1fr auto;gap:1mm;align-items:center;border:1px solid #333;padding:1.5mm 2mm 1.5mm 1mm;overflow:hidden}" +
                    ".fecha-box{display:grid;grid-template-columns:repeat(3,10mm);gap:0.5mm;text-align:center}" +
                    ".fecha-box div{border:1px solid #999;padding:0.5mm;font-size:8pt}" +
                    ".fecha-box .lbl{font-size:5.5pt;font-weight:700;border:none;padding:0}" +
                    ".fnum{font-size:11pt;font-weight:900;color:#C00}" +
                    ".nctrl{font-size:8pt;font-weight:700}" +
                    ".cli-box{height:8mm;margin-top:1mm;border:1px solid #333;padding:1.5mm 2mm;font-size:8.5pt;overflow:hidden}" +
                    ".cli-box b{font-size:7pt;color:#555;text-transform:uppercase}" +
                    ".cli-grid{height:11mm;margin-top:2mm;display:grid;grid-template-columns:1fr 1fr 1fr 1fr 1fr;gap:0.5mm;border:1px solid #333;padding:1.5mm 2mm;font-size:7.5pt;overflow:hidden}" +
                    ".cli-grid b{font-size:6pt;color:#555;text-transform:uppercase}" +
                    ".above-footer{height:140mm;overflow:hidden}" +
                    ".items-section{height:63mm;margin-top:1mm;padding-right:3mm;overflow:hidden}" +
                    "table{width:100%;border-collapse:collapse;border:1px solid #333}" +
                    "thead{background:#eee}" +
                    "th{font-size:7pt;padding:1.5mm;border:1px solid #333;text-transform:uppercase;font-weight:700}" +
                    "td{padding:0.7mm 1.5mm;border-bottom:1px solid #ccc;font-size:8.5pt;line-height:1;height:5mm}" +
                    ".c{text-align:center}.r{text-align:right}" +
                    ".footer-grid{height:42mm;display:grid;grid-template-columns:1fr 1fr;gap:2mm;overflow:hidden}" +
                    ".fp-box{border:1px solid #333;padding:1.5mm 2mm;font-size:7.5pt}" +
                    ".fp-box b{font-size:7pt;text-transform:uppercase}" +
                    ".chk{display:inline-block;width:3mm;height:3mm;border:1px solid #333;text-align:center;font-size:7pt;line-height:3mm;margin-right:0.5mm;vertical-align:middle}" +
                    ".fp-item{display:inline-block;margin-right:2mm;margin-bottom:0.5mm}" +
                    ".totales-box{border:1px solid #333}" +
                    ".totales-box tr td{padding:1mm 2mm;border-bottom:1px solid #ccc;font-size:8.5pt;line-height:1;height:7mm}" +
                    ".totales-box tr:last-child td{font-weight:900;font-size:9pt;border-top:2px solid #333}" +
                    ".firma-grid{height:24mm;margin-top:1mm;display:grid;grid-template-columns:1fr 1fr;gap:5mm;text-align:center;font-size:7.5pt}" +
                    ".firma-line{border-top:1px solid #333;padding-top:1mm;margin-top:6mm}" +
                    ".no-print{position:fixed;top:5mm;left:5mm;z-index:99;padding:2mm 6mm;background:#C00;color:#fff;border:none;border-radius:3px;cursor:pointer;font-size:9pt}" +
                    "@media print{.no-print{display:none!important}.pp{visibility:hidden}.firma-grid{display:none!important}*{border-color:transparent!important;background:none!important}}" +
                    "</style></head><body>" +
                    "<button class='no-print' onclick='window.print()'>Imprimir</button>" +
                    "<div class='page-wrap'>" +

                    // Contenedor fijo 140mm: cambiar algo aqui NO mueve el footer
                    "<div class='above-footer'>" +
                    "<div class='empresa pp'>" +
                    "<h1>EL HATO PASCHER C.A.</h1>" +
                    "<div class='dir'>Ctra. Villa de Cura - San Francisco de As&iacute;s, Local N&deg; S/N, Sector Los Colorados - Villa de Cura - Estado Aragua</div>" +
                    "<div class='rif'>RIF: J-30571871-7 &nbsp;&nbsp; Telfs: 0244 386.10.13 - 386.33.04</div>" +
                    "</div>" +

                    // Info documento: Fecha | FACTURA N° | N° CONTROL
                    "<div class='doc-info'>" +
                    "<div><b class='pp' style='font-size:6pt'>FECHA</b><div class='fecha-box'>" +
                    "<div class='lbl pp'>D&Iacute;A</div><div class='lbl pp'>MES</div><div class='lbl pp'>A&Ntilde;O</div>" +
                    "<div>" + H(fechaDia) + "</div><div>" + H(fechaMes) + "</div><div>" + H(fechaAno) + "</div>" +
                    "</div></div>" +
                    "<div style='text-align:center'><span class='pp' style='font-size:7.5pt;font-weight:700'>FACTURA</span><br>" +
                    "<span class='fnum pp'>N&deg; " + H(numF) + "</span></div>" +
                    "<div style='text-align:right'><span class='nctrl pp'>N&deg; CONTROL</span><br>" +
                    "<span class='pp' style='font-size:8pt;font-weight:700'>00 &mdash; " + H(numF.PadLeft(6,'0')) + "</span></div>" +
                    "</div>" +

                    // Datos cliente linea 1
                    "<div class='cli-box' style='margin-top:2mm;height:7mm'><b class='pp'>Nombre o Raz&oacute;n Social:</b> " + H(nomCli) + "</div>" +
                    "<div class='cli-box' style='height:9mm'><b class='pp'>Domicilio Fiscal:</b> " + H(dirCompleta) + "</div>" +

                    // Datos cliente linea 2
                    "<div class='cli-grid'>" +
                    "<div><b class='pp'>RIF N&deg;</b><br>" + H(rifCli) + "</div>" +
                    "<div><b class='pp'>Tel&eacute;fonos</b><br>" + H(telCli) + "</div>" +
                    "<div><b class='pp'>Gu&iacute;a de Despacho</b><br>" + H(obs ?? "") + "</div>" +
                    "<div><b class='pp'>Orden de Compra N&deg;</b><br></div>" +
                    "<div><b class='pp'>Condici&oacute;n de Pago</b><br>" + H(condPago) + "</div>" +
                    "</div>" +

                    // Tabla de items (flex:1 para llenar la página)
                    "<div class='items-section'>" +
                    "<table><thead class='pp'><tr>" +
                    "<th style='width:10mm'>Cant.</th><th style='width:14mm'>Ref.</th><th>DESCRIPCI&Oacute;N</th>" +
                    "<th style='width:22mm'>P. UNIT.</th><th style='width:23mm'>TOTAL</th>" +
                    "</tr></thead><tbody>" + filasFactura + "</tbody></table>" +
                    "</div>" +
                    "</div>" + // cierre above-footer

                    // Pie: Forma de pago + Totales (SIEMPRE a 140mm del top)
                    "<div class='footer-grid'>" +
                    "<div class='fp-box'><b class='pp'>FORMA DE PAGO</b><br>" +
                    "<span class='fp-item'><span class='chk'>" + chkEfectivo + "</span><span class='pp'>EFECTIVO</span></span>" +
                    "<span class='fp-item'><span class='chk'>" + chkCheque + "</span><span class='pp'>CHEQUE</span></span>" +
                    "<span class='fp-item'><span class='chk'>" + chkTarjC + "</span><span class='pp'>TARJ. CR&Eacute;DITO</span></span><br>" +
                    "<span class='fp-item'><span class='chk'>" + chkTransf + "</span><span class='pp'>TRANSF. BANCARIA</span></span>" +
                    "<span class='fp-item'><span class='chk'>" + chkTarjD + "</span><span class='pp'>TARJ. D&Eacute;BITO</span></span>" +
                    "</div>" +
                    "<table class='totales-box'>" +
                    "<tr class='pp'><td colspan='2' style='height:" + (desc > 0 ? "2" : "9") + "mm;padding:0'></td></tr>" +
                    "<tr><td class='r pp'>SUB-TOTAL</td><td class='r' style='width:22mm'>" + (baseImponible + montoDesc).ToString("N2") + "</td></tr>" +
                    (desc > 0 ? "<tr><td class='r pp'>DESCUENTO " + desc.ToString("N0") + "%</td><td class='r'>-" + montoDesc.ToString("N2") + "</td></tr>" : "") +
                    "<tr><td class='r pp'>EXENTO</td><td class='r'></td></tr>" +
                    "<tr><td class='r pp'>BASE IMPONIBLE</td><td class='r'>" + baseImponible.ToString("N2") + "</td></tr>" +
                    "<tr><td class='r pp' style='height:5mm'>IVA 16%</td><td class='r' style='height:5mm'>" + iva.ToString("N2") + "</td></tr>" +
                    "<tr><td class='r pp'>TOTAL</td><td class='r'>" + total.ToString("N2") + "</td></tr>" +
                    "</table></div>" +

                    // Firmas (todo pre-impreso)
                    "<div class='firma-grid pp'>" +
                    "<div><div class='firma-line'>Firma y Sello</div></div>" +
                    "<div><div class='firma-line'>Cliente Conforme</div></div>" +
                    "</div>" +

                    "</div>" + // cierre page-wrap
                    "</body></html>";
                }

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
