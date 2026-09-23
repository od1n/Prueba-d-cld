using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════════
    //  LISTA DE FACTURAS
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmFacturas : FormBase
    {
        private DataGridView dgv;
        private Label        lblResumen;
        private List<Factura> _facturas = new List<Factura>();
        private string _columnaOrden = "Fecha";
        private bool _ordenAsc = false;

        public FrmFacturas()
        {
            Text          = "Facturas";
            Size          = new Size(1130, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(240, 244, 250);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label
            {
                Text = "Facturas", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13)
            });

            var btnNueva    = Btn("+ Nueva",     Color.FromArgb(40, 160, 80));
            var btnVer      = Btn("Ver detalle", Color.FromArgb(60, 130, 190));
            var btnPdf      = Btn("Exportar PDF",Color.FromArgb(180, 60, 30));
            var btnImprimir = Btn("Imprimir",    Color.FromArgb(100, 60, 160));
            var btnReporte  = Btn("Reporte PDF", Color.FromArgb(20, 110, 80));
            var btnAnular   = Btn("Anular",      Color.FromArgb(180, 40, 40));
            var btnCorregir = Btn("Corregir",    Color.FromArgb(180, 130, 30), 100);
            
            
            var _btnAyuda = new Button {
                Text = "? Ayuda", Size = new Size(80, 32),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnAyuda.FlatAppearance.BorderSize = 0;
            _btnAyuda.Click += (s, e) => AyudaService.Mostrar(this, "FrmFacturas");
            var _pnlHdr = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            _pnlHdr.Controls.AddRange(new Control[] {
                _btnAyuda, btnNueva, btnVer, btnPdf, btnImprimir, btnReporte,
                btnCorregir, btnAnular });
            hdr.Controls.Add(_pnlHdr);
            // ── ToolTips ─────────────────────────────────────────────────
            var _tip = AyudaService.CrearTooltip();
            AyudaService.Tip(_tip, _btnAyuda, "Abrir la guía de ayuda de este módulo");
            AyudaService.Tip(_tip, btnNueva, "Crear una nueva factura de venta");
            AyudaService.Tip(_tip, btnVer, "Ver el detalle completo de la factura seleccionada");
            AyudaService.Tip(_tip, btnPdf, "Exportar la factura seleccionada a PDF");
            AyudaService.Tip(_tip, btnImprimir, "Imprimir la factura directamente en la impresora");
            AyudaService.Tip(_tip, btnReporte, "Generar reporte PDF del período filtrado");
            AyudaService.Tip(_tip, btnAnular,
                "Anular la factura seleccionada (queda en BD para auditoría, " +
                "no afecta CxC ni reportes). Requiere motivo y permiso.");
            AyudaService.Tip(_tip, btnCorregir,
                "Corregir la fecha de una factura anulada por fecha inválida. " +
                "Útil para recuperar facturas afectadas por la migración v21.");

            dgv = Grid(Color.FromArgb(26, 74, 138));
            dgv.DataBindingComplete += (s, e) => ColorearFilas();
            dgv.CellDoubleClick     += (s, e) => VerDetalle();
            dgv.ColumnHeaderMouseClick += (s, e) => OrdenarPorColumna(e.ColumnIndex);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 45, BackColor = Color.FromArgb(240, 244, 250) };
            lblResumen = new Label
            {
                AutoSize  = true, Location = new Point(12, 12),
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 50, 120)
            };
            var btnCerrar = BtnFooter("Cerrar", footer);
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(lblResumen);

            Controls.AddRange(new Control[] { dgv, hdr, footer });

            btnNueva.Click    += (s, e) => NuevaFactura();
            btnVer.Click      += (s, e) => VerDetalle();
            btnPdf.Click      += (s, e) => ExportarPdf();
            btnImprimir.Click += (s, e) => ImprimirFactura();
            btnReporte.Click  += (s, e) => ExportarReporte();
            btnAnular.Click   += (s, e) => AnularFacturaConDialogo();
            btnCorregir.Click += (s, e) => CorregirFechaConDialogo();

            
            VentanaPrefs.Restaurar(this);
            var _tp = new System.Windows.Forms.Timer { Interval = 800 };
            _tp.Tick        += (s, e) => { _tp.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tp.Stop(); _tp.Start(); };
            LocationChanged += (s, e) => { _tp.Stop(); _tp.Start(); };
            FormClosed      += (s, e) => { _tp.Stop(); _tp.Dispose(); };

CargarDatos();
        }

        private void CargarDatos()
        {
            // En el LISTADO de Facturas siempre mostramos las NE (con badge),
            // independiente del toggle global. El toggle sólo afecta reportes
            // y agregados (dashboard, P&L, libro de IVA).
            _facturas = DatabaseService.ObtenerFacturas(incluirNotasEntrega: true);

            IEnumerable<Factura> ordenadas = _facturas;
            switch (_columnaOrden)
            {
                case "NumeroF":
                    ordenadas = _ordenAsc
                        ? ordenadas
                            .OrderBy(f => long.TryParse(f.NumeroF, out _) ? 0 : 1)
                            .ThenBy(f => long.TryParse(f.NumeroF, out var n) ? n : long.MaxValue)
                            .ThenBy(f => f.NumeroF)
                        : ordenadas
                            .OrderBy(f => long.TryParse(f.NumeroF, out _) ? 0 : 1)
                            .ThenByDescending(f => long.TryParse(f.NumeroF, out var n) ? n : long.MinValue)
                            .ThenByDescending(f => f.NumeroF);
                    break;
                case "Cliente":
                    ordenadas = _ordenAsc
                        ? ordenadas
                            .OrderBy(f => long.TryParse(f.Codigo, out _) ? 0 : 1)
                            .ThenBy(f => long.TryParse(f.Codigo, out var n) ? n : long.MaxValue)
                            .ThenBy(f => f.Codigo)
                        : ordenadas
                            .OrderBy(f => long.TryParse(f.Codigo, out _) ? 0 : 1)
                            .ThenByDescending(f => long.TryParse(f.Codigo, out var n) ? n : long.MinValue)
                            .ThenByDescending(f => f.Codigo);
                    break;
                case "Fecha":
                    ordenadas = _ordenAsc
                        ? ordenadas.OrderBy(f => f.Fecha)
                        : ordenadas.OrderByDescending(f => f.Fecha);
                    break;
                case "Total":
                    ordenadas = _ordenAsc
                        ? ordenadas.OrderBy(f => f.TotalF)
                        : ordenadas.OrderByDescending(f => f.TotalF);
                    break;
            }

            dgv.DataSource = null;
            dgv.DataSource = ordenadas.Select(f => new
            {
                Tipo       = f.EsNotaEntrega        ? "NE"
                           : f.EsFacturaExportacion ? "EXP"
                                                    : "FAC",
                f.NumeroF,
                Cliente    = f.Codigo,
                // Si la fecha es válida la mostramos, si no señalizamos el problema
                Fecha      = f.Fecha == DateTime.MinValue
                                ? "(fecha inválida)"
                                : f.Fecha.ToShortDateString(),
                FormaPago  = f.FormaDePago,
                Desc       = f.Descuento + "%",
                IVA        = f.EsNotaEntrega ? "—" : f.IvaF.ToString("C2"),
                Total      = f.TotalF.ToString("C2"),
                Abono      = f.Anulada       ? "—"
                           : f.Abono < 0     ? "CANCELADA"
                                             : f.Abono.ToString("C2"),
                Saldo      = f.Anulada || f.Abono < 0
                                ? "---" : (f.TotalF - f.Abono).ToString("C2"),
                Estado     = f.Anulada       ? "Anulada"
                           : f.Abono < 0     ? "Cancelada"
                           : (f.TotalF - f.Abono) <= 0 ? "Saldada"
                                             : "Pendiente"
            }).ToList();

            foreach (DataGridViewColumn col in dgv.Columns)
                col.SortMode = DataGridViewColumnSortMode.Programmatic;
            AplicarGlyphOrden();

            // Excluir anuladas y NE de los totales (no son ventas declarables)
            var validas = _facturas.Where(f =>
                !f.Anulada && !f.EsNotaEntrega && f.Abono >= 0).ToList();
            decimal total   = validas.Sum(f => f.TotalF);
            decimal cobrado = validas.Sum(f => f.Abono);
            int anuladas    = _facturas.Count(f => f.Anulada);
            int notasEntrega= _facturas.Count(f => f.EsNotaEntrega);
            decimal totalNE = _facturas.Where(f => f.EsNotaEntrega && !f.Anulada)
                                       .Sum(f => f.TotalF);

            string textoNE = notasEntrega > 0
                ? "   |   NE: " + notasEntrega + " (" + totalNE.ToString("C2") + ")"
                : "";
            lblResumen.Text =
                "  Documentos: " + _facturas.Count +
                (anuladas > 0 ? "  (" + anuladas + " anuladas)" : "") +
                "   |   Facturas legales: " + validas.Count +
                "   |   Total: " + total.ToString("C2") +
                "   |   Cobrado: " + cobrado.ToString("C2") +
                "   |   Saldo: " + (total - cobrado).ToString("C2") +
                textoNE;
        }

        private void OrdenarPorColumna(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= dgv.Columns.Count) return;
            var nombre = dgv.Columns[columnIndex].Name;

            if (nombre != "NumeroF" && nombre != "Cliente" && nombre != "Fecha" && nombre != "Total")
                return;

            if (_columnaOrden == nombre)
                _ordenAsc = !_ordenAsc;
            else
            {
                _columnaOrden = nombre;
                _ordenAsc = true;
            }

            CargarDatos();
        }

        private void AplicarGlyphOrden()
        {
            foreach (DataGridViewColumn col in dgv.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;

            if (dgv.Columns.Contains(_columnaOrden))
                dgv.Columns[_columnaOrden].HeaderCell.SortGlyphDirection =
                    _ordenAsc ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var estado = row.Cells["Estado"].Value?.ToString();
                var tipo   = row.Cells["Tipo"].Value?.ToString();

                if (estado == "Anulada")
                {
                    // Gris suave + texto tachado para que se vea claro que NO cuenta
                    row.DefaultCellStyle.BackColor = Color.FromArgb(235, 235, 240);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(140, 140, 150);
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Strikeout);
                }
                else if (tipo == "NE")
                {
                    // Notas de Entrega: ámbar claro — fácil de identificar a primera vista
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 215);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(120, 70, 10);
                }
                else if (tipo == "EXP")
                {
                    // Exportación: lila tenue — diferenciable del FAC normal
                    row.DefaultCellStyle.BackColor = Color.FromArgb(238, 232, 252);
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(60, 40, 130);
                }
                else if (estado == "Saldada")
                    row.DefaultCellStyle.BackColor = Color.FromArgb(225, 255, 230);
                else if (estado == "Cancelada")
                    row.DefaultCellStyle.BackColor = Color.FromArgb(245, 230, 230);
                else
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 215);
            }
        }

        // ── PDF ───────────────────────────────────────────────────────────────
        private (Factura f, List<LineaFactura> lineas) ObtenerFacturaSeleccionada()
        {
            if (dgv.CurrentRow == null) return (null, null);
            var num = dgv.CurrentRow.Cells["NumeroF"].Value?.ToString();
            var f   = DatabaseService.ObtenerFacturas().Find(x => x.NumeroF == num);
            if (f == null) return (null, null);
            int id  = ObtenerIdFactura(num);
            var lineas = DatabaseService.ObtenerDetallesFactura(id);
            return (f, lineas);
        }

        /// Busca datos del cliente para inyectar en el PDF
        private (string rif, string dir, string tel) ObtenerDatosCliente(string codigoCliente)
        {
            var cli = DatabaseService.ObtenerClientes()
                .Find(c => c.Codigo == codigoCliente || c.Descripcion == codigoCliente);
            if (cli == null) return ("", "", "");
            return (cli.CiRif ?? "", cli.Direccion ?? "", cli.Telefono ?? "");
        }

        private void ExportarPdf()
        {
            var (f, lineas) = ObtenerFacturaSeleccionada();
            if (f == null)
            {
                MessageBox.Show("Selecciona una factura primero.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var (rif, dir, tel) = ObtenerDatosCliente(f.Codigo);
                var pdf = PdfService.GenerarFactura(f, lineas,
                    rifCliente: rif, dirCliente: dir, telCliente: tel);
                string nombre = f.EsNotaEntrega ? "NotaEntrega_" : "Factura_";
                PdfService.GuardarYAbrir(pdf, nombre + f.NumeroF + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF:" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImprimirFactura()
        {
            var (f, lineas) = ObtenerFacturaSeleccionada();
            if (f == null)
            {
                MessageBox.Show("Selecciona una factura primero.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var (rif, dir, tel) = ObtenerDatosCliente(f.Codigo);
                var pdf = PdfService.GenerarFactura(f, lineas,
                    rifCliente: rif, dirCliente: dir, telCliente: tel);
                string nombre = f.EsNotaEntrega ? "NotaEntrega_" : "Factura_";
                PdfService.Imprimir(pdf, nombre + f.NumeroF + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al imprimir:" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportarReporte()
        {
            try
            {
                var lista = DatabaseService.ObtenerFacturas();
                var pdf   = PdfService.GenerarReporteFacturas(lista);
                PdfService.GuardarYAbrir(pdf,
                    "Reporte_Facturas_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar reporte:" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NuevaFactura()
        {
            using var dlg = new DlgFactura(null);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                // Verificar morosidad antes de crear factura a crédito
                if (dlg.Resultado.FormaDePago == "Credito")
                {
                    var (tieneMora, montoVencido, cuentas) = DatabaseService.VerificarMorosidadCliente(dlg.Resultado.Codigo);
                    if (tieneMora)
                    {
                        var resp = MessageBox.Show(
                            $"⚠ Este cliente tiene {cuentas} cuenta(s) por cobrar vencida(s)\n" +
                            $"Monto vencido: ${montoVencido:N2}\n\n" +
                            "¿Desea continuar con la venta a crédito?",
                            "Cliente con morosidad", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (resp != DialogResult.Yes) return;
                    }
                }

                try
                {
                    DatabaseService.InsertarFactura(dlg.Resultado);

                    // Obtener el ID de la factura recien insertada
                    int idFactura = ObtenerIdFactura(dlg.Resultado.NumeroF);

                    DatabaseService.GuardarDetallesYKardex(
                        idFactura,
                        dlg.Resultado.NumeroF,
                        dlg.Lineas,
                        esNueva: true);

                    // Guardar comprobantes de pago y crear abonos
                    if (dlg.Comprobantes.Count > 0)
                    {
                        string carpeta = System.IO.Path.Combine(
                            SecurityService.CarpetaDatos, "comprobantes", "facturas",
                            DateTime.Today.ToString("yyyy-MM"));
                        System.IO.Directory.CreateDirectory(carpeta);

                        foreach (var cf in dlg.Comprobantes)
                        {
                            cf.NumeroF = dlg.Resultado.NumeroF;
                            cf.Fecha   = dlg.Resultado.Fecha;

                            // Copiar imagen al almacén si existe
                            if (!string.IsNullOrWhiteSpace(cf.RutaImagen) &&
                                System.IO.File.Exists(cf.RutaImagen))
                            {
                                string ext = System.IO.Path.GetExtension(cf.RutaImagen);
                                string destino = System.IO.Path.Combine(carpeta,
                                    $"{cf.NumeroF}_{cf.MetodoPago}_{DateTime.Now:HHmmss}{ext}");
                                System.IO.File.Copy(cf.RutaImagen, destino, true);
                                cf.RutaImagen = destino;
                            }

                            DatabaseService.InsertarComprobanteFactura(cf);

                            // Crear abono vinculado
                            DatabaseService.InsertarAbonoExtendido(new Abono
                            {
                                NumeroF           = cf.NumeroF,
                                Fecha             = cf.Fecha,
                                Monto             = cf.Monto,
                                Cliente           = dlg.Resultado.Codigo,
                                MetodoPago        = cf.MetodoPago,
                                Referencia        = cf.Referencia,
                                RutaComprobante   = cf.RutaImagen ?? "",
                                IdCuentaReceptora = cf.IdCuentaReceptora,
                                Observaciones     = $"Comprobante factura {cf.NumeroF}",
                                Estado            = "Confirmado"
                            });
                        }
                    }

                    // Auto-crear CxC si la factura es a crédito
                    if (dlg.Resultado.FormaDePago == "Credito")
                    {
                        decimal saldoCxC = dlg.Resultado.TotalF - dlg.Resultado.Abono;
                        if (saldoCxC > 0)
                        {
                            var cli = DatabaseService.ObtenerClientes()
                                .Find(c => c.Codigo == dlg.Resultado.Codigo);
                            string nombreCli = cli != null
                                ? $"{cli.Codigo} - {cli.Descripcion}"
                                : dlg.Resultado.Codigo;
                            int diasCred = dlg.Resultado.DiasCreditoF > 0
                                ? dlg.Resultado.DiasCreditoF : 30;

                            DatabaseService.InsertarCuenta(new CuentaCobrar
                            {
                                Cliente      = nombreCli,
                                Concepto     = $"Factura {dlg.Resultado.NumeroF}",
                                Monto        = saldoCxC,
                                Pagado       = 0,
                                FechaEmision = dlg.Resultado.Fecha,
                                FechaVence   = dlg.Resultado.Fecha.AddDays(diasCred),
                                Estado       = "Vigente"
                            });
                        }
                    }

                    CargarDatos();
                }
                catch (Exception ex)
                {
                    // Si falla el kardex, revertir la factura tambien
                    DatabaseService.EliminarFactura(dlg.Resultado.NumeroF);
                    MessageBox.Show(
                        "Error al guardar la factura:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private int ObtenerIdFactura(string numeroF)
        {
            // ALTA-3 FIX: delegado a DatabaseService — sin SqliteConnection directa
            return DatabaseService.ObtenerIdFactura(numeroF);
        }

        private void VerDetalle()
        {
            if (dgv.CurrentRow == null) return;
            var num = dgv.CurrentRow.Cells["NumeroF"].Value?.ToString();
            if (string.IsNullOrEmpty(num)) return;

            try
            {
                var f = DatabaseService.ObtenerFacturas(incluirNotasEntrega: true)
                            .Find(x => x.NumeroF == num);
                if (f == null)
                {
                    MessageBox.Show(this, $"No se encontró la factura {num}.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int id     = ObtenerIdFactura(num);
                var lineas = DatabaseService.ObtenerDetallesFactura(id);

                using var dlg = new DlgFactura(f, lineas, soloLectura: true);
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error al abrir detalle:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── ANULACIÓN (v21) ───────────────────────────────────────────────
        // Reemplaza la vieja "Cancelar" (que usaba el hack Abono=-1).
        // Ahora marca Anulada=1 con motivo + usuario + fecha auditados.
        private void AnularFacturaConDialogo()
        {
            // Permiso
            if (!Sesion.EsAdmin && !Sesion.TienePermiso("FacturasAnular"))
            {
                MessageBox.Show(this,
                    "No tienes permiso para anular facturas.\n" +
                    "Solicita acceso a un administrador.",
                    "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (dgv.CurrentRow == null)
            {
                MessageBox.Show(this, "Selecciona una factura primero.",
                    "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var num = dgv.CurrentRow.Cells["NumeroF"].Value?.ToString();
            var f = _facturas.Find(x => x.NumeroF == num);
            if (f == null) return;

            if (f.Anulada)
            {
                MessageBox.Show(this,
                    "La factura " + num + " ya está anulada.\n\n" +
                    "Motivo registrado:\n" + (f.MotivoAnulacion ?? "(sin motivo)"),
                    "Ya anulada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Diálogo de motivo
            using var dlg = new DlgAnularFactura(f.NumeroF, f.TotalF);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                DatabaseService.AnularFactura(num, dlg.Motivo, Sesion.NombreUsuario ?? "desconocido");
                MessageBox.Show(this,
                    "Factura " + num + " anulada.\n\n" +
                    "ATENCIÓN: el stock NO se revertió automáticamente. " +
                    "Si la operación incluyó salida de inventario, registra " +
                    "una entrada manual en Kardex.",
                    "Anulada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al anular: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Corrección de fecha — pensado para des-romper facturas que la
        // migración v21 anuló por tener fecha inválida (año < 2000). El
        // operador ingresa la fecha correcta, se actualiza Y se reactiva.
        private void CorregirFechaConDialogo()
        {
            if (!Sesion.EsAdmin && !Sesion.TienePermiso("FacturasAnular"))
            {
                MessageBox.Show(this,
                    "No tienes permiso para corregir facturas.",
                    "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            if (dgv.CurrentRow == null) return;
            var num = dgv.CurrentRow.Cells["NumeroF"].Value?.ToString();
            var f = _facturas.Find(x => x.NumeroF == num);
            if (f == null) return;

            // Solo tiene sentido para anuladas-por-fecha o facturas con fecha rota
            bool fechaRota = f.Fecha == DateTime.MinValue || f.Fecha.Year < 2000;
            bool anuladaPorFecha = f.Anulada
                && (f.MotivoAnulacion?.IndexOf("Fecha", StringComparison.OrdinalIgnoreCase) >= 0);

            if (!fechaRota && !anuladaPorFecha)
            {
                MessageBox.Show(this,
                    "Esta opción es solo para facturas con fecha inválida " +
                    "(típicamente anuladas por la migración v21).\n\n" +
                    "Para editar una factura normal usa 'Ver detalle'.",
                    "No aplica", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new DlgCorregirFecha(f.NumeroF,
                f.MotivoAnulacion ?? "(sin motivo registrado)");
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                DatabaseService.CorregirFechaFactura(num, dlg.NuevaFecha,
                    Sesion.NombreUsuario ?? "desconocido");
                if (f.Anulada)
                {
                    var resp = MessageBox.Show(this,
                        "Fecha corregida a " + dlg.NuevaFecha.ToShortDateString() + ".\n\n" +
                        "¿Reactivar la factura (quitar el estado de anulada)?",
                        "Reactivar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (resp == DialogResult.Yes)
                        DatabaseService.ReactivarFactura(num, Sesion.NombreUsuario ?? "desconocido");
                }
                CargarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al corregir: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataGridView Grid(Color h)
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = h;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            return g;
        }

        private Button Btn(string t, Color c, int w = 90)
        {
            var b = new Button { Text = t, Size = new Size(w, 32),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0) };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        private Button BtnFooter(string t, Panel p)
        {
            var b = new Button
            {
                Text = t, Size = new Size(90, 27), Dock = DockStyle.Right,
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            b.FlatAppearance.BorderSize = 0;
            p.Controls.Add(b);
            return b;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO FACTURA  (cabecera + lineas de productos)
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgFactura : FormBase
    {
        public Factura                       Resultado   { get; private set; }
        public List<LineaFactura> Lineas { get; private set; }
            = new List<LineaFactura>();

        private class ClienteComboItem
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public string Texto => string.IsNullOrWhiteSpace(Descripcion)
                ? Codigo
                : Codigo + " - " + Descripcion;
        }

        // Cabecera
        private TextBox       txtNumero, txtTransporte, txtObs, txtDestinoExp;
        private DateTimePicker dtpFecha;
        private NumericUpDown  numDescuento, numAbono, numTotal, numDias;
        private ComboBox       cmbCliente, cmbFormaPago, cmbIva;
        private CheckBox       chkExportacion;
        private Label          lblDestinoExp, lblTipoDoc;
        private decimal        _ivaPctSistema = 16m;
        private decimal        _ivaCalculado = 0m;
        private List<Cliente>  _clientesFactura = new List<Cliente>();

        // ── Comprobantes de pago ────────────────────────────────────────
        private Button         _btnComprobantes;
        private List<ComprobanteFactura> _comprobantesTemp = new List<ComprobanteFactura>();
        private List<string>   _rutasImagenOrigen = new List<string>(); // rutas originales del file dialog
        public  List<ComprobanteFactura> Comprobantes => _comprobantesTemp;

        // Grid de lineas
        private DataGridView   dgvLineas;
        private Label          lblTotalLineas;

        private readonly bool _soloLectura;

        public DlgFactura(Factura f,
                          List<LineaFactura> lineas = null,
                          bool soloLectura = false)
        {
            _soloLectura = soloLectura;

            Text            = f == null ? "Nueva Factura"
                : soloLectura ? "Detalle Factura"
                : "Editar Factura " + f.NumeroF;
            Size            = new Size(900, 720);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox     = MinimizeBox = false;
            BackColor       = Color.FromArgb(240, 244, 250);

            // ── Banner ────────────────────────────────────────────────────
            var banner = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(26, 74, 138) };
            banner.Controls.Add(new Label
            {
                Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 10)
            });

            // ── Panel izquierdo: cabecera ─────────────────────────────────
            var panelIzq = new Panel
            {
                Location = new Point(0, 42),
                Size     = new Size(380, 460),
                BackColor= Color.FromArgb(240, 244, 250),
                AutoScroll = true
            };

            int y = 10;
            void Lbl(string t, int yy) => panelIzq.Controls.Add(
                new Label { Text = t, Location = new Point(10, yy + 3), AutoSize = true,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            TextBox Txt(int yy, string val = "")
            {
                var tb = new TextBox { Text = val, Location = new Point(130, yy), Size = new Size(220, 24),
                    ReadOnly = soloLectura };
                panelIzq.Controls.Add(tb);
                return tb;
            }
            NumericUpDown Num(int yy, decimal max, decimal val = 0)
            {
                var n = new NumericUpDown { Location = new Point(130, yy), Size = new Size(220, 24),
                    Maximum = max, DecimalPlaces = 2, Value = val,
                    ReadOnly = soloLectura, Enabled = !soloLectura };
                panelIzq.Controls.Add(n);
                return n;
            }

            Lbl("No. Factura:", y);  txtNumero = Txt(y); y += 32;

            Lbl("Cliente (Cod):", y);
            cmbCliente = new ComboBox
            {
                Location = new Point(130, y),
                Size = new Size(220, 24),
                DropDownStyle = soloLectura ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown,
                Enabled = !soloLectura
            };

            _clientesFactura = DatabaseService.ObtenerClientes()
                .OrderBy(c => int.TryParse(c.Codigo, out _) ? 0 : 1)
                .ThenBy(c => int.TryParse(c.Codigo, out var n) ? n : int.MaxValue)
                .ThenBy(c => c.Codigo)
                .ToList();

            var clienteItems = _clientesFactura
                .Select(c => new ClienteComboItem
                {
                    Codigo = c.Codigo,
                    Descripcion = c.Descripcion
                })
                .ToList();

            cmbCliente.DataSource = clienteItems;
            cmbCliente.DisplayMember = "Texto";
            cmbCliente.ValueMember = "Codigo";

            if (!soloLectura)
            {
                var ac = new AutoCompleteStringCollection();
                foreach (var c in _clientesFactura)
                {
                    if (!string.IsNullOrWhiteSpace(c.Codigo)) ac.Add(c.Codigo);
                    if (!string.IsNullOrWhiteSpace(c.Descripcion)) ac.Add(c.Descripcion);
                    if (!string.IsNullOrWhiteSpace(c.Codigo) || !string.IsNullOrWhiteSpace(c.Descripcion))
                        ac.Add((c.Codigo + " - " + c.Descripcion).Trim(' ', '-'));
                }
                cmbCliente.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                cmbCliente.AutoCompleteSource = AutoCompleteSource.CustomSource;
                cmbCliente.AutoCompleteCustomSource = ac;
            }

            panelIzq.Controls.Add(cmbCliente);

            // Botón para crear cliente rápido sin salir de la factura
            var btnNuevoCliente = new Button
            {
                Text = "+", Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Size = new Size(30, 24), Location = new Point(354, y),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 163, 127),
                ForeColor = Color.White, Cursor = Cursors.Hand
            };
            btnNuevoCliente.FlatAppearance.BorderSize = 0;
            new ToolTip().SetToolTip(btnNuevoCliente, "Crear cliente nuevo");
            btnNuevoCliente.Click += (s, e) => CrearClienteRapido();
            if (!soloLectura) panelIzq.Controls.Add(btnNuevoCliente);

            y += 32;

            Lbl("Fecha:", y);
            // Acotar el rango de fechas para evitar typos como "30/5/0117".
            // Min: 2000-01-01  |  Max: año actual + 1.
            int _anioMaxFct = DateTime.Today.Year + 1;
            dtpFecha = new DateTimePicker
            {
                Location = new Point(130, y), Size = new Size(220, 24),
                Format   = DateTimePickerFormat.Short,
                Enabled  = !soloLectura,
                MinDate  = new DateTime(2000, 1, 1),
                MaxDate  = new DateTime(_anioMaxFct, 12, 31),
                Value    = DateTime.Today
            };
            panelIzq.Controls.Add(dtpFecha); y += 32;

            Lbl("Forma Pago:", y);
            cmbFormaPago = new ComboBox
            {
                Location = new Point(130, y), Size = new Size(220, 24),
                DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !soloLectura
            };
            cmbFormaPago.Items.AddRange(new object[] { "Contado", "Credito", "Cheque", "Transferencia", "Mixto" });
            cmbFormaPago.SelectedIndex = 0;
            panelIzq.Controls.Add(cmbFormaPago);

            // Botón comprobantes — junto al combo FormaPago (mismo patrón que "+" cliente)
            _btnComprobantes = new Button
            {
                Text = "📎", Size = new Size(30, 24),
                Location = new Point(354, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 120, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _btnComprobantes.FlatAppearance.BorderSize = 0;
            new ToolTip().SetToolTip(_btnComprobantes,
                "Adjuntar comprobantes de pago (transferencias, Zelle, pago móvil)");
            _btnComprobantes.Click += (s, e) => AbrirComprobantes();
            panelIzq.Controls.Add(_btnComprobantes);

            cmbFormaPago.SelectedIndexChanged += (s, e) =>
            {
                bool esPago = cmbFormaPago.SelectedItem?.ToString() != "Contado";
                if (_btnComprobantes == null) return;
                // En modo lectura: mostrar solo si hay comprobantes
                // En modo edición: mostrar si forma de pago no es contado
                _btnComprobantes.Visible = _soloLectura
                    ? (_comprobantesTemp.Count > 0)
                    : esPago;
            };

            y += 32;

            Lbl("Descuento (%):", y);   numDescuento = Num(y, 100);       y += 32;
            _ivaPctSistema = decimal.TryParse(
                DatabaseService.ObtenerConfig("IVA_Porcentaje", "16"), out var _ipv) ? _ipv : 16m;
            Lbl("IVA %:", y);
            cmbIva = new ComboBox
            {
                Location = new Point(130, y), Size = new Size(220, 24),
                DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !soloLectura
            };
            cmbIva.Items.Add($"{_ivaPctSistema}%");
            cmbIva.Items.Add("0% (Nota de Entrega)");
            cmbIva.SelectedIndex = 0;
            panelIzq.Controls.Add(cmbIva);                                    y += 32;

            // ── EXPORTACIÓN (v22) ──────────────────────────────────────────
            // Checkbox que sólo importa cuando IVA = 0. Si se marca, el
            // documento será FacturaExportacion (legal, IVA 0%). Si NO se
            // marca y el IVA es 0, el sistema lo guardará como NotaEntrega
            // (sin valor fiscal).
            chkExportacion = new CheckBox
            {
                Text = "Es exportación (IVA 0% legal)",
                Location = new Point(130, y), Size = new Size(220, 22),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 110, 80),
                Enabled = !soloLectura
            };
            panelIzq.Controls.Add(chkExportacion); y += 26;

            // Destino exportación — visible sólo cuando chkExportacion = true
            lblDestinoExp = new Label
            {
                Text = "Destino exp.:", Location = new Point(10, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Visible = false
            };
            panelIzq.Controls.Add(lblDestinoExp);
            txtDestinoExp = new TextBox
            {
                Location = new Point(130, y), Size = new Size(220, 24),
                ReadOnly = soloLectura, Visible = false
            };
            panelIzq.Controls.Add(txtDestinoExp); y += 32;

            chkExportacion.CheckedChanged += (s, e) =>
            {
                lblDestinoExp.Visible = chkExportacion.Checked;
                txtDestinoExp.Visible = chkExportacion.Checked;
            };

            Lbl("Abono ($):", y);        numAbono     = Num(y, 9999999);   y += 32;
            Lbl("Total ($):", y);        numTotal     = Num(y, 9999999);   y += 32;
            Lbl("Dias credito:", y);
            numDias = new NumericUpDown
            {
                Location = new Point(130, y), Size = new Size(220, 24),
                Maximum = 365, DecimalPlaces = 0, Enabled = !soloLectura
            };
            panelIzq.Controls.Add(numDias); y += 32;
            Lbl("Transporte:", y);  txtTransporte = Txt(y); y += 32;
            Lbl("Obs:", y);
            txtObs = new TextBox
            {
                Location = new Point(130, y), Size = new Size(220, 40),
                Multiline = true, ReadOnly = soloLectura
            };
            panelIzq.Controls.Add(txtObs);

            // Banner informativo del tipo de documento — POR FUERA del
            // panelIzq para no chocar con los campos del formulario. Se
            // posiciona como una franja ancha bajo los dos paneles.
            // Se agregará a `this.Controls` MÁS ABAJO (después de panelIzq/
            // panelDer) para que quede al frente en z-order.
            lblTipoDoc = new Label
            {
                AutoSize = false, Size = new Size(870, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 230, 245),
                ForeColor = Color.FromArgb(40, 60, 140),
                Padding = new Padding(8, 0, 8, 0),
                Location = new Point(8, 510),   // justo debajo de los dos paneles
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            void RefrescarTipoDoc()
            {
                // En SOLO LECTURA usar el TipoDocumento REAL guardado de la factura
                // (el recálculo en vivo daría "NotaEntrega" porque las líneas aún no
                // están cargadas cuando corre la primera vez). En edición/nuevo sí se
                // clasifica en vivo según el IVA y la exportación actuales.
                string tipo;
                if (_soloLectura && f != null)
                    tipo = string.IsNullOrEmpty(f.TipoDocumento)
                         ? DatabaseService.ClasificarTipoDocumento(f.IvaF, f.EsExportacion)
                         : f.TipoDocumento;
                else
                    tipo = DatabaseService.ClasificarTipoDocumento(
                        _ivaCalculado, chkExportacion.Checked);
                switch (tipo)
                {
                    case "FacturaConIVA":
                        lblTipoDoc.Text = "✓ Factura con IVA — documento fiscal";
                        lblTipoDoc.BackColor = Color.FromArgb(220, 245, 230);
                        lblTipoDoc.ForeColor = Color.FromArgb(20, 110, 60);
                        break;
                    case "FacturaExportacion":
                        lblTipoDoc.Text = "✓ Factura de Exportación — IVA 0% legal";
                        lblTipoDoc.BackColor = Color.FromArgb(220, 235, 250);
                        lblTipoDoc.ForeColor = Color.FromArgb(40, 80, 160);
                        break;
                    case "NotaEntrega":
                        lblTipoDoc.Text = "⚠ NOTA DE ENTREGA — sin valor fiscal";
                        lblTipoDoc.BackColor = Color.FromArgb(250, 235, 215);
                        lblTipoDoc.ForeColor = Color.FromArgb(180, 100, 20);
                        break;
                }
            }
            cmbIva.SelectedIndexChanged += (s, e) => { RecalcularTotal(); RefrescarTipoDoc(); };
            chkExportacion.CheckedChanged += (s, e) => RefrescarTipoDoc();
            RefrescarTipoDoc();

            // ── Panel derecho: lineas de productos ────────────────────────
            var panelDer = new Panel
            {
                Location  = new Point(384, 42),
                Size      = new Size(498, 460),
                BackColor = Color.FromArgb(245, 248, 255)
            };

            var lblLineas = new Label
            {
                Text     = "Productos / Servicios",
                Location = new Point(8, 8), AutoSize = true,
                Font     = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor= Color.FromArgb(40, 60, 140)
            };

            // Toolbar de lineas
            var btnAgregar = new Button
            {
                Text = "+ Agregar producto", Location = new Point(8, 28), Size = new Size(150, 26),
                BackColor = Color.FromArgb(40, 140, 80), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Visible = !soloLectura
            };
            btnAgregar.FlatAppearance.BorderSize = 0;

            var btnQuitarLinea = new Button
            {
                Text = "Quitar", Location = new Point(168, 28), Size = new Size(70, 26),
                BackColor = Color.FromArgb(26, 74, 138), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Visible = !soloLectura
            };
            btnQuitarLinea.FlatAppearance.BorderSize = 0;

            // Grid de lineas
            dgvLineas = new DataGridView
            {
                Location  = new Point(8, 62),
                Size      = new Size(504, 250),
                ReadOnly  = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle     = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            dgvLineas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(26, 74, 138);
            dgvLineas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvLineas.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvLineas.EnableHeadersVisualStyles = false;

            // Columnas del grid de lineas
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Codigo", HeaderText = "Codigo", ReadOnly = true, Width = 80 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Descripcion", HeaderText = "Descripcion", ReadOnly = true });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Cantidad", HeaderText = "Cant.", Width = 70 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "PrecioUnit", HeaderText = "Precio", ReadOnly = false, Width = 80 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Subtotal", HeaderText = "Subtotal", ReadOnly = true, Width = 90 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Stock", HeaderText = "Stock", ReadOnly = true, Width = 60 });

            if (soloLectura)
                foreach (DataGridViewColumn col in dgvLineas.Columns)
                    col.ReadOnly = true;

            lblTotalLineas = new Label
            {
                Location  = new Point(8, 318),
                Size      = new Size(504, 24),
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 80, 150),
                TextAlign = ContentAlignment.MiddleRight
            };

            panelDer.Controls.AddRange(new Control[]
                { lblLineas, btnAgregar, btnQuitarLinea, dgvLineas, lblTotalLineas });

            // ── Botones inferiores ────────────────────────────────────────
            var panelBtns = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 52,
                BackColor = Color.FromArgb(220, 225, 245)
            };

            if (!soloLectura)
            {
                var bOk = new Button
                {
                    Text      = "Guardar Factura",
                    Location  = new Point(260, 10), Size = new Size(160, 34),
                    BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                };
                bOk.FlatAppearance.BorderSize = 0;
                bOk.Click += (s, e) => Guardar(f);
                panelBtns.Controls.Add(bOk);
            }

            var bCerrar = new Button
            {
                Text      = soloLectura ? "Cerrar" : "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location  = new Point(440, 10), Size = new Size(110, 34),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            bCerrar.FlatAppearance.BorderSize = 0;
            panelBtns.Controls.Add(bCerrar);

            
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { banner, panelIzq, panelDer, panelBtns });
            // El banner del tipo se agrega DESPUÉS para garantizar z-order correcto
            this.Controls.Add(lblTipoDoc);
            lblTipoDoc.BringToFront();

            // ── Cargar datos si es edicion/lectura ────────────────────────
            if (f != null)
            {
                txtNumero.Text             = f.NumeroF;
                if (!string.IsNullOrWhiteSpace(f.Codigo))
                    cmbCliente.SelectedValue = f.Codigo;
                // Si la fecha está corrupta (anulada por v21), usar HOY como
                // fallback en el picker para evitar excepciones del control.
                dtpFecha.Value = (f.Fecha < dtpFecha.MinDate || f.Fecha > dtpFecha.MaxDate)
                                    ? DateTime.Today : f.Fecha;
                if (cmbFormaPago.Items.Contains(f.FormaDePago))
                    cmbFormaPago.SelectedItem = f.FormaDePago;
                numDescuento.Value         = f.Descuento;
                cmbIva.SelectedIndex       = f.IvaF > 0 ? 0 : 1;
                numAbono.Value             = f.Abono < 0 ? 0 : f.Abono;
                numTotal.Value             = f.TotalF;
                numDias.Value              = f.DiasCreditoF;
                txtTransporte.Text         = f.Transporte;
                txtObs.Text                = f.Observaciones;
                txtNumero.ReadOnly         = true;
                // v22: cargar tipo de documento existente
                chkExportacion.Checked     = f.EsExportacion;
                txtDestinoExp.Text         = f.DestinoExportacion ?? "";
                lblDestinoExp.Visible      = f.EsExportacion;
                txtDestinoExp.Visible      = f.EsExportacion;

                // Cargar comprobantes existentes
                try
                {
                    _comprobantesTemp = DatabaseService.ObtenerComprobantesFactura(f.NumeroF);
                    ActualizarBotonComprobantes();
                }
                catch { /* tabla puede no existir aún */ }
            }

            // Cargar lineas existentes
            if (lineas != null)
                foreach (var l in lineas)
                    AgregarFilaLinea(l.CodigoItem, l.Descripcion, l.Cantidad, l.PrecioUnit);

            // ── Eventos de lineas ─────────────────────────────────────────
            btnAgregar.Click += (s, e) => AbrirSelectorProducto();

            btnQuitarLinea.Click += (s, e) =>
            {
                if (dgvLineas.CurrentRow != null)
                    dgvLineas.Rows.RemoveAt(dgvLineas.CurrentRow.Index);
                RecalcularTotal();
            };

            dgvLineas.CellEndEdit += (s, e) =>
            {
                var col = dgvLineas.Columns[e.ColumnIndex].Name;
                if (col == "Cantidad" || col == "PrecioUnit")
                    RecalcularFila(e.RowIndex);

                if (col == "Cantidad")
                {
                    var row = dgvLineas.Rows[e.RowIndex];
                    decimal cant = ParseDecimal(row.Cells["Cantidad"].Value?.ToString());
                    if (cant > 0)
                        BeginInvoke(new Action(AbrirSelectorProducto));
                }
            };

            dgvLineas.KeyDown += (s, e) =>
            {
                if (dgvLineas.CurrentCell == null) return;

                var colName = dgvLineas.Columns[dgvLineas.CurrentCell.ColumnIndex].Name;
                if (colName == "Cantidad" && (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab))
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    dgvLineas.EndEdit();
                }
            };

            numDescuento.ValueChanged += (s, e) => RecalcularTotal();
            // cmbIva ya tiene evento en RefrescarTipoDoc arriba

            RecalcularTotal();
        
            VentanaPrefs.Restaurar(this);
        }

        // ── Agregar producto al grid ───────────────────────────────────────
        private void AbrirSelectorProducto()
        {
            using var dlg = new DlgSelProducto();
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var inv = dlg.ProductoSeleccionado;
            if (inv == null) return;

            // Verificar si ya esta en la lista
            foreach (DataGridViewRow row in dgvLineas.Rows)
            {
                if (row.Cells["Codigo"].Value?.ToString() == inv.Codigo)
                {
                    decimal cantActual = ParseDecimal(row.Cells["Cantidad"].Value?.ToString());
                    row.Cells["Cantidad"].Value = cantActual + 1;
                    RecalcularFila(row.Index);
                    dgvLineas.CurrentCell = row.Cells["Cantidad"];
                    dgvLineas.BeginEdit(true);
                    return;
                }
            }

            int idx = AgregarFilaLinea(inv.Codigo, inv.Descripcion, 0, inv.Precio, inv.Existencia);
            RecalcularTotal();
            dgvLineas.CurrentCell = dgvLineas.Rows[idx].Cells["Cantidad"];
            dgvLineas.BeginEdit(true);
        }

        private int AgregarFilaLinea(string codigo, string desc, decimal cant,
                                       decimal precio, decimal stock = 0)
        {
            int idx = dgvLineas.Rows.Add();
            var row = dgvLineas.Rows[idx];
            row.Cells["Codigo"].Value      = codigo;
            row.Cells["Descripcion"].Value = desc;
            row.Cells["Cantidad"].Value    = cant;
            row.Cells["PrecioUnit"].Value  = precio.ToString("F2");
            row.Cells["Subtotal"].Value    = (cant * precio).ToString("F2");
            row.Cells["Stock"].Value       = stock;

            if (stock > 0 && cant > stock)
                row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);

            return idx;
        }

        private void RecalcularFila(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvLineas.Rows.Count) return;
            var row = dgvLineas.Rows[rowIndex];

            decimal cant = ParseDecimal(row.Cells["Cantidad"].Value?.ToString());
            decimal precio = ParseDecimal(row.Cells["PrecioUnit"].Value?.ToString());
            decimal stock = ParseDecimal(row.Cells["Stock"].Value?.ToString());

            row.Cells["PrecioUnit"].Value = precio.ToString("F2");
            row.Cells["Subtotal"].Value = (cant * precio).ToString("F2");

            row.DefaultCellStyle.BackColor = (stock > 0 && cant > stock)
                ? Color.FromArgb(255, 220, 220)
                : Color.White;

            RecalcularTotal();
        }

        private void RecalcularTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvLineas.Rows)
                total += ParseDecimal(row.Cells["Subtotal"].Value?.ToString());

            lblTotalLineas.Text = "Subtotal lineas: " + total.ToString("C2");

            decimal desc = numDescuento?.Value ?? 0;
            decimal pctIva = (cmbIva?.SelectedIndex == 0) ? _ivaPctSistema : 0m;
            decimal baseIva = total * (1 - desc / 100m);
            _ivaCalculado = baseIva * (pctIva / 100m);
            decimal totalConDesc = baseIva + _ivaCalculado;
            if (numTotal != null)
                numTotal.Value = Math.Min(totalConDesc, numTotal.Maximum);
        }

        private void Guardar(Factura fExistente)
        {
            if (string.IsNullOrWhiteSpace(txtNumero.Text))
            {
                MessageBox.Show("Indica el numero de factura.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string codigoCliente = ObtenerCodigoClienteSeleccionado();
            if (string.IsNullOrWhiteSpace(codigoCliente))
            {
                MessageBox.Show("Selecciona un cliente valido.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (dgvLineas.Rows.Count == 0)
            {
                MessageBox.Show("Agrega al menos un producto.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var lineas = new List<LineaFactura>();
            foreach (DataGridViewRow row in dgvLineas.Rows)
            {
                var cod = row.Cells["Codigo"].Value?.ToString();
                if (string.IsNullOrWhiteSpace(cod)) continue;

                decimal cant = ParseDecimal(row.Cells["Cantidad"].Value?.ToString());
                decimal precio = ParseDecimal(row.Cells["PrecioUnit"].Value?.ToString());
                decimal stock = ParseDecimal(row.Cells["Stock"].Value?.ToString());

                if (cant <= 0)
                {
                    MessageBox.Show("La cantidad debe ser mayor que cero para todos los productos.",
                        "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (precio <= 0)
                {
                    MessageBox.Show("El precio unitario debe ser mayor que cero para todos los productos.",
                        "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (stock > 0 && cant > stock)
                {
                    MessageBox.Show("Stock insuficiente para: " + cod, "Validacion",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                lineas.Add(new LineaFactura
                {
                    CodigoItem = cod,
                    Descripcion = row.Cells["Descripcion"].Value?.ToString() ?? "",
                    Cantidad = cant,
                    PrecioUnit = precio,
                    Descuento = 0
                });
            }

            if (lineas.Count == 0)
            {
                MessageBox.Show("No hay lineas validas para guardar.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validación de fecha — defensa en profundidad además del MinDate/MaxDate
            // del DateTimePicker (por si el control se cambia en el futuro).
            int anioMaxV = DateTime.Today.Year + 1;
            if (dtpFecha.Value.Year < 2000 || dtpFecha.Value.Year > anioMaxV)
            {
                MessageBox.Show(this,
                    $"La fecha debe estar entre 2000 y {anioMaxV}.\n\n" +
                    "Verifica que el año esté bien tipeado (errores comunes: " +
                    "tipear '0117' en vez de '2017').",
                    "Fecha inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpFecha.Focus();
                return;
            }

            // ── Validación de exportación ────────────────────────────────
            // Si marcó exportación pero IVA > 0 → no tiene sentido.
            if (chkExportacion.Checked && cmbIva.SelectedIndex == 0)
            {
                MessageBox.Show(this,
                    "Una factura de exportación tiene IVA 0% por ley.\n" +
                    "Selecciona 0% o desmarca la exportación.",
                    "Inconsistencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbIva.Focus();
                return;
            }
            // Si marcó exportación, exigir destino
            if (chkExportacion.Checked &&
                string.IsNullOrWhiteSpace(txtDestinoExp.Text))
            {
                MessageBox.Show(this,
                    "Indica el país/destino de la exportación.",
                    "Falta destino", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDestinoExp.Focus();
                return;
            }

            // ── Si va a guardarse como NotaEntrega, confirmar ────────────
            string tipoFinal = DatabaseService.ClasificarTipoDocumento(
                _ivaCalculado, chkExportacion.Checked);
            if (tipoFinal == "NotaEntrega")
            {
                if (!Sesion.EsAdmin && !Sesion.TienePermiso("NotasEntregaCrear"))
                {
                    MessageBox.Show(this,
                        "No tienes permiso para emitir Notas de Entrega.\n\n" +
                        "Una venta sin IVA y sin marcar como exportación se " +
                        "registra automáticamente como Nota de Entrega (sin " +
                        "valor fiscal). Si esto NO es lo que querías, agrega " +
                        "el IVA correspondiente o marca la casilla de exportación.",
                        "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
                var resp = MessageBox.Show(this,
                    "Esta venta se registrará como NOTA DE ENTREGA " +
                    "(sin valor fiscal, sin IVA, no se declara).\n\n" +
                    "El número será asignado automáticamente con el formato " +
                    "NE-" + DateTime.Today.Year + "-XXXX.\n\n" +
                    "¿Confirmas?",
                    "Nota de Entrega",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (resp != DialogResult.Yes) return;
            }

            // ── Vista previa antes de guardar ────────────────────────
            decimal pctIva = (cmbIva?.SelectedIndex == 0) ? _ivaPctSistema : 0m;
            string nombreCli = cmbCliente.Text;
            string formaPago = cmbFormaPago.SelectedItem?.ToString() ?? "Contado";
            decimal subTotal = lineas.Sum(l => l.Cantidad * l.PrecioUnit);
            decimal descMonto = subTotal * (numDescuento.Value / 100m);
            decimal baseImponible = subTotal - descMonto;

            string resumen = $"═══ VISTA PREVIA ═══\n\n" +
                $"Tipo: {tipoFinal}\n" +
                $"Cliente: {nombreCli}\n" +
                $"Fecha: {dtpFecha.Value:dd/MM/yyyy}\n" +
                $"Forma de pago: {formaPago}\n" +
                $"IVA: {pctIva}%\n\n" +
                $"── Artículos ({lineas.Count}) ──\n";
            foreach (var l in lineas)
                resumen += $"  {l.Descripcion}  ×{l.Cantidad}  @{l.PrecioUnit:C2}  = {(l.Cantidad * l.PrecioUnit):C2}\n";
            resumen += $"\n── Totales ──\n" +
                $"  Subtotal:       {subTotal:C2}\n" +
                $"  Descuento ({numDescuento.Value}%): -{descMonto:C2}\n" +
                $"  IVA ({pctIva}%):      {_ivaCalculado:C2}\n" +
                $"  ─────────────────\n" +
                $"  TOTAL:          {numTotal.Value:C2}\n";
            if (_comprobantesTemp.Count > 0)
            {
                decimal totalComp = _comprobantesTemp.Sum(c => c.Monto);
                resumen += $"\n── Comprobantes ({_comprobantesTemp.Count}) ──\n";
                foreach (var c in _comprobantesTemp)
                    resumen += $"  {c.MetodoPago}  {c.Monto:C2}  Ref: {c.Referencia}\n";
                resumen += $"  Total comprobantes: {totalComp:C2}\n";
            }
            resumen += "\n¿Confirmar y guardar?";

            var confirmar = MessageBox.Show(this, resumen,
                "Confirmar factura", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmar != DialogResult.Yes) return;

            Resultado = new Factura
            {
                NumeroF = txtNumero.Text.Trim(),
                Codigo = codigoCliente,
                Fecha = dtpFecha.Value.Date,
                Descuento = numDescuento.Value,
                Transporte = txtTransporte.Text?.Trim() ?? "",
                Observaciones = txtObs.Text?.Trim() ?? "",
                FormaDePago = formaPago,
                Abono = numAbono.Value,
                IvaF = _ivaCalculado,
                DiasCreditoF = (int)numDias.Value,
                EsExportacion      = chkExportacion.Checked,
                DestinoExportacion = chkExportacion.Checked
                                        ? txtDestinoExp.Text?.Trim() : null,
                TotalF = numTotal.Value
            };

            Lineas = lineas;
            DialogResult = DialogResult.OK;
            Close();
        }

        private decimal ParseDecimal(string txt)
        {
            if (string.IsNullOrWhiteSpace(txt)) return 0m;

            txt = txt.Trim();
            if (decimal.TryParse(txt, out var v1)) return v1;
            if (decimal.TryParse(txt, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v2)) return v2;

            txt = txt.Replace("$", "").Replace("€", "").Replace("Bs", "").Trim();
            if (decimal.TryParse(txt, out var v3)) return v3;
            if (decimal.TryParse(txt, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v4)) return v4;

            return 0m;
        }

        private string ObtenerCodigoClienteSeleccionado()
        {
            if (cmbCliente.SelectedValue != null)
            {
                var code = cmbCliente.SelectedValue.ToString();
                if (!string.IsNullOrWhiteSpace(code)) return code;
            }

            var texto = (cmbCliente.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(texto)) return "";

            var exactCode = _clientesFactura.FirstOrDefault(c =>
                string.Equals(c.Codigo, texto, StringComparison.OrdinalIgnoreCase));
            if (exactCode != null) return exactCode.Codigo;

            var exactDesc = _clientesFactura.FirstOrDefault(c =>
                string.Equals(c.Descripcion ?? "", texto, StringComparison.OrdinalIgnoreCase));
            if (exactDesc != null) return exactDesc.Codigo;

            var prefix = _clientesFactura.FirstOrDefault(c =>
                string.Equals((c.Codigo + " - " + (c.Descripcion ?? "")).Trim(),
                    texto, StringComparison.OrdinalIgnoreCase));
            if (prefix != null) return prefix.Codigo;

            return "";
        }

        private void CrearClienteRapido()
        {
            // Auto-generar siguiente código numérico
            int maxCod = _clientesFactura
                .Select(c => int.TryParse(c.Codigo, out var n) ? n : 0)
                .DefaultIfEmpty(0).Max();
            string codigoSugerido = (maxCod + 1).ToString();

            using var dlg = new Form
            {
                Text = "Cliente Rápido",
                Size = new Size(400, 310),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                Font = new Font("Segoe UI", 9.5f)
            };

            int cy = 15;
            Label MkLbl(string t) => new Label { Text = t, AutoSize = true, Location = new Point(12, cy + 3) };
            TextBox MkTxt(string val = "") => new TextBox
            {
                Location = new Point(130, cy), Size = new Size(240, 24), Text = val
            };

            dlg.Controls.Add(MkLbl("Código:")); var txtCod  = MkTxt(codigoSugerido); dlg.Controls.Add(txtCod);  cy += 34;
            dlg.Controls.Add(MkLbl("Nombre:"));  var txtNom  = MkTxt();               dlg.Controls.Add(txtNom);  cy += 34;
            dlg.Controls.Add(MkLbl("CI/RIF:"));  var txtRif  = MkTxt();               dlg.Controls.Add(txtRif);  cy += 34;
            dlg.Controls.Add(MkLbl("Teléfono:")); var txtTel  = MkTxt();              dlg.Controls.Add(txtTel);  cy += 34;
            dlg.Controls.Add(MkLbl("Dirección:")); var txtDir = MkTxt();              dlg.Controls.Add(txtDir);  cy += 42;

            var btnGuardar = new Button
            {
                Text = "Guardar", DialogResult = DialogResult.None,
                Location = new Point(130, cy), Size = new Size(110, 28),
                BackColor = Color.FromArgb(16, 163, 127), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnGuardar.FlatAppearance.BorderSize = 0;

            var btnCancelar = new Button
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(250, cy), Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat
            };

            btnGuardar.Click += (s, e) =>
            {
                var cod  = txtCod.Text.Trim();
                var nom  = txtNom.Text.Trim();
                if (string.IsNullOrWhiteSpace(cod) || string.IsNullOrWhiteSpace(nom))
                {
                    MessageBox.Show("Código y nombre son obligatorios.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    DatabaseService.InsertarCliente(new Cliente
                    {
                        Codigo       = cod,
                        Descripcion  = nom,
                        CiRif        = txtRif.Text.Trim(),
                        Telefono     = txtTel.Text.Trim(),
                        Direccion    = txtDir.Text.Trim(),
                        Fecha        = DateTime.Today
                    });
                    dlg.Tag = cod;
                    dlg.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al guardar: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.AddRange(new Control[] { btnGuardar, btnCancelar });
            dlg.AcceptButton = btnGuardar;
            dlg.CancelButton = btnCancelar;
            txtNom.Focus();

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var nuevoCodigo = dlg.Tag?.ToString() ?? "";

            // Refrescar lista de clientes
            _clientesFactura = DatabaseService.ObtenerClientes()
                .OrderBy(c => int.TryParse(c.Codigo, out _) ? 0 : 1)
                .ThenBy(c => int.TryParse(c.Codigo, out var n) ? n : int.MaxValue)
                .ThenBy(c => c.Codigo)
                .ToList();

            var clienteItems = _clientesFactura
                .Select(c => new ClienteComboItem { Codigo = c.Codigo, Descripcion = c.Descripcion })
                .ToList();

            cmbCliente.DataSource = null;
            cmbCliente.DataSource = clienteItems;
            cmbCliente.DisplayMember = "Texto";
            cmbCliente.ValueMember = "Codigo";

            // Actualizar autocompletar
            var ac = new AutoCompleteStringCollection();
            foreach (var c in _clientesFactura)
            {
                if (!string.IsNullOrWhiteSpace(c.Codigo)) ac.Add(c.Codigo);
                if (!string.IsNullOrWhiteSpace(c.Descripcion)) ac.Add(c.Descripcion);
                if (!string.IsNullOrWhiteSpace(c.Codigo) || !string.IsNullOrWhiteSpace(c.Descripcion))
                    ac.Add((c.Codigo + " - " + c.Descripcion).Trim(' ', '-'));
            }
            cmbCliente.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbCliente.AutoCompleteSource = AutoCompleteSource.CustomSource;
            cmbCliente.AutoCompleteCustomSource = ac;

            // Seleccionar el nuevo cliente en el combo
            if (!string.IsNullOrWhiteSpace(nuevoCodigo))
                cmbCliente.SelectedValue = nuevoCodigo;
        }

        // ── Helpers comprobantes ──────────────────────────────────────────

        private void ActualizarBotonComprobantes()
        {
            if (_btnComprobantes == null) return;
            bool tiene = _comprobantesTemp.Count > 0;
            _btnComprobantes.Text = tiene ? $"📎{_comprobantesTemp.Count}" : "📎";
            _btnComprobantes.BackColor = tiene
                ? Color.FromArgb(16, 163, 127)   // verde = tiene datos
                : Color.FromArgb(30, 120, 180);  // azul = vacío

            // Actualizar visibilidad (cubre caso soloLectura
            // donde comprobantes se cargan después del evento FormaPago)
            bool esPago = cmbFormaPago.SelectedItem?.ToString() != "Contado";
            _btnComprobantes.Visible = _soloLectura
                ? tiene
                : esPago;
        }

        private void AbrirComprobantes()
        {
            using var dlg = new DlgComprobantesFactura(_comprobantesTemp, _rutasImagenOrigen, _soloLectura);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _comprobantesTemp  = dlg.ComprobantesResultado;
                _rutasImagenOrigen = dlg.RutasImagenResultado;
                ActualizarBotonComprobantes();
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO GESTIONAR COMPROBANTES DE FACTURA
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgComprobantesFactura : FormBase
    {
        public List<ComprobanteFactura> ComprobantesResultado { get; private set; } = new();
        public List<string> RutasImagenResultado { get; private set; } = new();

        private DataGridView dgv;
        private List<ComprobanteFactura> _items;
        private List<string> _rutas;
        private readonly bool _soloLectura;

        public DlgComprobantesFactura(
            List<ComprobanteFactura> actuales,
            List<string> rutasOrigen,
            bool soloLectura)
        {
            _soloLectura = soloLectura;
            _items = actuales.Select(c => new ComprobanteFactura
            {
                Id = c.Id, NumeroF = c.NumeroF, MetodoPago = c.MetodoPago,
                Monto = c.Monto, Referencia = c.Referencia, RutaImagen = c.RutaImagen,
                IdCuentaReceptora = c.IdCuentaReceptora, Fecha = c.Fecha,
                Observaciones = c.Observaciones
            }).ToList();
            _rutas = new List<string>(rutasOrigen);

            Text = "Comprobantes de Pago";
            Size = new Size(700, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            // ── Banner ──
            var banner = new Panel { Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(30, 120, 180) };
            banner.Controls.Add(new Label
            {
                Text = "Comprobantes de Pago", ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 10)
            });

            // ── Grid ──
            dgv = new DataGridView
            {
                Location = new Point(8, 50), Size = new Size(668, 240),
                ReadOnly = true, AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 28 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 120, 180);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Metodo", HeaderText = "Método", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Monto", HeaderText = "Monto", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Referencia", HeaderText = "Referencia", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cuenta", HeaderText = "Cuenta", FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Imagen", HeaderText = "Comprobante", FillWeight = 15 });

            CargarGrid();

            dgv.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _items.Count) return;
                var cf = _items[e.RowIndex];
                string ruta = e.RowIndex < _rutas.Count ? _rutas[e.RowIndex] : cf.RutaImagen;
                if (!string.IsNullOrWhiteSpace(ruta) && System.IO.File.Exists(ruta))
                {
                    try { System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(ruta) { UseShellExecute = true }); }
                    catch { }
                }
            };

            // ── Botones ──
            int by = 300;
            if (!soloLectura)
            {
                var btnAgregar = new Button
                {
                    Text = "+ Agregar", Location = new Point(8, by), Size = new Size(120, 30),
                    BackColor = Color.FromArgb(40, 140, 80), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnAgregar.FlatAppearance.BorderSize = 0;
                btnAgregar.Click += (s, e) => AgregarComprobante();
                Controls.Add(btnAgregar);

                var btnQuitar = new Button
                {
                    Text = "Quitar", Location = new Point(136, by), Size = new Size(90, 30),
                    BackColor = Color.FromArgb(180, 40, 40), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnQuitar.FlatAppearance.BorderSize = 0;
                btnQuitar.Click += (s, e) =>
                {
                    if (dgv.CurrentRow == null) return;
                    int idx = dgv.CurrentRow.Index;
                    if (idx >= 0 && idx < _items.Count)
                    {
                        _items.RemoveAt(idx);
                        if (idx < _rutas.Count) _rutas.RemoveAt(idx);
                        CargarGrid();
                    }
                };
                Controls.Add(btnQuitar);
            }

            // ── Footer ──
            var btnOk = new Button
            {
                Text = soloLectura ? "Cerrar" : "Aceptar",
                Location = new Point(460, 340), Size = new Size(100, 32),
                BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (s, e) =>
            {
                ComprobantesResultado = _items;
                RutasImagenResultado = _rutas;
                DialogResult = soloLectura ? DialogResult.Cancel : DialogResult.OK;
                Close();
            };

            var btnCancelar = new Button
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(570, 340), Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat, Visible = !soloLectura
            };

            Controls.AddRange(new Control[] { banner, dgv, btnOk, btnCancelar });

            // Total label
            var lblTotal = new Label
            {
                Location = new Point(240, by + 6), AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 80, 150)
            };
            void RefrescarTotal() => lblTotal.Text =
                $"Total: {_items.Sum(c => c.Monto):C2}  ({_items.Count} comprobante{(_items.Count != 1 ? "s" : "")})";
            RefrescarTotal();
            dgv.RowsAdded += (s, e) => RefrescarTotal();
            dgv.RowsRemoved += (s, e) => RefrescarTotal();
            Controls.Add(lblTotal);
        }

        private void CargarGrid()
        {
            var cuentas = DatabaseService.ObtenerCuentasReceptoras(soloActivas: false);
            dgv.Rows.Clear();
            foreach (var c in _items)
            {
                string nombreCta = cuentas.FirstOrDefault(ct => ct.Id == c.IdCuentaReceptora)?.Nombre ?? "";
                bool tieneImg = !string.IsNullOrWhiteSpace(c.RutaImagen);
                dgv.Rows.Add(c.MetodoPago, c.Monto.ToString("C2"), c.Referencia,
                    nombreCta, tieneImg ? "Ver" : "—");
            }
        }

        private void AgregarComprobante()
        {
            var cuentas = DatabaseService.ObtenerCuentasReceptoras();

            using var dlg = new Form
            {
                Text = "Nuevo Comprobante",
                Size = new Size(460, 440),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                BackColor = Color.FromArgb(240, 244, 250),
                Font = new Font("Segoe UI", 9.5f)
            };

            int y = 15;
            Label MkL(string t)
            {
                var l = new Label { Text = t, AutoSize = true,
                    Location = new Point(12, y + 3),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
                dlg.Controls.Add(l);
                return l;
            }

            MkL("Método:");
            var cmbMetodo = new ComboBox
            {
                Location = new Point(140, y), Size = new Size(280, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMetodo.Items.AddRange(new object[] {
                "Zelle", "Transferencia", "PagoMovil", "Efectivo",
                "Cheque", "Deposito", "Otro" });
            cmbMetodo.SelectedIndex = 0;
            dlg.Controls.Add(cmbMetodo); y += 34;

            MkL("Monto:");
            var numMonto = new NumericUpDown
            {
                Location = new Point(140, y), Size = new Size(280, 24),
                Maximum = 9999999, DecimalPlaces = 2
            };
            dlg.Controls.Add(numMonto); y += 34;

            MkL("Referencia:");
            var txtRef = new TextBox { Location = new Point(140, y), Size = new Size(280, 24) };
            dlg.Controls.Add(txtRef); y += 34;

            MkL("Cuenta:");
            var cmbCuenta = new ComboBox
            {
                Location = new Point(140, y), Size = new Size(280, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCuenta.Items.Add("(ninguna)");
            foreach (var ct in cuentas) cmbCuenta.Items.Add(ct.Nombre);
            cmbCuenta.SelectedIndex = 0;
            dlg.Controls.Add(cmbCuenta); y += 34;

            MkL("Observaciones:");
            var txtObs = new TextBox
            {
                Location = new Point(140, y), Size = new Size(280, 40), Multiline = true
            };
            dlg.Controls.Add(txtObs); y += 50;

            MkL("Comprobante:");
            string rutaImagen = "";
            var pic = new PictureBox
            {
                Location = new Point(140, y), Size = new Size(160, 100),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            var btnImg = new Button
            {
                Text = "Seleccionar...", Location = new Point(310, y),
                Size = new Size(110, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 130, 190),
                ForeColor = Color.White
            };
            btnImg.FlatAppearance.BorderSize = 0;
            btnImg.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Seleccionar comprobante de pago",
                    Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Todos|*.*"
                };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    rutaImagen = ofd.FileName;
                    try { pic.Image = Image.FromFile(rutaImagen); }
                    catch { pic.Image = null; }
                }
            };
            dlg.Controls.AddRange(new Control[] { pic, btnImg }); y += 110;

            var btnGuardar = new Button
            {
                Text = "Agregar", Location = new Point(140, y), Size = new Size(120, 32),
                BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(270, y), Size = new Size(100, 32),
                FlatStyle = FlatStyle.Flat
            };

            btnGuardar.Click += (s, e) =>
            {
                if (numMonto.Value <= 0)
                {
                    MessageBox.Show("El monto debe ser mayor a cero.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string metodo = cmbMetodo.SelectedItem?.ToString() ?? "Efectivo";
                if (metodo != "Efectivo" && string.IsNullOrWhiteSpace(rutaImagen))
                {
                    var resp = MessageBox.Show(
                        "No ha adjuntado comprobante. Para pagos electrónicos " +
                        "se recomienda adjuntar imagen.\n\n¿Continuar sin comprobante?",
                        "Sin comprobante", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (resp != DialogResult.Yes) return;
                }

                int idCuenta = 0;
                if (cmbCuenta.SelectedIndex > 0)
                    idCuenta = cuentas[cmbCuenta.SelectedIndex - 1].Id;

                dlg.Tag = new ComprobanteFactura
                {
                    MetodoPago = metodo,
                    Monto = numMonto.Value,
                    Referencia = txtRef.Text.Trim(),
                    IdCuentaReceptora = idCuenta,
                    Fecha = DateTime.Today,
                    Observaciones = txtObs.Text.Trim(),
                    RutaImagen = rutaImagen
                };
                dlg.DialogResult = DialogResult.OK;
            };

            dlg.Controls.AddRange(new Control[] { btnGuardar, btnCancel });
            dlg.AcceptButton = btnGuardar;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Tag is ComprobanteFactura cf)
            {
                _items.Add(cf);
                _rutas.Add(cf.RutaImagen ?? "");
                CargarGrid();
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO SELECCIONAR PRODUCTO
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgSelProducto : FormBase
    {
        public Inventario ProductoSeleccionado { get; private set; }

        private TextBox txtBuscar;
        private DataGridView dgv;
        private List<Inventario> _inventario = new List<Inventario>();

        public DlgSelProducto()
        {
            Text = "Seleccionar producto";
            Size = new Size(760, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            var lbl = new Label
            {
                Text = "Buscar (codigo o descripcion):",
                AutoSize = true,
                Location = new Point(12, 16),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            txtBuscar = new TextBox { Location = new Point(220, 12), Size = new Size(520, 24) };

            dgv = new DataGridView
            {
                Location = new Point(12, 46),
                Size = new Size(728, 366),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f)
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo", HeaderText = "Codigo", Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Descripcion", HeaderText = "Descripcion" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Precio", HeaderText = "Precio", Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Stock", HeaderText = "Stock", Width = 90 });

            var btnSel = new Button
            {
                Text = "Seleccionar",
                Location = new Point(470, 420),
                Size = new Size(130, 32),
                BackColor = Color.FromArgb(40, 120, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSel.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(610, 420),
                Size = new Size(130, 32),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.FromArgb(120, 120, 130),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { lbl, txtBuscar, dgv, btnSel, btnCancel });

            _inventario = DatabaseService.ObtenerInventario()
                .OrderBy(i => int.TryParse(i.Codigo, out _) ? 0 : 1)
                .ThenBy(i => int.TryParse(i.Codigo, out var n) ? n : int.MaxValue)
                .ThenBy(i => i.Codigo)
                .ToList();

            var ac = new AutoCompleteStringCollection();
            foreach (var p in _inventario)
            {
                if (!string.IsNullOrWhiteSpace(p.Codigo)) ac.Add(p.Codigo);
                if (!string.IsNullOrWhiteSpace(p.Descripcion)) ac.Add(p.Descripcion);
                ac.Add(((p.Codigo ?? "") + " - " + (p.Descripcion ?? "")).Trim(' ', '-'));
            }
            txtBuscar.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtBuscar.AutoCompleteSource = AutoCompleteSource.CustomSource;
            txtBuscar.AutoCompleteCustomSource = ac;

            txtBuscar.TextChanged += (s, e) => Filtrar();
            dgv.DoubleClick += (s, e) => Seleccionar();
            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    Seleccionar();
                }
            };
            btnSel.Click += (s, e) => Seleccionar();

            Filtrar();
        
            VentanaPrefs.Restaurar(this);
        }

        private void Filtrar()
        {
            string q = (txtBuscar.Text ?? "").Trim().ToLowerInvariant();

            var data = string.IsNullOrWhiteSpace(q)
                ? _inventario
                : _inventario.Where(i => BusquedaHelper.ContieneFlexibleEnAlguno(q,
                        i.Codigo, i.Descripcion))
                  .ToList();

            dgv.Rows.Clear();
            foreach (var i in data)
                dgv.Rows.Add(i.Codigo, i.Descripcion, i.Precio.ToString("F2"), i.Existencia.ToString("N2"));

            if (dgv.Rows.Count > 0)
                dgv.Rows[0].Selected = true;
        }

        private void Seleccionar()
        {
            if (dgv.CurrentRow == null) return;
            var codigo = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(codigo)) return;

            ProductoSeleccionado = _inventario.FirstOrDefault(i => i.Codigo == codigo);
            if (ProductoSeleccionado == null) return;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
