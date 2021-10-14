using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace NetShield_Protector
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
        }

        RainbowObj rgb = null;
        private void About_Load(object sender, EventArgs e)
        {
            rgb = new RainbowObj(label2, "ForeColor", new Transitions.TransitionType_Linear(100));
            rgb.Start();
            checkBox8.Checked = Properties.Settings.Default.rgb_MainMenu;
        }

        private void About_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.DrawIcon(Properties.Resources.ux_badge, new Rectangle(new Point(200,148),new Size(32, 32)));
            int x = Properties.Resources.chara.Width, y = Properties.Resources.chara.Height;
            e.Graphics.DrawImage(Properties.Resources.chara, new Rectangle(new Point(147, 146), new Size(x*2, y*2)));
        }

        private void About_FormClosing(object sender, FormClosingEventArgs e)
        {
            rgb.Stop();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pictureBox2_Click(object sender, EventArgs e) { Process.Start("https://github.com/AhmedMinegames/NetShield_Protector");  }
        private void pictureBox1_Click(object sender, EventArgs e) { Process.Start("https://github.com/miso-xyz/NetShieldEx"); }

        private void checkBox8_CheckedChanged(object sender, EventArgs e) { Properties.Settings.Default.rgb_MainMenu = checkBox8.Checked; Properties.Settings.Default.Save(); }
    }
}
