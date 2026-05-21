namespace SistemaGestion.Api.Endpoints
{
    public static class DashboardEndpoints
    {
        public static void MapDashboardEndpoints(this WebApplication app)
        {
            app.MapGet("/api/dashboard", () =>
            {
                try
                {
                    using var c = Db.Conn();
                    var hoy = DateTime.Today.ToString("yyyy-MM-dd");

                    // Ventas hoy
                    var cmd = c.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*), COALESCE(SUM(TotalF),0) FROM Facturas WHERE Fecha=$h";
                    cmd.Parameters.AddWithValue("$h", hoy);
                    using var r = cmd.ExecuteReader();
                    r.Read();
                    int factHoy = r.GetInt32(0);
                    decimal ventasHoy = Db.D(r, 1);
                    r.Close();

                    // CxC vencidas
                    cmd = c.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM CuentasCobrar WHERE Estado!='Pagada' AND FechaVence<$h AND (Monto-Pagado)>0";
                    cmd.Parameters.AddWithValue("$h", hoy);
                    int cxcVencidas = Convert.ToInt32(cmd.ExecuteScalar());

                    // Stock bajo
                    cmd = c.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM Inventario WHERE ExistenMin>0 AND Existencia<ExistenMin AND (Activo IS NULL OR Activo=1)";
                    int stockBajo = Convert.ToInt32(cmd.ExecuteScalar());

                    // Alertas
                    cmd = c.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM ProductosProveedor WHERE StockMinimo>0 AND StockActual<StockMinimo";
                    int insumosbajos = 0;
                    try { insumosbajos = Convert.ToInt32(cmd.ExecuteScalar()); } catch { }

                    // Tasa de cambio
                    decimal tasaBCV = 0, tasaPar = 0; string fechaTasa = "";
                    cmd = c.CreateCommand();
                    cmd.CommandText = "SELECT TasaBCV, TasaParalelo, Fecha FROM TasaCambio ORDER BY Fecha DESC LIMIT 1";
                    try
                    {
                        using var rt = cmd.ExecuteReader();
                        if (rt.Read()) { tasaBCV = Db.D(rt,0); tasaPar = Db.D(rt,1); fechaTasa = Db.S(rt,2); }
                    }
                    catch { }

                    // Egresos del mes en curso
                    decimal egresosMesUSD = 0; int egresosMesCant = 0;
                    var primerDiaMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
                                        .ToString("yyyy-MM-dd");
                    cmd = c.CreateCommand();
                    cmd.CommandText = @"SELECT COUNT(*), COALESCE(SUM(MontoUSD),0)
                                        FROM Gastos WHERE Fecha >= $d";
                    cmd.Parameters.AddWithValue("$d", primerDiaMes);
                    try
                    {
                        using var re = cmd.ExecuteReader();
                        if (re.Read()) { egresosMesCant = re.GetInt32(0); egresosMesUSD = Db.D(re, 1); }
                    }
                    catch { }

                    return Results.Ok(new
                    {
                        fecha            = DateTime.Today.ToString("dd/MM/yyyy"),
                        ventasHoyUsd     = ventasHoy,
                        facturasEmitidas = factHoy,
                        cxcVencidas,
                        stockBajo,
                        egresosMesUSD,
                        egresosMesCant,
                        alertas = new
                        {
                            stockBajoCount   = stockBajo,
                            cxcVencidasCount = cxcVencidas,
                            insumosCount     = insumosbajos,
                            tieneAlertas     = stockBajo > 0 || cxcVencidas > 0
                        },
                        tasaCambio = tasaBCV > 0 ? new { tasaBCV, tasaParalelo = tasaPar, fecha = fechaTasa } : null
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DASHBOARD ERROR] {ex}");
                    return Results.Problem("Error al cargar el dashboard.", statusCode: 500);
                }
            }).RequireAuthorization();

            app.MapGet("/api/dashboard/alertas", () =>
            {
                try
                {
                    using var c = Db.Conn();
                    var hoy = DateTime.Today.ToString("yyyy-MM-dd");
                    var stockBajo = new List<object>();
                    var cxcVenc   = new List<object>();

                    var cmd = c.CreateCommand();
                    cmd.CommandText = @"SELECT Codigo,Descripcion,Existencia,ExistenMin
                        FROM Inventario WHERE ExistenMin>0 AND Existencia<ExistenMin
                        AND (Activo IS NULL OR Activo=1) ORDER BY (Existencia/ExistenMin) ASC LIMIT 20";
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            stockBajo.Add(new { codigo=Db.S(r,0), descripcion=Db.S(r,1),
                                actual=Db.D(r,2), minimo=Db.D(r,3), faltante=Db.D(r,3)-Db.D(r,2) });

                    cmd = c.CreateCommand();
                    cmd.CommandText = @"SELECT Cliente,Concepto,Monto-Pagado,FechaVence
                        FROM CuentasCobrar WHERE Estado!='Pagada' AND FechaVence<$h AND (Monto-Pagado)>0
                        ORDER BY FechaVence ASC LIMIT 20";
                    cmd.Parameters.AddWithValue("$h", hoy);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            cxcVenc.Add(new { cliente=Db.S(r,0), concepto=Db.S(r,1),
                                saldo=Db.D(r,2), vencio=Db.S(r,3) });

                    return Results.Ok(new { stockBajo, cxcVencidas = cxcVenc });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DASHBOARD ALERTAS ERROR] {ex}");
                    return Results.Problem("Error al cargar las alertas.", statusCode: 500);
                }
            }).RequireAuthorization();
        }
    }
}
