using SistemaGestion.Api.Auth;

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
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

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
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            app.MapDelete("/api/nomina/empleados/{id:int}", (int id) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "UPDATE Empleados SET Activo=0 WHERE Id=$id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                return Results.Ok(new { ok = true });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

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
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

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
                if (tasa <= 0) tasa = 1;

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
                var advertencias = new List<string>(); // #8 advertencias
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
                        // #8 advertencia si no hay datos
                        if (piezas == 0) advertencias.Add($"{nombre}: sin registros de producción en el período");
                    }

                    // Ventas cerradas (facturas del período)
                    decimal ventas = 0;
                    if (tipo == "Oficina" || tipo == "Ventas") {
                        var qv = c.CreateCommand();
                        qv.CommandText = "SELECT COUNT(*) FROM Facturas WHERE Fecha BETWEEN $d AND $h";
                        qv.Parameters.AddWithValue("$d", desde); qv.Parameters.AddWithValue("$h", hasta);
                        try { ventas = (decimal)(long)(qv.ExecuteScalar() ?? 0L); } catch {}
                        if (ventas == 0) advertencias.Add($"{nombre}: sin ventas registradas en el período");
                    }

                    // Cuota préstamo activo — #10 limitada al saldo pendiente
                    decimal cuota = 0;
                    var qpr = c.CreateCommand();
                    qpr.CommandText = @"SELECT COALESCE(SUM(CuotaSemanalUSD),0), COALESCE(SUM(SaldoPendiente),0)
                        FROM PrestamosEmpleado WHERE IdEmpleado=$id AND Estado='Activo'";
                    qpr.Parameters.AddWithValue("$id", empId);
                    try {
                        using var rpr2 = qpr.ExecuteReader();
                        if (rpr2.Read()) { cuota = Db.D(rpr2, 0); decimal saldo = Db.D(rpr2, 1); cuota = Math.Min(cuota, saldo); }
                    } catch {}

                    // Calcular devengados
                    decimal devSueldo = diarioUSD * diasPeriodo;
                    decimal devPiezas = piezas * tarifaPieza;   // tarifaPieza en USD/pieza
                    decimal devVentas = ventas * tarifaVenta;   // tarifaVenta en USD/venta
                    decimal totalDev   = devSueldo + devPiezas + devVentas;
                    decimal ivss  = Math.Round(totalDev * 0.04m, 2);
                    decimal rpvh  = Math.Round(totalDev * 0.01m, 2);

                    // Descuento por inasistencias registradas en el período
                    int diasInas = Db.ContarFaltasInjustificadas(empId, dtDesde, dtHasta);
                    decimal descFaltas;
                    if (tipo == "Produccion")
                        descFaltas = Db.CalcularDescuentoFaltas(empId, dtDesde, dtHasta, tasa);
                    else
                        descFaltas = diarioUSD * diasInas;

                    // Upsert detalle
                    var ups = c.CreateCommand();
                    ups.CommandText = @"INSERT INTO DetallesNomina
                        (IdPeriodo,IdEmpleado,NombreEmpleado,TipoEmpleado,
                         DiasTrabajados,DiasInasistencia,PiezasProducidas,VentasCerradas,
                         SueldoDiarioUSD,DevengadoSueldo,DevengadoPiezas,DevengadoVentas,OtrosIngresos,
                         DeduccionIVSS,DeduccionRPVH,DescuentoFaltas,CuotaPrestamo,OtrasDeduccion,
                         TasaBCV,Observaciones)
                        VALUES($ip,$ie,$ne,$te,$dt,$di,$pp,$vc,$sd,$ds,$dp,$dv,0,$ii,$rp,$df,$cp,0,$ta,'')
                        ON CONFLICT(IdPeriodo,IdEmpleado) DO UPDATE SET
                            DiasTrabajados=excluded.DiasTrabajados,
                            DiasInasistencia=excluded.DiasInasistencia,
                            PiezasProducidas=excluded.PiezasProducidas,
                            VentasCerradas=excluded.VentasCerradas,
                            SueldoDiarioUSD=excluded.SueldoDiarioUSD,
                            DevengadoSueldo=excluded.DevengadoSueldo,
                            DevengadoPiezas=excluded.DevengadoPiezas,
                            DevengadoVentas=excluded.DevengadoVentas,
                            DeduccionIVSS=excluded.DeduccionIVSS,
                            DeduccionRPVH=excluded.DeduccionRPVH,
                            DescuentoFaltas=excluded.DescuentoFaltas,
                            CuotaPrestamo=excluded.CuotaPrestamo,
                            TasaBCV=excluded.TasaBCV";
                    ups.Parameters.AddWithValue("$ip", id);
                    ups.Parameters.AddWithValue("$ie", empId);
                    ups.Parameters.AddWithValue("$ne", nombre);
                    ups.Parameters.AddWithValue("$te", tipo);
                    ups.Parameters.AddWithValue("$dt", (double)diasPeriodo);
                    ups.Parameters.AddWithValue("$di", (double)diasInas);
                    ups.Parameters.AddWithValue("$pp", (double)piezas);
                    ups.Parameters.AddWithValue("$vc", (double)ventas);
                    ups.Parameters.AddWithValue("$sd", (double)diarioUSD);
                    ups.Parameters.AddWithValue("$ds", (double)devSueldo);
                    ups.Parameters.AddWithValue("$dp", (double)devPiezas);
                    ups.Parameters.AddWithValue("$dv", (double)devVentas);
                    ups.Parameters.AddWithValue("$ii", (double)ivss);
                    ups.Parameters.AddWithValue("$rp", (double)rpvh);
                    ups.Parameters.AddWithValue("$df", (double)descFaltas);
                    ups.Parameters.AddWithValue("$cp", (double)cuota);
                    ups.Parameters.AddWithValue("$ta", (double)tasa);
                    try { ups.ExecuteNonQuery(); procesados++; } catch {}

                    // Abonar préstamos activos del empleado
                    if (cuota > 0)
                    {
                        var qprs = c.CreateCommand();
                        qprs.CommandText = "SELECT Id, CuotaSemanalUSD, SaldoPendiente FROM PrestamosEmpleado WHERE IdEmpleado=$id AND Estado='Activo'";
                        qprs.Parameters.AddWithValue("$id", empId);
                        var prestamos = new List<(int id, decimal cuotaS, decimal saldo)>();
                        using var rpr = qprs.ExecuteReader();
                        while (rpr.Read())
                            prestamos.Add((Db.I(rpr, 0), Db.D(rpr, 1), Db.D(rpr, 2)));
                        rpr.Close();

                        decimal restante = cuota;
                        foreach (var pr in prestamos)
                        {
                            if (restante <= 0) break;
                            decimal abono = Math.Min(restante, pr.saldo);
                            if (abono <= 0) continue;
                            decimal nuevoSaldo = pr.saldo - abono;
                            var upr = c.CreateCommand();
                            upr.CommandText = nuevoSaldo <= 0
                                ? "UPDATE PrestamosEmpleado SET SaldoPendiente=0, Estado='Saldado' WHERE Id=$id"
                                : "UPDATE PrestamosEmpleado SET SaldoPendiente=$s WHERE Id=$id";
                            if (nuevoSaldo > 0) upr.Parameters.AddWithValue("$s", (double)nuevoSaldo);
                            upr.Parameters.AddWithValue("$id", pr.id);
                            upr.ExecuteNonQuery();
                            restante -= abono;
                        }
                    }
                }
                // #7 Auditoría
                AuthEndpoints.RegistrarAuditoria(c, "NOMINA_PROCESADA", "DetallesNomina",
                    $"Período={id}, empleados={procesados}");
                return Results.Ok(new { procesados, diasPeriodo, advertencias });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // Actualizar una línea de detalle manualmente
            app.MapPut("/api/nomina/detalles/{id:int}", (int id, DetalleNominaRequest req, HttpContext ctx) =>
            {
                // #5 Validar valores negativos
                if (req.DiasTrabajados < 0 || req.DiasInasistencia < 0 || req.PiezasProducidas < 0 ||
                    req.VentasCerradas < 0 || req.SueldoDiarioUSD < 0 || req.DevengadoPiezas < 0 ||
                    req.DevengadoVentas < 0 || req.OtrosIngresos < 0 || req.CuotaPrestamo < 0 || req.OtrasDeduccion < 0)
                    return Results.BadRequest(new { error = "No se permiten valores negativos." });

                using var c = Db.Conn();
                // Obtener tipo de empleado y período para cálculo inteligente de faltas
                var qi = c.CreateCommand();
                qi.CommandText = @"SELECT d.TipoEmpleado, d.IdEmpleado, p.FechaDesde, p.FechaHasta
                    FROM DetallesNomina d JOIN PeriodosNomina p ON p.Id=d.IdPeriodo
                    WHERE d.Id=$id";
                qi.Parameters.AddWithValue("$id", id);
                string tipoEmp = ""; int idEmp = 0;
                DateTime perDesde = DateTime.Today, perHasta = DateTime.Today;
                using (var ri = qi.ExecuteReader())
                {
                    if (ri.Read()) {
                        tipoEmp = Db.S(ri,0); idEmp = Db.I(ri,1);
                        DateTime.TryParse(Db.S(ri,2), out perDesde);
                        DateTime.TryParse(Db.S(ri,3), out perHasta);
                    }
                }
                // Recalcular
                decimal tasa = req.TasaBCV > 0 ? req.TasaBCV : 1;
                decimal devSueldo = req.SueldoDiarioUSD * req.DiasTrabajados;
                decimal totalDev  = devSueldo + req.DevengadoPiezas + req.DevengadoVentas + req.OtrosIngresos;
                decimal ivss = Math.Round(totalDev * 0.04m, 2);
                decimal rpvh = Math.Round(totalDev * 0.01m, 2);
                // Descuento faltas: producción usa bono real del día, otros usan sueldo diario
                decimal descFaltas;
                if (tipoEmp == "Produccion" && idEmp > 0)
                    descFaltas = Db.CalcularDescuentoFaltas(idEmp, perDesde, perHasta, tasa);
                else
                    descFaltas = req.SueldoDiarioUSD * req.DiasInasistencia;

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
                decimal dedReales = descFaltas + req.CuotaPrestamo + req.OtrasDeduccion;
                decimal pagoReal = totalDev - dedReales;

                // #7 Auditoría
                var usuario = ctx.User?.FindFirst("nombre")?.Value ?? "API";
                AuthEndpoints.RegistrarAuditoria(c, "NOMINA_DETALLE_EDITADO", "DetallesNomina",
                    $"Id={id}, PagoReal={pagoReal:F2}, por {usuario}");

                return rows == 0 ? Results.NotFound() : Results.Ok(new { ok=true,
                    devengado=totalDev, deduccionesReales=dedReales,
                    pagoRealUSD=pagoReal, pagoRealBs=pagoReal*tasa });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // Cerrar período + #9 auto-calcular aportes patronales
            app.MapPut("/api/nomina/periodos/{id:int}/cerrar", (int id, HttpContext ctx) =>
            {
                using var c = Db.Conn();
                // Verificar que exista y esté abierto
                var chk = c.CreateCommand();
                chk.CommandText = "SELECT Estado,FechaDesde,FechaHasta,TasaBCV FROM PeriodosNomina WHERE Id=$id";
                chk.Parameters.AddWithValue("$id", id);
                using var rchk = chk.ExecuteReader();
                if (!rchk.Read()) return Results.NotFound();
                string estado = Db.S(rchk,0), desde = Db.S(rchk,1), hasta = Db.S(rchk,2);
                decimal tasa = Db.D(rchk,3);
                rchk.Close();
                if (estado == "Cerrado")
                    return Results.BadRequest(new { error = "El período ya está cerrado." });
                if (tasa <= 0) tasa = 1;

                // Cerrar
                var upd = c.CreateCommand();
                upd.CommandText = "UPDATE PeriodosNomina SET Estado='Cerrado' WHERE Id=$id";
                upd.Parameters.AddWithValue("$id", id);
                upd.ExecuteNonQuery();

                // #9 Auto-calcular aportes patronales con valores por defecto
                decimal pctIVSS = 9.0m, pctRPVH = 2.0m, pctINCES = 2.0m, valorUT = 10.0m;
                DateTime dtDesde = DateTime.Parse(desde), dtHasta = DateTime.Parse(hasta);
                int diasLab = 0;
                for (var d = dtDesde; d <= dtHasta; d = d.AddDays(1))
                    if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday) diasLab++;

                var qdet = c.CreateCommand();
                qdet.CommandText = @"SELECT IdEmpleado,DevengadoSueldo+DevengadoPiezas+DevengadoVentas+OtrosIngresos,TasaBCV
                    FROM DetallesNomina WHERE IdPeriodo=$id";
                qdet.Parameters.AddWithValue("$id", id);
                using var rdet = qdet.ExecuteReader();
                while (rdet.Read())
                {
                    int empId = Db.I(rdet,0);
                    decimal totalDev = Db.D(rdet,1);
                    decimal t = Db.D(rdet,2); if (t <= 0) t = tasa;
                    decimal salBs = totalDev * t;
                    decimal ivssP = Math.Round(salBs * pctIVSS / 100, 2);
                    decimal rpvhP = Math.Round(salBs * pctRPVH / 100, 2);
                    decimal incesP = Math.Round(salBs * pctINCES / 100, 2);
                    decimal cestaP = Math.Round(diasLab * 0.5m * valorUT, 2);

                    var uap = c.CreateCommand();
                    uap.CommandText = @"INSERT INTO AportesPatronales
                        (IdPeriodo,IdEmpleado,SalarioNormalBs,TasaBCV,PorcentajeIVSS,
                         IVSSPatronBs,RPVHPatronBs,INCESBs,CestaBs,DiasLaborales,ValorUTBs)
                        VALUES($ip,$ie,$sal,$ta,$pi,$iv,$rp,$in,$ce,$dl,$ut)
                        ON CONFLICT(IdPeriodo,IdEmpleado) DO UPDATE SET
                            SalarioNormalBs=excluded.SalarioNormalBs,TasaBCV=excluded.TasaBCV,
                            PorcentajeIVSS=excluded.PorcentajeIVSS,IVSSPatronBs=excluded.IVSSPatronBs,
                            RPVHPatronBs=excluded.RPVHPatronBs,INCESBs=excluded.INCESBs,
                            CestaBs=excluded.CestaBs,DiasLaborales=excluded.DiasLaborales,ValorUTBs=excluded.ValorUTBs";
                    uap.Parameters.AddWithValue("$ip", id);
                    uap.Parameters.AddWithValue("$ie", empId);
                    uap.Parameters.AddWithValue("$sal", (double)salBs);
                    uap.Parameters.AddWithValue("$ta", (double)t);
                    uap.Parameters.AddWithValue("$pi", (double)pctIVSS);
                    uap.Parameters.AddWithValue("$iv", (double)ivssP);
                    uap.Parameters.AddWithValue("$rp", (double)rpvhP);
                    uap.Parameters.AddWithValue("$in", (double)incesP);
                    uap.Parameters.AddWithValue("$ce", (double)cestaP);
                    uap.Parameters.AddWithValue("$dl", diasLab);
                    uap.Parameters.AddWithValue("$ut", (double)valorUT);
                    try { uap.ExecuteNonQuery(); } catch {}
                }
                rdet.Close();

                // #7 Auditoría
                var usuario = ctx.User?.FindFirst("nombre")?.Value ?? "API";
                AuthEndpoints.RegistrarAuditoria(c, "PERIODO_CERRADO_API", "PeriodosNomina",
                    $"Id={id}, por {usuario}");
                return Results.Ok(new { ok = true, aportesCalculados = true });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // Reabrir período (solo admin)
            app.MapPut("/api/nomina/periodos/{id:int}/reabrir", (int id, HttpContext ctx) =>
            {
                var rol = ctx.User?.FindFirst("rol")?.Value ?? "";
                if (rol != "Admin") return Results.Forbid();
                using var c = Db.Conn();
                // #12 Validar que esté cerrado
                var chk = c.CreateCommand();
                chk.CommandText = "SELECT Estado FROM PeriodosNomina WHERE Id=$id";
                chk.Parameters.AddWithValue("$id", id);
                var estadoActual = chk.ExecuteScalar()?.ToString() ?? "";
                if (estadoActual != "Cerrado")
                    return Results.BadRequest(new { error = "El período no está cerrado." });
                var upd = c.CreateCommand();
                upd.CommandText = "UPDATE PeriodosNomina SET Estado='Abierto' WHERE Id=$id";
                upd.Parameters.AddWithValue("$id", id);
                upd.ExecuteNonQuery();
                // #7 Auditoría
                var usuario = ctx.User?.FindFirst("nombre")?.Value ?? "Admin";
                AuthEndpoints.RegistrarAuditoria(c, "PERIODO_REABIERTO", "PeriodosNomina",
                    $"Id={id}, por {usuario}");
                return Results.Ok(new { ok = true });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // ── #4 Aportes patronales (consulta) ─────────────────────────
            app.MapGet("/api/nomina/periodos/{id:int}/aportes", (int id) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"SELECT a.IdEmpleado, e.Nombre||' '||e.Apellido AS Nombre,
                    a.SalarioNormalBs, a.TasaBCV, a.PorcentajeIVSS,
                    a.IVSSPatronBs, a.RPVHPatronBs, a.INCESBs, a.CestaBs, a.DiasLaborales, a.ValorUTBs,
                    a.IVSSPatronBs+a.RPVHPatronBs+a.INCESBs+a.CestaBs AS TotalBs
                    FROM AportesPatronales a
                    LEFT JOIN Empleados e ON e.Id=a.IdEmpleado
                    WHERE a.IdPeriodo=$id ORDER BY Nombre";
                cmd.Parameters.AddWithValue("$id", id);
                var lista = new List<object>();
                decimal sumTotal = 0;
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    decimal totalBs = Db.D(r, 11);
                    decimal tasaA = Db.D(r, 3); if (tasaA <= 0) tasaA = 1;
                    sumTotal += totalBs;
                    lista.Add(new {
                        idEmpleado = Db.I(r,0), nombre = Db.S(r,1),
                        salarioBs = Db.D(r,2), tasa = tasaA, pctIVSS = Db.D(r,4),
                        ivss = Db.D(r,5), rpvh = Db.D(r,6), inces = Db.D(r,7), cesta = Db.D(r,8),
                        diasLab = Db.I(r,9), valorUT = Db.D(r,10), totalBs, totalUSD = Math.Round(totalBs / tasaA, 2)
                    });
                }
                return Results.Ok(new { aportes = lista, totalAportesBs = sumTotal });
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
                    decimal dedReales=faltas+prestamo+otrasD;  // descuentos que SÍ aplican
                    decimal pagoReal=totalDev-dedReales;

                    bool hayDed = dedReales > 0;
                    recibos.Append($"""
                    <div class="recibo">
                      <div class="rec-header">
                        <div>
                          <strong>EL HATO PASCHER C.A.</strong><br>
                          <span class="dim">RIF: J-30571871-7</span><br>
                          <span class="dim" style="font-size:7.5pt">Ctra. Villa de Cura - San Francisco de Asís, Sector Los Colorados, Edo. Aragua</span>
                        </div>
                        <div style="text-align:right">
                          <strong>RECIBO DE PAGO</strong><br>
                          <span class="dim">Período: {H(desde)} al {H(hasta)}</span>
                        </div>
                      </div>
                      <div style="display:flex;justify-content:space-between;margin-bottom:3mm;padding:1.5mm 0;border-bottom:1px solid #ddd">
                        <div><strong>{H(nombre)}</strong></div>
                        <div class="dim">{H(tipo)}</div>
                      </div>
                      <table>
                        <tr class="sec"><td colspan="2"><strong>Devengado</strong></td><td class="r"><strong>USD</strong></td></tr>
                        <tr><td colspan="2">Sueldo ({dias:N0} días × {sdiario:N2})</td><td class="r">{devS:N2}</td></tr>
                        {(devP>0?$"<tr><td colspan='2'>Bono producción ({piezas:N0} pzas)</td><td class='r'>{devP:N2}</td></tr>":"")}
                        {(devV>0?$"<tr><td colspan='2'>Bono ventas</td><td class='r'>{devV:N2}</td></tr>":"")}
                        {(otros>0?$"<tr><td colspan='2'>Otros ingresos</td><td class='r'>{otros:N2}</td></tr>":"")}
                        {(hayDed?$"<tr class='subtotal'><td colspan='2'>Total devengado</td><td class='r'>{totalDev:N2}</td></tr>":"")}
                        {(faltas>0?$"<tr><td colspan='2'>Desc. inasistencias ({inas:N0} días)</td><td class='r'>-{faltas:N2}</td></tr>":"")}
                        {(prestamo>0?$"<tr><td colspan='2'>Cuota préstamo</td><td class='r'>-{prestamo:N2}</td></tr>":"")}
                        {(otrasD>0?$"<tr><td colspan='2'>Otras deducciones</td><td class='r'>-{otrasD:N2}</td></tr>":"")}
                        <tr class="neto"><td colspan="2">TOTAL A PAGAR (USD)</td><td class="r">{pagoReal:N2}</td></tr>
                        <tr class="neto-bs"><td colspan="2">TOTAL EN BOLÍVARES (× {tasaR:N2})</td><td class="r">{pagoReal*tasaR:N2}</td></tr>
                      </table>
                      <div class="firma">
                        <div class="firma-box">Firma del empleado</div>
                        <div class="firma-box">Firma empleador</div>
                      </div>
                    </div>
                    """);
                }

                string html = $$"""
<!DOCTYPE html><html lang="es"><head>
<meta charset="UTF-8"><title>Recibos de Pago — {{H(desde)}} al {{H(hasta)}}</title>
<style>
*{box-sizing:border-box;margin:0;padding:0;font-family:'Segoe UI',Arial,sans-serif;font-size:10pt}
body{padding:10mm;color:#111}
.recibo{border:1px solid #ccc;border-radius:4px;padding:6mm;margin-bottom:8mm;page-break-inside:avoid}
.rec-header{display:flex;justify-content:space-between;align-items:flex-start;
    border-bottom:2px solid #1A4A8A;padding-bottom:3mm;margin-bottom:4mm}
.dim{color:#555;font-size:9pt}
table{width:100%;border-collapse:collapse;font-size:9.5pt}
tr td{padding:1.5mm 2mm}
.sec td{background:#1A4A8A;color:#fff;padding:2mm}
.subtotal td{border-top:1px solid #ccc;font-weight:600}
.neto td{background:#e8f4e8;font-weight:700;font-size:11pt;padding:2mm}
.neto-bs td{background:#f0f0f0;font-weight:600;font-size:10pt;padding:2mm}
.r{text-align:right}
.firma{display:flex;gap:10mm;margin-top:6mm}
.firma-box{flex:1;border-top:1px solid #999;padding-top:2mm;font-size:9pt;color:#555;text-align:center}
@media print{
  body{padding:5mm}
  .no-print{display:none!important}
  .recibo{margin-bottom:4mm}
}
</style></head><body>
<button class="no-print" onclick="window.print()"
  style="margin-bottom:8mm;padding:2mm 8mm;background:#1A4A8A;color:#fff;border:none;
         border-radius:4px;cursor:pointer;font-size:11pt">🖨 Imprimir todos los recibos</button>
<div class="no-print" style="margin-bottom:6mm;color:#555">
  Período: {{H(desde)}} al {{H(hasta)}} — Tasa BCV: {{tasa:N2}} Bs/USD
</div>
{{recibos}}
</body></html>
""";
                ctx.Response.ContentType = "text/html; charset=utf-8";
                await ctx.Response.WriteAsync(html);
            }).RequireAuthorization();

            // ════════════════════════════════════════════════════════════════
            //  BENEFICIOS LOTTT
            // ════════════════════════════════════════════════════════════════

            // Obtener beneficios (filtrable por empleado y año)
            app.MapGet("/api/nomina/beneficios", (int? idEmpleado, int? anio) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                var where = new List<string>();
                if (idEmpleado.HasValue) where.Add("IdEmpleado=$ie");
                if (anio.HasValue)       where.Add("Anio=$an");
                cmd.CommandText = "SELECT * FROM BeneficiosAnuales"
                    + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
                    + " ORDER BY Anio DESC, TipoBeneficio";
                if (idEmpleado.HasValue) cmd.Parameters.AddWithValue("$ie", idEmpleado.Value);
                if (anio.HasValue)       cmd.Parameters.AddWithValue("$an", anio.Value);
                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    lista.Add(new {
                        id = Db.I(r,0), idEmpleado = Db.I(r,1), nombre = Db.S(r,2),
                        anio = Db.I(r,3), tipoBeneficio = Db.S(r,4),
                        diasCorresponden = Db.D(r,5), salarioDiarioUSD = Db.D(r,6),
                        montoLegalUSD = Db.D(r,7), montoGerenciaUSD = Db.D(r,8),
                        tasaBCV = Db.D(r,9),
                        fechaPago = Db.S(r,10), estado = Db.S(r,11), observaciones = Db.S(r,12)
                    });
                return Results.Ok(lista);
            }).RequireAuthorization();

            // Calcular beneficios LOTTT para un empleado y año
            app.MapPost("/api/nomina/beneficios/calcular", (HttpContext ctx) =>
            {
                var body = ctx.Request.ReadFromJsonAsync<BeneficioCalcRequest>().Result;
                if (body == null) return Results.BadRequest(new { error = "Cuerpo requerido." });

                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT * FROM Empleados WHERE Id=$id";
                cmd.Parameters.AddWithValue("$id", body.IdEmpleado);
                using var r = cmd.ExecuteReader();
                if (!r.Read()) return Results.NotFound(new { error = "Empleado no encontrado." });

                var fechaIng = DateTime.TryParse(Db.S(r,6), out var fi) ? fi : DateTime.Today;
                int aniosServ = Math.Max(0, (int)((DateTime.Today - fechaIng).TotalDays / 365.25));
                decimal sal = body.SalarioDiarioUSD > 0 ? body.SalarioDiarioUSD : Db.D(r,13); // SalarioNormalUSD
                string nombre = Db.S(r,2) + " " + Db.S(r,3);
                int anio = body.Anio;
                decimal tasa = body.TasaBCV > 0 ? body.TasaBCV : 1;

                // Utilidades: 15 días (art. 131)
                decimal diasUtil = 15;
                // Vacaciones: 15 + 1 por año (art. 190, max +15)
                decimal diasVac = 15 + Math.Min(aniosServ, 15);
                // Bono vacacional: 7 + 1 por año (art. 192, max +21)
                decimal diasBono = 7 + Math.Min(aniosServ, 21);
                // Prestaciones: 60 días/año (15 x 4 trimestres, art. 142)
                decimal diasPrest = 60;

                var result = new[] {
                    new { tipo = "Utilidades", dias = diasUtil, montoLegalUSD = Math.Round(sal * diasUtil, 2),
                          obs = $"Art. 131 LOTTT — {diasUtil} días" },
                    new { tipo = "Vacaciones", dias = diasVac, montoLegalUSD = Math.Round(sal * diasVac, 2),
                          obs = $"Art. 190 LOTTT — {diasVac} días ({aniosServ} años servicio)" },
                    new { tipo = "Bono Vacacional", dias = diasBono, montoLegalUSD = Math.Round(sal * diasBono, 2),
                          obs = $"Art. 192 LOTTT — {diasBono} días" },
                    new { tipo = "Prestaciones Sociales", dias = diasPrest, montoLegalUSD = Math.Round(sal * diasPrest, 2),
                          obs = $"Art. 142 LOTTT — garantía 15 días × 4 trimestres" }
                };

                return Results.Ok(new {
                    empleado = nombre, anio, aniosServicio = aniosServ,
                    salarioDiarioUSD = sal, tasaBCV = tasa, beneficios = result
                });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // Registrar beneficio (con monto legal y monto gerencia)
            app.MapPost("/api/nomina/beneficios", (HttpContext ctx) =>
            {
                var b = ctx.Request.ReadFromJsonAsync<BeneficioRegistroRequest>().Result;
                if (b == null) return Results.BadRequest(new { error = "Cuerpo requerido." });

                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"INSERT INTO BeneficiosAnuales
                    (IdEmpleado,NombreEmpleado,Anio,TipoBeneficio,DiasCorresponden,
                     SalarioDiarioUSD,MontoUSD,MontoGerenciaUSD,TasaBCV,FechaPago,Estado,Observaciones)
                    VALUES($ie,$ne,$an,$tb,$dc,$sd,$mo,$mg,$tc,$fp,'Pendiente',$ob)";
                cmd.Parameters.AddWithValue("$ie", b.IdEmpleado);
                cmd.Parameters.AddWithValue("$ne", b.NombreEmpleado ?? "");
                cmd.Parameters.AddWithValue("$an", b.Anio);
                cmd.Parameters.AddWithValue("$tb", b.TipoBeneficio);
                cmd.Parameters.AddWithValue("$dc", (double)b.DiasCorresponden);
                cmd.Parameters.AddWithValue("$sd", (double)b.SalarioDiarioUSD);
                cmd.Parameters.AddWithValue("$mo", (double)b.MontoLegalUSD);
                cmd.Parameters.AddWithValue("$mg", (double)b.MontoGerenciaUSD);
                cmd.Parameters.AddWithValue("$tc", (double)b.TasaBCV);
                cmd.Parameters.AddWithValue("$fp", b.FechaPago ?? DateTime.Today.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$ob", b.Observaciones ?? "");
                cmd.ExecuteNonQuery();

                AuthEndpoints.RegistrarAuditoria(ctx, "BENEFICIO_REGISTRADO",
                    $"{b.NombreEmpleado} — {b.TipoBeneficio} {b.Anio}");

                return Results.Ok(new { ok = true });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // Actualizar monto gerencia de un beneficio existente
            app.MapPut("/api/nomina/beneficios/{id}", (int id, HttpContext ctx) =>
            {
                var body = ctx.Request.ReadFromJsonAsync<BeneficioUpdateRequest>().Result;
                if (body == null) return Results.BadRequest(new { error = "Cuerpo requerido." });

                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"UPDATE BeneficiosAnuales
                    SET MontoGerenciaUSD=$mg, Observaciones=$ob WHERE Id=$id";
                cmd.Parameters.AddWithValue("$mg", (double)body.MontoGerenciaUSD);
                cmd.Parameters.AddWithValue("$ob", body.Observaciones ?? "");
                cmd.Parameters.AddWithValue("$id", id);
                int rows = cmd.ExecuteNonQuery();
                if (rows == 0) return Results.NotFound(new { error = "Beneficio no encontrado." });

                AuthEndpoints.RegistrarAuditoria(ctx, "BENEFICIO_ACTUALIZADO",
                    $"Id={id}, MontoGerencia={body.MontoGerenciaUSD}");

                return Results.Ok(new { ok = true });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // Marcar beneficio como pagado
            app.MapPost("/api/nomina/beneficios/{id}/pagar", (int id) =>
            {
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"UPDATE BeneficiosAnuales
                    SET Estado='Pagado', FechaPago=$fp WHERE Id=$id";
                cmd.Parameters.AddWithValue("$fp", DateTime.Today.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$id", id);
                int rows = cmd.ExecuteNonQuery();
                if (rows == 0) return Results.NotFound(new { error = "Beneficio no encontrado." });
                return Results.Ok(new { ok = true });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // ════════════════════════════════════════════════════════════════
            //  INASISTENCIAS
            // ════════════════════════════════════════════════════════════════

            // Listar inasistencias (filtrable por empleado y rango de fechas)
            app.MapGet("/api/nomina/inasistencias", (int? empleado, string? desde, string? hasta) =>
            {
                DateTime? d = null, h = null;
                if (!string.IsNullOrEmpty(desde) && DateTime.TryParse(desde, out var dd)) d = dd;
                if (!string.IsNullOrEmpty(hasta) && DateTime.TryParse(hasta, out var hh)) h = hh;
                var list = Db.ObtenerInasistencias(empleado, d, h);
                return Results.Ok(list.Select(i => new {
                    i.Id, i.IdEmpleado, i.NombreEmpleado, i.TipoEmpleado,
                    fecha = i.Fecha.ToString("yyyy-MM-dd"),
                    i.Justificada, i.MontoDescuentoBs, i.Observaciones
                }));
            }).RequireAuthorization();

            // Registrar inasistencia
            app.MapPost("/api/nomina/inasistencias", (InasistenciaRequest req, HttpContext ctx) =>
            {
                if (req.IdEmpleado <= 0)
                    return Results.BadRequest(new { error = "IdEmpleado requerido." });
                if (!DateTime.TryParse(req.Fecha, out var fecha))
                    return Results.BadRequest(new { error = "Fecha inválida." });

                int id = Db.RegistrarInasistencia(
                    req.IdEmpleado, fecha, req.Justificada,
                    req.Observaciones ?? "", req.TasaBCV > 0 ? req.TasaBCV : null);

                if (id == -2) return Results.BadRequest(new { error = "La fecha no pertenece a ningún período de nómina registrado." });
                if (id < 0) return Results.NotFound(new { error = "Empleado no encontrado." });

                AuthEndpoints.RegistrarAuditoria(ctx, "INASISTENCIA_REGISTRADA",
                    $"Emp={req.IdEmpleado}, Fecha={req.Fecha}, Just={req.Justificada}");

                // Devolver el registro completo
                var list = Db.ObtenerInasistencias(req.IdEmpleado, fecha, fecha);
                var inas = list.FirstOrDefault();
                return Results.Ok(new {
                    ok = true, id,
                    montoDescuentoBs = inas?.MontoDescuentoBs ?? 0
                });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // Actualizar inasistencia (justificar/injustificar)
            app.MapPut("/api/nomina/inasistencias/{id}", (int id, InasistenciaUpdateRequest req, HttpContext ctx) =>
            {
                bool ok = Db.ActualizarInasistencia(id, req.Justificada, req.Observaciones ?? "");
                if (!ok) return Results.NotFound(new { error = "Inasistencia no encontrada." });

                AuthEndpoints.RegistrarAuditoria(ctx, "INASISTENCIA_ACTUALIZADA",
                    $"Id={id}, Just={req.Justificada}");

                return Results.Ok(new { ok = true });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();

            // Eliminar inasistencia
            app.MapDelete("/api/nomina/inasistencias/{id}", (int id, HttpContext ctx) =>
            {
                bool ok = Db.EliminarInasistencia(id);
                if (!ok) return Results.NotFound(new { error = "Inasistencia no encontrada." });

                AuthEndpoints.RegistrarAuditoria(ctx, "INASISTENCIA_ELIMINADA", $"Id={id}");
                return Results.Ok(new { ok = true });
            }).AddEndpointFilter(RequireRole.Of(TokenService.PuedeEscribirNomina)).RequireAuthorization();
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
            decimal dedReales=faltas+prestamo+otrasD;  // descuentos reales (sin IVSS/RPVH)
            decimal pagoReal=totalDev-dedReales;
            return new {
                id=Db.I(r,0), idEmpleado=Db.I(r,1), nombre=Db.S(r,2), tipo=Db.S(r,3),
                diasTrabajados=Db.D(r,4), diasInasistencia=Db.D(r,5),
                piezasProducidas=Db.D(r,6), ventasCerradas=Db.D(r,7),
                sueldoDiario=Db.D(r,8), devSueldo=devS, devPiezas=devP, devVentas=devV,
                otrosIngresos=otros, totalDevengado=totalDev,
                faltas, prestamo, otrasDeduccion=otrasD,
                pagoRealUSD=pagoReal, tasa,
                pagoRealBs=pagoReal*tasa,
                obs=Db.S(r,19)
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

    public record BeneficioCalcRequest(int IdEmpleado, int Anio, decimal TasaBCV, decimal SalarioDiarioUSD = 0);

    public record BeneficioRegistroRequest(
        int IdEmpleado, string? NombreEmpleado, int Anio, string TipoBeneficio,
        decimal DiasCorresponden, decimal SalarioDiarioUSD,
        decimal MontoLegalUSD, decimal MontoGerenciaUSD,
        decimal TasaBCV, string? FechaPago, string? Observaciones);

    public record BeneficioUpdateRequest(decimal MontoGerenciaUSD, string? Observaciones);

    public record InasistenciaRequest(
        int IdEmpleado, string Fecha, bool Justificada,
        string? Observaciones, decimal TasaBCV = 0);

    public record InasistenciaUpdateRequest(bool Justificada, string? Observaciones);
}
