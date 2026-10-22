using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zelda_3_Launcher
{
    public partial class progressForm : Form
    {
        public progressForm()
        {
            InitializeComponent();
            ApplyTheme();
        }

        // Riquadro scuro con il bordo, come il resto del launcher
        private void ApplyTheme()
        {
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            updateLabel.Font = Theme.Body;
            updateLabel.ForeColor = Theme.Text;
            Paint += (s, e) =>
            {
                using var pen = new Pen(Theme.CardBorder);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };
        }

        private void InitializeComponent()
        {
            this.progBar = new SlimProgress();
            this.updateLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progBar
            // 
            this.progBar.Location = new System.Drawing.Point(16, 40);
            this.progBar.Maximum = 999999;
            this.progBar.Name = "progBar";
            this.progBar.Size = new System.Drawing.Size(500, 8);
            this.progBar.TabIndex = 0;
            // 
            // updateLabel
            // 
            this.updateLabel.Location = new System.Drawing.Point(16, 12);
            this.updateLabel.Name = "updateLabel";
            this.updateLabel.Size = new System.Drawing.Size(500, 20);
            this.updateLabel.TabIndex = 1;
            this.updateLabel.Text = "Update Status";
            this.updateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressForm
            // 
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(1191, 806);
            this.Controls.Add(this.updateLabel);
            this.Controls.Add(this.progBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "progressForm";
            this.Padding = new System.Windows.Forms.Padding(16, 6, 16, 14);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);

        }

    }
}
