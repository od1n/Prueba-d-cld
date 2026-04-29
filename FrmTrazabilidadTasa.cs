// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmTrazabilidadTasa.cs  →  raíz del proyecto
//
// Pantalla de auditoría de tasas de cambio. Muestra:
//   - Histórico de tasas registradas (últimos 60 días por defecto)
//   - Origen de cada registro: AUTO_API / MANUAL / EDITADO / IMPORTADO
//   - Usuario que la registró y cuándo
//   - Al seleccionar una tasa, lista las facturas que la usaron
//
// Permisos:
//   - Acceso: cualquier rol con TasaConsulta (Admin, Ventas, Almacen, Contab.)
//   - Botón "Editar tasa": solo si el usuario tiene TasaModificar
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmTrazabilidadTasa : FormBase
    {
        private DataGridView _dgvTasas = null!;
        private DataGridView _dgvFacturas = null!;
        private Label _lblResumen = null!;
        private Label _lblHeaderFact = null!;

        public FrmTrazabilidadTasa()
        {
            // Validación de acceso
            if (!Sesion.EsAdmin && !Sesion.TienePermiso("TasaConsulta"))
            {
                MessageBox.Show(
                    "Tu rol (" + Sesion.Rol + ") no tiene permiso para consultar el histórico de tasas.",
                    "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Load += (s, e) => Close();
                return;
            }

            Text = "Trazabilidad de Tasas de Cambio";
            Size = new Size(1100, 720);
            MinimumSize = new Size(900, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UITheme.Fondo;
            ConstruirUI();
            Load += (s, e) => CargarTasas();
        }

        // ── UI ──────────────────────────────────────────────────────────────
        private void ConstruirUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = UITheme.Fondo
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));

            root.Controls.Add(ConstruirHeader(),  0, 0);
            root.Controls.Add(ConstruirContenido(), 0, 1);
            root.Controls.Add(ConstruirFooter(), 0, 2);
            Controls.Add(root);
        }

        private Panel ConstruirHeader()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.AzulOscuro };
            var lblTit = new Label
            {
                Text = "  Trazabilidad de Tasas de Cambio",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = UITheme.AzulOscuro,
                Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 8, 0, 0)
            };
            var lblSub = new Label
            {
                Text = "  Histórico, origen y facturas afectadas por cada tasa.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(168, 177, 187), BackColor = UITheme.AzulOscuro,
                Dock = DockStyle.Bottom, Height = 24,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 4)
            };
            p.Controls.Add(lblSub);
            p.Controls.Add(lblTit);
            return p;
        }

        private TableLayoutPanel ConstruirContenido()
        {
            var t = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = UITheme.Fondo, Padding = new Padding(12, 12, 12, 6)
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));

            // ── Izquierda: histórico de tasas ──────────────────────────────
            var pnlIzq = TarjetaConTitulo("Histórico de tasas (últimos 60 días)", out var contIzq);
            pnlIzq.Margin = new Padding(0, 0, 6, 0);

            _dgvTasas = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToResizeRows = false, RowHeadersVisible = false,
                BackgroundColor = UITheme.FondoCard, BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AlternatingRowsDefaultCellStyle = { BackColor = Color.FromArgb(248, 250, 253) }
            };
            UITheme.EstilizarGrid(_dgvTasas);
            _dgvTasas.SelectionChanged += (s, e) => CargarFacturasDeTasaSeleccionada();
            contIzq.Controls.Add(_dgvTasas);
            t.Controls.Add(pnlIzq, 0, 0);

            // ── Derecha: facturas afectadas ────────────────────────────────
            var pnlDer = TarjetaConTitulo("Facturas que usaron esta tasa", out var contDer);
            pnlDer.Margin = new Padding(6, 0, 0, 0);

            _lblHeaderFact = new Label
            {
                Text = "Selecciona una tasa para ver las facturas asociadas.",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = UITheme.TextoTenue,
                Dock = DockStyle.Top, Height = 26, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 4, 0, 0)
            };
            _dgvFacturas = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToResizeRows = false, RowHeadersVisible = false,
                BackgroundColor = UITheme.FondoCard, BorderStyle = BorderStyle.None,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AlternatingRowsDefaultCellStyle = { BackColor = Color.FromArgb(248, 250, 253) }
            };
            UITheme.EstilizarGrid(_dgvFacturas);
            contDer.Controls.Add(_dgvFacturas);
            contDer.Controls.Add(_lblHeaderFact);
            t.Controls.Add(pnlDer, 1, 0);

            return t;
        }

        private Panel TarjetaConTitulo(string titulo, out Panel content)
        {
            var wrap = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.Borde,
                Padding = new Padding(1) };
            var inner = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.FondoCard };
            var head = new Panel { Dock = DockStyle.Top, Height = 30,
                BackColor = Color.FromArgb(244, 246, 250) };
            head.Controls.Add(new Label
            {
                Text = "  " + titulo,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = UITheme.Texto, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            content = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.FondoCard };
            inner.Controls.Add(content); inner.Controls.Add(head);
            wrap.Controls.Add(inner);
            return wrap;
        }

        private Panel ConstruirFooter()
        {
            // Barra inferior CLARA con buen contraste (estilo unificado)
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.FondoCard };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Borde, 1f);
                e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
            };

            _lblResumen = new Label
            {
                Text = "  Sin tasa seleccionada.",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = UITheme.Texto,
                BackColor = UITheme.FondoCard,
                Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };

            var btnCerrar = new Button
            {
                Text = "Cerrar",
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Azul,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand, Width = 130, Height = 36,
                Margin = new Padding(0, 7, 16, 7)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.FlatAppearance.MouseOverBackColor = UITheme.AzulHover;
            btnCerrar.Click += (s, e) => Close();

            var flowR = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = UITheme.FondoCard,
                Width = 170,
                WrapContents = false,
                Padding = new Padding(0)
            };
            flowR.Controls.Add(btnCerrar);

            // Orden importante: Dock=Fill primero, Dock=Right al final
            p.Controls.Add(_lblResumen);
            p.Controls.Add(flowR);
            return p;
        }

        // ── Carga de datos ──────────────────────────────────────────────────
        private void CargarTasas()
        {
            try
            {
                var tasas = DatabaseService.ObtenerHistorialTasas(60);
                _dgvTasas.DataSource = tasas.Select(t => new {
                    t.Id,
                    Fecha   = t.Fecha.ToString("dd/MM/yyyy"),
                    BCV     = t.TasaBCV.ToString("N4"),
                    Paralelo = t.TasaParalelo.ToString("N4"),
                    Origen  = OrigenAmigable(t.Origen ?? "MANUAL"),
                    Usuario = t.Usuario ?? "",
                    Registrada = t.FechaRegistro == default
                        ? "—"
                        : t.FechaRegistro.ToString("dd/MM HH:mm")
                }).ToList();
                if (_dgvTasas.Columns.Count > 0)
                    _dgvTasas.Columns[0].Visible = false;  // Id oculto
                _lblResumen.Text = "  " + tasas.Count + " tasas en el histórico mostrado.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al cargar tasas: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarFacturasDeTasaSeleccionada()
        {
            if (_dgvTasas.SelectedRows.Count == 0) return;
            var idCell = _dgvTasas.SelectedRows[0].Cells["Id"]?.Value;
            if (idCell == null) return;
            int idTasa = Convert.ToInt32(idCell);

            try
            {
                var facturas = DatabaseService.ObtenerFacturasQueUsaronTasa(idTasa);
                _dgvFacturas.DataSource = facturas.Select(f => new {
                    Numero  = f.NumeroF,
                    Fecha   = f.Fecha,
                    Cliente = f.Codigo,
                    Total   = "$ " + f.Total.ToString("N2")
                }).ToList();

                decimal totalFact = facturas.Sum(f => f.Total);
                _lblHeaderFact.Text =
                    "  " + facturas.Count + " facturas usaron esta tasa · " +
                    "Total: $ " + totalFact.ToString("N2");
                _lblHeaderFact.ForeColor = facturas.Count > 0
                    ? UITheme.AzulOscuro : UITheme.TextoTenue;

                _lblResumen.Text =
                    "  Tasa seleccionada: ID " + idTasa + "  ·  " +
                    facturas.Count + " factura(s) afectada(s)  ·  " +
                    "Total impactado: $ " + totalFact.ToString("N2");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al cargar facturas: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string OrigenAmigable(string codigo) => codigo switch
        {
            "AUTO_API"  => "● Automático (API)",
            "MANUAL"    => "○ Manual",
            "EDITADO"   => "◐ API editado",
            "IMPORTADO" => "▤ Importado",
            _           => codigo
        };
    }
}
