using System.Drawing.Drawing2D;
using System.Text;

namespace Zelda_3_Launcher
{
    // Anteprima di uno sprite ZSPR di Link: legge il file come fa il gioco
    // (ParseLinkGraphics in main.c) e disegna Link fermo nelle quattro
    // direzioni con la tunica verde, poi di fronte con quella blu, rossa e da
    // coniglio
    internal class LinkPreview : Control
    {
        private const int Zoom = 3, Gap = 6;
        private byte[]? pixels;      // 0x7000 byte: 896 caselle 8x8 a 4bpp, 16 per riga
        private Color[][]? palettes; // verde, blu, rossa, coniglio: 15 colori ciascuna
        private string name = "", author = "", error = "";
        private string path = "\0"; // nessun file letto ancora

        // Pose da ferme (kLinkDmaSources1/2 e kLinkSpriteBodys del gioco):
        // casella della testa 16x16, del corpo 16x16, spostamento x della testa, ribaltata
        private static readonly (int head, int body, int dx, bool flip)[] kPoses =
        {
            (2, 38, 0, false),   // di fronte
            (4, 66, 0, false),   // di spalle
            (0, 32, -1, false),  // a sinistra
            (0, 32, 1, true),    // a destra
            (810, 832, 0, false), // coniglio di fronte (yt 0x21, indice 286)
        };

        public LinkPreview()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.CardBack;
            Font = Theme.Body;
            UpdateHeight();
        }

        public void Load(string file)
        {
            file = file.Trim();
            if (file == path) return;
            path = file;
            pixels = null;
            palettes = null;
            name = author = error = "";
            if (file.Length == 0)
                error = "No sprite chosen.";
            else if (!File.Exists(file))
                error = "File not found.";
            else
            {
                try { Parse(File.ReadAllBytes(file)); }
                catch { error = "This is not a valid ZSPR sprite."; }
            }
            if (pixels == null) palettes = null;
            UpdateHeight();
            Invalidate();
        }

        private void Parse(byte[] f)
        {
            if (f.Length < 29 || Encoding.ASCII.GetString(f, 0, 4) != "ZSPR")
                throw new InvalidDataException();
            int pixOffs = BitConverter.ToInt32(f, 9), pixLen = BitConverter.ToUInt16(f, 13);
            int palOffs = BitConverter.ToInt32(f, 15), palLen = BitConverter.ToUInt16(f, 19);
            if (pixLen != 0x7000 || pixOffs < 0 || (long)pixOffs + pixLen > f.Length || palOffs < 0 || (long)palOffs + palLen > f.Length)
                throw new InvalidDataException();
            pixels = f.AsSpan(pixOffs, 0x7000).ToArray();
            // senza tavolozza il gioco tiene i colori originali: qui quelli di Link verde
            palettes = new Color[4][];
            for (int p = 0; p < 4; p++)
            {
                palettes[p] = new Color[16];
                for (int i = 0; i < 15; i++)
                {
                    int k = p * 30 + i * 2;
                    int c = palLen >= 120 ? BitConverter.ToUInt16(f, palOffs + k) : kDefaultGreen[i];
                    palettes[p][i + 1] = Color.FromArgb((c & 31) * 255 / 31, (c >> 5 & 31) * 255 / 31, (c >> 10 & 31) * 255 / 31);
                }
            }
            // nome e autore: testo UTF-16 finito da zero, subito dopo l'intestazione
            int pos = 29;
            name = ReadUtf16(f, ref pos);
            author = ReadUtf16(f, ref pos);
        }

        private static string ReadUtf16(byte[] f, ref int pos)
        {
            int start = pos;
            while (pos + 1 < f.Length && (f[pos] != 0 || f[pos + 1] != 0)) pos += 2;
            var s = Encoding.Unicode.GetString(f, start, Math.Max(0, pos - start)).Trim();
            pos += 2;
            return s.Length > 60 ? s[..60] : s;
        }

        // Tavolozza verde del Link originale (da 001.link.1.zspr)
        private static readonly ushort[] kDefaultGreen =
        {
            0x7fff, 0x237e, 0x11b7, 0x369e, 0x14a5, 0x01ff, 0x1078, 0x599d, 0x3647, 0x3b68, 0x0a4a, 0x12ef, 0x2a5c, 0x1571, 0x7a18,
        };

        private int Pixel(int tile, int x, int y)
        {
            int b = tile * 32 + y * 2, s = 7 - x;
            var p = pixels!;
            return (p[b] >> s & 1) | (p[b + 1] >> s & 1) << 1 | (p[b + 16] >> s & 1) << 2 | (p[b + 17] >> s & 1) << 3;
        }

        // Blocco 16x16 a partire dalla casella in alto a sinistra
        private void Block(Bitmap bmp, Color[] pal, int tile, int ox, int oy, bool flip)
        {
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int sx = flip ? 15 - x : x;
                    int v = Pixel(tile + (sx >> 3) + (y >> 3) * 16, sx & 7, y & 7);
                    int px = ox + x, py = oy + y;
                    if (v != 0 && px >= 0 && px < bmp.Width && py >= 0 && py < bmp.Height)
                        bmp.SetPixel(px, py, pal[v]);
                }
        }

        private Bitmap Figure(int pose, Color[] pal)
        {
            // il corpo 8 pixel sotto la testa, la testa sopra al corpo
            var (head, body, dx, flip) = kPoses[pose];
            var bmp = new Bitmap(18, 24);
            Block(bmp, pal, body, 1, 8, flip);
            Block(bmp, pal, head, 1 + dx, 0, flip);
            return bmp;
        }

        // Altezza: niente con lo sprite spento, una riga per il messaggio,
        // tutta l'anteprima solo con uno sprite letto. Il riquadro si adatta
        // (SizeChanged in SettingsWindow)
        private void UpdateHeight()
        {
            Height = !Enabled ? 0 : pixels == null ? 30 : 34 + 24 * Zoom + 26;
        }

        // Larghezza di una fila di figure, con lo stacco fra le pose e le tuniche
        private static int RowWidth(int count, int split) => 8 + count * 18 * Zoom + (count - 1) * Gap + (split > 0 ? Gap : 0);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            if (!Enabled) return;
            if (pixels == null || palettes == null)
            {
                TextRenderer.DrawText(g, Lang.T(error), Font, new Rectangle(8, 0, Width - 16, Height), Theme.Muted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                return;
            }

            string title = name.Length > 0 ? name : Path.GetFileNameWithoutExtension(path);
            if (author.Length > 0) title += "  ·  " + Lang.T("by") + " " + author;
            TextRenderer.DrawText(g, title, Font, new Rectangle(8, 6, Width - 16, 22), Theme.Text,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            int fw = 18 * Zoom, fh = 24 * Zoom, top = 34;
            // tutte le pose verdi, poi di fronte blu, rossa e coniglio; se la
            // riga non ci sta solo le figure di fronte
            var all = new (int pose, int pal, string label)[]
            {
                (0, 0, "Green"), (1, 0, ""), (2, 0, ""), (3, 0, ""), (0, 1, "Blue"), (0, 2, "Red"), (4, 3, "Bunny"),
            };
            var figures = all;
            int split = 4;
            if (RowWidth(all.Length, split) > Width)
            {
                figures = new[] { all[0], all[4], all[5], all[6] };
                split = 0;
            }
            int x = 8;
            for (int i = 0; i < figures.Length; i++)
            {
                if (i == split && split > 0) x += Gap; // stacco fra le pose e le tuniche
                using var bmp = Figure(figures[i].pose, palettes[figures[i].pal]);
                var r = new Rectangle(x, top, fw, fh);
                Theme.FillRound(g, Theme.Input, new RectangleF(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), 6);
                g.DrawImage(bmp, r);
                if (figures[i].label.Length > 0)
                    TextRenderer.DrawText(g, Lang.T(figures[i].label), Theme.Caption, new Rectangle(x - Gap / 2, top + fh + 4, fw + Gap, 18), Theme.Muted,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                x += fw + Gap;
            }
        }

        protected override void OnEnabledChanged(EventArgs e) { UpdateHeight(); Invalidate(); base.OnEnabledChanged(e); }
    }
}
