using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Zelda_3_Launcher
{
    // Aspetto moderno del launcher: tema scuro, interruttori al posto delle
    // caselle, pulsanti e riquadri arrotondati. I controlli disegnati qui
    // restano CheckBox, Button... cosi' il resto del codice li usa come prima.
    internal static class Theme
    {
        public static readonly Color Back = Color.FromArgb(0x16, 0x1d, 0x2b);      // finestre
        public static readonly Color Side = Color.FromArgb(0x11, 0x18, 0x27);      // barra laterale
        public static readonly Color CardBack = Color.FromArgb(0x1b, 0x23, 0x33);      // riquadri
        public static readonly Color CardBorder = Color.FromArgb(0x26, 0x31, 0x48);
        public static readonly Color Deep = Color.FromArgb(0x10, 0x16, 0x1f);      // stato, descrizione
        public static readonly Color Input = Color.FromArgb(0x26, 0x31, 0x48);     // campi ed elenchi
        public static readonly Color Hover = Color.FromArgb(0x24, 0x30, 0x49);
        public static readonly Color ButtonBack = Color.FromArgb(0x22, 0x2c, 0x3e);
        public static readonly Color ButtonBorder = Color.FromArgb(0x2f, 0x3a, 0x50);
        public static readonly Color Text = Color.FromArgb(0xe8, 0xed, 0xf5);
        public static readonly Color Muted = Color.FromArgb(0x9a, 0xa6, 0xba);
        public static readonly Color Faint = Color.FromArgb(0x66, 0x72, 0x8a);
        public static readonly Color Accent = Color.FromArgb(0xf2, 0xbf, 0x1c);
        public static readonly Color AccentLight = Color.FromArgb(0xff, 0xd4, 0x4a);
        public static readonly Color AccentText = Color.FromArgb(0x2b, 0x1e, 0x00);
        public static readonly Color Off = Color.FromArgb(0x33, 0x40, 0x5a);
        public static readonly Color Warn = Color.FromArgb(0xf0, 0x8c, 0x3c);
        public static readonly Color Danger = Color.FromArgb(0xe5, 0x6b, 0x5d);
        public static readonly Color Gold = Color.FromArgb(0xf0, 0xc0, 0x40);

        public static readonly Font Body = new("Segoe UI", 9.75F);
        public static readonly Font Bold = new("Segoe UI Semibold", 9.75F);
        public static readonly Font Small = new("Segoe UI", 8.25F);
        public static readonly Font Caption = new("Segoe UI Semibold", 7.5F);
        public static readonly Font Title = new("Segoe UI", 15F, FontStyle.Bold);
        public static readonly Font Big = new("Segoe UI", 12F, FontStyle.Bold);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // Barra del titolo scura (Windows 10 2004 e successivi)
        public static void DarkTitleBar(Form f)
        {
            void Set()
            {
                try
                {
                    int on = 1;
                    if (DwmSetWindowAttribute(f.Handle, 20, ref on, 4) != 0)
                        DwmSetWindowAttribute(f.Handle, 19, ref on, 4);
                }
                catch { }
            }
            if (f.IsHandleCreated) Set();
            else f.HandleCreated += (s, e) => Set();
        }

        public static void Window(Form f)
        {
            f.BackColor = Back;
            f.ForeColor = Text;
            f.Font = Body;
            DarkTitleBar(f);
        }

        public static GraphicsPath Round(RectangleF r, float radius) => Round(r, radius, true, true);

        // Solo gli angoli in alto o solo quelli in basso arrotondati (elenco aperto)
        public static GraphicsPath Round(RectangleF r, float radius, bool top, bool bottom)
        {
            var p = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0)
            {
                p.AddRectangle(r);
                return p;
            }
            if (top)
            {
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            }
            else
            {
                p.AddLine(r.X, r.Y, r.Right, r.Y);
            }
            if (bottom)
            {
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            }
            else
            {
                p.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            }
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Color c, RectangleF r, float radius)
        {
            using var path = Round(r, radius);
            using var b = new SolidBrush(c);
            g.FillPath(b, path);
        }

        public static void DrawRound(Graphics g, Color c, RectangleF r, float radius, float width = 1, DashStyle dash = DashStyle.Solid)
        {
            using var path = Round(r, radius);
            using var pen = new Pen(c, width) { DashStyle = dash };
            g.DrawPath(pen, path);
        }

        public static void Smooth(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        }

        // Colori scuri per i controlli normali (anche quelli della finestra dei tasti)
        public static void Apply(Control root)
        {
            foreach (Control c in root.Controls)
            {
                switch (c)
                {
                    case ToggleSwitch or FlatButton or Card or NavItem or Stepper or SlimProgress or InputBox or DarkComboBox or DescBox:
                        break;
                    case TextBox t:
                        t.BackColor = Input;
                        t.ForeColor = Text;
                        t.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case ComboBox cb:
                        DarkCombo(cb);
                        break;
                    case NumericUpDown n:
                        n.BackColor = Input;
                        n.ForeColor = Text;
                        n.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case LinkLabel l:
                        l.LinkColor = AccentLight;
                        l.ActiveLinkColor = Text;
                        l.VisitedLinkColor = AccentLight;
                        l.BackColor = Color.Transparent;
                        break;
                    case Button b:
                        b.FlatStyle = FlatStyle.Flat;
                        b.BackColor = ButtonBack;
                        b.ForeColor = Text;
                        b.FlatAppearance.BorderColor = ButtonBorder;
                        b.FlatAppearance.MouseOverBackColor = Hover;
                        break;
                    case GroupBox gb:
                        gb.ForeColor = Muted;
                        gb.BackColor = Back;
                        break;
                    case CheckBox or RadioButton or Label:
                        c.ForeColor = Text;
                        c.BackColor = Color.Transparent;
                        break;
                }
                if (c is InputBox) { }
                else if (c is GroupBox || c is Panel && c is not Card)
                    Apply(c);
                else if (c is not ComboBox && c is not NumericUpDown && c.Controls.Count > 0)
                    Apply(c);
                if (c is GroupBox)
                    foreach (Control child in c.Controls)
                        if (child is CheckBox or RadioButton or Label) child.ForeColor = Text;
            }
        }

        // Elenco a discesa scuro: le voci si disegnano qui
        public static void DarkCombo(ComboBox cb)
        {
            cb.FlatStyle = FlatStyle.Flat;
            cb.BackColor = Input;
            cb.ForeColor = Text;
            if (cb.DropDownStyle != ComboBoxStyle.DropDownList || cb.DrawMode != DrawMode.Normal) return;
            cb.DrawMode = DrawMode.OwnerDrawFixed;
            cb.ItemHeight = 20;
            cb.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool sel = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
                using (var b = new SolidBrush(sel ? Accent : Input))
                    e.Graphics.FillRectangle(b, e.Bounds);
                TextRenderer.DrawText(e.Graphics, cb.Items[e.Index]?.ToString(), cb.Font,
                    new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height),
                    cb.Enabled ? (sel ? AccentText : Text) : Faint, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
        }
    }

    // Interruttore: testo a sinistra, levetta a destra; e' una CheckBox
    internal class ToggleSwitch : CheckBox
    {
        private bool hover;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            AutoSize = false;
            Height = 32;
            Font = Theme.Body;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.CardBack);
            Theme.Smooth(g);
            if (hover && Enabled)
                Theme.FillRound(g, Theme.Hover, new RectangleF(0, 1, Width - 1, Height - 2), 7);
            var sw = new RectangleF(Width - 46, (Height - 20) / 2f, 36, 20);
            Theme.FillRound(g, !Enabled ? Theme.Input : Checked ? Theme.Accent : Theme.Off, sw, 10);
            float kx = Checked ? sw.Right - 17 : sw.X + 3;
            using (var knob = new SolidBrush(Enabled ? (Checked ? Color.White : Color.FromArgb(0xc9, 0xd2, 0xe0)) : Theme.Faint))
                g.FillEllipse(knob, kx, sw.Y + 3, 14, 14);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(8, 0, Width - 62, Height),
                Enabled ? Theme.Text : Theme.Faint, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            if (Focused && ShowFocusCues)
                Theme.DrawRound(g, Theme.AccentLight, new RectangleF(0.5f, 1.5f, Width - 2, Height - 4), 7);
        }
    }

    // Pulsante arrotondato: normale, principale (verde) o tratteggiato da spento
    internal class FlatButton : Button
    {
        public enum Kind { Normal, Primary, Danger }
        public Kind Style { get; set; } = Kind.Normal;
        public int Radius { get; set; } = 9;
        private bool hover, down;
        // ultima scelta fatta con la tastiera (Tab o frecce): si mostra il
        // riquadro del pulsante col focus anche se Windows nasconde i segni
        public static bool KeyboardCues;

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = Theme.Bold;
            Height = 36;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { down = true; KeyboardCues = false; Invalidate(); base.OnMouseDown(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnMouseUp(MouseEventArgs e) { down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Back);
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Color fore;
            if (!Enabled)
            {
                Theme.DrawRound(g, Theme.ButtonBorder, r, Radius, 1, DashStyle.Dash);
                fore = Theme.Faint;
            }
            else if (Style == Kind.Primary)
            {
                var top = hover ? Color.FromArgb(0xff, 0xe0, 0x78) : Theme.AccentLight;
                var bottom = down ? Color.FromArgb(0xc4, 0x92, 0x0c) : Color.FromArgb(0xdc, 0xa6, 0x14);
                using var path = Theme.Round(r, Radius);
                using var b = new LinearGradientBrush(new RectangleF(0, 0, Width, Height), top, bottom, 90f);
                g.FillPath(b, path);
                fore = Theme.AccentText;
            }
            else
            {
                Theme.FillRound(g, down ? Theme.Input : hover ? Theme.Hover : Theme.ButtonBack, r, Radius);
                Theme.DrawRound(g, Style == Kind.Danger ? Theme.Danger : Theme.ButtonBorder, r, Radius);
                fore = Style == Kind.Danger ? Theme.Danger : Theme.Text;
            }
            if (Focused && (ShowFocusCues || KeyboardCues) && Enabled)
                Theme.DrawRound(g, Theme.AccentLight, new RectangleF(1.5f, 1.5f, Width - 4, Height - 4), Radius - 1);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(4, 0, Width - 8, Height), fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    // Riquadro arrotondato con il titolo in piccolo, in maiuscolo
    internal class Card : Panel
    {
        public const int Top0 = 34;

        public Card()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.CardBack;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            Padding = new Padding(12, Top0, 12, 10);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Back);
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Theme.FillRound(g, Theme.CardBack, r, 12);
            Theme.DrawRound(g, Theme.CardBorder, r, 12);
            TextRenderer.DrawText(g, Text.ToUpperInvariant(), Theme.Caption, new Point(14, 12), Theme.Muted);
        }

        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }

        // Le righe si impilano sotto il titolo, larghe quanto il riquadro
        protected override void OnLayout(LayoutEventArgs e)
        {
            int y = Top0;
            foreach (Control c in Controls)
            {
                c.SetBounds(12, y, Width - 24, c.Height);
                y += c.Height;
            }
            base.OnLayout(e);
        }

        public int ContentHeight => Top0 + Controls.Cast<Control>().Sum(c => c.Height) + 10;
    }

    // Riga di un riquadro: testo a sinistra, il controllo a destra
    // (oppure, con Stretch, il controllo occupa tutta la riga)
    internal class FieldRow : Panel
    {
        public readonly Label Label = new FlatLabel() { AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, Font = Theme.Body };
        public readonly Control Editor;
        public bool Stretch { get; init; }

        public FieldRow(string text, Control editor)
        {
            Height = 34;
            BackColor = Theme.CardBack;
            Label.Text = text;
            Label.ForeColor = Theme.Text;
            Editor = editor;
            Controls.Add(Label);
            Controls.Add(editor);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            // (anche dal costruttore, prima che ci sia il controllo)
            if (Editor == null)
            {
                base.OnLayout(e);
                return;
            }
            if (Stretch)
            {
                Label.Visible = Label.Text.Length > 0;
                int x = Label.Visible ? 8 + TextRenderer.MeasureText(Label.Text, Label.Font).Width + 10 : 8;
                Label.SetBounds(8, 0, Math.Max(0, x - 18), Height);
                Editor.SetBounds(x, (Height - Editor.Height) / 2, Math.Max(20, Width - x - 2), Editor.Height);
            }
            else
            {
                Editor.Location = new Point(Width - Editor.Width - 2, (Height - Editor.Height) / 2);
                Label.SetBounds(8, 0, Math.Max(0, Editor.Left - 16), Height);
            }
            base.OnLayout(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Label.ForeColor = Enabled ? Theme.Text : Theme.Faint;
            base.OnEnabledChanged(e);
        }
    }

    // Etichetta che da spenta resta piatta (Windows la disegnerebbe in rilievo)
    internal class FlatLabel : Label
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextAlign switch
            {
                ContentAlignment.MiddleCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                ContentAlignment.MiddleRight => TextFormatFlags.Right | TextFormatFlags.VerticalCenter,
                ContentAlignment.MiddleLeft => TextFormatFlags.VerticalCenter,
                _ => TextFormatFlags.Default,
            };
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? ForeColor : Theme.Faint, flags);
        }
    }

    // Elenco a discesa disegnato a mano: l'elenco aperto ha gli angoli in basso
    // arrotondati e continua il bordo della casella, senza lo stacco (e il lampo
    // bianco) dell'elenco di Windows. Si usa come un ComboBox: Items,
    // SelectedIndex, SelectedIndexChanged.
    internal class DarkComboBox : Control
    {
        public readonly List<object> Items = new();
        public event EventHandler? SelectedIndexChanged;
        private int selected = -1;
        private bool hover, open;
        private ToolStripDropDown? popup;
        private DropList? list;
        private DateTime closedAt;
        // elenco aperto sopra la casella (sotto non c'e' posto sullo schermo)
        private bool above;

        public const int ItemHeight = 28, Radius = 7;

        public int SelectedIndex
        {
            get => selected;
            set
            {
                int v = Math.Clamp(value, -1, Items.Count - 1);
                if (v == selected) return;
                selected = v;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public DarkComboBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            Height = 28;
            Font = Theme.Body;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            // il clic che ha chiuso l'elenco non lo deve riaprire
            if (!open && (DateTime.Now - closedAt).TotalMilliseconds > 150) Open();
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData is Keys.Up or Keys.Down || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && e.Alt || e.KeyCode is Keys.F4 or Keys.Space or Keys.Enter) Open();
            else if (e.KeyCode == Keys.Down && selected < Items.Count - 1) SelectedIndex = selected + 1;
            else if (e.KeyCode == Keys.Up && selected > 0) SelectedIndex = selected - 1;
            base.OnKeyDown(e);
        }

        private void Open()
        {
            if (Items.Count == 0) return;
            if (popup == null)
            {
                list = new DropList(this);
                popup = new ToolStripDropDown { Padding = Padding.Empty, Margin = Padding.Empty, DropShadowEnabled = false, AutoSize = false };
                popup.Items.Add(new ToolStripControlHost(list) { Padding = Padding.Empty, Margin = Padding.Empty, AutoSize = false });
                popup.Closed += (s, e) =>
                {
                    open = false;
                    closedAt = DateTime.Now;
                    Invalidate();
                };
            }
            list!.Hot = selected;
            var size = new Size(Width, Items.Count * ItemHeight + 6);
            list.Size = size;
            list.Pressed = false;
            ((ToolStripControlHost)popup.Items[0]).Size = size;
            popup.Size = size;
            // Sotto la casella, oppure sopra se sotto non ci sta: mai sopra la
            // casella stessa (Windows lo sposterebbe lì e il rilascio del clic
            // che lo apre cadrebbe su una voce)
            var screen = Screen.FromControl(this).WorkingArea;
            var at = PointToScreen(Point.Empty);
            above = at.Y + Height - 1 + size.Height > screen.Bottom && at.Y + 1 - size.Height >= screen.Top;
            // angoli arrotondati dal lato lontano dalla casella
            using (var path = above
                ? Theme.Round(new RectangleF(0, 0, size.Width, size.Height + 1), Radius + 1, true, false)
                : Theme.Round(new RectangleF(0, -1, size.Width, size.Height + 1), Radius + 1, false, true))
                popup.Region = new Region(path);
            open = true;
            Invalidate();
            Update();
            popup.Show(this, above ? new Point(0, 1 - size.Height) : new Point(0, Height - 1));
        }

        internal void Pick(int index)
        {
            popup?.Close();
            SelectedIndex = index;
            Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.CardBack);
            Theme.Smooth(g);
            var r = !open ? new RectangleF(0.5f, 0.5f, Width - 2, Height - 2)
                : above ? new RectangleF(0.5f, -1, Width - 2, Height - 0.5f)
                : new RectangleF(0.5f, 0.5f, Width - 2, Height);
            using (var path = Theme.Round(r, Radius, !open || !above, !open || above))
            {
                // aperta: stesso fondo dell'elenco, un solo ovale lungo
                using (var b = new SolidBrush(!Enabled ? Theme.Deep : open ? Theme.Input : hover ? Theme.Hover : Theme.Input))
                    g.FillPath(b, path);
                if (open || Focused)
                    using (var pen = new Pen(Theme.Accent))
                        g.DrawPath(pen, path);
            }
            if (open)
            {
                // con l'elenco aperto il bordo prosegue verso l'elenco: niente riga di chiusura
                using (var b = new SolidBrush(Theme.Input))
                    g.FillRectangle(b, 1.5f, above ? -0.5f : Height - 1.5f, Width - 3.5f, 2);
                // ovale piccolo sulla casella: l'elenco parte da qui
                var pill = new RectangleF(3.5f, 3.5f, Width - 8, Height - 7);
                Theme.FillRound(g, Theme.Hover, pill, Radius - 1);
                Theme.DrawRound(g, Theme.AccentLight, pill, Radius - 1);
            }
            TextRenderer.DrawText(g, selected >= 0 ? Items[selected]?.ToString() : "", Font,
                new Rectangle(10, 0, Width - 34, Height), Enabled ? Theme.Text : Theme.Faint,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            float cx = Width - 15, cy = Height / 2f;
            using var arrow = new Pen(Enabled ? Theme.Muted : Theme.Faint, 1.6f);
            var pts = open
                ? new[] { new PointF(cx - 4, cy + 2), new PointF(cx, cy - 2), new PointF(cx + 4, cy + 2) }
                : new[] { new PointF(cx - 4, cy - 2), new PointF(cx, cy + 2), new PointF(cx + 4, cy - 2) };
            g.DrawLines(arrow, pts);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) popup?.Dispose();
            base.Dispose(disposing);
        }

        // Le voci dell'elenco aperto
        private class DropList : Control
        {
            private readonly DarkComboBox owner;
            public int Hot;
            // il clic e' cominciato sull'elenco (non e' il rilascio di quello che l'ha aperto)
            public bool Pressed;

            public DropList(DarkComboBox owner)
            {
                this.owner = owner;
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
                Font = owner.Font;
                Cursor = Cursors.Hand;
            }

            private int IndexAt(Point p) =>
                p.X < 0 || p.X >= Width || p.Y < 2 || (p.Y - 2) / ItemHeight >= owner.Items.Count ? -1 : (p.Y - 2) / ItemHeight;

            protected override void OnMouseDown(MouseEventArgs e)
            {
                Pressed = true;
                base.OnMouseDown(e);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                int i = IndexAt(e.Location);
                if (i != Hot) { Hot = i; Invalidate(); }
                base.OnMouseMove(e);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (!Pressed) return;
                Pressed = false;
                int i = IndexAt(e.Location);
                if (i >= 0) owner.Pick(i);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.Clear(Theme.CardBack);
                Theme.Smooth(g);
                bool up = owner.above;
                var r = up ? new RectangleF(0.5f, 0.5f, Width - 2, Height + 0.5f) : new RectangleF(0.5f, -1, Width - 2, Height - 0.5f);
                using (var path = Theme.Round(r, Radius, up, !up))
                {
                    using (var b = new SolidBrush(Theme.Input))
                        g.FillPath(b, path);
                    using (var pen = new Pen(Theme.Accent))
                        g.DrawPath(pen, path);
                }
                for (int i = 0; i < owner.Items.Count; i++)
                {
                    var item = new Rectangle(4, 2 + i * ItemHeight, Width - 9, ItemHeight);
                    if (i == Hot)
                        Theme.FillRound(g, Theme.Hover, new RectangleF(item.X, item.Y + 1, item.Width, item.Height - 2), 6);
                    bool sel = i == owner.SelectedIndex;
                    if (sel)
                        using (var dot = new SolidBrush(Theme.AccentLight))
                            g.FillEllipse(dot, item.X + 6, item.Y + item.Height / 2f - 3, 6, 6);
                    TextRenderer.DrawText(g, owner.Items[i]?.ToString(), sel ? Theme.Bold : Font,
                        new Rectangle(item.X + 18, item.Y, item.Width - 22, item.Height), sel ? Color.White : Theme.Text,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                }
            }
        }
    }

    // Campo di testo senza il bordo chiaro di Windows, su un riquadro arrotondato
    internal class InputBox : Panel
    {
        public readonly TextBox Box = new() { BorderStyle = BorderStyle.None, BackColor = Theme.Input, ForeColor = Theme.Text, Font = Theme.Body };

        public InputBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 28;
            Controls.Add(Box);
            Box.GotFocus += (s, e) => Invalidate();
            Box.LostFocus += (s, e) => Invalidate();
            Box.Click += (s, e) => OnClick(e);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            Box.SetBounds(8, (Height - Box.Height) / 2, Math.Max(10, Width - 16), Box.Height);
            base.OnLayout(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Box.BackColor = Enabled ? Theme.Input : Theme.Deep;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.CardBack);
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Theme.FillRound(g, Enabled ? Theme.Input : Theme.Deep, r, 7);
            if (Box.Focused) Theme.DrawRound(g, Theme.Accent, r, 7);
        }
    }

    // Riquadro della descrizione in fondo alle impostazioni
    internal class DescBox : Control
    {
        private string title = "", body = "";

        public DescBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 76;
        }

        // Testi in inglese: si traducono quando si disegnano
        public void Show(string title, string body)
        {
            if (this.title == title && this.body == body) return;
            this.title = title;
            this.body = body;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Back);
            Theme.Smooth(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 2, Height - 2);
            Theme.FillRound(g, Theme.Deep, r, 10);
            Theme.FillRound(g, Theme.Accent, new RectangleF(0, 0, 4, Height - 1), 2);
            int x = 18;
            string t = Lang.T(title);
            if (t.Length > 0)
            {
                t += ":";
                TextRenderer.DrawText(g, t, Theme.Bold, new Point(x, 10), Color.White);
                x += TextRenderer.MeasureText(g, t, Theme.Bold, Size.Empty, TextFormatFlags.NoPadding).Width + 10;
            }
            // Il testo va a capo: si toglie quello che non entra in tre righe
            string b = Lang.T(body).Replace("\n\n", " ").Replace('\n', ' ');
            TextRenderer.DrawText(g, b, Theme.Body, new Rectangle(x, 10, Width - x - 14, Height - 16), Theme.Muted,
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.TextBoxControl);
        }
    }

    // Voce della barra laterale delle impostazioni
    internal class NavItem : Control
    {
        private bool selected, hover;
        public bool Selected { get => selected; set { selected = value; Invalidate(); } }

        public NavItem()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            Height = 38;
            Font = Theme.Bold;
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter or Keys.Space) OnClick(EventArgs.Empty);
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Side);
            Theme.Smooth(g);
            if (selected || hover)
                Theme.FillRound(g, selected ? Color.FromArgb(0x1f, 0x2a, 0x3d) : Color.FromArgb(0x18, 0x21, 0x33), new RectangleF(0, 0, Width - 1, Height - 1), 8);
            using (var dot = new SolidBrush(selected ? Theme.AccentLight : Color.FromArgb(0x3a, 0x46, 0x5c)))
                g.FillEllipse(dot, 13, Height / 2f - 3.5f, 7, 7);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(30, 0, Width - 34, Height), selected ? Color.White : Theme.Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            if (Focused && ShowFocusCues)
                Theme.DrawRound(g, Theme.AccentLight, new RectangleF(0.5f, 0.5f, Width - 2, Height - 2), 8);
        }

        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
    }

    // Numero con - e + (al posto del NumericUpDown, che non si scurisce)
    internal class Stepper : Control
    {
        private int value, min, max = 100, step = 1;
        private readonly FlatButton minus = new() { Text = "−", Width = 30, Height = 28, Radius = 7 };
        private readonly FlatButton plus = new() { Text = "+", Width = 30, Height = 28, Radius = 7 };
        public event EventHandler? ValueChanged;

        public int Minimum { get => min; set { min = value; Value = this.value; } }
        public int Maximum { get => max; set { max = value; Value = this.value; } }
        public int Step { get => step; set => step = Math.Max(1, value); }
        public int Value
        {
            get => value;
            set
            {
                int v = Math.Clamp(value, min, max);
                if (v == this.value) { Invalidate(); return; }
                this.value = v;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Stepper()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(118, 28);
            Font = Theme.Bold;
            minus.Click += (s, e) => Value = value - step;
            plus.Click += (s, e) => Value = value + step;
            Controls.Add(minus);
            Controls.Add(plus);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            minus.Location = new Point(0, 0);
            plus.Location = new Point(Width - plus.Width, 0);
            base.OnLayout(e);
        }

        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? Theme.CardBack);
            TextRenderer.DrawText(e.Graphics, value.ToString(), Font, new Rectangle(30, 0, Width - 60, Height),
                Enabled ? Theme.Text : Theme.Faint, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // Barra di avanzamento sottile e arrotondata
    internal class SlimProgress : Control
    {
        private int value, maximum = 100;
        public int Maximum { get => maximum; set { maximum = Math.Max(1, value); Invalidate(); } }
        public int Value { get => value; set { this.value = Math.Clamp(value, 0, maximum); Invalidate(); } }

        public SlimProgress()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Height = 8;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Back);
            Theme.Smooth(g);
            Theme.FillRound(g, Theme.Input, new RectangleF(0, 0, Width - 1, Height - 1), Height / 2f);
            float w = (Width - 1) * value / (float)maximum;
            if (w > 0)
                Theme.FillRound(g, Theme.Accent, new RectangleF(0, 0, Math.Max(w, Height), Height - 1), Height / 2f);
        }
    }

    // La Triforza disegnata
    internal class TriforceLogo : Control
    {
        public TriforceLogo()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Size = new Size(64, 56);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? Theme.Back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = Width - 2, h = Height - 2;
            PointF P(float x, float y) => new(1 + x * w, 1 + y * h);
            var tris = new[]
            {
                new[] { P(0.5f, 0), P(0.75f, 0.5f), P(0.25f, 0.5f) },
                new[] { P(0.25f, 0.5f), P(0.5f, 1), P(0, 1) },
                new[] { P(0.75f, 0.5f), P(1, 1), P(0.5f, 1) },
            };
            using var fill = new LinearGradientBrush(new RectangleF(0, 0, Width, Height), Color.FromArgb(0xff, 0xd8, 0x60), Color.FromArgb(0xe0, 0xa0, 0x20), 90f);
            using var pen = new Pen(Color.FromArgb(0xa0, 0x68, 0x00), 1.4f);
            foreach (var t in tris)
            {
                g.FillPolygon(fill, t);
                g.DrawPolygon(pen, t);
            }
        }
    }
}
