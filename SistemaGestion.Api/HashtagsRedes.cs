using System.Text.RegularExpressions;

namespace SistemaGestion.Api
{
    // ══════════════════════════════════════════════════════════════════════
    //  HashtagsRedes — arma el texto final de un post para cada red.
    //
    //  Instagram acepta como máximo 5 hashtags por publicación o reel
    //  (diciembre de 2025): cuentan los del texto y los agregados al final.
    //  Facebook no tiene ese límite.
    //
    //  Instagram usa, en este orden: los hashtags elegidos para Instagram
    //  (campo HashtagsIG, curados para el mercado objetivo) o, si no hay, los
    //  generales con los de la marca primero. Facebook usa los generales más
    //  los de Instagram que falten.
    // ══════════════════════════════════════════════════════════════════════
    public static class HashtagsRedes
    {
        public const int MaxInstagram = 5;

        // Un hashtag: '#' seguido de letras (con acentos), números o guion bajo.
        // No cuenta '#' pegado a una palabra (p. ej. "C#" o "pag#2").
        private static readonly Regex RxTag = new(@"(?<![\p{L}\p{N}_&/])#[\p{L}\p{N}_]+", RegexOptions.Compiled);

        public record Resultado(string Caption, List<string> Hashtags, List<string> Avisos);

        /// <summary>Normaliza un hashtag: agrega '#', quita espacios y signos. "" si queda vacío.</summary>
        public static string Normalizar(string? tag)
        {
            var s = (tag ?? "").Trim().TrimStart('#');
            s = Regex.Replace(s, @"[^\p{L}\p{N}_]", "");
            return s.Length == 0 ? "" : "#" + s;
        }

        /// <summary>
        /// Hashtags de un texto, sin repetir (sin distinguir mayúsculas), en orden.
        /// esLista = true para campos de hashtags: acepta también palabras sin '#'
        /// ("EHPSaddles, Riverina") si el campo no trae ninguno con '#'.
        /// </summary>
        public static List<string> Extraer(string? texto, bool esLista = false)
        {
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lista = new List<string>();
            if (string.IsNullOrWhiteSpace(texto)) return lista;
            foreach (Match m in RxTag.Matches(texto))
                if (vistos.Add(m.Value)) lista.Add(m.Value);
            // Hashtags escritos sin '#' (p. ej. "EHPSaddles, saddletree"): solo si el texto no tiene ninguno con '#'.
            if (lista.Count == 0 && esLista)
                foreach (var parte in texto.Split(new[] { ' ', ',', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = Normalizar(parte);
                    if (t != "" && vistos.Add(t)) lista.Add(t);
                }
            return lista;
        }

        /// <summary>Pone primero los hashtags de la marca (en el orden de la marca) y luego el resto.</summary>
        public static List<string> PriorizarMarca(List<string> tags, string? hashtagsBaseMarca)
        {
            var baseMarca = Extraer(hashtagsBaseMarca, true);
            var res = new List<string>();
            foreach (var b in baseMarca)
            {
                var t = tags.FirstOrDefault(x => x.Equals(b, StringComparison.OrdinalIgnoreCase));
                if (t != null) res.Add(t);
            }
            foreach (var t in tags)
                if (!res.Contains(t, StringComparer.OrdinalIgnoreCase)) res.Add(t);
            return res;
        }

        /// <summary>Texto final para Instagram: cuerpo + hashtags, con 5 hashtags como máximo en total.</summary>
        public static Resultado ParaInstagram(string cuerpo, string? hashtagsIG, string? hashtagsGenerales, string? hashtagsBaseMarca)
        {
            var avisos = new List<string>();
            cuerpo = (cuerpo ?? "").Trim();

            // 1) Hashtags que ya vienen dentro del texto: si pasan de 5, se quitan los sobrantes.
            var enCuerpo = Extraer(cuerpo);
            if (enCuerpo.Count > MaxInstagram)
            {
                var sobran = new HashSet<string>(enCuerpo.Skip(MaxInstagram), StringComparer.OrdinalIgnoreCase);
                cuerpo = RxTag.Replace(cuerpo, m => sobran.Contains(m.Value) ? "" : m.Value);
                cuerpo = Regex.Replace(cuerpo, @"[ \t]{2,}", " ").Trim();
                avisos.Add($"El texto traía {enCuerpo.Count} hashtags; se quitaron {enCuerpo.Count - MaxInstagram} para respetar el máximo de {MaxInstagram} de Instagram.");
                enCuerpo = enCuerpo.Take(MaxInstagram).ToList();
            }

            // 2) Candidatos: los elegidos para Instagram o, si no hay, los generales con los de la marca primero.
            var elegidos = Extraer(hashtagsIG, true);
            List<string> candidatos;
            if (elegidos.Count > 0) candidatos = elegidos;
            else
            {
                candidatos = PriorizarMarca(Extraer(hashtagsGenerales, true), hashtagsBaseMarca);
                if (candidatos.Count > MaxInstagram)
                    avisos.Add($"No hay hashtags elegidos para Instagram: se usan los primeros {MaxInstagram} de la lista general (los de la marca primero).");
            }
            if (elegidos.Count > MaxInstagram)
                avisos.Add($"Los hashtags para Instagram son {elegidos.Count}; solo se usan los primeros {MaxInstagram}.");

            int cupo = MaxInstagram - enCuerpo.Count;
            var agregados = candidatos
                .Where(t => !enCuerpo.Contains(t, StringComparer.OrdinalIgnoreCase))
                .Take(Math.Max(0, cupo)).ToList();

            var finales = enCuerpo.Concat(agregados).ToList();
            var caption = agregados.Count > 0 ? (cuerpo + "\n\n" + string.Join(" ", agregados)) : cuerpo;
            return new Resultado(caption, finales, avisos);
        }

        /// <summary>Texto final para Facebook: cuerpo + hashtags generales + los de Instagram que falten (sin límite).</summary>
        public static Resultado ParaFacebook(string cuerpo, string? hashtagsIG, string? hashtagsGenerales)
        {
            cuerpo = (cuerpo ?? "").Trim();
            var enCuerpo = Extraer(cuerpo);
            var todos = Extraer(hashtagsGenerales, true);
            foreach (var t in Extraer(hashtagsIG, true))
                if (!todos.Contains(t, StringComparer.OrdinalIgnoreCase)) todos.Add(t);
            var agregados = todos.Where(t => !enCuerpo.Contains(t, StringComparer.OrdinalIgnoreCase)).ToList();
            var caption = agregados.Count > 0 ? (cuerpo + "\n\n" + string.Join(" ", agregados)) : cuerpo;
            return new Resultado(caption, enCuerpo.Concat(agregados).ToList(), new List<string>());
        }

        /// <summary>Lista limpia (normalizada, sin repetir) como texto separado por espacios.</summary>
        public static string Unir(IEnumerable<string?> tags, int max = int.MaxValue)
        {
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var res = new List<string>();
            foreach (var t in tags)
            {
                var n = Normalizar(t);
                if (n != "" && vistos.Add(n)) res.Add(n);
                if (res.Count >= max) break;
            }
            return string.Join(" ", res);
        }
    }
}
