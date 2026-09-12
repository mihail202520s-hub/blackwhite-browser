using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace CefSharp.MinimalExample.WinForms
{
    public class BrowserForm : Form
    {
        private ChromiumWebBrowser browser;
        private TextBox addressBar;
        private Panel topPanel;
        private bool isDragging = false;
        private Point dragStart;

        [DllImport("Gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

                public BrowserForm() : this(string.Empty)
        {
        }

        public BrowserForm(string initialUrl)
        {
            Text = "BlackWhite";
            BackColor = Color.Black;
            ForeColor = Color.White;
            Size = new Size(1200, 800);
            MinimumSize = new Size(600, 400);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            // Панель сверху
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 45;
            topPanel.BackColor = Color.Black;
            topPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { isDragging = true; dragStart = e.Location; } };
            topPanel.MouseMove += (s, e) => { if (isDragging) { Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y); } };
            topPanel.MouseUp += (s, e) => { isDragging = false; };

            // Кнопки управления
            var closeBtn = new Button { Text = "×", Size = new Size(45, 45), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 14) };
            closeBtn.FlatAppearance.BorderColor = Color.Black;
            closeBtn.Click += (s, e) => Close();

            var maxBtn = new Button { Text = "□", Size = new Size(45, 45), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 12) };
            maxBtn.FlatAppearance.BorderColor = Color.Black;
            maxBtn.Click += (s, e) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;

            var minBtn = new Button { Text = "—", Size = new Size(45, 45), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 12) };
            minBtn.FlatAppearance.BorderColor = Color.Black;
            minBtn.Click += (s, e) => WindowState = FormWindowState.Minimized;

            // Адресная строка
            addressBar = new TextBox();
            addressBar.Dock = DockStyle.Fill;
            addressBar.BackColor = Color.FromArgb(20, 20, 20);
            addressBar.ForeColor = Color.White;
            addressBar.BorderStyle = BorderStyle.None;
            addressBar.Font = new Font("Segoe UI", 13);
            addressBar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Navigate(); e.SuppressKeyPress = true; } };

            // Кнопка перехода
            var goBtn = new Button { Text = "→", Size = new Size(50, 45), Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 14) };
            goBtn.FlatAppearance.BorderColor = Color.Black;
            goBtn.Click += (s, e) => Navigate();

            // Панель для адресной строки с отступами
            var addressPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5, 8, 5, 8), BackColor = Color.Black };
            addressPanel.Controls.Add(addressBar);

            topPanel.Controls.Add(addressPanel);
            topPanel.Controls.Add(goBtn);
            topPanel.Controls.Add(closeBtn);
            topPanel.Controls.Add(maxBtn);
            topPanel.Controls.Add(minBtn);

            // Браузер
            browser = new ChromiumWebBrowser(string.IsNullOrEmpty(initialUrl) ? "https://www.google.com" : initialUrl);
            browser.Dock = DockStyle.Fill;

            Controls.Add(browser);
            Controls.Add(topPanel);

            SetWindowRegion();
        }

        private void Navigate()
        {
            string url = addressBar.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;
            if (!url.StartsWith("http://") && !url.StartsWith("https://")) url = "https://" + url;
            browser.Load(url);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            SetWindowRegion();
        }

        private void SetWindowRegion()
        {
            IntPtr region = CreateRoundRectRgn(0, 0, Width, Height, 20, 20);
            SetWindowRgn(Handle, region, true);
        }
    }
}

