using System.Runtime.CompilerServices;
using System.Text;
using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;
using LibGit2Sharp;

namespace Zelda_3_Launcher
{
    // Lettura e scrittura di zelda3.ini senza mai rovinarlo:
    // - si legge come lo legge il gioco: '#' apre un commento ovunque, le righe
    //   con ';' o senza '=' si ignorano, maiuscole e minuscole non contano e con
    //   una chiave ripetuta vale l'ultima (niente piu' eccezioni del parser);
    // - si scrive aggiornando solo le righe dei valori cambiati rispetto a
    //   quando il file e' stato letto: commenti, ordine, spazi, chiavi che il
    //   launcher non conosce e valori cambiati nel frattempo dal gioco restano
    //   come sono. Le chiavi mancanti vanno in fondo alla loro sezione;
    // - il file si legge e si scrive byte per byte (Latin-1) e si sostituisce
    //   solo a scrittura riuscita, quindi non resta mai a meta'.
    internal static class IniFile
    {
        public static readonly Encoding kBytes = Encoding.Latin1;

        // Il gioco usa zelda3.user.ini al posto di zelda3.ini quando esiste
        public static string UserIni
        {
            get
            {
                var user = Path.Combine(Program.repoDir, "zelda3.user.ini");
                return File.Exists(user) ? user : Path.Combine(Program.repoDir, "zelda3.ini");
            }
        }

        private static readonly string[] kSections = { "General", "Graphics", "Sound", "Features", "KeyMap", "GamepadMap" };
        private static readonly ConditionalWeakTable<IniData, IniData> baselines = new();

        private static IniParserConfiguration Config() => new IniParserConfiguration
        {
            CommentString = "#",
            AllowDuplicateKeys = true,
            OverrideDuplicateKeys = true,
            AllowDuplicateSections = true,
            SkipInvalidLines = true,
            CaseInsensitive = true,
        };

        // La riga come la vede il gioco
        private static string Clean(string line)
        {
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash);
            var t = line.Trim();
            if (t.StartsWith(";")) return "";
            if (t.StartsWith("[")) return t.EndsWith("]") ? t : "";
            return t.Contains('=') ? line : "";
        }

        public static IniData Parse(string text)
        {
            var clean = string.Join("\n", text.Replace("\r\n", "\n").Split('\n').Select(Clean));
            var data = new IniDataParser(Config()).Parse(clean);
            foreach (var s in kSections)
                if (!data.Sections.ContainsSection(s)) data.Sections.AddSection(s);
            return data;
        }

        // Legge il file e ricorda i valori letti, per scrivere poi solo quelli cambiati
        public static IniData Load(string path)
        {
            var data = Parse(File.ReadAllText(path, kBytes));
            baselines.AddOrUpdate(data, (IniData)data.Clone());
            return data;
        }

        public static void Save(string path, IniData data)
        {
            baselines.TryGetValue(data, out var baseline);
            // value null = chiave tolta (RemoveKey): si toglie dal file
            var changes = new List<(string section, string key, string? value, List<string> comments)>();
            if (baseline != null)
                foreach (var sec in baseline.Sections)
                    foreach (var kd in sec.Keys)
                        if (!data.Sections.ContainsSection(sec.SectionName) || !data[sec.SectionName].ContainsKey(kd.KeyName))
                            changes.Add((sec.SectionName, kd.KeyName, null, new List<string>()));
            foreach (var sec in data.Sections)
            {
                var old = baseline != null && baseline.Sections.ContainsSection(sec.SectionName) ? baseline[sec.SectionName] : null;
                foreach (var kd in sec.Keys)
                {
                    var value = kd.Value ?? "";
                    if (old != null && old.ContainsKey(kd.KeyName) && (old[kd.KeyName] ?? "") == value)
                        continue;
                    changes.Add((sec.SectionName, kd.KeyName, value, kd.Comments ?? new List<string>()));
                }
            }
            if (changes.Count > 0)
                WriteValues(path, changes);
            baselines.AddOrUpdate(data, (IniData)data.Clone());
        }

        private static bool IsSection(string t) => t.StartsWith("[") && t.EndsWith("]");

        // Aggiorna nel file le chiavi indicate (tutte le loro righe)
        private static void WriteValues(string path, List<(string section, string key, string? value, List<string> comments)> changes)
        {
            var text = File.Exists(path) ? File.ReadAllText(path, kBytes) : "";
            var nl = text.Contains("\r\n") ? "\r\n" : "\n";
            // ogni riga tiene il suo fine riga (il file puo' averli misti)
            var lines = new List<string>();
            var eols = new List<string>();
            for (int pos = 0; pos < text.Length;)
            {
                int n = text.IndexOf('\n', pos);
                if (n < 0) { lines.Add(text.Substring(pos)); eols.Add(""); break; }
                int e = n > pos && text[n - 1] == '\r' ? n - 1 : n;
                lines.Add(text.Substring(pos, e - pos));
                eols.Add(text.Substring(e, n + 1 - e));
                pos = n + 1;
            }
            void InsertLines(int at, IEnumerable<string> block)
            {
                if (at == lines.Count && eols.Count > 0 && eols[^1] == "") eols[^1] = nl;
                foreach (var b in block)
                {
                    lines.Insert(at, b);
                    eols.Insert(at++, nl);
                }
            }
            var done = new bool[changes.Count];
            var removed = new HashSet<int>();

            string section = "";
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var t = line.Trim();
                if (IsSection(Clean(line).Trim()))
                {
                    section = Clean(line).Trim();
                    section = section.Substring(1, section.Length - 2).Trim();
                    continue;
                }
                if (t.StartsWith("#") || t.StartsWith(";")) continue;
                int eq = line.IndexOf('=');
                int hash = line.IndexOf('#');
                if (eq < 0 || (hash >= 0 && hash < eq)) continue;
                var key = line.Substring(0, eq).Trim();
                for (int k = 0; k < changes.Count; k++)
                {
                    if (!changes[k].section.Equals(section, StringComparison.OrdinalIgnoreCase) ||
                        !changes[k].key.Equals(key, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (changes[k].value == null)
                    {
                        // la riga, i commenti subito sopra e la riga vuota che li separava
                        removed.Add(i);
                        int j = i - 1;
                        while (j >= 0 && lines[j].Trim().StartsWith("#")) removed.Add(j--);
                        if (j >= 0 && lines[j].Trim().Length == 0 && (i + 1 >= lines.Count || lines[i + 1].Trim().Length == 0))
                            removed.Add(j);
                        done[k] = true;
                        break;
                    }
                    var indent = line.Substring(0, line.Length - line.TrimStart().Length);
                    var comment = hash >= 0 ? " " + line.Substring(hash) : "";
                    lines[i] = indent + key + " = " + changes[k].value + comment;
                    done[k] = true;
                    break;
                }
            }

            foreach (var i in removed.OrderByDescending(x => x))
            {
                lines.RemoveAt(i);
                eols.RemoveAt(i);
            }

            // Chiavi non presenti: in fondo alla loro sezione, o in una sezione nuova
            for (int k = 0; k < changes.Count; k++)
            {
                if (done[k] || changes[k].value == null) continue;
                var (sec, key, value, comments) = changes[k];
                int start = lines.FindIndex(l =>
                {
                    var c = Clean(l).Trim();
                    return IsSection(c) && c.Substring(1, c.Length - 2).Trim().Equals(sec, StringComparison.OrdinalIgnoreCase);
                });
                var block = comments.Select(c => "#" + c).ToList();
                block.Add(key + " = " + value);
                if (start < 0)
                {
                    int at = lines.Count;
                    while (at > 0 && lines[at - 1].Trim().Length == 0) at--;
                    block.InsertRange(0, new[] { "", "[" + sec + "]" });
                    InsertLines(at, block);
                    continue;
                }
                int end = lines.FindIndex(start + 1, l => IsSection(Clean(l).Trim()));
                if (end < 0) end = lines.Count;
                while (end - 1 > start && lines[end - 1].Trim().Length == 0) end--;
                if (comments.Count > 0) block.Insert(0, "");
                InsertLines(end, block);
            }

            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
                sb.Append(lines[i]).Append(eols[i]);
            WriteAtomic(path, sb.ToString());
        }

        public static void WriteAtomic(string path, string text)
        {
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, text, kBytes);
            File.Move(tmp, path, true);
        }

        // Righe dell'ini predefinito che il launcher toglie o cambia alla prima
        // installazione (lo faceva gia' il launcher originale)
        private static readonly HashSet<string> kDropped = new()
        {
            "# Change the appearance of Link by loading a ZSPR file",
            "# See all sprites here: https://snesrev.github.io/sprites-gfx/snes/zelda3/link/",
            "# Download the files with \"git clone https://github.com/snesrev/sprites-gfx.git\"",
            "# LinkGraphics = sprites-gfx/snes/zelda3/link/sheets/megaman-x.2.zspr",
            "# This default is suitable for QWERTZ keyboards.",
            "#Controls = Up, Down, Left, Right, Right Shift, Return, x, y, s, a, c, v",
            "# This one is suitable for AZERTY keyboards.",
            "#Controls = Up, Down, Left, Right, Right Shift, Return, x, w, s, q, c, v",
        };

        // "# Language = de" diventa "Language = us" ("it" con Windows in italiano)
        // solo se nel file non c'e' gia' una lingua: altrimenti nascerebbero due
        // righe Language
        public static string Prepare(string text)
        {
            // si divide solo su '\n': ogni riga tiene il suo '\r' (fine riga misti)
            var lines = text.Split('\n');
            bool hasLanguage = Parse(text)["General"].ContainsKey("Language");
            var result = new List<string>();
            foreach (var raw in lines)
            {
                var line = raw.TrimEnd('\r');
                var cr = raw.Length != line.Length ? "\r" : "";
                if (line == "# Language = de")
                {
                    if (!hasLanguage) result.Add("Language = " + (Lang.WindowsItalian ? "it" : "us") + cr);
                    hasLanguage = true;
                    continue;
                }
                if (!kDropped.Contains(line)) result.Add(raw);
            }
            return string.Join("\n", result);
        }

        // Sistema l'ini dell'utente sul posto, senza spostarlo
        public static void PrepareInPlace(string path)
        {
            if (!File.Exists(path)) return;
            var text = File.ReadAllText(path, kBytes);
            var prepared = Prepare(text);
            if (prepared != text) WriteAtomic(path, prepared);
        }

        // L'ini predefinito della versione installata: dal repository del gioco,
        // altrimenti da GitHub. null se non si trova.
        public static string? DefaultsText()
        {
            try
            {
                using var repo = new Repository(Program.repoDir);
                if (repo.Head.Tip?["zelda3.ini"]?.Target is Blob blob)
                {
                    using var s = blob.GetContentStream();
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    return kBytes.GetString(ms.ToArray());
                }
            }
            catch { }
            try
            {
                using var client = new HttpClient();
                var bytes = client.GetByteArrayAsync("https://raw.githubusercontent.com/ghost19880/zelda3/master/zelda3.ini").Result;
                return kBytes.GetString(bytes);
            }
            catch { }
            return null;
        }

        // Rimette l'ini predefinito (Reset, ini mancante). false se non e' disponibile.
        public static bool RestoreDefaults(string path)
        {
            var defaults = DefaultsText();
            if (defaults == null)
            {
                Lang.Show("Unable to find the default zelda3.ini and unable to connect to the internet to download it.",
                    "No Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            WriteAtomic(path, Prepare(defaults));
            return true;
        }

        // Lingue con i file dei dialoghi e del font pronti negli asset
        public static string[] AvailableLanguages()
        {
            var assets = Path.Combine(Program.repoDir, "assets");
            string[] all = { "de", "fr", "fr-c", "en", "es", "pl", "pt", "redux", "nl", "sv", "it" };
            return all.Where(c =>
            {
                var f = c.Replace('-', '_');
                return File.Exists(Path.Combine(assets, "dialogue_" + f + ".txt")) &&
                       (c == "it" || File.Exists(Path.Combine(assets, "font_" + f + ".png")));
            }).ToArray();
        }
    }
}
