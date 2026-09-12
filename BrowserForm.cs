using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace CefSharp.MinimalExample.WinForms
{
    public class BrowserForm : Form
    {
        private TabControl tabControl;
        private Panel topPanel;
        private TextBox addressBar;
        private Button backButton;
        private Button goButton;
        private bool isDragging = false;
        private Point dragStart;
        private Rectangle closeButtonRect = Rectangle.Empty;
        private int hoveredCloseIndex = -1;

        [DllImport("Gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        public BrowserForm() : this(string.Empty) { }

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

            // --- Верхняя панель ---
            topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 45;
            topPanel.BackColor = Color.Black;
            topPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { isDragging = true; dragStart = e.Location; } };
            topPanel.MouseMove += (s, e) => { if (isDragging) { Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y); } };
            topPanel.MouseUp += (s, e) => { isDragging = false; };

            var closeBtn = new Button { Text = "×", Size = new Size(45, 45), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 14) };
            closeBtn.FlatAppearance.BorderColor = Color.Black;
            closeBtn.Click += (s, e) => Close();

            var maxBtn = new Button { Text = "□", Size = new Size(45, 45), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 12) };
            maxBtn.FlatAppearance.BorderColor = Color.Black;
            maxBtn.Click += (s, e) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;

            var minBtn = new Button { Text = "—", Size = new Size(45, 45), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 12) };
            minBtn.FlatAppearance.BorderColor = Color.Black;
            minBtn.Click += (s, e) => WindowState = FormWindowState.Minimized;

            backButton = new Button { Text = "←", Size = new Size(45, 45), Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 14) };
            backButton.FlatAppearance.BorderColor = Color.Black;
            backButton.Click += (s, e) => { GetCurrentBrowser()?.Back(); };

            goButton = new Button { Text = "→", Size = new Size(45, 45), Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 14) };
            goButton.FlatAppearance.BorderColor = Color.Black;
            goButton.Click += (s, e) => Navigate();

            var newTabBtn = new Button { Text = "+", Size = new Size(45, 45), Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat, BackColor = Color.Black, ForeColor = Color.White, Font = new Font("Segoe UI", 14) };
            newTabBtn.FlatAppearance.BorderColor = Color.Black;
            newTabBtn.Click += (s, e) => AddNewTab("https://www.google.com");

            addressBar = new TextBox();
            addressBar.Dock = DockStyle.Fill;
            addressBar.BackColor = Color.FromArgb(20, 20, 20);
            addressBar.ForeColor = Color.White;
            addressBar.BorderStyle = BorderStyle.None;
            addressBar.Font = new Font("Segoe UI", 13);
            addressBar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { Navigate(); e.SuppressKeyPress = true; } };

            var addressPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5, 8, 5, 8), BackColor = Color.Black };
            addressPanel.Controls.Add(addressBar);

            topPanel.Controls.Add(addressPanel);
            topPanel.Controls.Add(goButton);
            topPanel.Controls.Add(backButton);
            topPanel.Controls.Add(newTabBtn);
            topPanel.Controls.Add(closeBtn);
            topPanel.Controls.Add(maxBtn);
            topPanel.Controls.Add(minBtn);

            // --- Вкладки ---
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Appearance = TabAppearance.Normal;
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.ItemSize = new Size(180, 34);
            tabControl.Padding = new Point(14, 6);
            tabControl.SizeMode = TabSizeMode.Fixed;
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.MouseDown += TabControl_MouseDown;
            tabControl.MouseMove += TabControl_MouseMove;
            tabControl.MouseLeave += (s, e) => { hoveredCloseIndex = -1; tabControl.Invalidate(); };
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            Controls.Add(tabControl);
            Controls.Add(topPanel);

            SetWindowRegion();
            AddNewTab(string.IsNullOrEmpty(initialUrl) ? "https://www.google.com" : initialUrl);
        }

        private void AddNewTab(string url)
        {
            var page = new TabPage("Новая вкладка");
            page.BackColor = Color.Black;

            var browser = new ChromiumWebBrowser(url);
            browser.Dock = DockStyle.Fill;
            browser.TitleChanged += (s, e) =>
            {
                if (tabControl.SelectedTab == page)
                    UpdateAddressBar(browser.Address);
                this.Invoke(new Action(() =>
                {
                    int idx = tabControl.TabPages.IndexOf(page);
                    if (idx >= 0) tabControl.TabPages[idx].Text = string.IsNullOrEmpty(e.Title) ? "Новая вкладка" : e.Title;
                    tabControl.Invalidate();
                }));
            };
            browser.AddressChanged += (s, e) =>
            {
                if (tabControl.SelectedTab == page)
                    UpdateAddressBar(e.Address);
            };

            page.Controls.Add(browser);
            tabControl.TabPages.Add(page);
            tabControl.SelectedTab = page;
        }

        private ChromiumWebBrowser GetCurrentBrowser()
        {
            if (tabControl.SelectedTab == null) return null;
            foreach (Control c in tabControl.SelectedTab.Controls)
                if (c is ChromiumWebBrowser browser) return browser;
            return null;
        }

        private void UpdateAddressBar(string url)
        {
            if (addressBar.InvokeRequired)
                addressBar.Invoke(new Action(() => UpdateAddressBar(url)));
            else
                addressBar.Text = url;
        }

        private void Navigate()
        {
            string url = addressBar.Text.Trim();
            if (string.IsNullOrEmpty(url)) return;
            if (!url.StartsWith("http://") && !url.StartsWith("https://")) url = "https://" + url;
            GetCurrentBrowser()?.Load(url);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var browser = GetCurrentBrowser();
            if (browser != null) UpdateAddressBar(browser.Address);
        }

        // --- Рисование вкладок ---
        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tab = tabControl.TabPages[e.Index];
            bool selected = (e.Index == tabControl.SelectedIndex);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Фон окна в области вкладок
            using (var bg = new SolidBrush(Color.Black))
                g.FillRectangle(bg, e.Bounds);

            // Скруглённая вкладка
            var tabRect = new Rectangle(e.Bounds.X + 2, e.Bounds.Y + 4, e.Bounds.Width - 6, e.Bounds.Height - 4);
            int radius = 12;
            using (var path = RoundedRect(tabRect, radius))
            {
                using (var brush = new SolidBrush(selected ? Color.FromArgb(40, 40, 40) : Color.FromArgb(15, 15, 15)))
                    g.FillPath(brush, path);
            }

            // Текст вкладки
            var textRect = new Rectangle(tabRect.X + 10, tabRect.Y, tabRect.Width - 36, tabRect.Height);
            using (var text = new SolidBrush(selected ? Color.White : Color.Gray))
            {
                var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(tab.Text, e.Font, text, textRect, format);
            }

            // Крестик закрытия
            var closeRect = new Rectangle(tabRect.Right - 24, tabRect.Y + (tabRect.Height - 16) / 2, 16, 16);
            closeButtonRect = closeRect;

            bool hover = (hoveredCloseIndex == e.Index);
            using (var closeBg = new SolidBrush(hover ? Color.FromArgb(80, 80, 80) : Color.Transparent))
                g.FillEllipse(closeBg, closeRect);

            using (var pen = new Pen(selected ? Color.White : Color.Gray, 1.5f))
            {
                g.DrawLine(pen, closeRect.X + 5, closeRect.Y + 5, closeRect.Right - 5, closeRect.Bottom - 5);
                g.DrawLine(pen, closeRect.Right - 5, closeRect.Y + 5, closeRect.X + 5, closeRect.Bottom - 5);
            }
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // --- Обработка клика по крестику ---
        private void TabControl_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabCount; i++)
            {
                var rect = tabControl.GetTabRect(i);
                var closeRect = new Rectangle(rect.Right - 26, rect.Y + (rect.Height - 16) / 2 + 4, 16, 16);
                if (closeRect.Contains(e.Location))
                {
                    CloseTab(i);
                    return;
                }
            }
        }

        private void TabControl_MouseMove(object sender, MouseEventArgs e)
        {
            int newHover = -1;
            for (int i = 0; i < tabControl.TabCount; i++)
            {
                var rect = tabControl.GetTabRect(i);
                var closeRect = new Rectangle(rect.Right - 26, rect.Y + (rect.Height - 16) / 2 + 4, 16, 16);
                if (closeRect.Contains(e.Location)) { newHover = i; break; }
            }
            if (newHover != hoveredCloseIndex)
            {
                hoveredCloseIndex = newHover;
                tabControl.Invalidate();
            }
        }

        private void CloseTab(int index)
        {
            if (tabControl.TabPages.Count <= 1) return;

            var page = tabControl.TabPages[index];
            foreach (Control c in page.Controls)
            {
                if (c is ChromiumWebBrowser browser)
                {
                    browser.Dispose();
                    break;
                }
            }
            tabControl.TabPages.Remove(page);
            page.Dispose();
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
