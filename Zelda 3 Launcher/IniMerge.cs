using System.Text;

namespace Zelda_3_Launcher
{
    // Dopo un aggiornamento: aggiunge al zelda3.ini dell'utente le voci nuove del
    // zelda3.ini predefinito che ancora mancano (con i loro commenti, in fondo
    // alla sezione giusta). Le voci gia' presenti, i commenti e l'ordine del
    // file dell'utente non vengono toccati. Il file e' letto e scritto byte per
    // byte (Latin-1), quindi nessun carattere viene alterato.
    internal static class IniMerge
    {
        private static readonly Encoding kBytes = Encoding.Latin1;

        private static string Id(string section, string key) =>
            section.ToLowerInvariant() + "\0" + key.ToLowerInvariant();

        private static bool IsSection(string t) => t.StartsWith("[") && t.EndsWith("]");

        // Chiave di una riga "chiave = valore" (non commentata), oppure null
        private static string? KeyOf(string line)
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith("#") || t.StartsWith(";") || IsSection(t))
                return null;
            int eq = t.IndexOf('=');
            return eq > 0 ? t.Substring(0, eq).Trim() : null;
        }

        public static int AddMissingKeys(string userPath, string defaultsText)
        {
            var lines = File.ReadAllText(userPath, kBytes).Replace("\r\n", "\n").Split('\n').ToList();
            bool crlf = File.ReadAllText(userPath, kBytes).Contains("\r\n");

            var present = new HashSet<string>();
            string section = "";
            foreach (var l in lines)
            {
                var t = l.Trim();
                if (IsSection(t)) { section = t; continue; }
                var k = KeyOf(l);
                if (k != null) present.Add(Id(section, k));
            }

            int added = 0;
            section = "";
            var comments = new List<string>();
            foreach (var raw in defaultsText.Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.TrimEnd();
                var t = line.Trim();
                if (IsSection(t)) { section = t; comments.Clear(); continue; }
                if (t.Length == 0) { comments.Clear(); continue; }
                if (t.StartsWith("#") || t.StartsWith(";")) { comments.Add(line); continue; }
                var key = KeyOf(line);
                if (key != null && section != "" && !present.Contains(Id(section, key)))
                {
                    Insert(lines, section, comments, line);
                    present.Add(Id(section, key));
                    added++;
                }
                comments.Clear();
            }

            if (added > 0)
                File.WriteAllText(userPath, string.Join(crlf ? "\r\n" : "\n", lines), kBytes);
            return added;
        }

        // Inserisce commenti e voce in fondo alla sezione (creandola se manca)
        private static void Insert(List<string> lines, string section, List<string> comments, string entry)
        {
            int start = lines.FindIndex(l => l.Trim().Equals(section, StringComparison.OrdinalIgnoreCase));
            if (start < 0)
            {
                while (lines.Count > 0 && lines[^1].Trim().Length == 0)
                    lines.RemoveAt(lines.Count - 1);
                lines.Add("");
                lines.Add(section);
                start = lines.Count - 1;
            }
            int end = lines.FindIndex(start + 1, l => IsSection(l.Trim()));
            if (end < 0) end = lines.Count;
            while (end - 1 > start && lines[end - 1].Trim().Length == 0)
                end--;
            var block = new List<string> { "" };
            block.AddRange(comments);
            block.Add(entry);
            lines.InsertRange(end, block);
        }
    }
}
