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
            new("Graphics", "FxMenuBlur", "Blur behind menus and dialogues",
                "The game is blurred and darkened behind the inventory, dialogues and menus.",
                "Blurred, darker game behind the inventory, dialogues and menus"),
            new("Graphics", "FxSpriteShadows", "Shadows of characters and enemies",
                "Soft shadows under characters, enemies and objects.",
                "Soft shadows under characters, enemies and objects"),
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
            new("General", "AutosaveOnAreaChange", "Automatic save",
                "Silently saves at every area change and when entering or leaving dungeons and houses. Load it from the in-game menu.",
                "Automatic save at every area change (saves/auto0.sav, auto1.sav, ...)"),
        };

        private readonly Dictionary<BoolOption, CheckBox> checks = new();
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
            ClientSize = new Size(620, 500);

            var note = new Label
            {
                Text = "You can also change these while playing, from the options menu (Esc or right stick click).",
                Location = new Point(12, 9),
                Size = new Size(596, 20),
            };
            Controls.Add(note);

            // Colonna sinistra: grafica ed effetti
            var graphics = MakeGroup("Graphics", 12, 35, 296, 160);
            comboCamera = AddCombo(graphics, "Smooth camera", new[] { "Off", "Light", "Medium", "Strong" },
                "Follows Link with a slight delay instead of staying locked to him.", 22);
            int y = 52;
            foreach (var o in kGraphics) y = AddCheck(graphics, o, y);
            checkShader = new CheckBox { Text = "Four Swords shader", Location = new Point(10, y), AutoSize = true };
            toolTip.SetToolTip(checkShader, "Painted look in the style of Four Swords Adventures (uses OpenGL). The HUD, text and characters stay sharp.");
            graphics.Controls.Add(checkShader);

            var effects = MakeGroup("Effects", 12, 201, 296, 250);
            y = 22;
            foreach (var o in kEffects) y = AddCheck(effects, o, y);

            // Colonna destra: gioco e salvataggi
            var gameplay = MakeGroup("Gameplay", 314, 35, 294, 240);
            comboTextSpeed = AddCombo(gameplay, "Dialogue speed",
                new[] { "Original", "Fast holding A or B", "Instant" },
                "How fast the dialogue text appears.", 22);
            y = 52;
            foreach (var o in kGameplay) y = AddCheck(gameplay, o, y);

            var labelSlots = new Label { Text = "Rotating automatic saves", Location = new Point(10, y + 4), AutoSize = true };
            numericSlots = new NumericUpDown { Minimum = 1, Maximum = 9, Value = 3, Location = new Point(230, y), Size = new Size(50, 23) };
            toolTip.SetToolTip(numericSlots, "How many automatic saves to keep (auto0.sav, auto1.sav, ...).");
            gameplay.Controls.Add(labelSlots);
            gameplay.Controls.Add(numericSlots);

            var buttonAll = new Button { Text = "Enable all", Location = new Point(314, 289), Size = new Size(142, 30) };
            buttonAll.Click += (s, e) =>
            {
                foreach (var c in checks.Values) c.Checked = true;
                checkShader.Checked = true;
                if (comboCamera.SelectedIndex == 0) comboCamera.SelectedIndex = 2;
                if (comboTextSpeed.SelectedIndex == 0) comboTextSpeed.SelectedIndex = 1;
            };
            var buttonNone = new Button { Text = "Disable all", Location = new Point(466, 289), Size = new Size(142, 30) };
            buttonNone.Click += (s, e) =>
            {
                foreach (var c in checks.Values) c.Checked = false;
                checkShader.Checked = false;
                comboCamera.SelectedIndex = 0;
                comboTextSpeed.SelectedIndex = 0;
            };
            Controls.Add(buttonAll);
            Controls.Add(buttonNone);

            var buttonSave = new Button { Text = "Save", Location = new Point(314, 457), Size = new Size(142, 32) };
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
            var buttonCancel = new Button { Text = "Cancel", Location = new Point(466, 457), Size = new Size(142, 32), DialogResult = DialogResult.Cancel };
            Controls.Add(buttonSave);
            Controls.Add(buttonCancel);
            AcceptButton = buttonSave;
            CancelButton = buttonCancel;

            Load_();
            Lang.Apply(this, toolTip);
        }

        private GroupBox MakeGroup(string title, int x, int y, int w, int h)
        {
            var g = new GroupBox { Text = title, Location = new Point(x, y), Size = new Size(w, h) };
            Controls.Add(g);
            return g;
        }

        private int AddCheck(GroupBox g, BoolOption o, int y)
        {
            var c = new CheckBox { Text = o.Text, Location = new Point(10, y), AutoSize = true };
            toolTip.SetToolTip(c, o.Tip);
            g.Controls.Add(c);
            checks[o] = c;
            return y + 25;
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

        private void Load_()
        {
            var ini = Parse();
            foreach (var (o, c) in checks)
                c.Checked = GetInt(ini, o.Section, o.Key, 0) != 0;
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
