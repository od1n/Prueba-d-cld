// AbonosEndpoints.cs — API REST para abonos, CxC, cuentas receptoras y estado de cuenta
// GET  /api/abonos?cliente=&desde=&hasta=
// POST /api/abonos  (multipart: campos + imagen comprobante; aplica a CxC/Factura)
// PUT  /api/abonos/{id}/estado  { estado: "Confirmado"|"Pendiente"|"Rechazado" }
// GET  /api/abonos/cuentas
// GET  /api/abonos/estado-cuenta/{codigoCliente}
// GET  /api/cxc?cliente=&estado=&soloPendientes=
// POST /api/cxc/{id}/pago  (JSON o multipart; actualiza CxC + Factura + Abonos)
// POST /api/cxc/sincronizar  (Admin: reconcilia Facturas <-> CuentasCobrar)
using Microsoft.Data.Sqlite;
using SistemaGestion.Api.Auth;
using System.Globalization;

namespace SistemaGestion.Api.Endpoints
{
    public static class AbonosEndpoints
    {
        internal const decimal TOLERANCIA = 0.005m;

        internal static bool EsEstadoCerrado(string? estado) =>
            estado != null && (
                estado.Equals("Saldada", StringComparison.OrdinalIgnoreCase) ||
                estado.Equals("Pagada",  StringComparison.OrdinalIgnoreCase) ||
                estado.Equals("Pagado",  StringComparison.OrdinalIgnoreCase) ||
                estado.Equals("Anulada", StringComparison.OrdinalIgnoreCase));

        internal static string? ExtraerNumeroFactura(string? concepto)
        {
            if (string.IsNullOrWhiteSpace(concepto)) return null;
            var t = concepto.Trim();
            if (t.StartsWith("Factura ", StringComparison.OrdinalIgnoreCase))
            {
                var num = t.Substring("Factura ".Length).Trim();
                return string.IsNullOrWhiteSpace(num) ? null : num;
            }
            return null;
        }

        internal static bool ExisteFacturaTx(SqliteConnection c, SqliteTransaction tx, string? numeroF)
        {
            if (string.IsNullOrWhiteSpace(numeroF)) return false;
            var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT 1 FROM Facturas WHERE NumeroF = $n LIMIT 1";
            cmd.Parameters.AddWithValue("$n", numeroF.Trim());
            return cmd.ExecuteScalar() != null;
        }

        internal static string? ObtenerNombreClienteTx(SqliteConnection c, SqliteTransaction? tx, string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT Descripcion FROM Clientes WHERE Codigo = $cod LIMIT 1";
            cmd.Parameters.AddWithValue("$cod", codigo.Trim());
            return cmd.ExecuteScalar()?.ToString();
        }

        internal static string ExtraerCodigoCliente(string? clienteCxC)
        {
            if (string.IsNullOrWhiteSpace(clienteCxC)) return "";
            var s = clienteCxC.Trim();
            int idx = s.IndexOf(" - ", StringComparison.Ordinal);
            return idx > 0 ? s.Substring(0, idx).Trim() : s;
        }

        internal static async Task<(string ruta, string? error)> GuardarComprobanteArchivoAsync(
            IFormFile? archivo, string subcarpeta, string prefijo)
        {
            if (archivo == null || archivo.Length == 0) return ("", null);
            const long MAX_BYTES = 15 * 1024 * 1024;
            if (archivo.Length > MAX_BYTES)
                return ("", "Imagen demasiado grande (máx 15 MB).");

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var carpeta = Path.Combine(appData, "SistemaGestion", "comprobantes", subcarpeta,
                DateTime.Today.ToString("yyyy-MM"));
            Directory.CreateDirectory(carpeta);
            string ext = Path.GetExtension(archivo.FileName);
            string destino = Path.Combine(carpeta,
                $"{prefijo}_{DateTime.Now:yyyyMMdd_HHmmss_fff}{ext}");
            using (var fs = new FileStream(destino, FileMode.Create))
                await archivo.CopyToAsync(fs);
            return (Path.GetFullPath(destino), null);
        }

        internal record ResultadoPagoFactura(
            bool Encontrada,
            string? Error,
            int IdAbono,
            string NumeroF,
            decimal MontoAplicado,
            decimal AbonoTotal,
            decimal SaldoRestante,
            bool Pagada,
            bool CxcActualizada,
            int? IdCxC);

        internal record ResultadoPagoCxC(
            bool Encontrada,
            string? Error,
            int IdAbono,
            int IdCxC,
            string Concepto,
            string? NumeroF,
            decimal MontoAplicado,
            decimal PagadoTotal,
            decimal SaldoRestante,
            string EstadoCxC,
            bool FacturaActualizada);

        internal static int InsertarRegistroAbonoTx(
            SqliteConnection c, SqliteTransaction tx,
            string? numeroF, string fecha, decimal monto, string cliente,
            string metodo, string referencia, string remitente,
            string rutaComprobante, string observaciones, int idCuenta, string estado)
        {
            object nfVal = ExisteFacturaTx(c, tx, numeroF) ? numeroF!.Trim() : DBNull.Value;
            var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO Abonos
                    (NumeroF, Fecha, Monto, Cliente, MetodoPago, Referencia,
                     NombreRemitente, RutaComprobante, Observaciones,
                     IdCuentaReceptora, Estado)
                VALUES
                    ($nf, $fe, $mo, $cl, $mp, $ref,
                     $nr, $rc, $obs, $icr, $est);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$nf",  nfVal);
            cmd.Parameters.AddWithValue("$fe",  string.IsNullOrWhiteSpace(fecha) ? DateTime.Today.ToString("yyyy-MM-dd") : fecha);
            cmd.Parameters.AddWithValue("$mo",  (double)monto);
            cmd.Parameters.AddWithValue("$cl",  cliente ?? "");
            cmd.Parameters.AddWithValue("$mp",  string.IsNullOrWhiteSpace(metodo) ? "Efectivo" : metodo);
            cmd.Parameters.AddWithValue("$ref", referencia ?? "");
            cmd.Parameters.AddWithValue("$nr",  remitente ?? "");
            cmd.Parameters.AddWithValue("$rc",  rutaComprobante ?? "");
            cmd.Parameters.AddWithValue("$obs", observaciones ?? "");
            cmd.Parameters.AddWithValue("$icr", idCuenta);
            cmd.Parameters.AddWithValue("$est", string.IsNullOrWhiteSpace(estado) ? "Confirmado" : estado);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Aplica un pago parcial o total a una Factura y sincroniza su CuentaCobrar
        /// asociada + inserta el registro en Abonos (todo en la misma transacción).
        /// </summary>
        internal static ResultadoPagoFactura AplicarPagoAFacturaTx(
            SqliteConnection c, SqliteTransaction tx,
            string numeroF, decimal monto, string fecha,
            string metodo, string referencia, string remitente,
            string rutaComprobante, string observaciones, int idCuenta,
            bool insertarAbono = true)
        {
            if (monto <= 0)
                return new ResultadoPagoFactura(true, "Monto inválido.", 0, numeroF, 0, 0, 0, false, false, null);

            var q = c.CreateCommand();
            q.Transaction = tx;
            q.CommandText = "SELECT TotalF, COALESCE(Abono,0), COALESCE(Codigo,''), COALESCE(Anulada,0) FROM Facturas WHERE NumeroF = $n";
            q.Parameters.AddWithValue("$n", numeroF);
            decimal total, abonoAnt; string codCli; int anulada;
            using (var r = q.ExecuteReader())
            {
                if (!r.Read())
                    return new ResultadoPagoFactura(false, "Factura no encontrada.", 0, numeroF, 0, 0, 0, false, false, null);
                total    = Db.D(r, 0);
                abonoAnt = Db.D(r, 1);
                codCli   = Db.S(r, 2);
                anulada  = Db.I(r, 3);
            }

            if (anulada == 1 || abonoAnt < 0)
                return new ResultadoPagoFactura(true, "No se puede abonar una factura anulada.", 0, numeroF, 0, abonoAnt, 0, false, false, null);

            decimal saldoAnt = Math.Max(0, total - abonoAnt);
            if (saldoAnt <= TOLERANCIA)
                return new ResultadoPagoFactura(true, "Esta factura ya está pagada en su totalidad.", 0, numeroF, 0, total, 0, true, false, null);

            decimal aplicado = Math.Min(saldoAnt, monto);
            decimal nuevoAbono = abonoAnt + aplicado;
            bool pagada = (total - nuevoAbono) <= TOLERANCIA;
            if (pagada) nuevoAbono = total;

            var updF = c.CreateCommand();
            updF.Transaction = tx;
            updF.CommandText = "UPDATE Facturas SET Abono = $a WHERE NumeroF = $n";
            updF.Parameters.AddWithValue("$a", (double)nuevoAbono);
            updF.Parameters.AddWithValue("$n", numeroF);
            updF.ExecuteNonQuery();

            // Sincronizar la CuentaCobrar vinculada ("Factura <numeroF>")
            bool cxcActualizada = false;
            int? idCxC = null;
            var qCxC = c.CreateCommand();
            qCxC.Transaction = tx;
            qCxC.CommandText = @"
                SELECT Id, Monto, COALESCE(Pagado,0), COALESCE(Estado,'Vigente'), COALESCE(FechaVence,'')
                FROM CuentasCobrar
                WHERE LOWER(TRIM(Concepto)) = LOWER($conc)
                ORDER BY Id DESC LIMIT 1";
            qCxC.Parameters.AddWithValue("$conc", $"Factura {numeroF.Trim()}");
            using (var rc = qCxC.ExecuteReader())
            {
                if (rc.Read())
                {
                    int cid = Db.I(rc, 0);
                    decimal cMonto  = Db.D(rc, 1);
                    decimal cPagado = Db.D(rc, 2);
                    string cEstado  = Db.S(rc, 3);
                    string cVence   = Db.S(rc, 4);
                    rc.Close();

                    idCxC = cid;
                    if (!cEstado.Equals("Anulada", StringComparison.OrdinalIgnoreCase))
                    {
                        decimal nuevoPagado = Math.Min(cMonto, cPagado + aplicado);
                        string nuevoEstado;
                        if (pagada || (cMonto - nuevoPagado) <= TOLERANCIA)
                        {
                            nuevoPagado = cMonto;
                            nuevoEstado = "Saldada";
                        }
                        else
                        {
                            string hoy = DateTime.Today.ToString("yyyy-MM-dd");
                            nuevoEstado = (!string.IsNullOrEmpty(cVence) && string.CompareOrdinal(cVence[..Math.Min(10, cVence.Length)], hoy) < 0)
                                ? "Vencida" : "Vigente";
                        }

                        var updC = c.CreateCommand();
                        updC.Transaction = tx;
                        updC.CommandText = "UPDATE CuentasCobrar SET Pagado = $p, Estado = $e WHERE Id = $id";
                        updC.Parameters.AddWithValue("$p", (double)nuevoPagado);
                        updC.Parameters.AddWithValue("$e", nuevoEstado);
                        updC.Parameters.AddWithValue("$id", cid);
                        updC.ExecuteNonQuery();
                        cxcActualizada = true;
                    }
                }
            }

            int idAbono = 0;
            if (insertarAbono)
            {
                string obsFinal = string.IsNullOrWhiteSpace(observaciones)
                    ? $"Abono a factura {numeroF}" : observaciones;
                idAbono = InsertarRegistroAbonoTx(
                    c, tx, numeroF, fecha, aplicado, codCli,
                    metodo, referencia, remitente, rutaComprobante, obsFinal, idCuenta, "Confirmado");
            }

            return new ResultadoPagoFactura(
                true, null, idAbono, numeroF, aplicado, nuevoAbono,
                Math.Max(0, total - nuevoAbono), pagada, cxcActualizada, idCxC);
        }

        /// <summary>
        /// Aplica un pago parcial o total directamente sobre una fila de CuentasCobrar,
        /// sincroniza la Factura vinculada (si existe) e inserta en Abonos.
        /// </summary>
        internal static ResultadoPagoCxC AplicarPagoACxCTx(
            SqliteConnection c, SqliteTransaction tx,
            int idCxC, decimal monto, string fecha,
            string metodo, string referencia, string remitente,
            string rutaComprobante, string observaciones, int idCuenta,
            bool insertarAbono = true)
        {
            if (monto <= 0)
                return new ResultadoPagoCxC(true, "Monto inválido.", 0, idCxC, "", null, 0, 0, 0, "", false);

            var q = c.CreateCommand();
            q.Transaction = tx;
            q.CommandText = @"
                SELECT Cliente, Concepto, Monto, COALESCE(Pagado,0), COALESCE(Estado,'Vigente'), COALESCE(FechaVence,'')
                FROM CuentasCobrar WHERE Id = $id";
            q.Parameters.AddWithValue("$id", idCxC);

            string cliCxC, concepto, estadoAnt, fechaVence;
            decimal cMonto, cPagadoAnt;
            using (var r = q.ExecuteReader())
            {
                if (!r.Read())
                    return new ResultadoPagoCxC(false, "Cuenta por cobrar no encontrada.", 0, idCxC, "", null, 0, 0, 0, "", false);
                cliCxC     = Db.S(r, 0);
                concepto   = Db.S(r, 1);
                cMonto     = Db.D(r, 2);
                cPagadoAnt = Db.D(r, 3);
                estadoAnt  = Db.S(r, 4);
                fechaVence = Db.S(r, 5);
            }

            if (EsEstadoCerrado(estadoAnt) || (cMonto - cPagadoAnt) <= TOLERANCIA)
                return new ResultadoPagoCxC(true, "Esta cuenta por cobrar ya está saldada o anulada.", 0, idCxC, concepto, ExtraerNumeroFactura(concepto), 0, cPagadoAnt, 0, estadoAnt, false);

            decimal saldoAnt = Math.Max(0, cMonto - cPagadoAnt);
            decimal aplicado = Math.Min(saldoAnt, monto);
            decimal nuevoPagado = cPagadoAnt + aplicado;
            bool saldada = (cMonto - nuevoPagado) <= TOLERANCIA;
            string nuevoEstado;
            if (saldada)
            {
                nuevoPagado = cMonto;
                nuevoEstado = "Saldada";
            }
            else
            {
                string hoy = DateTime.Today.ToString("yyyy-MM-dd");
                nuevoEstado = (!string.IsNullOrEmpty(fechaVence) && string.CompareOrdinal(fechaVence[..Math.Min(10, fechaVence.Length)], hoy) < 0)
                    ? "Vencida" : "Vigente";
            }

            var updC = c.CreateCommand();
            updC.Transaction = tx;
            updC.CommandText = "UPDATE CuentasCobrar SET Pagado = $p, Estado = $e WHERE Id = $id";
            updC.Parameters.AddWithValue("$p", (double)nuevoPagado);
            updC.Parameters.AddWithValue("$e", nuevoEstado);
            updC.Parameters.AddWithValue("$id", idCxC);
            updC.ExecuteNonQuery();

            // Sincronizar la Factura vinculada si el concepto es "Factura <NUM>"
            string? numF = ExtraerNumeroFactura(concepto);
            bool facturaActualizada = false;
            string codCliente = ExtraerCodigoCliente(cliCxC);

            if (!string.IsNullOrWhiteSpace(numF))
            {
                var qF = c.CreateCommand();
                qF.Transaction = tx;
                qF.CommandText = "SELECT TotalF, COALESCE(Abono,0), COALESCE(Codigo,''), COALESCE(Anulada,0) FROM Facturas WHERE NumeroF = $n";
                qF.Parameters.AddWithValue("$n", numF);
                using var rf = qF.ExecuteReader();
                if (rf.Read())
                {
                    decimal fTotal = Db.D(rf, 0);
                    decimal fAbono = Db.D(rf, 1);
                    string fCod    = Db.S(rf, 2);
                    int fAnul      = Db.I(rf, 3);
                    rf.Close();

                    if (!string.IsNullOrWhiteSpace(fCod)) codCliente = fCod;
                    if (fAnul == 0 && fAbono >= 0)
                    {
                        decimal nuevoAbonoF = saldada ? fTotal : Math.Min(fTotal, fAbono + aplicado);
                        var updF = c.CreateCommand();
                        updF.Transaction = tx;
                        updF.CommandText = "UPDATE Facturas SET Abono = $a WHERE NumeroF = $n";
                        updF.Parameters.AddWithValue("$a", (double)nuevoAbonoF);
                        updF.Parameters.AddWithValue("$n", numF);
                        updF.ExecuteNonQuery();
                        facturaActualizada = true;
                    }
                }
            }

            int idAbono = 0;
            if (insertarAbono)
            {
                string obsFinal = string.IsNullOrWhiteSpace(observaciones)
                    ? $"Abono a {concepto}" : observaciones;
                idAbono = InsertarRegistroAbonoTx(
                    c, tx, numF, fecha, aplicado, codCliente,
                    metodo, referencia, remitente, rutaComprobante, obsFinal, idCuenta, "Confirmado");
            }

            return new ResultadoPagoCxC(
                true, null, idAbono, idCxC, concepto, numF, aplicado, nuevoPagado,
                Math.Max(0, cMonto - nuevoPagado), nuevoEstado, facturaActualizada);
        }

        /// <summary>
        /// Aplica un monto en orden FIFO (vencimiento más antiguo primero) a las CxC pendientes
        /// de un cliente, actualizando CuentasCobrar y Facturas vinculadas.
        /// Devuelve la lista de conceptos/montos aplicados y el saldo restante sin aplicar (si sobró).
        /// </summary>
        internal static (List<(int idCxC, string concepto, string? numeroF, decimal aplicado)> aplicadas, decimal sobrante)
            AplicarFifoAClienteTx(SqliteConnection c, SqliteTransaction tx, string codigoCliente, decimal monto)
        {
            var aplicadas = new List<(int idCxC, string concepto, string? numeroF, decimal aplicado)>();
            decimal restante = monto;
            if (restante <= TOLERANCIA || string.IsNullOrWhiteSpace(codigoCliente))
                return (aplicadas, restante);

            string? nombre = ObtenerNombreClienteTx(c, tx, codigoCliente);
            var q = c.CreateCommand();
            q.Transaction = tx;
            q.CommandText = @"
                SELECT Id, Concepto, Monto, COALESCE(Pagado,0), COALESCE(FechaVence,'')
                FROM CuentasCobrar
                WHERE (Cliente = $cod OR Cliente = $nom OR Cliente LIKE $pref)
                  AND COALESCE(Estado,'Vigente') NOT IN ('Saldada','Pagada','Pagado','Anulada')
                  AND (Monto - COALESCE(Pagado,0)) > 0.005
                ORDER BY FechaVence ASC, Id ASC";
            q.Parameters.AddWithValue("$cod",  codigoCliente.Trim());
            q.Parameters.AddWithValue("$nom",  nombre ?? codigoCliente.Trim());
            q.Parameters.AddWithValue("$pref", codigoCliente.Trim() + " - %");

            var pendientes = new List<(int id, string concepto, decimal saldo)>();
            using (var r = q.ExecuteReader())
            {
                while (r.Read())
                    pendientes.Add((Db.I(r, 0), Db.S(r, 1), Math.Max(0, Db.D(r, 2) - Db.D(r, 3))));
            }

            foreach (var p in pendientes)
            {
                if (restante <= TOLERANCIA) break;
                decimal aplicar = Math.Min(restante, p.saldo);
                var res = AplicarPagoACxCTx(c, tx, p.id, aplicar, "", "", "", "", "", "", 0, insertarAbono: false);
                if (res.Encontrada && res.Error == null && res.MontoAplicado > 0)
                {
                    aplicadas.Add((p.id, res.Concepto, res.NumeroF, res.MontoAplicado));
                    restante -= res.MontoAplicado;
                }
            }

            return (aplicadas, Math.Max(0, restante));
        }

        /// <summary>
        /// Reconciliación idempotente entre Facturas y CuentasCobrar:
        /// corrige desincronizaciones previas (facturas abonadas sin rebajar CxC,
        /// CxC pagadas en escritorio sin actualizar Facturas.Abono, facturas anuladas
        /// cuya CxC quedó abierta, y estados vencidos/saldados desactualizados).
        /// </summary>
        public static (int cxcActualizadas, int facturasActualizadas, int cxcAnuladas) SincronizarCxCYFacturas(SqliteConnection c)
        {
            int cxcUpd = 0, factUpd = 0, cxcAnul = 0;
            string hoy = DateTime.Today.ToString("yyyy-MM-dd");

            using var tx = c.BeginTransaction();
            try
            {
                // 1. Marcar CxC como Anulada si su factura está anulada o cancelada (Abono < 0)
                var qAnul = c.CreateCommand();
                qAnul.Transaction = tx;
                qAnul.CommandText = @"
                    SELECT cc.Id
                    FROM CuentasCobrar cc
                    JOIN Facturas f ON LOWER(TRIM(cc.Concepto)) = LOWER('Factura ' || TRIM(f.NumeroF))
                    WHERE (COALESCE(f.Anulada,0) = 1 OR COALESCE(f.Abono,0) < 0)
                      AND COALESCE(cc.Estado,'') != 'Anulada'";
                var idsAnul = new List<int>();
                try
                {
                    using var ra = qAnul.ExecuteReader();
                    while (ra.Read()) idsAnul.Add(Db.I(ra, 0));
                }
                catch { /* BD de prueba mínima sin columna Anulada/Abono */ }

                foreach (var id in idsAnul)
                {
                    var u = c.CreateCommand(); u.Transaction = tx;
                    u.CommandText = "UPDATE CuentasCobrar SET Estado = 'Anulada' WHERE Id = $id";
                    u.Parameters.AddWithValue("$id", id);
                    cxcAnul += u.ExecuteNonQuery();
                }

                // 2. Sincronizar pares (Facturas, CuentasCobrar) activos usando el saldo mínimo real
                var qPares = c.CreateCommand();
                qPares.Transaction = tx;
                qPares.CommandText = @"
                    SELECT cc.Id, cc.Monto, COALESCE(cc.Pagado,0), COALESCE(cc.Estado,'Vigente'), COALESCE(cc.FechaVence,''),
                           f.NumeroF, COALESCE(f.TotalF,0), COALESCE(f.Abono,0)
                    FROM CuentasCobrar cc
                    JOIN Facturas f ON LOWER(TRIM(cc.Concepto)) = LOWER('Factura ' || TRIM(f.NumeroF))
                    WHERE COALESCE(f.Anulada,0) = 0 AND COALESCE(f.Abono,0) >= 0
                      AND COALESCE(cc.Estado,'') != 'Anulada'";
                var pares = new List<(int id, decimal cMonto, decimal cPag, string cEst, string cVence, string numF, decimal fTot, decimal fAb)>();
                try
                {
                    using var rp = qPares.ExecuteReader();
                    while (rp.Read())
                        pares.Add((Db.I(rp, 0), Db.D(rp, 1), Db.D(rp, 2), Db.S(rp, 3), Db.S(rp, 4),
                                   Db.S(rp, 5), Db.D(rp, 6), Db.D(rp, 7)));
                }
                catch { /* BD mínima de tests */ }

                foreach (var p in pares)
                {
                    decimal saldoF = Math.Max(0, p.fTot - p.fAb);
                    decimal saldoC = Math.Max(0, p.cMonto - p.cPag);

                    if (saldoF < saldoC - TOLERANCIA)
                    {
                        // La factura recibió abonos que no se reflejaron en CuentasCobrar
                        decimal nuevoPag = saldoF <= TOLERANCIA ? p.cMonto : Math.Max(0, p.cMonto - saldoF);
                        string nuevoEst = saldoF <= TOLERANCIA
                            ? "Saldada"
                            : (!string.IsNullOrEmpty(p.cVence) && string.CompareOrdinal(p.cVence[..Math.Min(10, p.cVence.Length)], hoy) < 0 ? "Vencida" : "Vigente");
                        var u = c.CreateCommand(); u.Transaction = tx;
                        u.CommandText = "UPDATE CuentasCobrar SET Pagado = $p, Estado = $e WHERE Id = $id";
                        u.Parameters.AddWithValue("$p", (double)nuevoPag);
                        u.Parameters.AddWithValue("$e", nuevoEst);
                        u.Parameters.AddWithValue("$id", p.id);
                        cxcUpd += u.ExecuteNonQuery();
                    }
                    else if (saldoC < saldoF - TOLERANCIA)
                    {
                        // La CxC recibió pagos (ej. desde escritorio) que no se reflejaron en Facturas.Abono
                        decimal nuevoAb = saldoC <= TOLERANCIA ? p.fTot : Math.Max(0, p.fTot - saldoC);
                        var u = c.CreateCommand(); u.Transaction = tx;
                        u.CommandText = "UPDATE Facturas SET Abono = $a WHERE NumeroF = $n";
                        u.Parameters.AddWithValue("$a", (double)nuevoAb);
                        u.Parameters.AddWithValue("$n", p.numF);
                        factUpd += u.ExecuteNonQuery();
                    }
                }

                // 3. Normalizar estados: cualquier CxC con saldo <= 0 -> 'Saldada'; vigente vencida -> 'Vencida'
                var uSald = c.CreateCommand(); uSald.Transaction = tx;
                uSald.CommandText = @"
                    UPDATE CuentasCobrar
                    SET Pagado = Monto, Estado = 'Saldada'
                    WHERE COALESCE(Estado,'') NOT IN ('Saldada','Anulada')
                      AND Monto > 0 AND (Monto - COALESCE(Pagado,0)) <= 0.005";
                try { cxcUpd += uSald.ExecuteNonQuery(); } catch { }

                var uVenc = c.CreateCommand(); uVenc.Transaction = tx;
                uVenc.CommandText = @"
                    UPDATE CuentasCobrar
                    SET Estado = 'Vencida'
                    WHERE Estado = 'Vigente'
                      AND FechaVence IS NOT NULL AND substr(FechaVence,1,10) < $hoy
                      AND (Monto - COALESCE(Pagado,0)) > 0.005";
                uVenc.Parameters.AddWithValue("$hoy", hoy);
                try { uVenc.ExecuteNonQuery(); } catch { }

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
            }
            return (cxcUpd, factUpd, cxcAnul);
        }

        public static void SincronizarCxCYFacturasEnArranque()
        {
            try
            {
                using var c = Db.Conn();
                var (cxc, fact, anul) = SincronizarCxCYFacturas(c);
                if (cxc > 0 || fact > 0 || anul > 0)
                    Console.WriteLine($"[API] Reconciliación CxC↔Facturas: {cxc} CxC actualizadas, {fact} facturas actualizadas, {anul} CxC anuladas.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] WARN Reconciliación CxC: {ex.Message}");
            }
        }

        public static void MapAbonosEndpoints(this WebApplication app)
        {
            // ── GET /api/cxc ────────────────────────────────────────────────
            // Lista cuentas por cobrar (filtrables por cliente y/o estado pendiente).
            app.MapGet("/api/cxc", (HttpContext ctx, string? cliente, string? estado, bool? soloPendientes) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                string hoy = DateTime.Today.ToString("yyyy-MM-dd");

                var cmd = c.CreateCommand();
                var where = new List<string>();
                if (!string.IsNullOrWhiteSpace(cliente))
                {
                    string cod = cliente.Trim();
                    string? nom = ObtenerNombreClienteTx(c, null, cod);
                    where.Add("(Cliente = $cod OR Cliente = $nom OR Cliente LIKE $pref)");
                    cmd.Parameters.AddWithValue("$cod",  cod);
                    cmd.Parameters.AddWithValue("$nom",  nom ?? cod);
                    cmd.Parameters.AddWithValue("$pref", cod + " - %");
                }
                if (soloPendientes == true)
                {
                    where.Add("COALESCE(Estado,'Vigente') NOT IN ('Saldada','Pagada','Pagado','Anulada') AND (Monto - COALESCE(Pagado,0)) > 0.005");
                }
                else if (!string.IsNullOrWhiteSpace(estado))
                {
                    string est = estado.Trim().ToLowerInvariant();
                    if (est == "pendiente" || est == "abiertas")
                        where.Add("COALESCE(Estado,'Vigente') NOT IN ('Saldada','Pagada','Pagado','Anulada') AND (Monto - COALESCE(Pagado,0)) > 0.005");
                    else if (est == "vencida")
                        where.Add("COALESCE(Estado,'Vigente') NOT IN ('Saldada','Pagada','Pagado','Anulada') AND substr(FechaVence,1,10) < $hoy AND (Monto - COALESCE(Pagado,0)) > 0.005");
                    else if (est == "saldada" || est == "pagada")
                        where.Add("(COALESCE(Estado,'') IN ('Saldada','Pagada','Pagado') OR (Monto - COALESCE(Pagado,0)) <= 0.005)");
                }
                cmd.Parameters.AddWithValue("$hoy", hoy);

                string filtro = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
                cmd.CommandText = $@"
                    SELECT Id, Cliente, Concepto, Monto, COALESCE(Pagado,0),
                           COALESCE(FechaEmision,''), COALESCE(FechaVence,''), COALESCE(Estado,'Vigente')
                    FROM CuentasCobrar
                    {filtro}
                    ORDER BY
                        CASE WHEN COALESCE(Estado,'Vigente') IN ('Saldada','Pagada','Pagado','Anulada') THEN 1 ELSE 0 END,
                        FechaVence ASC, Id ASC
                    LIMIT 500";

                var lista = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    decimal monto  = Db.D(r, 3);
                    decimal pagado = Db.D(r, 4);
                    decimal saldo  = Math.Max(0, monto - pagado);
                    string concepto = Db.S(r, 2);
                    string fe = Db.S(r, 5);
                    string fv = Db.S(r, 6);
                    string estDb = Db.S(r, 7);
                    bool cerrada = EsEstadoCerrado(estDb) || saldo <= TOLERANCIA;
                    bool vencida = !cerrada && !string.IsNullOrEmpty(fv)
                                   && string.CompareOrdinal(fv[..Math.Min(10, fv.Length)], hoy) < 0;
                    string estadoNorm = estDb.Equals("Anulada", StringComparison.OrdinalIgnoreCase) ? "Anulada"
                        : cerrada ? "Saldada"
                        : vencida ? "Vencida" : "Vigente";

                    lista.Add(new
                    {
                        id            = Db.I(r, 0),
                        cliente       = Db.S(r, 1),
                        concepto,
                        numeroFactura = ExtraerNumeroFactura(concepto) ?? concepto,
                        monto,
                        pagado,
                        saldo,
                        fecha         = fe,
                        fechaEmision  = fe,
                        fechaVence    = fv,
                        estado        = estadoNorm,
                        estaVencida   = vencida
                    });
                }
                return Results.Ok(lista);
            }).RequireAuthorization();

            // ── POST /api/cxc/{id}/pago ─────────────────────────────────────
            // Registra un abono parcial o total directamente sobre una CxC.
            // Acepta JSON o multipart/form-data (con comprobante opcional).
            app.MapPost("/api/cxc/{id:int}/pago", async (HttpContext ctx, int id) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();

                decimal monto = 0;
                string fecha = DateTime.Today.ToString("yyyy-MM-dd");
                string metodo = "Efectivo", referencia = "", remitente = "", observaciones = "", rutaComp = "";
                int idCuenta = 0;

                if (ctx.Request.HasFormContentType)
                {
                    var form = await ctx.Request.ReadFormAsync();
                    string montoStr = form["monto"].ToString();
                    if (!decimal.TryParse(montoStr, NumberStyles.Any, CultureInfo.InvariantCulture, out monto) || monto <= 0)
                        return Results.BadRequest(new { error = "Monto inválido." });
                    if (!string.IsNullOrWhiteSpace(form["fecha"].ToString())) fecha = form["fecha"].ToString();
                    if (!string.IsNullOrWhiteSpace(form["metodoPago"].ToString())) metodo = form["metodoPago"].ToString();
                    referencia    = form["referencia"].ToString();
                    remitente     = form["remitente"].ToString();
                    observaciones = form["observaciones"].ToString();
                    int.TryParse(form["idCuenta"].ToString(), out idCuenta);

                    var archivo = form.Files.GetFile("comprobante") ?? form.Files.GetFile("imagen");
                    var (ruta, errArch) = await GuardarComprobanteArchivoAsync(archivo, "abonos", $"cxc_{id}");
                    if (errArch != null) return Results.BadRequest(new { error = errArch });
                    rutaComp = ruta;
                }
                else
                {
                    var body = await ctx.Request.ReadFromJsonAsync<PagoCxCRequest>();
                    if (body == null || body.Monto <= 0)
                        return Results.BadRequest(new { error = "Monto inválido." });
                    monto         = body.Monto;
                    fecha         = string.IsNullOrWhiteSpace(body.Fecha) ? fecha : body.Fecha!;
                    metodo        = string.IsNullOrWhiteSpace(body.MetodoPago) ? "Efectivo" : body.MetodoPago!;
                    referencia    = body.Referencia ?? "";
                    remitente     = body.Remitente ?? "";
                    observaciones = body.Observaciones ?? "";
                    idCuenta      = body.IdCuenta;
                }

                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    var res = AplicarPagoACxCTx(c, tx, id, monto, fecha, metodo, referencia, remitente, rutaComp, observaciones, idCuenta, insertarAbono: true);
                    if (!res.Encontrada) { tx.Rollback(); return Results.NotFound(new { error = res.Error }); }
                    if (res.Error != null) { tx.Rollback(); return Results.BadRequest(new { error = res.Error }); }
                    tx.Commit();
                    return Results.Ok(new
                    {
                        ok                 = true,
                        idAbono            = res.IdAbono,
                        idCxC              = res.IdCxC,
                        concepto           = res.Concepto,
                        numeroF            = res.NumeroF,
                        montoAplicado      = res.MontoAplicado,
                        pagado             = res.PagadoTotal,
                        saldo              = res.SaldoRestante,
                        estado             = res.EstadoCxC,
                        saldada            = res.EstadoCxC == "Saldada",
                        facturaActualizada = res.FacturaActualizada
                    });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem(ex.Message);
                }
            }).RequireAuthorization().DisableAntiforgery();

            // ── POST /api/cxc/sincronizar ───────────────────────────────────
            app.MapPost("/api/cxc/sincronizar", (HttpContext ctx) =>
            {
                if (!TokenService.EsAdmin(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var (cxc, fact, anul) = SincronizarCxCYFacturas(c);
                return Results.Ok(new
                {
                    ok = true,
                    cxcActualizadas = cxc,
                    facturasActualizadas = fact,
                    cxcAnuladas = anul
                });
            }).RequireAuthorization();

            // ── GET /api/abonos ─────────────────────────────────────────────
            app.MapGet("/api/abonos", (HttpContext ctx,
                string? cliente, string? desde, string? hasta) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                var where = new List<string>();
                if (!string.IsNullOrWhiteSpace(cliente))
                {
                    where.Add("a.Cliente = $cl");
                    cmd.Parameters.AddWithValue("$cl", cliente);
                }
                if (!string.IsNullOrWhiteSpace(desde))
                {
                    where.Add("a.Fecha >= $desde");
                    cmd.Parameters.AddWithValue("$desde", desde);
                }
                if (!string.IsNullOrWhiteSpace(hasta))
                {
                    where.Add("a.Fecha <= $hasta");
                    cmd.Parameters.AddWithValue("$hasta", hasta);
                }
                string filtro = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";
                cmd.CommandText = $@"
                    SELECT a.NumAbono, a.NumeroF, a.Fecha, a.Monto, a.Cliente,
                           COALESCE(a.MetodoPago,'Efectivo'),
                           COALESCE(a.Referencia,''),
                           COALESCE(a.NombreRemitente,''),
                           COALESCE(a.RutaComprobante,''),
                           COALESCE(a.Observaciones,''),
                           COALESCE(a.IdCuentaReceptora,0),
                           COALESCE(a.Estado,'Confirmado'),
                           COALESCE(cl.Descripcion, a.Cliente)
                    FROM Abonos a
                    LEFT JOIN Clientes cl ON cl.Codigo = a.Cliente
                    {filtro}
                    ORDER BY a.Fecha DESC, a.NumAbono DESC
                    LIMIT 500";

                var list = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new
                    {
                        id              = r.GetInt32(0),
                        numeroF         = Db.S(r, 1),
                        fecha           = Db.S(r, 2),
                        monto           = Db.D(r, 3),
                        cliente         = Db.S(r, 4),
                        metodoPago      = Db.S(r, 5),
                        referencia      = Db.S(r, 6),
                        remitente       = Db.S(r, 7),
                        rutaComprobante = Db.S(r, 8),
                        observaciones   = Db.S(r, 9),
                        idCuenta        = r.GetInt32(10),
                        estado          = Db.S(r, 11),
                        clienteNombre   = Db.S(r, 12)
                    });
                }
                return Results.Ok(list);
            }).RequireAuthorization();

            // ── POST /api/abonos (multipart) ────────────────────────────────
            // Soporta aplicar el abono a una CxC específica (idCuentaCobrar),
            // a una factura específica (numeroF), o automáticamente en orden FIFO
            // sobre las CxC pendientes del cliente (comportamiento por defecto si
            // estado == "Confirmado" y aplicarCxC != "false").
            app.MapPost("/api/abonos", async (HttpContext ctx) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();

                var form = await ctx.Request.ReadFormAsync();
                string cliente        = form["cliente"].ToString().Trim();
                string fecha          = form["fecha"].ToString().Trim();
                string montoStr       = form["monto"].ToString().Trim();
                string metodo         = form["metodoPago"].ToString().Trim();
                string referencia     = form["referencia"].ToString().Trim();
                string remitente      = form["remitente"].ToString().Trim();
                string observaciones  = form["observaciones"].ToString().Trim();
                string cuentaStr      = form["idCuenta"].ToString().Trim();
                string estado         = form["estado"].ToString().Trim();
                string idCxcStr       = form["idCuentaCobrar"].ToString().Trim();
                string numeroF        = form["numeroF"].ToString().Trim();
                string aplicarCxcStr  = form["aplicarCxC"].ToString().Trim();

                if (string.IsNullOrWhiteSpace(cliente) || string.IsNullOrWhiteSpace(montoStr))
                    return Results.BadRequest(new { error = "cliente y monto son requeridos." });

                if (!decimal.TryParse(montoStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
                    return Results.BadRequest(new { error = "Monto inválido." });

                int.TryParse(cuentaStr, out int idCuenta);
                int.TryParse(idCxcStr, out int idCuentaCobrar);
                if (string.IsNullOrWhiteSpace(fecha)) fecha = DateTime.Today.ToString("yyyy-MM-dd");
                if (string.IsNullOrWhiteSpace(estado)) estado = "Confirmado";
                bool aplicarCxC = !string.Equals(aplicarCxcStr, "false", StringComparison.OrdinalIgnoreCase);

                var archivo = form.Files.GetFile("comprobante");
                var (rutaComprobante, errArch) = await GuardarComprobanteArchivoAsync(archivo, "abonos", "abono");
                if (errArch != null) return Results.BadRequest(new { error = errArch });

                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    if (estado == "Confirmado" && aplicarCxC)
                    {
                        // Caso 1: CxC específica seleccionada
                        if (idCuentaCobrar > 0)
                        {
                            var res = AplicarPagoACxCTx(c, tx, idCuentaCobrar, monto, fecha, metodo, referencia,
                                remitente, rutaComprobante, observaciones, idCuenta, insertarAbono: true);
                            if (!res.Encontrada) { tx.Rollback(); return Results.NotFound(new { error = res.Error }); }
                            if (res.Error != null) { tx.Rollback(); return Results.BadRequest(new { error = res.Error }); }
                            tx.Commit();
                            return Results.Ok(new
                            {
                                id = res.IdAbono,
                                mensaje = res.EstadoCxC == "Saldada"
                                    ? $"Abono registrado. {res.Concepto} saldada en su totalidad."
                                    : $"Abono registrado en {res.Concepto}. Saldo restante: ${res.SaldoRestante:F2}.",
                                aplicadas = new[] { new { idCxC = res.IdCxC, concepto = res.Concepto, monto = res.MontoAplicado, saldo = res.SaldoRestante, estado = res.EstadoCxC } }
                            });
                        }

                        // Caso 2: Factura específica indicada
                        if (!string.IsNullOrWhiteSpace(numeroF))
                        {
                            var res = AplicarPagoAFacturaTx(c, tx, numeroF, monto, fecha, metodo, referencia,
                                remitente, rutaComprobante, observaciones, idCuenta, insertarAbono: true);
                            if (!res.Encontrada) { tx.Rollback(); return Results.NotFound(new { error = res.Error }); }
                            if (res.Error != null) { tx.Rollback(); return Results.BadRequest(new { error = res.Error }); }
                            tx.Commit();
                            return Results.Ok(new
                            {
                                id = res.IdAbono,
                                mensaje = res.Pagada
                                    ? $"Abono registrado. Factura {res.NumeroF} saldada."
                                    : $"Abono registrado en Factura {res.NumeroF}. Saldo restante: ${res.SaldoRestante:F2}.",
                                aplicadas = new[] { new { idCxC = res.IdCxC ?? 0, concepto = $"Factura {res.NumeroF}", monto = res.MontoAplicado, saldo = res.SaldoRestante, estado = res.Pagada ? "Saldada" : "Vigente" } }
                            });
                        }

                        // Caso 3: Automático FIFO sobre las CxC pendientes del cliente
                        var (aplicadas, sobrante) = AplicarFifoAClienteTx(c, tx, cliente, monto);
                        string? nfGuardar = aplicadas.Count == 1 ? aplicadas[0].numeroF : null;
                        string obsFinal = observaciones;
                        if (aplicadas.Count > 0)
                        {
                            string detalle = "Aplicado a: " + string.Join(", ",
                                aplicadas.Select(a => $"{a.concepto} (${a.aplicado:F2})"));
                            obsFinal = string.IsNullOrWhiteSpace(obsFinal) ? detalle : $"{obsFinal} | {detalle}";
                        }
                        int newIdFifo = InsertarRegistroAbonoTx(c, tx, nfGuardar, fecha, monto, cliente,
                            metodo, referencia, remitente, rutaComprobante, obsFinal, idCuenta, estado);
                        tx.Commit();
                        return Results.Ok(new
                        {
                            id = newIdFifo,
                            mensaje = aplicadas.Count > 0
                                ? $"Abono registrado y aplicado a {aplicadas.Count} cuenta(s) por cobrar."
                                : "Abono registrado (el cliente no tenía CxC pendientes).",
                            aplicadas = aplicadas.Select(a => new { a.idCxC, a.concepto, monto = a.aplicado }).ToList(),
                            sobrante
                        });
                    }

                    // Caso 4: Abono Pendiente o explícitamente sin aplicar a CxC
                    int newId = InsertarRegistroAbonoTx(c, tx, string.IsNullOrWhiteSpace(numeroF) ? null : numeroF,
                        fecha, monto, cliente, metodo, referencia, remitente, rutaComprobante, observaciones, idCuenta, estado);
                    tx.Commit();
                    return Results.Ok(new { id = newId, mensaje = "Abono registrado." });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem(ex.Message);
                }
            }).RequireAuthorization()
              .DisableAntiforgery();

            // ── PUT /api/abonos/{id}/estado ──────────────────────────────────
            app.MapPut("/api/abonos/{id}/estado", async (HttpContext ctx, int id) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();

                var body = await ctx.Request.ReadFromJsonAsync<EstadoRequest>();
                if (body == null || string.IsNullOrWhiteSpace(body.Estado))
                    return Results.BadRequest(new { error = "estado requerido." });

                var validos = new[] { "Confirmado", "Pendiente", "Rechazado" };
                if (!validos.Contains(body.Estado))
                    return Results.BadRequest(new { error = $"Estado inválido. Valores: {string.Join(", ", validos)}" });

                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    string estadoAnt, cliente, numF;
                    decimal monto;
                    var q = c.CreateCommand(); q.Transaction = tx;
                    q.CommandText = "SELECT COALESCE(Estado,'Confirmado'), COALESCE(Cliente,''), COALESCE(NumeroF,''), COALESCE(Monto,0) FROM Abonos WHERE NumAbono = $id";
                    q.Parameters.AddWithValue("$id", id);
                    using (var r = q.ExecuteReader())
                    {
                        if (!r.Read()) { tx.Rollback(); return Results.NotFound(new { error = "Abono no encontrado." }); }
                        estadoAnt = Db.S(r, 0);
                        cliente   = Db.S(r, 1);
                        numF      = Db.S(r, 2);
                        monto     = Db.D(r, 3);
                    }

                    var cmd = c.CreateCommand(); cmd.Transaction = tx;
                    cmd.CommandText = "UPDATE Abonos SET Estado = $est WHERE NumAbono = $id";
                    cmd.Parameters.AddWithValue("$est", body.Estado);
                    cmd.Parameters.AddWithValue("$id", id);
                    cmd.ExecuteNonQuery();

                    // Si pasa de Pendiente -> Confirmado, aplicar a la factura o CxC FIFO del cliente
                    if (estadoAnt == "Pendiente" && body.Estado == "Confirmado" && monto > 0)
                    {
                        if (!string.IsNullOrWhiteSpace(numF))
                            AplicarPagoAFacturaTx(c, tx, numF, monto, "", "", "", "", "", "", 0, insertarAbono: false);
                        else if (!string.IsNullOrWhiteSpace(cliente))
                            AplicarFifoAClienteTx(c, tx, cliente, monto);
                    }

                    tx.Commit();
                    return Results.Ok(new { mensaje = "Estado actualizado." });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem(ex.Message);
                }
            }).RequireAuthorization();

            // ── GET /api/abonos/cuentas ─────────────────────────────────────
            app.MapGet("/api/abonos/cuentas", (HttpContext ctx) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Nombre, Banco, Tipo, Titular, Documento, Activa
                    FROM CuentasReceptoras ORDER BY Nombre";
                var list = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new
                    {
                        id        = r.GetInt32(0),
                        nombre    = Db.S(r, 1),
                        banco     = Db.S(r, 2),
                        tipo      = Db.S(r, 3),
                        titular   = Db.S(r, 4),
                        documento = Db.S(r, 5),
                        activa    = r.GetInt32(6) == 1
                    });
                return Results.Ok(list);
            }).RequireAuthorization();

            // ── GET /api/abonos/estado-cuenta/{codigoCliente} ───────────────
            app.MapGet("/api/abonos/estado-cuenta/{codigoCliente}", (HttpContext ctx, string codigoCliente) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();

                // Datos del cliente
                var cmdCl = c.CreateCommand();
                cmdCl.CommandText = "SELECT Descripcion, CiRif, COALESCE(AFavor,0) FROM Clientes WHERE Codigo=$cod";
                cmdCl.Parameters.AddWithValue("$cod", codigoCliente);
                using var rcl = cmdCl.ExecuteReader();
                if (!rcl.Read())
                    return Results.NotFound(new { error = "Cliente no encontrado." });
                string nombre = Db.S(rcl, 0);
                string ciRif  = Db.S(rcl, 1);
                decimal saldoFavor = Db.D(rcl, 2);
                rcl.Close();

                // Abonos del cliente
                var cmdA = c.CreateCommand();
                cmdA.CommandText = @"
                    SELECT NumAbono, Fecha, Monto,
                           COALESCE(MetodoPago,'Efectivo'),
                           COALESCE(Referencia,''),
                           COALESCE(Estado,'Confirmado'),
                           COALESCE(NumeroF,'')
                    FROM Abonos WHERE Cliente=$cod
                    ORDER BY Fecha DESC, NumAbono DESC";
                cmdA.Parameters.AddWithValue("$cod", codigoCliente);
                var abonos = new List<object>();
                decimal totalConfirmado = 0;
                using var ra = cmdA.ExecuteReader();
                while (ra.Read())
                {
                    string est = Db.S(ra, 5);
                    decimal m  = Db.D(ra, 2);
                    if (est == "Confirmado") totalConfirmado += m;
                    abonos.Add(new
                    {
                        id         = ra.GetInt32(0),
                        fecha      = Db.S(ra, 1),
                        monto      = m,
                        metodo     = Db.S(ra, 3),
                        referencia = Db.S(ra, 4),
                        estado     = est,
                        numeroF    = Db.S(ra, 6)
                    });
                }

                // Cuentas por cobrar pendientes
                var cmdF = c.CreateCommand();
                cmdF.CommandText = @"
                    SELECT Id, Concepto, Monto, COALESCE(Pagado,0), FechaEmision, FechaVence, COALESCE(Estado,'Vigente')
                    FROM CuentasCobrar
                    WHERE (Cliente = $cod OR Cliente = $nom OR Cliente LIKE $pref)
                      AND COALESCE(Estado,'Vigente') NOT IN ('Saldada','Pagada','Pagado','Anulada')
                      AND (Monto - COALESCE(Pagado,0)) > 0.005
                    ORDER BY FechaVence";
                cmdF.Parameters.AddWithValue("$cod",  codigoCliente);
                cmdF.Parameters.AddWithValue("$nom",  nombre);
                cmdF.Parameters.AddWithValue("$pref", codigoCliente + " - %");
                var facturas = new List<object>();
                decimal totalDeuda = 0;
                using var rf = cmdF.ExecuteReader();
                while (rf.Read())
                {
                    decimal monto2 = Db.D(rf, 2);
                    decimal pagado = Db.D(rf, 3);
                    decimal saldo2 = Math.Max(0, monto2 - pagado);
                    totalDeuda += saldo2;
                    facturas.Add(new
                    {
                        id         = Db.I(rf, 0),
                        concepto   = Db.S(rf, 1),
                        total      = monto2,
                        pagado     = pagado,
                        saldo      = saldo2,
                        emision    = Db.S(rf, 4),
                        vence      = Db.S(rf, 5),
                        estado     = Db.S(rf, 6)
                    });
                }

                return Results.Ok(new
                {
                    cliente = new { codigo = codigoCliente, nombre, ciRif },
                    saldoFavor,
                    totalAbonos = totalConfirmado,
                    totalDeuda,
                    abonos,
                    facturasPendientes = facturas
                });
            }).RequireAuthorization();

            // ── GET /api/abonos/{id}/comprobante ────────────────────────────
            app.MapGet("/api/abonos/{id}/comprobante", (HttpContext ctx, int id) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = "SELECT COALESCE(RutaComprobante,'') FROM Abonos WHERE NumAbono=$id";
                cmd.Parameters.AddWithValue("$id", id);
                var ruta = cmd.ExecuteScalar()?.ToString() ?? "";
                if (string.IsNullOrEmpty(ruta) || !File.Exists(ruta))
                    return Results.NotFound(new { error = "Comprobante no encontrado." });

                string mime = Path.GetExtension(ruta).ToLower() switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png"           => "image/png",
                    ".webp"          => "image/webp",
                    ".pdf"           => "application/pdf",
                    ".bmp"           => "image/bmp",
                    _                => "application/octet-stream"
                };
                return Results.File(ruta, mime);
            }).RequireAuthorization();

            // ── POST /api/facturas/{numeroF}/comprobantes ──────────────────
            // Crea un ComprobanteFactura + Abono vinculado (multipart).
            // Si la factura tiene saldo pendiente (crédito), aplica también el abono
            // sobre Facturas.Abono y CuentasCobrar.Pagado/Estado.
            app.MapPost("/api/facturas/{numeroF}/comprobantes", async (HttpContext ctx, string numeroF) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();

                var form = await ctx.Request.ReadFormAsync();
                string metodoPago  = form["metodoPago"].FirstOrDefault() ?? "Efectivo";
                string montoStr    = form["monto"].FirstOrDefault() ?? "0";
                string referencia  = form["referencia"].FirstOrDefault() ?? "";
                string cuentaIdStr = form["idCuentaReceptora"].FirstOrDefault() ?? "0";
                string obs         = form["observaciones"].FirstOrDefault() ?? "";
                string cliente     = form["cliente"].FirstOrDefault() ?? "";

                if (!decimal.TryParse(montoStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
                    return Results.BadRequest(new { error = "Monto inválido." });

                int.TryParse(cuentaIdStr, out int idCuenta);

                var archivo = form.Files.GetFile("imagen");
                var (rutaImagen, errArch) = await GuardarComprobanteArchivoAsync(archivo, "facturas", $"{numeroF}_{metodoPago}");
                if (errArch != null) return Results.BadRequest(new { error = errArch });

                using var c = Db.Conn();
                using var tx = c.BeginTransaction();
                try
                {
                    string hoy = DateTime.Today.ToString("yyyy-MM-dd");

                    // Insertar ComprobanteFactura
                    var cmd = c.CreateCommand(); cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO ComprobantesFactura
                            (NumeroF, MetodoPago, Monto, Referencia, RutaImagen,
                             IdCuentaReceptora, Fecha, Observaciones)
                        VALUES ($nf, $mp, $mo, $ref, $ri, $icr, $fe, $obs)";
                    cmd.Parameters.AddWithValue("$nf",  numeroF);
                    cmd.Parameters.AddWithValue("$mp",  metodoPago);
                    cmd.Parameters.AddWithValue("$mo",  (double)monto);
                    cmd.Parameters.AddWithValue("$ref", referencia);
                    cmd.Parameters.AddWithValue("$ri",  rutaImagen);
                    cmd.Parameters.AddWithValue("$icr", idCuenta);
                    cmd.Parameters.AddWithValue("$fe",  hoy);
                    cmd.Parameters.AddWithValue("$obs", obs);
                    cmd.ExecuteNonQuery();

                    // Verificar si la factura tiene saldo pendiente (crédito)
                    decimal fTotal = 0, fAbono = 0; string fCli = cliente;
                    bool existeFact = false;
                    var qF = c.CreateCommand(); qF.Transaction = tx;
                    qF.CommandText = "SELECT TotalF, COALESCE(Abono,0), COALESCE(Codigo,'') FROM Facturas WHERE NumeroF = $n";
                    qF.Parameters.AddWithValue("$n", numeroF);
                    using (var rf = qF.ExecuteReader())
                    {
                        if (rf.Read())
                        {
                            existeFact = true;
                            fTotal = Db.D(rf, 0);
                            fAbono = Db.D(rf, 1);
                            if (string.IsNullOrWhiteSpace(fCli)) fCli = Db.S(rf, 2);
                        }
                    }

                    bool abonoAplicadoACxC = false;
                    if (existeFact && (fTotal - fAbono) > TOLERANCIA)
                    {
                        var res = AplicarPagoAFacturaTx(c, tx, numeroF, monto, hoy, metodoPago, referencia,
                            "", rutaImagen, string.IsNullOrWhiteSpace(obs) ? $"Comprobante factura {numeroF}" : obs,
                            idCuenta, insertarAbono: true);
                        abonoAplicadoACxC = res.CxcActualizada;
                    }
                    else
                    {
                        // Factura de contado (ya pagada): registrar el abono solo como respaldo del comprobante
                        InsertarRegistroAbonoTx(c, tx, numeroF, hoy, monto, fCli, metodoPago, referencia,
                            "", rutaImagen, string.IsNullOrWhiteSpace(obs) ? $"Comprobante factura {numeroF}" : obs,
                            idCuenta, "Confirmado");
                    }

                    tx.Commit();
                    return Results.Ok(new { ok = true, metodo = metodoPago, monto, abonoAplicadoACxC });
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return Results.Problem(ex.Message);
                }
            }).RequireAuthorization().DisableAntiforgery();

            // ── GET /api/facturas/{numeroF}/comprobantes ───────────────────
            app.MapGet("/api/facturas/{numeroF}/comprobantes", (HttpContext ctx, string numeroF) =>
            {
                if (!TokenService.PuedeVerCxC(ctx.User)) return Results.Forbid();
                using var c = Db.Conn();
                var cmd = c.CreateCommand();
                cmd.CommandText = @"
                    SELECT cf.Id, cf.MetodoPago, cf.Monto, cf.Referencia,
                           cf.IdCuentaReceptora, cf.Fecha, cf.Observaciones,
                           COALESCE(cr.Nombre,'') as CuentaNombre
                    FROM ComprobantesFactura cf
                    LEFT JOIN CuentasReceptoras cr ON cr.Id = cf.IdCuentaReceptora
                    WHERE cf.NumeroF = $nf
                    ORDER BY cf.Fecha";
                cmd.Parameters.AddWithValue("$nf", numeroF);
                var list = new List<object>();
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    list.Add(new
                    {
                        id        = r.GetInt32(0),
                        metodo    = Db.S(r, 1),
                        monto     = Db.D(r, 2),
                        referencia= Db.S(r, 3),
                        idCuenta  = r.GetInt32(4),
                        fecha     = Db.S(r, 5),
                        obs       = Db.S(r, 6),
                        cuenta    = Db.S(r, 7)
                    });
                return Results.Ok(list);
            }).RequireAuthorization();
        }
    }

    // DTOs
    public record EstadoRequest(string Estado);
    public record PagoCxCRequest(
        decimal Monto,
        string? Fecha = null,
        string? MetodoPago = null,
        string? Referencia = null,
        string? Remitente = null,
        int IdCuenta = 0,
        string? Observaciones = null);
}
