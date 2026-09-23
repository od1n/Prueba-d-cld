#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════
    //  FrmConfigLegal — Parámetros legales y fiscales
    //  Permite configurar IVA, aportes IVSS/RPVH/INCES, UT y salario mínimo.
    //  Solo accesible por administradores.
    // ══════════════════════════════════════════════════════════════════════
    public class FrmConfigLegal : FormBase
    {
        // ── Controles de facturación ───────────────────────────────────────
        private NumericUpDown _nudIVA       = null!;

        // ── Controles de nómina empleado ──────────────────────────────────
        private NumericUpDown _nudIVSSEmp   = null!;
        private NumericUpDown _nudRPVHEmp   = null!;

        // ── Controles de nómina patronal ──────────────────────────────────
        private NumericUpDown _nudIVSSPat   = null!;
        private NumericUpDown _nudRPVHPat   = null!;
        private NumericUpDown _nudINCS      = null!;

        // ── Controles de valores legales ──────────────────────────────────
        private NumericUpDown _nudUT        = null!;
        private NumericUpDown _nudSalMin    = null!;

        // ── Controles de crédito ──────────────────────────────────────────
        private NumericUpDown _nudDiasCredit = null!;

        public FrmConfigLegal()
        {
            Text            = "Configuración de Parámetros Legales";
            Size            = new Size(500, 590);
            MinimumSize     = new Size(460, 540);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = MinimizeBox = false;
            BackColor       = Color.FromArgb(245, 245, 250);
            Font            = new Font("Segoe UI", 9.5f);

            ConstruirUI();
            CargarValores();
        }

        // ── Construcción de la interfaz ────────────────────────────────────
        private void ConstruirUI()
        {
            // Header azul
            var hdr = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.FromArgb(26, 74, 138)
            };
            hdr.Controls.Add(new Label
            {
                Text      = "⚖  Parámetros Legales y Fiscales",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(14, 13)
            });
            Controls.Add(hdr);

            // Panel con scroll para todo el contenido
            var scroll = new Panel
            {
                Dock          = DockStyle.Fill,
                AutoScroll    = true,
                Padding       = new Padding(0, 0, 0, 60),
                BackColor     = Color.FromArgb(245, 245, 250)
            };
            Controls.Add(scroll);

            int y = 14;
            const int xLbl  = 20;
            const int xCtrl = 260;
            const int wCtrl = 160;

            // ── Sección: Facturación ───────────────────────────────────────
            AgregarSeccion(scroll, "FACTURACIÓN", ref y, xLbl);

            AgregarFila(scroll, "IVA (%):", "Impuesto al Valor Agregado aplicado en facturas.",
                        ref y, xLbl, xCtrl, wCtrl, 0m, 100m, 2, out _nudIVA);

            AgregarSeparador(scroll, ref y);

            // ── Sección: Nómina — Empleado ────────────────────────────────
            AgregarSeccion(scroll, "NÓMINA — EMPLEADO", ref y, xLbl);

            AgregarFila(scroll, "IVSS Empleado (%):", "Aporte del empleado al seguro social (IVSS).",
                        ref y, xLbl, xCtrl, wCtrl, 0m, 20m, 2, out _nudIVSSEmp);

            AgregarFila(scroll, "RPVH Empleado (%):", "Aporte del empleado al régimen de vivienda (RPVH/FAOV).",
                        ref y, xLbl, xCtrl, wCtrl, 0m, 20m, 2, out _nudRPVHEmp);

            AgregarSeparador(scroll, ref y);

            // ── Sección: Nómina — Patronal ────────────────────────────────
            AgregarSeccion(scroll, "NÓMINA — PATRONAL", ref y, xLbl);

            AgregarFila(scroll, "IVSS Patronal (%):", "Aporte patronal al IVSS (varía 9–11% según riesgo).",
                        ref y, xLbl, xCtrl, wCtrl, 0m, 20m, 2, out _nudIVSSPat);

            AgregarFila(scroll, "RPVH Patronal (%):", "Aporte patronal al régimen de vivienda (RPVH/FAOV).",
                        ref y, xLbl, xCtrl, wCtrl, 0m, 20m, 2, out _nudRPVHPat);

            AgregarFila(scroll, "INCES Patronal (%):", "Aporte patronal al instituto de capacitación (INCES).",
                        ref y, xLbl, xCtrl, wCtrl, 0m, 10m, 2, out _nudINCS);

            AgregarSeparador(scroll, ref y);

            // ── Sección: Valores legales ───────────────────────────────────
            AgregarSeccion(scroll, "VALORES LEGALES", ref y, xLbl);

            AgregarFila(scroll, "Unidad Tributaria (Bs):", "Valor actual de la UT en bolívares (para cesta ticket y INCES).",
                        ref y, xLbl, xCtrl, wCtrl, 0m, 9_999_999m, 2, out _nudUT);

            AgregarFila(scroll, "Salario Mínimo (Bs):", "Salario mínimo nacional vigente en bolívares.",
                        ref y, xLbl, xCtrl, wCtrl, 0m, 9_999_999m, 2, out _nudSalMin);

            AgregarSeparador(scroll, ref y);

            // ── Sección: Crédito ───────────────────────────────────────────
            AgregarSeccion(scroll, "CRÉDITO", ref y, xLbl);

            AgregarFila(scroll, "Días crédito default:", "Días de crédito predeterminados para nuevas facturas.",
                        ref y, xLbl, xCtrl, wCtrl, 0m, 365m, 0, out _nudDiasCredit);

            y += 10;

            // ── Botones al pie ─────────────────────────────────────────────
            var pnlBotones = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 52,
                BackColor = Color.FromArgb(235, 235, 242),
                Padding   = new Padding(0)
            };

            var btnGuardar = new Button
            {
                Text      = "💾  Guardar",
                Location  = new Point(20, 10),
                Size      = new Size(140, 34),
                BackColor = Color.FromArgb(26, 74, 138),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize           = 0;
            btnGuardar.FlatAppearance.MouseOverBackColor   = Color.FromArgb(45, 108, 192);
            btnGuardar.Click += GuardarValores;

            var btnCerrar = new Button
            {
                Text      = "Cerrar",
                Location  = new Point(170, 10),
                Size      = new Size(110, 34),
                BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10f),
                Cursor    = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();

            pnlBotones.Controls.Add(btnGuardar);
            pnlBotones.Controls.Add(btnCerrar);
            Controls.Add(pnlBotones);
        }

        // ── Helpers de construcción ────────────────────────────────────────

        private void AgregarSeccion(Panel parent, string titulo, ref int y, int x)
        {
            var lbl = new Label
            {
                Text      = titulo,
                Location  = new Point(x, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 74, 138)
            };
            parent.Controls.Add(lbl);
            y += 22;

            var sep = new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(440, 1),
                BackColor = Color.FromArgb(180, 190, 210)
            };
            parent.Controls.Add(sep);
            y += 8;
        }

        private void AgregarSeparador(Panel parent, ref int y)
        {
            y += 6;
            var sep = new Panel
            {
                Location  = new Point(20, y),
                Size      = new Size(440, 1),
                BackColor = Color.FromArgb(215, 220, 230)
            };
            parent.Controls.Add(sep);
            y += 12;
        }

        private void AgregarFila(Panel parent, string etiqueta, string ayuda,
                                  ref int y, int xLbl, int xCtrl, int wCtrl,
                                  decimal min, decimal max, int decimales,
                                  out NumericUpDown nud)
        {
            var lbl = new Label
            {
                Text      = etiqueta,
                Location  = new Point(xLbl, y + 3),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(40, 40, 60)
            };
            parent.Controls.Add(lbl);

            var nudLocal = new NumericUpDown
            {
                Location      = new Point(xCtrl, y),
                Size          = new Size(wCtrl, 26),
                Font          = new Font("Segoe UI", 9.5f),
                Minimum       = min,
                Maximum       = max,
                DecimalPlaces = decimales,
                ThousandsSeparator = decimales == 0 ? false : true
            };
            parent.Controls.Add(nudLocal);
            nud = nudLocal;

            y += 28;

            // Etiqueta de ayuda en gris
            var lblAyuda = new Label
            {
                Text      = ayuda,
                Location  = new Point(xLbl + 4, y),
                AutoSize  = false,
                Size      = new Size(420, 16),
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(130, 140, 160)
            };
            parent.Controls.Add(lblAyuda);
            y += 22;
        }

        // ── Carga y guardado de valores ────────────────────────────────────

        private void CargarValores()
        {
            decimal Leer(string clave, decimal def)
                => decimal.TryParse(DatabaseService.ObtenerConfig(clave, def.ToString()), out var v) ? v : def;

            _nudIVA.Value        = Leer("IVA_Porcentaje",    16);
            _nudIVSSEmp.Value    = Leer("IVSS_Empleado",      4);
            _nudRPVHEmp.Value    = Leer("RPVH_Empleado",      1);
            _nudIVSSPat.Value    = Leer("IVSS_Patronal",      9);
            _nudRPVHPat.Value    = Leer("RPVH_Patronal",      2);
            _nudINCS.Value       = Leer("INCES_Patronal",     2);
            _nudUT.Value         = Leer("UT_Bs",              0);
            _nudSalMin.Value     = Leer("SalarioMinimo_Bs",   0);
            _nudDiasCredit.Value = Leer("DiasCredito_Default",30);
        }

        private void GuardarValores(object? sender, EventArgs e)
        {
            try
            {
                DatabaseService.GuardarConfig("IVA_Porcentaje",     _nudIVA.Value.ToString());
                DatabaseService.GuardarConfig("IVSS_Empleado",      _nudIVSSEmp.Value.ToString());
                DatabaseService.GuardarConfig("RPVH_Empleado",      _nudRPVHEmp.Value.ToString());
                DatabaseService.GuardarConfig("IVSS_Patronal",      _nudIVSSPat.Value.ToString());
                DatabaseService.GuardarConfig("RPVH_Patronal",      _nudRPVHPat.Value.ToString());
                DatabaseService.GuardarConfig("INCES_Patronal",     _nudINCS.Value.ToString());
                DatabaseService.GuardarConfig("UT_Bs",              _nudUT.Value.ToString());
                DatabaseService.GuardarConfig("SalarioMinimo_Bs",   _nudSalMin.Value.ToString());
                DatabaseService.GuardarConfig("DiasCredito_Default",_nudDiasCredit.Value.ToString());

                DatabaseService.RegistrarAuditoria("EDITAR", "ConfigLegal",
                    $"Parámetros legales actualizados por {Sesion.NombreUsuario}");

                MessageBox.Show("Parámetros guardados correctamente.",
                    "Configuración Legal", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
