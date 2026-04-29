namespace SistemaGestion.Api.Endpoints
{
    public static class InventarioEndpoints
    {
        public static void MapInventarioEndpoints(this WebApplication app)
        {
            app.MapGet("/api/inventario", (string? q) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Codigo,Descripcion,Departamento,Unidad,
                    Existencia,ExistenMin,ExistenMax,
                    Precio,CostoActual,UltimaActualz
                    FROM Inventario ORDER BY Descripcion";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var cod  = Db.S(r,0); var desc = Db.S(r,1);
                    if (!string.IsNullOrWhiteSpace(q) &&
                        !cod.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                        !desc.Contains(q, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var stock = Db.D(r,4); var min = Db.D(r,5);
                    lista.Add(new { codigo=cod, descripcion=desc, departamento=Db.S(r,2),
                        unidad=Db.S(r,3), stock, stockMin=min, stockMax=Db.D(r,6),
                        precioActual=Db.D(r,7), costoActual=Db.D(r,8),
                        ultimaActualz=Db.S(r,9), bajMin=min>0&&stock<min });
                }
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapGet("/api/inventario/{codigo}", (string codigo) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Codigo,Descripcion,Departamento,Unidad,
                    Existencia,ExistenMin,ExistenMax,
                    Precio,CostoActual
                    FROM Inventario WHERE Codigo=$cod";
                cmd.Parameters.AddWithValue("$cod", codigo);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return Results.NotFound(new { error = "Producto no encontrado." });
                var stock = Db.D(r,4); var min = Db.D(r,5);
                var item = new { codigo=Db.S(r,0), descripcion=Db.S(r,1),
                    departamento=Db.S(r,2), unidad=Db.S(r,3),
                    stock, stockMin=min, precioActual=Db.D(r,7), costoActual=Db.D(r,8),
                    bajMin=min>0&&stock<min };
                r.Close();

                var movs = new List<object>();
                var cmd2 = c.CreateCommand();
                cmd2.CommandText = @"SELECT FechaHora,TipoMovimiento,Concepto,Cantidad,NuevoSaldo,Usuario
                    FROM MovimientosInventario WHERE CodigoProducto=$cod
                    ORDER BY IdMovimiento DESC LIMIT 15";
                cmd2.Parameters.AddWithValue("$cod", codigo);
                using var r2 = cmd2.ExecuteReader();
                while (r2.Read())
                    movs.Add(new { fecha=Db.S(r2,0), tipo=Db.S(r2,1), concepto=Db.S(r2,2),
                        cantidad=Db.D(r2,3), saldo=Db.D(r2,4), usuario=Db.S(r2,5) });

                return Results.Ok(new { item, movimientos = movs });
            }).RequireAuthorization();

            app.MapGet("/api/insumos", (string? q) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Id,CodigoInterno,Descripcion,Unidad,
                    StockActual,CostoPromedio,StockMinimo
                    FROM ProductosProveedor ORDER BY Descripcion";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var cod = Db.S(r,1); var desc = Db.S(r,2);
                    if (!string.IsNullOrWhiteSpace(q) &&
                        !cod.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                        !desc.Contains(q, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var stock = Db.D(r,4); var min = Db.D(r,6);
                    lista.Add(new { id=Db.I(r,0), codigoInterno=cod, descripcion=desc,
                        unidad=Db.S(r,3), stock, costoPromedio=Db.D(r,5),
                        stockMinimo=min, bajMin=min>0&&stock<min });
                }
                return Results.Ok(lista);
            }).RequireAuthorization();
        }
    }
}
