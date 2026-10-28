#nullable enable
using System;
using System.Linq;

namespace SistemaGestion
{
    /// <summary>
    /// Cédula / RIF de clientes. Lo usan la app de escritorio y la API web
    /// (archivo enlazado) para que ambas apliquen exactamente la misma regla.
    ///
    ///  - Normalizar: "10 100 000", "v10.100.000", "V-10100000" → "V-10100000".
    ///    Si no trae letra se asume V. Con 9 dígitos (RIF con dígito
    ///    verificador) queda "V-23601914-0" / "J-12345678-9".
    ///  - Clave: lo que se compara para no repetir clientes. Es la letra más
    ///    el número sin ceros a la izquierda y sin el dígito verificador del RIF,
    ///    así "V-23601914-0", "23.601.914" y "V23601914" son la misma persona.
    /// </summary>
    public static class ClienteDocumento
    {
        private const string LetrasRif = "VEJGC";   // con dígito verificador al final
        private const string LetrasValidas = "VEJGCP";

        private static bool Separar(string? texto, out char letra, out string digitos)
        {
            letra = 'V'; digitos = "";
            if (string.IsNullOrWhiteSpace(texto)) return false;
            string s = new string(texto.Trim().ToUpperInvariant()
                .Where(ch => !char.IsWhiteSpace(ch) && ch != '.' && ch != '-' && ch != ',' && ch != '_' && ch != '/')
                .ToArray());
            if (s.Length == 0) return false;
            if (char.IsLetter(s[0]))
            {
                if (!LetrasValidas.Contains(s[0])) return false;
                letra = s[0]; s = s.Substring(1);
            }
            if (s.Length == 0 || !s.All(char.IsDigit)) return false;
            digitos = s;
            return true;
        }

        /// <summary>Formato estándar para guardar. Si no se reconoce, devuelve el texto recortado.</summary>
        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            if (!Separar(texto, out char letra, out string dig)) return texto.Trim().ToUpperInvariant();
            if (LetrasRif.Contains(letra) && dig.Length == 9)
                return $"{letra}-{dig.Substring(0, 8)}-{dig[8]}";
            return $"{letra}-{dig}";
        }

        /// <summary>Clave de comparación ("" si no hay documento).</summary>
        public static string Clave(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            if (!Separar(texto, out char letra, out string dig))
                return new string(texto.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
            if (LetrasRif.Contains(letra) && dig.Length == 9) dig = dig.Substring(0, 8);
            dig = dig.TrimStart('0');
            return dig.Length == 0 ? "" : letra + dig;
        }
    }
}
