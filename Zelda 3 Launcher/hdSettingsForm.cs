using System.Text;
using IniParser;
using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;

namespace Zelda_3_Launcher
{
    // Finestra delle migliorie HD: legge e scrive le opzioni aggiunte in zelda3.ini.
    // Le stesse opzioni si cambiano anche dal menu dentro il gioco (Esc o R3).
    public class hdSettingsForm : Form
    {
        // Opzione si/no: sezione, chiave, testo, spiegazione
        private record BoolOption(string Section, string Key, string Text, string Tip, string Comment);

        private static readonly BoolOption[] kGraphics =
        {
            new("Graphics", "HdText", "Sharp dialogue text",
                "Redraws the dialogue box on top of the shader so the letters stay sharp.",
                "Redraw the dialogue box sharp on top of the shader (1 = on)"),
            new("Graphics", "HighRefreshInterpolation", "Smoothness above 60 Hz",
                "On displays above 60 Hz, scrolling is interpolated in the extra frames. Nothing changes at 60 Hz.",
                "Displays above 60 Hz: scrolling is interpolated in the extra frames"),
            new("Graphics", "DynamicLighting", "Dynamic lighting",
                "Glow around torches, fire and lava; dark rooms are lit only by burning flames.",
                "Dynamic lighting: glow around fire, dark rooms lit by the flames"),
        };

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
            new("Graphics", "FxSoftIris", "Soft transition circle",
                "Soft edge on the circle transition when entering and leaving places.",
                "Soft edge on the circle transition (entering and leaving places)"),
            new("Graphics", "FxButtonHints", "Button hints",
                "On-screen hints for the buttons in the inventory and on the maps.",
                "On-screen hints for the buttons in the inventory and on the maps"),
        };

        private static readonly BoolOption[] kGameplay =
        {
            new("Features", "FastTransitions", "Fast transitions",
                "Moving between areas and rooms happens at double speed.",
                "Transitions between areas and rooms at double speed"),
            new("Features", "ContinuousTransitions", "Continuous area transitions",
                "Between outdoor areas Link keeps walking and the camera eases in and out, without stops.",
                "Continuous transition between outdoor areas"),
            new("Features", "ContinuousWorld", "Continuous world",
                "Outdoors you see the neighbouring areas beyond the edge, the camera follows Link across it and the world remembers what you changed while you stay outdoors.",
                "Continuous world outdoors: neighbouring areas visible, the view follows Link across the edge"),
            new("Features", "ModernHud", "Modern HUD",
                "Hearts and magic top left, Y/A/B buttons top right with the item, the sword and what the A button does (Talk, Read, Open...), rupees and keys bottom left.",
                "Modern HUD: hearts and magic top left, Y/A/B buttons top right, rupees and keys bottom left"),
            new("Features", "ShowXLRItemsInHud", "X, L, R items in the HUD",
                "Shows the items assigned to X, L and R in the HUD (requires Advanced Item Selection).",
                "Show the items assigned to X, L and R in the HUD (requires ItemSwitchLR)"),
            new("Features", "DungeonNames", "Dungeon names",
                "When Link enters a dungeon from outside, its name appears in the middle of the screen like on GBA and disappears as soon as he moves.",
                "Dungeon name in the middle of the screen when entering from outside, like on GBA"),
            new("General", "AutosaveOnAreaChange", "Automatic save",
                "Silently saves at every area change and when entering or leaving dungeons and houses. Load it from the in-game menu.",
                "Automatic save at every area change (saves/auto0.sav, auto1.sav, ...)"),
        };

        // Opzioni originali del gioco (prima nelle impostazioni): "Attiva/Disattiva
        // tutto" non le tocca
        private static readonly BoolOption[] kItems =
        {
            new("Features", "ItemSwitchLR", "Advanced Item Selection",
                "Allows you to switch items with L/R but also allows you to assign items to X, Y, L, and R.\n\nIf X is assigned an item, select will now open the map. To save and quit, you will need to hit Select while in the item menu.\n\nIf L and/or R are assigned an item, quick item switching will be disabled.",
                "Item switch on L/R and items on X, L, R"),
            new("Features", "ItemSwitchLRLimit", "Limit L/R Switch to First 4 Items",
                "Limit the items you will cycle through to just the first four items",
                "L/R only cycle through the first four items"),
            new("Features", "TurnWhileDashing", "Allow Turning While Dashing",
                "Allows you to turn when dashing",
                "Allow turning while dashing"),
            new("Features", "MirrorToDarkworld", "Mirror Teleports to Dark World",
                "Magic Mirror works in both directions",
                "Magic Mirror works in both directions"),
            new("Features", "SwordAndItemsInteraction", "Sword and items world interaction",
                "Swords and items behave like on GBA and interact with the world",
                "Swords and items behave like on GBA and interact with the world"),
            new("Features", "MoreActiveBombs", "More Active Bombs",
                "Can have 4 active bombs instead of 2",
                "4 active bombs instead of 2"),
            new("Features", "CarryMoreRupees", "Larger Wallet",
                "Can carry 9999 rupees",
                "Carry up to 9999 rupees"),
            new("Features", "CancelBirdTravel", "Cancel Bird",
                "Allows you to cancel bird travel by hitting the X button",
                "Cancel bird travel with X"),
            new("Features", "BirdTravelPoint9", "Bird travel point 9",
                "Adds a ninth point to the bird travel map: the eastern summit of Death Mountain, on the plateau with the pegs above Turtle Rock.",
                "Ninth bird travel point on the eastern summit of Death Mountain"),
        };

        private static readonly BoolOption[] kConvenience =
        {
            new("Features", "DisableLowHealthBeep", "Disable Low Heart Beep",
                "Disables the beeping when you are low health",
                "No beep at low health"),
            new("Features", "SkipIntroOnKeypress", "Skip Intro w/ Keypress",
                "Allows you to skip the intro by pressing a key",
                "Skip the intro with a key"),
            new("Features", "ShowMaxItemsInYellow", "Indicate Max Resources",
                "Values will appear as yellow/orange when you have max resources (rupees, arrows, bombs)",
                "Show maxed rupees, arrows and bombs in yellow"),
        };

        private static readonly BoolOption[] kFixes =
        {
            new("Features", "MiscBugFixes", "Miscellaneous Minor Fixes",
                "Miscellaneous bug fixes",
                "Miscellaneous minor bug fixes"),
            new("Features", "GameChangingBugFixes", "Game Changing Bug Fixes",
                "Some bug fixes that are significant enough to impact game behavior",
                "Bug fixes that change game behavior"),
        };

        private readonly Dictionary<BoolOption, CheckBox> checks = new();
        private readonly Dictionary<BoolOption, CheckBox> classic = new();
        private CheckBox checkShader = null!;

        // Shader "Four Swords" incluso nel progetto (percorso relativo alla cartella zelda3)
        public const string FourSwordsShader = "shaders/foursword/foursword.glslp";

        public static bool IsFourSwordsShader(string? path) =>
            path != null && path.Trim().Replace('\\', '/').EndsWith(FourSwordsShader, StringComparison.OrdinalIgnoreCase);
        private ComboBox comboCamera = null!, comboTextSpeed = null!;
        private NumericUpDown numericSlots = null!;
        private readonly ToolTip toolTip = new();

        public hdSettingsForm()
        {
            Text = "HD Improvements";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9F);
            int width = ColX(kColumns) - kGap + 12;

            var note = new Label
            {
                Text = "You can also change these while playing, from the options menu (Esc or right stick click).",
                Location = new Point(12, 9),
                Size = new Size(width - 24, 20),
            };
            Controls.Add(note);

            // Colonna 1: grafica ed effetti
            var graphics = MakeGroup("Graphics", 0);
            comboCamera = AddCombo(graphics, "Smooth camera", new[] { "Off", "Light", "Medium", "Strong" },
                "Follows Link with a slight delay instead of staying locked to him.", 22);
            int y = 52;
            foreach (var o in kGraphics) y = AddCheck(graphics, o, y, checks);
            checkShader = new CheckBox { Text = "Four Swords shader", Location = new Point(10, y), AutoSize = true };
            toolTip.SetToolTip(checkShader, "Painted look in the style of Four Swords Adventures (uses OpenGL). The HUD, text and characters stay sharp.");
            graphics.Controls.Add(checkShader);
            graphics.Height = y + 25 + 6;

            var effects = MakeGroup("Effects", 0);
            effects.Height = AddChecks(effects, kEffects, 22, checks) + 6;

            // Colonna 2: gioco HD, salvataggi e comodita'
            var gameplay = MakeGroup("Gameplay", 1);
            comboTextSpeed = AddCombo(gameplay, "Dialogue speed",
                new[] { "Original", "Fast holding A or B", "Instant" },
                "How fast the dialogue text appears.", 22);
            y = AddChecks(gameplay, kGameplay, 52, checks);

            var labelSlots = new Label { Text = "Rotating automatic saves", Location = new Point(10, y + 4), AutoSize = true };
            numericSlots = new NumericUpDown { Minimum = 1, Maximum = 9, Value = 3, Location = new Point(kColW - 60, y), Size = new Size(50, 23) };
            toolTip.SetToolTip(numericSlots, "How many automatic saves to keep (auto0.sav, auto1.sav, ...).");
            gameplay.Controls.Add(labelSlots);
            gameplay.Controls.Add(numericSlots);
            gameplay.Height = y + 25 + 6;

            var convenience = MakeGroup("Convenience", 1);
            convenience.Height = AddChecks(convenience, kConvenience, 22, classic) + 6;

            // Colonna 3: oggetti, comandi e correzioni del gioco originale
            var items = MakeGroup("Items and controls", 2);
            items.Height = AddChecks(items, kItems, 22, classic) + 6;
            var quick = classic[kItems[0]];
            var lrLimit = classic[kItems[1]];
            quick.CheckedChanged += (s, e) => lrLimit.Enabled = quick.Checked;

            var fixes = MakeGroup("Bug fixes", 2);
            fixes.Height = AddChecks(fixes, kFixes, 22, classic) + 6;
            AddHelpLink(fixes, 22, "https://github.com/snesrev/zelda3/wiki/Bug-Fixes-:-Misc.");
            AddHelpLink(fixes, 47, "https://github.com/snesrev/zelda3/wiki/Bug-Fixes-:-Game-Changing");

            // Colonne della stessa altezza: nelle colonne piu' corte lo spazio in
            // piu' si divide in parti uguali fra le righe
            int bottom = 0;
            for (int c = 0; c < kColumns; c++)
                bottom = Math.Max(bottom, 35 + columns[c].Sum(g => g.Height + kGap) - kGap);
            for (int c = 0; c < kColumns; c++)
            {
                var rows = columns[c].Select(Rows).ToList();
                int extra = bottom - (35 + columns[c].Sum(g => g.Height + kGap) - kGap);
                int total = rows.Sum(r => r.Count), k = 0;
                int Shift(int row) => extra * row / total;
                y = 35;
                for (int i = 0; i < columns[c].Count; i++)
                {
                    var g = columns[c][i];
                    int first = k;
                    foreach (var row in rows[i])
                    {
                        foreach (var ctl in row) ctl.Top += Shift(k) - Shift(first);
                        k++;
                    }
                    g.Location = new Point(ColX(c), y);
                    g.Height += Shift(k) - Shift(first);
                    y += g.Height + kGap;
                }
            }

            int buttonsY = bottom + 12;
            ClientSize = new Size(width, buttonsY + 32 + 12);

            var buttonAll = new Button { Text = "Enable all", Location = new Point(ColX(0), buttonsY), Size = new Size(145, 32) };
            toolTip.SetToolTip(buttonAll, "Only the HD improvements: graphics, effects and gameplay.");
            buttonAll.Click += (s, e) =>
            {
                foreach (var c in checks.Values) c.Checked = true;
                checkShader.Checked = true;
                if (comboCamera.SelectedIndex == 0) comboCamera.SelectedIndex = 2;
                if (comboTextSpeed.SelectedIndex == 0) comboTextSpeed.SelectedIndex = 1;
            };
            var buttonNone = new Button { Text = "Disable all", Location = new Point(ColX(0) + 151, buttonsY), Size = new Size(145, 32) };
            toolTip.SetToolTip(buttonNone, "Only the HD improvements: graphics, effects and gameplay.");
            buttonNone.Click += (s, e) =>
            {
                foreach (var c in checks.Values) c.Checked = false;
                checkShader.Checked = false;
                comboCamera.SelectedIndex = 0;
                comboTextSpeed.SelectedIndex = 0;
            };
            Controls.Add(buttonAll);
            Controls.Add(buttonNone);

            var buttonSave = new Button { Text = "Save", Location = new Point(ColX(2), buttonsY), Size = new Size(145, 32) };
            buttonSave.Click += (s, e) =>
            {
                try
                {
                    Save();
                    DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Lang.T("Unable to save zelda3.ini:\n\n") + ex.Message, Lang.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            var buttonCancel = new Button { Text = "Cancel", Location = new Point(ColX(2) + 151, buttonsY), Size = new Size(145, 32), DialogResult = DialogResult.Cancel };
            Controls.Add(buttonSave);
            Controls.Add(buttonCancel);
            AcceptButton = buttonSave;
            CancelButton = buttonCancel;

            Load_();
            Lang.Apply(this, toolTip);
        }

        // Tre colonne larghe uguali; i gruppi si impilano nella loro colonna
        private const int kColumns = 3, kColW = 296, kGap = 6;
        private readonly List<GroupBox>[] columns = { new(), new(), new() };

        private static int ColX(int column) => 12 + column * (kColW + kGap);

        private GroupBox MakeGroup(string title, int column)
        {
            var g = new GroupBox { Text = title, Size = new Size(kColW, 50) };
            Controls.Add(g);
            columns[column].Add(g);
            return g;
        }

        // Controlli di un gruppo divisi per riga (stessa altezza, a meno di qualche pixel)
        private static List<List<Control>> Rows(GroupBox g)
        {
            var rows = new List<List<Control>>();
            foreach (var ctl in g.Controls.Cast<Control>().OrderBy(x => x.Top))
            {
                if (rows.Count > 0 && ctl.Top - rows[^1][0].Top < 8) rows[^1].Add(ctl);
                else rows.Add(new List<Control> { ctl });
            }
            return rows;
        }

        private int AddCheck(GroupBox g, BoolOption o, int y, Dictionary<BoolOption, CheckBox> into)
        {
            var c = new CheckBox { Text = o.Text, Location = new Point(10, y), AutoSize = true };
            toolTip.SetToolTip(c, o.Tip);
            g.Controls.Add(c);
            into[o] = c;
            return y + 25;
        }

        private int AddChecks(GroupBox g, BoolOption[] options, int y, Dictionary<BoolOption, CheckBox> into)
        {
            foreach (var o in options) y = AddCheck(g, o, y, into);
            return y;
        }

        // "?" in fondo alla riga che apre la spiegazione sul wiki
        private void AddHelpLink(GroupBox g, int y, string url)
        {
            var link = new LinkLabel { Text = "?", Location = new Point(kColW - 24, y + 2), AutoSize = true };
            link.LinkClicked += (s, e) =>
            {
                link.LinkVisited = true;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            };
            g.Controls.Add(link);
            link.BringToFront();
        }

        private ComboBox AddCombo(GroupBox g, string label, string[] items, string tip, int y)
        {
            var l = new Label { Text = label, Location = new Point(10, y + 3), AutoSize = true };
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(140, y), Size = new Size(146, 23) };
            c.Items.AddRange(items.Select(Lang.T).ToArray());
            toolTip.SetToolTip(c, tip);
            toolTip.SetToolTip(l, tip);
            g.Controls.Add(l);
            g.Controls.Add(c);
            return c;
        }

        private static string IniPath => IniFile.UserIni;

        private static IniData Parse() => IniFile.Load(IniPath);

        // Valore di una chiave: se manca vale come nel gioco (spento / 0)
        private static int GetInt(IniData ini, string section, string key, int def)
        {
            var v = ini.Sections.ContainsSection(section) ? ini[section][key] : null;
            return int.TryParse(v?.Trim(), out int n) ? n : def;
        }

        // Si/no come li scrive il launcher originale (1/0) o a mano (true, yes, on)
        private static bool GetBool(IniData ini, string section, string key)
        {
            var v = (ini.Sections.ContainsSection(section) ? ini[section][key] : null)?.Trim().ToLowerInvariant();
            if (int.TryParse(v, out int n)) return n != 0;
            return v == "true" || v == "yes" || v == "on";
        }

        private void Load_()
        {
            var ini = Parse();
            foreach (var (o, c) in checks)
                c.Checked = GetInt(ini, o.Section, o.Key, 0) != 0;
            foreach (var (o, c) in classic)
                c.Checked = GetBool(ini, o.Section, o.Key);
            classic[kItems[1]].Enabled = classic[kItems[0]].Checked;
            comboCamera.SelectedIndex = Math.Clamp(GetInt(ini, "Graphics", "SmoothCamera", 0), 0, 3);
            comboTextSpeed.SelectedIndex = Math.Clamp(GetInt(ini, "Features", "TextSpeed", 0), 0, 2);
            numericSlots.Value = Math.Clamp(GetInt(ini, "General", "AutosaveSlots", 3), 1, 9);
            checkShader.Checked = ini.Sections.ContainsSection("Graphics") && IsFourSwordsShader(ini["Graphics"]["Shader"]);
        }

        private static void Set(IniData ini, string section, string key, string value, string comment)
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
            keys.GetKeyData(key).Comments = new List<string> { " " + comment };
        }

        private void Save()
        {
            var ini = Parse();
            foreach (var (o, c) in checks)
                Set(ini, o.Section, o.Key, c.Checked ? "1" : "0", o.Comment);
            // le opzioni originali si riscrivono solo se cambiate, per non
            // trasformare in 1/0 un "true" scritto a mano
            foreach (var (o, c) in classic)
                if (c.Checked != GetBool(ini, o.Section, o.Key))
                    Set(ini, o.Section, o.Key, c.Checked ? "1" : "0", o.Comment);
            Set(ini, "Graphics", "SmoothCamera", comboCamera.SelectedIndex.ToString(),
                "Smooth camera: 0 = off, 1 = light, 2 = medium, 3 = strong");
            Set(ini, "Features", "TextSpeed", comboTextSpeed.SelectedIndex.ToString(),
                "Dialogue speed: 0 = original, 1 = fast holding A or B, 2 = instant");
            Set(ini, "General", "AutosaveSlots", ((int)numericSlots.Value).ToString(),
                "How many automatic saves to keep in rotation (1-9)");

            // Lo shader vuole il rendering OpenGL; togliendolo si lascia stare un
            // eventuale altro shader scelto a mano nelle impostazioni
            if (checkShader.Checked)
            {
                Set(ini, "Graphics", "Shader", FourSwordsShader, "GLSL shader (Four Swords look included with the HD improvements)");
                Set(ini, "Graphics", "OutputMethod", "OpenGL", "Output method: SDL, SDL-Software or OpenGL");
            }
            else if (ini.Sections.ContainsSection("Graphics") && IsFourSwordsShader(ini["Graphics"]["Shader"]))
            {
                ini["Graphics"]["Shader"] = "";
            }

            IniFile.Save(IniPath, ini);
        }
    }
}
