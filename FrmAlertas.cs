using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmAlertas : FormBase
    {
        private readonly AlertasData _data;
        private TabControl tabControl;
        private Button btnCerrar;

        // ── Silenciado de alertas ────────────────────────────────────────
        // Se guarda en un archivo txt con la fecha de hoy en el nombre.
        // Al dia siguiente el archivo tiene otro nombre y las alertas vuelven.

        private static string RutaSilenciados =>
            Path.Combine(SecurityService.CarpetaDatos,
                $"alertas_silenciadas_{DateTime.Today:yyyyMMdd}.txt");

        // NO es static — cada instancia del formulario tiene su propio set en memoria
        // El archivo persiste en disco para que sobreviva entre sesiones del mismo dia
        private HashSet<string> _silenciadas;

        private HashSet<string> CargarSilenciadas()
        {
            if (_silenciadas != null) return _silenciadas;
            _silenciadas = new HashSet<string>();
            try
            {
                if (File.Exists(RutaSilenciados))
                    foreach (var linea in File.ReadAllLines(RutaSilenciados))
                        if (!string.IsNullOrWhiteSpace(linea))
                            _silenciadas.Add(linea.Trim());
            }
            catch { }
            return _silenciadas;
        }

        private void SilenciarAlerta(string clave)
        {
            try
            {
                // Leer el archivo fresco antes de escribir para no perder otras claves
                var enDisco = new HashSet<string>();
                if (File.Exists(RutaSilenciados))
                    foreach (var linea in File.ReadAllLines(RutaSilenciados))
                        if (!string.IsNullOrWhiteSpace(linea))
                            enDisco.Add(linea.Trim());

                enDisco.Add(clave);
                File.WriteAllLines(RutaSilenciados, enDisco);

                // Actualizar cache de instancia
                CargarSilenciadas().Add(clave);
            }
            catch { }
        }

        private bool EstaSilenciada(string clave) =>
            CargarSilenciadas().Contains(clave);

        // Claves de cada categoria
        private const string CLAVE_STOCK     = "stock_bajo";
        private const string CLAVE_CXC       = "cxc_vencidas";
        private const string CLAVE_CXC_PROX  = "cxc_por_vencer";
        private const string CLAVE_MANT      = "mantenimiento";
        private const string CLAVE_PRECIO    = "precios_pendientes";

        // ── Constructor ─────────────────────────────────────────────────

        public FrmAlertas(AlertasData data)
        {
            _data = data;
            InitializeComponent();
            CargarTabs();
        }

        private void InitializeComponent()
        {
            this.Text = "Alertas del Sistema";
            this.Size = new Size(880, 600);
            this.MinimumSize = new Size(720, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;  // permite VentanaPrefs auto
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.BackColor = UITheme.Fondo;
            this.Font = UITheme.Body;

            // ── Colores unificados con MenuPrincipalV2 ────────────────
            Color cAzulOsc = Color.FromArgb(15, 45, 85);
            Color cBarra   = Color.FromArgb(15, 20, 30);
            Color cCyan    = Color.FromArgb(0, 180, 216);
            Color cWarn    = Color.FromArgb(210, 153, 34);

            // --- Header (oscuro, consistente con MenuPrincipalV2) ---
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 64,
                BackColor = cAzulOsc
            };
            pnlHeader.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var fIco = new Font("Segoe UI", 22f);
                g.DrawString("⚠", fIco, new SolidBrush(cWarn), 16, 14);
                using var fTit = new Font("Segoe UI", 14f, FontStyle.Bold);
                using var brTit = new SolidBrush(UITheme.Texto);
                g.DrawString("Alertas Pendientes", fTit, brTit, 56, 12);
                using var fSub = new Font("Segoe UI", 9.5f);
                using var brSub = new SolidBrush(UITheme.TextoTenue);
                g.DrawString($"Revisa los items que requieren atención — {DateTime.Now:dd/MM/yyyy}",
                    fSub, brSub, 58, 36);
            };

            // --- Badges resumen ---
            var pnlBadges = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = UITheme.FondoCard,
                Padding = new Padding(12, 10, 12, 10)
            };
            pnlBadges.Paint += PnlBadges_Paint;

            // --- TabControl ---
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = UITheme.Body,
                Padding = new Point(14, 6)
            };

            // --- Footer CLARO con buen contraste (estilo unificado) ---
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 52,
                BackColor = UITheme.FondoCard
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Borde, 1f);
                e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            var lblNota = new Label
            {
                Text = "  Tip: \"Ignorar hoy\" oculta la alerta hasta mañana.",
                AutoSize = false, Dock = DockStyle.Fill,
                ForeColor = UITheme.TextoTenue,
                BackColor = UITheme.FondoCard,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };

            // Ignorar = botón secundario (ghost gris)
            var btnIgnorar = new Button
            {
                Text = "Ignorar esta alerta hoy",
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.FondoCard,
                ForeColor = UITheme.TextoTenue,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Width = 200, Height = 36
            };
            btnIgnorar.FlatAppearance.BorderSize = 1;
            btnIgnorar.FlatAppearance.BorderColor = UITheme.Borde;
            btnIgnorar.FlatAppearance.MouseOverBackColor = UITheme.Borde;
            btnIgnorar.Click += BtnIgnorar_Click;

            // Cerrar = botón primario azul (acción principal)
            btnCerrar = new Button
            {
                Text = "Entendido — Cerrar",
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Azul,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Width = 180, Height = 36,
                DialogResult = DialogResult.OK
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.FlatAppearance.MouseOverBackColor = UITheme.AzulHover;

            // Wrap derecho con FlowLayoutPanel (RightToLeft: el primero queda más a la derecha)
            var pnlR = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, Width = 410,
                BackColor = UITheme.FondoCard,
                FlowDirection = FlowDirection.RightToLeft, WrapContents = false,
                Padding = new Padding(0, 7, 16, 7)
            };
            btnCerrar.Margin   = new Padding(0, 0, 0, 0);
            btnIgnorar.Margin  = new Padding(0, 0, 8, 0);
            pnlR.Controls.Add(btnCerrar);
            pnlR.Controls.Add(btnIgnorar);

            // Orden importante: Dock=Fill primero, Dock=Right al final
            pnlFooter.Controls.Add(lblNota);
            pnlFooter.Controls.Add(pnlR);

            this.Controls.Add(tabControl);
            this.Controls.Add(pnlBadges);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
            this.AcceptButton = btnCerrar;
        }

        // ── Ignorar tab actual ──────────────────────────────────────────

        private void BtnIgnorar_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null) return;

            string clave = tabControl.SelectedIndex switch
            {
                0 => CLAVE_STOCK,
                1 => CLAVE_CXC,
                2 => CLAVE_CXC_PROX,
                3 => CLAVE_MANT,
                4 => CLAVE_PRECIO,
                _ => null
            };
            if (clave == null) return;
            if (EstaSilenciada(clave))
            {
                MessageBox.Show("Esta alerta ya fue ignorada hoy.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string nombre = tabControl.SelectedTab.Text.Split('(')[0].Trim();
            if (MessageBox.Show(
                $"Ignorar la alerta \"{nombre}\" por hoy?\n\nVolvera a aparecer manana si sigue pendiente.",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            SilenciarAlerta(clave);

            // Marcar tab visualmente
            tabControl.SelectedTab.Text += " ✓";
            tabControl.SelectedTab.BackColor = Color.FromArgb(240, 245, 240);

            // Recargar el grid del tab con el mensaje de silenciada
            tabControl.SelectedTab.Controls.Clear();
            var dgv = CrearDGV();
            MostrarMensajeSilenciada(dgv, $"{nombre} ignorada hasta manana");
            tabControl.SelectedTab.Controls.Add(dgv);

            // Invalidar badges para reflejar el cambio
            foreach (Control ctrl in this.Controls)
                if (ctrl is Panel p && p.Height == 48) { p.Invalidate(); break; }

            // Si todos estan silenciados, cerrar
            if (EstaSilenciada(CLAVE_STOCK) && EstaSilenciada(CLAVE_CXC) && EstaSilenciada(CLAVE_CXC_PROX) && EstaSilenciada(CLAVE_MANT) && EstaSilenciada(CLAVE_PRECIO))
            {
                MessageBox.Show("Todas las alertas han sido ignoradas por hoy.",
                    "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        // -------------------------------------------------------------------------
        // BADGES
        // -------------------------------------------------------------------------

        private void PnlBadges_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var badges = new (string Texto, int Cantidad, Color Color, string Clave)[]
            {
                ("Stock Bajo",      _data.StockBajo.Count,         Color.FromArgb(192, 57, 43),  CLAVE_STOCK),
                ("CxC Vencidas",    _data.CxCVencidas.Count,       Color.FromArgb(230, 126, 34), CLAVE_CXC),
                ("CxC Por Vencer",  _data.CxCProximasVencer.Count, Color.FromArgb(52, 152, 219), CLAVE_CXC_PROX),
                ("Mantenimiento",   _data.MantVencido.Count,       Color.FromArgb(142, 68, 173), CLAVE_MANT),
                ("Precio Pendiente",_data.PreciosPendientes.Count, Color.FromArgb(150, 90, 0),   CLAVE_PRECIO),
            };

            int x = 10;
            foreach (var (texto, cant, color, clave) in badges)
            {
                bool silenciada = EstaSilenciada(clave);
                var rect = new Rectangle(x, 6, 170, 34);
                using var path = RoundedRect(rect, 6);
                g.FillPath(silenciada
                    ? new SolidBrush(Color.FromArgb(240, 245, 240))
                    : Brushes.White, path);
                g.DrawPath(new Pen(silenciada ? Color.FromArgb(160, 190, 160) : color, 1.5f), path);

                var colorText = silenciada ? Color.FromArgb(150, 170, 150) : color;
                using var fNum = new Font("Segoe UI", 14f, FontStyle.Bold);
                g.DrawString(silenciada ? "✓" : cant.ToString(), fNum,
                    new SolidBrush(colorText),
                    new RectangleF(x + 6, 4, 36, 38),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

                using var fTxt = new Font("Segoe UI", 8.5f);
                g.DrawString(silenciada ? texto + " (ignorada)" : texto,
                    fTxt, new SolidBrush(silenciada ? Color.FromArgb(150, 170, 150) : Color.FromArgb(60, 70, 80)),
                    new RectangleF(x + 44, 12, 120, 20));

                x += 180;
            }
        }

        // -------------------------------------------------------------------------
        // CARGAR TABS
        // -------------------------------------------------------------------------

        private void CargarTabs()
        {
            tabControl.TabPages.Clear();

            var tabStock = new TabPage($"Stock Bajo  ({_data.StockBajo.Count})")
                { BackColor = EstaSilenciada(CLAVE_STOCK) ? Color.FromArgb(240,245,240) : Color.FromArgb(245,247,250) };
            if (EstaSilenciada(CLAVE_STOCK)) tabStock.Text += " ✓";
            tabStock.Controls.Add(CrearGridStock());
            tabControl.TabPages.Add(tabStock);

            var tabCxC = new TabPage($"CxC Vencidas  ({_data.CxCVencidas.Count})")
                { BackColor = EstaSilenciada(CLAVE_CXC) ? Color.FromArgb(240,245,240) : Color.FromArgb(245,247,250) };
            if (EstaSilenciada(CLAVE_CXC)) tabCxC.Text += " ✓";
            tabCxC.Controls.Add(CrearGridCxC());
            tabControl.TabPages.Add(tabCxC);

            var tabCxCProx = new TabPage($"CxC Por Vencer  ({_data.CxCProximasVencer.Count})")
                { BackColor = EstaSilenciada(CLAVE_CXC_PROX) ? Color.FromArgb(240,245,240) : Color.FromArgb(245,247,250) };
            if (EstaSilenciada(CLAVE_CXC_PROX)) tabCxCProx.Text += " ✓";
            tabCxCProx.Controls.Add(CrearGridCxCProx());
            tabControl.TabPages.Add(tabCxCProx);

            var tabMant = new TabPage($"Mantenimiento  ({_data.MantVencido.Count})")
                { BackColor = EstaSilenciada(CLAVE_MANT) ? Color.FromArgb(240,245,240) : Color.FromArgb(245,247,250) };
            if (EstaSilenciada(CLAVE_MANT)) tabMant.Text += " ✓";
            tabMant.Controls.Add(CrearGridMant());
            tabControl.TabPages.Add(tabMant);

            var tabPrecio = new TabPage($"Precio Pendiente  ({_data.PreciosPendientes.Count})")
                { BackColor = EstaSilenciada(CLAVE_PRECIO) ? Color.FromArgb(240,245,240) : Color.FromArgb(245,247,250) };
            if (EstaSilenciada(CLAVE_PRECIO)) tabPrecio.Text += " ✓";
            tabPrecio.Controls.Add(CrearGridPrecio());
            tabControl.TabPages.Add(tabPrecio);

            // Seleccionar el tab con mas alertas no silenciado
            int maxIdx = 0, maxVal = EstaSilenciada(CLAVE_STOCK) ? -1 : _data.StockBajo.Count;
            if (!EstaSilenciada(CLAVE_CXC) && _data.CxCVencidas.Count > maxVal)
            { maxVal = _data.CxCVencidas.Count; maxIdx = 1; }
            if (!EstaSilenciada(CLAVE_CXC_PROX) && _data.CxCProximasVencer.Count > maxVal)
            { maxVal = _data.CxCProximasVencer.Count; maxIdx = 2; }
            if (!EstaSilenciada(CLAVE_MANT) && _data.MantVencido.Count > maxVal)
            { maxVal = _data.MantVencido.Count; maxIdx = 3; }
            if (!EstaSilenciada(CLAVE_PRECIO) && _data.PreciosPendientes.Count > maxVal)
                maxIdx = 4;
            tabControl.SelectedIndex = maxIdx;
        }

        // -------------------------------------------------------------------------
        // GRIDS
        // -------------------------------------------------------------------------

        private Control CrearGridStock()
        {
            var dgv = CrearDGV();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCodigo",      HeaderText = "Codigo",       FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDescripcion", HeaderText = "Descripcion",  FillWeight = 35 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDepto",       HeaderText = "Departamento", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colExistencia",  HeaderText = "Existencia",   FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMinimo",      HeaderText = "Minimo",       FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });

            if (EstaSilenciada(CLAVE_STOCK)) { MostrarMensajeSilenciada(dgv, "Alerta de stock ignorada hoy"); return dgv; }

            foreach (var item in _data.StockBajo.OrderBy(x => x.Existencia / (x.ExistenMin == 0 ? 1 : x.ExistenMin)))
            {
                int row = dgv.Rows.Add();
                dgv.Rows[row].Cells["colCodigo"].Value      = item.Codigo;
                dgv.Rows[row].Cells["colDescripcion"].Value = item.Descripcion;
                dgv.Rows[row].Cells["colDepto"].Value       = item.Departamento;
                dgv.Rows[row].Cells["colExistencia"].Value  = item.Existencia.ToString("N2");
                dgv.Rows[row].Cells["colMinimo"].Value      = item.ExistenMin.ToString("N2");
                dgv.Rows[row].DefaultCellStyle.BackColor = item.Existencia <= 0
                    ? Color.FromArgb(255, 220, 220) : Color.FromArgb(255, 243, 220);
            }
            if (_data.StockBajo.Count == 0) AgregarLabelVacio(dgv, "✓  Sin productos bajo el stock minimo", Color.FromArgb(39, 174, 96));
            return dgv;
        }

        private Control CrearGridCxC()
        {
            var dgv = CrearDGV();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCliente",  HeaderText = "Cliente",      FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colConcepto", HeaderText = "Concepto",     FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSaldo",    HeaderText = "Saldo (USD)",  FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colVence",    HeaderText = "Vencio",       FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDias",     HeaderText = "Dias Vencida", FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });

            if (EstaSilenciada(CLAVE_CXC)) { MostrarMensajeSilenciada(dgv, "Alerta de CxC vencidas ignorada hoy"); return dgv; }

            foreach (var item in _data.CxCVencidas.OrderByDescending(x => (DateTime.Today - x.FechaVence).Days))
            {
                int dias = (DateTime.Today - item.FechaVence).Days;
                int row = dgv.Rows.Add();
                dgv.Rows[row].Cells["colCliente"].Value  = item.Cliente;
                dgv.Rows[row].Cells["colConcepto"].Value = item.Concepto;
                dgv.Rows[row].Cells["colSaldo"].Value    = $"$ {(item.Monto - item.Pagado):N2}";
                dgv.Rows[row].Cells["colVence"].Value    = item.FechaVence.ToString("dd/MM/yyyy");
                dgv.Rows[row].Cells["colDias"].Value     = dias.ToString();
                dgv.Rows[row].DefaultCellStyle.BackColor = dias > 30
                    ? Color.FromArgb(255, 210, 210) : Color.FromArgb(255, 243, 220);
            }
            if (_data.CxCVencidas.Count == 0) AgregarLabelVacio(dgv, "✓  Sin cuentas por cobrar vencidas", Color.FromArgb(39, 174, 96));
            return dgv;
        }

        private Control CrearGridCxCProx()
        {
            var dgv = CrearDGV();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCliente",  HeaderText = "Cliente",          FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colConcepto", HeaderText = "Concepto",         FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSaldo",    HeaderText = "Saldo (USD)",      FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colVence",    HeaderText = "Vence",            FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDias",     HeaderText = "Dias Restantes",   FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });

            if (EstaSilenciada(CLAVE_CXC_PROX)) { MostrarMensajeSilenciada(dgv, "Alerta de CxC por vencer ignorada hoy"); return dgv; }

            foreach (var item in _data.CxCProximasVencer.OrderBy(x => x.FechaVence))
            {
                int dias = (item.FechaVence - DateTime.Today).Days;
                int row = dgv.Rows.Add();
                dgv.Rows[row].Cells["colCliente"].Value  = item.Cliente;
                dgv.Rows[row].Cells["colConcepto"].Value = item.Concepto;
                dgv.Rows[row].Cells["colSaldo"].Value    = $"$ {(item.Monto - item.Pagado):N2}";
                dgv.Rows[row].Cells["colVence"].Value    = item.FechaVence.ToString("dd/MM/yyyy");
                dgv.Rows[row].Cells["colDias"].Value     = dias.ToString();
                dgv.Rows[row].DefaultCellStyle.BackColor = dias == 0
                    ? Color.FromArgb(255, 230, 200)
                    : Color.FromArgb(255, 248, 210);
            }
            if (_data.CxCProximasVencer.Count == 0) AgregarLabelVacio(dgv, "✓  Sin cuentas por cobrar próximas a vencer", Color.FromArgb(39, 174, 96));
            return dgv;
        }

        private Control CrearGridMant()
        {
            var dgv = CrearDGV();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMolde",  HeaderText = "Molde",        FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc",   HeaderText = "Descripcion",  FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTipo",   HeaderText = "Tipo Mant.",   FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUltimo", HeaderText = "Ultimo Mant.", FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDias",   HeaderText = "Dias Vencido", FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEstado", HeaderText = "Estado",       FillWeight = 10 });

            if (EstaSilenciada(CLAVE_MANT)) { MostrarMensajeSilenciada(dgv, "Alerta de mantenimiento ignorada hoy"); return dgv; }

            foreach (var item in _data.MantVencido.OrderBy(x => x.DiasRestantes))
            {
                int row = dgv.Rows.Add();
                dgv.Rows[row].Cells["colMolde"].Value  = item.CodigoMolde;
                dgv.Rows[row].Cells["colDesc"].Value   = item.DescMolde;
                dgv.Rows[row].Cells["colTipo"].Value   = item.TipoMant;
                dgv.Rows[row].Cells["colUltimo"].Value = item.UltimoMant.ToString("dd/MM/yyyy");
                dgv.Rows[row].Cells["colDias"].Value   = Math.Abs(item.DiasRestantes).ToString();
                dgv.Rows[row].Cells["colEstado"].Value = item.Estado;
                dgv.Rows[row].DefaultCellStyle.BackColor = item.DiasRestantes < -7
                    ? Color.FromArgb(255, 210, 210) : Color.FromArgb(255, 243, 220);
            }
            if (_data.MantVencido.Count == 0) AgregarLabelVacio(dgv, "✓  Sin mantenimientos vencidos", Color.FromArgb(39, 174, 96));
            return dgv;
        }

        private Control CrearGridPrecio()
        {
            var dgv = CrearDGV();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFecha",    HeaderText = "Fecha",       FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProv",     HeaderText = "Proveedor",   FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colConcepto", HeaderText = "Concepto",    FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCat",      HeaderText = "Categoría",   FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCant",     HeaderText = "Cantidad",    FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });

            if (EstaSilenciada(CLAVE_PRECIO)) { MostrarMensajeSilenciada(dgv, "Alerta de precio pendiente ignorada hoy"); return dgv; }

            foreach (var item in _data.PreciosPendientes.OrderBy(x => x.Fecha))
            {
                int row = dgv.Rows.Add();
                dgv.Rows[row].Cells["colFecha"].Value    = item.Fecha.ToString("dd/MM/yyyy");
                dgv.Rows[row].Cells["colProv"].Value     = item.Proveedor;
                dgv.Rows[row].Cells["colConcepto"].Value = item.Concepto;
                dgv.Rows[row].Cells["colCat"].Value      = item.NombreCategoria;
                dgv.Rows[row].Cells["colCant"].Value     = item.Cantidad > 0 ? item.Cantidad.ToString("N2") : "—";
                dgv.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(255, 244, 224);
            }
            if (_data.PreciosPendientes.Count == 0)
                AgregarLabelVacio(dgv, "✓  Sin egresos con precio pendiente", Color.FromArgb(39, 174, 96));
            return dgv;
        }

        // -------------------------------------------------------------------------
        // HELPERS
        // -------------------------------------------------------------------------

        private DataGridView CrearDGV()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f)
            };
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(44, 62, 80), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            dgv.ColumnHeadersHeight = 30;
            dgv.RowTemplate.Height = 26;
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(41, 128, 185),
                SelectionForeColor = Color.White
            };
            return dgv;
        }

        private void MostrarMensajeSilenciada(DataGridView dgv, string mensaje)
        {
            dgv.BackgroundColor = Color.FromArgb(240, 245, 240);
            AgregarLabelVacio(dgv, "✓  " + mensaje, Color.FromArgb(39, 174, 96));
        }

        private void AgregarLabelVacio(DataGridView dgv, string mensaje, Color color)
        {
            // WinForms no tiene EmptyDataText — superponemos un Label sobre el DGV
            var lbl = new Label
            {
                Text = mensaje,
                Font = new Font("Segoe UI", 10f),
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            // El label se agrega al TabPage padre del DGV cuando este ya este en el arbol
            dgv.VisibleChanged += (s, e) =>
            {
                if (dgv.Visible && dgv.Parent != null && !dgv.Parent.Controls.Contains(lbl))
                {
                    dgv.Parent.Controls.Add(lbl);
                    lbl.BringToFront();
                }
            };
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
