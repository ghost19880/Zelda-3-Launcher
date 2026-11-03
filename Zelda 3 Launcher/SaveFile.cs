using System.Diagnostics;

namespace Zelda_3_Launcher
{
    // Salvataggio del gioco: zelda3\saves\sram.dat e' la memoria della cartuccia
    // (8 KB, uguale al file .srm degli emulatori SNES) con le tre partite della
    // schermata iniziale. Ogni partita occupa 0x500 byte, con una seconda copia
    // 0xF00 byte piu' avanti; la somma delle sue 0x280 parole deve fare 0x5A5A.
    // Anche i salvataggi rapidi (saves\save*.sav, slot del menu e F1-F10) hanno
    // dentro una copia della cartuccia: si aggiornano insieme, altrimenti caricarne
    // uno e salvare in gioco rimetterebbe la cartuccia di prima.
    internal static class SaveFile
    {
        public const int SramSize = 0x2000;
        private const int SlotSize = 0x500, MirrorOffset = 0xF00, NameOffset = 0x3D9, ValidOffset = 0x3E5;
        private const int RamSize = 0x20000, RamSaveData = 0xF000;

        public static string SavesDir => Path.Combine(Program.repoDir, "saves");
        public static string SramPath => Path.Combine(SavesDir, "sram.dat");
        // copia fatta da "Modifica salvataggio" (ripristinata da "Ripristina salvataggio")
        public static string BackupDir => Path.Combine(SavesDir, "backup_launcher");
        // copia della cartuccia di prima, fatta da "Importa salvataggio"
        public static string ImportBackupDir => Path.Combine(SavesDir, "backup_import");

        public static bool GameRunning()
        {
            try
            {
                var exe = Path.GetFullPath(Path.Combine(Program.repoDir, "zelda3.exe"));
                foreach (var p in Process.GetProcessesByName("zelda3"))
                {
                    try
                    {
                        if (string.Equals(p.MainModule?.FileName, exe, StringComparison.OrdinalIgnoreCase)) return true;
                    }
                    catch { return true; }  // non leggibile: meglio fermarsi
                }
            }
            catch { }
            return false;
        }

        //
        // Partite
        //

        public static bool SlotValid(byte[] sram, int slot, int baseOffset = 0)
        {
            int o = baseOffset + slot * SlotSize;
            if (BitConverter.ToUInt16(sram, o + ValidOffset) != 0x55AA) return false;
            ushort sum = 0;
            for (int i = 0; i < SlotSize; i += 2)
                sum += BitConverter.ToUInt16(sram, o + i);
            return sum == 0x5A5A;
        }

        private static void FixChecksum(byte[] data, int o)
        {
            ushort sum = 0;
            for (int i = 0; i < SlotSize - 2; i += 2)
                sum += BitConverter.ToUInt16(data, o + i);
            BitConverter.GetBytes((ushort)(0x5A5A - sum)).CopyTo(data, o + SlotSize - 2);
        }

        // Codici dei caratteri del nome (quelli della griglia del gioco)
        public static int[] ReadName(byte[] sram, int slot, int baseOffset = 0)
        {
            var name = new int[6];
            for (int i = 0; i < 6; i++)
            {
                int a = BitConverter.ToUInt16(sram, baseOffset + slot * SlotSize + NameOffset + i * 2);
                name[i] = a & 0xF | (a >> 1) & 0xF0;
            }
            return name;
        }

        private static void WriteName(byte[] data, int o, int[] name)
        {
            for (int i = 0; i < 6; i++)
            {
                int t = name[i];
                BitConverter.GetBytes((ushort)((t & 0xFFF0) * 2 + (t & 0xF))).CopyTo(data, o + NameOffset + i * 2);
            }
        }

        // Cuori (in ottavi) per mostrare le partite
        public static int Hearts(byte[] sram, int slot) => sram[slot * SlotSize + 0x36C] / 8;

        public static byte[]? ReadSram()
        {
            if (!File.Exists(SramPath)) return null;
            var b = File.ReadAllBytes(SramPath);
            return b.Length >= SramSize ? b[..SramSize] : null;
        }

        //
        // Nome
        //

        public const int Space = 0x59, LinkLeft = 0x4A, LinkRight = 0x4B;

        // Carattere scritto -> codice del gioco. Come nella griglia del gioco la
        // l minuscola e' la stessa I maiuscola (non c'e' una l a parte)
        public static int CharToCode(char c)
        {
            if (c == 'I' || c == 'l') return 0x5F;
            if (c == 'i') return 0x60;
            if (c >= 'A' && c <= 'Z') return c - 'A';
            if (c >= 'a' && c <= 'z') return 0x1A + c - 'a';
            if (c >= '0' && c <= '9') return 0x76 + c - '0';
            return c switch
            {
                ' ' => Space,
                '-' => 0x40,
                '.' => 0x41,
                ',' => 0x42,
                '!' => 0x61,
                '?' => 0x3F,
                '(' => 0x45,
                ')' => 0x46,
                _ => -1,
            };
        }

        // Codice -> testo da mostrare ('\0' per le due meta' dell'icona di Link)
        public static string CodeToText(int t)
        {
            if (t == 0x5F) return "I";
            if (t == 0x60) return "i";
            if (t >= 0 && t < 26) return ((char)('A' + t)).ToString();
            if (t >= 0x1A && t < 0x34) return ((char)('a' + t - 0x1A)).ToString();
            if (t >= 0x76 && t < 0x80) return ((char)('0' + t - 0x76)).ToString();
            return t switch
            {
                Space => " ",
                0x40 => "-",
                0x41 => ".",
                0x42 => ",",
                0x61 => "!",
                0x3F => "?",
                0x45 => "(",
                0x46 => ")",
                _ => "",
            };
        }

        public static string NameText(int[] name)
        {
            var s = "";
            foreach (int t in name)
                s += t == LinkLeft ? "[Link]" : t == LinkRight ? "" : CodeToText(t);
            return s.TrimEnd();
        }

        //
        // Scrittura
        //

        public static void SetName(byte[] sram, int slot, int[] name) => WriteName(sram, slot * SlotSize, name);

        // Scrive le partite modificate (edited, letto da sram.dat in original):
        // in sram.dat e, solo i byte cambiati, nelle copie dei salvataggi rapidi
        // (anche nella RAM, se in quel salvataggio si sta giocando quella
        // partita: il resto della partita in corso resta com'e'). Ritorna quanti
        // salvataggi rapidi sono stati aggiornati.
        public static int Save(byte[] original, byte[] edited)
        {
            var sram = File.ReadAllBytes(SramPath);
            var slots = new List<int>();
            for (int s = 0; s < 3; s++)
            {
                int o = s * SlotSize;
                bool changed = false;
                for (int i = 0; i < SlotSize - 2 && !changed; i++)
                    changed = original[o + i] != edited[o + i];
                if (!changed) continue;
                slots.Add(s);
                Array.Copy(edited, o, sram, o, SlotSize);
                FixChecksum(sram, o);
                Array.Copy(sram, o, sram, o + MirrorOffset, SlotSize);
            }
            if (slots.Count == 0) return 0;
            File.WriteAllBytes(SramPath, sram);

            int files = 0;
            foreach (var file in QuickSaves())
            {
                var b = File.ReadAllBytes(file);
                if (!QuickSaveLayout(b, out int sramAt, out int ramAt)) continue;
                bool touched = false;
                foreach (int s in slots)
                {
                    int o = s * SlotSize;
                    if (SlotValid(b, s, sramAt))
                    {
                        Patch(original, edited, o, b, sramAt + o);
                        Array.Copy(b, sramAt + o, b, sramAt + o + MirrorOffset, SlotSize);
                        touched = true;
                    }
                    // partita in corso nel salvataggio rapido (srm_var1 = slot * 2 + 2)
                    if (BitConverter.ToUInt16(b, sramAt + 0x1FFE) == s * 2 + 2)
                    {
                        Patch(original, edited, o, b, ramAt + RamSaveData);
                        touched = true;
                    }
                }
                if (touched)
                {
                    File.WriteAllBytes(file, b);
                    files++;
                }
            }
            return files;
        }

        private static void Patch(byte[] original, byte[] edited, int from, byte[] dest, int to)
        {
            for (int i = 0; i < SlotSize - 2; i++)
                if (original[from + i] != edited[from + i])
                    dest[to + i] = edited[from + i];
            FixChecksum(dest, to);
        }

        // Cartuccia importata: diventa sram.dat e sostituisce le tre partite nelle
        // copie dei salvataggi rapidi (la partita in corso di ciascuno resta)
        public static void Import(byte[] srm)
        {
            var sram = srm[..SramSize];
            File.WriteAllBytes(SramPath, sram);
            foreach (var file in QuickSaves())
            {
                var b = File.ReadAllBytes(file);
                if (!QuickSaveLayout(b, out int sramAt, out _)) continue;
                Array.Copy(sram, 0, b, sramAt, 3 * SlotSize + MirrorOffset);
                File.WriteAllBytes(file, b);
            }
        }

        private static IEnumerable<string> QuickSaves() =>
            Directory.Exists(SavesDir) ? Directory.EnumerateFiles(SavesDir, "*.sav") : Enumerable.Empty<string>();

        // Nel salvataggio rapido la RAM (128 KB) sta in fondo, prima di 4 byte;
        // la cartuccia 58 byte prima della RAM. Si accetta solo se almeno una
        // partita in quel punto ha la somma giusta
        private static bool QuickSaveLayout(byte[] b, out int sramAt, out int ramAt)
        {
            ramAt = b.Length - 4 - RamSize;
            sramAt = ramAt - 58 - SramSize;
            if (sramAt < 0) return false;
            for (int s = 0; s < 3; s++)
                if (SlotValid(b, s, sramAt)) return true;
            return false;
        }

        //
        // Copie di sicurezza
        //

        private static void CopyAll(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (var f in Directory.EnumerateFiles(to)) File.Delete(f);
            foreach (var f in Directory.EnumerateFiles(from))
            {
                var name = Path.GetFileName(f);
                if (name.Equals("sram.dat", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
                    File.Copy(f, Path.Combine(to, name), true);
            }
        }

        public static void Backup(string dir) => CopyAll(SavesDir, dir);

        public static bool HasBackup => File.Exists(Path.Combine(BackupDir, "sram.dat"));

        public static DateTime BackupTime => File.GetLastWriteTime(Path.Combine(BackupDir, "stamp"));

        // Copia per "Ripristina salvataggio", con l'ora in cui e' stata fatta
        public static void BackupForEditor()
        {
            Backup(BackupDir);
            File.WriteAllText(Path.Combine(BackupDir, "stamp"), DateTime.Now.ToString("s"));
        }

        // Rimette i file della copia; i salvataggi rapidi fatti dopo restano
        public static void RestoreBackup()
        {
            foreach (var f in Directory.EnumerateFiles(BackupDir))
            {
                var name = Path.GetFileName(f);
                if (name == "stamp") continue;
                File.Copy(f, Path.Combine(SavesDir, name), true);
            }
        }
    }
}
