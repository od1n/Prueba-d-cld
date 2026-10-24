using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SistemaGestion
{
    /// <summary>
    /// Ordena una grilla al hacer clic en el encabezado de una columna (un clic
    /// ascendente, otro clic descendente). Sirve para grillas enlazadas a listas
    /// (incluidas listas de tipos anónimos, que la grilla no sabe ordenar sola).
    /// Reconoce números con formato ("$ 1.234,56", "12,5 %"), fechas (dd/MM/yyyy,
    /// MM/yyyy) y texto. Una fila cuyo primer valor sea "TOTAL" queda siempre al final.
    /// Conserva encabezados, anchos, alineaciones y columnas ocultas.
    /// </summary>
    public static class GridOrden
    {
        private class Estado { public string Columna = ""; public bool Asc = true; }

        public static void Habilitar(DataGridView g)
        {
            if (g.Tag is Estado) return;
            g.Tag = new Estado();
            g.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn c in g.Columns)
                    if (c.SortMode != DataGridViewColumnSortMode.Programmatic)
                        c.SortMode = DataGridViewColumnSortMode.Programmatic;
                MarcarTotal(g);
            };
            g.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left || e.ColumnIndex < 0) return;
                try { Ordenar(g, g.Columns[e.ColumnIndex]); }
                catch { /* si la lista no se puede ordenar, se deja como está */ }
            };
        }

        private static void Ordenar(DataGridView g, DataGridViewColumn col)
        {
            if (g.DataSource is not IList lista || lista.Count < 2) return;
            var est = (Estado)g.Tag;
            string prop = string.IsNullOrEmpty(col.DataPropertyName) ? col.Name : col.DataPropertyName;
            Type tipo = TipoElemento(lista);
            if (tipo == null) return;
            PropertyInfo pi = tipo.GetProperty(prop);
            if (pi == null) return;

            bool asc = est.Columna == prop ? !est.Asc : true;
            est.Columna = prop; est.Asc = asc;

            var items = lista.Cast<object>().ToList();
            PropertyInfo primera = tipo.GetProperties().FirstOrDefault();
            bool EsTotal(object o)
            {
                if (primera == null) return false;
                var v = primera.GetValue(o)?.ToString() ?? "";
                return v.Trim().StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase);
            }
            var totales = items.Where(EsTotal).ToList();
            var resto   = items.Where(o => !EsTotal(o)).ToList();

            var claves = resto.ToDictionary(o => o, o => Clave(pi.GetValue(o)));
            bool numerico = claves.Values.Where(k => k.Tipo != 0).All(k => k.Tipo == 1) && claves.Values.Any(k => k.Tipo == 1);
            bool fecha    = !numerico && claves.Values.Where(k => k.Tipo != 0).All(k => k.Tipo == 2) && claves.Values.Any(k => k.Tipo == 2);

            int Comparar(object a, object b)
            {
                var ka = claves[a]; var kb = claves[b];
                if (ka.Tipo == 0 && kb.Tipo == 0) return 0;
                if (ka.Tipo == 0) return 1;      // vacíos siempre al final
                if (kb.Tipo == 0) return -1;
                int r = numerico ? ka.Num.CompareTo(kb.Num)
                      : fecha    ? ka.Fecha.CompareTo(kb.Fecha)
                      : string.Compare(ka.Texto, kb.Texto, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
                return asc ? r : -r;
            }
            // Orden estable
            var ordenados = resto.Select((o, i) => (o, i))
                .OrderBy(x => x, Comparer<(object o, int i)>.Create((x, y) => { int c = Comparar(x.o, y.o); return c != 0 ? c : x.i.CompareTo(y.i); }))
                .Select(x => x.o).Concat(totales).ToList();

            // Guardar la apariencia de las columnas antes de volver a enlazar
            var guardado = g.Columns.Cast<DataGridViewColumn>().ToDictionary(c => c.Name, c => new
            {
                c.HeaderText, c.Width, c.FillWeight, c.Visible, c.DisplayIndex, c.Frozen, c.MinimumWidth,
                c.AutoSizeMode, Estilo = c.DefaultCellStyle.Clone()
            });
            int primeraFila = g.FirstDisplayedScrollingRowIndex;

            var nueva = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(tipo));
            foreach (var o in ordenados) nueva.Add(o);
            g.DataSource = nueva;

            foreach (var kv in guardado.OrderBy(k => k.Value.DisplayIndex))
            {
                if (!g.Columns.Contains(kv.Key)) continue;
                var c = g.Columns[kv.Key];
                c.HeaderText = kv.Value.HeaderText;
                c.AutoSizeMode = kv.Value.AutoSizeMode;
                c.MinimumWidth = kv.Value.MinimumWidth;
                c.FillWeight = kv.Value.FillWeight;
                if (g.AutoSizeColumnsMode != DataGridViewAutoSizeColumnsMode.Fill) c.Width = kv.Value.Width;
                c.Visible = kv.Value.Visible;
                c.DefaultCellStyle = kv.Value.Estilo;
                c.DisplayIndex = Math.Min(kv.Value.DisplayIndex, g.Columns.Count - 1);
                c.Frozen = kv.Value.Frozen;
                c.SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            foreach (DataGridViewColumn c in g.Columns) c.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (g.Columns.Contains(col.Name))
                g.Columns[col.Name].HeaderCell.SortGlyphDirection = asc ? SortOrder.Ascending : SortOrder.Descending;
            MarcarTotal(g);
            if (primeraFila >= 0 && g.Rows.Count > 0) g.FirstDisplayedScrollingRowIndex = 0;
        }

        private static void MarcarTotal(DataGridView g)
        {
            if (g.Rows.Count == 0 || g.Columns.Count == 0) return;
            var row = g.Rows[g.Rows.Count - 1];
            var v = row.Cells[0].Value?.ToString() ?? "";
            if (!v.Trim().StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase)) return;
            row.DefaultCellStyle.Font = new Font(g.Font, FontStyle.Bold);
            row.DefaultCellStyle.BackColor = Color.FromArgb(222, 234, 244);
        }

        private static Type TipoElemento(IList lista)
        {
            var t = lista.GetType();
            if (t.IsGenericType) return t.GetGenericArguments()[0];
            foreach (var o in lista) if (o != null) return o.GetType();
            return null;
        }

        private struct ClaveOrden { public int Tipo; public decimal Num; public DateTime Fecha; public string Texto; }

        private static readonly string[] FormatosFecha =
            { "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "MM/yyyy", "yyyy-MM-dd" };

        // Tipo: 0 = vacío, 1 = número, 2 = fecha, 3 = texto
        private static ClaveOrden Clave(object v)
        {
            switch (v)
            {
                case null: return new ClaveOrden { Tipo = 0 };
                case DateTime dt: return new ClaveOrden { Tipo = 2, Fecha = dt };
                case decimal d: return new ClaveOrden { Tipo = 1, Num = d };
                case double db: return new ClaveOrden { Tipo = 1, Num = (decimal)db };
                case float f: return new ClaveOrden { Tipo = 1, Num = (decimal)f };
                case int i: return new ClaveOrden { Tipo = 1, Num = i };
                case long l: return new ClaveOrden { Tipo = 1, Num = l };
                case bool b: return new ClaveOrden { Tipo = 3, Texto = b ? "1" : "0" };
            }
            string s = v.ToString()?.Trim() ?? "";
            if (s == "" || s == "—" || s == "-") return new ClaveOrden { Tipo = 0 };
            if (s == "∞") return new ClaveOrden { Tipo = 1, Num = decimal.MaxValue };

            if (DateTime.TryParseExact(s, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fe))
                return new ClaveOrden { Tipo = 2, Fecha = fe, Texto = s };

            var nf = CultureInfo.CurrentCulture.NumberFormat;
            string limpio = s.Replace(nf.CurrencySymbol, "").Replace("$", "").Replace("€", "").Replace("%", "")
                             .Replace("pts", "").Replace(" ", "").Replace(" ", "");
            if (limpio.EndsWith("m")) limpio = limpio.Substring(0, limpio.Length - 1);
            if (limpio.Length > 0 && (char.IsDigit(limpio[0]) || limpio[0] == '-' || limpio[0] == '+')
                && decimal.TryParse(limpio, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out var n))
                return new ClaveOrden { Tipo = 1, Num = n, Texto = s };

            return new ClaveOrden { Tipo = 3, Texto = s };
        }
    }
}
