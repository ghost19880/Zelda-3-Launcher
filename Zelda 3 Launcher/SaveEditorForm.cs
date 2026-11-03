using System.Drawing.Drawing2D;

namespace Zelda_3_Launcher
{
    // Editor delle partite di zelda3\saves\sram.dat: nome, vita, oggetti,
    // amuleti e cristalli, dungeon, frammenti di cuore. La copia di sicurezza la
    // fa chi apre la finestra (SettingsWindow).
    //
    // Posizioni e significato dei dati presi dall'editor di game-tools-collection
    // (RyudoSynbios, licenza MIT, templates/legend-of-zelda-the-a-link-to-the-past),
    // piu' la Pozione d'oro (bottiglia 9) e l'Ape d'oro (8) di questo gioco.
    internal class SaveEditorForm : Form
    {
        private const int SlotSize = 0x500;

        private readonly byte[] original, data;
        private readonly List<int> slots = new();
        private int slot;
        private bool loading;

        private readonly DarkComboBox comboSlot = new() { Width = 260 };
        private readonly Panel tabsBar = new() { Dock = DockStyle.Top, Height = 46, BackColor = Theme.Back };
        private readonly Panel host = new() { Dock = DockStyle.Fill, BackColor = Theme.Back, Padding = new Padding(22, 0, 22, 0) };
        private readonly List<(NavItem nav, Panel panel, List<Card>[] cols)> pages = new();
        private readonly NameCells name = new();
        private readonly FlatButton buttonSave = new() { Text = "Save", Width = 112, Style = FlatButton.Kind.Primary };
        private readonly FlatButton buttonCancel = new() { Text = "Cancel", Width = 104 };
        private readonly DescBox desc = new() { Dock = DockStyle.Fill };
        // rilettura dei controlli quando cambia la partita
        private readonly List<Action> refresh = new();
        private Stepper heartsNow = null!, heartsMax = null!, bombs = null!, arrows = null!;

        public int Updated { get; private set; } = -1;

        // Elenchi: valore nel salvataggio e testo
        private static readonly (int v, string t)[]
            kSwords = { (0, "-"), (1, "Fighter's Sword"), (2, "Master Sword"), (3, "Tempered Sword"), (4, "Golden Sword") },
            kShields = { (0, "-"), (1, "Fighter's Shield"), (2, "Red Shield"), (3, "Mirror Shield") },
            kTunics = { (0, "Green Tunic"), (1, "Blue Mail"), (2, "Red Mail") },
            kBows = { (0, "-"), (1, "Bow"), (2, "Bow & Arrows"), (4, "Bow & Silver Arrows") },
            kBoomerangs = { (0, "-"), (1, "Boomerang"), (2, "Magical Boomerang") },
            kPowder = { (0, "-"), (1, "Mushroom"), (2, "Magic Powder") },
            kShovelFlute = { (0, "-"), (1, "Shovel"), (2, "Flute"), (3, "Flute with Bird") },
            kGloves = { (0, "-"), (1, "Power Glove"), (2, "Titan's Mitt") },
            kBottles = { (0, "-"), (2, "Empty bottle"), (3, "Life Medicine"), (4, "Magic Medicine"), (5, "Cure-All Medicine"),
                (6, "Faerie"), (7, "Bee"), (8, "Good Bee") },
            kProgress = { (0, "Part 1: Equipment"), (1, "Part 2: Rescuing Zelda"), (2, "Part 3: The 3 Pendants of Virtue"), (3, "Part 4: The 7 Crystals") };
        private static readonly int[] kMaxBombs = { 10, 15, 20, 25, 30, 35, 40, 50 };
        private static readonly int[] kMaxArrows = { 30, 35, 40, 45, 50, 55, 60, 70 };

        private static readonly (int o, int bit, string t)[] kItems =
        {
            (0x342, 0, "Hookshot"), (0x345, 0, "Fire Rod"), (0x346, 0, "Ice Rod"), (0x347, 0, "Bombos Medallion"),
            (0x348, 0, "Ether Medallion"), (0x349, 0, "Quake Medallion"), (0x34A, 0, "Lamp"), (0x34B, 0, "Magic Hammer"),
            (0x34D, 0, "Bug-Catching Net"), (0x34E, 0, "Book of Mudora"), (0x350, 0, "Cane of Somaria"), (0x351, 0, "Cane of Byrna"),
            (0x352, 0, "Magic Cape"), (0x353, 1, "Magic Mirror"), (0x355, 0, "Pegasus Boots"), (0x356, 0, "Zora's Flippers"),
            (0x357, 0, "Moon Pearl"),
        };

        // Dungeon: nome, mappa, bussola, grande chiave (offset, bit; -1 se non c'e'),
        // contenitore di cuore o Agahnim sconfitto, chiavi, salvataggi
        private record Dungeon(string Name, (int o, int b) Map, (int o, int b) Compass, (int o, int b) BigKey,
            (int o, int b) Heart, string HeartText, int Keys, int Saves);
        private static readonly (int, int) No = (-1, 0);
        private static readonly Dungeon[] kDungeons =
        {
            new("Hyrule Castle", (0x369, 6), No, (0x367, 6), No, "", 0x37C, 0x3E9),
            new("Eastern Palace", (0x369, 5), (0x365, 5), (0x367, 5), (0x191, 3), "Heart Container", 0x37E, 0x3EB),
            new("Desert Palace", (0x369, 4), (0x365, 4), (0x367, 4), (0x067, 3), "Heart Container", 0x37F, 0x3ED),
            new("Tower of Hera", (0x368, 5), (0x364, 5), (0x366, 5), (0x00F, 3), "Heart Container", 0x386, 0x3FB),
            new("Agahnim's Tower", No, No, No, (0x041, 3), "Agahnim defeated", 0x380, 0x3E7),
            new("Palace of Darkness (Level 1)", (0x369, 1), (0x365, 1), (0x367, 1), (0x0B5, 3), "Heart Container", 0x382, 0x3F3),
            new("Swamp Palace (Level 2)", (0x369, 2), (0x365, 2), (0x367, 2), (0x00D, 3), "Heart Container", 0x381, 0x3F1),
            new("Skull Woods (Level 3)", (0x368, 7), (0x364, 7), (0x366, 7), (0x053, 3), "Heart Container", 0x384, 0x3F7),
            new("Thieves' Town (Level 4)", (0x368, 4), (0x364, 4), (0x366, 4), (0x159, 3), "Heart Container", 0x387, 0x3FD),
            new("Ice Palace (Level 5)", (0x368, 6), (0x364, 6), (0x366, 6), (0x1BD, 3), "Heart Container", 0x385, 0x3F9),
            new("Misery Mire (Level 6)", (0x369, 0), (0x365, 0), (0x367, 0), (0x121, 3), "Heart Container", 0x383, 0x3F5),
            new("Turtle Rock (Level 7)", (0x368, 3), (0x364, 3), (0x366, 3), (0x149, 3), "Heart Container", 0x388, 0x3FF),
            new("Ganon's Tower (Level 8)", (0x368, 2), No, (0x366, 2), (0x01B, 3), "Agahnim defeated", 0x389, 0x401),
        };

        private static readonly (int o, int bit, string t)[] kHeartPieces =
        {
            (0x1C3, 1, "Lost Woods: in the thieves' hideout"),
            (0x05E, 4, "Kakariko Village: in a well"),
            (0x23A, 4, "Kakariko Village: in the house with the grass roof"),
            (0x2A8, 6, "South of Kakariko Village: race mini-game won"),
            (0x248, 4, "Sanctuary: in a cave above a pile of rocks"),
            (0x237, 1, "Graveyard: in a cave"),
            (0x1C5, 1, "Lumberjacks' house: in a cave under a tree"),
            (0x2BB, 6, "Great Swamp: in the water near the ruins"),
            (0x214, 4, "Desert of Mystery: in a secret room near Aginah"),
            (0x2B0, 6, "Desert of Mystery: on a ledge left of the Desert Palace"),
            (0x24D, 1, "Desert of Mystery: in a cave in the north-east corner"),
            (0x301, 6, "Zora's Waterfall: at the end of a path near a fence"),
            (0x2CA, 6, "Foot of Death Mountain: on a ledge near a signpost"),
            (0x1D5, 2, "Death Mountain: in a cave near a telepathy tile"),
            (0x285, 6, "Death Mountain: at the top of the mountain"),
            (0x218, 4, "Death Mountain: in a cave guarded by 4 green Goriyas"),
            (0x283, 6, "Spectacle Rock: at the top"),
            (0x2DB, 6, "Pyramid of Power: on a ledge right of the pyramid"),
            (0x2B5, 6, "Lake Hylia: on an island"),
            (0x237, 2, "South of the Haunted Grove: in a cave on the plateau"),
            (0x2E8, 6, "Treasure Field: dug up in the mini-game"),
            (0x20D, 2, "Village of Outcasts: treasure chest mini-game"),
            (0x24F, 2, "Stake Garden: in a secret cave"),
            (0x21A, 4, "Swamp of Evil: inside the left head"),
        };

        // goldPotion / largerWallet: opzioni "Pozione d'oro" e "Borsellino piu' grande" del launcher
        private readonly bool goldPotion;
        private readonly int rupeeMax;

        public SaveEditorForm(byte[] sram, bool goldPotion, bool largerWallet)
        {
            this.goldPotion = goldPotion;
            rupeeMax = largerWallet ? 9999 : 999;
            original = (byte[])sram.Clone();
            data = (byte[])sram.Clone();
            Text = "Edit save";
            Theme.Window(this);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1040, 720);
            MinimumSize = new Size(860, 560);

            for (int s = 0; s < 3; s++)
                if (SaveFile.SlotValid(sram, s)) slots.Add(s);
            slot = slots[0];

            // intestazione: titolo e partita
            var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Theme.Back };
            header.Controls.Add(new Label { Text = "Edit save", AutoSize = true, Font = Theme.Title, ForeColor = Color.White, Location = new Point(22, 14) });
            header.Controls.Add(new Label { Text = "The game files of the title screen (saves\\sram.dat)", AutoSize = true, Font = Theme.Body, ForeColor = Theme.Muted, Location = new Point(24, 46) });
            var slotLabel = new FlatLabel { Text = "Game file", AutoSize = false, Width = 110, Height = 28, TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.Muted };
            header.Controls.Add(slotLabel);
            header.Controls.Add(comboSlot);
            header.Layout += (s, e) =>
            {
                comboSlot.Location = new Point(header.Width - 22 - comboSlot.Width, 24);
                slotLabel.Location = new Point(comboSlot.Left - slotLabel.Width - 10, 24);
            };

            var descHost = new Panel { Dock = DockStyle.Bottom, Height = 80, BackColor = Theme.Back, Padding = new Padding(22, 8, 22, 0) };
            descHost.Controls.Add(desc);
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Theme.Back };
            footer.Controls.Add(buttonCancel);
            footer.Controls.Add(buttonSave);
            footer.Layout += (s, e) =>
            {
                buttonSave.Location = new Point(footer.Width - 22 - buttonSave.Width, 12);
                buttonCancel.Location = new Point(buttonSave.Left - 10 - buttonCancel.Width, 12);
            };
            buttonSave.Click += (s, e) => SaveAll();
            buttonCancel.Click += (s, e) => Close();
            CancelButton = buttonCancel;

            Controls.Add(host);
            Controls.Add(tabsBar);
            Controls.Add(descHost);
            Controls.Add(footer);
            Controls.Add(header);

            BuildGeneral();
            BuildInventory();
            BuildDungeons();
            BuildHeartPieces();
            host.Resize += (s, e) => { foreach (var p in pages) if (p.panel.Visible) LayoutPage(p.panel, p.cols); };
            tabsBar.Layout += (s, e) =>
            {
                int x = 22;
                foreach (var p in pages)
                {
                    p.nav.Width = TextRenderer.MeasureText(p.nav.Text, p.nav.Font).Width + 46;
                    p.nav.SetBounds(x, 4, p.nav.Width, 38);
                    x += p.nav.Width + 6;
                }
            };

            Lang.Apply(this, null);
            comboSlot.SelectedIndexChanged += (s, e) =>
            {
                if (comboSlot.SelectedIndex < 0) return;
                slot = slots[comboSlot.SelectedIndex];
                Reload();
            };
            FillSlots();
            comboSlot.SelectedIndex = 0;
            ShowPage(0);
            desc.Show("", "Move the mouse over an option to read what it does.");
        }

        private void FillSlots()
        {
            int i = comboSlot.SelectedIndex;
            comboSlot.Items.Clear();
            foreach (int s in slots)
                comboSlot.Items.Add($"{s + 1}. {SaveFile.NameText(SaveFile.ReadName(data, s))}");
            comboSlot.Invalidate();
            if (i >= 0) comboSlot.SelectedIndex = i;
        }

        // Alta abbastanza per la pagina piu' lunga: nessuna pagina da scorrere
        // alla prima apertura (entro lo schermo)
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int content = pages.Max(p => PageHeight(p.cols));
            int others = 0;
            foreach (Control c in Controls) if (c != host) others += c.Height;
            int want = others + content + 6;
            var area = Screen.FromControl(Owner ?? this).WorkingArea;
            int chrome = Height - ClientSize.Height;
            ClientSize = new Size(ClientSize.Width, Math.Min(want, area.Height - chrome));
            var center = Owner != null ? new Point(Owner.Left + Owner.Width / 2, Owner.Top + Owner.Height / 2)
                : new Point(area.Left + area.Width / 2, area.Top + area.Height / 2);
            Location = new Point(Math.Clamp(center.X - Width / 2, area.Left, Math.Max(area.Left, area.Right - Width)),
                Math.Clamp(center.Y - Height / 2, area.Top, Math.Max(area.Top, area.Bottom - Height)));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            name.Focus();
        }

        //
        // Lettura e scrittura dei dati della partita scelta
        //

        private int Base => slot * SlotSize;
        private int U8(int o) => data[Base + o];
        private void SetU8(int o, int v) { if (!loading) data[Base + o] = (byte)v; }
        private int U16(int o) => BitConverter.ToUInt16(data, Base + o);
        private void SetU16(int o, int v) { if (!loading) BitConverter.GetBytes((ushort)v).CopyTo(data, Base + o); }
        private bool Bit(int o, int b) => (data[Base + o] >> b & 1) != 0;
        private void SetBit(int o, int b, bool on)
        {
            if (loading) return;
            if (on) data[Base + o] |= (byte)(1 << b);
            else data[Base + o] &= (byte)~(1 << b);
        }

        private void Reload()
        {
            loading = true;
            foreach (var r in refresh) r();
            loading = false;
        }

        //
        // Pagine e riquadri
        //

        private Panel AddPage(string text, out List<Card>[] cols, int columns = 2)
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Back, Visible = false };
            var nav = new NavItem { Text = text, BackColor = Theme.Back };
            var c = Enumerable.Range(0, columns).Select(_ => new List<Card>()).ToArray();
            int index = pages.Count;
            nav.Click += (s, e) => ShowPage(index);
            pages.Add((nav, panel, c));
            host.Controls.Add(panel);
            tabsBar.Controls.Add(nav);
            cols = c;
            return panel;
        }

        private void ShowPage(int index)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].nav.Selected = i == index;
                pages[i].panel.Visible = i == index;
            }
            LayoutPage(pages[index].panel, pages[index].cols);
        }

        private const int Gap = 14;

        // Altezza della colonna piu' lunga della pagina
        private static int PageHeight(List<Card>[] cols) =>
            cols.Max(col => col.Sum(c => c.ContentHeight + Gap));

        // Colonne di riquadri come nelle impostazioni
        private static void LayoutPage(Panel panel, List<Card>[] cols)
        {
            const int gap = Gap;
            var pos = panel.AutoScrollPosition;
            // con la barra di scorrimento (finestra rimpicciolita) le colonne si stringono
            int width = panel.ClientSize.Width;
            if (PageHeight(cols) > panel.ClientSize.Height && !panel.VerticalScroll.Visible)
                width -= SystemInformation.VerticalScrollBarWidth;
            int n = cols.Length;
            int colW = (width - gap * (n - 1)) / n;
            for (int c = 0; c < n; c++)
            {
                int y = 0;
                foreach (var card in cols[c])
                {
                    card.SetBounds(c * (colW + gap) + pos.X, y + pos.Y, colW, card.ContentHeight);
                    y += card.Height + gap;
                }
            }
        }

        private static Card AddCard(Panel page, List<Card>[] cols, int column, string text)
        {
            var c = new Card { Text = text };
            page.Controls.Add(c);
            cols[column].Add(c);
            return c;
        }

        private void Hover(Control c, string text, string tip)
        {
            void Show(object? s, EventArgs e) => desc.Show(text, tip);
            c.MouseEnter += Show;
            c.GotFocus += Show;
            foreach (Control child in c.Controls) Hover(child, text, tip);
        }

        private FieldRow Row(Card card, string text, Control editor, string tip)
        {
            var row = new FieldRow(text, editor);
            Hover(row, text, tip);
            card.Controls.Add(row);
            return row;
        }

        private void Flag(Card card, int o, int b, string text, string tip = "", Action<bool>? after = null) =>
            card.Controls.Add(MakeFlag(o, b, text, tip, after));

        // Interruttori due per riga (elenchi lunghi)
        private static void Pairs(Card card, List<ToggleSwitch> list)
        {
            for (int i = 0; i < list.Count; i += 2)
            {
                var row = new Panel { Height = 32, BackColor = Theme.CardBack };
                var a = list[i];
                var b = i + 1 < list.Count ? list[i + 1] : null;
                row.Controls.Add(a);
                if (b != null) row.Controls.Add(b);
                row.Layout += (s, e) =>
                {
                    int w = (row.Width - 12) / 2;
                    a.SetBounds(0, 0, w, 32);
                    b?.SetBounds(w + 12, 0, row.Width - w - 12, 32);
                };
                card.Controls.Add(row);
            }
        }

        private ToggleSwitch MakeFlag(int o, int b, string text, string tip = "", Action<bool>? after = null)
        {
            var t = new ToggleSwitch { Text = text, BackColor = Theme.CardBack };
            t.CheckedChanged += (s, e) =>
            {
                if (loading) return;
                SetBit(o, b, t.Checked);
                after?.Invoke(t.Checked);
            };
            refresh.Add(() => t.Checked = Bit(o, b));
            Hover(t, text, tip);
            return t;
        }

        private DarkComboBox Choice(Card card, string text, int o, (int v, string t)[] items, string tip = "", Action? after = null)
        {
            var c = MakeChoice(o, items, after);
            Row(card, text, c, tip);
            return c;
        }

        private DarkComboBox MakeChoice(int o, (int v, string t)[] items, Action? after = null, int width = 250)
        {
            var c = new DarkComboBox { Width = width };
            c.Items.AddRange(items.Select(i => (object)Lang.T(i.t)));
            c.SelectedIndexChanged += (s, e) =>
            {
                if (loading || c.SelectedIndex < 0 || c.SelectedIndex >= items.Length) return;
                SetU8(o, items[c.SelectedIndex].v);
                after?.Invoke();
            };
            refresh.Add(() =>
            {
                int v = U8(o);
                int i = Array.FindIndex(items, x => x.v == v);
                // valore che l'elenco non ha: lo si mostra cosi' com'e'
                if (i < 0)
                {
                    if (c.Items.Count > items.Length) c.Items.RemoveAt(items.Length);
                    c.Items.Add(o >= 0x35C && o <= 0x35F && v == 9 ? Lang.T("Gold Medicine (option off)") : $"? ({v})");
                    i = items.Length;
                }
                c.SelectedIndex = i;
                c.Invalidate();
            });
            return c;
        }

        private Stepper Number(Card card, string text, int o, int min, int max, int step, string tip = "", bool word = false,
            Func<int, string>? format = null, Action? after = null, Func<string, int?>? parse = null)
        {
            var s = MakeNumber(o, min, max, step, word, format, after, parse);
            Row(card, text, s, tip);
            return s;
        }

        private Stepper MakeNumber(int o, int min, int max, int step, bool word = false, Func<int, string>? format = null, Action? after = null,
            Func<string, int?>? parse = null)
        {
            var s = new Stepper { Minimum = min, Maximum = max, Step = step, Format = format, Parse = parse, BackColor = Theme.CardBack, Width = 150 };
            s.ValueChanged += (_, _) =>
            {
                if (loading) return;
                if (word) SetU16(o, s.Value); else SetU8(o, s.Value);
                after?.Invoke();
            };
            refresh.Add(() => s.Value = Math.Clamp(word ? U16(o) : U8(o), s.Minimum, s.Maximum));
            return s;
        }

        // Numero grande scritto a mano (rupie, conteggi)
        private void Typed(Card card, string text, int o, int max, string tip = "") => Row(card, text, MakeTyped(o, max), tip);

        private InputBox MakeTyped(int o, int max, int width = 150)
        {
            var input = new InputBox { Width = width };
            var box = input.Box;
            box.TextAlign = HorizontalAlignment.Right;
            box.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b') e.Handled = true; };
            box.TextChanged += (s, e) =>
            {
                if (loading) return;
                if (int.TryParse(box.Text, out int v))
                {
                    if (v > max) { box.Text = max.ToString(); return; }
                    SetU16(o, v);
                }
            };
            box.Leave += (s, e) => { if (!int.TryParse(box.Text, out _)) box.Text = U16(o).ToString(); };
            refresh.Add(() => box.Text = Math.Min(U16(o), max).ToString());
            return input;
        }

        private static string Hearts(int eighths)
        {
            int whole = eighths / 8, rest = eighths % 8;
            return rest == 0 ? whole.ToString() : rest == 4 ? $"{whole}½" : (eighths / 8.0).ToString("0.##");
        }

        // cuori scritti a mano ("3", "3½", "3,5", "3.5") in ottavi
        private static int? ParseHearts(string text)
        {
            text = text.Replace("½", ",5").Replace(".", ",");
            if (text.StartsWith(",")) text = "0" + text;
            if (!decimal.TryParse(text, System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.GetCultureInfo("it-IT"), out decimal h)) return null;
            decimal e = h * 8;
            return e == decimal.Truncate(e) && e <= 1000 ? (int)e : null;
        }

        //
        // Generale
        //

        private void BuildGeneral()
        {
            var page = AddPage("General", out var cols);

            var player = AddCard(page, cols, 0, "Player");
            var nameRow = new Panel { Height = 56, BackColor = Theme.CardBack };
            nameRow.Controls.Add(name);
            nameRow.Layout += (s, e) => name.Location = new Point((nameRow.Width - name.Width) / 2, 4);
            Hover(nameRow, "Player name", "Type letters (A-Z, a-z), numbers and space on the keyboard, or use the buttons. The Link icon takes two boxes, as in the game. As in the game, a lowercase l looks like an uppercase I.");
            player.Controls.Add(nameRow);
            name.Changed += () =>
            {
                if (loading) return;
                SaveFile.SetName(data, slot, name.Codes);
                FillSlots();
            };
            refresh.Add(() => name.Codes = SaveFile.ReadName(data, slot));

            var keys = new FlowLayoutPanel { Height = 40, BackColor = Theme.CardBack, WrapContents = false, Padding = new Padding(4, 2, 0, 0) };
            foreach (var c in "-.,!?()")
            {
                var b = new FlatButton { Text = c.ToString(), Width = 32, Height = 32, Radius = 7, Font = Theme.Bold, Margin = new Padding(0, 0, 5, 0), TabStop = false };
                b.Click += (s, e) => { name.Type(SaveFile.CharToCode(c)); name.Focus(); };
                keys.Controls.Add(b);
            }
            var link = new LinkButton { Width = 46, Height = 32, Margin = new Padding(0, 0, 5, 0), TabStop = false };
            link.Click += (s, e) => { name.TypeLink(); name.Focus(); };
            Hover(link, "Link icon", "Puts the Link icon in the name: it takes two boxes, as in the game.");
            keys.Controls.Add(link);
            var space = new FlatButton { Text = "Space", Width = 70, Height = 32, Radius = 7, Font = Theme.Body, Margin = new Padding(0, 0, 5, 0), TabStop = false };
            space.Click += (s, e) => { name.Type(SaveFile.Space); name.Focus(); };
            keys.Controls.Add(space);
            var back = new FlatButton { Text = "⌫", Width = 42, Height = 32, Radius = 7, Font = Theme.Body, Margin = new Padding(0), TabStop = false };
            back.Click += (s, e) => { name.Back(); name.Focus(); };
            keys.Controls.Add(back);
            player.Controls.Add(keys);

            Choice(player, "Progression", 0x3C5, kProgress, "Part of the story the game file is in: it decides, among other things, where Link can start.");
            Typed(player, "Save count", 0x403, 999, "Saves and deaths of the game file so far.");
            Typed(player, "Total count", 0x405, 999, "Total of saves and deaths shown at the end of the game.");

            var life = AddCard(page, cols, 1, "Life and magic");
            heartsMax = Number(life, "Heart containers", 0x36C, 24, 160, 8, "Maximum hearts (3 to 20).", format: v => Hearts(v), parse: ParseHearts, after: () =>
            {
                heartsNow.Maximum = heartsMax.Value;
            });
            heartsNow = Number(life, "Hearts", 0x36D, 8, 160, 8, "Hearts when the game file starts.", format: v => Hearts(v), parse: ParseHearts);
            refresh.Add(() => heartsNow.Maximum = Math.Max(24, U8(0x36C)));
            Number(life, "Pieces of heart", 0x36B, 0, 3, 1, "Pieces of heart collected towards the next heart container (4 make a container).");
            Number(life, "Magic", 0x36E, 0, 128, 8, "Magic meter: 128 is full.").TypeStep = 1;  // a mano qualsiasi valore
            Choice(life, "Magic upgrade", 0x37B, new[] { (0, "Normal"), (1, "Half magic") },
                "Magic consumption: normal, or halved by the fairy of the Pyramid's fountain.");

            var money = AddCard(page, cols, 1, "Rupees");
            Typed(money, "Rupees", 0x360, rupeeMax, "Rupees of the game file (999, or 9999 with the Larger Wallet option).");
            Number(money, "Rupees in the fountain", 0x36A, 0, 99, 1, "Rupees already thrown into the fountain of the Lake Hylia fairy.");
        }

        //
        // Inventario
        //

        private void BuildInventory()
        {
            var page = AddPage("Inventory", out var cols);

            var items = AddCard(page, cols, 0, "Items");
            var list = new List<ToggleSwitch>();
            foreach (var (o, b, t) in kItems)
            {
                Action<bool>? after = null;
                // stivali e pinne danno anche la corsa e il nuoto (come il gioco)
                if (o == 0x355) after = on => SetBit(0x379, 2, on);
                if (o == 0x356) after = on => SetBit(0x379, 1, on);
                list.Add(MakeFlag(o, b, t, "", after));
            }
            Pairs(items, list);

            var quest = AddCard(page, cols, 0, "Pendants and crystals");
            list = new List<ToggleSwitch>
            {
                MakeFlag(0x374, 2, "Pendant of Courage"),
                MakeFlag(0x374, 1, "Pendant of Power"),
                MakeFlag(0x374, 0, "Pendant of Wisdom"),
            };
            int[] crystalBits = { 1, 4, 6, 5, 2, 0, 3 };
            for (int i = 0; i < 7; i++)
                list.Add(MakeFlag(0x37A, crystalBits[i], $"{Lang.T("Crystal")} {i + 1}", Lang.T(kDungeons[5 + i].Name)));
            Pairs(quest, list);

            var equip = AddCard(page, cols, 1, "Equipment");
            Choice(equip, "Sword", 0x359, kSwords);
            Choice(equip, "Shield", 0x35A, kShields);
            Choice(equip, "Tunic", 0x35B, kTunics);
            Choice(equip, "Glove", 0x354, kGloves);
            Choice(equip, "Bow", 0x340, kBows);
            Choice(equip, "Boomerang", 0x341, kBoomerangs);
            Choice(equip, "Mushroom / Magic Powder", 0x344, kPowder);
            Choice(equip, "Shovel / Flute", 0x34C, kShovelFlute, "With the extended inventory the flute also keeps the shovel.");

            // quantita' e capienza sulla stessa riga
            var ammo = AddCard(page, cols, 1, "Bombs and arrows");
            var bombBag = (from i in Enumerable.Range(0, 8) select (i, Lang.T("Up to") + " " + kMaxBombs[i])).ToArray();
            var quiver = (from i in Enumerable.Range(0, 8) select (i, Lang.T("Up to") + " " + kMaxArrows[i])).ToArray();
            bombs = MakeNumber(0x343, 0, 50, 1);
            arrows = MakeNumber(0x377, 0, 70, 1);
            Row(ammo, "Bombs", Pair(bombs, MakeChoice(0x370, bombBag, ClampAmmo, 120)),
                "Bombs held and the most the bag can hold (upgrades from the fairy of the Pyramid's fountain).");
            Row(ammo, "Arrows", Pair(arrows, MakeChoice(0x371, quiver, ClampAmmo, 120)),
                "Arrows held and the most the quiver can hold (upgrades from the fairy of the Pyramid's fountain).");
            refresh.Add(ClampAmmo);

            var bottles = AddCard(page, cols, 1, "Bottles");
            // la Pozione d'oro solo con l'opzione accesa nel launcher
            var bottleItems = goldPotion ? kBottles.Append((9, "Gold Medicine")).ToArray() : kBottles;
            for (int i = 0; i < 4; i += 2)
                Row(bottles, $"{Lang.T("Bottles")} {i + 1}-{i + 2}", Pair(MakeChoice(0x35C + i, bottleItems, null, 160), MakeChoice(0x35D + i, bottleItems, null, 160)),
                    goldPotion ? "Content of the bottles: \"-\" means no bottle." : "Content of the bottles: \"-\" means no bottle. The Gold Medicine can be chosen only with the Gold Medicine option on (Items).");
        }

        // due controlli affiancati nella stessa riga
        private static Panel Pair(Control a, Control b)
        {
            var p = new Panel { Width = a.Width + 8 + b.Width, Height = Math.Max(a.Height, b.Height), BackColor = Theme.CardBack };
            a.Location = new Point(0, (p.Height - a.Height) / 2);
            b.Location = new Point(a.Width + 8, (p.Height - b.Height) / 2);
            p.Controls.Add(a);
            p.Controls.Add(b);
            return p;
        }

        // bombe e frecce non oltre la capienza
        private void ClampAmmo()
        {
            bool was = loading;
            int maxB = kMaxBombs[Math.Min(7, U8(0x370))], maxA = kMaxArrows[Math.Min(7, U8(0x371))];
            bombs.Maximum = maxB;
            arrows.Maximum = maxA;
            if (!was)
            {
                if (U8(0x343) > maxB) SetU8(0x343, maxB);
                if (U8(0x377) > maxA) SetU8(0x377, maxA);
            }
        }

        //
        // Dungeon: una tabella, una riga per dungeon
        //

        private void BuildDungeons()
        {
            var page = AddPage("Dungeons", out var cols, 1);
            var card = AddCard(page, cols, 0, "Dungeons");
            string[] heads = { "", "Map", "Compass", "Big Key", "Boss", "Small keys", "Save count" };
            card.Controls.Add(new DungeonRow(heads.Select(h => (Control)new FlatLabel
            {
                Text = h, AutoSize = false, Height = 26, ForeColor = Theme.Muted, Font = Theme.Caption,
                TextAlign = h == "" ? ContentAlignment.MiddleLeft : ContentAlignment.MiddleCenter,
            }).ToArray()) { Height = 26 });
            foreach (var d in kDungeons)
            {
                var name = new FlatLabel { Text = d.Name, AutoSize = false, Height = 34, ForeColor = Theme.Text, TextAlign = ContentAlignment.MiddleLeft };
                Control Cell((int o, int b) f, string text, string tip) =>
                    f.o < 0 ? new Panel { BackColor = Theme.CardBack, Height = 32 } : Small(MakeFlag(f.o, f.b, "", ""), $"{Lang.T(d.Name)}: {Lang.T(text)}", tip);
                card.Controls.Add(new DungeonRow(new[]
                {
                    name,
                    Cell(d.Map, "Map", "The dungeon map has been found."),
                    Cell(d.Compass, "Compass", "The dungeon compass has been found."),
                    Cell(d.BigKey, "Big Key", "The big key has been found."),
                    Cell(d.Heart, d.HeartText, d.HeartText == "Heart Container" ? "The boss's heart container has been taken." : "Agahnim has been defeated here."),
                    Small(MakeNumber(d.Keys, 0, 9, 1), $"{Lang.T(d.Name)}: {Lang.T("Small keys")}", "Small keys held for this dungeon."),
                    Small(MakeTyped(d.Saves, 999), $"{Lang.T(d.Name)}: {Lang.T("Save count")}", "Saves and deaths in this dungeon, counted for the end of the game."),
                }));
            }
        }

        private Control Small(Control c, string text, string tip)
        {
            Hover(c, text, tip);
            return c;
        }

        // colonne fisse: nome largo, poi interruttori e numeri centrati
        private class DungeonRow : Panel
        {
            private static readonly int[] kWidths = { 0, 96, 96, 96, 96, 140, 110 };
            private readonly Control[] cells = null!;

            public DungeonRow(Control[] cells)
            {
                this.cells = cells;
                Height = 34;
                BackColor = Theme.CardBack;
                Controls.AddRange(cells);
            }

            protected override void OnLayout(LayoutEventArgs e)
            {
                if (cells == null) { base.OnLayout(e); return; }
                int x = Width;
                for (int i = cells.Length - 1; i >= 1; i--)
                {
                    x -= kWidths[i];
                    var c = cells[i];
                    // interruttore senza testo: solo la levetta, centrata
                    int w = c is ToggleSwitch ? 56 : c is Label ? kWidths[i] : Math.Min(c.Width, kWidths[i] - 8);
                    c.SetBounds(x + (kWidths[i] - w) / 2, (Height - c.Height) / 2, w, c.Height);
                }
                cells[0].SetBounds(8, 0, Math.Max(40, x - 8), Height);
                base.OnLayout(e);
            }
        }

        //
        // Frammenti di cuore
        //

        private void BuildHeartPieces()
        {
            var page = AddPage("Pieces of heart", out var cols);
            var a = AddCard(page, cols, 0, "Light World");
            var b = AddCard(page, cols, 1, "Death Mountain and Lake Hylia");
            var c = AddCard(page, cols, 1, "Dark World");
            int[] dark = { 17, 19, 20, 21, 22, 23 };
            for (int i = 0; i < kHeartPieces.Length; i++)
            {
                var (o, bit, t) = kHeartPieces[i];
                Flag(i < 12 ? a : dark.Contains(i) ? c : b, o, bit, t, "On: the piece of heart has already been taken. This doesn't change the pieces counted in General.");
            }
        }

        //
        // Salvataggio
        //

        private void SaveAll()
        {
            foreach (int s in slots)
            {
                var n = SaveFile.ReadName(data, s);
                if (n.All(c => c == SaveFile.Space))
                {
                    Lang.Show("The name can't be empty.", "Edit save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            try
            {
                Updated = SaveFile.Save(original, data);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.T("Unable to write the save:") + "\n\n" + ex.Message, Lang.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK && !data.SequenceEqual(original))
            {
                var answer = Lang.Show("The changes that you have made will not be saved.\n\nDo you wish to continue?", "Confirmation", MessageBoxButtons.YesNo);
                if (answer == DialogResult.No) e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        // Icona di Link disegnata qui (testa con il berretto verde): sta in due
        // caselle come nel gioco. Disegno nostro, non quello del gioco.
        public static void DrawLinkFace(Graphics g, RectangleF r)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float s = Math.Min(r.Width, r.Height);
            float x = r.X + (r.Width - s) / 2, y = r.Y + (r.Height - s) / 2;
            PointF P(float px, float py) => new(x + px * s, y + py * s);
            using var cap = new SolidBrush(Color.FromArgb(0x3c, 0xb0, 0x48));
            using var capDark = new SolidBrush(Color.FromArgb(0x24, 0x7a, 0x30));
            using var skin = new SolidBrush(Color.FromArgb(0xf6, 0xc8, 0x98));
            using var hair = new SolidBrush(Color.FromArgb(0xe8, 0xb8, 0x30));
            using var eye = new SolidBrush(Color.FromArgb(0x20, 0x30, 0x60));
            // punta del berretto che scende a destra
            g.FillPolygon(capDark, new[] { P(0.62f, 0.18f), P(0.98f, 0.62f), P(0.70f, 0.42f) });
            // orecchie
            g.FillPolygon(skin, new[] { P(0.14f, 0.50f), P(0.02f, 0.40f), P(0.20f, 0.66f) });
            g.FillPolygon(skin, new[] { P(0.86f, 0.50f), P(0.98f, 0.40f), P(0.80f, 0.66f) });
            // viso
            g.FillEllipse(skin, x + 0.18f * s, y + 0.30f * s, 0.64f * s, 0.62f * s);
            // ciuffo biondo e berretto
            g.FillEllipse(hair, x + 0.18f * s, y + 0.30f * s, 0.64f * s, 0.22f * s);
            g.FillPie(cap, x + 0.16f * s, y + 0.06f * s, 0.68f * s, 0.56f * s, 180, 180);
            // occhi
            g.FillEllipse(eye, x + 0.34f * s, y + 0.56f * s, 0.09f * s, 0.14f * s);
            g.FillEllipse(eye, x + 0.57f * s, y + 0.56f * s, 0.09f * s, 0.14f * s);
            g.SmoothingMode = old;
        }

        // Pulsante con l'icona di Link
        private class LinkButton : FlatButton
        {
            public LinkButton() { Radius = 7; }

            protected override void OnPaint(PaintEventArgs e)
            {
                var t = Text;
                Text = "";
                base.OnPaint(e);
                Text = t;
                DrawLinkFace(e.Graphics, new RectangleF((Width - 26) / 2f, (Height - 26) / 2f, 26, 26));
            }
        }

        // Le sei caselle del nome, come nella schermata del gioco
        private class NameCells : Control
        {
            private const int Cell = 40, Gap = 6;
            private int[] codes = Enumerable.Repeat(SaveFile.Space, 6).ToArray();
            private int cursor;
            private static readonly Font Big = new("Segoe UI Semibold", 17F);

            public event Action? Changed;

            public NameCells()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
                Size = new Size(6 * Cell + 5 * Gap, 46);
                TabStop = true;
                Cursor = Cursors.IBeam;
            }

            public int[] Codes
            {
                get => (int[])codes.Clone();
                set
                {
                    codes = (int[])value.Clone();
                    // si scrive dopo l'ultima lettera
                    cursor = Math.Min(5, Array.FindLastIndex(codes, c => c != SaveFile.Space) + 1);
                    Invalidate();
                }
            }

            private void Edited()
            {
                Invalidate();
                Changed?.Invoke();
            }

            private Rectangle CellRect(int i) => new(i * (Cell + Gap), 2, Cell, Height - 4);

            // Scrivendo sopra meta' dell'icona di Link si toglie anche l'altra meta'
            private void Clear(int i)
            {
                if (codes[i] == SaveFile.LinkLeft && i < 5 && codes[i + 1] == SaveFile.LinkRight) codes[i + 1] = SaveFile.Space;
                if (codes[i] == SaveFile.LinkRight && i > 0 && codes[i - 1] == SaveFile.LinkLeft) codes[i - 1] = SaveFile.Space;
                codes[i] = SaveFile.Space;
            }

            public void Type(int code)
            {
                if (code < 0) return;
                Clear(cursor);
                codes[cursor] = code;
                cursor = Math.Min(5, cursor + 1);
                Edited();
            }

            public void TypeLink()
            {
                int i = Math.Min(cursor, 4);
                Clear(i);
                Clear(i + 1);
                codes[i] = SaveFile.LinkLeft;
                codes[i + 1] = SaveFile.LinkRight;
                cursor = Math.Min(5, i + 2);
                Edited();
            }

            public void Back()
            {
                // all'ultima casella piena si cancella quella, altrimenti la precedente
                if (!(cursor == 5 && codes[5] != SaveFile.Space) && cursor > 0) cursor--;
                if (codes[cursor] == SaveFile.LinkRight && cursor > 0 && codes[cursor - 1] == SaveFile.LinkLeft) cursor--;
                Clear(cursor);
                Edited();
            }

            protected override bool IsInputKey(Keys keyData) =>
                keyData is Keys.Left or Keys.Right or Keys.Home or Keys.End || base.IsInputKey(keyData);

            protected override void OnKeyDown(KeyEventArgs e)
            {
                switch (e.KeyCode)
                {
                    case Keys.Left: cursor = Math.Max(0, cursor - 1); break;
                    case Keys.Right: cursor = Math.Min(5, cursor + 1); break;
                    case Keys.Home: cursor = 0; break;
                    case Keys.End: cursor = 5; break;
                    case Keys.Delete: Clear(cursor); Edited(); break;
                    default: base.OnKeyDown(e); return;
                }
                e.Handled = true;
                Invalidate();
            }

            protected override void OnKeyPress(KeyPressEventArgs e)
            {
                if (e.KeyChar == '\b') Back();
                else if (e.KeyChar >= ' ')
                {
                    int code = SaveFile.CharToCode(e.KeyChar);
                    if (code >= 0) Type(code);
                    else System.Media.SystemSounds.Beep.Play();
                }
                e.Handled = true;
                base.OnKeyPress(e);
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                Focus();
                cursor = Math.Clamp(e.X / (Cell + Gap), 0, 5);
                Invalidate();
                base.OnMouseDown(e);
            }

            protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
            protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Parent?.BackColor ?? Theme.CardBack);
                Theme.Smooth(g);
                for (int i = 0; i < 6; i++)
                {
                    var r = CellRect(i);
                    Theme.FillRound(g, Theme.Input, r, 7);
                    if (Focused && i == cursor)
                        Theme.DrawRound(g, Theme.Accent, new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height - 1), 7, 2);
                    int t = codes[i];
                    if (t == SaveFile.LinkLeft || t == SaveFile.LinkRight)
                    {
                        // l'icona e' larga due caselle: ogni meta' nella sua
                        var both = t == SaveFile.LinkLeft ? new RectangleF(r.X + 4, r.Y + 4, Cell * 2 + Gap - 8, r.Height - 8)
                            : new RectangleF(r.X - Cell - Gap + 4, r.Y + 4, Cell * 2 + Gap - 8, r.Height - 8);
                        var state = g.Save();
                        g.SetClip(r);
                        DrawLinkFace(g, new RectangleF(both.X + (both.Width - both.Height * 1.3f) / 2, both.Y - 2, both.Height * 1.3f, both.Height + 4));
                        g.Restore(state);
                    }
                    else
                    {
                        TextRenderer.DrawText(g, SaveFile.CodeToText(t), Big, r, Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                }
            }
        }
    }
}
