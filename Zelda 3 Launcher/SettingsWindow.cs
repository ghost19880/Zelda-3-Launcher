using System.Diagnostics;
using IniParser.Model;

namespace Zelda_3_Launcher
{
    // Cosa chiede di fare la finestra delle impostazioni quando si chiude
    public enum SettingsRequest { None, Rebuild }

    // Tutte le impostazioni in una finestra: schede a sinistra, riquadri con gli
    // interruttori, la descrizione dell'opzione sotto il mouse in fondo.
    // Legge e scrive zelda3.ini; i tasti li legge e li scrive KeyBindings.
    public class SettingsWindow : Form
    {
        // Opzione si/no: sezione, chiave, testo, spiegazione, commento nell'ini.
        // Hd: migliorie HD (0/1, toccate da "Attiva/Disattiva tutto"); le altre
        // sono opzioni del gioco originale e si riscrivono solo se cambiano.
        private record BoolOption(string Section, string Key, string Text, string Tip, string Comment, bool Hd = true);

        private static readonly BoolOption
            HdText = new("Graphics", "HdText", "Sharp dialogue text",
                "Redraws the dialogue box on top of the shader so the letters stay sharp.",
                "Redraw the dialogue box sharp on top of the shader (1 = on)"),
            HighRefresh = new("Graphics", "HighRefreshInterpolation", "Smoothness above 60 Hz",
                "On displays above 60 Hz, scrolling is interpolated in the extra frames. Nothing changes at 60 Hz.",
                "Displays above 60 Hz: scrolling is interpolated in the extra frames"),
            Lighting = new("Graphics", "DynamicLighting", "Dynamic lighting",
                "Glow around torches, fire and lava; dark rooms are lit only by burning flames.",
                "Dynamic lighting: glow around fire, dark rooms lit by the flames"),
            SoftIris = new("Graphics", "FxSoftIris", "Soft transition circle",
                "Soft edge on the circle transition when entering and leaving places.",
                "Soft edge on the circle transition (entering and leaving places)"),
            ButtonHints = new("Graphics", "FxButtonHints", "Button hints",
                "On-screen hints for the buttons in the inventory and on the maps.",
                "On-screen hints for the buttons in the inventory and on the maps"),
            FastTransitions = new("Features", "FastTransitions", "Fast transitions",
                "Moving between areas and rooms happens at double speed.",
                "Transitions between areas and rooms at double speed"),
            ContinuousWorld = new("Features", "ContinuousWorld", "Continuous world",
                "Outdoors you see the neighbouring areas beyond the edge, the camera follows Link across it and the world remembers what you changed while you stay outdoors.",
                "Continuous world outdoors: neighbouring areas visible, the view follows Link across the edge"),
            ModernHud = new("Features", "ModernHud", "Modern HUD",
                "Hearts and magic top left, Y/A/B buttons top right with the item, the sword and what the A button does (Talk, Read, Open...), rupees and keys bottom left.",
                "Modern HUD: hearts and magic top left, Y/A/B buttons top right, rupees and keys bottom left"),
            XlrInHud = new("Features", "ShowXLRItemsInHud", "X, L, R items in the HUD",
                "Shows the items assigned to X, L and R in the HUD (requires Advanced Item Selection).",
                "Show the items assigned to X, L and R in the HUD (requires ItemSwitchLR)"),
            DungeonNames = new("Features", "DungeonNames", "Dungeon names",
                "When Link enters a dungeon from outside, its name appears in the middle of the screen like on GBA and disappears as soon as he moves.",
                "Dungeon name in the middle of the screen when entering from outside, like on GBA"),
            AreaAutosave = new("General", "AutosaveOnAreaChange", "Automatic save at area changes",
                "Silently saves at every area change and when entering or leaving dungeons and houses. Load it from the in-game menu.",
                "Automatic save at every area change (saves/auto0.sav, auto1.sav, ...)"),

            ItemSwitch = new("Features", "ItemSwitchLR", "Advanced Item Selection",
                "Allows you to switch items with L/R but also allows you to assign items to X, Y, L, and R.\n\nIf X is assigned an item, select will now open the map. To save and quit, you will need to hit Select while in the item menu.\n\nIf L and/or R are assigned an item, quick item switching will be disabled.",
                "Item switch on L/R and items on X, L, R", false),
            ItemSwitchLimit = new("Features", "ItemSwitchLRLimit", "Limit L/R Switch to First 4 Items",
                "Limit the items you will cycle through to just the first four items",
                "L/R only cycle through the first four items", false),
            TurnWhileDashing = new("Features", "TurnWhileDashing", "Allow Turning While Dashing",
                "Allows you to turn when dashing", "Allow turning while dashing", false),
            MirrorToDarkworld = new("Features", "MirrorToDarkworld", "Mirror Teleports to Dark World",
                "Magic Mirror works in both directions", "Magic Mirror works in both directions", false),
            SwordInteraction = new("Features", "SwordAndItemsInteraction", "Sword and items world interaction",
                "Swords and items behave like on GBA and interact with the world",
                "Swords and items behave like on GBA and interact with the world", false),
            BurnFreeze = new("Features", "BurnAndFreezeInteraction", "Burn and freeze interaction",
                "The world physically reacts to fire and ice.",
                "The world physically reacts to fire and ice: lamp, rods and medallions burn or freeze bushes and grass", false),
            MoreBombs = new("Features", "MoreActiveBombs", "More Active Bombs",
                "Can have 10 active bombs instead of 2", "10 active bombs instead of 2", false),
            LargerWallet = new("Features", "CarryMoreRupees", "Larger Wallet",
                "Can carry 9999 rupees", "Carry up to 9999 rupees", false),
            CancelBird = new("Features", "CancelBirdTravel", "Cancel Bird",
                "Allows you to cancel bird travel by hitting the X button", "Cancel bird travel with X", false),
            BirdPoint9 = new("Features", "BirdTravelPoint9", "Bird travel point 9",
                "Adds bird travel point 9 like the GBA version.",
                "Ninth bird travel point on the eastern summit of Death Mountain", false),
            NoLowHealthBeep = new("Features", "DisableLowHealthBeep", "Disable Low Heart Beep",
                "Disables the beeping when you are low health", "No beep at low health", false),
            SkipIntro = new("Features", "SkipIntroOnKeypress", "Skip Intro w/ Keypress",
                "Allows you to skip the intro by pressing a key", "Skip the intro with a key", false),
            MaxInYellow = new("Features", "ShowMaxItemsInYellow", "Indicate Max Resources",
                "Values will appear as yellow/orange when you have max resources (rupees, arrows, bombs)",
                "Show maxed rupees, arrows and bombs in yellow", false),
            MiscFixes = new("Features", "MiscBugFixes", "Miscellaneous Minor Fixes",
                "Miscellaneous bug fixes", "Miscellaneous minor bug fixes", false),
            GameFixes = new("Features", "GameChangingBugFixes", "Game Changing Bug Fixes",
                "Some bug fixes that are significant enough to impact game behavior",
                "Bug fixes that change game behavior", false);

        private static readonly BoolOption[] kEffects =
        {
            new("Graphics", "FxEmbers", "Embers from flames",
                "Sparks rising from torches, braziers, fire and lava.",
                "Embers rising from torches, braziers and lava"),
            new("Graphics", "FxWindowRays", "Light rays from windows",
                "Light shafts with floating dust in rooms with windows.",
                "Light rays and dust from windows"),
            new("Graphics", "FxItemGlow", "Glow on received items",
                "Light rays behind Link when he holds up a newly found item.",
                "Ray glow when Link holds up an item"),
            new("Graphics", "FxHeatHaze", "Heat haze over lava",
                "The air shimmers slightly above lava.",
                "Air shimmering above lava"),
            new("Graphics", "FxRain", "Improved rain and lightning",
                "Denser rain with splashes; a single soft flash instead of the quick flickers.",
                "Denser rain, splashes and soft lightning"),
            new("Graphics", "FxMenuBlur", "Blur behind menus",
                "The game is blurred and darkened behind the inventory and menus.",
                "Blurred, darker game behind the inventory and menus"),
            new("Graphics", "FxSpriteShadows", "Shadows of characters and enemies",
                "Soft shadows under Link and enemies walking on the ground.",
                "Soft shadows under Link and walking enemies"),
        };

        // Lingue disponibili: testo nell'elenco, sigla per zelda3.ini, nome della ROM
        private static readonly (string text, string abbreviated, string full)[] kLanguages =
        {
            ("Dutch", "nl", "Dutch"),
            ("English (EU)", "en", "European"),
            ("English (Redux)", "redux", "Redux"),
            ("English (US)", "us", "English"),
            ("French (CA)", "fr-c", "French-Canadian"),
            ("French (EU)", "fr", "French"),
            ("German", "de", "German"),
            ("Italian", "it", "Italian"),
            ("Polish", "pl", "Polish"),
            ("Portuguese", "pt", "Portuguese"),
            ("Spanish", "es", "Spanish"),
            ("Swedish", "sv", "Swedish"),
        };

        private static readonly string[] kFrequencies = { "48000", "44100", "32000", "22050", "11025" };
        private static readonly string[] kSamples = { "512", "1024", "2048", "4096" };
        private static readonly string[] kMsuVersions = { "true", "deluxe", "opuz", "deluxe-opuz" };

        // Shader "Four Swords" incluso nel progetto (percorso relativo alla cartella zelda3)
        public const string FourSwordsShader = "shaders/foursword/foursword.glslp";

        public static bool IsFourSwordsShader(string? path) =>
            path != null && path.Trim().Replace('\\', '/').EndsWith(FourSwordsShader, StringComparison.OrdinalIgnoreCase);

        public SettingsRequest Request { get; private set; }

        // Pagine: voce nel menu, titolo, sottotitolo, due colonne di riquadri
        // (e, se c'e', un riquadro largo quanto la pagina sopra le colonne)
        private class Page
        {
            public string Title = "", Subtitle = "";
            public NavItem Nav = null!;
            public Panel Panel = null!;
            public Card? Wide;
            public readonly List<Card>[] Columns = { new(), new() };
        }

        private readonly List<Page> pages = new();
        private Page current = null!;
        private readonly Panel sidebar = new() { Dock = DockStyle.Left, Width = 196, BackColor = Theme.Side };
        private readonly Panel main = new() { Dock = DockStyle.Fill, BackColor = Theme.Back };
        private readonly Panel host = new() { Dock = DockStyle.Fill, BackColor = Theme.Back };
        private readonly Label title = new() { AutoSize = true, Font = Theme.Title, ForeColor = Color.White, Location = new Point(22, 14) };
        private readonly Label subtitle = new() { AutoSize = true, Font = Theme.Body, ForeColor = Theme.Muted, Location = new Point(24, 46) };
        private readonly DescBox desc = new() { Dock = DockStyle.Bottom };
        private Panel descHost = null!;
        private readonly Label note = new() { AutoSize = false, Font = Theme.Small, ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleLeft, Text = "You can also change these in game (Esc)" };
        private readonly SlimProgress progressMSU = new() { Visible = false };
        private readonly FlatButton buttonAll = new() { Text = "Enable all", Width = 124 };
        private readonly FlatButton buttonNone = new() { Text = "Disable all", Width = 124 };
        private readonly FlatButton buttonCancel = new() { Text = "Cancel", Width = 104 };
        private readonly FlatButton buttonSave = new() { Text = "Save", Width = 112, Style = FlatButton.Kind.Primary };

        private readonly Dictionary<BoolOption, ToggleSwitch> options = new();
        private readonly List<(DarkComboBox combo, string[] items)> combos = new();
        private readonly KeyBindings keys = new();
        private Panel keysPage = null!;
        private bool loading = true, dirty, saved, failed, refilling;

        // Controlli delle opzioni con una logica propria
        private ToggleSwitch fourSwords = null!, resume = null!, performance = null!, frameDelay = null!, ppu = null!,
            extendY = null!, stretch = null!, linear = null!, mode7 = null!, dimFlashes = null!,
            spriteLimit = null!, unchangedSprites = null!, noVisualFixes = null!,
            customLink = null!, customShader = null!, enableAudio = null!, enableMsu = null!, resumeMsu = null!;
        private DarkComboBox comboCamera = null!, comboTextSpeed = null!, comboLanguage = null!, comboDisplay = null!,
            comboWindowSize = null!, comboRender = null!, comboAspect = null!, comboFrequency = null!,
            comboSamples = null!, comboChannels = null!, comboMsu = null!;
        private Stepper stepSlots = null!, stepScale = null!, stepMsuVolume = null!;
        private TextBox textWidth = null!, textHeight = null!, textLink = null!, textShader = null!, textMsu = null!;
        private FieldRow rowCustomSize = null!, rowLink = null!, rowShader = null!, rowMsuFolder = null!;
        private readonly List<Control> audioRows = new(), msuRows = new();

        public SettingsWindow()
        {
            Text = "Settings";
            Theme.Window(this);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            // larga quanto basta per la scheda dei tasti
            ClientSize = new Size(1180, 700);
            MinimumSize = new Size(900, 600);
            KeyPreview = true;

            BuildFrame();
            BuildGeneral();
            BuildGraphics();
            BuildGameplay();
            BuildItems();
            BuildAudio();
            BuildControls();
            BuildAdvanced();

            Lang.Apply(this, null);
            RefillCombos();

            try
            {
                ImportINI();
            }
            catch
            {
                var answer = Lang.Show("INI file is corrupted and preventing the settings menu from loading.\n\nDo you want to restore to default settings?", "Corrupted Settings", MessageBoxButtons.YesNo);
                if (answer == DialogResult.No) failed = true;
                else
                {
                    IniFile.RestoreDefaults(IniFile.UserIni);
                    try { ImportINI(); } catch { failed = true; }
                }
            }
            loading = false;
            ShowPage(pages[0]);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // il fuoco sulla scheda, non sul primo elenco
            ActiveControl = current.Nav;
            if (failed)
            {
                saved = true;
                BeginInvoke(new MethodInvoker(Close));
            }
        }

        //
        // Struttura della finestra
        //

        private void BuildFrame()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 78, BackColor = Theme.Back };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            descHost = new Panel { Dock = DockStyle.Bottom, Height = 88, BackColor = Theme.Back, Padding = new Padding(22, 12, 22, 0) };
            descHost.Controls.Add(desc);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Theme.Back };
            footer.Controls.AddRange(new Control[] { buttonAll, buttonNone, note, progressMSU, buttonCancel, buttonSave });
            footer.Layout += (s, e) =>
            {
                int y = 14;
                buttonAll.Location = new Point(22, y);
                buttonNone.Location = new Point(buttonAll.Right + 10, y);
                buttonSave.Location = new Point(footer.Width - 22 - buttonSave.Width, y);
                buttonCancel.Location = new Point(buttonSave.Left - 10 - buttonCancel.Width, y);
                note.SetBounds(buttonNone.Right + 16, y, Math.Max(0, buttonCancel.Left - buttonNone.Right - 28), 36);
                progressMSU.SetBounds(note.Left, y + 30, note.Width, 6);
            };

            host.Padding = new Padding(22, 0, 22, 0);

            // Ordine di aggiunta: prima chi riempie, poi i bordi
            main.Controls.Add(host);
            main.Controls.Add(descHost);
            main.Controls.Add(footer);
            main.Controls.Add(header);

            var side = new Label { Text = "Settings", AutoSize = true, Font = Theme.Big, ForeColor = Color.White, Location = new Point(18, 20) };
            sidebar.Controls.Add(side);
            sidebar.Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.ButtonBack);
                e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
            };
            sidebar.Layout += (s, e) =>
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    var nav = pages[i].Nav;
                    bool last = i == pages.Count - 1;
                    nav.SetBounds(10, last ? sidebar.Height - 12 - nav.Height : 62 + i * 42, sidebar.Width - 21, nav.Height);
                }
            };

            Controls.Add(main);
            Controls.Add(sidebar);

            buttonAll.Click += (s, e) => SetAllHd(true);
            buttonNone.Click += (s, e) => SetAllHd(false);
            Hover(buttonAll, "Enable all", "Only the HD improvements: graphics, effects and gameplay.");
            Hover(buttonNone, "Disable all", "Only the HD improvements: graphics, effects and gameplay.");
            buttonSave.Click += (s, e) => Save();
            buttonCancel.Click += (s, e) => Close();
            CancelButton = buttonCancel;

            host.Resize += (s, e) => { if (current != null) LayoutPage(current); };
        }

        private Page AddPage(string nav, string subtitle)
        {
            var p = new Page { Title = nav, Subtitle = subtitle };
            p.Panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Back, Visible = false };
            p.Nav = new NavItem { Text = nav };
            p.Nav.Click += (s, e) => ShowPage(p);
            host.Controls.Add(p.Panel);
            sidebar.Controls.Add(p.Nav);
            pages.Add(p);
            return p;
        }

        private void ShowPage(Page p)
        {
            current = p;
            foreach (var q in pages)
            {
                q.Nav.Selected = q == p;
                q.Panel.Visible = q == p;
            }
            keys.Cancel();
            if (p.Panel == keysPage)
            {
                // legge il controller e zelda3.ini solo se si apre la scheda
                bool was = loading;
                loading = true;
                keys.EnsureLoaded();
                loading = was;
            }
            title.Text = Lang.T(p.Title);
            subtitle.Text = Lang.T(p.Subtitle);
            desc.Show("", p.Panel == keysPage ? "Click a box and press the key or button to assign. Hover over an item for details."
                : "Move the mouse over an option to read what it does.");
            LayoutPage(p);
        }

        // Due colonne di riquadri, larghe uguali; la pagina scorre se serve
        private void LayoutPage(Page p)
        {
            const int gap = 14;
            var pos = p.Panel.AutoScrollPosition;
            int width = p.Panel.ClientSize.Width;
            int colW = (width - gap) / 2;
            int top = 0;
            if (p.Wide != null)
            {
                // il controller disegnato ha bisogno di spazio: sotto una certa larghezza la pagina scorre
                p.Wide.SetBounds(pos.X, pos.Y, Math.Max(width, 860), p.Wide.ContentHeight);
                top = p.Wide.Height + gap;
            }
            int bottom = top;
            for (int c = 0; c < 2; c++)
            {
                int y = top;
                foreach (var card in p.Columns[c])
                {
                    card.SetBounds(c * (colW + gap) + pos.X, y + pos.Y, colW, card.ContentHeight);
                    y += card.Height + gap;
                }
                bottom = Math.Max(bottom, y);
            }
            // Le colonne finiscono alla stessa altezza: l'ultimo riquadro si allunga
            for (int c = 0; c < 2; c++)
            {
                var last = p.Columns[c].LastOrDefault();
                if (last == null) continue;
                last.Height = Math.Max(last.Height, bottom - gap - (last.Top - pos.Y));
            }
            // Senza barra di scorrimento le colonne si allargano a tutta la pagina
            if (p.Panel.ClientSize.Width != width) LayoutPage(p);
        }

        private Card AddCard(Page p, int column, string text)
        {
            var c = new Card { Text = text };
            p.Panel.Controls.Add(c);
            p.Columns[column].Add(c);
            return c;
        }

        // La descrizione in fondo segue il mouse e lo stato attivo
        private void Hover(Control c, string text, string tip)
        {
            void Show(object? s, EventArgs e) => desc.Show(text, tip);
            c.MouseEnter += Show;
            c.GotFocus += Show;
            foreach (Control child in c.Controls) Hover(child, text, tip);
        }

        private ToggleSwitch Toggle(Card card, string text, string tip)
        {
            var t = new ToggleSwitch { Text = text, BackColor = Theme.CardBack };
            t.CheckedChanged += Changed;
            Hover(t, text, tip);
            card.Controls.Add(t);
            return t;
        }

        private ToggleSwitch Toggle(Card card, BoolOption o)
        {
            var t = Toggle(card, o.Text, o.Tip);
            options[o] = t;
            return t;
        }

        private FieldRow Field(Card card, string text, Control editor, string tip, bool stretch = false)
        {
            var row = new FieldRow(text, editor) { Stretch = stretch };
            Hover(row, text, tip);
            card.Controls.Add(row);
            return row;
        }

        private DarkComboBox Combo(Card card, string text, string[] items, string tip, int width = 230)
        {
            var c = new DarkComboBox { Width = width };
            combos.Add((c, items));
            c.SelectedIndexChanged += Changed;
            Field(card, text, c, tip);
            return c;
        }

        private Stepper Step(Card card, string text, int min, int max, int step, string tip)
        {
            var s = new Stepper { Minimum = min, Maximum = max, Step = step, BackColor = Theme.CardBack };
            s.ValueChanged += Changed;
            Field(card, text, s, tip);
            return s;
        }

        // Percorso di un file o di una cartella con il pulsante per sceglierlo
        private FieldRow PathField(Card card, string text, out TextBox box, string tip, Action<TextBox> browse)
        {
            var panel = new Panel { Height = 28, BackColor = Theme.CardBack };
            var input = new InputBox();
            var b = input.Box;
            var button = new FlatButton { Text = "...", Width = 34, Height = 28, Radius = 7, Font = Theme.Body };
            panel.Controls.Add(input);
            panel.Controls.Add(button);
            panel.Layout += (s, e) =>
            {
                button.Location = new Point(panel.Width - button.Width, 0);
                input.SetBounds(0, 0, Math.Max(10, button.Left - 6), panel.Height);
            };
            var box_ = b;
            button.Click += (s, e) => browse(box_);
            b.TextChanged += Changed;
            box = b;
            return Field(card, text, panel, tip, stretch: true);
        }

        private void Link(Card card, string text, string tip, params (string text, string url)[] links)
        {
            var panel = new Panel { Height = 30, BackColor = Theme.CardBack };
            int x = 8;
            var label = new Label { Text = text, AutoSize = true, ForeColor = Theme.Muted, Location = new Point(x, 7) };
            if (text.Length > 0) panel.Controls.Add(label);
            foreach (var (t, url) in links)
            {
                var l = new LinkLabel { Text = t, AutoSize = true, LinkColor = Theme.AccentLight, ActiveLinkColor = Color.White, VisitedLinkColor = Theme.AccentLight };
                l.LinkClicked += (s, e) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                panel.Controls.Add(l);
            }
            // le posizioni dipendono dalla lingua
            panel.Layout += (s, e) =>
            {
                int px = 8;
                foreach (Control c in panel.Controls)
                {
                    c.Location = new Point(px, 7);
                    px += c.PreferredSize.Width + 8;
                }
            };
            Hover(panel, "", tip);
            card.Controls.Add(panel);
        }

        private void Changed(object? sender, EventArgs e)
        {
            if (!loading) dirty = true;
        }

        //
        // Pagine
        //

        private void BuildGeneral()
        {
            var p = AddPage("General", "Window, rendering and performance");

            var window = AddCard(p, 0, "Window");
            comboDisplay = Combo(window, "Display mode", new[] { "Windowed", "Borderless Fullscreen", "Fullscreen" },
                "Windowed: the game plays within a window at your aspect ratio and window scaling. Borderless: fullscreen without changing the screen mode. Fullscreen: exclusive fullscreen mode.");
            comboWindowSize = Combo(window, "Window Size", new[] { "Auto", "Custom" },
                "Auto resizes the window to match the selected aspect ratio; Custom uses the width and height below.");
            var size = new Panel { Width = 200, Height = 28, BackColor = Theme.CardBack };
            var inputWidth = new InputBox { Width = 84, Location = new Point(0, 0) };
            var x = new FlatLabel { Text = "×", AutoSize = false, Width = 32, Height = 28, Location = new Point(84, 0), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Muted };
            var inputHeight = new InputBox { Width = 84, Location = new Point(116, 0) };
            textWidth = inputWidth.Box;
            textHeight = inputHeight.Box;
            textWidth.TextAlign = textHeight.TextAlign = HorizontalAlignment.Right;
            size.Controls.AddRange(new Control[] { inputWidth, x, inputHeight });
            textWidth.TextChanged += Changed;
            textHeight.TextChanged += Changed;
            rowCustomSize = Field(window, "Width × height", size, "Window size in pixels when Window Size is Custom.");
            stepScale = Step(window, "Window scaling", 1, 10, 1, "Size of the game window when not fullscreen: 1 = 100%, 2 = 200%, 3 = 300%...");
            comboWindowSize.SelectedIndexChanged += (s, e) => rowCustomSize.Enabled = comboWindowSize.SelectedIndex == 1;

            var perf = AddCard(p, 1, "Performance");
            comboRender = Combo(perf, "Render method", new[] { "SDL (Default)", "SDL-Software", "OpenGL" },
                "Rendering software used. SDL-software rendering may give better performance on Raspberry Pi. Shaders need OpenGL: choosing SDL turns them off.");
            comboRender.SelectedIndexChanged += (s, e) =>
            {
                // gli shader funzionano solo con OpenGL: scegliendo SDL si spengono
                if (!loading && comboRender.SelectedIndex != 2)
                    fourSwords.Checked = customShader.Checked = false;
            };
            ppu = Toggle(perf, "Optimized SNES PPU", "Use an optimized SNES PPU implementation (potentially buggy)");
            frameDelay = Toggle(perf, "Disable Frame Delay", "Disable the SDL_Delay that happens on each frame (Gives slightly better performance if your display is set to exactly 60Hz)");
            performance = Toggle(perf, "Performance in Title", "Show current performance in window title");
        }

        private void BuildGraphics()
        {
            var p = AddPage("Graphics", "Image, HD look and effects");

            var image = AddCard(p, 0, "Image");
            comboAspect = Combo(image, "Aspect ratio", new[] { "Normal (4:3)", "Widescreen (16:9)", "Steam Deck (16:10)" },
                "This is the aspect ratio which the game will render. All aspect ratios other than Normal will result in black bars on the left and right in various scenarios.");
            extendY = Toggle(image, "Extend Y", "Displays 240 vertical lines rather than the default 224");
            stretch = Toggle(image, "Stretch", "Stretches the graphics to take up the entire window/screen regardless of aspect ratio.");
            linear = Toggle(image, "Linear Filtering", "Use linear filter. Softens image and results in less crisp pixels.");
            mode7 = Toggle(image, "Enhanced Mode7", "Display the world map with higher resolution");
            dimFlashes = Toggle(image, "Dim Flashing", "Softens the intensity of the flashes, not how many there are (like the Virtual Console releases): the screen turns only slightly white during lightning, magic and boss flashes, the intro sword flash is dimmer and the Ether Medallion flickers more slowly.");

            var hd = AddCard(p, 0, "HD look");
            comboCamera = Combo(hd, "Smooth camera", new[] { "Off", "Light", "Medium", "Strong" },
                "Follows Link with a slight delay instead of staying locked to him.");
            Toggle(hd, HighRefresh);
            Toggle(hd, Lighting);
            fourSwords = Toggle(hd, "Four Swords shader", "Painted look in the style of Four Swords Adventures (uses OpenGL). The HUD, text and characters stay sharp.");
            fourSwords.CheckedChanged += (s, e) =>
            {
                if (fourSwords.Checked) customShader.Checked = false;
                UpdateShaderState();
            };

            var effects = AddCard(p, 1, "Effects");
            foreach (var o in kEffects) Toggle(effects, o);
        }

        private void BuildGameplay()
        {
            var p = AddPage("Gameplay", "How the world moves, what the screen shows, dialogue and saves");

            var world = AddCard(p, 0, "World and transitions");
            Toggle(world, FastTransitions);
            Toggle(world, ContinuousWorld);
            Toggle(world, SoftIris);

            var dialogue = AddCard(p, 0, "Dialogue");
            comboTextSpeed = Combo(dialogue, "Speed", new[] { "Original", "Fast holding A or B", "Instant" },
                "How fast the dialogue text appears.");
            Toggle(dialogue, HdText);
            comboLanguage = Combo(dialogue, "Language", kLanguages.Select(l => l.text).ToArray(),
                "Language of the dialogue. Other languages need a ROM of that version; the launcher is in Italian when the dialogue is.");
            comboLanguage.SelectedIndexChanged += comboLanguage_Select;

            var hud = AddCard(p, 1, "Interface");
            Toggle(hud, ModernHud);
            Toggle(hud, XlrInHud);
            Toggle(hud, DungeonNames);
            Toggle(hud, ButtonHints);

            var saves = AddCard(p, 1, "Saves and convenience");
            Toggle(saves, AreaAutosave);
            stepSlots = Step(saves, "Rotating automatic saves", 1, 9, 1, "How many automatic saves to keep (auto0.sav, auto1.sav, ...).");
            resume = Toggle(saves, "Resume where you left off", "Saves the game state when you quit and reloads it at the next start.");
            Toggle(saves, SkipIntro);
            Toggle(saves, NoLowHealthBeep);
            Toggle(saves, MaxInYellow);
        }

        private void BuildItems()
        {
            var p = AddPage("Items", "Items, controls and fixes of the original game");

            var selection = AddCard(p, 0, "Item selection");
            var quick = Toggle(selection, ItemSwitch);
            var limit = Toggle(selection, ItemSwitchLimit);
            quick.CheckedChanged += (s, e) => limit.Enabled = options[XlrInHud].Enabled = quick.Checked;

            var travel = AddCard(p, 0, "Movement and travel");
            Toggle(travel, TurnWhileDashing);
            Toggle(travel, CancelBird);
            Toggle(travel, BirdPoint9);

            var world = AddCard(p, 1, "World and items");
            Toggle(world, MirrorToDarkworld);
            Toggle(world, SwordInteraction);
            Toggle(world, BurnFreeze);
            Toggle(world, MoreBombs);
            Toggle(world, LargerWallet);

            var fixes = AddCard(p, 1, "Bug fixes");
            Toggle(fixes, MiscFixes);
            Toggle(fixes, GameFixes);
            Link(fixes, "List on the wiki:", "What the fixes change, on the zelda3 wiki.",
                ("Minor", "https://github.com/snesrev/zelda3/wiki/Bug-Fixes-:-Misc."),
                ("Game changing", "https://github.com/snesrev/zelda3/wiki/Bug-Fixes-:-Game-Changing"));
        }

        private void BuildAudio()
        {
            var p = AddPage("Audio", "Sound output and MSU music");

            var output = AddCard(p, 0, "Output");
            enableAudio = Toggle(output, "Enable Audio", "Enable audio output by the game");
            comboFrequency = Combo(output, "Frequency", kFrequencies, "DSP frequency in samples per second");
            comboSamples = Combo(output, "Buffer", kSamples, "Audio buffer size in samples");
            comboChannels = Combo(output, "Channels", new[] { "Mono", "Stereo" },
                "Mono: uniform audio output across all speakers. Stereo: outputs audio to the left or right speakers based where the sound is generated on screen.");
            foreach (Control c in output.Controls) if (c != enableAudio) audioRows.Add(c);
            enableAudio.CheckedChanged += (s, e) => UpdateAudioState();

            var msu = AddCard(p, 1, "MSU music");
            enableMsu = Toggle(msu, "Enable MSU", "Enable MSU support for audio.\n\nSupports MSU or MSU Deluxe in PCM or OPUZ format.");
            comboMsu = Combo(msu, "MSU Version", new[] { "MSU", "MSU Deluxe", "OPUZ", "OPUZ Deluxe" }, "Select the version of MSU you wish to use");
            rowMsuFolder = PathField(msu, "Folder", out textMsu, "Directory where MSU files are located", BrowseMsu);
            stepMsuVolume = Step(msu, "MSU volume", 0, 100, 5, "Volume of MSU output (0-100)");
            resumeMsu = Toggle(msu, "Resume MSU", "Resume MSU music from the same point in the track when returning to an area");
            foreach (Control c in msu.Controls) if (c != enableMsu) msuRows.Add(c);
            enableMsu.CheckedChanged += (s, e) => UpdateAudioState();
        }

        private void BuildControls()
        {
            var p = AddPage("Controls", "Keyboard and controller buttons");
            keysPage = p.Panel;
            keys.Status = (t, b) => desc.Show(t, b);

            var pad = new Card { Text = "Controller" };
            pad.Controls.Add(keys.View);
            p.Panel.Controls.Add(pad);
            p.Wide = pad;
            Hover(keys.View.Input, "Input Type", "Select the input type you want to configure. Controller can be chosen when one is connected.");
            Hover(keys.View.AssignAll, "Assign All", "Set each input sequentially (hit ESC to skip to the next input)");
            Hover(keys.View.Defaults, "Default keys", "Puts back the default keys of both keyboard and controller. They are saved with Save.");
            for (int i = 0; i < keys.View.Chips.Length; i++)
                Hover(keys.View.Chips[i], ControllerView.Names[i],
                    "Click the box and press the key, or the controller button if Controller is chosen above. Esc cancels; after 5 seconds the previous one stays.");

            var game = AddCard(p, 0, "Game Functions");
            KeyField(game, "Reset Game", keys.Reset, "Reset the game");
            KeyField(game, "Pause", keys.Pause, "Pause the game without opening the menu");
            keys.Dim = Toggle(game, "Dim When Paused", "If enabled, the game will dim when paused via the special pause key");
            KeyField(game, "Increase Window Size", keys.WindowBigger, "Increase window scale by a factor of 1");
            KeyField(game, "Decrease Window Size", keys.WindowSmaller, "Decrease window scale by a factor of 1");
            KeyField(game, "Volume Up", keys.VolumeUp, "Increases the volume output of the game");
            KeyField(game, "Volume Down", keys.VolumeDown, "Decreases the volume output of the game");

            var perf = AddCard(p, 0, "Performance");
            KeyField(perf, "Toggle Renderer", keys.Renderer, "Switches between the renderers while playing.");
            KeyField(perf, "Toggle FPS", keys.Fps, "Shows or hides the frames per second while playing.");

            var cheats = AddCard(p, 1, "Cheats");
            KeyField(cheats, "Restore Health", keys.Life, "Restores all health. Holding \"Shift\" when pushing the button will refill your other resources.");
            KeyField(cheats, "Give One Key", keys.Keys, "If you have no keys, give yourself a single key.");
            KeyField(cheats, "No Clip", keys.NoClip, "Allows you to walk through walls");
            KeyField(cheats, "Turbo", keys.Turbo, "The game plays at an extremely fast speed.");

            var replays = AddCard(p, 1, "Replays");
            KeyField(replays, "Toggle Replay Speed", keys.ReplayTurbo, "Toggle between Turbo and regular speed of the replays.");
            KeyField(replays, "Stop Replay", keys.StopReplay, "Stop the current replay in progress.");
            // i tasti fissi dei salvataggi rapidi e dei replay
            var note = new Label
            {
                AutoSize = false, Height = 78, Font = Theme.Small, ForeColor = Theme.Muted, BackColor = Theme.CardBack,
                Padding = new Padding(8, 6, 4, 0),
                Text = "F1 - F10: Load savestates/replays\nShift+F1 - F10: Saves savestate/replay\n" +
                    "Ctrl: When held while loading a savestate, the game replays itself up to point of the savestate rather than immediately load the savestate.",
            };
            Hover(note, "Replay Controls", "The game has an integrated replay/savestate system. Here are the controls:");
            replays.Controls.Add(note);
        }

        // Riga con la casella di un tasto del gioco
        private void KeyField(Card card, string text, KeyChip chip, string tip)
        {
            chip.Caption = text;
            Field(card, text, chip, tip);
        }

        private void BuildAdvanced()
        {
            var p = AddPage("Advanced", "Compatibility, custom files and maintenance");

            var compat = AddCard(p, 0, "Compatibility");
            spriteLimit = Toggle(compat, "No Sprite Limit", "Remove the sprite limit per scan line");
            unchangedSprites = Toggle(compat, "Unchanged Sprites", "Avoid changing sprite spawn/die behavior. (Required for replay compatibility)");
            noVisualFixes = Toggle(compat, "No Visual Fixes", "Avoid fixing some graphics glitches. If enabled, memory compare will not work.");

            var maintenance = AddCard(p, 0, "Maintenance");
            var reset = new FlatButton { Text = "Reset", Width = 150, Height = 30, Style = FlatButton.Kind.Danger };
            reset.Click += (s, e) => ResetSettings();
            Field(maintenance, "Default settings", reset, "Reset all settings to their default values. The keys are reset in Controls.");
            var rebuild = new FlatButton { Text = "Rebuild", Width = 150, Height = 30 };
            rebuild.Click += (s, e) => RequestRebuild();
            Field(maintenance, "Game files", rebuild,
                "Downloads the latest version, puts back the game files changed by hand (it asks first) and rebuilds zelda3.exe. Settings and saves are kept.");

            var files = AddCard(p, 1, "Custom files");
            customLink = Toggle(files, "Custom Link Sprites", "Enables custom ZSPR Link sprites");
            rowLink = PathField(files, "", out textLink, "The path where your .zspr file is located", BrowseSprite);
            Link(files, "", "Click here for some custom sprites you can download",
                ("Custom Sprite Downloads", "https://snesrev.github.io/sprites-gfx/snes/zelda3/link/"));
            customShader = Toggle(files, "Use GLSL Shader", "This will enable the use of GLSL shaders.\r\n\r\nThis is only supported by the OpenGL renderer.");
            rowShader = PathField(files, "", out textShader, "The path where your .glsl or .slslp file is located", BrowseShader);
            Link(files, "", "Shaders you can download", ("Suggested Shaders", "https://github.com/snesrev/glsl-shaders"));
            customLink.CheckedChanged += (s, e) => rowLink.Enabled = customLink.Checked;
            customShader.CheckedChanged += (s, e) =>
            {
                if (customShader.Checked) fourSwords.Checked = false;
                UpdateShaderState();
            };
        }

        //
        // Stato dei controlli che dipendono da altri
        //

        private void UpdateShaderState()
        {
            rowShader.Enabled = customShader.Checked;
            // gli shader funzionano solo con OpenGL: accendendone uno si passa a OpenGL
            if (!loading && (fourSwords.Checked || customShader.Checked)) comboRender.SelectedIndex = 2;
        }

        private void UpdateAudioState()
        {
            foreach (var c in audioRows) c.Enabled = enableAudio.Checked;
            enableMsu.Enabled = enableAudio.Checked;
            foreach (var c in msuRows) c.Enabled = enableAudio.Checked && enableMsu.Checked;
        }

        private void SetAllHd(bool on)
        {
            foreach (var (o, t) in options)
                if (o.Hd) t.Checked = on;
            fourSwords.Checked = on;
            if (on)
            {
                if (comboCamera.SelectedIndex == 0) comboCamera.SelectedIndex = 2;
                if (comboTextSpeed.SelectedIndex == 0) comboTextSpeed.SelectedIndex = 1;
            }
            else
            {
                comboCamera.SelectedIndex = 0;
                comboTextSpeed.SelectedIndex = 0;
            }
        }

        // Elenchi tradotti nella lingua del launcher, senza perdere la scelta
        private void RefillCombos()
        {
            refilling = true;
            bool wasLoading = loading;
            loading = true;
            foreach (var (c, items) in combos)
            {
                int i = c.SelectedIndex;
                c.Items.Clear();
                c.Items.AddRange(items.Select(t => (object)Lang.T(t)).ToArray());
                // valori dell'ini che l'elenco non ha (per esempio un buffer scritto a mano)
                if (c.Tag is string extra) c.Items.Add(extra);
                c.SelectedIndex = Math.Min(i, c.Items.Count - 1);
            }
            loading = wasLoading;
            refilling = false;
        }

        private void Relocalize()
        {
            Lang.Apply(this, null);
            keys.View.Invalidate();
            RefillCombos();
            Text = Lang.T("Settings");
            ShowPage(current);
        }

        //
        // Lettura e scrittura di zelda3.ini
        //

        // Valore di una chiave: se manca vale come nel gioco (spento / 0)
        private static int GetInt(IniData ini, string section, string key, int def)
        {
            var v = ini.Sections.ContainsSection(section) ? ini[section][key] : null;
            return int.TryParse(v?.Trim().TrimEnd('%'), out int n) ? n : def;
        }

        // Si/no come li scrive il launcher originale (1/0) o a mano (true, yes, on)
        private static bool GetBool(IniData ini, string section, string key)
        {
            var v = (ini.Sections.ContainsSection(section) ? ini[section][key] : null)?.Trim().ToLowerInvariant();
            if (int.TryParse(v, out int n)) return n != 0;
            return v == "true" || v == "yes" || v == "on";
        }

        private static string Get(IniData ini, string section, string key) =>
            (ini.Sections.ContainsSection(section) ? ini[section][key] : null)?.Trim() ?? "";

        private static void Set(IniData ini, string section, string key, string value, string? comment = null)
        {
            if (!ini.Sections.ContainsSection(section))
                ini.Sections.AddSection(section);
            var keys = ini[section];
            if (keys.ContainsKey(key))
            {
                keys[key] = value;
                return;
            }
            keys.AddKey(key, value);
            if (comment != null)
                keys.GetKeyData(key).Comments = new List<string> { " " + comment };
        }

        private static string Bit(bool b) => b ? "1" : "0";

        // Lingua letta da zelda3.ini (per tornarci se la scelta di un'altra non riesce)
        private string loadedLanguage = "us";

        private static int IndexOf(string[] items, string value)
        {
            for (int i = 0; i < items.Length; i++)
                if (string.Equals(items[i], value, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        private void ImportINI()
        {
            loading = true;
            var iniFile = IniFile.UserIni;

            // Ini mancante: si rimette quello predefinito
            if (!File.Exists(iniFile))
                IniFile.RestoreDefaults(iniFile);

            var ini = IniFile.Load(iniFile);

            foreach (var (o, t) in options)
                t.Checked = o.Hd ? GetInt(ini, o.Section, o.Key, 0) != 0 : GetBool(ini, o.Section, o.Key);
            options[ItemSwitchLimit].Enabled = options[XlrInHud].Enabled = options[ItemSwitch].Checked;
            comboCamera.SelectedIndex = Math.Clamp(GetInt(ini, "Graphics", "SmoothCamera", 0), 0, 3);
            comboTextSpeed.SelectedIndex = Math.Clamp(GetInt(ini, "Features", "TextSpeed", 0), 0, 2);
            stepSlots.Value = Math.Clamp(GetInt(ini, "General", "AutosaveSlots", 3), 1, 9);

            // Generale
            resume.Checked = GetBool(ini, "General", "Autosave");
            performance.Checked = GetBool(ini, "General", "DisplayPerfInTitle");
            frameDelay.Checked = GetBool(ini, "General", "DisableFrameDelay");

            unchangedSprites.Checked = noVisualFixes.Checked = extendY.Checked = false;
            comboAspect.SelectedIndex = 0;
            foreach (var item in Get(ini, "General", "ExtendedAspectRatio").Split(','))
            {
                switch (item.Trim())
                {
                    case "unchanged_sprites": unchangedSprites.Checked = true; break;
                    case "no_visual_fixes": noVisualFixes.Checked = true; break;
                    case "extend_y": extendY.Checked = true; break;
                    case "16:9": comboAspect.SelectedIndex = 1; break;
                    case "16:10": comboAspect.SelectedIndex = 2; break;
                    case "4:3": comboAspect.SelectedIndex = 0; break;
                }
            }

            // La scelta caricata dall'ini non deve rigenerare i file della lingua
            loadedLanguage = Get(ini, "General", "Language").ToLowerInvariant();
            if (loadedLanguage == "") loadedLanguage = "us";
            SelectLanguage(loadedLanguage);

            // Grafica
            var windowSize = Get(ini, "Graphics", "WindowSize");
            var resolution = windowSize.Split('x');
            if (!windowSize.Equals("Auto", StringComparison.OrdinalIgnoreCase) && resolution.Length == 2)
            {
                comboWindowSize.SelectedIndex = 1;
                textWidth.Text = resolution[0].Trim();
                textHeight.Text = resolution[1].Trim();
            }
            else
            {
                comboWindowSize.SelectedIndex = 0;
                textWidth.Text = textHeight.Text = "";
            }
            rowCustomSize.Enabled = comboWindowSize.SelectedIndex == 1;

            comboDisplay.SelectedIndex = Get(ini, "Graphics", "Fullscreen") switch { "1" => 1, "2" => 2, _ => 0 };
            stepScale.Value = Math.Clamp(GetInt(ini, "Graphics", "WindowScale", 2), 1, 10);
            ppu.Checked = GetBool(ini, "Graphics", "NewRenderer");
            mode7.Checked = GetBool(ini, "Graphics", "EnhancedMode7");
            stretch.Checked = GetBool(ini, "Graphics", "IgnoreAspectRatio");
            spriteLimit.Checked = GetBool(ini, "Graphics", "NoSpriteLimits");
            linear.Checked = GetBool(ini, "Graphics", "LinearFiltering");
            dimFlashes.Checked = GetBool(ini, "Graphics", "DimFlashes");
            comboRender.SelectedIndex = Get(ini, "Graphics", "OutputMethod") switch { "SDL-Software" => 1, "OpenGL" => 2, _ => 0 };

            var link = ini.Sections.ContainsSection("Graphics") ? ini["Graphics"]["LinkGraphics"] : null;
            customLink.Checked = link != null;
            textLink.Text = link ?? "";
            rowLink.Enabled = customLink.Checked;

            var shader = Get(ini, "Graphics", "Shader");
            fourSwords.Checked = IsFourSwordsShader(shader);
            customShader.Checked = shader != "" && !fourSwords.Checked;
            textShader.Text = customShader.Checked ? shader : "";
            UpdateShaderState();

            // Audio
            enableAudio.Checked = GetBool(ini, "Sound", "EnableAudio");
            resumeMsu.Checked = GetBool(ini, "Sound", "ResumeMSU");
            textMsu.Text = Get(ini, "Sound", "MSUPath");
            comboChannels.SelectedIndex = Get(ini, "Sound", "AudioChannels") == "1" ? 0 : 1;
            int f = IndexOf(kFrequencies, Get(ini, "Sound", "AudioFreq"));
            comboFrequency.SelectedIndex = f >= 0 ? f : 1;
            var samples = Get(ini, "Sound", "AudioSamples");
            comboSamples.Tag = IndexOf(kSamples, samples) < 0 && int.TryParse(samples, out _) ? samples : null;
            RefillCombos();
            int sm = IndexOf(kSamples, samples);
            comboSamples.SelectedIndex = sm >= 0 ? sm : comboSamples.Tag != null ? kSamples.Length : 1;
            stepMsuVolume.Value = Math.Clamp(GetInt(ini, "Sound", "MSUVolume", 100), 0, 100);
            var msu = Get(ini, "Sound", "EnableMSU").ToLowerInvariant();
            enableMsu.Checked = msu != "false" && msu != "0" && msu != "";
            int mv = IndexOf(kMsuVersions, msu);
            comboMsu.SelectedIndex = mv >= 0 ? mv : 0;
            UpdateAudioState();

            dirty = false;
            loading = false;
        }

        private void SaveToINI()
        {
            var iniFile = IniFile.UserIni;
            var ini = IniFile.Load(iniFile);

            foreach (var (o, t) in options)
            {
                // le opzioni originali si riscrivono solo se cambiate, per non
                // trasformare in 1/0 un "true" scritto a mano
                if (o.Hd || t.Checked != GetBool(ini, o.Section, o.Key))
                    Set(ini, o.Section, o.Key, Bit(t.Checked), o.Comment);
            }
            Set(ini, "Graphics", "SmoothCamera", comboCamera.SelectedIndex.ToString(),
                "Smooth camera: 0 = off, 1 = light, 2 = medium, 3 = strong");
            Set(ini, "Features", "TextSpeed", comboTextSpeed.SelectedIndex.ToString(),
                "Dialogue speed: 0 = original, 1 = fast holding A or B, 2 = instant");
            Set(ini, "General", "AutosaveSlots", stepSlots.Value.ToString(),
                "How many automatic saves to keep in rotation (1-9)");

            // Generale
            Set(ini, "General", "Autosave", Bit(resume.Checked));
            Set(ini, "General", "DisplayPerfInTitle", Bit(performance.Checked));
            Set(ini, "General", "DisableFrameDelay", Bit(frameDelay.Checked));

            var ratio = comboAspect.SelectedIndex switch { 1 => "16:9", 2 => "16:10", _ => "4:3" };
            if (unchangedSprites.Checked) ratio += ", unchanged_sprites";
            if (noVisualFixes.Checked) ratio += ", no_visual_fixes";
            if (extendY.Checked) ratio += ", extend_y";
            Set(ini, "General", "ExtendedAspectRatio", ratio);

            // La lingua si scrive solo se e' stata scelta una lingua dell'elenco:
            // un valore che il launcher non conosce resta com'e'
            if (comboLanguage.SelectedIndex >= 0)
                Set(ini, "General", "Language", kLanguages[comboLanguage.SelectedIndex].abbreviated);

            // Grafica
            Set(ini, "Graphics", "WindowSize", comboWindowSize.SelectedIndex == 1 && int.TryParse(textWidth.Text, out _) && int.TryParse(textHeight.Text, out _)
                ? textWidth.Text.Trim() + "x" + textHeight.Text.Trim() : "Auto");
            Set(ini, "Graphics", "Fullscreen", comboDisplay.SelectedIndex switch { 1 => "1", 2 => "2", _ => "0" });
            Set(ini, "Graphics", "NewRenderer", Bit(ppu.Checked));
            Set(ini, "Graphics", "NoSpriteLimits", Bit(spriteLimit.Checked));
            Set(ini, "Graphics", "LinearFiltering", Bit(linear.Checked));
            Set(ini, "Graphics", "EnhancedMode7", Bit(mode7.Checked));
            Set(ini, "Graphics", "IgnoreAspectRatio", Bit(stretch.Checked));
            Set(ini, "Graphics", "WindowScale", stepScale.Value.ToString());
            Set(ini, "Graphics", "DimFlashes", Bit(dimFlashes.Checked));
            Set(ini, "Graphics", "OutputMethod", comboRender.SelectedIndex switch { 1 => "SDL-Software", 2 => "OpenGL", _ => "SDL" },
                "Output method: SDL, SDL-Software or OpenGL");

            if (customLink.Checked && File.Exists(textLink.Text))
            {
                if (ini["Graphics"]["LinkGraphics"] == null)
                {
                    ini["Graphics"].AddKey("LinkGraphics", textLink.Text);
                    ini["Graphics"].GetKeyData("LinkGraphics").Comments = new List<string>
                    {
                        @" Change the appearance of Link by loading a ZSPR file",
                        @" See all sprites here: https://snesrev.github.io/sprites-gfx/snes/zelda3/link/",
                        " Download the files with \"git clone https://github.com/snesrev/sprites-gfx.git\"",
                    };
                }
                else ini["Graphics"]["LinkGraphics"] = textLink.Text;
            }
            else if (ini["Graphics"]["LinkGraphics"] != null)
            {
                ini["Graphics"].RemoveKey("LinkGraphics");
            }

            // Lo shader vuole il rendering OpenGL (scelto sopra)
            // (un percorso dello shader Four Swords gia' scritto, anche completo, resta com'e')
            if (fourSwords.Checked)
            {
                if (!IsFourSwordsShader(Get(ini, "Graphics", "Shader")))
                    Set(ini, "Graphics", "Shader", FourSwordsShader, "GLSL shader (Four Swords look included with the HD improvements)");
            }
            else if (customShader.Checked && textShader.Text.Trim() != "")
                Set(ini, "Graphics", "Shader", textShader.Text.Trim());
            else
                Set(ini, "Graphics", "Shader", "");

            // Audio
            Set(ini, "Sound", "EnableAudio", Bit(enableAudio.Checked));
            Set(ini, "Sound", "AudioFreq", comboFrequency.SelectedIndex >= 0 ? kFrequencies[comboFrequency.SelectedIndex] : "44100");
            Set(ini, "Sound", "ResumeMSU", Bit(resumeMsu.Checked));
            Set(ini, "Sound", "MSUVolume", stepMsuVolume.Value.ToString());
            if (comboSamples.SelectedIndex >= 0 && comboSamples.SelectedIndex < kSamples.Length)
                Set(ini, "Sound", "AudioSamples", kSamples[comboSamples.SelectedIndex]);
            Set(ini, "Sound", "AudioChannels", comboChannels.SelectedIndex == 0 ? "1" : "2");

            CopyMsu(ini);

            if (enableMsu.Checked)
            {
                int v = Math.Max(0, comboMsu.SelectedIndex);
                Set(ini, "Sound", "EnableMSU", kMsuVersions[v]);
                // i file opuz sono a 48 kHz, i pcm a 44,1 kHz
                Set(ini, "Sound", "AudioFreq", v >= 2 ? "48000" : "44100");
            }
            else Set(ini, "Sound", "EnableMSU", "false");

            // opzione tolta dal gioco (la fa meglio il mondo continuo)
            if (ini.Sections.ContainsSection("Features") && ini["Features"].ContainsKey("ContinuousTransitions"))
                ini["Features"].RemoveKey("ContinuousTransitions");

            IniFile.Save(iniFile, ini);
        }

        // Una cartella MSU scelta fuori dal gioco diventa zelda3\msu (collegamento
        // simbolico, oppure copia dei file se il collegamento non si puo' creare)
        private void CopyMsu(IniData ini)
        {
            var source = textMsu.Text.Trim();
            if (source == "" || source.Split('/')[0].Equals("msu") || !Directory.Exists(source)) return;

            var MSUDir = Path.Combine(Program.repoDir, "msu");
            progressMSU.Maximum = Math.Max(1, Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories).Count());
            progressMSU.Value = 0;
            progressMSU.Visible = true;
            note.Text = Lang.T("Copying MSU Files...");
            note.Refresh();

            if (!MSUDir.Equals(source))
            {
                if (Directory.Exists(MSUDir)) Directory.Delete(MSUDir, true);
                try
                {
                    Directory.CreateSymbolicLink(MSUDir, source);
                }
                catch
                {
                    var answer = Lang.Show("A symlink could not be created.\n\n" +
                        "It could be due to a lack of privileges, so running as administrator could allow for symlink creation. " +
                        "If you are already running the launcher as an administrator, your problem is likely filesystem related and cannot be avoided.\n\n" +
                        "At this point, you can copy the files instead of creating a symlink, but this will take up more space on your drive. " +
                        "Alternatively, you can choose to not copy the files over and the MSU directory setting will not be saved at which point you can restart the launcher as administrator and attempt again.\n\n" +
                        "Would you like to copy the files instead?", "Symlink Creation Failed", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (answer == DialogResult.Yes)
                    {
                        Directory.CreateDirectory(MSUDir);
                        foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                            Directory.CreateDirectory(dirPath.Replace(source, MSUDir));
                        int copied = 0;
                        foreach (string newPath in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                        {
                            File.Copy(newPath, newPath.Replace(source, MSUDir), true);
                            progressMSU.Value = ++copied;
                            progressMSU.Refresh();
                        }
                    }
                }
            }

            if (Directory.Exists(MSUDir))
            {
                var files = Directory.EnumerateFiles(MSUDir, "*.pcm").Select(Path.GetFileName).ToList();
                if (files.Count == 0)
                    files = Directory.EnumerateFiles(MSUDir, "*.opuz").Select(Path.GetFileName).ToList();
                if (files.Count > 0)
                    Set(ini, "Sound", "MSUPath", "msu/" + findMSUPrefix(files!));
            }

            progressMSU.Visible = false;
            note.Text = Lang.T("You can also change these in game (Esc)");
        }

        private static string findMSUPrefix(IList<string> files)
        {
            var length = files.Min(n => n.Length);
            var reference = files[0];
            var result = "";

            for (int i = 0; i < length; i++)
            {
                for (int j = i + 1; j <= length; j++)
                {
                    var substring = reference.Substring(i, j - i);
                    if (substring.Length > result.Length && files.All(n => n.Contains(substring))) result = substring;
                }
            }

            return result;
        }

        //
        // Salvataggio, chiusura, manutenzione
        //

        private bool Save()
        {
            Enabled = false;
            buttonSave.Text = Lang.T("Saving...");
            try
            {
                // I tasti per primi: riscrivono l'ini letto quando si e' aperta la scheda
                keys.Save();
                SaveToINI();
                saved = true;
                DialogResult = DialogResult.OK;
                Close();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.T("Unable to save zelda3.ini:\n\n") + ex.Message, Lang.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                Enabled = true;
                buttonSave.Text = Lang.T("Save");
            }
        }

        // Mentre una casella aspetta il tasto, la tastiera va a lei (anche Esc, Tab e le frecce)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keys.HandleKey(ref msg, keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            keys.Cancel();
            if (!saved && (dirty || keys.Changed))
            {
                var answer = Lang.Show("The changes that you have made will not be saved.\n\nDo you wish to continue?", "Confirmation", MessageBoxButtons.YesNo);
                if (answer == DialogResult.No) e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        private void ResetSettings()
        {
            var answer = Lang.Show("This will reset the INI file immediately and cannot be reversed.\n\nDo you wish to continue?", "Confirmation", MessageBoxButtons.YesNo);
            if (answer == DialogResult.No) return;

            // Si torna all'ini predefinito della versione installata
            if (IniFile.RestoreDefaults(IniFile.UserIni))
            {
                // i tasti hanno in memoria l'ini di prima: si rileggono
                keys.Reload();
                ImportINI();
                Lang.Load();
                Relocalize();
            }
        }

        private void RequestRebuild()
        {
            var answer = Lang.Show("The game will be downloaded again if there is a newer version, the game files changed by hand will be put back and zelda3.exe will be rebuilt.\n\n" +
                "Settings and saves are kept; the changes made here are saved first.\n\nDo you want to continue?",
                "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer == DialogResult.No) return;
            Request = SettingsRequest.Rebuild;
            if (!Save()) Request = SettingsRequest.None;
        }

        private void BrowseSprite(TextBox box)
        {
            using var result = new OpenFileDialog { Filter = "ZSPR sprites (*.zspr)|*.zspr" };
            if (result.ShowDialog(this) != DialogResult.OK || result.FileName == "") return;
            if (!result.FileName.EndsWith(".zspr", StringComparison.OrdinalIgnoreCase))
            {
                Lang.Show("Selected file must be of the .zspr format.", "Invalid File Type", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            box.Text = result.FileName;
        }

        private void BrowseShader(TextBox box)
        {
            using var result = new OpenFileDialog { Filter = "Shaders (*.glsl, *glslp)|*.glsl;*.glslp" };
            if (result.ShowDialog(this) != DialogResult.OK || result.FileName == "") return;
            if (!result.FileName.EndsWith(".glsl", StringComparison.OrdinalIgnoreCase) && !result.FileName.EndsWith(".glslp", StringComparison.OrdinalIgnoreCase))
            {
                Lang.Show("Selected file must be of the .glsl or .glslp formats.", "Invalid File Type", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            box.Text = result.FileName;
        }

        private void BrowseMsu(TextBox box)
        {
            using var result = new FolderBrowserDialog();
            if (result.ShowDialog(this) != DialogResult.OK || string.IsNullOrEmpty(result.SelectedPath)) return;
            if (Directory.GetFiles(result.SelectedPath, "*.pcm").Length == 0 && Directory.GetFiles(result.SelectedPath, "*.opuz").Length == 0)
            {
                Lang.Show("No MSU files were found in the selected directory. Please verify the correct folder was selected and that the proper .pcm or .opuz files are available.", "No MSU Files Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            box.Text = result.SelectedPath;
        }

        //
        // Lingua dei dialoghi
        //

        private void SelectLanguage(string abbreviated)
        {
            refilling = true;
            for (int i = 0; i < kLanguages.Length; i++)
                if (string.Equals(kLanguages[i].abbreviated, abbreviated?.Trim(), StringComparison.OrdinalIgnoreCase))
                    comboLanguage.SelectedIndex = i;
            refilling = false;
            if (!loading && Lang.Italian != (GetLanguage().abbreviated == "it"))
            {
                Lang.Italian = !Lang.Italian;
                Relocalize();
            }
        }

        private (string text, string abbreviated, string full) GetLanguage() =>
            comboLanguage.SelectedIndex >= 0 ? kLanguages[comboLanguage.SelectedIndex] : kLanguages[3];

        private void comboLanguage_Select(object? sender, EventArgs e)
        {
            if (refilling || loading) return;
            var language = GetLanguage();

            // Il launcher e' in italiano quando i dialoghi sono in italiano
            if (Lang.Italian != (language.abbreviated == "it"))
            {
                Lang.Italian = language.abbreviated == "it";
                Relocalize();
            }
            if (language.abbreviated == "us") return;

            // L'italiano non esiste su ROM: i dialoghi vengono da assets/dialogue_it.txt
            // e il font con le lettere accentate si genera da quello inglese.
            if (language.abbreviated == "it")
            {
                var assets = Path.Combine(Program.repoDir, "assets");
                if (!File.Exists(Path.Combine(assets, "dialogue_it.txt")))
                    FetchItalianTranslation(Path.Combine(assets, "dialogue_it.txt"));
                if (!File.Exists(Path.Combine(assets, "dialogue_it.txt")))
                {
                    Lang.Show("The Italian translation (assets\\dialogue_it.txt) was not found.", "Italian",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SelectLanguage("us");
                    return;
                }
                if (!File.Exists(Path.Combine(assets, "font_it.png")) &&
                    runProcess("cmd.exe", @"/C cd .\assets && .\python.exe make_font_it.py"))
                {
                    Lang.Show("Error occurred while creating the Italian font.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
                    SelectLanguage("us");
                    return;
                }
                if (CompileLanguages())
                {
                    Lang.Show("Error occurred while generating language files.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
                    SelectLanguage(loadedLanguage);
                }
                return;
            }

            if (!File.Exists(Path.Combine(Program.repoDir, "assets", "dialogue_" + language.abbreviated + ".txt")) &&
                !File.Exists(Path.Combine(Program.repoDir, "assets", "font_" + language.abbreviated + ".png")))
            {
                var answer = Lang.Show("A ROM of the " + language.text + " version is required to extract the dialogue and unique font.\n\n" +
                    "Would you like to select one now?", language.text + " ROM Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer == DialogResult.No)
                {
                    SelectLanguage("us");
                    return;
                }

                bool exit = false;
                using var result = new OpenFileDialog { Filter = "Zelda 3 ROM (*.sfc)|*.sfc" };
                while (!exit)
                {
                    if (result.ShowDialog(this) == DialogResult.OK)
                    {
                        using var pf = new progressForm();
                        var hashCheck = pf.checkHash(result.FileName, language.abbreviated);
                        if (hashCheck.success)
                        {
                            File.Copy(result.FileName, Path.Combine(Program.repoDir, language.full.ToLower() + ".sfc"), true);
                            exit = true;
                        }
                        else
                        {
                            answer = Lang.Show("ROM hash is not valid for the " + language.full + " version.\n\n" +
                                "The hash of the file selected is " + hashCheck.yourHash + ".\n\n" +
                                "The correct hash is " + hashCheck.hash + ".\n\n" +
                                "Would you like to select another?", "Invalid ROM Hash", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                            if (answer == DialogResult.No)
                            {
                                SelectLanguage("us");
                                return;
                            }
                        }
                    }
                    else
                    {
                        // nessuna ROM scelta: si torna alla lingua di prima
                        SelectLanguage(loadedLanguage);
                        return;
                    }
                }

                if (runProcess("cmd.exe", "/C cd .\\assets && .\\python.exe restool.py --extract-dialogue -r " + language.full.ToLower() + ".sfc"))
                {
                    Lang.Show("Error occurred while extracting dialog.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
                    SelectLanguage(loadedLanguage);
                    return;
                }

                if (CompileLanguages())
                {
                    Lang.Show("Error occurred while generating language files.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
                    SelectLanguage(loadedLanguage);
                }
            }
        }

        // Le risorse del gioco si compilano sempre con tutte le lingue pronte:
        // scegliendone una nuova (o annullando) non se ne perde nessuna.
        // true se c'e' stato un errore, come runProcess.
        private bool CompileLanguages()
        {
            var langs = IniFile.AvailableLanguages();
            if (langs.Length == 0) return false;
            return runProcess("cmd.exe", @"/C cd .\assets && .\python.exe restool.py --languages=" + string.Join(",", langs));
        }

        // La traduzione italiana sta in un repository a parte: si scarica la prima volta
        // (e a ogni aggiornamento, per le correzioni). Si scrive in un file a parte
        // e si sostituisce solo a download riuscito: senza connessione resta la
        // traduzione che c'era.
        internal static void FetchItalianTranslation(string destination)
        {
            var tmp = destination + ".tmp";
            try
            {
                var source = Program.ItalianTranslation;
                if (source.StartsWith("http"))
                {
                    using var client = new HttpClient();
                    var bytes = client.GetByteArrayAsync(source).Result;
                    if (bytes.Length == 0) return;
                    File.WriteAllBytes(tmp, bytes);
                }
                else if (File.Exists(source))
                {
                    File.Copy(source, tmp, true);
                }
                else return;
                File.Move(tmp, destination, true);
            }
            catch
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        public bool runProcess(string filename, string arguments)
        {
            var logFile = Program.currentDirectory + "\\zelda3.log";
            var fileInfo = new FileInfo(logFile);

            if (File.Exists(logFile) && fileInfo.Length > 51200)
            {
                var lines = File.ReadLines(logFile).Skip(10).ToArray();
                File.WriteAllLines(logFile, lines);
            }

            File.AppendAllText(logFile, "\n" + DateTime.Now.ToString() + "\n----------------------------\n");
            File.AppendAllText(logFile, "Executing via " + filename + ":\n " + arguments + "\n");

            var sInfo = new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                WorkingDirectory = Program.repoDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = sInfo };
            if (!IsHandleCreated) CreateHandle();

            process.OutputDataReceived += (s, e) => BeginInvoke(new MethodInvoker(() => File.AppendAllText(logFile, e.Data + "\n")));
            process.ErrorDataReceived += (s, e) => BeginInvoke(new MethodInvoker(() => File.AppendAllText(logFile, e.Data + "\n")));

            UseWaitCursor = true;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            while (!process.HasExited)
            {
                Application.DoEvents();
                Thread.Sleep(15);
            }
            UseWaitCursor = false;

            if (process.ExitCode > 0) return true;

            File.AppendAllText(logFile, "\n");
            return false;
        }
    }
}
