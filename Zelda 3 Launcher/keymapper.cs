using IniParser;
using IniParser.Model;
using IniParser.Model.Configuration;
using IniParser.Parser;
using Microsoft.VisualBasic.Devices;
using SDL2;
using System.Configuration;
using System.Net.Quic;
using System.Text;
using XAct;

namespace Zelda_3_Launcher
{
    public partial class keymapper : Form
    {
        bool changed = false;
        bool saving = false;
        IniData settings = new IniData();
        string iniFile = IniFile.UserIni;
        MyController controller = new MyController();

        public keymapper()
        {
            InitializeComponent();
            Lang.Apply(this, toolTip1);

            ImportINI();

            if (controller.connected)
            {
                radioButtonController.Enabled = true;
                radioButtonController.Checked = true;
            }

            // Controller specific inputs since both keyboard and controller inputs can be set simultaneously
            textBoxDpadLeft.Click += ControllerMappings_Click;
            textBoxDpadUp.Click += ControllerMappings_Click;
            textBoxDpadDown.Click += ControllerMappings_Click;
            textBoxDpadRight.Click += ControllerMappings_Click;
            textBoxSelect.Click += ControllerMappings_Click;
            textBoxStart.Click += ControllerMappings_Click;
            textBoxA.Click += ControllerMappings_Click;
            textBoxB.Click += ControllerMappings_Click;
            textBoxX.Click += ControllerMappings_Click;
            textBoxY.Click += ControllerMappings_Click;
            textBoxL.Click += ControllerMappings_Click;
            textBoxR.Click += ControllerMappings_Click;

            // All other inputs can only be one key/button at a time
            textBoxReset.Click += AssortedMappings_Click;
            textBoxPause.Click += AssortedMappings_Click;
            textBoxIncreaseWindowSize.Click += AssortedMappings_Click;
            textBoxDecreaseWindowSize.Click += AssortedMappings_Click;
            textBoxVolumeDown.Click += AssortedMappings_Click;
            textBoxVolumeUp.Click += AssortedMappings_Click;
            textBoxLife.Click += AssortedMappings_Click;
            textBoxKey.Click += AssortedMappings_Click;
            textBoxNoClip.Click += AssortedMappings_Click;
            textBoxTurbo.Click += AssortedMappings_Click;
            textBoxRenderer.Click += AssortedMappings_Click;
            textBoxFPS.Click += AssortedMappings_Click;
            textBoxToggleReplay.Click += AssortedMappings_Click;
            textBoxStopReplay.Click += AssortedMappings_Click;
        }

        private void AssortedMappings_Click(object? sender, EventArgs e)
        {
            EnableWindow(false);
            AssortedMappings(sender);
            EnableWindow(true);
        }

        private void AssortedMappings(object? sender)
        {
            TextBox textBox = sender as TextBox;
            var original = textBox.Text;
            string result;

            var leftTrigger = false;
            var rightTrigger = false;

            // Clear textbox
            textBox.Text = Lang.T("");
            this.Refresh();

            SDL.SDL_PumpEvents();
            SDL.SDL_FlushEvents(SDL.SDL_EventType.SDL_FIRSTEVENT, SDL.SDL_EventType.SDL_LASTEVENT);

            var window = SDL.SDL_CreateWindow("Awaiting input...", Form.ActiveForm.Location.X + 292, Form.ActiveForm.Location.Y + 199, 150, 50,
                SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_GRABBED |
                SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS |
                SDL.SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP);

            SDL.SDL_ShowCursor(SDL.SDL_DISABLE);

            var stop = DateTime.Now.AddSeconds(5);

            while (true)
            {
                SDL.SDL_Event ev;

                // Wait for an event to occur
                SDL.SDL_PollEvent(out ev);

                switch (ev.type)
                {
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                        if (SDL.SDL_GetKeyName(ev.key.keysym.sym) == "Escape")
                        {
                            textBox.Text = original;
                            SDL.SDL_DestroyWindow(window);
                            return;
                        }
                        else
                        {
                            textBox.Text = FormatKeyInput(ev.key.keysym.sym);
                            SDL.SDL_DestroyWindow(window);
                            return;
                        }
                    case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                        textBox.Text = (MyController.ConvertButtonID(ev.cbutton.button));
                        SDL.SDL_DestroyWindow(window);
                        return;
                    case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                        if (ev.caxis.axis == ((byte)SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT) && ev.caxis.axisValue == 0 && leftTrigger)
                        {
                            textBox.Text = Lang.T("L2");
                            SDL.SDL_DestroyWindow(window);
                            return;
                        }
                        else if (ev.caxis.axis == ((byte)SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT) && ev.caxis.axisValue == 0 && rightTrigger)
                        {
                            textBox.Text = Lang.T("R2");
                            SDL.SDL_DestroyWindow(window);
                            return;
                        }
                        else if (ev.caxis.axis == ((byte)SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT))
                        {
                            leftTrigger = true;
                        }
                        else if (ev.caxis.axis == ((byte)SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT))
                        {
                            rightTrigger = true;
                        }
                        break;
                }

                if (DateTime.Now >= stop)
                {
                    textBox.Text = original;
                    SDL.SDL_DestroyWindow(window);
                    return;
                }
            }
        }

        private string? FormatKeyInput(SDL.SDL_Keycode key)
        {
            var result = SDL.SDL_GetKeyName(key);

            if (result.Length.Equals(1))
            {
                result = result.ToLower();
            }

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

            if (result.Contains("F") && result.Length == 2 | result.Length == 3)
            {
                Lang.Show("The function keys are utilized by zelda3.exe's replay/savestate functionality and thus cannot be used for other inputs.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            if (result.IsNumeric())
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

        private void ControllerMappings_Click(object? sender, EventArgs e)
        {
            EnableWindow(false);
            ControllerMappings(sender);
            EnableWindow(true);
        }

        private void ControllerMappings(object? sender)
        {
            TextBox textBox = sender as TextBox;
            var original = textBox.Text;
            string result;

            var selection = groupBoxInput.Controls.OfType<RadioButton>()
                .FirstOrDefault(n => n.Checked)!.OrigText();

            // Clear textbox
            textBox.Text = Lang.T("");
            this.Refresh();

            if (selection == "Controller") result = controller.GetButtonName();
            else result = GetKeyInput();

            if (result is null) textBox.Text = original;
            else
            {
                textBox.Text = result;
                changed = true;
            }
        }

        private string? GetKeyInput()
        {
            SDL.SDL_PumpEvents();
            SDL.SDL_FlushEvent(SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN);

            var window = SDL.SDL_CreateWindow("Awaiting input...", Form.ActiveForm.Location.X + 292, Form.ActiveForm.Location.Y + 199, 150, 50,
                SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_GRABBED |
                SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS |
                SDL.SDL_WindowFlags.SDL_WINDOW_ALWAYS_ON_TOP);

            SDL.SDL_ShowCursor(SDL.SDL_DISABLE);

            var stop = DateTime.Now.AddSeconds(5);

            while (true)
            {
                SDL.SDL_Event ek;

                // Wait for an event to occur
                SDL.SDL_PollEvent(out ek);

                // Check if the event was a joystick button press
                if (ek.type == SDL.SDL_EventType.SDL_KEYDOWN)
                {
                    if (SDL.SDL_GetKeyName(ek.key.keysym.sym) == "Escape")
                    {
                        SDL.SDL_DestroyWindow(window);
                        return null;
                    }
                    else
                    {
                        SDL.SDL_DestroyWindow(window);
                        return FormatKeyInput(ek.key.keysym.sym);
                    }
                }

                if (DateTime.Now >= stop)
                {
                    SDL.SDL_DestroyWindow(window);
                    return null;
                }
            }
        }

        private void SaveINI()
        {
            var selection = groupBoxInput.Controls.OfType<RadioButton>()
                .FirstOrDefault(n => n.Checked)!.OrigText();

            SaveControllerSettings(selection);

            SaveAdditionalSettings();

            FileINI();
        }

        private void SaveAdditionalSettings()
        {
            settings["KeyMap"]["CheatLife"] = textBoxLife.Text;
            settings["KeyMap"]["CheatKeys"] = textBoxKey.Text;
            settings["KeyMap"]["CheatWalkThroughWalls"] = textBoxNoClip.Text;
            settings["KeyMap"]["StopReplay"] = textBoxStopReplay.Text;
            settings["KeyMap"]["Reset"] = textBoxReset.Text;
            settings["KeyMap"]["Turbo"] = textBoxTurbo.Text;
            settings["KeyMap"]["ReplayTurbo"] = textBoxToggleReplay.Text;
            settings["KeyMap"]["WindowBigger"] = textBoxIncreaseWindowSize.Text;
            settings["KeyMap"]["WindowSmaller"] = textBoxDecreaseWindowSize.Text;
            settings["KeyMap"]["VolumeUp"] = textBoxVolumeUp.Text;
            settings["KeyMap"]["VolumeDown"] = textBoxVolumeDown.Text;
            settings["KeyMap"]["ToggleRenderer"] = textBoxRenderer.Text;
            settings["KeyMap"]["DisplayPerf"] = textBoxFPS.Text;

            if (checkBoxDim.Checked)
            {
                settings["KeyMap"]["Pause"] = "Shift+" + textBoxPause.Text;
                settings["KeyMap"]["PauseDimmed"] = textBoxPause.Text;
            }
            else
            {

                settings["KeyMap"]["PauseDimmed"] = "Shift+" + textBoxPause.Text;
                settings["KeyMap"]["Pause"] = textBoxPause.Text;
            }
        }

        private void ImportINI()
        {
            // Ini mancante: si rimette quello predefinito
            if (!File.Exists(iniFile))
                IniFile.RestoreDefaults(iniFile);

            settings = IniFile.Load(iniFile);

            // KeyMap Settings
            DisplayController();

            // Additional Settings
            textBoxLife.Text = settings["KeyMap"]["CheatLife"];
            textBoxKey.Text = settings["KeyMap"]["CheatKeys"];
            textBoxNoClip.Text = settings["KeyMap"]["CheatWalkThroughWalls"];
            textBoxStopReplay.Text = settings["KeyMap"]["StopReplay"];
            textBoxReset.Text = settings["KeyMap"]["Reset"];
            textBoxTurbo.Text = settings["KeyMap"]["Turbo"];
            textBoxToggleReplay.Text = settings["KeyMap"]["ReplayTurbo"];
            textBoxIncreaseWindowSize.Text = settings["KeyMap"]["WindowBigger"];
            textBoxDecreaseWindowSize.Text = settings["KeyMap"]["WindowSmaller"];
            textBoxVolumeUp.Text = settings["KeyMap"]["VolumeUp"];
            textBoxVolumeDown.Text = settings["KeyMap"]["VolumeDown"];

            if (settings["KeyMap"]["ToggleRenderer"] != null)
            {
                textBoxRenderer.Text = settings["KeyMap"]["ToggleRenderer"];
            }
            else
            {
                textBoxRenderer.Text = Lang.T("r");
            }

            if (settings["KeyMap"]["DisplayPerf"] != null)
            {
                textBoxFPS.Text = settings["KeyMap"]["DisplayPerf"];
            }
            else
            {
                textBoxFPS.Text = Lang.T("f");
            }

            if (settings["KeyMap"]["Pause"].Contains("Shift"))
            {
                checkBoxDim.Checked = true;
                textBoxPause.Text = settings["KeyMap"]["PauseDimmed"];
            }
            else
            {
                checkBoxDim.Checked = false;
                textBoxPause.Text = settings["KeyMap"]["Pause"];
            }
        }

        private void DisplayController()
        {
            var selection = groupBoxInput.Controls.OfType<RadioButton>()
                .FirstOrDefault(n => n.Checked)!.OrigText();

            if (selection == "Controller")
            {
                var controllerControls = settings["GamepadMap"]["Controls"].Replace(" ", "").Split(",");

                textBoxDpadUp.Text = controllerControls[0];
                textBoxDpadDown.Text = controllerControls[1];
                textBoxDpadLeft.Text = controllerControls[2];
                textBoxDpadRight.Text = controllerControls[3];
                textBoxSelect.Text = controllerControls[4];
                textBoxStart.Text = controllerControls[5];
                textBoxA.Text = controllerControls[6];
                textBoxB.Text = controllerControls[7];
                textBoxX.Text = controllerControls[8];
                textBoxY.Text = controllerControls[9];
                textBoxL.Text = controllerControls[10];
                textBoxR.Text = controllerControls[11];
            }
            else
            {
                var keyControls = settings["KeyMap"]["Controls"].Replace(" ", "").Split(",");

                textBoxDpadUp.Text = keyControls[0];
                textBoxDpadDown.Text = keyControls[1];
                textBoxDpadLeft.Text = keyControls[2];
                textBoxDpadRight.Text = keyControls[3];
                textBoxSelect.Text = keyControls[4];
                textBoxStart.Text = keyControls[5];
                textBoxA.Text = keyControls[6];
                textBoxB.Text = keyControls[7];
                textBoxX.Text = keyControls[8];
                textBoxY.Text = keyControls[9];
                textBoxL.Text = keyControls[10];
                textBoxR.Text = keyControls[11];

                if (textBoxSelect.Text.Contains("Shift")) textBoxSelect.Text = @"\";
            }
        }

        private void InputType_CheckedChanged(object sender, EventArgs e)
        {
            var selection = groupBoxInput.Controls.OfType<RadioButton>()
                .FirstOrDefault(n => !n.Checked)!.OrigText();

            if (changed)
            {
                var answer = Lang.Show("Do you want to save your changes to " + Lang.T(selection) + " settings?", "Save Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer == DialogResult.Yes)
                {
                    SaveControllerSettings(selection);
                    FileINI();
                }
            }

            DisplayController();
        }

        private void FileINI()
        {
            IniFile.Save(iniFile, settings);
        }

        private void SaveControllerSettings(string selection)
        {
            if (selection == "Controller")
            {
                settings["GamepadMap"]["Controls"] =
                    textBoxDpadUp.Text + ", " +
                    textBoxDpadDown.Text + ", " +
                    textBoxDpadLeft.Text + ", " +
                    textBoxDpadRight.Text + ", " +
                    textBoxSelect.Text + ", " +
                    textBoxStart.Text + ", " +
                    textBoxA.Text + ", " +
                    textBoxB.Text + ", " +
                    textBoxX.Text + ", " +
                    textBoxY.Text + ", " +
                    textBoxL.Text + ", " +
                    textBoxR.Text;
            }
            else
            {
                settings["KeyMap"]["Controls"] =
                    textBoxDpadUp.Text + ", " +
                    textBoxDpadDown.Text + ", " +
                    textBoxDpadLeft.Text + ", " +
                    textBoxDpadRight.Text + ", " +
                    textBoxSelect.Text + ", " +
                    textBoxStart.Text + ", " +
                    textBoxA.Text + ", " +
                    textBoxB.Text + ", " +
                    textBoxX.Text + ", " +
                    textBoxY.Text + ", " +
                    textBoxL.Text + ", " +
                    textBoxR.Text;
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            saving = true;

            SaveINI();

            this.Close();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            RestoreFromBackupINI();

            FileINI();

            ImportINI();
        }

        private void RestoreFromBackupINI()
        {
            // Tasti predefiniti: dall'ini predefinito della versione installata
            var defaults = IniFile.DefaultsText();
            if (defaults == null)
            {
                Lang.Show("Unable to find the default zelda3.ini and unable to connect to the internet to download it.",
                    "No Connection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var backupSettings = IniFile.Parse(defaults);

            foreach (var option in backupSettings["KeyMap"])
            {
                settings["KeyMap"][option.KeyName] = option.Value;
            }

            foreach (var option in backupSettings["GamepadMap"])
            {
                settings["GamepadMap"][option.KeyName] = option.Value;
            }
        }

        private void formClosing(object sender, FormClosingEventArgs e)
        {
            if (saving) return;

            var answer = Lang.Show("The changes that you have made will not be saved.\n\nDo you wish to continue?", "Confirmation", MessageBoxButtons.YesNo);

            if (answer == DialogResult.No) e.Cancel = true;

            return;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonAssignAll_Click(object sender, EventArgs e)
        {
            EnableWindow(false);

            var selection = groupBoxInput.Controls.OfType<RadioButton>()
                .FirstOrDefault(n => n.Checked)!.OrigText();

            // Controller assignments
            buttonAssignAll.Text = Lang.T("Up");
            ControllerMappings(textBoxDpadUp);

            buttonAssignAll.Text = Lang.T("Down");
            ControllerMappings(textBoxDpadDown);

            buttonAssignAll.Text = Lang.T("Left");
            ControllerMappings(textBoxDpadLeft);

            buttonAssignAll.Text = Lang.T("Right");
            ControllerMappings(textBoxDpadRight);

            buttonAssignAll.Text = Lang.T("Select");
            ControllerMappings(textBoxSelect);

            buttonAssignAll.Text = Lang.T("Start");
            ControllerMappings(textBoxStart);

            buttonAssignAll.Text = Lang.T("A");
            ControllerMappings(textBoxA);

            buttonAssignAll.Text = Lang.T("B");
            ControllerMappings(textBoxB);

            buttonAssignAll.Text = Lang.T("X");
            ControllerMappings(textBoxX);

            buttonAssignAll.Text = Lang.T("Y");
            ControllerMappings(textBoxY);

            buttonAssignAll.Text = Lang.T("L");
            ControllerMappings(textBoxL);

            buttonAssignAll.Text = Lang.T("R");
            ControllerMappings(textBoxR);

            // Assorted functionality assignments
            buttonAssignAll.Text = Lang.T("Reset");
            AssortedMappings(textBoxReset);

            buttonAssignAll.Text = Lang.T("Pause");
            AssortedMappings(textBoxPause);

            buttonAssignAll.Text = Lang.T("Increase Window Size");
            AssortedMappings(textBoxIncreaseWindowSize);

            buttonAssignAll.Text = Lang.T("Decrease Window Size");
            AssortedMappings(textBoxDecreaseWindowSize);

            buttonAssignAll.Text = Lang.T("Volume Up");
            AssortedMappings(textBoxVolumeUp);

            buttonAssignAll.Text = Lang.T("Volume Down");
            AssortedMappings(textBoxVolumeDown);

            buttonAssignAll.Text = Lang.T("Restore Health");
            AssortedMappings(textBoxLife);

            buttonAssignAll.Text = Lang.T("One Key");
            AssortedMappings(textBoxKey);

            buttonAssignAll.Text = Lang.T("No Clip");
            AssortedMappings(textBoxNoClip);

            buttonAssignAll.Text = Lang.T("Turbo");
            AssortedMappings(textBoxTurbo);

            buttonAssignAll.Text = Lang.T("Toggle Replay Speed");
            AssortedMappings(textBoxToggleReplay);

            buttonAssignAll.Text = Lang.T("Stop Replay");
            AssortedMappings(textBoxStopReplay);

            buttonAssignAll.Text = Lang.T("Toggle Renderer");
            AssortedMappings(textBoxRenderer);

            buttonAssignAll.Text = Lang.T("Toggle FPS");
            AssortedMappings(textBoxFPS);

            buttonAssignAll.Text = Lang.T("Assign All");
            EnableWindow(true);
        }

        private void EnableWindow(bool input)
        {
            groupBoxCheats.Enabled = input;
            groupBoxController.Enabled = input;
            groupBoxGame.Enabled = input;
            groupBoxReplays.Enabled = input;
            groupBoxPerformance.Enabled = input;

            buttonReset.Enabled = input;
            buttonCancel.Enabled = input;
            buttonSave.Enabled = input;
        }
    }
}
