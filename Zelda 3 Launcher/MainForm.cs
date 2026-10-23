using System.Diagnostics;
using LibGit2Sharp;

namespace Zelda_3_Launcher
{
    public partial class MainForm : Form
    {
        // Stato del repository del gioco, letto in sottofondo
        private record RepoState(bool Exists, bool Online, int Behind, int Modified, string Sha);

        private RepoState repoState = new(false, true, 0, 0, "");
        private bool checking, building, running;

        public MainForm()
        {
            InitializeComponent();
            Lang.Apply(this, null);
            UpdateTexts();
            RefreshStatus(fetch: true);

            // Controlla se su GitHub c'e' una versione piu' recente del launcher
            this.Shown += (s, e) => { KeepFocus(); LauncherUpdate.CheckAsync(this); };
        }

        private FlatButton[] Buttons => new[] { launch, settings, build };

        // Il focus resta su un pulsante attivo: se quello che lo aveva si spegne
        // (Gioca mentre il gioco gira, Aggiorna durante il controllo) passa al primo
        private void KeepFocus()
        {
            if (ActiveControl is FlatButton { Enabled: true }) return;
            var first = Buttons.FirstOrDefault(b => b.Enabled && b.Visible);
            if (first != null) ActiveControl = first;
        }

        // Tab, Maiusc+Tab e le frecce passano da un pulsante all'altro; Invio e
        // Spazio premono quello scelto
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            if ((key is Keys.Tab or Keys.Up or Keys.Down or Keys.Left or Keys.Right) && (keyData & (Keys.Control | Keys.Alt)) == 0)
            {
                bool forward = key is Keys.Down or Keys.Right || (key == Keys.Tab && (keyData & Keys.Shift) == 0);
                var list = Buttons.Where(b => b.Enabled && b.Visible).ToList();
                if (list.Count > 0)
                {
                    int i = ActiveControl is FlatButton fb ? list.IndexOf(fb) : -1;
                    // Tab fa il giro, le frecce si fermano al primo e all'ultimo
                    int next = i < 0 ? 0 : key == Keys.Tab ? (i + (forward ? 1 : list.Count - 1)) % list.Count
                        : Math.Clamp(i + (forward ? 1 : -1), 0, list.Count - 1);
                    FlatButton.KeyboardCues = true;
                    ActiveControl = list[next];
                    foreach (var b in Buttons) b.Invalidate();
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Relocalize()
        {
            Lang.Apply(this, null);
            UpdateTexts();
        }

        private static bool GameBuilt => File.Exists(Path.Combine(Program.repoDir, "zelda3.exe"));

        // Testi e stato dei pulsanti secondo il repository e quello che sta succedendo
        private void UpdateTexts()
        {
            bool busy = building || running;
            launch.Text = Lang.T(running ? "Running..." : "Play");
            launch.Enabled = !busy && GameBuilt;
            settings.Text = Lang.T("Settings");
            settings.Enabled = !busy && GameBuilt;

            if (!repoState.Exists && !checking)
            {
                build.Text = Lang.T("Download");
                build.Enabled = !busy;
            }
            else if (checking)
            {
                build.Text = Lang.T("Checking for updates...");
                build.Enabled = false;
            }
            else if (repoState.Behind > 0)
            {
                build.Text = Lang.T("Update") + " · " + string.Format(Lang.T("{0} new"), repoState.Behind);
                build.Enabled = !busy;
            }
            else
            {
                build.Text = Lang.T("Update") + " · " + Lang.T(repoState.Online ? "no news" : "offline");
                build.Enabled = false;
            }
            if (building) build.Text = Lang.T("Building...");

            if (ContainsFocus || ActiveControl == null) KeepFocus();
            status.Invalidate();
        }

        private async void RefreshStatus(bool fetch)
        {
            if (!Directory.Exists(Program.repoDir))
            {
                repoState = new(false, true, 0, 0, "");
                UpdateTexts();
                return;
            }
            checking = true;
            UpdateTexts();
            repoState = await Task.Run(() => ReadRepo(fetch));
            checking = false;
            UpdateTexts();
        }

        private static RepoState ReadRepo(bool fetch)
        {
            try
            {
                using var repo = new Repository(Program.repoDir);
                bool online = true;
                // Controlla se nel repository ci sono novita' (senza connessione si va avanti)
                if (fetch)
                {
                    try
                    {
                        var remote = repo.Network.Remotes["origin"];
                        Commands.Fetch(repo, remote.Name, remote.FetchRefSpecs.Select(x => x.Specification), null, null);
                    }
                    catch
                    {
                        online = false;
                    }
                }
                int behind = repo.Head.TrackingDetails.BehindBy ?? 0;
                var sha = repo.Head.Tip?.Sha ?? "";
                return new(true, online, behind, SourceChanges(repo), sha.Length > 7 ? sha[..7] : sha);
            }
            catch
            {
                return new(false, true, 0, 0, "");
            }
        }

        // Sorgenti modificati a mano. zelda3.ini non conta: cambia a ogni salvataggio
        // delle impostazioni e il launcher lo conserva durante l'aggiornamento.
        private static int SourceChanges(Repository repo)
        {
            int n = 0;
            foreach (var entry in repo.RetrieveStatus(new StatusOptions { IncludeUntracked = false }))
            {
                if (entry.State != FileStatus.Unaltered && entry.State != FileStatus.Ignored && entry.FilePath != "zelda3.ini")
                    n++;
            }
            return n;
        }

        private void launch_click(object sender, EventArgs e)
        {
            running = true;
            UpdateTexts();

            this.WindowState = FormWindowState.Minimized;
            if (runProcess("cmd.exe", "/C zelda3.exe"))
            {
                Lang.Show("Error occurred while launching Zelda 3.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
            }

            // la lingua puo' essere cambiata dal menu del gioco
            running = false;
            Lang.Load();
            Relocalize();

            this.WindowState = FormWindowState.Normal;
        }

        private void settings_click(object sender, EventArgs e)
        {
            SettingsRequest request;
            using (var window = new SettingsWindow())
            {
                window.ShowDialog(this);
                request = window.Request;
            }

            // La lingua del launcher segue quella dei dialoghi salvata in zelda3.ini
            Lang.Load();
            Relocalize();

            if (request == SettingsRequest.Rebuild)
                RunBuild(Lang.T("Re-build"));
            else
                RefreshStatus(fetch: false);
        }

        private void build_Click(object sender, EventArgs e)
        {
            RunBuild(Lang.T(repoState.Exists ? "Update" : "Download"));
        }

        private void RunBuild(string action)
        {
            // Il ripristino del repository riporta i sorgenti allo stato scaricato:
            // le modifiche fatte a mano ai file del repository andrebbero perse.
            if (Directory.Exists(Program.repoDir))
            {
                int modified = 0;
                try
                {
                    using (var repo = new Repository(Program.repoDir))
                        modified = SourceChanges(repo);
                }
                catch { }
                if (modified > 0)
                {
                    var answer = Lang.Show("The zelda3 folder contains local changes to the source files.\n\n" +
                        action + " will reset the source files to the original repository and these changes will be lost.\n\n" +
                        "Do you want to continue?", "Local changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);
                    if (answer == DialogResult.No) return;
                }
            }

            building = true;
            UpdateTexts();
            try
            {
                Build();
            }
            finally
            {
                building = false;
                progressCompile.Visible = false;
                labelCompileStatus.Visible = false;
                LayoutControls();
                File.AppendAllText(Program.currentDirectory + "\\zelda3.log", "\n\n\n\n");
                RefreshStatus(fetch: false);
            }
        }

        private void Build()
        {
            using (progressForm cloneRepo = new progressForm("Repository Download", "Downloading a fresh copy of the zelda3 repository..."))
            {
                cloneRepo.ShowDialog(this);
            }

            if (!Directory.Exists(Program.repoDir)) return;

            using (progressForm copyROM = new progressForm("Copying ROM File", "Copying ROM file to proper directory..."))
            {
                copyROM.ShowDialog(this);
            }

            if (!File.Exists(Path.Combine(Program.repoDir, "zelda3.sfc")))
            {
                Lang.Show("No ROM provided. Process cancelled.", "No ROM", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (progressForm downloadTCC = new progressForm("Downloading TCC", "Downloading TCC build tools..."))
            {
                downloadTCC.ShowDialog(this);
            }

            using (progressForm downloadSDL2 = new progressForm("Downloading SDL2", "Downloading SDL2..."))
            {
                downloadSDL2.ShowDialog(this);
            }

            using (progressForm downloadPython = new progressForm("Downloading Python", "Downloading Python..."))
            {
                downloadPython.ShowDialog(this);
            }

            using (progressForm downloadPip = new progressForm("Downloading pip", "Downloading pip..."))
            {
                downloadPip.ShowDialog(this);
            }

            progressCompile.Visible = true;
            labelCompileStatus.Visible = true;
            progressCompile.Value = 0;
            LayoutControls();

            var pythonEXE = @".\assets\python.exe";
            var python311 = @".\zelda3\assets\python311._pth";
            var python311Old = @".\zelda3\assets\python311._pth.old";

            File.AppendAllText(Program.currentDirectory + "\\zelda3.log", "Starting commandline processess...");

            Step("Modifying python311._pth...");
            // Modify python311._pth to allow for pip installation
            File.Move(python311, python311Old);
            using (var pythonFile = File.AppendText(python311))
            {
                foreach (var line in File.ReadLines(python311Old))
                {
                    if (line == "#import site") pythonFile.WriteLine("import site");
                    else pythonFile.WriteLine(line);
                }
            }
            File.Delete(python311Old);

            // Download pip
            Step("Downloading pip...");
            if (runProcess("cmd.exe", "/C " + pythonEXE + @" .\assets\get-pip.py"))
            {
                Lang.Show("Error occurred while downloding pip.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
                return;
            }

            // Install dependencies
            Step("Installing dependencies...");
            if (runProcess("cmd.exe", @"/C " + pythonEXE + " -m pip install --upgrade pip pillow pyyaml"))
            {
                Lang.Show("Error occurred while installing/updating dependencies.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
                return;
            }

            // Extract assets
            Step("Extracting assets...");
            if (runProcess("cmd.exe", @"/C cd .\assets && .\python.exe restool.py --extract-from-rom"))
            {
                Lang.Show("Error occurred while extracting resources.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
                return;
            }

            // L'estrazione compila solo l'inglese: si aggiungono le lingue pronte
            // (l'italiano sempre, se c'e' la traduzione), altrimenti dopo ogni
            // aggiornamento i dialoghi tornerebbero in inglese
            var assetsDir = Path.Combine(Program.repoDir, "assets");
            SettingsWindow.FetchItalianTranslation(Path.Combine(assetsDir, "dialogue_it.txt"));
            if (File.Exists(Path.Combine(assetsDir, "dialogue_it.txt")) && !File.Exists(Path.Combine(assetsDir, "font_it.png")))
                runProcess("cmd.exe", @"/C cd .\assets && .\python.exe make_font_it.py");
            var langs = IniFile.AvailableLanguages().Where(l => l != "it" || File.Exists(Path.Combine(assetsDir, "font_it.png"))).ToArray();
            if (langs.Length > 0 &&
                runProcess("cmd.exe", @"/C cd .\assets && .\python.exe restool.py --languages=" + string.Join(",", langs)))
            {
                Lang.Show("Error occurred while generating language files.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
                return;
            }

            // Se su GitHub c'e' l'exe veloce (MSVC) di questo commit si usa quello
            Step("Downloading zelda3.exe...");
            if (GameExe.TryDownload())
            {
                progressCompile.Value += 2;
            }
            else
            {
                // Need to make some small modifications to bat before exectuting
                Step("Modifying installation bat...");
                var batOld = Path.Combine(Program.repoDir, "run_with_tcc.bat");
                var batNew = Path.Combine(Program.repoDir, "radzprower.bat");
                using (var pythonFile = File.AppendText(batNew))
                {
                    foreach (var line in File.ReadLines(batOld))
                    {
                        if (line != "pause" && line != "echo Running..." && line != "zelda3.exe") pythonFile.WriteLine(line);
                    }
                }

                // build zelda3.exe
                Step("Building zelda3.exe...");
                if (runProcess("cmd.exe", @"/C radzprower.bat"))
                {
                    Lang.Show("Error occurred while building executable.\n\nPlease refer to " + Path.Combine(Program.currentDirectory, "zelda3.log") + " for further details.");
                    return;
                }
            }

            // zelda3.ini si sistema sul posto (senza spostarlo e senza creare una
            // seconda riga Language); se manca si rimette quello predefinito
            Step("Modifying zelda3.ini...");
            var iniFile = IniFile.UserIni;
            if (File.Exists(iniFile)) IniFile.PrepareInPlace(iniFile);
            else IniFile.RestoreDefaults(iniFile);
        }

        private void Step(string text)
        {
            progressCompile.Value++;
            labelCompileStatus.Text = Lang.T(text);
            labelCompileStatus.Refresh();
            progressCompile.Refresh();
        }

        //
        // Finestra
        //

        private readonly TriforceLogo logo = new();
        private readonly Label heading = new() { Text = "A Link to the Past HD", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.White };
        private readonly Label caption = new() { Text = "ZELDA 3 LAUNCHER", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Font = Theme.Caption, ForeColor = Theme.Muted };
        private StatusPanel status = null!;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.launch = new FlatButton { Style = FlatButton.Kind.Primary, Height = 54, Font = new Font("Segoe UI", 12F, FontStyle.Bold), Radius = 11, TabIndex = 0 };
            this.settings = new FlatButton { Height = 40, TabIndex = 1 };
            this.build = new FlatButton { Height = 40, TabIndex = 2 };
            this.labelCompileStatus = new Label { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Muted, Font = Theme.Small, Visible = false };
            this.progressCompile = new SlimProgress { Maximum = 8, Visible = false };
            this.status = new StatusPanel(this);
            this.SuspendLayout();

            this.launch.Click += new System.EventHandler(this.launch_click);
            this.settings.Click += new System.EventHandler(this.settings_click);
            this.build.Click += new System.EventHandler(this.build_Click);

            Theme.Window(this);
            this.Controls.AddRange(new Control[] { logo, heading, caption, launch, settings, build, labelCompileStatus, progressCompile, status });
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Text = "Zelda 3 Launcher HD";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            LayoutControls();
            this.ResumeLayout(false);
        }

        private void LayoutControls()
        {
            const int w = 300, pad = 22;
            logo.Location = new Point((w - logo.Width) / 2, 22);
            heading.SetBounds(0, logo.Bottom + 8, w, 28);
            caption.SetBounds(0, heading.Bottom, w, 18);
            launch.SetBounds(pad, caption.Bottom + 18, w - 2 * pad, launch.Height);
            settings.SetBounds(pad, launch.Bottom + 10, w - 2 * pad, settings.Height);
            build.SetBounds(pad, settings.Bottom + 10, w - 2 * pad, build.Height);
            int y = build.Bottom + 14;
            if (progressCompile.Visible)
            {
                labelCompileStatus.SetBounds(pad, y, w - 2 * pad, 20);
                progressCompile.SetBounds(pad, labelCompileStatus.Bottom + 2, w - 2 * pad, 8);
                y = progressCompile.Bottom + 14;
            }
            status.SetBounds(pad, y, w - 2 * pad, 76);
            ClientSize = new Size(w, status.Bottom + pad);
        }

        // Riquadro in basso: versione del gioco, modifiche locali, versione del launcher
        private class StatusPanel : Control
        {
            private readonly MainForm form;

            public StatusPanel(MainForm form)
            {
                this.form = form;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                SetStyle(ControlStyles.Selectable, false);
                TabStop = false;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Theme.Back);
                Theme.Smooth(g);
                Theme.FillRound(g, Theme.Deep, new RectangleF(0, 0, Width - 1, Height - 1), 10);

                var s = form.repoState;
                int y = 10;
                void Line(params (string text, Font font, Color color)[] parts)
                {
                    int x = 12;
                    foreach (var (text, font, color) in parts)
                    {
                        TextRenderer.DrawText(g, text, font, new Point(x, y), color, TextFormatFlags.NoPadding);
                        x += TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                    }
                    y += 19;
                }

                if (s.Exists)
                    Line((Lang.T("Game") + ": ", Theme.Body, Theme.Muted), (s.Sha, Theme.Bold, Color.White));
                else
                    Line((Lang.T("Game") + ": ", Theme.Body, Theme.Muted), (Lang.T("not downloaded"), Theme.Body, Theme.Warn));

                if (s.Modified > 0)
                    Line(new[] { (s.Modified == 1 ? Lang.T("Modified locally (1 file)") : string.Format(Lang.T("Modified locally ({0} files)"), s.Modified), Theme.Body, Theme.Warn) });
                else if (s.Exists && !GameBuilt)
                    Line(new[] { (Lang.T("Not built yet"), Theme.Body, Theme.Warn) });
                else if (s.Exists)
                    Line(new[] { (Lang.T("No local changes"), Theme.Body, Theme.Muted) });
                else
                    Line(new[] { (Lang.T("Press Download to get the game"), Theme.Body, Theme.Muted) });

                var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new System.Version(0, 0, 0);
                Line(("Launcher: ", Theme.Body, Theme.Muted), (v.ToString(3), Theme.Bold, Color.White),
                    (" · .NET " + Environment.Version.Major, Theme.Body, Theme.Muted));
            }
        }

        public Boolean runProcess(string filename, string arguments)
        {
            var logFile = Program.currentDirectory + "\\zelda3.log";

            bool createdNew;
            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "console process", out createdNew);
            var signaled = false;

            if(!createdNew)
            {
                waitHandle.Set();
            }

            File.AppendAllText(logFile, "\n" + DateTime.Now.ToString() + "\n----------------------------\n");

            File.AppendAllText(logFile, "Executing via " + filename + ":\n " + arguments + "\n");

            ProcessStartInfo sInfo = new ProcessStartInfo();
            sInfo.FileName = filename;
            sInfo.Arguments = arguments;
            sInfo.WorkingDirectory = Program.repoDir;
            sInfo.RedirectStandardOutput = true;
            sInfo.RedirectStandardError = true;
            sInfo.UseShellExecute = false;
            sInfo.CreateNoWindow = true;

            Process process = new Process();
            process.StartInfo = sInfo;
            processes.Add(process);

            while (!IsHandleCreated)
            {
                this.CreateHandle();
            }

            process.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    File.AppendAllText(logFile, e.Data + "\n");
                }));
            });

            process.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    File.AppendAllText(logFile, e.Data + "\n");
                }));
            });

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            do
            {
                Application.DoEvents();

                signaled = waitHandle.WaitOne(TimeSpan.FromSeconds(1));
            } while (!process.HasExited);

            processes.Remove(process);

            if (process.ExitCode > 0)
            {
                return true;
            }

            process.Close();

            File.AppendAllText(logFile, "\n");

            if (File.Exists(logFile))
            {
                var fileInfo = new FileInfo(logFile);

                while (fileInfo.Length > (51200))
                {
                    var lines = File.ReadLines(logFile).ToArray();

                    File.WriteAllLines(logFile, lines.Skip(1).ToArray());

                    fileInfo = new FileInfo(logFile);
                }
            }

            return false;
        }

        List<Process> processes = new List<Process>();
    }
}
