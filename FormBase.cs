#nullable enable
// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FormBase.cs  →  raíz del proyecto
//
// Clase base para todos los formularios del sistema. Aplica automáticamente:
//   - Tipografia base de UITheme (10pt)
//   - Paleta clara unificada (UITheme.Aplicar)
//   - Tab en cualquier campo → selecciona todo el contenido
//   - NumericUpDown de moneda → máximo 2 decimales
//   - Tecla Escape → cierra el formulario (si no es la ventana principal)
//   - VentanaPrefs: persiste tamaño y posición automaticamente, con debounce
//     800ms para no escribir en BD en cada pixel de resize/movimiento
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FormBase : Form
    {
        private System.Windows.Forms.Timer? _timerPrefs;

        public FormBase()
        {
            // Aplicar tema antes de OnLoad para que los controles añadidos
            // en el constructor del subform ya hereden las fuentes/colores.
            this.Font      = UITheme.Body;
            this.BackColor = Tema.Fondo;
            this.AutoScaleMode       = AutoScaleMode.Dpi;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96f, 96f);
        }

        /// <summary>
        /// Las ventanas que ya aplican su propio tema neumórfico a mano
        /// (Menú y Dashboard) sobrescriben esto en false para que el
        /// aplicador global no reescriba su estilo.
        /// </summary>
        protected virtual bool TemaAutomatico => true;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (TemaAutomatico) Tema.Aplicar(this);

            // ── Persistencia de tamaño/posición ─────────────────────────────
            // Restaurar antes de mostrar, guardar con debounce al cambiar.
            // Solo aplica a ventanas redimensionables (no a diálogos fixed).
            try
            {
                if (this.FormBorderStyle == FormBorderStyle.Sizable ||
                    this.FormBorderStyle == FormBorderStyle.SizableToolWindow)
                {
                    VentanaPrefs.Restaurar(this);

                    _timerPrefs = new System.Windows.Forms.Timer { Interval = 800 };
                    _timerPrefs.Tick += (s, ev) =>
                    {
                        _timerPrefs!.Stop();
                        try { VentanaPrefs.Guardar(this); } catch { }
                    };
                    SizeChanged     += GuardarPrefsConDebounce;
                    LocationChanged += GuardarPrefsConDebounce;
                    FormClosed      += (s, ev) =>
                    {
                        _timerPrefs?.Stop();
                        try { VentanaPrefs.Guardar(this); } catch { }
                        _timerPrefs?.Dispose();
                    };
                }
            }
            catch { /* sin VentanaPrefs en versiones viejas */ }
        }

        private void GuardarPrefsConDebounce(object? sender, EventArgs e)
        {
            if (_timerPrefs == null) return;
            // Reinicia el timer para acumular cambios y guardar solo al final
            _timerPrefs.Stop();
            _timerPrefs.Start();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UIHelper.ConfigurarFormulario(this);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Escape cierra diálogos (no la ventana principal)
            if (keyData == Keys.Escape && this.Modal)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ── Navegación entre ventanas ───────────────────────────────────────
        // Cuando se abre una ventana via ShowDialog desde otra, se OCULTA
        // automáticamente la ventana padre para que no se vean apiladas.
        // Al cerrar la hija, la padre se restaura.
        //
        // Esto se hace SIN tocar las 40+ llamadas existentes — solo
        // override del ShowDialog en FormBase.
        //
        // Mecanismo: se identifica la ventana padre como ActiveForm (la
        // que tenía el foco antes de abrir esta) y se Hide() temporalmente
        // mientras self está visible.
        public new DialogResult ShowDialog()
        {
            return ShowDialogConOcultarPadre(null);
        }

        public new DialogResult ShowDialog(IWin32Window owner)
        {
            return ShowDialogConOcultarPadre(owner as Form);
        }

        private DialogResult ShowDialogConOcultarPadre(Form? ownerExplicito)
        {
            // Identificar la ventana a esconder: el owner explícito si lo hay,
            // o la ventana actualmente activa.
            Form? padre = ownerExplicito ?? Form.ActiveForm;

            // Solo esconder si:
            //  - existe
            //  - no es self
            //  - está visible
            //  - es FormBase (no esconder ventanas del sistema o no controladas)
            bool ocultarPadre = padre != null
                              && padre != this
                              && padre.Visible
                              && padre is FormBase
                              && !padre.IsDisposed;

            if (ocultarPadre && padre != null)
            {
                try { padre.Hide(); } catch { ocultarPadre = false; }
            }

            DialogResult resultado;
            try
            {
                resultado = padre != null
                    ? base.ShowDialog(padre)
                    : base.ShowDialog();
            }
            finally
            {
                // Restaurar la ventana padre al cerrar self
                if (ocultarPadre && padre != null && !padre.IsDisposed)
                {
                    try
                    {
                        padre.Show();
                        padre.BringToFront();
                        padre.Activate();
                    }
                    catch { }
                }
            }
            return resultado;
        }
    }
}
