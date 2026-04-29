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
        public static void MapFacturasEndpoints(this WebApplication app)
        {
            app.MapGet("/api/facturas", (string? cliente) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT NumeroF,Fecha,Codigo,TotalF,Abono,FormaDePago
                    FROM Facturas ORDER BY Fecha DESC, NumeroF DESC LIMIT 50";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var cl = Db.S(r,2);
                    if (!string.IsNullOrWhiteSpace(cliente) &&
                        !cl.Contains(cliente, StringComparison.OrdinalIgnoreCase)) continue;
                    decimal total=Db.D(r,3); decimal abono=Db.D(r,4);
                    lista.Add(new { numeroF=Db.S(r,0), fecha=Db.S(r,1), cliente=cl,
                        total, abono, saldo=total-abono, formaDePago=Db.S(r,5), pagada=abono>=total });
                }
                return Results.Ok(lista.Take(30).ToList());
            }).RequireAuthorization();

            app.MapGet("/api/facturas/{numero}", (string numero) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT NumeroF,Fecha,Codigo,TotalF,Abono,IvaF,FormaDePago,Observaciones,rowid
                    FROM Facturas WHERE NumeroF=$n";
                cmd.Parameters.AddWithValue("$n", numero);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return Results.NotFound(new { error = "Factura no encontrada." });
                decimal total=Db.D(r,3); decimal abono=Db.D(r,4);
                int rowid = Db.I(r,8);
                var fact = new { numeroF=Db.S(r,0), fecha=Db.S(r,1), cliente=Db.S(r,2),
                    total, abono, saldo=total-abono, iva=Db.D(r,5),
                    formaDePago=Db.S(r,6), observaciones=Db.S(r,7), pagada=abono>=total };
                r.Close();

                var lineas = new List<object>();
                var cmd2 = c.CreateCommand();
                cmd2.CommandText = @"SELECT df.Codigo,i.Descripcion,df.Cantidad,i.Precio
                    FROM DetallesFactura df
                    LEFT JOIN Inventario i ON i.Codigo=df.Codigo
                    WHERE df.IdFactura=$id";
                cmd2.Parameters.AddWithValue("$id", rowid);
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read())
                {
                    decimal cant=Db.D(r2,2); decimal precio=Db.D(r2,3);
                    lineas.Add(new { codigoItem=Db.S(r2,0), descripcion=Db.S(r2,1),
                        cantidad=cant, precioUnit=precio, subtotal=cant*precio });
                }
                return Results.Ok(new { factura=fact, lineas });
            }).RequireAuthorization();
        }
    }
}
