namespace SistemaGestion.Api.Endpoints
{
    public static class NominaEndpoints
    {
        public static void MapNominaEndpoints(this WebApplication app)
        {
            // ════════════════════════════════════════════════════════════════
            //  EMPLEADOS
            // ════════════════════════════════════════════════════════════════
            app.MapGet("/api/nomina/empleados", (bool? soloActivos) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                bool filtrar = soloActivos ?? true;
                cmd.CommandText = @"SELECT Id,Cedula,Nombre,Apellido,Cargo,TipoEmpleado,
                    FechaIngreso,Activo,Telefono,Email,
                    SueldoDiarioUSD,TarifaPiezaUSD,TarifaVentaUSD,SalarioNormalUSD
                    FROM Empleados" + (filtrar?" WHERE Activo=1":"") + " ORDER BY Apellido,Nombre";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(MapEmpleado(r));
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapPost("/api/nomina/empleados", (EmpleadoRequest req) =>
            {
                if (string.IsNullOrWhiteSpace(req.Cedula) || string.IsNullOrWhiteSpace(req.Nombre))
                    return Results.BadRequest(new { error = "Cédula y nombre son requeridos." });
                using var c = Db.Conn();
                var ins = c.CreateCommand();
                ins.CommandText = @"INSERT INTO Empleados
                    (Cedula,Nombre,Apellido,Cargo,TipoEmpleado,FechaIngreso,Activo,
                     Telefono,Email,SueldoDiarioUSD,TarifaPiezaUSD,TarifaVentaUSD,SalarioNormalUSD)
                    VALUES($ci,$no,$ap,$ca,$te,$fi,1,$tf,$em,$sd,$tp,$tv,$sn)";
                ins.Parameters.AddWithValue("$ci", req.Cedula.Trim());
                ins.Parameters.AddWithValue("$no", req.Nombre.Trim());
                ins.Parameters.AddWithValue("$ap", req.Apellido?.Trim() ?? "");
                ins.Parameters.AddWithValue("$ca", req.Cargo?.Trim() ?? "");
                ins.Parameters.AddWithValue("$te", req.TipoEmpleado ?? "Fijo");
                ins.Parameters.AddWithValue("$fi", req.FechaIngreso ?? DateTime.Today.ToString("yyyy-MM-dd"));
                ins.Parameters.AddWithValue("$tf", req.Telefono ?? "");
                ins.Parameters.AddWithValue("$em", req.Email ?? "");
                ins.Parameters.AddWithValue("$sd", (double)req.SueldoDiarioUSD);
                ins.Parameters.AddWithValue("$tp", (double)req.TarifaPiezaUSD);
                ins.Parameters.AddWithValue("$tv", (double)req.TarifaVentaUSD);
                ins.Parameters.AddWithValue("$sn", (double)req.SalarioNormalUSD);
                try { ins.ExecuteNonQuery(); }
                catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 19)
                    { return Results.Conflict(new { error = "Ya existe un empleado con esa cédula." }); }
                var id = c.CreateCommand();
                id.CommandText = "SELECT last_insert_rowid()";
                int newId = (int)(long)(id.ExecuteScalar() ?? 0L);
                return Results.Created($"/api/nomina/empleados/{newId}", new { id = newId });
            }).RequireAuthorization();

            app.MapPut("/api/nomina/empleados/{id:int}", (int id, EmpleadoRequest req) =>
            {
                using var c = Db.Conn();
                var upd = c.CreateCommand();
                upd.CommandText = @"UPDATE Empleados SET
                    Cedula=$ci,Nombre=$no,Apellido=$ap,Cargo=$ca,TipoEmpleado=$te,
                    FechaIngreso=$fi,Telefono=$tf,Email=$em,
                    SueldoDiarioUSD=$sd,TarifaPiezaUSD=$tp,TarifaVentaUSD=$tv,SalarioNormalUSD=$sn
                    WHERE Id=$id";
                upd.Parameters.AddWithValue("$id", id);
                upd.Parameters.AddWithValue("$ci", req.Cedula?.Trim() ?? "");
                upd.Parameters.AddWithValue("$no", req.Nombre?.Trim() ?? "");
                upd.Parameters.AddWithValue("$ap", req.Apellido?.Trim() ?? "");
                upd.Parameters.AddWithValue("$ca", req.Cargo?.Trim() ?? "");
                upd.Parameters.AddWithValue("$te", req.TipoEmpleado ?? "Fijo");
                upd.Parameters.AddWithValue("$fi", req.FechaIngreso ?? "");
                upd.Parameters.AddWithValue("$tf", req.Telefono ?? "");
                upd.Parameters.AddWithValue("$em", req.Email ?? "");
                upd.Parameters.AddWithValue("$sd", (double)req.SueldoDiarioUSD);
                upd.Parameters.AddWithValue("$tp", (double)req.TarifaPiezaUSD);
                upd.Parameters.AddWithValue("$tv", (double)req.TarifaVentaUSD);
                upd.Parameters.AddWithValue("$sn", (double)req.SalarioNormalUSD);
                int rows = upd.ExecuteNonQuery();
                return rows == 0 ? Results.NotFound() : Results.Ok(new { ok = true });
            }).RequireAuthorization();

            app.MapDelete("/api/nomina/empleados/{id:int}", (int id) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "UPDATE Empleados SET Activo=0 WHERE Id=$id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // ════════════════════════════════════════════════════════════════
            //  PERÍODOS
            // ════════════════════════════════════════════════════════════════
            app.MapGet("/api/nomina/periodos", () =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Id,FechaDesde,FechaHasta,TasaBCV,Estado,Observaciones
                    FROM PeriodosNomina ORDER BY Id DESC LIMIT 20";
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new { id=Db.I(r,0), desde=Db.S(r,1), hasta=Db.S(r,2),
                        tasa=Db.D(r,3), estado=Db.S(r,4), obs=Db.S(r,5) });
                return Results.Ok(lista);
            }).RequireAuthorization();

            app.MapPost("/api/nomina/periodos", (PeriodoRequest req) =>
            {
                if (string.IsNullOrWhiteSpace(req.Desde) || string.IsNullOrWhiteSpace(req.Hasta))
                    return Results.BadRequest(new { error = "Fechas desde/hasta requeridas." });
                using var c = Db.Conn();
                // Obtener tasa BCV actual si no se especifica
                decimal tasa = req.TasaBCV > 0 ? req.TasaBCV : 1;
                if (tasa == 1) {
                    try {
                        var tc = c.CreateCommand();
                        tc.CommandText = "SELECT TasaBCV FROM TasaCambio ORDER BY Fecha DESC LIMIT 1";
                        var t = tc.ExecuteScalar();
                        if (t != null && t != DBNull.Value) tasa = (decimal)(double)t;
                    } catch {}
                }
                var ins = c.CreateCommand();
                ins.CommandText = @"INSERT INTO PeriodosNomina(FechaDesde,FechaHasta,TasaBCV,Estado,Observaciones)
                    VALUES($d,$h,$t,'Abierto',$o)";
                ins.Parameters.AddWithValue("$d", req.Desde);
                ins.Parameters.AddWithValue("$h", req.Hasta);
                ins.Parameters.AddWithValue("$t", (double)tasa);
                ins.Parameters.AddWithValue("$o", req.Observaciones ?? "");
                ins.ExecuteNonQuery();
                var id = c.CreateCommand();
                id.CommandText = "SELECT last_insert_rowid()";
                int newId = (int)(long)(id.ExecuteScalar() ?? 0L);
                return Results.Created($"/api/nomina/periodos/{newId}", new { id=newId, tasa });
            }).RequireAuthorization();

            // ════════════════════════════════════════════════════════════════
            //  DETALLES / PROCESAMIENTO
            // ════════════════════════════════════════════════════════════════
            app.MapGet("/api/nomina/periodos/{id:int}/detalles", (int id) =>
            {
                using var c = Db.Conn();
                // Datos del período
                var cp = c.CreateCommand();
                cp.CommandText = "SELECT Id,FechaDesde,FechaHasta,TasaBCV,Estado FROM PeriodosNomina WHERE Id=$id";
                cp.Parameters.AddWithValue("$id", id);
                using var rp = cp.ExecuteReader();
                if (!rp.Read()) return Results.NotFound(new { error = "Período no encontrado." });
                var periodo = new { id=Db.I(rp,0), desde=Db.S(rp,1), hasta=Db.S(rp,2),
                    tasa=Db.D(rp,3), estado=Db.S(rp,4) };
                rp.Close();

                // Detalles
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Id,IdEmpleado,NombreEmpleado,TipoEmpleado,
                    DiasTrabajados,DiasInasistencia,PiezasProducidas,VentasCerradas,
                    SueldoDiarioUSD,DevengadoSueldo,DevengadoPiezas,DevengadoVentas,OtrosIngresos,
                    DeduccionIVSS,DeduccionRPVH,DescuentoFaltas,CuotaPrestamo,OtrasDeduccion,
                    TasaBCV,Observaciones
                    FROM DetallesNomina WHERE IdPeriodo=$id ORDER BY NombreEmpleado";
                cmd.Parameters.AddWithValue("$id", id);
                var detalles = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    detalles.Add(MapDetalle(r));

                return Results.Ok(new { periodo, detalles });
            }).RequireAuthorization();

            // Procesar: genera/regenera detalles para todos los empleados activos
            app.MapPost("/api/nomina/periodos/{id:int}/procesar", (int id) =>
            {
                using var c = Db.Conn();
                var cp = c.CreateCommand();
                cp.CommandText = "SELECT FechaDesde,FechaHasta,TasaBCV,Estado FROM PeriodosNomina WHERE Id=$id";
                cp.Parameters.AddWithValue("$id", id);
                using var rp = cp.ExecuteReader();
                if (!rp.Read()) return Results.NotFound(new { error = "Período no encontrado." });
                string desde=Db.S(rp,0), hasta=Db.S(rp,1);
                decimal tasa=Db.D(rp,2); string estado=Db.S(rp,3);
                rp.Close();
                if (estado == "Cerrado")
                    return Results.BadRequest(new { error = "El período está cerrado." });

                // Días calendario del período
                DateTime dtDesde = DateTime.Parse(desde), dtHasta = DateTime.Parse(hasta);
                decimal diasPeriodo = (decimal)(dtHasta - dtDesde).TotalDays + 1;

                var cemp = c.CreateCommand();
                // cols: 0=Id 1=Nombre 2=Apellido 3=TipoEmpleado 4=SueldoDiarioUSD 5=TarifaPiezaUSD 6=TarifaVentaUSD
                cemp.CommandText = @"SELECT Id,Nombre,Apellido,TipoEmpleado,
                    SueldoDiarioUSD,TarifaPiezaUSD,TarifaVentaUSD
                    FROM Empleados WHERE Activo=1 ORDER BY Apellido,Nombre";
                var empleados = new List<(int id, string nombre, string tipo, decimal diarioUSD, decimal tarifaPieza, decimal tarifaVenta)>();
                using var re = cemp.ExecuteReader();
                while (re.Read())
                    empleados.Add((Db.I(re,0),
                        $"{Db.S(re,1)} {Db.S(re,2)}",
                        Db.S(re,3), Db.D(re,4), Db.D(re,5), Db.D(re,6)));
                re.Close();

                // Calcular producción del período por empleado
                int procesados = 0;
                foreach (var (empId, nombre, tipo, diarioUSD, tarifaPieza, tarifaVenta) in empleados)
                {
                    // Producción en el período
                    decimal piezas = 0;
                    if (tipo == "Produccion") {
                        var qp = c.CreateCommand();
                        qp.CommandText = @"SELECT COALESCE(SUM(CantPrimeraCalidad+CantSegundaCalidad),0)
                            FROM RegistroProduccionDia WHERE Fecha BETWEEN $d AND $h AND Operario LIKE $o";
                        qp.Parameters.AddWithValue("$d", desde);
                        qp.Parameters.AddWithValue("$h", hasta);
                        qp.Parameters.AddWithValue("$o", "%" + nombre.Split(' ')[0] + "%");
                        try { piezas = (decimal)(double)(qp.ExecuteScalar() ?? 0.0); } catch {}
                    }

                    // Ventas cerradas (facturas del período)
                    decimal ventas = 0;
                    if (tipo == "Oficina" || tipo == "Ventas") {
                        var qv = c.CreateCommand();
                        qv.CommandText = "SELECT COUNT(*) FROM Facturas WHERE Fecha BETWEEN $d AND $h";
                        qv.Parameters.AddWithValue("$d", desde); qv.Parameters.AddWithValue("$h", hasta);
                        try { ventas = (decimal)(long)(qv.ExecuteScalar() ?? 0L); } catch {}
                    }

                    // Cuota préstamo activo
                    decimal cuota = 0;
                    var qpr = c.CreateCommand();
                    qpr.CommandText = @"SELECT COALESCE(SUM(CuotaSemanalUSD),0)
                        FROM PrestamosEmpleado WHERE IdEmpleado=$id AND Estado='Activo'";
                    qpr.Parameters.AddWithValue("$id", empId);
                    try { cuota = (decimal)(double)(qpr.ExecuteScalar() ?? 0.0); } catch {}

                    // Calcular devengados
                    decimal devSueldo = diarioUSD * diasPeriodo;
                    decimal devPiezas = piezas * tarifaPieza;   // tarifaPieza en USD/pieza
                    decimal devVentas = ventas * tarifaVenta;   // tarifaVenta en USD/venta
                    decimal totalDev   = devSueldo + devPiezas + devVentas;
                    decimal totalDevBs = totalDev * tasa;
                    decimal ivss  = Math.Round(totalDevBs * 0.04m / tasa, 2);
                    decimal rpvh  = Math.Round(totalDevBs * 0.01m / tasa, 2);

                    // Upsert detalle
                    var ups = c.CreateCommand();
                    ups.CommandText = @"INSERT INTO DetallesNomina
                        (IdPeriodo,IdEmpleado,NombreEmpleado,TipoEmpleado,
                         DiasTrabajados,DiasInasistencia,PiezasProducidas,VentasCerradas,
                         SueldoDiarioUSD,DevengadoSueldo,DevengadoPiezas,DevengadoVentas,OtrosIngresos,
                         DeduccionIVSS,DeduccionRPVH,DescuentoFaltas,CuotaPrestamo,OtrasDeduccion,
                         TasaBCV,Observaciones)
                        VALUES($ip,$ie,$ne,$te,$dt,0,$pp,$vc,$sd,$ds,$dp,$dv,0,$ii,$rp,0,$cp,0,$ta,'')
                        ON CONFLICT(IdPeriodo,IdEmpleado) DO UPDATE SET
                            DiasTrabajados=excluded.DiasTrabajados,
                            PiezasProducidas=excluded.PiezasProducidas,
                            VentasCerradas=excluded.VentasCerradas,
                            SueldoDiarioUSD=excluded.SueldoDiarioUSD,
                            DevengadoSueldo=excluded.DevengadoSueldo,
                            DevengadoPiezas=excluded.DevengadoPiezas,
                            DevengadoVentas=excluded.DevengadoVentas,
                            DeduccionIVSS=excluded.DeduccionIVSS,
                            DeduccionRPVH=excluded.DeduccionRPVH,
                            CuotaPrestamo=excluded.CuotaPrestamo,
                            TasaBCV=excluded.TasaBCV";
                    ups.Parameters.AddWithValue("$ip", id);
                    ups.Parameters.AddWithValue("$ie", empId);
                    ups.Parameters.AddWithValue("$ne", nombre);
                    ups.Parameters.AddWithValue("$te", tipo);
                    ups.Parameters.AddWithValue("$dt", (double)diasPeriodo);
                    ups.Parameters.AddWithValue("$pp", (double)piezas);
                    ups.Parameters.AddWithValue("$vc", (double)ventas);
                    ups.Parameters.AddWithValue("$sd", (double)diarioUSD);
                    ups.Parameters.AddWithValue("$ds", (double)devSueldo);
                    ups.Parameters.AddWithValue("$dp", (double)devPiezas);
                    ups.Parameters.AddWithValue("$dv", (double)devVentas);
                    ups.Parameters.AddWithValue("$ii", (double)ivss);
                    ups.Parameters.AddWithValue("$rp", (double)rpvh);
                    ups.Parameters.AddWithValue("$cp", (double)cuota);
                    ups.Parameters.AddWithValue("$ta", (double)tasa);
                    try { ups.ExecuteNonQuery(); procesados++; } catch {}
                }
                return Results.Ok(new { procesados, diasPeriodo });
            }).RequireAuthorization();

            // Actualizar una línea de detalle manualmente
            app.MapPut("/api/nomina/detalles/{id:int}", (int id, DetalleNominaRequest req) =>
            {
                using var c = Db.Conn();
                // Recalcular
                decimal tasa = req.TasaBCV > 0 ? req.TasaBCV : 1;
                decimal devSueldo = req.SueldoDiarioUSD * req.DiasTrabajados;
                decimal totalDev  = devSueldo + req.DevengadoPiezas + req.DevengadoVentas + req.OtrosIngresos;
                decimal totalDevBs = totalDev * tasa;
                decimal ivss = Math.Round(totalDevBs * 0.04m / tasa, 2);
                decimal rpvh = Math.Round(totalDevBs * 0.01m / tasa, 2);
                decimal descFaltas = req.SueldoDiarioUSD * req.DiasInasistencia;

                var upd = c.CreateCommand();
                upd.CommandText = @"UPDATE DetallesNomina SET
                    DiasTrabajados=$dt,DiasInasistencia=$di,PiezasProducidas=$pp,VentasCerradas=$vc,
                    SueldoDiarioUSD=$sd,DevengadoSueldo=$ds,DevengadoPiezas=$dp,DevengadoVentas=$dv,
                    OtrosIngresos=$oi,DeduccionIVSS=$ii,DeduccionRPVH=$rp,DescuentoFaltas=$df,
                    CuotaPrestamo=$cp,OtrasDeduccion=$od,TasaBCV=$ta,Observaciones=$ob
                    WHERE Id=$id";
                upd.Parameters.AddWithValue("$id", id);
                upd.Parameters.AddWithValue("$dt", (double)req.DiasTrabajados);
                upd.Parameters.AddWithValue("$di", (double)req.DiasInasistencia);
                upd.Parameters.AddWithValue("$pp", (double)req.PiezasProducidas);
                upd.Parameters.AddWithValue("$vc", (double)req.VentasCerradas);
                upd.Parameters.AddWithValue("$sd", (double)req.SueldoDiarioUSD);
                upd.Parameters.AddWithValue("$ds", (double)devSueldo);
                upd.Parameters.AddWithValue("$dp", (double)req.DevengadoPiezas);
                upd.Parameters.AddWithValue("$dv", (double)req.DevengadoVentas);
                upd.Parameters.AddWithValue("$oi", (double)req.OtrosIngresos);
                upd.Parameters.AddWithValue("$ii", (double)ivss);
                upd.Parameters.AddWithValue("$rp", (double)rpvh);
                upd.Parameters.AddWithValue("$df", (double)descFaltas);
                upd.Parameters.AddWithValue("$cp", (double)req.CuotaPrestamo);
                upd.Parameters.AddWithValue("$od", (double)req.OtrasDeduccion);
                upd.Parameters.AddWithValue("$ta", (double)tasa);
                upd.Parameters.AddWithValue("$ob", req.Observaciones ?? "");
                int rows = upd.ExecuteNonQuery();
                return rows == 0 ? Results.NotFound() : Results.Ok(new { ok=true,
                    devengado=totalDev, deducciones=ivss+rpvh+descFaltas+req.CuotaPrestamo+req.OtrasDeduccion,
                    neto=totalDev-(ivss+rpvh+descFaltas+req.CuotaPrestamo+req.OtrasDeduccion),
                    netoBs=(totalDev-(ivss+rpvh+descFaltas+req.CuotaPrestamo+req.OtrasDeduccion))*tasa });
            }).RequireAuthorization();

            // Cerrar período
            app.MapPut("/api/nomina/periodos/{id:int}/cerrar", (int id) =>
            {
                using var c = Db.Conn();
                var upd = c.CreateCommand();
                upd.CommandText = "UPDATE PeriodosNomina SET Estado='Cerrado' WHERE Id=$id";
                upd.Parameters.AddWithValue("$id", id);
                int rows = upd.ExecuteNonQuery();
                return rows == 0 ? Results.NotFound() : Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // Reabrir período (solo admin)
            app.MapPut("/api/nomina/periodos/{id:int}/reabrir", (int id, HttpContext ctx) =>
            {
                var rol = ctx.User?.FindFirst("rol")?.Value ?? "";
                if (rol != "Admin") return Results.Forbid();
                using var c = Db.Conn();
                var upd = c.CreateCommand();
                upd.CommandText = "UPDATE PeriodosNomina SET Estado='Abierto' WHERE Id=$id";
                upd.Parameters.AddWithValue("$id", id);
                upd.ExecuteNonQuery();
                return Results.Ok(new { ok = true });
            }).RequireAuthorization();

            // ── Recibos HTML imprimibles ──────────────────────────────────
            app.MapGet("/api/nomina/periodos/{id:int}/recibos", async (int id, HttpContext ctx) =>
            {
                using var c = Db.Conn();
                var cp = c.CreateCommand();
                cp.CommandText = "SELECT FechaDesde,FechaHasta,TasaBCV FROM PeriodosNomina WHERE Id=$id";
                cp.Parameters.AddWithValue("$id", id);
                using var rp = cp.ExecuteReader();
                if (!rp.Read()) { ctx.Response.StatusCode=404; return; }
                string desde=Db.S(rp,0), hasta=Db.S(rp,1); decimal tasa=Db.D(rp,2);
                rp.Close();

                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT Id,NombreEmpleado,TipoEmpleado,
                    DiasTrabajados,DiasInasistencia,PiezasProducidas,VentasCerradas,
                    SueldoDiarioUSD,DevengadoSueldo,DevengadoPiezas,DevengadoVentas,OtrosIngresos,
                    DeduccionIVSS,DeduccionRPVH,DescuentoFaltas,CuotaPrestamo,OtrasDeduccion,TasaBCV
                    FROM DetallesNomina WHERE IdPeriodo=$id ORDER BY NombreEmpleado";
                cmd.Parameters.AddWithValue("$id", id);

                var recibos = new System.Text.StringBuilder();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    string nombre=Db.S(r,1), tipo=Db.S(r,2);
                    decimal dias=Db.D(r,3), inas=Db.D(r,4), piezas=Db.D(r,5);
                    decimal sdiario=Db.D(r,7), devS=Db.D(r,8), devP=Db.D(r,9), devV=Db.D(r,10), otros=Db.D(r,11);
                    decimal ivss=Db.D(r,12), rpvh=Db.D(r,13), faltas=Db.D(r,14), prestamo=Db.D(r,15), otrasD=Db.D(r,16);
                    decimal tasaR=Db.D(r,17); if(tasaR<=0)tasaR=tasa;
                    decimal totalDev=devS+devP+devV+otros;
                    decimal totalDed=ivss+rpvh+faltas+prestamo+otrasD;
                    decimal neto=totalDev-totalDed;

                    recibos.Append($"""
                    <div class="recibo">
                      <div class="rec-header">
                        <div><strong>RECIBO DE PAGO</strong><br><span class="dim">Período: {H(desde)} al {H(hasta)}</span></div>
                        <div style="text-align:right"><strong style="font-size:13pt">{H(nombre)}</strong><br><span class="dim">{H(tipo)}</span></div>
                      </div>
                      <table>
                        <tr class="sec"><td colspan="2"><strong>Devengados</strong></td><td class="r"><strong>USD</strong></td></tr>
                        <tr><td colspan="2">Sueldo ({dias:N0} días × {sdiario:N2})</td><td class="r">{devS:N2}</td></tr>
                        {(devP>0?$"<tr><td colspan='2'>Bono producción ({piezas:N0} pzas)</td><td class='r'>{devP:N2}</td></tr>":"")}
                        {(devV>0?$"<tr><td colspan='2'>Bono ventas</td><td class='r'>{devV:N2}</td></tr>":"")}
                        {(otros>0?$"<tr><td colspan='2'>Otros ingresos</td><td class='r'>{otros:N2}</td></tr>":"")}
                        <tr class="subtotal"><td colspan="2">Total devengado</td><td class="r">{totalDev:N2}</td></tr>
                        <tr class="sec"><td colspan="2"><strong>Deducciones</strong></td><td class="r"></td></tr>
                        {(ivss>0?$"<tr><td colspan='2'>IVSS (4%)</td><td class='r'>-{ivss:N2}</td></tr>":"")}
                        {(rpvh>0?$"<tr><td colspan='2'>RPVH (1%)</td><td class='r'>-{rpvh:N2}</td></tr>":"")}
                        {(faltas>0?$"<tr><td colspan='2'>Descuento inasistencias ({inas:N0} días)</td><td class='r'>-{faltas:N2}</td></tr>":"")}
                        {(prestamo>0?$"<tr><td colspan='2'>Cuota préstamo</td><td class='r'>-{prestamo:N2}</td></tr>":"")}
                        {(otrasD>0?$"<tr><td colspan='2'>Otras deducciones</td><td class='r'>-{otrasD:N2}</td></tr>":"")}
                        <tr class="subtotal"><td colspan="2">Total deducciones</td><td class="r">-{totalDed:N2}</td></tr>
                        <tr class="neto"><td colspan="2">NETO A PAGAR (USD)</td><td class="r">{neto:N2}</td></tr>
                        <tr class="neto-bs"><td colspan="2">NETO EN BOLÍVARES (× {tasaR:N2})</td><td class="r">{neto*tasaR:N2}</td></tr>
                      </table>
                      <div class="firma">
                        <div class="firma-box">Firma del empleado</div>
                        <div class="firma-box">Firma empleador</div>
                      </div>
                    </div>
                    """);
                }

                string html = $"""
<!DOCTYPE html><html lang="es"><head>
<meta charset="UTF-8"><title>Recibos de Pago — {H(desde)} al {H(hasta)}</title>
<style>
*{{box-sizing:border-box;margin:0;padding:0;font-family:'Segoe UI',Arial,sans-serif;font-size:10pt}}
body{{padding:10mm;color:#111}}
.recibo{{border:1px solid #ccc;border-radius:4px;padding:6mm;margin-bottom:8mm;page-break-inside:avoid}}
.rec-header{{display:flex;justify-content:space-between;align-items:flex-start;
    border-bottom:2px solid #1A4A8A;padding-bottom:3mm;margin-bottom:4mm}}
.dim{{color:#555;font-size:9pt}}
table{{width:100%;border-collapse:collapse;font-size:9.5pt}}
tr td{{padding:1.5mm 2mm}}
.sec td{{background:#1A4A8A;color:#fff;padding:2mm}}
.subtotal td{{border-top:1px solid #ccc;font-weight:600}}
.neto td{{background:#e8f4e8;font-weight:700;font-size:11pt;padding:2mm}}
.neto-bs td{{background:#f0f0f0;font-weight:600;font-size:10pt;padding:2mm}}
.r{{text-align:right}}
.firma{{display:flex;gap:10mm;margin-top:6mm}}
.firma-box{{flex:1;border-top:1px solid #999;padding-top:2mm;font-size:9pt;color:#555;text-align:center}}
@media print{{
  body{{padding:5mm}}
  .no-print{{display:none!important}}
  .recibo{{margin-bottom:4mm}}
}}
</style></head><body>
<button class="no-print" onclick="window.print()"
  style="margin-bottom:8mm;padding:2mm 8mm;background:#1A4A8A;color:#fff;border:none;
         border-radius:4px;cursor:pointer;font-size:11pt">🖨 Imprimir todos los recibos</button>
<div class="no-print" style="margin-bottom:6mm;color:#555">
  Período: {H(desde)} al {H(hasta)} — Tasa BCV: {tasa:N2} Bs/USD
</div>
{recibos}
</body></html>
""";
                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.WriteAsync(html);
            }).RequireAuthorization();
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private static object MapEmpleado(Microsoft.Data.Sqlite.SqliteDataReader r) => new {
            id=Db.I(r,0), cedula=Db.S(r,1), nombre=Db.S(r,2), apellido=Db.S(r,3),
            nombreCompleto=$"{Db.S(r,2)} {Db.S(r,3)}",
            cargo=Db.S(r,4), tipoEmpleado=Db.S(r,5), fechaIngreso=Db.S(r,6),
            activo=Db.I(r,7)==1, telefono=Db.S(r,8), email=Db.S(r,9),
            sueldoDiarioUSD=Db.D(r,10), tarifaPiezaUSD=Db.D(r,11),
            tarifaVentaUSD=Db.D(r,12), salarioNormalUSD=Db.D(r,13)
        };

        private static object MapDetalle(Microsoft.Data.Sqlite.SqliteDataReader r)
        {
            decimal devS=Db.D(r,9),devP=Db.D(r,10),devV=Db.D(r,11),otros=Db.D(r,12);
            decimal ivss=Db.D(r,13),rpvh=Db.D(r,14),faltas=Db.D(r,15),prestamo=Db.D(r,16),otrasD=Db.D(r,17);
            decimal tasa=Db.D(r,18);
            decimal totalDev=devS+devP+devV+otros;
            decimal totalDed=ivss+rpvh+faltas+prestamo+otrasD;
            decimal neto=totalDev-totalDed;
            return new {
                id=Db.I(r,0), idEmpleado=Db.I(r,1), nombre=Db.S(r,2), tipo=Db.S(r,3),
                diasTrabajados=Db.D(r,4), diasInasistencia=Db.D(r,5),
                piezasProducidas=Db.D(r,6), ventasCerradas=Db.D(r,7),
                sueldoDiario=Db.D(r,8), devSueldo=devS, devPiezas=devP, devVentas=devV,
                otrosIngresos=otros, totalDevengado=totalDev,
                ivss, rpvh, faltas, prestamo, otrasDeduccion=otrasD,
                totalDeducciones=totalDed, netoUSD=neto, tasa,
                netoBs=neto*tasa, obs=Db.S(r,19)
            };
        }

        private static string H(string? s) => System.Web.HttpUtility.HtmlEncode(s ?? "");
    }

    // ── DTOs ──────────────────────────────────────────────────────────────
    public record EmpleadoRequest(
        string  Cedula, string Nombre, string? Apellido, string? Cargo,
        string  TipoEmpleado, string? FechaIngreso, string? Telefono, string? Email,
        decimal SueldoDiarioUSD, decimal TarifaPiezaUSD, decimal TarifaVentaUSD, decimal SalarioNormalUSD);

    public record PeriodoRequest(string Desde, string Hasta, decimal TasaBCV, string? Observaciones);

    public record DetalleNominaRequest(
        decimal DiasTrabajados, decimal DiasInasistencia, decimal PiezasProducidas,
        decimal VentasCerradas, decimal SueldoDiarioUSD,
        decimal DevengadoPiezas, decimal DevengadoVentas, decimal OtrosIngresos,
        decimal CuotaPrestamo, decimal OtrasDeduccion, decimal TasaBCV, string? Observaciones);
}
