using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ExcelDna.Contrib.Library
{
    class SplashScreen : Form
    {
        public static void Display(string splashImageFullPath, bool warnIfNotFound)
        {
            if (!splashImageFullPath.StartsWith("http", true, CultureInfo.InvariantCulture) && 
                !File.Exists(splashImageFullPath))
            {
                if (warnIfNotFound && splashImageFullPath.Length > 0)
                    MessageBox.Show("Splash screen image '" + splashImageFullPath + "' not found.");

                return;
            }

            SplashScreen ss = new SplashScreen(splashImageFullPath);
            ss.Show();
        }

        private Timer timer1;
        private System.ComponentModel.IContainer components;
        private PictureBox pictureBox1;
    
        private SplashScreen(string splashImageFullPath)
        {
            initializeComponent(splashImageFullPath);
            timer1.Enabled = true;
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            if (Opacity==0)
            {
                Close();
                timer1.Enabled = false;
                return;
            }

            Opacity = Opacity - .01d;
        }

        private void initializeComponent(string splashImageFullPath)
        {
            components = new System.ComponentModel.Container();
            pictureBox1 = new PictureBox();
            timer1 = new Timer(components);
            ((System.ComponentModel.ISupportInitialize)(pictureBox1)).BeginInit();
            SuspendLayout();
            
            // 
            // Picture Box
            // 
            pictureBox1.Location = new System.Drawing.Point(0, 0);
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.Load(splashImageFullPath);
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(pictureBox1);
            
            // 
            // Form
            // 
            ClientSize = pictureBox1.Size;
            FormBorderStyle = FormBorderStyle.None;
            AllowTransparency = true;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            this.Click += ((sender, e) => Close());
            
            //
            // Timer
            //
            timer1.Interval = 10;
            timer1.Tick += timer1_Tick;

            ((System.ComponentModel.ISupportInitialize)(pictureBox1)).EndInit();

            ResumeLayout(true);
        }

    }
}
