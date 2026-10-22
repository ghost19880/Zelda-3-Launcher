using IniParser.Model;
using SDL2;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Zelda_3_Launcher
{
    // Tasti di zelda3.ini (sezioni KeyMap e GamepadMap) per la scheda Controlli
    // delle impostazioni. Il tasto si legge dentro la finestra: la tastiera dai
    // messaggi di Windows, convertita nei nomi di SDL che legge zelda3.exe, il
    // controller da SDL. Esc annulla; dopo 5 secondi resta il tasto di prima.
    internal class KeyBindings
    {
        private const double kWait = 5;

        private IniData settings = new();
        private readonly string iniFile = IniFile.UserIni;
        private MyController? controller;
        private bool loaded, touched;
        private int inputType;  // 0 tastiera, 1 controller

        public readonly ControllerView View;
        public readonly KeyChip Reset = new(), Pause = new(), WindowBigger = new(), WindowSmaller = new(),
            VolumeUp = new(), VolumeDown = new(), Life = new(), Keys = new(), NoClip = new(), Turbo = new(),
            Renderer = new(), Fps = new(), ReplayTurbo = new(), StopReplay = new();
        public ToggleSwitch Dim = null!;

        // Titolo e testo da mostrare in fondo alla finestra mentre si aspetta il tasto
        public Action<string, string>? Status;
        public bool Changed => touched;
        public bool Capturing => capture != null;

        public KeyBindings()
        {
            View = new ControllerView();
            View.Input.SelectedIndexChanged += (s, e) => SwitchInput();
            View.AssignAll.Click += (s, e) => AssignAll();
            View.Defaults.Click += (s, e) => RestoreDefaults();
            foreach (var chip in View.Chips) chip.Click += (s, e) => Start((KeyChip)s!);
            foreach (var chip in Commands) chip.Click += (s, e) => Start((KeyChip)s!);
            // Pausa senza modificatori: con Shift davanti e' la pausa che scurisce
            Pause.Modifiers = false;
            foreach (var chip in Commands) chip.Command = true;
        }

        private IEnumerable<KeyChip> Commands => new[] { Reset, Pause, WindowBigger, WindowSmaller, VolumeUp, VolumeDown,
            Life, Keys, NoClip, Turbo, ReplayTurbo, StopReplay, Renderer, Fps };

        //
        // Lettura e scrittura
        //

        // Alla prima apertura della scheda: controller e zelda3.ini
        public void EnsureLoaded()
        {
            if (loaded) return;
            controller ??= new MyController();
            View.Input.SetEnabled(1, controller.connected);
            inputType = controller.connected ? 1 : 0;
            View.Input.SelectedIndex = inputType;
            Load();
            loaded = true;
        }

        // Rilegge l'ini (dopo il ripristino delle impostazioni)
        public void Reload()
        {
            if (!loaded) return;
            Cancel();
            Load();
            touched = false;
        }

        private void Load()
        {
            if (!File.Exists(iniFile))
                IniFile.RestoreDefaults(iniFile);
            settings = IniFile.Load(iniFile);
            ShowAll();
        }

        private void ShowAll()
        {
            ShowPad();
            var k = settings["KeyMap"];
            Life.Key = k["CheatLife"];
            Keys.Key = k["CheatKeys"];
            NoClip.Key = k["CheatWalkThroughWalls"];
            StopReplay.Key = k["StopReplay"];
            Reset.Key = k["Reset"];
            Turbo.Key = k["Turbo"];
            ReplayTurbo.Key = k["ReplayTurbo"];
            WindowBigger.Key = k["WindowBigger"];
            WindowSmaller.Key = k["WindowSmaller"];
            VolumeUp.Key = k["VolumeUp"];
            VolumeDown.Key = k["VolumeDown"];
            Renderer.Key = k["ToggleRenderer"] ?? "r";
            Fps.Key = k["DisplayPerf"] ?? "f";

            // Pause e PauseDimmed: quella con Shift davanti e' l'altra
            bool dim = k["Pause"]?.Contains("Shift") == true;
            Dim.Checked = dim;
            Pause.Key = dim ? k["PauseDimmed"] : k["Pause"];
        }

        private string PadKey => inputType == 1 ? "GamepadMap" : "KeyMap";

        private void ShowPad()
        {
            var keys = (settings[PadKey]["Controls"] ?? "").Replace(" ", "").Split(",");
            for (int i = 0; i < View.Chips.Length; i++)
                View.Chips[i].Key = i < keys.Length ? keys[i] : "";
            if (inputType == 0 && View.Chips[4].Key.Contains("Shift")) View.Chips[4].Key = @"\";
        }

        // Le due serie di tasti (tastiera e controller) restano in memoria:
        // passando dall'una all'altra non si perde niente, si salva tutto insieme
        private void KeepPad()
        {
            settings[PadKey]["Controls"] = string.Join(", ", View.Chips.Select(c => c.Key));
        }

        private void SwitchInput()
        {
            if (!loaded || View.Input.SelectedIndex == inputType) return;
            Cancel();
            KeepPad();
            inputType = View.Input.SelectedIndex;
            ShowPad();
        }

        public void Save()
        {
            if (!loaded) return;
            Cancel();
            KeepPad();
            var k = settings["KeyMap"];
            k["CheatLife"] = Life.Key;
            k["CheatKeys"] = Keys.Key;
            k["CheatWalkThroughWalls"] = NoClip.Key;
            k["StopReplay"] = StopReplay.Key;
            k["Reset"] = Reset.Key;
            k["Turbo"] = Turbo.Key;
            k["ReplayTurbo"] = ReplayTurbo.Key;
            k["WindowBigger"] = WindowBigger.Key;
            k["WindowSmaller"] = WindowSmaller.Key;
            k["VolumeUp"] = VolumeUp.Key;
            k["VolumeDown"] = VolumeDown.Key;
            k["ToggleRenderer"] = Renderer.Key;
            k["DisplayPerf"] = Fps.Key;
            k["Pause"] = Dim.Checked ? "Shift+" + Pause.Key : Pause.Key;
            k["PauseDimmed"] = Dim.Checked ? Pause.Key : "Shift+" + Pause.Key;
            IniFile.Save(iniFile, settings);
        }

        // Tasti predefiniti dall'ini della versione installata (si salvano con Salva)
        private void RestoreDefaults()
        {
            Cancel();
            var defaults = IniFile.DefaultsText();
            if (defaults == null)
            {
                Lang.Show("Unable to find the default zelda3.ini and unable to connect to the internet to download it.",
                    "No Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var backup = IniFile.Parse(defaults);
            foreach (var option in backup["KeyMap"]) settings["KeyMap"][option.KeyName] = option.Value;
            foreach (var option in backup["GamepadMap"]) settings["GamepadMap"][option.KeyName] = option.Value;
            ShowAll();
            touched = true;
        }

        //
        // Attesa del tasto
        //

        private class Pending
        {
            public KeyChip Chip = null!;
            public string Original = "";
            public DateTime Stop;
            public bool LeftTrigger, RightTrigger;
            public TaskCompletionSource Done = new();
        }

        private Pending? capture;
        private readonly System.Windows.Forms.Timer timer = new() { Interval = 15 };
        private bool assigning, stopAssign;
        private bool timerHooked;

        private void Start(KeyChip chip)
        {
            if (assigning) return;
            _ = Capture(chip);
        }

        private Task Capture(KeyChip chip)
        {
            Cancel();
            touched = true;
            capture = new Pending { Chip = chip, Original = chip.Key, Stop = DateTime.Now.AddSeconds(kWait) };
            chip.Waiting = 1;
            View.Hot = chip;
            chip.Focus();

            string what = Lang.T(chip.Caption);
            bool pad = !chip.Command;
            Status?.Invoke(Lang.T("Press a key") + (what.Length > 0 ? " · " + what : ""),
                pad && inputType == 1 ? Lang.T("Press the controller button. Esc cancels, after 5 seconds the previous one stays.")
                : pad ? Lang.T("Press the key. Esc cancels, after 5 seconds the previous one stays.")
                : Lang.T("Press the key (Ctrl, Shift and Alt can go with it) or a controller button. Esc cancels, after 5 seconds the previous one stays."));

            SDL.SDL_PumpEvents();
            SDL.SDL_FlushEvents(SDL.SDL_EventType.SDL_FIRSTEVENT, SDL.SDL_EventType.SDL_LASTEVENT);
            if (!timerHooked)
            {
                timer.Tick += (s, e) => Poll();
                timerHooked = true;
            }
            timer.Start();
            return capture.Done.Task;
        }

        // Fine dell'attesa: il tasto nuovo, oppure null per lasciare quello di prima
        private void Finish(string? key)
        {
            var c = capture;
            if (c == null) return;
            capture = null;
            timer.Stop();
            c.Chip.Waiting = 0;
            c.Chip.Key = key ?? c.Original;
            View.Hot = null;
            Status?.Invoke("", Lang.T("Click a box and press the key or button to assign. Hover over an item for details."));
            c.Done.TrySetResult();
        }

        public void Cancel()
        {
            stopAssign = true;
            Finish(null);
        }

        // Il controller (e il tempo che passa)
        private void Poll()
        {
            var c = capture;
            if (c == null) return;
            bool wantPad = c.Chip.Command || inputType == 1;
            while (SDL.SDL_PollEvent(out var ev) != 0)
            {
                if (!wantPad) continue;
                if (ev.type == SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN)
                {
                    var name = MyController.ConvertButtonID(ev.cbutton.button);
                    if (name != null) { Finish(name); return; }
                }
                else if (ev.type == SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION)
                {
                    // i grilletti contano quando si lasciano
                    var axis = (SDL.SDL_GameControllerAxis)ev.caxis.axis;
                    if (axis == SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT)
                    {
                        if (ev.caxis.axisValue == 0 && c.LeftTrigger) { Finish("L2"); return; }
                        c.LeftTrigger = true;
                    }
                    else if (axis == SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT)
                    {
                        if (ev.caxis.axisValue == 0 && c.RightTrigger) { Finish("R2"); return; }
                        c.RightTrigger = true;
                    }
                }
            }
            double left = (c.Stop - DateTime.Now).TotalSeconds;
            if (left <= 0) Finish(null);
            else c.Chip.Waiting = (float)(left / kWait);
        }

        // La tastiera: la finestra passa qui i tasti mentre si aspetta
        public bool HandleKey(ref Message msg, System.Windows.Forms.Keys keyData)
        {
            var c = capture;
            if (c == null) return false;
            const int WM_KEYDOWN = 0x100, WM_SYSKEYDOWN = 0x104;
            if (msg.Msg != WM_KEYDOWN && msg.Msg != WM_SYSKEYDOWN) return true;

            var code = keyData & System.Windows.Forms.Keys.KeyCode;
            bool extended = ((long)msg.LParam & (1 << 24)) != 0;
            int scan = (int)(((long)msg.LParam >> 16) & 0xff);
            if (code == System.Windows.Forms.Keys.Escape) { Finish(null); return true; }

            // tasti del controller in modalita' controller: dalla tastiera solo Esc
            if (!c.Chip.Command && inputType == 1) return true;

            var key = SdlKey(code, extended, scan);
            if (key == null) return true;
            bool modifier = IsModifier(key.Value);
            // Ctrl, Maiusc e Alt da soli: si aspetta il tasto che li accompagna
            if (c.Chip.Command && modifier) return true;

            // il messaggio d'errore e' una finestra modale: intanto il tempo non conta
            timer.Stop();
            string? result;
            if (c.Chip.Command && c.Chip.Modifiers)
            {
                var mod = SDL.SDL_Keymod.KMOD_NONE;
                if ((keyData & System.Windows.Forms.Keys.Control) != 0) mod |= SDL.SDL_Keymod.KMOD_LCTRL;
                if ((keyData & System.Windows.Forms.Keys.Shift) != 0) mod |= SDL.SDL_Keymod.KMOD_LSHIFT;
                if ((keyData & System.Windows.Forms.Keys.Alt) != 0) mod |= SDL.SDL_Keymod.KMOD_LALT;
                result = FormatKeyInput(key.Value, mod);
            }
            else result = FormatKeyInput(key.Value);
            // tasto rifiutato: resta quello di prima
            Finish(result);
            return true;
        }

        private static bool IsModifier(SDL.SDL_Keycode key) =>
            key is SDL.SDL_Keycode.SDLK_LCTRL or SDL.SDL_Keycode.SDLK_RCTRL or
                SDL.SDL_Keycode.SDLK_LSHIFT or SDL.SDL_Keycode.SDLK_RSHIFT or
                SDL.SDL_Keycode.SDLK_LALT or SDL.SDL_Keycode.SDLK_RALT or
                SDL.SDL_Keycode.SDLK_LGUI or SDL.SDL_Keycode.SDLK_RGUI;

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint code, uint mapType);

        // Tasto di Windows -> tasto di SDL, come lo vedrebbe SDL nel gioco
        private static SDL.SDL_Keycode? SdlKey(System.Windows.Forms.Keys code, bool extended, int scan)
        {
            switch (code)
            {
                case System.Windows.Forms.Keys.ShiftKey: return scan == 0x36 ? SDL.SDL_Keycode.SDLK_RSHIFT : SDL.SDL_Keycode.SDLK_LSHIFT;
                case System.Windows.Forms.Keys.ControlKey: return extended ? SDL.SDL_Keycode.SDLK_RCTRL : SDL.SDL_Keycode.SDLK_LCTRL;
                case System.Windows.Forms.Keys.Menu: return extended ? SDL.SDL_Keycode.SDLK_RALT : SDL.SDL_Keycode.SDLK_LALT;
                case System.Windows.Forms.Keys.Return: return extended ? SDL.SDL_Keycode.SDLK_KP_ENTER : SDL.SDL_Keycode.SDLK_RETURN;
            }
            // frecce e tasti di navigazione senza il bit esteso: sono quelli del tastierino
            if (!extended && kKeypad.TryGetValue(code, out var kp)) return kp;
            if (kSpecial.TryGetValue(code, out var sp)) return sp;
            if (code >= System.Windows.Forms.Keys.F1 && code <= System.Windows.Forms.Keys.F12)
                return SDL.SDL_Keycode.SDLK_F1 + (code - System.Windows.Forms.Keys.F1);
            if (code >= System.Windows.Forms.Keys.NumPad1 && code <= System.Windows.Forms.Keys.NumPad9)
                return SDL.SDL_Keycode.SDLK_KP_1 + (code - System.Windows.Forms.Keys.NumPad1);

            // Tasti con un carattere: quello della tastiera in uso, in minuscolo (come SDL)
            uint ch = MapVirtualKey((uint)code, 2) & 0x7fffffff;
            if (ch == 0) return null;
            return (SDL.SDL_Keycode)char.ToLowerInvariant((char)ch);
        }

        private static readonly Dictionary<System.Windows.Forms.Keys, SDL.SDL_Keycode> kSpecial = new()
            {
                [System.Windows.Forms.Keys.Back] = SDL.SDL_Keycode.SDLK_BACKSPACE,
                [System.Windows.Forms.Keys.Tab] = SDL.SDL_Keycode.SDLK_TAB,
                [System.Windows.Forms.Keys.Space] = SDL.SDL_Keycode.SDLK_SPACE,
                [System.Windows.Forms.Keys.PageUp] = SDL.SDL_Keycode.SDLK_PAGEUP,
                [System.Windows.Forms.Keys.PageDown] = SDL.SDL_Keycode.SDLK_PAGEDOWN,
                [System.Windows.Forms.Keys.End] = SDL.SDL_Keycode.SDLK_END,
                [System.Windows.Forms.Keys.Home] = SDL.SDL_Keycode.SDLK_HOME,
                [System.Windows.Forms.Keys.Left] = SDL.SDL_Keycode.SDLK_LEFT,
                [System.Windows.Forms.Keys.Up] = SDL.SDL_Keycode.SDLK_UP,
                [System.Windows.Forms.Keys.Right] = SDL.SDL_Keycode.SDLK_RIGHT,
                [System.Windows.Forms.Keys.Down] = SDL.SDL_Keycode.SDLK_DOWN,
                [System.Windows.Forms.Keys.Insert] = SDL.SDL_Keycode.SDLK_INSERT,
                [System.Windows.Forms.Keys.Delete] = SDL.SDL_Keycode.SDLK_DELETE,
                [System.Windows.Forms.Keys.NumPad0] = SDL.SDL_Keycode.SDLK_KP_0,
                [System.Windows.Forms.Keys.Multiply] = SDL.SDL_Keycode.SDLK_KP_MULTIPLY,
                [System.Windows.Forms.Keys.Add] = SDL.SDL_Keycode.SDLK_KP_PLUS,
                [System.Windows.Forms.Keys.Subtract] = SDL.SDL_Keycode.SDLK_KP_MINUS,
                [System.Windows.Forms.Keys.Decimal] = SDL.SDL_Keycode.SDLK_KP_PERIOD,
                [System.Windows.Forms.Keys.Divide] = SDL.SDL_Keycode.SDLK_KP_DIVIDE,
                [System.Windows.Forms.Keys.CapsLock] = SDL.SDL_Keycode.SDLK_CAPSLOCK,
                [System.Windows.Forms.Keys.NumLock] = SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR,
                [System.Windows.Forms.Keys.Scroll] = SDL.SDL_Keycode.SDLK_SCROLLLOCK,
                [System.Windows.Forms.Keys.Pause] = SDL.SDL_Keycode.SDLK_PAUSE,
                [System.Windows.Forms.Keys.PrintScreen] = SDL.SDL_Keycode.SDLK_PRINTSCREEN,
                [System.Windows.Forms.Keys.Apps] = SDL.SDL_Keycode.SDLK_APPLICATION,
                [System.Windows.Forms.Keys.LWin] = SDL.SDL_Keycode.SDLK_LGUI,
                [System.Windows.Forms.Keys.RWin] = SDL.SDL_Keycode.SDLK_RGUI,
            };
        private static readonly Dictionary<System.Windows.Forms.Keys, SDL.SDL_Keycode> kKeypad = new()
            {
                [System.Windows.Forms.Keys.Insert] = SDL.SDL_Keycode.SDLK_KP_0,
                [System.Windows.Forms.Keys.End] = SDL.SDL_Keycode.SDLK_KP_1,
                [System.Windows.Forms.Keys.Down] = SDL.SDL_Keycode.SDLK_KP_2,
                [System.Windows.Forms.Keys.PageDown] = SDL.SDL_Keycode.SDLK_KP_3,
                [System.Windows.Forms.Keys.Left] = SDL.SDL_Keycode.SDLK_KP_4,
                [System.Windows.Forms.Keys.Clear] = SDL.SDL_Keycode.SDLK_KP_5,
                [System.Windows.Forms.Keys.Right] = SDL.SDL_Keycode.SDLK_KP_6,
                [System.Windows.Forms.Keys.Home] = SDL.SDL_Keycode.SDLK_KP_7,
                [System.Windows.Forms.Keys.Up] = SDL.SDL_Keycode.SDLK_KP_8,
                [System.Windows.Forms.Keys.PageUp] = SDL.SDL_Keycode.SDLK_KP_9,
                [System.Windows.Forms.Keys.Delete] = SDL.SDL_Keycode.SDLK_KP_PERIOD,
            };

        // Comandi del gioco: il tasto puo' avere Ctrl, Shift e Alt davanti
        // (zelda3.exe legge "Ctrl+o", "Shift+=" e cosi' via)
        private static string? FormatKeyInput(SDL.SDL_Keycode key, SDL.SDL_Keymod mod)
        {
            var prefix = "";
            if ((mod & SDL.SDL_Keymod.KMOD_CTRL) != 0) prefix += "Ctrl+";
            if ((mod & SDL.SDL_Keymod.KMOD_SHIFT) != 0) prefix += "Shift+";
            if ((mod & SDL.SDL_Keymod.KMOD_ALT) != 0) prefix += "Alt+";
            if (prefix == "") return FormatKeyInput(key);

            var result = SDL.SDL_GetKeyName(key);
            if (result.Length.Equals(1))
                result = result.ToLower();

            // F1-F10 con Shift salvano, con Ctrl ripercorrono il replay
            if (result.StartsWith("F") && result.Length is 2 or 3 && result.Substring(1).All(char.IsDigit))
            {
                Lang.Show("The function keys are utilized by zelda3.exe's replay/savestate functionality and thus cannot be used for other inputs.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return prefix + result;
        }

        private static string? FormatKeyInput(SDL.SDL_Keycode key)
        {
            var result = SDL.SDL_GetKeyName(key);

            if (result.Length.Equals(1))
                result = result.ToLower();

            if (result.Contains("Shift"))
            {
                Lang.Show("The Shift keys are not recognized by zelda3.exe and thus cannot be used.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            if (result.Contains("Ctrl"))
            {
                Lang.Show("The Control keys are not recognized by zelda3.exe and thus cannot be used.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            if (result.Contains("Alt"))
            {
                Lang.Show("The Alt keys are not recognized by zelda3.exe and thus cannot be used.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            if (result.StartsWith("F") && result.Length is 2 or 3 && result.Substring(1).All(char.IsDigit))
            {
                Lang.Show("The function keys are utilized by zelda3.exe's replay/savestate functionality and thus cannot be used for other inputs.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            if (result.Length > 0 && result.All(char.IsDigit))
            {
                Lang.Show("The number keys are utilized by zelda3.exe's replay/savestate functionality and thus cannot be used for other inputs.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            if (result.Equals("-") | result.Equals("=") | result.Equals("Backspace"))
            {
                Lang.Show((Lang.Italian ? "Il tasto " : "The ") + result + " key is utilized by zelda3.exe's replay/savestate functionality and thus cannot be used for other inputs.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return result;
        }

        // Tutti i tasti uno dopo l'altro; Esc passa al successivo, il pulsante ferma
        private async void AssignAll()
        {
            if (assigning)
            {
                stopAssign = true;
                Finish(null);
                return;
            }
            assigning = true;
            stopAssign = false;
            View.AssignAll.Text = Lang.T("Stop");
            try
            {
                foreach (var chip in View.Chips.Concat(Commands))
                {
                    if (stopAssign) break;
                    var wait = Capture(chip);
                    // Capture annulla l'attesa precedente e con lei la serie: si riparte
                    stopAssign = false;
                    await wait;
                }
            }
            finally
            {
                assigning = false;
                View.AssignAll.Text = Lang.T("Assign All");
            }
        }
    }

    // Casella di un tasto: si clicca e si preme il tasto nuovo
    internal class KeyChip : Control
    {
        private string key = "";
        private float waiting;
        private bool hover;

        public string Key { get => key; set { key = value ?? ""; Invalidate(); } }
        // Testo della voce (per il messaggio in fondo), in inglese
        public string Caption { get; set; } = "";
        // Comando del gioco (non un pulsante del controller) e se accetta Ctrl/Shift/Alt
        public bool Command { get; set; }
        public bool Modifiers { get; set; } = true;
        // 0 = fermo; altrimenti il tempo che resta (1 -> 0)
        public float Waiting { get => waiting; set { waiting = value; Invalidate(); Parent?.Invalidate(); } }
        public bool Hot => hover || waiting > 0;

        public KeyChip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Size = new Size(112, 28);
            Font = Theme.Bold;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); Parent?.Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); Parent?.Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnMouseDown(MouseEventArgs e) { Focus(); base.OnMouseDown(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (waiting == 0 && (e.KeyCode == System.Windows.Forms.Keys.Space || e.KeyCode == System.Windows.Forms.Keys.Enter))
            {
                e.Handled = e.SuppressKeyPress = true;
                OnClick(EventArgs.Empty);
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.CardBack);
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            bool wait = waiting > 0;
            Theme.FillRound(g, !Enabled ? Theme.Deep : wait ? Theme.Deep : hover ? Theme.Hover : Theme.Input, r, 7);
            if (wait)
            {
                Theme.DrawRound(g, Theme.Accent, r, 7);
                // il tempo che resta, come una barra che si accorcia
                using var b = new SolidBrush(Theme.Accent);
                float w = (Width - 16) * waiting;
                g.FillRectangle(b, 8 + (Width - 16 - w) / 2, Height - 5, w, 2);
            }
            else if (Focused && ShowFocusCues)
                Theme.DrawRound(g, Theme.AccentLight, r, 7);
            string text = wait ? Lang.T("Press...") : key.Length > 0 ? key : "—";
            var color = !Enabled ? Theme.Faint : wait ? Theme.AccentLight : key.Length > 0 ? Theme.Text : Theme.Faint;
            TextRenderer.DrawText(g, text, wait ? Theme.Body : Font, new Rectangle(4, 0, Width - 8, Height - 1), color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    // Il controller disegnato, con le caselle dei 12 pulsanti ai lati
    // collegate al pulsante da una linea; sopra la scelta tastiera/controller
    internal class ControllerView : Control
    {
        public readonly Segmented Input = new(new[] { "Keyboard", "Controller" }) { Width = 230 };
        public readonly FlatButton AssignAll = new() { Text = "Assign All", Width = 150, Height = 30 };
        public readonly FlatButton Defaults = new() { Text = "Default keys", Width = 150, Height = 30 };

        // Nell'ordine di zelda3.ini: su, giu', sinistra, destra, select, start, A, B, X, Y, L, R
        public readonly KeyChip[] Chips = Enumerable.Range(0, 12).Select(_ => new KeyChip()).ToArray();
        public static readonly string[] Names = { "Up", "Down", "Left", "Right", "Select", "Start", "A", "B", "X", "Y", "L", "R" };

        // Le due colonne di caselle, dall'alto: indici in Chips
        private static readonly int[] kLeft = { 10, 0, 2, 3, 1, 4 };
        private static readonly int[] kRight = { 11, 8, 9, 6, 7, 5 };

        private const int kTop = 48, kPadW = 440, kRow = 42, kGap = 34;
        private KeyChip? hot;
        public KeyChip? Hot { get => hot; set { hot = value; Invalidate(); } }

        public ControllerView()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.CardBack;
            Height = kTop + 6 + 5 * kRow + 32;
            Controls.Add(Input);
            Controls.Add(AssignAll);
            Controls.Add(Defaults);
            for (int i = 0; i < Chips.Length; i++)
            {
                Chips[i].Caption = Names[i];
                Chips[i].Width = 104;
                Controls.Add(Chips[i]);
            }
        }

        private int PadLeft => (Width - kPadW) / 2;

        protected override void OnLayout(LayoutEventArgs e)
        {
            Input.Location = new Point(8, 0);
            Defaults.Location = new Point(Width - Defaults.Width - 2, 0);
            AssignAll.Location = new Point(Defaults.Left - 10 - AssignAll.Width, 0);
            int x0 = PadLeft;
            for (int row = 0; row < 6; row++)
            {
                int y = kTop + 6 + row * kRow;
                Chips[kLeft[row]].Location = new Point(x0 - kGap - Chips[kLeft[row]].Width, y);
                Chips[kRight[row]].Location = new Point(x0 + kPadW + kGap, y);
            }
            base.OnLayout(e);
        }

        // Punto del pulsante a cui arriva la linea, nel disegno (440 x 236), e il
        // raggio del pulsante: la linea passa sotto e il puntino resta sul bordo
        private static (PointF at, float radius) Anchor(int i) => i switch
        {
            0 => (new(105, 100), 0),    // su
            1 => (new(105, 160), 0),    // giu'
            2 => (new(75, 130), 0),     // sinistra
            3 => (new(135, 130), 0),    // destra
            4 => (new(186, 160), 0),    // select
            5 => (new(226, 160), 0),    // start
            6 => (new(375, 130), 19),   // A
            7 => (new(335, 170), 19),   // B
            8 => (new(335, 90), 19),    // X
            9 => (new(295, 130), 19),   // Y
            10 => (new(80, 24), 0),     // L
            _ => (new(360, 24), 0),     // R
        };

        // Colori del controller: la forma del Super Nintendo nei toni scuri del launcher
        private static readonly Color kBody = Color.FromArgb(0x2a, 0x34, 0x48), kBodyEdge = Color.FromArgb(0x3a, 0x46, 0x5e),
            kDish = Color.FromArgb(0x24, 0x2d, 0x3f), kDishEdge = Color.FromArgb(0x33, 0x3e, 0x55),
            kPlate = Color.FromArgb(0x19, 0x20, 0x2e), kPill = Color.FromArgb(0x2e, 0x38, 0x4e),
            kShoulder = Color.FromArgb(0x22, 0x2b, 0x3c), kKeys = Color.FromArgb(0x12, 0x18, 0x22);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            Theme.Smooth(g);
            float ox = PadLeft, oy = kTop;
            PointF P(float x, float y) => new(ox + x, oy + y);
            RectangleF R(float x, float y, float w, float h) => new(ox + x, oy + y, w, h);
            RectangleF C(float x, float y, float r) => R(x - r, y - r, 2 * r, 2 * r);

            // Capsula da a a b (due cerchi uniti da un rettangolo ruotato)
            GraphicsPath Capsule(PointF a, PointF b, float r)
            {
                var path = new GraphicsPath();
                float dx = b.X - a.X, dy = b.Y - a.Y, len = MathF.Sqrt(dx * dx + dy * dy);
                float ang = MathF.Atan2(dy, dx) * 180 / MathF.PI;
                path.AddArc(-r, -r, 2 * r, 2 * r, 90, 180);
                path.AddArc(len - r, -r, 2 * r, 2 * r, 270, 180);
                path.CloseFigure();
                using var m = new Matrix();
                m.Translate(ox + a.X, oy + a.Y);
                m.Rotate(ang);
                path.Transform(m);
                return path;
            }

            // Dorsali L e R: spuntano dal bordo di sopra
            Theme.FillRound(g, kShoulder, R(36, 16, 136, 30), 12);
            Theme.FillRound(g, kShoulder, R(268, 16, 136, 30), 12);
            Theme.DrawRound(g, kBodyEdge, R(36, 16, 136, 30), 12);
            Theme.DrawRound(g, kBodyEdge, R(268, 16, 136, 30), 12);

            // Corpo "a osso": due cerchi, sopra dritto, sotto la vita incavata
            using (var body = new GraphicsPath())
            {
                body.AddLine(P(105, 30), P(335, 30));
                body.AddArc(C(335, 130, 100), 270, 205);
                var r1 = P(335 + 100 * MathF.Cos(115 * MathF.PI / 180), 130 + 100 * MathF.Sin(115 * MathF.PI / 180));
                var l1 = P(105 + 100 * MathF.Cos(65 * MathF.PI / 180), 130 + 100 * MathF.Sin(65 * MathF.PI / 180));
                body.AddBezier(r1, P(262, 204), P(178, 204), l1);
                body.AddArc(C(105, 130, 100), 65, 205);
                body.CloseFigure();
                using var fill = new LinearGradientBrush(R(0, 30, 440, 200), Color.FromArgb(0x30, 0x3b, 0x51), kBody, 90f);
                g.FillPath(fill, body);
                using var pen = new Pen(kBodyEdge, 1.5f);
                g.DrawPath(pen, body);
            }

            // Conca della croce e piastra scura dei pulsanti
            using (var b = new SolidBrush(kDish)) g.FillEllipse(b, C(105, 130, 60));
            using (var pen = new Pen(kDishEdge, 1.2f)) g.DrawEllipse(pen, C(105, 130, 60));
            using (var b = new SolidBrush(kPlate)) g.FillEllipse(b, C(335, 130, 79));
            using (var pen = new Pen(kDishEdge, 1.2f)) g.DrawEllipse(pen, C(335, 130, 79));
            // le due capsule in diagonale: Y-X e B-A
            using (var b = new SolidBrush(kPill))
            {
                using var p1 = Capsule(new(295, 130), new(335, 90), 26);
                using var p2 = Capsule(new(335, 170), new(375, 130), 26);
                g.FillPath(b, p1);
                g.FillPath(b, p2);
            }

            // Lettere fuori dai pulsanti, come sul controller vero
            void Letter(string t, float x, float y) =>
                TextRenderer.DrawText(g, t, Theme.Bold, Rectangle.Round(C(x, y, 10)), Theme.Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            Letter("X", 377, 50);
            Letter("A", 418, 96);
            Letter("Y", 251, 154);
            Letter("B", 297, 212);

            // Linee dalle caselle ai pulsanti (sotto i pulsanti)
            var ends = new PointF[Chips.Length];
            for (int row = 0; row < 6; row++)
            {
                foreach (var (i, left) in new[] { (kLeft[row], true), (kRight[row], false) })
                {
                    var chip = Chips[i];
                    var (a, radius) = Anchor(i);
                    var center = P(a.X, a.Y);
                    var start = new PointF(left ? chip.Right + 2 : chip.Left - 2, chip.Top + chip.Height / 2f);
                    var elbow = new PointF(left ? ox + 14 : ox + kPadW - 14, start.Y);
                    float dx = elbow.X - center.X, dy = elbow.Y - center.Y, len = MathF.Max(1, MathF.Sqrt(dx * dx + dy * dy));
                    ends[i] = new PointF(center.X + dx / len * radius, center.Y + dy / len * radius);
                    bool lit = chip == hot || chip.Hot;
                    using var pen = new Pen(lit ? Theme.Accent : Theme.Faint, lit ? 1.6f : 1f);
                    g.DrawLines(pen, new[] { start, elbow, ends[i] });

                    var text = Lang.T(Names[i]);
                    var size = TextRenderer.MeasureText(text, Theme.Body);
                    var at = left ? new Point(chip.Left - 10 - size.Width, chip.Top + (chip.Height - size.Height) / 2)
                        : new Point(chip.Right + 10, chip.Top + (chip.Height - size.Height) / 2);
                    TextRenderer.DrawText(g, text, Theme.Body, at, lit ? Theme.Text : Theme.Muted);
                }
            }

            // Croce con le frecce in rilievo e l'incavo al centro
            using (var cross = new GraphicsPath())
            {
                cross.AddPath(Theme.Round(R(57, 113, 96, 34), 5), false);
                cross.AddPath(Theme.Round(R(88, 82, 34, 96), 5), false);
                cross.FillMode = FillMode.Winding;
                using var b = new SolidBrush(kKeys);
                g.FillPath(b, cross);
            }
            using (var b = new SolidBrush(Color.FromArgb(0x1d, 0x25, 0x33)))
            {
                g.FillEllipse(b, C(105, 130, 9));
                void Arrow(float x, float y, float ux, float uy)
                {
                    // triangolo che punta verso (ux, uy)
                    float px = -uy, py = ux;
                    g.FillPolygon(b, new[] { P(x + ux * 6, y + uy * 6), P(x - ux * 4 + px * 6, y - uy * 4 + py * 6), P(x - ux * 4 - px * 6, y - uy * 4 - py * 6) });
                }
                Arrow(105, 96, 0, -1);
                Arrow(105, 164, 0, 1);
                Arrow(71, 130, -1, 0);
                Arrow(139, 130, 1, 0);
            }

            // Select e Start inclinati
            using (var b = new SolidBrush(kKeys))
            {
                using var s1 = Capsule(new(176, 168), new(196, 152), 6.5f);
                using var s2 = Capsule(new(216, 168), new(236, 152), 6.5f);
                g.FillPath(b, s1);
                g.FillPath(b, s2);
            }
            TextRenderer.DrawText(g, "SELECT", Theme.Caption, Rectangle.Round(R(151, 176, 70, 16)), Theme.Faint, TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, "START", Theme.Caption, Rectangle.Round(R(191, 176, 70, 16)), Theme.Faint, TextFormatFlags.HorizontalCenter);

            // A B X Y con i colori del Super Nintendo e un riflesso in alto
            void Button(float x, float y, Color c)
            {
                using var b = new SolidBrush(c);
                g.FillEllipse(b, C(x, y, 17));
                using var shine = new SolidBrush(Color.FromArgb(70, Color.White));
                g.FillEllipse(shine, R(x - 9, y - 13, 12, 8));
                using var pen = new Pen(Color.FromArgb(90, Color.Black), 1.5f);
                g.DrawEllipse(pen, C(x, y, 17));
            }
            Button(335, 90, Color.FromArgb(0x2f, 0x5f, 0xc8));
            Button(375, 130, Color.FromArgb(0xd0, 0x2e, 0x2e));
            Button(335, 170, Color.FromArgb(0xe6, 0xb4, 0x1e));
            Button(295, 130, Color.FromArgb(0x22, 0x9a, 0x55));

            // Puntini in cima a tutto
            for (int i = 0; i < Chips.Length; i++)
            {
                bool lit = Chips[i] == hot || Chips[i].Hot;
                using var dot = new SolidBrush(lit ? Theme.AccentLight : Theme.Muted);
                g.FillEllipse(dot, ends[i].X - 3, ends[i].Y - 3, 6, 6);
            }
        }
    }

    // Scelta tra poche voci, come pulsanti uniti: la voce scelta e' verde
    internal class Segmented : Control
    {
        private readonly string[] items;
        private readonly bool[] enabled;
        private int selected, hover = -1;
        public event EventHandler? SelectedIndexChanged;

        public int SelectedIndex
        {
            get => selected;
            set
            {
                if (value == selected || value < 0 || value >= items.Length) return;
                selected = value;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Segmented(string[] items)
        {
            this.items = items;
            enabled = items.Select(_ => true).ToArray();
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 30;
            Font = Theme.Bold;
            Cursor = Cursors.Hand;
        }

        public void SetEnabled(int i, bool on) { enabled[i] = on; Invalidate(); }

        private int At(int x) => Math.Clamp(x * items.Length / Math.Max(1, Width), 0, items.Length - 1);

        protected override void OnMouseMove(MouseEventArgs e) { int h = At(e.X); if (h != hover) { hover = h; Invalidate(); } base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            int i = At(e.X);
            if (enabled[i]) SelectedIndex = i;
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.CardBack);
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Theme.FillRound(g, Theme.Deep, r, 9);
            Theme.DrawRound(g, Theme.ButtonBorder, r, 9);
            float w = (Width - 2) / (float)items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                var cell = new RectangleF(0.5f + i * w + 3, 3.5f, w - 6, Height - 8);
                if (i == selected)
                    Theme.FillRound(g, Theme.Accent, cell, 7);
                else if (i == hover && enabled[i])
                    Theme.FillRound(g, Theme.Hover, cell, 7);
                var color = i == selected ? Theme.AccentText : enabled[i] ? Theme.Text : Theme.Faint;
                TextRenderer.DrawText(g, Lang.T(items[i]), Font, Rectangle.Round(cell), color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
