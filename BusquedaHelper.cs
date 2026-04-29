// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: BusquedaHelper.cs  →  raíz del proyecto
//
// Helpers comunes para búsqueda y ordenamiento "humano" en listados:
//
//   • NaturalCompare(a, b): orden natural — "2" antes de "10", ignora
//     ceros a la izquierda. Resuelve el bug clásico donde códigos
//     numéricos se ordenan como cadenas: "1, 10, 11, 2, 20, 3..."
//
//   • ContieneFlexible(campo, query): búsqueda con tolerancia a ceros
//     a la izquierda y mayúsculas. Buscar "03" encuentra códigos "3",
//     "30", "103", etc. También ignora signos como guiones y espacios
//     en CI/RIF, teléfonos y campos similares.
//
// Centralizado aquí para que todas las pantallas con buscador
// (FrmClientes, FrmInventario, FrmProveedores, FrmProductosProveedor,
// FrmRentabilidad, FrmMoldes, FrmFacturas...) usen la misma lógica
// y se evite el bug de "no encuentro al cliente 3 si tipeo 03".
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaGestion
{
    public static class BusquedaHelper
    {
        // ── Comparador de orden natural ──────────────────────────────────────
        // Lee chunks de texto y de dígitos; los chunks de dígitos se comparan
        // numéricamente (ignorando ceros a la izquierda), los de texto por
        // valor case-insensitive ordinal.
        //
        // Ejemplos:
        //   NaturalCompare("3",   "10")  →  negativo  (3 < 10)
        //   NaturalCompare("003", "10")  →  negativo  (3 < 10, sin importar ceros)
        //   NaturalCompare("CLI-2",  "CLI-10") → negativo
        //   NaturalCompare("A",   "a")   →  0  (case-insensitive)
        public static int NaturalCompare(string a, string b)
        {
            a ??= ""; b ??= "";
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                bool da = char.IsDigit(a[i]);
                bool db = char.IsDigit(b[j]);
                if (da && db)
                {
                    // Leer chunks completos de dígitos
                    int ai = i; while (i < a.Length && char.IsDigit(a[i])) i++;
                    int bj = j; while (j < b.Length && char.IsDigit(b[j])) j++;
                    string na = a.Substring(ai, i - ai).TrimStart('0');
                    string nb = b.Substring(bj, j - bj).TrimStart('0');
                    if (na.Length == 0) na = "0";
                    if (nb.Length == 0) nb = "0";
                    if (na.Length != nb.Length) return na.Length - nb.Length;
                    int cmpN = string.CompareOrdinal(na, nb);
                    if (cmpN != 0) return cmpN;
                    // Si los números son iguales, gana el que tiene MENOS ceros
                    // a la izquierda (003 después de 3) — orden estable.
                    int cerosA = (i - ai) - na.Length;
                    int cerosB = (j - bj) - nb.Length;
                    if (cerosA != cerosB) return cerosA - cerosB;
                }
                else if (da != db)
                {
                    // Los dígitos van antes que las letras (criterio común)
                    return da ? -1 : 1;
                }
                else
                {
                    int cmp = char.ToUpperInvariant(a[i])
                              .CompareTo(char.ToUpperInvariant(b[j]));
                    if (cmp != 0) return cmp;
                    i++; j++;
                }
            }
            return (a.Length - i) - (b.Length - j);
        }

        // ── Comparer reutilizable ────────────────────────────────────────────
        public static readonly Comparer<string> NaturalComparer =
            Comparer<string>.Create(NaturalCompare);

        // ── Helpers de OrderBy con comparador natural ────────────────────────
        public static IOrderedEnumerable<T> OrderByNatural<T>(
            this IEnumerable<T> src, Func<T, string> sel) =>
            src.OrderBy(sel, NaturalComparer);

        public static IOrderedEnumerable<T> OrderByDescendingNatural<T>(
            this IEnumerable<T> src, Func<T, string> sel) =>
            src.OrderByDescending(sel, NaturalComparer);

        // ── Búsqueda flexible en un campo único ──────────────────────────────
        // Devuelve true si:
        //   • El campo (lower) contiene la query (lower)              → match clásico
        //   • Ambos son numéricos (puros dígitos): se comparan sin
        //     ceros a la izquierda, en cualquier sentido               → "03" matchea "3"
        //   • La query, normalizada (sin separadores), está contenida
        //     en el campo normalizado                                  → CI/RIF, tel.
        public static bool ContieneFlexible(string campo, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            if (string.IsNullOrEmpty(campo))      return false;

            string fLow = campo.ToLowerInvariant();
            string qLow = query.ToLowerInvariant().Trim();

            if (fLow.Contains(qLow)) return true;

            // Normalización numérica: quitar ceros a la izquierda en ambos
            // y comprobar que la versión "limpia" matchee.
            if (qLow.All(char.IsDigit) && qLow.Length > 0)
            {
                string qNum = qLow.TrimStart('0'); if (qNum.Length == 0) qNum = "0";
                // Buscar dentro del campo cualquier chunk numérico que matchee
                // sin ceros a la izquierda.
                int i = 0;
                while (i < fLow.Length)
                {
                    if (char.IsDigit(fLow[i]))
                    {
                        int ini = i;
                        while (i < fLow.Length && char.IsDigit(fLow[i])) i++;
                        string chunk = fLow.Substring(ini, i - ini).TrimStart('0');
                        if (chunk.Length == 0) chunk = "0";
                        if (chunk == qNum || chunk.Contains(qNum) || qNum.Contains(chunk))
                            return true;
                    }
                    else i++;
                }
            }

            // Normalización de signos (CI/RIF, teléfonos): quitar guiones,
            // puntos, espacios y paréntesis y comparar.
            string fNorm = QuitarSeparadores(fLow);
            string qNorm = QuitarSeparadores(qLow);
            if (qNorm.Length > 0 && fNorm.Contains(qNorm)) return true;

            return false;
        }

        // ── Búsqueda en múltiples campos (cualquiera matchea) ────────────────
        public static bool ContieneFlexibleEnAlguno(string query, params string[] campos)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            foreach (var c in campos)
                if (ContieneFlexible(c, query)) return true;
            return false;
        }

        private static string QuitarSeparadores(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch == '-' || ch == '.' || ch == ' ' ||
                    ch == '(' || ch == ')' || ch == '/' || ch == '\\') continue;
                sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
