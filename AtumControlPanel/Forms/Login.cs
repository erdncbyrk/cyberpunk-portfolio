using AtumControlPanel.Controls;
using AtumControlPanel.Models;
using AtumControlPanel.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AtumControlPanel.Forms
{
    public partial class Login : Form
    {
        private ModernTextBox _txtServer = null!;
        private ModernTextBox _txtPort = null!;
        private ModernTextBox _txtDbName = null!;
        private ModernTextBox _txtGameDbName = null!;
        private ModernTextBox _txtDbUser = null!;
        private ModernTextBox _txtDbPassword = null!;
        private ModernTextBox _txtUsername = null!;
        private ModernTextBox _txtPassword = null!;
        private Button _btnLogin = null!;
        private Label _lblStatus = null!;
        private Panel _mainPanel = null!;

        public AppConfig Config { get; private set; } = new();
        public AccountInfo? LoggedInAccount { get; private set; }
        public Login()
        {
            InitializeComponent();

            ApplyCustomStyles();
        }

        private void ApplyCustomStyles()
        {
            // ─── Draggable title bar ─────────────────
            var dragPanel = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent };
            var closeBtn = new Label
            {
                Text = "\u2715",
                Font = new Font("Segoe UI", 11f),
                ForeColor = Theme.TextMuted,
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Right,
                Cursor = Cursors.Hand
            };

            closeBtn.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            closeBtn.MouseEnter += (s, e) => closeBtn.ForeColor = Theme.Danger;
            closeBtn.MouseLeave += (s, e) => closeBtn.ForeColor = Theme.TextMuted;
            dragPanel.Controls.Add(closeBtn);

            Point dragStart = Point.Empty;
            dragPanel.MouseDown += (s, e) => dragStart = e.Location;
            dragPanel.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y);
            };

            // ─── Main content ────────────────────────
            _mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(44, 10, 44, 24)
            };

            // Logo
            var titleLabel = new Label
            {
                Text = "\u2726  ATUM CONTROL PANEL",
                Font = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = false,
                Size = new Size(420, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top
            };

            var subtitleLabel = new Label
            {
                Text = "Administrator Access Only",
                Font = Theme.SmallFont,
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                Size = new Size(420, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top
            };

            // ─── DB Connection section ───────────────
            var dbSectionLabel = CreateSectionLabel("DATABASE CONNECTION");

            _txtServer = new ModernTextBox("DB Server IP", "127.0.0.1") { Dock = DockStyle.Top, Enabled = false };
            _txtPort = new ModernTextBox("Port", "1433") { Dock = DockStyle.Top, Enabled = false };
            _txtDbName = new ModernTextBox("Account Database", "atum2_db_account") { Dock = DockStyle.Top, Enabled = false };
            _txtGameDbName = new ModernTextBox("Game Database", "atum2_db_1") { Dock = DockStyle.Top, Enabled = false };
            _txtDbUser = new ModernTextBox("DB Username", "atum") { Dock = DockStyle.Top, Enabled = false };
            _txtDbPassword = new ModernTextBox("DB Password", "callweb") { Dock = DockStyle.Top, UseSystemPasswordChar = true, Enabled = false };


            // Separator
            var separator = new Panel { Height = 1, Dock = DockStyle.Top, BackColor = Theme.Border, Margin = new Padding(0, 8, 0, 8) };

            // ─── Admin Login section ─────────────────
            var loginSectionLabel = CreateSectionLabel("ADMIN LOGIN");

            _txtUsername = new ModernTextBox("Game Account Name", "") { Dock = DockStyle.Top };
            _txtPassword = new ModernTextBox("Game Password", "") { Dock = DockStyle.Top, UseSystemPasswordChar = true };

            // Login button - gradient style
            _btnLogin = Theme.CreateButton("\u25B6  LOGIN", Theme.Primary, 412, 46);
            _btnLogin.Dock = DockStyle.Top;
            _btnLogin.Margin = new Padding(0, 12, 0, 0);
            _btnLogin.Click += btnLogin_Click;

            _lblStatus = new Label
            {
                Text = "",
                Font = Theme.SmallFont,
                ForeColor = Theme.Danger,
                AutoSize = false,
                Height = 28,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Add in reverse order (Dock = Top stacks from top)
            _mainPanel.Controls.Add(_lblStatus);
            _mainPanel.Controls.Add(_btnLogin);
            _mainPanel.Controls.Add(new Panel { Height = 4, Dock = DockStyle.Top });
            _mainPanel.Controls.Add(_txtPassword);
            _mainPanel.Controls.Add(CreateFieldLabel("Password"));
            _mainPanel.Controls.Add(_txtUsername);
            _mainPanel.Controls.Add(CreateFieldLabel("Account Name"));
            _mainPanel.Controls.Add(loginSectionLabel);
            _mainPanel.Controls.Add(new Panel { Height = 10, Dock = DockStyle.Top });
            _mainPanel.Controls.Add(separator);
            _mainPanel.Controls.Add(new Panel { Height = 4, Dock = DockStyle.Top });
            _mainPanel.Controls.Add(_txtDbPassword);
            _mainPanel.Controls.Add(CreateFieldLabel("DB Password"));
            _mainPanel.Controls.Add(_txtDbUser);
            _mainPanel.Controls.Add(CreateFieldLabel("DB User"));
            _mainPanel.Controls.Add(_txtGameDbName);
            _mainPanel.Controls.Add(CreateFieldLabel("Game DB"));
            _mainPanel.Controls.Add(_txtDbName);
            _mainPanel.Controls.Add(CreateFieldLabel("Account DB"));
            _mainPanel.Controls.Add(_txtPort);
            _mainPanel.Controls.Add(CreateFieldLabel("Port"));
            _mainPanel.Controls.Add(_txtServer);
            _mainPanel.Controls.Add(CreateFieldLabel("Server IP"));
            _mainPanel.Controls.Add(dbSectionLabel);
            _mainPanel.Controls.Add(new Panel { Height = 8, Dock = DockStyle.Top });
            _mainPanel.Controls.Add(subtitleLabel);
            _mainPanel.Controls.Add(titleLabel);

            Controls.Add(_mainPanel);
            Controls.Add(dragPanel);

            // ─── Form border with rounded corners ────
            Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Subtle glow border
                using var borderPen = new Pen(Color.FromArgb(40, Theme.Primary), 1f);
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using var path = Theme.RoundedRect(rect, 10);
                g.DrawPath(borderPen, path);

                // Top accent line
                using var accentBrush = Theme.CreateGradient(
                    new Rectangle(0, 0, Width, 2), Theme.Primary, Theme.Accent, 0f);
                g.FillRectangle(accentBrush, new Rectangle(40, 0, Width - 80, 2));
            };
        }

        private Label CreateSectionLabel(string text) => new()
        {
            Text = text,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Theme.Accent,
            Height = 32,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(2, 0, 0, 0)
        };

        private Label CreateFieldLabel(string text) => new()
        {
            Text = text,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Height = 20,
            Dock = DockStyle.Top,
            Padding = new Padding(2, 3, 0, 0)
        };

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            _btnLogin.Enabled = false;
            _lblStatus.ForeColor = Theme.Accent;
            _lblStatus.Text = "Connecting...";

            Config = new AppConfig
            {
                AccountDbServer = _txtServer.Text,
                AccountDbPort = int.TryParse(_txtPort.Text, out var p) ? p : 1433,
                AccountDbName = _txtDbName.Text,
                AccountDbUser = _txtDbUser.Text,
                AccountDbPassword = _txtDbPassword.Text,
                GameDbServer = _txtServer.Text,
                GameDbPort = int.TryParse(_txtPort.Text, out var p2) ? p2 : 1433,
                GameDbName = _txtGameDbName.Text,
                GameDbUser = _txtDbUser.Text,
                GameDbPassword = _txtDbPassword.Text,
                PreServerIP = _txtServer.Text,
                PreServerPort = 40100
            };

            var db = new DatabaseService(Config);
            if (!await db.TestConnectionAsync())
            {
                _lblStatus.ForeColor = Theme.Danger;
                _lblStatus.Text = $"Database connection failed! {db.LastError}";
                _btnLogin.Enabled = true;
                return;
            }

            _lblStatus.Text = "Checking Game DB...";
            try
            {
                using var testConn = new SqlConnection(Config.GetGameConnectionString());
                await testConn.OpenAsync();
            }
            catch
            {
                _lblStatus.Text = "Game DB not found, scanning...";
                var dbs = await db.GetDatabaseListAsync();
                var gameDb = dbs.FirstOrDefault(d => d.Contains("atum") && !d.Contains("account") && !d.Contains("WorldRanking") && !d.Contains("Log"));
                if (gameDb != null)
                {
                    Config.GameDbName = gameDb;
                    _txtGameDbName.Text = gameDb;
                    _lblStatus.Text = $"Found Game DB: {gameDb}";
                }
                else
                {
                    _lblStatus.ForeColor = Theme.Warning;
                    _lblStatus.Text = $"Game DB not found. Available: {string.Join(", ", dbs.Where(d => d.Contains("atum")))}";
                    _btnLogin.Enabled = true;
                    return;
                }
            }

            var account = await db.AuthenticateAdminAsync(_txtUsername.Text, _txtPassword.Text);
            if (account == null)
            {
                _lblStatus.ForeColor = Theme.Warning;
                _lblStatus.Text = db.LastError;
                _btnLogin.Enabled = true;
                return;
            }

            LoggedInAccount = account;
            LoggedInAccount.Password = _txtPassword.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

    }
}
