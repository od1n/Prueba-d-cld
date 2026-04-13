using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════
    //  REPORTES
    // ══════════════════════════════════════════════════════════════
    public class FrmReportes : Form
    {
        public FrmReportes()
        {
            Text = "Reportes"; Size = new Size(880, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 245, 248);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(180, 60, 60) };
            hdr.Controls.Add(new Label { Text = "Reportes del Sistema", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5f) };
            tabs.TabPages.Add(TabResumen());
            tabs.TabPages.Add(TabFacturas());
            tabs.TabPages.Add(TabCompras());
            tabs.TabPages.Add(TabInventario());
            tabs.TabPages.Add(TabMoldes());
            tabs.TabPages.Add(TabRentabilidad());
            tabs.TabPages.Add(TabFlujoCaja());
            tabs.TabPages.Add(TabLibroIVA());
            tabs.TabPages.Add(TabMermas());
            tabs.TabPages.Add(TabComparacion());

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(220, 220, 230) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 26), Location = new Point(760, 7), BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0; btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(btnCerrar);

            Controls.AddRange(new Control[] { tabs, hdr, footer });
        }

        private TabPage TabResumen()
        {
            var tab = new TabPage("Resumen General");
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.White };

            var facturas    = DatabaseService.ObtenerFacturas();
            var compras     = DatabaseService.ObtenerCompras();
            var clientes    = DatabaseService.ObtenerClientes();
            var proveedores = DatabaseService.ObtenerProveedores();
            var inventario  = DatabaseService.ObtenerInventario();
            var moldes      = DatabaseService.ObtenerMoldes();

            decimal totalFact   = facturas.Sum(f => f.TotalF);
            decimal totalCompra = compras.Sum(c => c.TotalC);

            panel.Controls.Add(Card("Total Facturado",   $"{totalFact:C2}",    Color.FromArgb(40, 160, 80)));
            panel.Controls.Add(Card("Total Compras",     $"{totalCompra:C2}",  Color.FromArgb(180, 60, 60)));
            panel.Controls.Add(Card("Clientes",          clientes.Count.ToString(),    Color.FromArgb(60, 100, 180)));
            panel.Controls.Add(Card("Proveedores",       proveedores.Count.ToString(), Color.FromArgb(160, 90, 20)));
            panel.Controls.Add(Card("Productos",         inventario.Count.ToString(),  Color.FromArgb(30, 120, 80)));
            panel.Controls.Add(Card("Bajo Minimo",       inventario.Count(i => i.Existencia < i.ExistenMin).ToString(), Color.FromArgb(200, 80, 0)));
            panel.Controls.Add(Card("Moldes",            moldes.Count.ToString(),      Color.FromArgb(20, 100, 140)));
            panel.Controls.Add(Card("Mant. Vencido",     moldes.Count(m => (DateTime.Today - m.FechaLastMant).Days > 180).ToString(), Color.FromArgb(180, 40, 60)));

            tab.Controls.Add(panel);
            return tab;
        }

        private Panel Card(string titulo, string valor, Color color)
        {
            var p = new Panel { Size = new Size(200, 100), Margin = new Padding(8), BackColor = color };
            p.Controls.Add(new Label { Text = titulo, Location = new Point(10, 10), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9f) });
            p.Controls.Add(new Label { Text = valor,  Location = new Point(10, 40), AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 18f, FontStyle.Bold) });
            return p;
        }

        private TabPage TabFacturas()
        {
            var tab = new TabPage("Facturas");
            var dgv = Grid(Color.FromArgb(180, 60, 60));
            dgv.DataSource = DatabaseService.ObtenerFacturas().Select(f => new {
                f.NumeroF, Fecha=f.Fecha.ToShortDateString(), Cliente=f.Codigo,
                f.FormaDePago, IVA=f.IvaF.ToString("C2"), Total=f.TotalF.ToString("C2")
            }).ToList();
            tab.Controls.Add(dgv);
            return tab;
        }
        private TabPage TabMermas()
        {
            var tab = new TabPage("Mermas");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var btnAbrir = new Button
            {
                Text = "Abrir Control de Mermas y Desperdicios",
                Size = new Size(340, 44),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(100, 50, 10),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAbrir.FlatAppearance.BorderSize = 0;
            btnAbrir.Click += (s, e) => new FrmMermas().ShowDialog();

            pnl.Controls.Add(btnAbrir);
            pnl.Resize += (s, e) => btnAbrir.Location = new System.Drawing.Point(
                (pnl.Width - btnAbrir.Width) / 2,
                (pnl.Height - btnAbrir.Height) / 2);

            tab.Controls.Add(pnl);
            return tab;
        }

        private TabPage TabComparacion()
        {
            var tab = new TabPage("Comparacion");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var btnAbrir = new Button
            {
                Text      = "Abrir Comparacion de Periodos",
                Size      = new Size(300, 44),
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(50, 50, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnAbrir.FlatAppearance.BorderSize = 0;
            btnAbrir.Click += (s, e) => new FrmComparacion().ShowDialog();

            pnl.Controls.Add(btnAbrir);
            pnl.Resize += (s, e) => btnAbrir.Location = new System.Drawing.Point(
                (pnl.Width  - btnAbrir.Width)  / 2,
                (pnl.Height - btnAbrir.Height) / 2);

            tab.Controls.Add(pnl);
            return tab;
        }

        private TabPage TabCompras()
        {
            var tab = new TabPage("Compras");
            var dgv = Grid(Color.FromArgb(100, 50, 160));
            dgv.DataSource = DatabaseService.ObtenerCompras().Select(c => new {
                c.CompraN, Fecha=c.FechaC.ToShortDateString(), Proveedor=c.CodigoP,
                c.FormaDePago, Total=c.TotalC.ToString("C2")
            }).ToList();
            tab.Controls.Add(dgv);
            return tab;
        }

        private TabPage TabInventario()
        {
            var tab = new TabPage("Inventario");
            var dgv = Grid(Color.FromArgb(30, 100, 60));
            var lista = DatabaseService.ObtenerInventario();
            dgv.DataSource = lista.Select(i => new {
                i.Codigo, i.Descripcion, i.Departamento,
                Costo=i.CostoActual.ToString("C2"), Precio=i.Precio.ToString("C2"),
                i.Existencia, i.ExistenMin, i.ExistenMax
            }).ToList();
            dgv.DataBindingComplete += (s, e) =>
            {
                for (int idx = 0; idx < dgv.Rows.Count; idx++)
                {
                    var item = lista[idx];
                    if (item.Existencia == 0)         dgv.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 210, 210);
                    else if (item.Existencia < item.ExistenMin) dgv.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 190);
                }
            };
            tab.Controls.Add(dgv);
            return tab;
        }

        private TabPage TabMoldes()
        {
            var tab = new TabPage("Moldes");
            var dgv = Grid(Color.FromArgb(20, 100, 140));
            dgv.DataSource = DatabaseService.ObtenerMoldes().Select(m => new {
                m.CodigoMold, m.Descripcion, m.ModeloQfabrica,
                UltMant = m.FechaLastMant.ToShortDateString(),
                Dias = (DateTime.Today - m.FechaLastMant).Days,
                m.CantMatXunid
            }).ToList();
            tab.Controls.Add(dgv);
            return tab;
        }

        private TabPage TabFlujoCaja()
        {
            var tab = new TabPage("Flujo de Caja");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var btnAbrir = new Button
            {
                Text = "Abrir proyeccion de flujo de caja 30/60/90 dias",
                Size = new Size(360, 44), Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(20, 70, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnAbrir.FlatAppearance.BorderSize = 0;
            btnAbrir.Click += (s, e) => new FrmFlujoCaja().ShowDialog();
            pnl.Controls.Add(btnAbrir);
            pnl.Resize += (s, e) => btnAbrir.Location = new System.Drawing.Point(
                (pnl.Width - btnAbrir.Width) / 2, (pnl.Height - btnAbrir.Height) / 2);
            tab.Controls.Add(pnl);
            return tab;
        }

        private TabPage TabRentabilidad()
        {
            var tab = new TabPage("Rentabilidad");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var btnAbrir = new Button
            {
                Text      = "Abrir reporte completo de rentabilidad",
                Size      = new Size(340, 44),
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 100, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnAbrir.FlatAppearance.BorderSize = 0;
            btnAbrir.Click += (s, e) => new FrmRentabilidad().ShowDialog();

            // Centrar el boton
            pnl.Controls.Add(btnAbrir);
            pnl.Resize += (s, e) =>
            {
                btnAbrir.Location = new System.Drawing.Point(
                    (pnl.Width  - btnAbrir.Width)  / 2,
                    (pnl.Height - btnAbrir.Height) / 2);
            };

            tab.Controls.Add(pnl);
            return tab;
        }
        private TabPage TabLibroIVA()
        {
            var tab = new TabPage("Libro IVA");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var btnAbrir = new Button
            {
                Text = "Abrir Libro de Ventas y Compras IVA (SENIAT)",
                Size = new Size(380, 44),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(140, 20, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAbrir.FlatAppearance.BorderSize = 0;
            btnAbrir.Click += (s, e) => new FrmLibroIVA().ShowDialog();

            pnl.Controls.Add(btnAbrir);
            pnl.Resize += (s, e) => btnAbrir.Location = new System.Drawing.Point(
                (pnl.Width - btnAbrir.Width) / 2,
                (pnl.Height - btnAbrir.Height) / 2);

            tab.Controls.Add(pnl);
            return tab;
        }
        private DataGridView Grid(Color h)
        {
            var g = new DataGridView { Dock=DockStyle.Fill, ReadOnly=true, SelectionMode=DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor=Color.White, BorderStyle=BorderStyle.None, RowHeadersVisible=false };
            g.ColumnHeadersDefaultCellStyle.BackColor=h; g.ColumnHeadersDefaultCellStyle.ForeColor=Color.White; g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",9f,FontStyle.Bold); g.EnableHeadersVisualStyles=false;
            return g;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  ARCHIVOS DEL SISTEMA  (catálogos maestros)
    // ══════════════════════════════════════════════════════════════
    public class FrmArchivos : Form
    {
        public FrmArchivos()
        {
            Text = "Archivos del Sistema"; Size = new Size(860, 660);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            BackColor = Color.FromArgb(248, 245, 235);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(180, 140, 20) };
            hdr.Controls.Add(new Label { Text = "Archivos del Sistema", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 13) });

            var panelBtns = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30, 20, 30, 20), BackColor = Color.FromArgb(250, 247, 235), FlowDirection = FlowDirection.LeftToRight, WrapContents = true };

            panelBtns.Controls.Add(ModBtn("Inventario",  Color.FromArgb(30,120,60),   () => new FrmInventario().ShowDialog()));
            panelBtns.Controls.Add(ModBtn("Clientes",    Color.FromArgb(40,80,160),   () => new FrmClientes().ShowDialog()));
            panelBtns.Controls.Add(ModBtn("Proveedores", Color.FromArgb(160,90,20),   () => new FrmProveedores().ShowDialog()));
            panelBtns.Controls.Add(ModBtn("Compras",     Color.FromArgb(100,50,160),  () => new FrmCompras().ShowDialog()));
            panelBtns.Controls.Add(ModBtn("Moldes",      Color.FromArgb(20,100,140),  () => new FrmMoldes().ShowDialog()));
            panelBtns.Controls.Add(ModBtn("Mant.Moldes",Color.FromArgb(20, 80,120),  () => new FrmMantenimientoMoldes().ShowDialog()));
            panelBtns.Controls.Add(ModBtn("Kardex",      Color.FromArgb(20, 80, 20),  () => new FrmKardex().ShowDialog()));
            panelBtns.Controls.Add(ModBtn("Ordenes OP",  Color.FromArgb(60, 120, 40),  () => new FrmOrdenesProduccion().ShowDialog()));
            panelBtns.Controls.Add(ModBtn("Control Cal.", Color.FromArgb(40, 80, 160),  () => new FrmControlCalidad().ShowDialog()));
            if (Sesion.EsAdmin)
                panelBtns.Controls.Add(ModBtn("Respaldos",   Color.FromArgb(20, 100, 50), () => new FrmRespaldos().ShowDialog()));

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = Color.FromArgb(235, 228, 210) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(120, 35), Location = new Point(270, 8), BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            btnCerrar.FlatAppearance.BorderSize = 0; btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(btnCerrar);

            Controls.AddRange(new Control[] { panelBtns, hdr, footer });
        }

        private Button ModBtn(string texto, Color color, Action onClick)
        {
            var btn = new Button { Text = texto, Size = new Size(160, 100), Margin = new Padding(10),
                BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => onClick();
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(color, 0.2f);
            btn.MouseLeave += (s, e) => btn.BackColor = color;
            return btn;
        }
    }

    // ══════════════════════════════════════════════════════════════
    //  CONFIGURACION DEL SISTEMA
    // ══════════════════════════════════════════════════════════════
    public class FrmSistema : Form
    {
        public FrmSistema()
        {
            Text = "Configuracion del Sistema"; Size = new Size(500, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(245, 245, 250);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(60, 80, 160) };
            hdr.Controls.Add(new Label { Text = "Configuracion del Sistema", ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 14) });

            int y = 70;
            Lbl("Nombre del sistema:", y); TxtBox("Sistema de Gestion", y); y += 45;
            Lbl("Version:", y);            TxtBox("1.0.0", y);              y += 45;
            Lbl("Empresa:", y);            TxtBox("Mi Empresa S.A.", y);    y += 45;
            Lbl("Moneda:", y);
            var cmbMoneda = new ComboBox { Location = new Point(200, y), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMoneda.Items.AddRange(new object[] { "MXN - Peso Mexicano", "USD - Dolar", "EUR - Euro", "VES - Bolivar" });
            cmbMoneda.SelectedIndex = 0; Controls.Add(cmbMoneda); y += 60;

            var btnRespaldos = new Button
            {
                Text = "Ver respaldos", Location = new Point(100, y), Size = new Size(160, 36),
                BackColor = Color.FromArgb(20, 100, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnRespaldos.FlatAppearance.BorderSize = 0;
            btnRespaldos.Click += (s, e) => new FrmRespaldos().ShowDialog();
            Controls.Add(btnRespaldos);
            y += 50;

            // Version del esquema
            var lblVersion = new Label
            {
                Location  = new Point(100, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(80, 100, 80)
            };
            try
            {
                using var cv = new Microsoft.Data.Sqlite.SqliteConnection(
                    SecurityService.CadenaConexion);
                cv.Open();
                var cmv = cv.CreateCommand();
                cmv.CommandText = "PRAGMA user_version";
                int ver = (int)(long)cmv.ExecuteScalar();
                lblVersion.Text = "Base de datos: version de esquema v" + ver +
                    " (codigo: v" + DatabaseService.SCHEMA_VERSION + ")" +
                    (ver == DatabaseService.SCHEMA_VERSION ? " — Al dia" : " — PENDIENTE MIGRACION");
                lblVersion.ForeColor = ver == DatabaseService.SCHEMA_VERSION
                    ? Color.FromArgb(30, 100, 40) : Color.FromArgb(160, 50, 30);
            }
            catch { lblVersion.Text = "No se pudo leer version de esquema"; }
            Controls.Add(lblVersion);
            y += 28;

            var btnPerfil2 = new Button
            {
                Text = "Mi Perfil y preferencias", Location = new Point(100, y),
                Size = new Size(220, 36),
                BackColor = Color.FromArgb(55, 70, 110), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnPerfil2.FlatAppearance.BorderSize = 0;
            btnPerfil2.Click += (s2, e2) => new FrmPerfil().ShowDialog();
            Controls.Add(btnPerfil2);
            y += 50;

            if (Sesion.EsAdmin)
            {
                var btnLog = new Button
                {
                    Text = "Ver log de auditoria", Location = new Point(100, y),
                    Size = new Size(220, 36),
                    BackColor = Color.FromArgb(40, 55, 90), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                };
                btnLog.FlatAppearance.BorderSize = 0;
                btnLog.Click += (s2, e2) => new FrmLogAuditoria().ShowDialog();
                Controls.Add(btnLog);
                y += 50;
            }

            var btnGuardar = new Button { Text = "Guardar cambios", Location = new Point(100, y), Size = new Size(160, 36),
                BackColor = Color.FromArgb(60, 130, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, e) => MessageBox.Show("Configuracion guardada.", "Exito", MessageBoxButtons.OK, MessageBoxIcon.Information);

            var btnCerrar = new Button { Text = "Cerrar", Location = new Point(280, y), Size = new Size(110, 36),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0; btnCerrar.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { hdr, btnGuardar, btnCerrar });
        }

        private void Lbl(string t, int y) => Controls.Add(new Label { Text = t, Location = new Point(30, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) });
        private void TxtBox(string val, int y) { var tb = new TextBox { Text = val, Location = new Point(200, y), Size = new Size(200, 25) }; Controls.Add(tb); }
    }
}
