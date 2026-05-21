// ════════════════════════════════════════════════════════════════════════════
//  AyudaService.cs — Ayuda contextual para todas las ventanas del sistema
//  Uso: AyudaService.Mostrar(this, "FrmInventario");
//       AyudaService.Tip(tooltip, control, "Texto");
// ════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public static class AyudaService
    {
        private static readonly Color C1 = Color.FromArgb(26, 74, 138);

        // ── Tooltip helper ────────────────────────────────────────────────────
        public static ToolTip CrearTooltip()
        {
            return new ToolTip {
                AutoPopDelay = 6000, InitialDelay = 400,
                ReshowDelay = 200, ShowAlways = true,
                ToolTipIcon = ToolTipIcon.Info
            };
        }

        public static void Tip(ToolTip tt, Control ctrl, string texto)
            => tt.SetToolTip(ctrl, texto);

        // ── Ventana de ayuda ──────────────────────────────────────────────────
        public static void Mostrar(Form owner, string ventana)
        {
            if (!_ayudas.ContainsKey(ventana)) return;
            var (titulo, secciones) = _ayudas[ventana];

            var frm = new Form {
                Text            = "Ayuda — " + titulo,
                Size            = new Size(820, 720),
                MinimumSize     = new Size(640, 500),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                BackColor       = Color.White
            };

            var hdr = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = C1 };
            hdr.Controls.Add(new Label {
                Text = "📋  " + titulo, ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 14) });

            var rtf = new RichTextBox {
                Dock = DockStyle.Fill, ReadOnly = true,
                BorderStyle = BorderStyle.None, BackColor = Color.White,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(10)
            };

            // Fuentes
            var fTit  = new Font("Segoe UI", 16f, FontStyle.Bold);
            var fSec  = new Font("Segoe UI", 14f, FontStyle.Bold);
            var fNorm = new Font("Segoe UI", 13f);
            var fNeg  = new Font("Segoe UI", 13f, FontStyle.Bold);
            var fMono = new Font("Courier New", 13f);
            var fSep  = new Font("Segoe UI", 4f);

            void Titulo(string t, Color c)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont = fTit; rtf.SelectionColor = c;
                rtf.AppendText("\r\n" + t + "\r\n");
                rtf.SelectionFont = fSep; rtf.SelectionColor = c;
                rtf.AppendText("──────────────────────────────────────────────────\r\n\r\n");
            }
            void Sec(string t, Color? c = null)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont = fSec; rtf.SelectionColor = c ?? C1;
                rtf.AppendText(t + "\r\n");
                rtf.SelectionFont = fSep; rtf.AppendText("\r\n");
            }
            void P(string t)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont = fNorm; rtf.SelectionColor = Color.FromArgb(55, 65, 81);
                rtf.AppendText(t + "\r\n");
            }
            void PB(string neg, string norm = "")
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont = fNeg; rtf.SelectionColor = Color.FromArgb(30, 41, 59);
                rtf.AppendText(neg);
                if (!string.IsNullOrEmpty(norm)) {
                    rtf.SelectionFont = fNorm; rtf.SelectionColor = Color.FromArgb(55, 65, 81);
                    rtf.AppendText(norm);
                }
                rtf.AppendText("\r\n");
            }
            void Paso(int n, string t)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont = fNeg; rtf.SelectionColor = C1;
                rtf.AppendText($"  {n}.  ");
                rtf.SelectionFont = fNorm; rtf.SelectionColor = Color.FromArgb(55, 65, 81);
                rtf.AppendText(t + "\r\n");
            }
            void Bul(string t, Color? c = null)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont = fNeg; rtf.SelectionColor = c ?? Color.FromArgb(26, 110, 58);
                rtf.AppendText("  •  ");
                rtf.SelectionFont = fNorm; rtf.SelectionColor = Color.FromArgb(55, 65, 81);
                rtf.AppendText(t + "\r\n");
            }
            void F(string t)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont = fMono; rtf.SelectionColor = Color.FromArgb(100, 60, 20);
                rtf.SelectionBackColor = Color.FromArgb(255, 248, 235);
                rtf.AppendText("  " + t + "\r\n");
                rtf.SelectionBackColor = Color.White;
            }
            void Esp() { rtf.SelectionFont = new Font("Segoe UI", 5f); rtf.AppendText("\r\n"); }

            // Renderizar secciones
            Titulo(titulo, C1);
            foreach (var s in secciones)
            {
                switch (s.Tipo)
                {
                    case "sec":  Sec(s.Texto, s.Color); break;
                    case "p":    P(s.Texto); break;
                    case "pb":   PB(s.Negrita, s.Texto); break;
                    case "paso": Paso(s.Numero, s.Texto); break;
                    case "bul":  Bul(s.Texto, s.Color); break;
                    case "f":    F(s.Texto); break;
                    case "esp":  Esp(); break;
                }
            }

            rtf.SelectionStart = 0; rtf.ScrollToCaret();

            // ── Panel de pregunta libre a Ollama ──────────────────────────
            var pnlIA = new Panel
            {
                Dock = DockStyle.Bottom, Height = 54,
                BackColor = Color.FromArgb(240, 244, 255)
            };
            pnlIA.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(190, 210, 245), 1f);
                e.Graphics.DrawLine(pen, 0, 0, pnlIA.Width, 0);
            };

            // Fila de entrada (siempre visible)
            var pnlRow = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent };
            var lblIA  = new Label
            {
                Text = "💬 Preguntar a la IA:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = C1, AutoSize = true
            };
            var txtPregunta = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            var btnPreguntar = new Button
            {
                Text = "Preguntar",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(83, 74, 183), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnPreguntar.FlatAppearance.BorderSize = 0;
            btnPreguntar.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 52, 145);

            pnlRow.Layout += (s, e) =>
            {
                int btnW = 110, pad = 12, gap = 8;
                int lblW = lblIA.PreferredWidth;
                lblIA.Location      = new Point(pad, (pnlRow.Height - lblIA.PreferredHeight) / 2);
                int txtX = pad + lblW + gap;
                int txtW = pnlRow.Width - txtX - btnW - gap - pad;
                txtPregunta.Location = new Point(txtX, (pnlRow.Height - txtPregunta.Height) / 2);
                txtPregunta.Width    = Math.Max(80, txtW);
                btnPreguntar.Location = new Point(pnlRow.Width - btnW - pad,
                                                   (pnlRow.Height - 30) / 2);
                btnPreguntar.Size = new Size(btnW, 30);
            };
            pnlRow.Controls.AddRange(new Control[] { lblIA, txtPregunta, btnPreguntar });

            // Área de respuesta (oculta inicialmente)
            var rtfRespIA = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(240, 244, 255),
                Font = new Font("Segoe UI", 10f),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(12, 4, 12, 4),
                Visible = false
            };

            // Fill primero, luego Top (orden correcto para WinForms)
            pnlIA.Controls.Add(rtfRespIA);
            pnlIA.Controls.Add(pnlRow);

            // Texto plano del contenido estático para contexto Ollama
            string contextoModulo = _ayudas.ContainsKey(ventana)
                ? string.Join("\n", _ayudas[ventana].Secciones
                    .Select(sec => ((sec.Negrita ?? "") + " " + (sec.Texto ?? "")).Trim())
                    .Where(t => t.Length > 1))
                : "";

            async void PreguntarClick(object sender, EventArgs ev)
            {
                string pregunta = txtPregunta.Text.Trim();
                if (string.IsNullOrEmpty(pregunta)) return;

                btnPreguntar.Enabled = false;
                btnPreguntar.Text    = "…";
                rtfRespIA.Visible    = true;
                pnlIA.Height         = 220;
                rtfRespIA.Text       = "⏳  Consultando Ollama…";

                try
                {
                    bool disponible = await OllamaService.IsAvailableAsync();
                    if (!disponible)
                    {
                        rtfRespIA.Text =
                            "⚠  Ollama no está disponible en http://localhost:11434.\n" +
                            "Asegúrate de que el servidor Ollama esté corriendo y que " +
                            "tengas un modelo descargado (ej: ollama pull llama3).";
                        return;
                    }

                    string modelo = DatabaseService.ObtenerConfig("OllamaModelo", "llama3");
                    string prompt =
                        $"Eres un asistente de soporte para el sistema de gestión empresarial " +
                        $"venezolano \"SistemaGestion\". El usuario está en el módulo \"{titulo}\".\n\n" +
                        $"Documentación del módulo:\n{contextoModulo}\n\n" +
                        $"Pregunta del usuario: {pregunta}\n\n" +
                        $"Responde de forma concisa y práctica en español. Máximo 120 palabras.";

                    string respuesta = await OllamaService.GenerateAsync(modelo, prompt, 300);
                    rtfRespIA.Text = respuesta;
                }
                catch (Exception ex)
                {
                    rtfRespIA.Text = $"⚠  Error al contactar Ollama: {ex.Message}";
                }
                finally
                {
                    btnPreguntar.Enabled = true;
                    btnPreguntar.Text    = "Preguntar";
                }
            }

            btnPreguntar.Click += PreguntarClick;
            txtPregunta.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; PreguntarClick(s, e); }
            };
            // ─────────────────────────────────────────────────────────────

            var btnCerrar = new Button {
                Text = "Cerrar", Dock = DockStyle.Bottom, Height = 42,
                BackColor = C1, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => frm.Close();
            frm.Controls.AddRange(new Control[] { rtf, pnlIA, btnCerrar, hdr });

            VentanaPrefs.Restaurar(frm);
            frm.ShowDialog(owner);
            frm.Dispose();
        }

        // ── Modelo de sección ─────────────────────────────────────────────────
        private class S
        {
            public string Tipo; public string Texto; public string Negrita;
            public Color? Color; public int Numero;
            public static S Sec(string t, Color? c=null) => new S{Tipo="sec",Texto=t,Color=c};
            public static S P(string t)                   => new S{Tipo="p",Texto=t};
            public static S PB(string neg, string n="")   => new S{Tipo="pb",Negrita=neg,Texto=n};
            public static S Paso(int n, string t)         => new S{Tipo="paso",Numero=n,Texto=t};
            public static S Bul(string t,Color? c=null)   => new S{Tipo="bul",Texto=t,Color=c};
            public static S F(string t)                   => new S{Tipo="f",Texto=t};
            public static S Esp()                         => new S{Tipo="esp"};
        }

        private static readonly Color CRojo   = Color.FromArgb(180, 50, 50);
        private static readonly Color CVerde  = Color.FromArgb(26, 110, 58);
        private static readonly Color CNar    = Color.FromArgb(140, 80, 10);

        // ════════════════════════════════════════════════════════════════════
        //  CATÁLOGO DE AYUDAS
        // ════════════════════════════════════════════════════════════════════
        private static readonly Dictionary<string, (string Titulo, List<S> Secciones)> _ayudas
            = new Dictionary<string, (string, List<S>)>
        {
            // ── FrmInventario ────────────────────────────────────────────────
            ["FrmInventario"] = ("Inventario de Productos", new List<S> {
                S.Sec("¿Qué es este módulo?"),
                S.P("Catálogo maestro de todos los productos terminados fabricados. Aquí se " +
                    "definen los códigos, descripciones, precios, costos y stock mínimo. " +
                    "Los movimientos reales de stock se hacen desde el Kardex."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nuevo]  ", "Abre el formulario para crear un nuevo producto en el catálogo. " +
                     "Requiere código único, descripción, unidad y costo. Solo Administrador."),
                S.PB("[Editar]  ", "Modifica los datos del producto seleccionado en la lista: " +
                     "descripción, precios, costos, peso y stock mínimo."),
                S.PB("[Archivar]  ", "Desactiva el producto seleccionado. No se elimina; queda " +
                     "en el historial pero no aparece en los combos de venta. Solo Administrador."),
                S.PB("[Modo Edición]  ", "Activa la edición directa en la grilla. Permite modificar " +
                     "celdas haciendo doble clic. Confirmar con Enter. Solo Administrador."),
                S.Esp(),
                S.Sec("Paso a paso — Agregar un producto nuevo"),
                S.Paso(1, "Clic en [+ Nuevo]"),
                S.Paso(2, "Código: identificador único (ej: 'SL-001', 'EST-002')"),
                S.Paso(3, "Descripción: nombre completo del producto"),
                S.Paso(4, "Departamento y Unidad (und, kg, m, etc.)"),
                S.Paso(5, "Costo actual y anterior (para comparación)"),
                S.Paso(6, "Precio de venta, existencia mínima y peso en kg"),
                S.Paso(7, "Clic en [Guardar]"),
                S.Esp(),
                S.Sec("Notas importantes"),
                S.Bul("El stock NO se modifica aquí — se registra desde el Kardex", CRojo),
                S.Bul("El buscador filtra por código, descripción y departamento simultáneamente"),
                S.Bul("Filas en rojo = stock por debajo del mínimo definido"),
                S.Bul("Exportar a Excel desde el menú de reportes"),
            }),

            // ── FrmClientes ──────────────────────────────────────────────────
            ["FrmClientes"] = ("Clientes", new List<S> {
                S.Sec("¿Qué es este módulo?"),
                S.P("Directorio maestro de todos los clientes. Desde aquí se crean, " +
                    "editan y eliminan los clientes que aparecen en las facturas."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nuevo]  ", "Crea un nuevo cliente. Requiere código, nombre y tipo " +
                     "(natural/jurídico). El RIF/Cédula es obligatorio para facturación."),
                S.PB("[Editar]  ", "Modifica los datos del cliente seleccionado: " +
                     "nombre, dirección, teléfono, email, crédito y condiciones de pago."),
                S.PB("[Eliminar]  ", "Elimina el cliente del sistema. No se puede eliminar " +
                     "un cliente que tenga facturas registradas."),
                S.Esp(),
                S.Sec("Paso a paso — Agregar un cliente"),
                S.Paso(1, "Clic en [+ Nuevo]"),
                S.Paso(2, "Código: asignado automáticamente (o personalizado)"),
                S.Paso(3, "Nombre completo o razón social"),
                S.Paso(4, "RIF o Cédula (sin guiones ni puntos)"),
                S.Paso(5, "Tipo: Natural (persona) o Jurídico (empresa)"),
                S.Paso(6, "Teléfono, email y dirección (opcionales pero recomendados)"),
                S.Paso(7, "Límite de crédito y días de plazo si aplica"),
                S.Paso(8, "Clic en [Guardar]"),
                S.Esp(),
                S.Sec("Notas importantes"),
                S.Bul("El filtro busca por código, nombre y RIF simultáneamente"),
                S.Bul("Doble clic en la fila abre el formulario de edición"),
                S.Bul("Los clientes eliminados no pueden recuperarse", CRojo),
            }),

            // ── FrmProveedores ───────────────────────────────────────────────
            ["FrmProveedores"] = ("Proveedores", new List<S> {
                S.Sec("¿Qué es este módulo?"),
                S.P("Directorio de proveedores de materias primas, insumos y servicios. " +
                    "Los proveedores están vinculados a los insumos del Kardex A."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nuevo]  ", "Crea un nuevo proveedor. Solo Administrador. Requiere " +
                     "código único, nombre/razón social y RIF."),
                S.PB("[Editar]  ", "Modifica los datos del proveedor seleccionado: " +
                     "contacto, dirección, teléfonos, NIT y condiciones comerciales."),
                S.PB("[Eliminar]  ", "Elimina el proveedor. Solo si no tiene insumos " +
                     "vinculados. Solo Administrador."),
                S.Esp(),
                S.Sec("Paso a paso — Agregar un proveedor"),
                S.Paso(1, "Clic en [+ Nuevo]"),
                S.Paso(2, "Código: identificador corto (ej: 'PETRO', 'INDS-01')"),
                S.Paso(3, "Descripción / Razón social completa"),
                S.Paso(4, "RIF y NIT si aplica"),
                S.Paso(5, "Representante de ventas y teléfonos"),
                S.Paso(6, "Dirección y observaciones"),
                S.Paso(7, "Clic en [Guardar]"),
                S.Esp(),
                S.Sec("Notas importantes"),
                S.Bul("Vincular proveedor a los insumos desde Archivos → Insumos Proveedores"),
                S.Bul("Doble clic en la fila abre el formulario de edición"),
                S.Bul("El filtro busca por código, nombre y RIF"),
            }),

            // ── FrmFacturas ──────────────────────────────────────────────────
            ["FrmFacturas"] = ("Facturas", new List<S> {
                S.Sec("¿Qué es este módulo?"),
                S.P("Registro y consulta de todas las facturas de venta. Al guardar una " +
                    "factura el sistema descuenta automáticamente el stock del inventario " +
                    "y registra la cuenta por cobrar si aplica crédito."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nueva]  ", "Abre el formulario para crear una factura nueva. " +
                     "Se selecciona el cliente, los productos y las cantidades."),
                S.PB("[Ver detalle]  ", "Muestra el detalle completo de la factura " +
                     "seleccionada: líneas, precios, tasas de cambio e historial."),
                S.PB("[Exportar PDF]  ", "Genera un PDF de la factura seleccionada " +
                     "listo para enviar por correo o imprimir."),
                S.PB("[Imprimir]  ", "Envía directamente la factura a la impresora " +
                     "configurada como predeterminada."),
                S.PB("[Reporte PDF]  ", "Genera un reporte PDF del período filtrado " +
                     "con resumen de ventas por cliente y producto."),
                S.PB("[Cancelar]  ", "Cancela la factura seleccionada. Revierte el " +
                     "descuento del inventario. Requiere motivo. Solo Administrador."),
                S.Esp(),
                S.Sec("Paso a paso — Emitir una factura"),
                S.Paso(1, "Clic en [+ Nueva]"),
                S.Paso(2, "Seleccionar el cliente del combo (o buscarlo por nombre)"),
                S.Paso(3, "Verificar la tasa de cambio BCV/Paralelo del día"),
                S.Paso(4, "Agregar productos: seleccionar código → cantidad → precio"),
                S.Paso(5, "Revisar el total en USD y Bs."),
                S.Paso(6, "Seleccionar condición de pago (contado / crédito X días)"),
                S.Paso(7, "Clic en [Guardar Factura] — se descuenta el inventario automáticamente"),
                S.Paso(8, "Exportar o imprimir el PDF para entregarlo al cliente"),
                S.Esp(),
                S.Sec("Notas importantes"),
                S.Bul("Al guardar, el stock se descuenta inmediatamente del Kardex B"),
                S.Bul("Las facturas a crédito crean una cuenta por cobrar automáticamente"),
                S.Bul("Cancelar una factura revierte el inventario", CRojo),
                S.Bul("El filtro busca por número, cliente y rango de fechas"),
            }),

            // ── FrmRespaldos ─────────────────────────────────────────────────
            ["FrmRespaldos"] = ("Respaldos de Base de Datos", new List<S> {
                S.Sec("¿Qué es este módulo?"),
                S.P("Gestión de copias de seguridad (respaldos) de la base de datos. " +
                    "Los respaldos se crean automáticamente al iniciar el sistema y " +
                    "se pueden crear manualmente en cualquier momento."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Respaldar ahora]  ", "Crea una copia de seguridad inmediata de " +
                     "toda la base de datos. Se guarda en la carpeta de respaldos con " +
                     "fecha y hora en el nombre del archivo."),
                S.PB("[Verificar]  ", "Comprueba la integridad de todos los archivos de " +
                     "respaldo existentes. Detecta archivos corruptos o alterados."),
                S.PB("[Abrir carpeta]  ", "Abre en el Explorador de Windows la carpeta " +
                     "donde se guardan todos los respaldos del sistema."),
                S.Esp(),
                S.Sec("Rutina de respaldo recomendada"),
                S.Paso(1, "El sistema crea un respaldo automático al iniciar cada día"),
                S.Paso(2, "Crear respaldo manual antes de operaciones importantes"),
                S.Paso(3, "Copiar la carpeta de respaldos a un disco externo semanalmente"),
                S.Paso(4, "Verificar integridad mensualmente con el botón [Verificar]"),
                S.Paso(5, "Mantener respaldos de los últimos 30 días mínimo"),
                S.Esp(),
                S.Sec("Información importante"),
                S.Bul("La carpeta de respaldos está en: %AppData%\\SistemaGestion\\Respaldos"),
                S.Bul("Cada respaldo se cifra automáticamente para proteger los datos"),
                S.Bul("Los respaldos anteriores a 30 días se eliminan automáticamente"),
                S.Bul("Ante cualquier problema: restaurar desde el respaldo más reciente", CRojo),
            }),

            // ── FrmRegistroProduccionDia ─────────────────────────────────────
            ["FrmRegistroProduccionDia"] = ("Registro de Producción Diaria", new List<S> {
                S.Sec("¿Qué hace este formulario?"),
                S.P("Registra la producción de cada día: cuántas piezas de primera y " +
                    "segunda calidad produjo cada molde, y cuántos kg de materia prima " +
                    "se consumieron. Al confirmar, el sistema actualiza automáticamente " +
                    "el Kardex A (descuenta insumos) y el Kardex B (suma producción)."),
                S.Esp(),
                S.Sec("Partes de la ventana"),
                S.PB("Barra superior:  ", "Fecha del registro, tipo de material (Virgen/Reciclado), " +
                     "observaciones del día y botón de guardar."),
                S.PB("Panel izquierdo:  ", "Tabla de moldes donde se registra cuántas piezas " +
                     "de primera y segunda calidad produjo cada molde."),
                S.PB("Panel derecho:  ", "Insumos utilizados: qué materia prima se consumió " +
                     "y cuántos kg (mínimo 1 insumo obligatorio)."),
                S.PB("Barra de resumen:  ", "Muestra los totales de primera, segunda, peso " +
                     "teórico, material y merma estimada en tiempo real."),
                S.Esp(),
                S.Sec("Paso a paso — Registrar producción del día"),
                S.Paso(1, "Seleccionar la fecha correcta (por defecto hoy)"),
                S.Paso(2, "Seleccionar tipo de material: Virgen o Reciclado"),
                S.Paso(3, "Clic en [+ Molde] para agregar cada molde que produjo"),
                S.Paso(4, "Seleccionar el molde del combo y escribir las piezas de primera"),
                S.Paso(5, "Escribir las piezas de segunda calidad (puede ser 0)"),
                S.Paso(6, "Repetir para todos los moldes que trabajaron ese día"),
                S.Paso(7, "En el panel derecho, seleccionar los insumos utilizados"),
                S.Paso(8, "Ingresar los kg de cada insumo consumido"),
                S.Paso(9, "Escribir observaciones si hubo algún problema o evento"),
                S.Paso(10, "Clic en [Guardar y Confirmar Merma] — se actualiza el Kardex"),
                S.Esp(),
                S.Sec("Notas importantes"),
                S.Bul("Al guardar: insumos bajan en Kardex A, productos suben en Kardex B"),
                S.Bul("El separador central se puede mover para ver mejor cada panel"),
                S.Bul("Verificar que la merma estimada sea razonable antes de guardar"),
                S.Bul("Un registro por día — si se equivoca avisar al administrador", CRojo),
            }),

            // ── FrmProcesarNomina ────────────────────────────────────────────
            ["FrmProcesarNomina"] = ("Nómina Semanal", new List<S> {
                S.Sec("¿Qué es este módulo?"),
                S.P("Cálculo y cierre de la nómina semanal de todos los empleados. " +
                    "El sistema calcula automáticamente el pago por piezas producidas " +
                    "para operarios de producción, y por ventas para el personal de oficina."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nuevo período]  ", "Abre un nuevo período de nómina para la semana " +
                     "actual. Importa automáticamente la producción del período."),
                S.PB("[Cerrar período]  ", "Cierra el período seleccionado. Una vez cerrado " +
                     "no se pueden modificar los montos. Genera el recibo definitivo."),
                S.PB("[Recibo PDF]  ", "Genera el comprobante de pago individual para " +
                     "el empleado seleccionado en la lista."),
                S.PB("[Plan. IVSS]  ", "Genera la planilla del IVSS (Seguro Social) " +
                     "con los aportes patronales y del trabajador del período."),
                S.PB("[Aportes PDF]  ", "Genera el resumen de todos los aportes patronales: " +
                     "IVSS, INCES, Paro Forzoso y Política Habitacional."),
                S.Esp(),
                S.Sec("Paso a paso — Procesar nómina semanal"),
                S.Paso(1, "Verificar que todos los registros de producción de la semana estén cargados"),
                S.Paso(2, "Clic en [+ Nuevo período] — seleccionar semana"),
                S.Paso(3, "Revisar el detalle de cada empleado en la tabla"),
                S.Paso(4, "Ajustar bonos o descuentos si aplica"),
                S.Paso(5, "Verificar la tasa de cambio del día de pago"),
                S.Paso(6, "Clic en [Cerrar período] para confirmar los montos"),
                S.Paso(7, "Imprimir recibos individuales con [Recibo PDF]"),
                S.Paso(8, "Generar planilla IVSS con [Plan. IVSS] para el trámite mensual"),
                S.Esp(),
                S.Sec("Notas importantes"),
                S.Bul("Los empleados de producción cobran por pieza producida de primera calidad"),
                S.Bul("Las segundas calidades tienen tarifa reducida según configuración"),
                S.Bul("Una vez cerrado el período no se puede reabrir", CRojo),
                S.Bul("La pestaña Aportes muestra el detalle patronal por empleado"),
            }),

            // ── FrmComparacion ───────────────────────────────────────────────
            ["FrmComparacion"] = ("Comparación de Períodos", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Compara ventas, producción y costos entre dos períodos distintos. " +
                    "Ideal para análisis mensual, trimestral o anual. Identifica " +
                    "tendencias, crecimientos y caídas por producto."),
                S.Esp(),
                S.Sec("Cómo usar"),
                S.Paso(1, "Seleccionar el Período 1 (ej: enero 2025)"),
                S.Paso(2, "Seleccionar el Período 2 (ej: enero 2026)"),
                S.Paso(3, "Clic en [Comparar] para generar el análisis"),
                S.Paso(4, "Revisar la tabla: diferencias en unidades, montos y porcentaje"),
                S.Paso(5, "Exportar a PDF para presentación o archivo"),
                S.Esp(),
                S.Sec("Indicadores en la tabla"),
                S.Bul("Verde = crecimiento respecto al período anterior", CVerde),
                S.Bul("Rojo = caída respecto al período anterior", CRojo),
                S.Bul("% Variación = ((Período 2 - Período 1) / Período 1) × 100"),
            }),

            // ── FrmFlujoCaja ─────────────────────────────────────────────────
            ["FrmFlujoCaja"] = ("Flujo de Caja — Proyección", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Proyecta el flujo de efectivo a 30, 60 y 90 días basándose en " +
                    "las cuentas por cobrar, cuentas por pagar y el saldo inicial ingresado."),
                S.Esp(),
                S.Sec("Cómo usar"),
                S.Paso(1, "Ingresar el saldo de efectivo disponible hoy (en USD)"),
                S.Paso(2, "El sistema carga automáticamente cobros y pagos pendientes"),
                S.Paso(3, "Revisar la proyección en la tabla por semana/mes"),
                S.Paso(4, "Identificar semanas con saldo negativo proyectado"),
                S.Paso(5, "Exportar a PDF para revisión gerencial"),
                S.Esp(),
                S.Sec("Interpretación"),
                S.Bul("Saldo proyectado positivo = capacidad de pago suficiente", CVerde),
                S.Bul("Saldo proyectado negativo = necesidad de financiamiento", CRojo),
                S.Bul("Incluye cobros de facturas a crédito y pagos a proveedores"),
            }),

            // ── FrmPuntoEquilibrio ───────────────────────────────────────────
            ["FrmPuntoEquilibrio"] = ("Punto de Equilibrio", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Calcula el punto de equilibrio: cuántas unidades deben venderse " +
                    "para cubrir todos los costos (fijos + variables) sin ganar ni perder."),
                S.Esp(),
                S.Sec("Cómo usar"),
                S.Paso(1, "Ingresar los costos fijos mensuales (alquiler, nómina fija, etc.)"),
                S.Paso(2, "El sistema carga el costo variable promedio del inventario"),
                S.Paso(3, "Ingresar el precio de venta promedio por unidad"),
                S.Paso(4, "Clic en [Calcular]"),
                S.Paso(5, "Ver el resultado: unidades mínimas y ventas mínimas en USD"),
                S.Esp(),
                S.Sec("Fórmula"),
                S.F("PE (unidades) = Costos Fijos ÷ (Precio Venta - Costo Variable)"),
                S.Esp(),
                S.Bul("Por debajo del PE = pérdida operativa", CRojo),
                S.Bul("Por encima del PE = ganancia operativa", CVerde),
            }),


            // ── FrmCompras ───────────────────────────────────────────────────
            ["FrmCompras"] = ("Registro de Compras", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Registro de todas las compras realizadas a proveedores: materias primas, " +
                    "insumos y materiales. Cada compra actualiza el stock de los insumos."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nueva]  ", "Registrar una nueva compra a proveedor con sus líneas de detalle."),
                S.PB("[Editar]  ", "Modificar los datos de la compra seleccionada."),
                S.PB("[Eliminar]  ", "Eliminar la compra seleccionada del registro."),
                S.Esp(),
                S.Sec("Paso a paso — Registrar una compra"),
                S.Paso(1, "Tener a mano la factura del proveedor"),
                S.Paso(2, "Clic en [+ Nueva]"),
                S.Paso(3, "Seleccionar el proveedor"),
                S.Paso(4, "Ingresar número de factura y fecha"),
                S.Paso(5, "Agregar líneas: seleccionar insumo, cantidad y precio unitario"),
                S.Paso(6, "Verificar el total y clic en [Guardar]"),
                S.Esp(),
                S.Sec("Notas importantes"),
                S.Bul("Al guardar una compra el stock del insumo se actualiza automáticamente"),
                S.Bul("El costo promedio ponderado del insumo se recalcula con cada compra"),
                S.Bul("Mantener el número de factura del proveedor para auditoría"),
            }),

            // ── FrmCuentasCobrar ─────────────────────────────────────────────
            ["FrmCuentasCobrar"] = ("Cuentas por Cobrar", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Gestión de las deudas de clientes con la empresa. Las facturas a crédito " +
                    "generan cuentas por cobrar automáticamente al guardarse."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nueva]  ", "Crear una cuenta por cobrar manualmente (sin factura)."),
                S.PB("[Editar]  ", "Modificar los datos de la cuenta seleccionada."),
                S.PB("[Reg. Pago]  ", "Registrar un abono o pago total de la cuenta seleccionada."),
                S.Esp(),
                S.Sec("Paso a paso — Registrar un cobro"),
                S.Paso(1, "Seleccionar la cuenta en la lista"),
                S.Paso(2, "Verificar el saldo pendiente"),
                S.Paso(3, "Clic en [Reg. Pago]"),
                S.Paso(4, "Ingresar el monto recibido"),
                S.Paso(5, "Confirmar — el saldo se actualiza automáticamente"),
                S.Esp(),
                S.Sec("Estados de las cuentas"),
                S.Bul("Amarillo = vigente (dentro del plazo)", Color.FromArgb(140,100,0)),
                S.Bul("Rojo = vencida (pasó la fecha límite)", Color.FromArgb(180,50,50)),
                S.Bul("Verde = saldada (pagada completamente)", Color.FromArgb(26,110,58)),
            }),

            // ── FrmCambioPrecios ─────────────────────────────────────────────
            ["FrmCambioPrecios"] = ("Cambio de Precios y Costos", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Herramienta para actualizar precios y costos del inventario de productos. " +
                    "Permite ajustes individuales o aplicar un porcentaje global a toda la lista."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Producto]  ", "Agregar un nuevo producto al listado de precios."),
                S.PB("[Editar]  ", "Modificar precio actual, precio nuevo, costo actual y costo nuevo."),
                S.PB("[Aplicar]  ", "Confirma y aplica todos los precios nuevos al inventario. " +
                     "Los precios actuales son reemplazados por los nuevos."),
                S.PB("[Eliminar]  ", "Eliminar el producto seleccionado de la lista."),
                S.Esp(),
                S.Sec("Ajuste por porcentaje"),
                S.P("El campo '% Ajuste global' permite calcular automáticamente precios nuevos " +
                    "aplicando un porcentaje a toda la lista. Positivo = subida, negativo = bajada."),
                S.Esp(),
                S.Sec("Paso a paso — Actualizar precios"),
                S.Paso(1, "Ingresar el % de ajuste (ej: 10 para subir 10%)"),
                S.Paso(2, "Clic en [Aplicar %] — se calculan los precios nuevos"),
                S.Paso(3, "Revisar los precios nuevos en la columna 'PrecioNuevo' (fondo amarillo)"),
                S.Paso(4, "Ajustar individualmente con [Editar] si alguno necesita corrección"),
                S.Paso(5, "Clic en [Aplicar] para confirmar todos los cambios"),
                S.Esp(),
                S.Bul("Los precios nuevos aparecen en amarillo hasta ser aplicados"),
                S.Bul("Una vez aplicados, el precio actual = precio nuevo del paso anterior"),
            }),

            // ── FrmCuentasPagar ──────────────────────────────────────────────
            ["FrmCuentasPagar"] = ("Cuentas por Pagar", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Control de las obligaciones de pago a proveedores. Permite registrar " +
                    "deudas, hacer seguimiento de vencimientos y registrar los pagos realizados."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nueva]  ", "Registrar una nueva obligación de pago a un proveedor."),
                S.PB("[Editar]  ", "Modificar los datos de la cuenta seleccionada."),
                S.PB("[Registrar Pago]  ", "Registrar un pago total o parcial de la cuenta."),
                S.PB("[Ver Pagos]  ", "Ver el historial de pagos de la cuenta seleccionada."),
                S.PB("[Eliminar]  ", "Eliminar la cuenta por pagar seleccionada."),
                S.PB("[PDF]  ", "Exportar el reporte de cuentas por pagar a PDF."),
                S.Esp(),
                S.Sec("Notas importantes"),
                S.Bul("Rojo = cuenta vencida, pago atrasado", Color.FromArgb(180,50,50)),
                S.Bul("Amarillo = próxima a vencer (menos de 7 días)", Color.FromArgb(140,100,0)),
                S.Bul("Verde = pagada completamente", Color.FromArgb(26,110,58)),
            }),

            // ── FrmPrestamos ─────────────────────────────────────────────────
            ["FrmPrestamos"] = ("Préstamos a Empleados", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Gestión de préstamos internos otorgados a empleados. El sistema descuenta " +
                    "automáticamente las cuotas semanales al procesar la nómina."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nuevo]  ", "Registrar un nuevo préstamo a un empleado activo."),
                S.PB("[Abonar]  ", "Registrar un abono manual al préstamo seleccionado."),
                S.PB("[Cancelar]  ", "Cancelar el préstamo seleccionado (marca como cancelado)."),
                S.Esp(),
                S.Sec("Paso a paso — Otorgar un préstamo"),
                S.Paso(1, "Clic en [+ Nuevo]"),
                S.Paso(2, "Seleccionar el empleado"),
                S.Paso(3, "Ingresar el monto total del préstamo en USD"),
                S.Paso(4, "Definir la cuota semanal de descuento"),
                S.Paso(5, "Agregar una descripción del motivo"),
                S.Paso(6, "Clic en [Registrar préstamo]"),
                S.Esp(),
                S.Sec("Notas importantes"),
                S.Bul("Las cuotas se descuentan automáticamente al procesar la nómina semanal"),
                S.Bul("El saldo se actualiza con cada cuota descontada"),
                S.Bul("Verde = saldado | Gris = cancelado", Color.FromArgb(26,110,58)),
            }),

            // ── FrmBeneficiosLOTTT ───────────────────────────────────────────
            ["FrmBeneficiosLOTTT"] = ("Beneficios Anuales — LOTTT", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Cálculo y registro de los beneficios anuales obligatorios según la LOTTT " +
                    "venezolana: Utilidades, Vacaciones, Bono Vacacional y Prestaciones Sociales."),
                S.Esp(),
                S.Sec("Artículos LOTTT aplicados"),
                S.PB("Art. 131 — Utilidades:  ", "Mínimo 15 días de salario normal. Pago en diciembre."),
                S.PB("Art. 190 — Vacaciones:  ", "15 días + 1 día adicional por año de servicio."),
                S.PB("Art. 192 — Bono vacacional:  ", "7 días + 1 día adicional por año de servicio."),
                S.PB("Art. 142 — Prestaciones:  ", "15 días por trimestre trabajado."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[Calcular beneficios]  ", "Calcula automáticamente los 4 beneficios para " +
                     "todos los empleados activos del año seleccionado."),
                S.PB("[Marcar pagado]  ", "Registra el pago del beneficio seleccionado en la tabla."),
                S.PB("[Eliminar]  ", "Eliminar el registro de beneficio seleccionado."),
                S.PB("[Reporte PDF]  ", "Generar reporte PDF de todos los beneficios del año."),
                S.Esp(),
                S.Sec("Paso a paso — Calcular beneficios anuales"),
                S.Paso(1, "Seleccionar el año en el filtro superior"),
                S.Paso(2, "Clic en [Calcular beneficios]"),
                S.Paso(3, "Ingresar la tasa BCV vigente cuando se solicite"),
                S.Paso(4, "El sistema calcula y registra los 4 beneficios por empleado"),
                S.Paso(5, "Al pagar cada beneficio, seleccionarlo y clic en [Marcar pagado]"),
                S.Paso(6, "Exportar reporte PDF para respaldo y firma"),
            }),

            // ── FrmControlCalidad ─────────────────────────────────────────────
            ["FrmControlCalidad"] = ("Control de Calidad", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Registro de inspecciones de calidad de la producción. Permite identificar " +
                    "lotes con defectos, registrar los tipos de fallas y hacer seguimiento."),
                S.Esp(),
                S.Sec("Botones del encabezado"),
                S.PB("[+ Nueva insp.]  ", "Registrar una nueva inspección de control de calidad."),
                S.PB("[Ver detalle]  ", "Ver el detalle completo de la inspección seleccionada."),
                S.PB("[Eliminar]  ", "Eliminar la inspección seleccionada."),
                S.PB("[Reporte PDF]  ", "Generar reporte PDF de inspecciones del período."),
                S.Esp(),
                S.Sec("Paso a paso — Registrar una inspección"),
                S.Paso(1, "Seleccionar el lote de producción a inspeccionar"),
                S.Paso(2, "Clic en [+ Nueva insp.]"),
                S.Paso(3, "Seleccionar el tipo de defecto si aplica"),
                S.Paso(4, "Ingresar la cantidad de piezas inspeccionadas y defectuosas"),
                S.Paso(5, "Agregar observaciones y el inspector responsable"),
                S.Paso(6, "Guardar la inspección"),
                S.Esp(),
                S.Bul("Las piezas de segunda calidad del Kardex se controlan aquí"),
                S.Bul("Exportar el reporte mensualmente para análisis de calidad"),
            }),

            // ── FrmLibroIVA ───────────────────────────────────────────────────
            ["FrmLibroIVA"] = ("Libro de IVA", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Generación del Libro de Ventas y Compras para la declaración de IVA. " +
                    "Consolida todas las facturas y compras del período seleccionado."),
                S.Esp(),
                S.Sec("Pestañas"),
                S.PB("Libro de Ventas:  ", "Todas las facturas emitidas en el período con base " +
                     "imponible, IVA débito fiscal y total."),
                S.PB("Libro de Compras:  ", "Todas las compras a proveedores con IVA crédito fiscal."),
                S.Esp(),
                S.Sec("Paso a paso — Generar el libro del período"),
                S.Paso(1, "Seleccionar el período (mes y año)"),
                S.Paso(2, "El sistema carga automáticamente las facturas y compras"),
                S.Paso(3, "Verificar los totales de IVA débito y crédito"),
                S.Paso(4, "Exportar a PDF para presentación ante el SENIAT"),
                S.Esp(),
                S.Bul("La alícuota de IVA se configura en Configuración del Sistema"),
                S.Bul("Verificar que todas las facturas del mes estén registradas antes de exportar"),
            }),

            // ── FrmRentabilidad ──────────────────────────────────────────────
            ["FrmRentabilidad"] = ("Análisis de Rentabilidad", new List<S> {
                S.Sec("¿Qué hace este módulo?"),
                S.P("Analiza la rentabilidad por producto: margen bruto, margen neto, " +
                    "rotación y valor del inventario. Identifica los productos más y " +
                    "menos rentables para tomar decisiones de producción y precios."),
                S.Esp(),
                S.Sec("Indicadores mostrados"),
                S.PB("Margen Bruto:  ", "(Precio Venta - Costo) / Precio Venta × 100"),
                S.PB("Rotación:  ", "Unidades vendidas / Stock promedio en el período"),
                S.PB("Valor Inventario:  ", "Stock actual × Costo promedio ponderado"),
                S.Esp(),
                S.Sec("Cómo usar"),
                S.Paso(1, "Seleccionar el período de análisis"),
                S.Paso(2, "Clic en [Calcular] para generar el análisis"),
                S.Paso(3, "Ordenar por columna haciendo clic en el encabezado"),
                S.Paso(4, "Exportar a PDF para presentación gerencial"),
                S.Esp(),
                S.Bul("Verde = margen > 20%  |  Amarillo = 10-20%  |  Rojo = < 10%"),
                S.Bul("Los costos se toman del Costo Promedio Ponderado del Kardex"),
            }),
        };
    }
}
