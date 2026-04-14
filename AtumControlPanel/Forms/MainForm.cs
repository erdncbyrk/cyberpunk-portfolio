using System.Drawing.Drawing2D;
using AtumControlPanel.Controls;
using AtumControlPanel.Models;
using AtumControlPanel.Services;

namespace AtumControlPanel.Forms
{
    public class MainForm : Form
    {
        private readonly AppConfig _config;
        private readonly AccountInfo _admin;
        private readonly DatabaseService _db;

        private Panel _sidebar = null!;
        private Panel _contentPanel = null!;
        private GradientHeader _headerPanel = null!;
        private Label _pageTitle = null!;
        private Label _adminLabel = null!;

        private readonly List<SidebarButton> _menuButtons = new();
        private int _activePageIndex = 0;

        public MainForm(AppConfig config, AccountInfo admin)
        {
            _config = config;
            _admin = admin;
            _db = new DatabaseService(config);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Atum Control Panel";
            Size = new Size(1400, 900);
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background;
            DoubleBuffered = true;
            Icon = SystemIcons.Shield;

            // ─── Sidebar ─────────────────────────────
            _sidebar = new DoubleBufferedPanel { Dock = DockStyle.Left, Width = 256, BackColor = Theme.SidebarBg };

            // Sidebar right border (subtle gradient line)
            var sidebarBorder = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Color.FromArgb(30, Theme.Primary) };
            _sidebar.Controls.Add(sidebarBorder);

            // Logo panel with gradient underline
            var logoPanel = new DoubleBufferedPanel { Dock = DockStyle.Top, Height = 66, BackColor = Theme.SidebarBg };
            var logoLabel = new Label
            {
                Text = "\u2726  ATUM CP",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = false,
                Size = new Size(250, 52),
                TextAlign = ContentAlignment.MiddleCenter
            };

            logoPanel.Controls.Add(logoLabel);
            logoPanel.Paint += LogoPanel_Paint;

            // Menu container
            var menuContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(4, 8, 4, 0),
                BackColor = Theme.SidebarBg
            };

            var menuDef = new (string icon, string text, int page)[]
            {
                ("\u25A3", "Dashboard", 0),
                ("", "COMMUNITY", -1),
                ("\u25CB", "Accounts", 1),
                ("\u2694", "Characters", 2),
                ("\u2691", "Guilds", 3),
                ("\u25D0", "Banned Accounts", 4),
                ("", "MANAGEMENT", -1),
                ("\u266B", "Events", 5),
                ("\u2709", "Auto Notices", 6),
                ("\u2694", "StrategyPoint", 7),
                ("\u2620", "Event Monster", 12),
                ("\u2693", "Influence War", 13),
                ("\u2668", "Cash Shop", 8),
                ("", "SECURITY", -1),
                ("\u26E8", "Anti-Cheat", 14),
                ("", "TOOLS", -1),
                ("\u2B06", "Auto Update", 15),
                ("\u2630", "Logs", 9),
                ("\u25C8", "SQL Console", 10),
                ("\u2699", "Settings", 11),
            };

            foreach (var (icon, text, page) in menuDef)
            {
                if (page == -1)
                {
                    var sectionLabel = new Label
                    {
                        Text = "    " + text,
                        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                        ForeColor = Theme.TextMuted,
                        Size = new Size(240, 30),
                        TextAlign = ContentAlignment.BottomLeft,
                        Padding = new Padding(12, 0, 0, 0)
                    };
                    menuContainer.Controls.Add(sectionLabel);
                    continue;
                }

                var btn = new SidebarButton(icon, text);
                btn.Width = 240;
                btn.Tag = page;  // Store page index for active state matching
                int idx = page;
                btn.Click += (s, e) => SwitchPage(idx);
                _menuButtons.Add(btn);
                menuContainer.Controls.Add(btn);
            }

            // Admin panel at bottom - glass style
            var adminPanel = new DoubleBufferedPanel { Dock = DockStyle.Bottom, Height = 64, BackColor = Theme.SidebarBg };
            adminPanel.Paint += (s, e) =>
            {
                // Top separator line
                using var brush = Theme.CreateGradient(new Rectangle(16, 0, 220, 1), Color.FromArgb(40, Theme.Primary), Color.Transparent, 0f);
                e.Graphics.FillRectangle(brush, 16, 0, 220, 1);
            };
            _adminLabel = new Label
            {
                Text = $"\u25CF  {_admin.AccountName} ({_admin.GetRoleString()})",
                Font = Theme.SmallFontBold,
                ForeColor = Theme.Success,
                AutoSize = false,
                Size = new Size(250, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 8)
            };
            var logoutLabel = new Label
            {
                Text = "Logout",
                Font = Theme.SmallFont,
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                Size = new Size(250, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 38),
                Cursor = Cursors.Hand
            };
            logoutLabel.Click += (s, e) => { DialogResult = DialogResult.Retry; Close(); };
            logoutLabel.MouseEnter += (s, e) => logoutLabel.ForeColor = Theme.Danger;
            logoutLabel.MouseLeave += (s, e) => logoutLabel.ForeColor = Theme.TextMuted;
            adminPanel.Controls.AddRange(new Control[] { _adminLabel, logoutLabel });

            _sidebar.Controls.Add(menuContainer);
            _sidebar.Controls.Add(adminPanel);
            _sidebar.Controls.Add(logoPanel);

            // ─── Header ──────────────────────────────
            _headerPanel = new GradientHeader { Padding = new Padding(28, 0, 28, 0) };
            _pageTitle = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                AutoSize = false,
                BackColor = Color.Transparent,
                Size = new Size(800, 56),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left
            };
            _headerPanel.Controls.Add(_pageTitle);

            // ─── Content ─────────────────────────────
            _contentPanel = new DoubleBufferedPanel { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = new Padding(24, 16, 24, 16) };

            var mainArea = new Panel { Dock = DockStyle.Fill };
            mainArea.Controls.Add(_contentPanel);
            mainArea.Controls.Add(_headerPanel);

            Controls.Add(mainArea);
            Controls.Add(_sidebar);

            SwitchPage(0);
        }

        private void LogoPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            using (var brush = Theme.CreateGradient(
                new Rectangle(20, 60, 210, 2),
                Theme.Primary,
                Theme.Accent,
                0f))
            {
                g.FillRectangle(brush, 20, 58, 210, 1);
            }
        }

        private void SwitchPage(int index)
        {
            _activePageIndex = index;
            for (int i = 0; i < _menuButtons.Count; i++)
                _menuButtons[i].IsActive = ((int)_menuButtons[i].Tag == index);

            _contentPanel.SuspendLayout();
            _contentPanel.Controls.Clear();

            switch (index)
            {
                case 0: _pageTitle.Text = "Dashboard"; LoadDashboard(); break;
                case 1: _pageTitle.Text = "Account Management"; LoadAccountsPage(); break;
                case 2: _pageTitle.Text = "Character Management"; LoadCharactersPage(); break;
                case 3: _pageTitle.Text = "Guild Management"; LoadGuildsPage(); break;
                case 4: _pageTitle.Text = "Banned Accounts"; LoadBannedPage(); break;
                case 5: _pageTitle.Text = "Event Management"; LoadEventsPage(); break;
                case 6: _pageTitle.Text = "Auto Notices"; LoadNoticesPage(); break;
                case 7: _pageTitle.Text = "StrategyPoint Manager"; LoadStrategyPointPage(); break;
                case 8: _pageTitle.Text = "Cash Shop"; LoadCashShopPage(); break;
                case 9: _pageTitle.Text = "Log Viewer"; LoadLogsPage(); break;
                case 10: _pageTitle.Text = "SQL Console"; LoadSqlConsolePage(); break;
                case 11: _pageTitle.Text = "Settings"; LoadSettingsPage(); break;
                case 12: _pageTitle.Text = "Event Monster Management"; LoadEventMonsterPage(); break;
                case 13: _pageTitle.Text = "Influence War (Mothership)"; LoadInfluenceWarPage(); break;
                case 14: _pageTitle.Text = "Anti-Cheat & Security"; LoadAntiCheatPage(); break;
                case 15: _pageTitle.Text = "Auto Update"; LoadAutoUpdatePage(); break;
            }
            _contentPanel.ResumeLayout(true);
        }

        // ═══════════════════════════════════════════════
        // DASHBOARD
        // ═══════════════════════════════════════════════
        private async void LoadDashboard()
        {
            var statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 130,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0)
            };

            var cards = new[]
            {
                new StatCard("Total Accounts", "\u25CB", Theme.Primary),
                new StatCard("Total Characters", "\u2694", Theme.Accent),
                new StatCard("Total Guilds", "\u2691", Theme.Warning),
                new StatCard("Banned Accounts", "\u2718", Theme.Danger),
                new StatCard("Today's Registrations", "\u2606", Theme.Success),
            };
            foreach (var c in cards) { c.Margin = new Padding(0, 0, 14, 0); statsPanel.Controls.Add(c); }

            // Server info card
            var infoCard = CreateCard(120, Theme.Accent);
            var infoText = new Label
            {
                Text = $"Welcome back, {_admin.AccountName}!\nRole: {_admin.GetRoleString()}  |  Account DB: {_config.AccountDbName}  |  Game DB: {_config.GameDbName}\nServer: {_config.AccountDbServer}:{_config.AccountDbPort}",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 15, 20, 15)
            };
            infoCard.Controls.Add(infoText);

            _contentPanel.Controls.Add(infoCard);
            _contentPanel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 14 });
            _contentPanel.Controls.Add(statsPanel);

            var stats = await _db.GetDashboardStatsAsync();
            cards[0].Value = stats.TotalAccounts.ToString("N0");
            cards[1].Value = stats.TotalCharacters.ToString("N0");
            cards[2].Value = stats.TotalGuilds.ToString("N0");
            cards[3].Value = stats.BannedAccounts.ToString("N0");
            cards[4].Value = stats.TodayRegistrations.ToString("N0");

            if (stats.Error != null)
                infoText.Text += $"\n\nNote: {stats.Error}";
        }

        // ═══════════════════════════════════════════════
        // ACCOUNTS
        // ═══════════════════════════════════════════════
        private void LoadAccountsPage()
        {
            var (searchPanel, txtSearch, btnSearch) = CreateSearchBar("Search account name...");
            var statusLabel = CreateStatusLabel();
            var dgv = CreateGrid("AccountName:Account:22", "UID:UID:8", "Role:Role:10", "Status:Status:8",
                "ChatBlock:Chat:8", "Registered:Registered:18", "LastLogin:Last Login:18");

            var ctx = CreateContextMenu(dgv,
                ("Ban Account", async () =>
                {
                    var name = GetSelectedCell(dgv, "AccountName");
                    if (name == null) return;
                    var reason = Prompt("Enter ban reason:", "Ban Account");
                    if (reason == null) return;
                    if (await _db.BlockAccountAsync(name, reason, _admin.AccountName, null))
                        ShowSuccess($"'{name}' banned."); btnSearch.PerformClick();
                }
            ),
                ("Unban Account", async () =>
                {
                    var name = GetSelectedCell(dgv, "AccountName");
                    if (name == null) return;
                    var (uOk, uMsg) = await _db.UnblockAccountAsync(name); if (uOk) ShowSuccess($"'{name}' unbanned."); btnSearch.PerformClick();
                }
            ),
                ("Toggle Chat Block", async () =>
                {
                    var name = GetSelectedCell(dgv, "AccountName");
                    if (name == null) return;
                    var chatBlocked = GetSelectedCell(dgv, "ChatBlock") == "Yes";
                    if (await _db.SetChatBlockAsync(name, !chatBlocked)) ShowSuccess($"Chat block toggled."); btnSearch.PerformClick();
                }
            ),
                ("View Characters", async () =>
                {
                    var name = GetSelectedCell(dgv, "AccountName");
                    if (name == null) return;
                    SwitchPage(2);
                    await Task.CompletedTask;
                }
            )
            );

            async void DoSearch()
            {
                dgv.Rows.Clear();
                statusLabel.Text = "Searching...";
                var (accounts, info) = await _db.SearchAccountsAsync(txtSearch.Text);
                statusLabel.Text = info;
                foreach (var a in accounts)
                {
                    int ri = dgv.Rows.Add(a.AccountName, a.AccountUniqueNumber, a.GetRoleString(),
                        a.IsBlocked ? "BANNED" : "Active", a.ChattingBlocked ? "Yes" : "No",
                        a.RegisteredDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                        a.LastLoginDate?.ToString("yyyy-MM-dd HH:mm") ?? "");
                    dgv.Rows[ri].Cells["Status"].Style.ForeColor = a.IsBlocked ? Theme.Danger : Theme.Success;
                    if (a.IsAdmin || a.IsGM) dgv.Rows[ri].Cells["Role"].Style.ForeColor = Theme.Accent;
                }
            }

            btnSearch.Click += (s, e) => DoSearch();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { DoSearch(); e.SuppressKeyPress = true; } };

            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(statusLabel);
            _contentPanel.Controls.Add(searchPanel);
            DoSearch();
        }

        // ═══════════════════════════════════════════════
        // CHARACTERS
        // ═══════════════════════════════════════════════
        private void LoadCharactersPage()
        {
            var (searchPanel, txtSearch, btnSearch) = CreateSearchBar("Search character or account name...");

            var chkByAccount = new CheckBox
            {
                Text = "By Account",
                ForeColor = Theme.TextSecondary,
                Font = Theme.SmallFont,
                Location = new Point(420, 15),
                AutoSize = true
            };
            searchPanel.Controls.Add(chkByAccount);

            var statusLabel = CreateStatusLabel();
            var dgv = CreateGrid("UID:UID:6", "CharacterName:Character:15", "AccountName:Account:12",
                "Level:Lv:5", "Money:Money:10", "Race:Race:6", "UnitKind:Unit:6",
                "PlayTime:Play Time:10", "PKPoint:PK:6", "Fame:Fame:6");

            // Detail panel (right side)
            var detailPanel = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Theme.CardBg, Padding = new Padding(12) };
            var detailBorder = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Border };
            detailPanel.Controls.Add(detailBorder);
            var detailTitle = new Label { Text = "Character Details", Font = Theme.SubtitleFont, ForeColor = Theme.Accent, Dock = DockStyle.Top, Height = 35 };
            var detailInfo = new Label { Text = "Select a character", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 180, FlowDirection = FlowDirection.TopDown, Padding = new Padding(0, 5, 0, 0) };
            var btnSetLevel = Theme.CreateButton("Set Level", Theme.Primary, 270, 32);
            var btnSetMoney = Theme.CreateButton("Set Money", Theme.Warning, 270, 32);
            var btnSetExp = Theme.CreateButton("Set Experience", Theme.Accent, 270, 32);
            var btnSetFame = Theme.CreateButton("Set Fame", Theme.Success, 270, 32);
            var btnViewItems = Theme.CreateButton("View Items", Theme.PrimaryDark, 270, 32);
            btnSetLevel.Margin = btnSetMoney.Margin = btnSetExp.Margin = btnSetFame.Margin = btnViewItems.Margin = new Padding(0, 2, 0, 2);
            btnPanel.Controls.AddRange(new Control[] { btnSetLevel, btnSetMoney, btnSetExp, btnSetFame, btnViewItems });

            detailPanel.Controls.Add(detailInfo);
            detailPanel.Controls.Add(btnPanel);
            detailPanel.Controls.Add(detailTitle);

            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var row = dgv.SelectedRows[0];
                detailInfo.Text = $"Name: {row.Cells["CharacterName"].Value}\n" +
                    $"Account: {row.Cells["AccountName"].Value}\n" +
                    $"Level: {row.Cells["Level"].Value}\n" +
                    $"Money: {row.Cells["Money"].Value:N0}\n" +
                    $"Race: {row.Cells["Race"].Value}  Unit: {row.Cells["UnitKind"].Value}\n" +
                    $"PK: {row.Cells["PKPoint"].Value}  Fame: {row.Cells["Fame"].Value}\n" +
                    $"Play Time: {row.Cells["PlayTime"].Value}";
            };

            btnSetLevel.Click += async (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var uid = (int)dgv.SelectedRows[0].Cells["UID"].Value;
                var input = Prompt("Enter new level (1-110):", "Set Level");
                if (input != null && int.TryParse(input, out int lvl) && lvl >= 1 && lvl <= 255)
                    if (await _db.SetCharacterLevelAsync(uid, lvl)) { ShowSuccess("Level updated!"); btnSearch.PerformClick(); }
            };

            btnSetMoney.Click += async (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var uid = (int)dgv.SelectedRows[0].Cells["UID"].Value;
                var input = Prompt("Enter money amount:", "Set Money");
                if (input != null && long.TryParse(input, out long money))
                    if (await _db.SetCharacterMoneyAsync(uid, money)) { ShowSuccess("Money updated!"); btnSearch.PerformClick(); }
            };

            btnSetExp.Click += async (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var uid = (int)dgv.SelectedRows[0].Cells["UID"].Value;
                var input = Prompt("Enter experience:", "Set Experience");
                if (input != null && long.TryParse(input, out long exp))
                    if (await _db.SetCharacterExperienceAsync(uid, exp)) { ShowSuccess("Experience updated!"); btnSearch.PerformClick(); }
            };

            btnSetFame.Click += async (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var uid = (int)dgv.SelectedRows[0].Cells["UID"].Value;
                var input = Prompt("Enter fame:", "Set Fame");
                if (input != null && int.TryParse(input, out int fame))
                    if (await _db.SetCharacterFameAsync(uid, fame)) { ShowSuccess("Fame updated!"); btnSearch.PerformClick(); }
            };

            btnViewItems.Click += async (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var uid = (int)dgv.SelectedRows[0].Cells["UID"].Value;
                var charName = dgv.SelectedRows[0].Cells["CharacterName"].Value?.ToString() ?? "";
                ShowItemsDialog(uid, charName);
            };

            async void DoSearch()
            {
                dgv.Rows.Clear();
                statusLabel.Text = "Searching...";
                var (chars, info) = await _db.SearchCharactersAsync(txtSearch.Text, chkByAccount.Checked);
                statusLabel.Text = info;
                foreach (var c in chars)
                {
                    dgv.Rows.Add(c.CharacterUniqueNumber, c.CharacterName, c.AccountName,
                        c.Level, c.Money, c.Race, c.UnitKind,
                        TimeSpan.FromSeconds(c.TotalPlayTime).ToString(@"d\d\ hh\h"), c.PKPoint, c.Fame);
                }
            }

            btnSearch.Click += (s, e) => DoSearch();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { DoSearch(); e.SuppressKeyPress = true; } };

            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(detailPanel);
            _contentPanel.Controls.Add(statusLabel);
            _contentPanel.Controls.Add(searchPanel);
            DoSearch();
        }

        private async void ShowItemsDialog(int charUID, string charName)
        {
            var dlg = new Form
            {
                Text = $"Items - {charName}",
                Size = new Size(800, 500),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Theme.Background,
                ForeColor = Theme.TextPrimary
            };

            var dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleDataGridView(dgv);
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "UID", HeaderText = "UID", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "ItemNum", HeaderText = "Item#", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Count", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "Window", HeaderText = "Window", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "Slot", HeaderText = "Slot", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "Prefix", HeaderText = "Prefix", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "Suffix", HeaderText = "Suffix", FillWeight = 8 },
                new DataGridViewTextBoxColumn { Name = "Rare", HeaderText = "Rare", FillWeight = 8 }
            );

            var ctx = new ContextMenuStrip { BackColor = Theme.CardBg, ForeColor = Theme.TextPrimary };
            var deleteItem = ctx.Items.Add("Delete Item");
            deleteItem!.Click += async (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var uid = (int)dgv.SelectedRows[0].Cells["UID"].Value;
                if (MessageBox.Show($"Delete item UID={uid}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (await _db.DeleteItemAsync(uid))
                    {
                        dgv.Rows.Remove(dgv.SelectedRows[0]);
                        ShowSuccess("Item deleted!");
                    }
                }
            };
            dgv.ContextMenuStrip = ctx;

            var (items, info) = await _db.GetCharacterItemsAsync(charUID);
            foreach (var item in items)
                dgv.Rows.Add(item.UniqueNumber, item.ItemNum, item.CurrentCount,
                    item.ItemWindowIndex, item.Possess, item.Prefix, item.Suffix, item.RareIndex);

            var lbl = new Label { Text = info, Dock = DockStyle.Bottom, Height = 25, ForeColor = Theme.TextMuted, Font = Theme.SmallFont };
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(lbl);
            dlg.ShowDialog(this);
        }

        // ═══════════════════════════════════════════════
        // GUILDS
        // ═══════════════════════════════════════════════
        private void LoadGuildsPage()
        {
            var (searchPanel, txtSearch, btnSearch) = CreateSearchBar("Search guild name...");
            var statusLabel = CreateStatusLabel();
            var dgv = CreateGrid("UID:UID:8", "GuildName:Guild Name:18", "Master:Master:15",
                "Members:Members:8", "Level:Level:8", "Fame:Fame:10", "Money:Money:12", "Created:Created:15");

            // Members panel
            var memberPanel = new Panel { Dock = DockStyle.Right, Width = 320, BackColor = Theme.CardBg, Padding = new Padding(12) };
            var memberBorder = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Theme.Border };
            memberPanel.Controls.Add(memberBorder);
            var memberTitle = new Label { Text = "Guild Members", Font = Theme.SubtitleFont, ForeColor = Theme.Accent, Dock = DockStyle.Top, Height = 35 };
            var memberList = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleDataGridView(memberList);
            memberList.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "MName", HeaderText = "Character", FillWeight = 30 },
                new DataGridViewTextBoxColumn { Name = "MRank", HeaderText = "Rank", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "MLvl", HeaderText = "Lv", FillWeight = 10 },
                new DataGridViewTextBoxColumn { Name = "MUnit", HeaderText = "Unit", FillWeight = 15 }
            );
            memberPanel.Controls.Add(memberList);
            memberPanel.Controls.Add(memberTitle);

            dgv.SelectionChanged += async (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                var uid = (int)dgv.SelectedRows[0].Cells["UID"].Value;
                memberList.Rows.Clear();
                memberTitle.Text = $"Members - {dgv.SelectedRows[0].Cells["GuildName"].Value}";
                var members = await _db.GetGuildMembersAsync(uid);
                foreach (var m in members)
                {
                    string rank = m.Rank switch { 0 => "Master", 1 => "Officer", _ => "Member" };
                    memberList.Rows.Add(m.CharacterName, rank, m.Level, m.UnitKind);
                }
            };

            async void DoSearch()
            {
                dgv.Rows.Clear();
                statusLabel.Text = "Searching...";
                var (guilds, info) = await _db.SearchGuildsAsync(txtSearch.Text);
                statusLabel.Text = info;
                foreach (var g in guilds)
                    dgv.Rows.Add(g.GuildUniqueNumber, g.GuildName, g.MasterCharacterName,
                        g.MemberCount, g.GuildLevel, g.GuildFame, g.GuildMoney,
                        g.CreateDate?.ToString("yyyy-MM-dd") ?? "");
            }

            btnSearch.Click += (s, e) => DoSearch();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { DoSearch(); e.SuppressKeyPress = true; } };

            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(memberPanel);
            _contentPanel.Controls.Add(statusLabel);
            _contentPanel.Controls.Add(searchPanel);
            DoSearch();
        }

        // ═══════════════════════════════════════════════
        // BANNED ACCOUNTS
        // ═══════════════════════════════════════════════
        private async void LoadBannedPage()
        {
            var dgv = CreateGrid("AccountName:Account:25", "Reason:Reason:30",
                "BannedBy:Banned By:15", "StartDate:Start:15", "EndDate:End:15");

            var ctx = CreateContextMenu(dgv,
                ("Unban Account", async () =>
                {
                    var name = GetSelectedCell(dgv, "AccountName");
                    if (name == null) return;
                    if (MessageBox.Show($"Unban '{name}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    { var (ubOk, _) = await _db.UnblockAccountAsync(name); if (ubOk) { ShowSuccess("Unbanned!"); SwitchPage(4); } }
                }
            )
            );

            _contentPanel.Controls.Add(dgv);

            var banned = await _db.GetBlockedAccountsAsync();
            foreach (var b in banned)
                dgv.Rows.Add(b.AccountName, b.BlockedReason, b.AdminAccountName,
                    b.StartDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    b.EndDate?.ToString("yyyy-MM-dd HH:mm") ?? "Permanent");
        }

        // ═══════════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════════
        private async void LoadEventsPage()
        {
            var statusLabel = CreateStatusLabel();
            var dgv = CreateGrid("UID:UID:8", "Type:Type:10", "BonusValue:Bonus:10",
                "StartDate:Start:20", "EndDate:End:20", "Active:Active:10");

            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(statusLabel);

            var (events, info) = await _db.GetHappyHourEventsAsync();
            statusLabel.Text = info;
            foreach (var ev in events)
            {
                int ri = dgv.Rows.Add(ev.EventUID, ev.BonusType, ev.BonusValue,
                    ev.StartDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    ev.EndDate?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    ev.IsActive ? "Active" : "Inactive");
                dgv.Rows[ri].Cells["Active"].Style.ForeColor = ev.IsActive ? Theme.Success : Theme.TextMuted;
            }
        }

        // ═══════════════════════════════════════════════
        // AUTO NOTICES
        // ═══════════════════════════════════════════════
        private async void LoadNoticesPage()
        {
            var statusLabel = CreateStatusLabel();

            // ── Settings panel ──
            var settingsPanel = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Theme.CardBg, Padding = new Padding(16, 12, 16, 8) };
            var settingsTitle = new Label { Text = "Admin Auto Notice Information", Font = Theme.SubtitleFont, ForeColor = Theme.Accent, Location = new Point(16, 8), AutoSize = true };

            var lblUse = new Label { Text = "Use Flag:", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(16, 42), AutoSize = true };
            var rbYes = new RadioButton { Text = "Yes", ForeColor = Theme.Success, Font = Theme.BodyFont, Location = new Point(100, 40), AutoSize = true };
            var rbNo = new RadioButton { Text = "No", ForeColor = Theme.Danger, Font = Theme.BodyFont, Location = new Point(160, 40), AutoSize = true, Checked = true };

            var lblLoop = new Label { Text = "Loop Time:", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(250, 42), AutoSize = true };
            var txtLoop = new TextBox { Text = "30", Width = 60, Location = new Point(340, 39) };
            Theme.StyleTextBox(txtLoop);
            var lblLoopUnit = new Label { Text = "(min)", Font = Theme.SmallFont, ForeColor = Theme.TextMuted, Location = new Point(405, 42), AutoSize = true };

            var lblInterval = new Label { Text = "Interval:", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(460, 42), AutoSize = true };
            var txtInterval = new TextBox { Text = "60", Width = 60, Location = new Point(535, 39) };
            Theme.StyleTextBox(txtInterval);
            var lblIntervalUnit = new Label { Text = "(sec)", Font = Theme.SmallFont, ForeColor = Theme.TextMuted, Location = new Point(600, 42), AutoSize = true };

            var lblEditor = new Label { Text = "", Font = Theme.SmallFont, ForeColor = Theme.TextMuted, Location = new Point(16, 72), AutoSize = true };

            settingsPanel.Controls.AddRange(new Control[] { settingsTitle, lblUse, rbYes, rbNo, lblLoop, txtLoop, lblLoopUnit, lblInterval, txtInterval, lblIntervalUnit, lblEditor });

            // ── Notice list ──
            var dgv = CreateGrid("Idx:Idx:8", "NoticeString:Notice String:85");
            dgv.AllowUserToOrderColumns = false;

            // ── Input panel ──
            var inputPanel = new Panel { Dock = DockStyle.Top, Height = 45 };
            var txtNotice = new TextBox { Width = 550, Height = 30, Location = new Point(0, 8) };
            Theme.StyleTextBox(txtNotice);
            txtNotice.PlaceholderText = "Enter notice message... (max 256 chars)";
            txtNotice.MaxLength = 256;

            var btnInsert = Theme.CreateButton("Insert", Theme.Success, 80, 30);
            btnInsert.Location = new Point(560, 8);
            var btnDelete = Theme.CreateButton("Delete", Theme.Danger, 80, 30);
            btnDelete.Location = new Point(648, 8);
            var btnUp = Theme.CreateButton("UP", Theme.PrimaryDark, 60, 30);
            btnUp.Location = new Point(750, 8);
            var btnDown = Theme.CreateButton("DOWN", Theme.PrimaryDark, 70, 30);
            btnDown.Location = new Point(818, 8);

            inputPanel.Controls.AddRange(new Control[] { txtNotice, btnInsert, btnDelete, btnUp, btnDown });

            // ── Bottom action panel ──
            var actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 45 };
            var btnUpdateDB = Theme.CreateButton("Update DB", Theme.Warning, 130, 34);
            btnUpdateDB.Location = new Point(0, 5);
            var btnReload = Theme.CreateButton("GameServer Reload", Theme.Primary, 160, 34);
            btnReload.Location = new Point(140, 5);
            actionPanel.Controls.AddRange(new Control[] { btnUpdateDB, btnReload });

            // ── Local notice list (editable before saving) ──
            var localNotices = new List<NoticeInfo>();

            void RefreshGrid()
            {
                dgv.Rows.Clear();
                for (int i = 0; i < localNotices.Count; i++)
                    dgv.Rows.Add(i, localNotices[i].NoticeString);
            }

            // Insert
            btnInsert.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNotice.Text)) return;
                if (localNotices.Count >= 20) { MessageBox.Show("Maximum 20 notices allowed.", "Limit", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                localNotices.Add(new NoticeInfo { NoticeString = txtNotice.Text, OrderIndex = localNotices.Count });
                txtNotice.Clear();
                RefreshGrid();
                statusLabel.Text = $"{localNotices.Count}/20 notices (unsaved)";
            };

            // Delete
            btnDelete.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int idx = dgv.SelectedRows[0].Index;
                if (idx >= 0 && idx < localNotices.Count)
                {
                    localNotices.RemoveAt(idx);
                    RefreshGrid();
                    statusLabel.Text = $"{localNotices.Count}/20 notices (unsaved)";
                }
            };

            // Move UP
            btnUp.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int idx = dgv.SelectedRows[0].Index;
                if (idx > 0)
                {
                    (localNotices[idx], localNotices[idx - 1]) = (localNotices[idx - 1], localNotices[idx]);
                    RefreshGrid();
                    dgv.Rows[idx - 1].Selected = true;
                }
            };

            // Move DOWN
            btnDown.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0) return;
                int idx = dgv.SelectedRows[0].Index;
                if (idx < localNotices.Count - 1)
                {
                    (localNotices[idx], localNotices[idx + 1]) = (localNotices[idx + 1], localNotices[idx]);
                    RefreshGrid();
                    dgv.Rows[idx + 1].Selected = true;
                }
            };

            // Update DB
            btnUpdateDB.Click += async (s, e) =>
            {
                var settings = new NoticeSettings
                {
                    UsingFlag = rbYes.Checked,
                    LoopSec = int.TryParse(txtLoop.Text, out int loop) ? loop * 60 : 1800, // min to sec
                    IntervalSec = int.TryParse(txtInterval.Text, out int intv) ? intv : 60
                };

                // Validate
                if (settings.LoopSec < 60) { MessageBox.Show("Loop time minimum 1 minute.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (settings.IntervalSec < 5) { MessageBox.Show("Interval time minimum 5 seconds.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                btnUpdateDB.Enabled = false;
                statusLabel.Text = "Saving to database...";
                var (ok, info) = await _db.SaveNoticeSystemAsync(settings, localNotices, _admin.AccountName);
                statusLabel.Text = info;
                statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                btnUpdateDB.Enabled = true;

                if (ok) lblEditor.Text = $"Last edited by: {_admin.AccountName}";
            };

            // GameServer Reload - connect to PreServer and send reload command
            btnReload.Click += async (s, e) =>
            {
                btnReload.Enabled = false;
                statusLabel.Text = "Connecting to PreServer...";
                statusLabel.ForeColor = Theme.TextSecondary;

                var proto = new AtumProtocolService(_config.PreServerIP, _config.PreServerPort);
                var (ok, info) = await proto.SendNoticeReloadAsync(_admin.AccountName, _admin.Password ?? "");

                if (!ok || !string.IsNullOrEmpty(proto.DebugLog))
                {
                    // Show detailed debug dialog
                    var msg = info + "\n\n── Debug Log ──\n" + proto.DebugLog;
                    MessageBox.Show(msg, ok ? "Success" : "Error",
                        MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                }

                statusLabel.Text = ok ? "Reload sent successfully!" : "Reload failed - see debug info";
                statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                btnReload.Enabled = true;
            };

            // Layout - order matters (Dock stacking)
            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(inputPanel);
            _contentPanel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
            _contentPanel.Controls.Add(settingsPanel);
            _contentPanel.Controls.Add(actionPanel);
            _contentPanel.Controls.Add(statusLabel);

            // Load from DB
            statusLabel.Text = "Loading...";
            var (loadedSettings, loadedStrings, loadInfo) = await _db.LoadNoticeSystemAsync();
            statusLabel.Text = loadInfo;

            if (loadedSettings != null)
            {
                rbYes.Checked = loadedSettings.UsingFlag;
                rbNo.Checked = !loadedSettings.UsingFlag;
                txtLoop.Text = (loadedSettings.LoopSec / 60).ToString(); // sec to min
                txtInterval.Text = loadedSettings.IntervalSec.ToString();
                lblEditor.Text = $"Last edited by: {loadedSettings.EditorAccountName}";
            }

            localNotices.AddRange(loadedStrings);
            RefreshGrid();
        }

        // ═══════════════════════════════════════════════
        // CASH SHOP
        // ═══════════════════════════════════════════════
        private async void LoadCashShopPage()
        {
            var statusLabel = CreateStatusLabel();
            var dgv = CreateGrid("ItemNum:Item#:10", "ItemName:Name:25", "Price:Price:10",
                "Category:Category:10", "IsNew:New:8", "IsRecommended:Recommended:10",
                "Limited:Limited:10", "Sold:Sold:10");

            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(statusLabel);

            var (items, info) = await _db.GetCashShopItemsAsync();
            statusLabel.Text = info;
            foreach (var item in items)
            {
                int ri = dgv.Rows.Add(item.ItemNum, item.ItemName, item.Price,
                    item.Category, item.IsNew ? "Yes" : "", item.IsRecommended ? "Yes" : "",
                    item.LimitedCount > 0 ? item.LimitedCount.ToString() : "-",
                    item.SoldCount > 0 ? item.SoldCount.ToString() : "-");
                if (item.IsNew) dgv.Rows[ri].Cells["IsNew"].Style.ForeColor = Theme.Accent;
                if (item.IsRecommended) dgv.Rows[ri].Cells["IsRecommended"].Style.ForeColor = Theme.Success;
            }
        }

        // ═══════════════════════════════════════════════
        // LOGS
        // ═══════════════════════════════════════════════
        private async void LoadLogsPage()
        {
            var statusLabel = CreateStatusLabel();

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 50 };
            var cmbTable = new ComboBox
            {
                Width = 200,
                Height = 30,
                Location = new Point(0, 10),
                DropDownStyle = ComboBoxStyle.DropDown,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = Theme.BodyFont
            };
            cmbTable.Items.AddRange(new[] { "tl_user", "tl_item", "tl_guilditem", "tl_connection" });
            cmbTable.Text = "tl_user";

            var txtQuery = new TextBox { Width = 200, Height = 30, Location = new Point(210, 10) };
            Theme.StyleTextBox(txtQuery);
            txtQuery.PlaceholderText = "Character/Account name...";

            var btnSearch = Theme.CreateButton("Search", Theme.Primary, 100, 30);
            btnSearch.Location = new Point(420, 10);

            toolPanel.Controls.AddRange(new Control[] { cmbTable, txtQuery, btnSearch });

            var dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleDataGridView(dgv);
            dgv.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", FillWeight = 15 },
                new DataGridViewTextBoxColumn { Name = "Account", HeaderText = "Account", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "Character", HeaderText = "Character", FillWeight = 12 },
                new DataGridViewTextBoxColumn { Name = "Detail", HeaderText = "Details", FillWeight = 55 }
            );

            btnSearch.Click += async (s, e) =>
            {
                dgv.Rows.Clear();
                statusLabel.Text = "Searching...";
                var (logs, info) = await _db.SearchLogsAsync(cmbTable.Text, txtQuery.Text, null, null);
                statusLabel.Text = info;
                foreach (var log in logs)
                    dgv.Rows.Add(log.LogDate.ToString("yyyy-MM-dd HH:mm:ss"), log.AccountName, log.CharacterName, log.Detail);
            };

            // Auto-load log tables
            var tables = await _db.GetLogTablesAsync();
            if (tables.Count > 0)
            {
                cmbTable.Items.Clear();
                cmbTable.Items.AddRange(tables.ToArray());
                cmbTable.Text = tables[0];
            }

            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(statusLabel);
            _contentPanel.Controls.Add(toolPanel);
        }

        // ═══════════════════════════════════════════════
        // SQL CONSOLE
        // ═══════════════════════════════════════════════
        private void LoadSqlConsolePage()
        {
            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
            var cmbDb = new ComboBox
            {
                Width = 200,
                Height = 30,
                Location = new Point(0, 5),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary,
                Font = Theme.BodyFont
            };
            cmbDb.Items.AddRange(new[] { "Account DB", "Game DB" });
            cmbDb.SelectedIndex = 1;

            var btnExecute = Theme.CreateButton("Execute (F5)", Theme.Primary, 120, 30);
            btnExecute.Location = new Point(210, 5);
            var statusLabel = new Label
            {
                Text = "Ready",
                Location = new Point(340, 10),
                AutoSize = true,
                Font = Theme.SmallFont,
                ForeColor = Theme.TextMuted
            };

            toolPanel.Controls.AddRange(new Control[] { cmbDb, btnExecute, statusLabel });

            // SQL editor
            var txtSql = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 120,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
                AcceptsTab = true,
                BackColor = Theme.Surface,
                ForeColor = Theme.Accent,
                Font = new Font("Cascadia Code", 10f, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "SELECT TOP 100 * FROM td_Character ORDER BY [Level] DESC"
            };

            // Result grid
            var dgvResult = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleDataGridView(dgvResult);
            dgvResult.ReadOnly = true;
            dgvResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            async Task ExecuteQuery()
            {
                try
                {
                    var connStr = cmbDb.SelectedIndex == 0 ? _config.GetAccountConnectionString() : _config.GetGameConnectionString();
                    var sql = txtSql.SelectedText.Length > 0 ? txtSql.SelectedText : txtSql.Text;

                    if (sql.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                        sql.Trim().StartsWith("EXEC", StringComparison.OrdinalIgnoreCase) ||
                        sql.Trim().StartsWith("SP_", StringComparison.OrdinalIgnoreCase))
                    {
                        var dt = await _db.QueryTableAsync(connStr, sql);
                        dgvResult.DataSource = dt;
                        statusLabel.Text = dt.TableName.StartsWith("Error") ? dt.TableName : $"{dt.Rows.Count} rows returned";
                        statusLabel.ForeColor = dt.TableName.StartsWith("Error") ? Theme.Danger : Theme.Success;
                    }
                    else
                    {
                        if (MessageBox.Show($"Execute non-SELECT query?\n\n{sql.Substring(0, Math.Min(200, sql.Length))}",
                            "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

                        var rows = await _db.ExecuteNonQueryAsync(connStr, sql);
                        statusLabel.Text = $"{rows} rows affected";
                        statusLabel.ForeColor = Theme.Success;
                    }
                }
                catch (Exception ex)
                {
                    statusLabel.Text = ex.Message;
                    statusLabel.ForeColor = Theme.Danger;
                }
            }

            btnExecute.Click += async (s, e) => await ExecuteQuery();
            txtSql.KeyDown += async (s, e) => { if (e.KeyCode == Keys.F5) { await ExecuteQuery(); e.SuppressKeyPress = true; } };

            _contentPanel.Controls.Add(dgvResult);
            _contentPanel.Controls.Add(txtSql);
            _contentPanel.Controls.Add(toolPanel);
        }

        // ═══════════════════════════════════════════════
        // STRATEGYPOINT MANAGER
        // ═══════════════════════════════════════════════
        private async void LoadStrategyPointPage()
        {
            var statusLabel = CreateStatusLabel();

            // ── Weekly Schedule Panel ──
            var schedulePanel = new GlassCard(Theme.Accent) { Dock = DockStyle.Top, Height = 240, Padding = new Padding(14, 10, 14, 10) };
            var schedTitle = new Label { Text = "Weekly Schedule", Font = Theme.SubtitleFont, ForeColor = Theme.Accent, Location = new Point(14, 10), AutoSize = true, BackColor = Color.Transparent };
            schedulePanel.Controls.Add(schedTitle);

            // Header row
            var headers = new[] { ("Week", 90), ("Start Time", 120), ("", 20), ("End Time", 120), ("BCU", 50), ("ANI", 50), ("", 160) };
            int hx = 12;
            foreach (var (text, w) in headers)
            {
                if (text == "") { hx += w; continue; }
                var lbl = new Label { Text = text, Font = Theme.SmallFont, ForeColor = Theme.TextMuted, Location = new Point(hx, 28), AutoSize = true };
                schedulePanel.Controls.Add(lbl);
                hx += w;
            }

            var dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            var startBoxes = new TextBox[7];
            var endBoxes = new TextBox[7];
            var bcuBoxes = new TextBox[7];
            var aniBoxes = new TextBox[7];

            for (int d = 0; d < 7; d++)
            {
                int y = 46 + d * 25;
                var dayLabel = new Label { Text = dayNames[d], Font = Theme.BodyFont, ForeColor = Theme.TextPrimary, Location = new Point(12, y + 2), AutoSize = true };
                schedulePanel.Controls.Add(dayLabel);

                startBoxes[d] = new TextBox { Text = "00:01:00", Width = 110, Location = new Point(102, y) };
                Theme.StyleTextBox(startBoxes[d]);
                var sep = new Label { Text = "~", ForeColor = Theme.TextMuted, Location = new Point(218, y + 2), AutoSize = true };
                endBoxes[d] = new TextBox { Text = "23:59:00", Width = 110, Location = new Point(232, y) };
                Theme.StyleTextBox(endBoxes[d]);
                bcuBoxes[d] = new TextBox { Text = "15", Width = 40, Location = new Point(352, y) };
                Theme.StyleTextBox(bcuBoxes[d]);
                aniBoxes[d] = new TextBox { Text = "15", Width = 40, Location = new Point(402, y) };
                Theme.StyleTextBox(aniBoxes[d]);

                int dayIdx = d;
                var btnApplyAll = Theme.CreateButton("Apply to all", Theme.PrimaryDark, 100, 22);
                btnApplyAll.Location = new Point(460, y);
                btnApplyAll.Click += (s, e) =>
                {
                    for (int i = 0; i < 7; i++)
                    {
                        startBoxes[i].Text = startBoxes[dayIdx].Text;
                        endBoxes[i].Text = endBoxes[dayIdx].Text;
                        bcuBoxes[i].Text = bcuBoxes[dayIdx].Text;
                        aniBoxes[i].Text = aniBoxes[dayIdx].Text;
                    }
                };

                schedulePanel.Controls.AddRange(new Control[] { startBoxes[d], sep, endBoxes[d], bcuBoxes[d], aniBoxes[d], btnApplyAll });
            }

            // ── Today's Summon Info Grid ──
            var dgv = CreateGrid("MapName:Map Name:25", "MapIndex:Map Index:12", "SummonTime:Summon Time:22", "Influence:Influence:12", "Creation:Creation:12");

            // Color influence column
            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
                var col = dgv.Columns[e.ColumnIndex].Name;
                if (col == "Influence")
                {
                    var val = e.Value?.ToString() ?? "";
                    e.CellStyle.ForeColor = val == "BCU" ? Color.FromArgb(100, 149, 237) : Color.FromArgb(220, 80, 80);
                }
                else if (col == "Creation")
                {
                    var val = e.Value?.ToString() ?? "";
                    e.CellStyle.ForeColor = val == "Finish" ? Theme.Success : Theme.Warning;
                }
            };

            // ── Right-click context menu for grid ──
            var ctxMenu = new ContextMenuStrip
            {
                BackColor = Theme.CardBg,
                ForeColor = Theme.TextPrimary,
                Font = Theme.BodyFont,
                RenderMode = ToolStripRenderMode.Professional
            };
            ctxMenu.Renderer = new ModernMenuRenderer();
            var mnuSummon = new ToolStripMenuItem("Summon Now") { ForeColor = Theme.Success, Font = Theme.BodyFont, Padding = new Padding(8, 4, 8, 4) };
            var mnuCancel = new ToolStripMenuItem("Cancel Summon") { ForeColor = Theme.Danger, Font = Theme.BodyFont, Padding = new Padding(8, 4, 8, 4) };
            ctxMenu.Items.AddRange(new ToolStripItem[] { mnuSummon, mnuCancel });
            dgv.ContextMenuStrip = ctxMenu;

            // Helper to get selected map
            (int mapIndex, string mapName) GetSelectedMap()
            {
                if (dgv.SelectedRows.Count == 0) return (-1, "");
                var row = dgv.SelectedRows[0];
                return (Convert.ToInt32(row.Cells["MapIndex"].Value), row.Cells["MapName"].Value?.ToString() ?? "");
            }

            mnuSummon.Click += async (s, e) =>
            {
                var (mapIndex, mapName) = GetSelectedMap();
                if (mapIndex < 0) return;

                var result = MessageBox.Show(
                    $"Summon StrategyPoint boss NOW on:\n\n{mapName} (MapIndex: {mapIndex})\n\nAre you sure?",
                    "Confirm Summon", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;

                statusLabel.Text = $"Scheduling summon for {mapName}...";
                statusLabel.ForeColor = Theme.TextSecondary;

                // Step 1: Update DB
                var (ok, info) = await _db.ForceStrategyPointSummonAsync(mapIndex, mapName);
                if (!ok) { statusLabel.Text = info; statusLabel.ForeColor = Theme.Danger; return; }

                // Step 2: Notify server to reload data ONLY (no recalculation)
                // dataReloadOnly=true sends ONLY T_PA_ADMIN_STRATRGYPOINT_INFO_CHANGE
                // which loads our DB values (SummonCount=1, SummonAttribute=1, SummonTime=past)
                // directly into server memory. The spawn timer will see these and summon immediately.
                statusLabel.Text = "DB updated. Notifying server (data reload)...";
                var proto = new AtumProtocolService(_config.PreServerIP, _config.PreServerPort);
                var (reloadOk, reloadInfo) = await proto.SendStrategyPointReloadAsync(
                    _admin.AccountName, _admin.Password ?? "", _config.GameDbName, dataReloadOnly: true);

                statusLabel.Text = reloadOk
                    ? $"Summon triggered for {mapName}! Boss should appear shortly."
                    : $"DB updated but server notify failed: {reloadInfo}";
                statusLabel.ForeColor = reloadOk ? Theme.Success : Theme.Warning;

                // Refresh grid
                await LoadData();
            };

            mnuCancel.Click += async (s, e) =>
            {
                var (mapIndex, mapName) = GetSelectedMap();
                if (mapIndex < 0) return;

                var (ok, info) = await _db.CancelStrategyPointSummonAsync(mapIndex);
                statusLabel.Text = ok ? $"Cancelled summon for {mapName}" : info;
                statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;

                // Notify server
                var proto = new AtumProtocolService(_config.PreServerIP, _config.PreServerPort);
                await proto.SendStrategyPointReloadAsync(_admin.AccountName, _admin.Password ?? "", _config.GameDbName);

                await LoadData();
            };

            // ── Bottom action panel ──
            var actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            var btnTodaySP = Theme.CreateButton("Today SP Setting", Theme.Accent, 150, 36);
            btnTodaySP.Location = new Point(0, 7);
            var btnSave = Theme.CreateButton("Save DB and Server Apply", Theme.Warning, 220, 36);
            btnSave.Location = new Point(160, 7);
            var btnRefresh = Theme.CreateButton("Refresh", Theme.Primary, 100, 36);
            btnRefresh.Location = new Point(390, 7);
            var btnSummonSelected = Theme.CreateButton("Summon Selected", Theme.Success, 140, 36);
            btnSummonSelected.Location = new Point(500, 7);
            actionPanel.Controls.AddRange(new Control[] { btnTodaySP, btnSave, btnRefresh, btnSummonSelected });

            // Summon Selected button (same as context menu)
            btnSummonSelected.Click += (s, e) => mnuSummon.PerformClick();

            // ── Today SP Setting: generate random summon times for today ──
            // Tracks the last generated plan so "Save DB and Server Apply" can persist it
            List<StrategyPointMapInfo>? generatedPlan = null;

            btnTodaySP.Click += async (s, e) =>
            {
                btnTodaySP.Enabled = false;
                statusLabel.Text = "Generating today's SP plan...";
                statusLabel.ForeColor = Theme.TextSecondary;

                try
                {
                    // 1. Get today's schedule from textboxes
                    int todayDow = (int)DateTime.Now.DayOfWeek; // 0=Sunday
                    if (!DateTime.TryParse(startBoxes[todayDow].Text, out var startTime) ||
                        !DateTime.TryParse(endBoxes[todayDow].Text, out var endTime))
                    {
                        statusLabel.Text = "Invalid start/end time for today!";
                        statusLabel.ForeColor = Theme.Danger;
                        btnTodaySP.Enabled = true;
                        return;
                    }
                    int countBCU = int.TryParse(bcuBoxes[todayDow].Text, out var b) ? b : 15;
                    int countANI = int.TryParse(aniBoxes[todayDow].Text, out var a) ? a : 15;
                    int totalCount = countBCU + countANI;

                    // Set start/end to today's date with the specified time
                    var today = DateTime.Now.Date;
                    startTime = today.Add(startTime.TimeOfDay);
                    endTime = today.Add(endTime.TimeOfDay);

                    if (endTime <= startTime || totalCount <= 0)
                    {
                        statusLabel.Text = "Invalid schedule: check times and counts!";
                        statusLabel.ForeColor = Theme.Danger;
                        btnTodaySP.Enabled = true;
                        return;
                    }

                    // 2. Load maps with influence from DB
                    var (mapData, mapInfo) = await _db.LoadStrategyPointMapsWithInfluenceAsync();
                    if (mapData.Count == 0)
                    {
                        statusLabel.Text = $"No maps loaded. {mapInfo}";
                        statusLabel.ForeColor = Theme.Danger;
                        btnTodaySP.Enabled = true;
                        return;
                    }

                    // 3. Generate random summon times (port of SettingRandumSummonTime)
                    // RenewalStrategyPointSummonTimeTermMin = 1800 seconds (30 minutes)
                    const int SummonTimeTermMin = 1800;
                    int totalSeconds = (int)(endTime - startTime).TotalSeconds;
                    int maxGap = totalCount > 0 ? totalSeconds / totalCount : totalSeconds;
                    maxGap -= SummonTimeTermMin; // subtract 30-minute minimum gap
                    if (maxGap < 0) maxGap = 0;

                    var rng = new Random();
                    var randomTimes = new List<DateTime>();
                    var currentStart = startTime;

                    for (int i = 0; i < totalCount; i++)
                    {
                        int randomGap = rng.Next(0, maxGap + 1);
                        var summonTime = currentStart.AddSeconds(randomGap);
                        randomTimes.Add(summonTime);
                        // Advance: add the remaining gap + 30-minute minimum term
                        currentStart = currentStart.AddSeconds(SummonTimeTermMin + maxGap - randomGap);
                    }

                    // 4. Create map entries and shuffle randomly
                    var shuffledMaps = mapData.Select(m => new { m.MapIndex, m.MapName, m.Influence })
                        .OrderBy(_ => rng.Next()).ToList();

                    // 5. Assign BCU/ANI maps with summon times
                    int bcuAssigned = 0, aniAssigned = 0;
                    int timeIndex = 0;
                    var plan = new List<StrategyPointMapInfo>();

                    foreach (var map in shuffledMaps)
                    {
                        var entry = new StrategyPointMapInfo
                        {
                            MapIndex = map.MapIndex,
                            MapName = map.MapName,
                            Influence = map.Influence,
                            SummonTime = null,
                            Creation = "Finish" // default: not scheduled
                        };

                        bool assigned = false;
                        if (map.Influence == "BCU" && bcuAssigned < countBCU)
                        {
                            entry.Creation = "Scheduled";
                            bcuAssigned++;
                            assigned = true;
                        }
                        else if (map.Influence == "ANI" && aniAssigned < countANI)
                        {
                            entry.Creation = "Scheduled";
                            aniAssigned++;
                            assigned = true;
                        }

                        if (assigned && timeIndex < randomTimes.Count)
                        {
                            entry.SummonTime = randomTimes[timeIndex];
                            timeIndex++;
                        }

                        plan.Add(entry);
                    }

                    // 6. Sort by SummonTime for display
                    plan.Sort((x, y) =>
                    {
                        if (x.Creation == "Scheduled" && y.Creation != "Scheduled") return -1;
                        if (x.Creation != "Scheduled" && y.Creation == "Scheduled") return 1;
                        return (x.SummonTime ?? DateTime.MaxValue).CompareTo(y.SummonTime ?? DateTime.MaxValue);
                    });

                    // 7. Display in grid (preview only — not saved yet)
                    dgv.Rows.Clear();
                    foreach (var m in plan)
                    {
                        dgv.Rows.Add(m.MapName, m.MapIndex,
                            m.SummonTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                            m.Influence, m.Creation);
                    }

                    generatedPlan = plan;
                    statusLabel.Text = $"Generated: {bcuAssigned} BCU + {aniAssigned} ANI = {bcuAssigned + aniAssigned} summons. Click 'Save DB and Server Apply' to apply.";
                    statusLabel.ForeColor = Theme.Success;
                }
                catch (Exception ex)
                {
                    statusLabel.Text = $"Error: {ex.Message}";
                    statusLabel.ForeColor = Theme.Danger;
                }
                btnTodaySP.Enabled = true;
            };

            // Layout
            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
            _contentPanel.Controls.Add(schedulePanel);
            _contentPanel.Controls.Add(actionPanel);
            _contentPanel.Controls.Add(statusLabel);

            // Load data
            async Task LoadData()
            {
                statusLabel.Text = "Loading...";
                statusLabel.ForeColor = Theme.TextSecondary;
                var (schedule, maps, info) = await _db.LoadStrategyPointDataAsync();
                statusLabel.Text = info;

                // Fill schedule textboxes
                foreach (var day in schedule)
                {
                    if (day.DayOfWeek >= 0 && day.DayOfWeek < 7)
                    {
                        startBoxes[day.DayOfWeek].Text = day.StartTime.ToString("HH:mm:ss");
                        endBoxes[day.DayOfWeek].Text = day.EndTime.ToString("HH:mm:ss");
                        bcuBoxes[day.DayOfWeek].Text = day.CountBCU.ToString();
                        aniBoxes[day.DayOfWeek].Text = day.CountANI.ToString();
                    }
                }

                // Fill grid
                dgv.Rows.Clear();
                foreach (var m in maps)
                {
                    dgv.Rows.Add(m.MapName, m.MapIndex,
                        m.SummonTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                        m.Influence, m.Creation);
                }
            }

            // Save & Apply
            btnSave.Click += async (s, e) =>
            {
                btnSave.Enabled = false;
                statusLabel.Text = "Saving...";

                // Build schedule from textboxes
                var schedule = new List<StrategyPointSchedule>();
                for (int d = 0; d < 7; d++)
                {
                    DateTime.TryParse(startBoxes[d].Text, out var start);
                    DateTime.TryParse(endBoxes[d].Text, out var end);
                    schedule.Add(new StrategyPointSchedule
                    {
                        DayOfWeek = d,
                        StartTime = start,
                        EndTime = end,
                        CountBCU = int.TryParse(bcuBoxes[d].Text, out var bcu) ? bcu : 15,
                        CountANI = int.TryParse(aniBoxes[d].Text, out var ani) ? ani : 15
                    });
                }

                // Save schedule to Account DB
                var (ok, info) = await _db.SaveStrategyPointScheduleAsync(schedule);
                if (!ok) { statusLabel.Text = info; statusLabel.ForeColor = Theme.Danger; btnSave.Enabled = true; return; }

                // If there's a generated Today SP plan, save it to Game DB
                if (generatedPlan != null && generatedPlan.Count > 0)
                {
                    statusLabel.Text = "Saving summon plan to Game DB...";
                    var (planOk, planInfo) = await _db.SaveTodayStrategyPointPlanAsync(schedule, generatedPlan);
                    if (!planOk) { statusLabel.Text = planInfo; statusLabel.ForeColor = Theme.Danger; btnSave.Enabled = true; return; }
                }

                // Send server reload via protocol
                // Sends T_PA_ADMIN_STRATRGYPOINT_INFO_CHANGE (0xB01D) to reload data
                statusLabel.Text = "Saved. Sending reload to server...";
                var proto = new AtumProtocolService(_config.PreServerIP, _config.PreServerPort);
                var (reloadOk, reloadInfo) = await proto.SendStrategyPointReloadAsync(_admin.AccountName, _admin.Password ?? "", _config.GameDbName);
                statusLabel.Text = reloadOk ? "Saved & applied to server!" : $"Saved to DB. Server: {reloadInfo}";
                statusLabel.ForeColor = reloadOk ? Theme.Success : Theme.Warning;
                generatedPlan = null; // Clear after successful save
                btnSave.Enabled = true;

                // Refresh grid to show current state
                await LoadData();
            };

            btnRefresh.Click += async (s, e) => await LoadData();

            await LoadData();
        }

        // ═══════════════════════════════════════════════
        // EVENT MONSTER MANAGEMENT
        // ═══════════════════════════════════════════════
        private async void LoadEventMonsterPage()
        {
            var statusLabel = CreateStatusLabel();

            // ── Load lookup dictionaries ──
            Dictionary<int, string> monsterNames = new();
            Dictionary<int, string> mapNames = new();
            try
            {
                monsterNames = await _db.LoadMonsterNamesAsync();
                mapNames = await _db.LoadMapNamesAsync();
            }
            catch { /* fallback to empty – will show IDs */ }

            string ResolveMonster(int monNum) => monsterNames.TryGetValue(monNum, out var name) ? name : $"#{monNum}";
            string ResolveMap(int mapIdx) => mapIdx == 0 ? "Any Map" : (mapNames.TryGetValue(mapIdx, out var name) ? name : $"#{mapIdx}");

            // ── Grid ──
            var dgv = CreateGrid(
                "UID:UID:5", "ServerGroup:Server:6", "StartDate:Start Date:12", "EndDate:End Date:12",
                "MapName:Map:12", "MinLevel:MinLv:5", "MaxLevel:MaxLv:5", "ExceptMon:Except:6",
                "MonsterName:Monster:14", "MonsterCount:Count:5", "DelayTime:Delay(s):5",
                "Probability:Prob:5", "Status:Status:6");

            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
                var col = dgv.Columns[e.ColumnIndex].Name;
                if (col == "Status")
                {
                    var val = e.Value?.ToString() ?? "";
                    e.CellStyle.ForeColor = val switch
                    {
                        "Active" => Theme.Success,
                        "Scheduled" => Theme.Accent,
                        _ => Theme.TextMuted
                    };
                }
            };

            // ── Bottom action panel ──
            var actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            var btnAdd = Theme.CreateButton("\u2795  Add Event Monster", Theme.Success, 190, 36);
            btnAdd.Location = new Point(0, 7);
            var btnModify = Theme.CreateButton("\u270E  Modify", Theme.Warning, 110, 36);
            btnModify.Location = new Point(200, 7);
            var btnDelete = Theme.CreateButton("\u2716  Delete", Theme.Danger, 110, 36);
            btnDelete.Location = new Point(320, 7);
            var btnRefresh = Theme.CreateButton("Refresh", Theme.Primary, 100, 36);
            btnRefresh.Location = new Point(440, 7);
            actionPanel.Controls.AddRange(new Control[] { btnAdd, btnModify, btnDelete, btnRefresh });

            // Context menu
            var ctxMenu = new ContextMenuStrip
            {
                BackColor = Theme.CardBg,
                ForeColor = Theme.TextPrimary,
                Font = Theme.BodyFont,
                RenderMode = ToolStripRenderMode.Professional
            };
            ctxMenu.Renderer = new ModernMenuRenderer();
            var mnuModify = new ToolStripMenuItem("Modify Event Monster") { ForeColor = Theme.Warning, Padding = new Padding(8, 4, 8, 4) };
            var mnuDelete = new ToolStripMenuItem("Delete Event Monster") { ForeColor = Theme.Danger, Padding = new Padding(8, 4, 8, 4) };
            ctxMenu.Items.AddRange(new ToolStripItem[] { mnuModify, mnuDelete });
            dgv.ContextMenuStrip = ctxMenu;

            // Layout
            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(actionPanel);
            _contentPanel.Controls.Add(statusLabel);

            // ── Load data ──
            async Task LoadData()
            {
                statusLabel.Text = "Loading event monsters...";
                statusLabel.ForeColor = Theme.TextSecondary;
                var (list, info) = await _db.LoadEventMonstersAsync();
                statusLabel.Text = info;
                dgv.Rows.Clear();
                foreach (var em in list)
                {
                    string exceptStr = "";
                    if (em.ExceptObjectMonster) exceptStr += "Obj ";
                    if (em.ExceptInfluenceMonster) exceptStr += "Infl ";
                    if (em.ExceptNotAttackMonster) exceptStr += "NoAtk";
                    exceptStr = exceptStr.Trim();
                    if (string.IsNullOrEmpty(exceptStr)) exceptStr = "-";

                    int ri = dgv.Rows.Add(
                        em.EventMonsterUID,
                        em.ServerGroupID == 0 ? "All" : em.ServerGroupID.ToString(),
                        em.StartDateTime.ToString("yyyy-MM-dd HH:mm"),
                        em.EndDateTime.ToString("yyyy-MM-dd HH:mm"),
                        ResolveMap(em.SummonerMapIndex),
                        em.SummonerReqMinLevel == 0 ? "-" : em.SummonerReqMinLevel.ToString(),
                        em.SummonerReqMaxLevel == 0 ? "-" : em.SummonerReqMaxLevel.ToString(),
                        exceptStr,
                        ResolveMonster(em.SummonMonsterNum),
                        em.SummonMonsterCount,
                        em.SummonDelayTime,
                        em.SummonProbability,
                        em.Status);
                }
            }

            // ── Get selected event monster ──
            EventMonster? GetSelectedFromGrid()
            {
                if (dgv.SelectedRows.Count == 0) return null;
                var row = dgv.SelectedRows[0];
                var uidStr = row.Cells["UID"].Value?.ToString();
                if (!int.TryParse(uidStr, out var uid)) return null;
                return new EventMonster { EventMonsterUID = uid };
            }

            // ── Helper: create a searchable ComboBox ──
            ComboBox CreateSearchableCombo(int x, int yPos, int width, List<KeyValuePair<int, string>> items, int selectedKey)
            {
                var cmb = new ComboBox
                {
                    Location = new Point(x, yPos),
                    Width = width,
                    DropDownStyle = ComboBoxStyle.DropDown,
                    BackColor = Theme.Surface,
                    ForeColor = Theme.TextPrimary,
                    Font = Theme.BodyFont,
                    AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                    AutoCompleteSource = AutoCompleteSource.ListItems
                };
                foreach (var kv in items)
                    cmb.Items.Add(kv);
                cmb.DisplayMember = "Value";
                cmb.ValueMember = "Key";
                // Format: show "Name (ID)"
                cmb.Format += (s, e) =>
                {
                    if (e.ListItem is KeyValuePair<int, string> kv)
                        e.Value = $"{kv.Value} ({kv.Key})";
                };
                // Select the matching item
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Key == selectedKey) { cmb.SelectedIndex = i; break; }
                }
                return cmb;
            }

            int GetComboSelectedKey(ComboBox cmb)
            {
                if (cmb.SelectedItem is KeyValuePair<int, string> kv) return kv.Key;
                // Fallback: try parse from text (user may have typed a number)
                var txt = cmb.Text.Trim();
                if (int.TryParse(txt, out var num)) return num;
                // Try extract ID from "Name (ID)" format
                var m = System.Text.RegularExpressions.Regex.Match(txt, @"\((\d+)\)\s*$");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var id)) return id;
                return -1;
            }

            // ── Build map and monster item lists for ComboBoxes ──
            var mapItems = new List<KeyValuePair<int, string>> { new(0, "Any Map") };
            foreach (var kv in mapNames.OrderBy(k => k.Key))
                mapItems.Add(new(kv.Key, kv.Value));

            var monsterItems = new List<KeyValuePair<int, string>>();
            foreach (var kv in monsterNames.OrderBy(k => k.Key))
                monsterItems.Add(new(kv.Key, kv.Value));

            // ── Show editor dialog ──
            async Task<bool> ShowEventMonsterEditor(EventMonster em, bool isNew)
            {
                using var dlg = new Form
                {
                    Text = isNew ? "Add Event Monster" : $"Modify Event Monster (UID: {em.EventMonsterUID})",
                    Size = new Size(540, 580),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Theme.Background
                };

                int y = 16;
                const int lblX = 20, valX = 200, valW = 300;

                Label AddLabel(string text)
                {
                    var lbl = new Label { Text = text, Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(lblX, y + 4), AutoSize = true };
                    dlg.Controls.Add(lbl);
                    return lbl;
                }
                TextBox AddTextBox(string defaultVal)
                {
                    var tb = new TextBox { Text = defaultVal, Location = new Point(valX, y), Width = valW };
                    Theme.StyleTextBox(tb);
                    dlg.Controls.Add(tb);
                    y += 34;
                    return tb;
                }

                // Server Group
                AddLabel("Server Group:");
                var cmbServerGroup = new ComboBox { Location = new Point(valX, y), Width = valW, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.Surface, ForeColor = Theme.TextPrimary, Font = Theme.BodyFont };
                cmbServerGroup.Items.Add("All Servers (0)");
                for (int i = 0; i < 20; i++) cmbServerGroup.Items.Add($"Group {10061 + i}");
                int sgIdx = em.ServerGroupID == 0 ? 0 : (em.ServerGroupID - 10061 + 1);
                if (sgIdx < 0 || sgIdx >= cmbServerGroup.Items.Count) sgIdx = 0;
                cmbServerGroup.SelectedIndex = sgIdx;
                dlg.Controls.Add(cmbServerGroup);
                y += 34;

                // Start DateTime
                AddLabel("Start Date/Time:");
                var dtpStartDate = new DateTimePicker { Location = new Point(valX, y), Width = 160, Format = DateTimePickerFormat.Short, Value = em.StartDateTime > DateTime.MinValue ? em.StartDateTime : DateTime.Now };
                var dtpStartTime = new DateTimePicker { Location = new Point(valX + 168, y), Width = 132, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = em.StartDateTime > DateTime.MinValue ? em.StartDateTime : DateTime.Now };
                dlg.Controls.AddRange(new Control[] { dtpStartDate, dtpStartTime });
                y += 34;

                // End DateTime
                AddLabel("End Date/Time:");
                var dtpEndDate = new DateTimePicker { Location = new Point(valX, y), Width = 160, Format = DateTimePickerFormat.Short, Value = em.EndDateTime > DateTime.MinValue ? em.EndDateTime : DateTime.Now.AddDays(1) };
                var dtpEndTime = new DateTimePicker { Location = new Point(valX + 168, y), Width = 132, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = em.EndDateTime > DateTime.MinValue ? em.EndDateTime : DateTime.Now };
                dlg.Controls.AddRange(new Control[] { dtpEndDate, dtpEndTime });
                y += 34;

                // Map (ComboBox dropdown)
                AddLabel("Map:");
                var cmbMap = CreateSearchableCombo(valX, y, valW, mapItems, em.SummonerMapIndex);
                dlg.Controls.Add(cmbMap);
                y += 34;

                // Level range
                AddLabel("Min Level (0=Any):");
                var txtMinLv = AddTextBox(em.SummonerReqMinLevel.ToString());
                AddLabel("Max Level (0=Any):");
                var txtMaxLv = AddTextBox(em.SummonerReqMaxLevel.ToString());

                // Exception flags
                AddLabel("Except Flags:");
                var chkExObj = new CheckBox { Text = "Object Monster", ForeColor = Theme.TextPrimary, Font = Theme.SmallFont, Location = new Point(valX, y), Checked = em.ExceptObjectMonster, AutoSize = true };
                var chkExInfl = new CheckBox { Text = "Influence Mon", ForeColor = Theme.TextPrimary, Font = Theme.SmallFont, Location = new Point(valX + 140, y), Checked = em.ExceptInfluenceMonster, AutoSize = true };
                y += 24;
                var chkExNoAtk = new CheckBox { Text = "Non-Attack Mon", ForeColor = Theme.TextPrimary, Font = Theme.SmallFont, Location = new Point(valX, y), Checked = em.ExceptNotAttackMonster, AutoSize = true };
                dlg.Controls.AddRange(new Control[] { chkExObj, chkExInfl, chkExNoAtk });
                y += 30;

                // Monster (ComboBox dropdown)
                AddLabel("Monster:");
                var cmbMonster = CreateSearchableCombo(valX, y, valW, monsterItems, em.SummonMonsterNum);
                dlg.Controls.Add(cmbMonster);
                y += 34;

                AddLabel("Monster Count (1-100):");
                var txtMonCnt = AddTextBox(em.SummonMonsterCount == 0 ? "1" : em.SummonMonsterCount.ToString());
                AddLabel("Delay Time (1-600 sec):");
                var txtDelay = AddTextBox(em.SummonDelayTime == 0 ? "10" : em.SummonDelayTime.ToString());
                AddLabel("Probability (0-10000):");
                var txtProb = AddTextBox(em.SummonProbability == 0 ? "5000" : em.SummonProbability.ToString());

                // Buttons
                var btnOk = Theme.CreateButton(isNew ? "Create" : "Update", Theme.Primary, 120, 38);
                btnOk.Location = new Point(valX, y + 8);
                var btnCancel = Theme.CreateButton("Cancel", Theme.Danger, 100, 38);
                btnCancel.Location = new Point(valX + 130, y + 8);
                dlg.Controls.AddRange(new Control[] { btnOk, btnCancel });

                var tcs = new TaskCompletionSource<bool>();

                btnOk.Click += (s, e) =>
                {
                    // Build start/end datetime
                    var startDt = dtpStartDate.Value.Date + dtpStartTime.Value.TimeOfDay;
                    var endDt = dtpEndDate.Value.Date + dtpEndTime.Value.TimeOfDay;
                    if (startDt >= endDt) { MessageBox.Show("Start must be before End!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                    int mapIdx = GetComboSelectedKey(cmbMap);
                    if (mapIdx < 0) { MessageBox.Show("Please select a valid map!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                    int.TryParse(txtMinLv.Text, out var minLv);
                    int.TryParse(txtMaxLv.Text, out var maxLv);
                    if (minLv > 0 && maxLv > 0 && minLv > maxLv) { MessageBox.Show("Min level must be <= Max level!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                    int monNum = GetComboSelectedKey(cmbMonster);
                    if (monNum <= 0) { MessageBox.Show("Please select a valid monster!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                    if (!int.TryParse(txtMonCnt.Text, out var monCnt) || monCnt < 1 || monCnt > 100) { MessageBox.Show("Monster Count must be 1-100!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    if (!int.TryParse(txtDelay.Text, out var delay) || delay < 1 || delay > 600) { MessageBox.Show("Delay must be 1-600 seconds!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    if (!int.TryParse(txtProb.Text, out var prob) || prob < 0 || prob > 10000) { MessageBox.Show("Probability must be 0-10000!", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                    // Build ServerGroupID
                    int sgid = cmbServerGroup.SelectedIndex == 0 ? 0 : (10061 + cmbServerGroup.SelectedIndex - 1);

                    // Build except flags
                    int exceptFlags = 0;
                    if (chkExObj.Checked) exceptFlags |= 0x01;
                    if (chkExInfl.Checked) exceptFlags |= 0x02;
                    if (chkExNoAtk.Checked) exceptFlags |= 0x04;

                    em.ServerGroupID = sgid;
                    em.StartDateTime = startDt;
                    em.EndDateTime = endDt;
                    em.SummonerMapIndex = mapIdx;
                    em.SummonerReqMinLevel = minLv;
                    em.SummonerReqMaxLevel = maxLv;
                    em.SummonerExceptMonster = exceptFlags;
                    em.SummonMonsterNum = monNum;
                    em.SummonMonsterCount = monCnt;
                    em.SummonDelayTime = delay;
                    em.SummonProbability = prob;

                    tcs.TrySetResult(true);
                    dlg.Close();
                };
                btnCancel.Click += (s, e) => { tcs.TrySetResult(false); dlg.Close(); };
                dlg.FormClosing += (s, e) => tcs.TrySetResult(false);

                dlg.ShowDialog();
                return await tcs.Task;
            }

            // ── Add button ──
            btnAdd.Click += async (s, e) =>
            {
                var em = new EventMonster
                {
                    ExceptObjectMonster = true,
                    ExceptInfluenceMonster = true,
                    ExceptNotAttackMonster = true,
                    StartDateTime = DateTime.Now,
                    EndDateTime = DateTime.Now.AddDays(1)
                };
                if (await ShowEventMonsterEditor(em, true))
                {
                    statusLabel.Text = "Creating event monster...";
                    var (ok, info) = await _db.InsertEventMonsterAsync(em);
                    statusLabel.Text = info;
                    statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                    if (ok) await LoadData();
                }
            };

            // ── Modify handler ──
            async Task DoModify()
            {
                var sel = GetSelectedFromGrid();
                if (sel == null) { statusLabel.Text = "Select an event monster first."; statusLabel.ForeColor = Theme.Warning; return; }

                // Reload full data from DB
                var (list, _) = await _db.LoadEventMonstersAsync();
                var em = list.FirstOrDefault(e => e.EventMonsterUID == sel.EventMonsterUID);
                if (em == null) { statusLabel.Text = "Event monster not found. Reload."; statusLabel.ForeColor = Theme.Danger; return; }

                if (await ShowEventMonsterEditor(em, false))
                {
                    statusLabel.Text = "Updating event monster...";
                    var (ok, info) = await _db.UpdateEventMonsterAsync(em);
                    statusLabel.Text = info;
                    statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                    if (ok) await LoadData();
                }
            }

            btnModify.Click += async (s, e) => await DoModify();
            mnuModify.Click += async (s, e) => await DoModify();

            // ── Delete handler ──
            async Task DoDelete()
            {
                var sel = GetSelectedFromGrid();
                if (sel == null) { statusLabel.Text = "Select an event monster first."; statusLabel.ForeColor = Theme.Warning; return; }

                var result = MessageBox.Show(
                    $"Delete Event Monster UID: {sel.EventMonsterUID}?",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes) return;

                statusLabel.Text = "Deleting...";
                var (ok, info) = await _db.DeleteEventMonsterAsync(sel.EventMonsterUID);
                statusLabel.Text = info;
                statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                if (ok) await LoadData();
            }

            btnDelete.Click += async (s, e) => await DoDelete();
            mnuDelete.Click += async (s, e) => await DoDelete();

            // ── Refresh ──
            btnRefresh.Click += async (s, e) => await LoadData();

            await LoadData();
        }

        // ═══════════════════════════════════════════════
        // INFLUENCE WAR (MOTHERSHIP WAR)
        // ═══════════════════════════════════════════════

        /// <summary>Send Declaration of War reload to all FieldServers via PreServer.</summary>
        private async Task SendWarReloadToServer(Label statusLabel)
        {
            try
            {
                var proto = new AtumProtocolService(_config.PreServerIP, _config.PreServerPort);
                var (reloadOk, reloadInfo) = await proto.SendDeclarationOfWarReloadAsync(
                    _admin.AccountName, _admin.Password ?? "");
                if (reloadOk)
                {
                    statusLabel.Text += " | Server reloaded.";
                }
                else
                {
                    statusLabel.Text += $" | Reload failed: {reloadInfo}";
                    statusLabel.ForeColor = Theme.Warning;
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text += $" | Reload error: {ex.Message}";
                statusLabel.ForeColor = Theme.Warning;
            }
        }

        private async void LoadInfluenceWarPage()
        {
            var statusLabel = CreateStatusLabel();

            // ── War Grid ──
            var dgv = CreateGrid(
                "Influence:Faction:6", "Step:Step:4", "NCP:NCP:5", "MSNum:Boss:5", "MSMap:Map:5",
                "StepStart:Step Start:12", "StepEnd:Step End:12",
                "WarStart:War Start:12", "WarEnd:War End:12",
                "Select:Sel:3", "GiveUp:Give:4", "Result:Result:6");

            dgv.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
                var col = dgv.Columns[e.ColumnIndex].Name;
                if (col == "Influence")
                {
                    var val = e.Value?.ToString() ?? "";
                    e.CellStyle.ForeColor = val == "BCU" ? Color.FromArgb(100, 160, 255) : val == "ANI" ? Color.FromArgb(255, 100, 100) : Theme.TextPrimary;
                    e.CellStyle.Font = new Font(Theme.BodyFont, FontStyle.Bold);
                }
                else if (col == "Result")
                {
                    var val = e.Value?.ToString() ?? "";
                    e.CellStyle.ForeColor = val switch
                    {
                        "WIN" => Theme.Success,
                        "LOSS" => Theme.Danger,
                        "In Progress" => Theme.Warning,
                        "Before" => Theme.Accent,
                        _ => Theme.TextMuted
                    };
                }
            };

            // ── Forbid Time Card ──
            var forbidCard = new GlassCard { Dock = DockStyle.Top, Height = 60, AccentColor = Theme.Warning };
            var lblForbid = new Label { Text = "Forbid Time: Loading...", Font = Theme.BodyFont, ForeColor = Theme.TextPrimary, Location = new Point(16, 8), AutoSize = true };
            var btnEditForbid = Theme.CreateButton("Edit Forbid", Theme.Warning, 110, 32);
            btnEditForbid.Location = new Point(16, 30);
            forbidCard.Controls.AddRange(new Control[] { lblForbid, btnEditForbid });

            // ── Action Panel ──
            var actionPanel = new Panel { Dock = DockStyle.Bottom, Height = 100 };

            // Row 1: Step operations
            var btnStepUpdate = Theme.CreateButton("Step Start Update", Theme.Primary, 150, 36);
            btnStepUpdate.Location = new Point(0, 7);
            var btnSelectUpdate = Theme.CreateButton("Select Count", Theme.Accent, 120, 36);
            btnSelectUpdate.Location = new Point(160, 7);
            var btnMSWarStart = Theme.CreateButton("Set War Time", Theme.Warning, 130, 36);
            btnMSWarStart.Location = new Point(290, 7);
            var btnRefresh = Theme.CreateButton("Refresh", Theme.Primary, 100, 36);
            btnRefresh.Location = new Point(430, 7);

            // Row 2: Special operations
            var btnInstantWar = Theme.CreateButton("\u26A1 Instant War", Color.FromArgb(255, 60, 60), 150, 36);
            btnInstantWar.Location = new Point(0, 52);
            var btnReset = Theme.CreateButton("\u26A0 Reset All Wars", Theme.Danger, 160, 36);
            btnReset.Location = new Point(160, 52);

            actionPanel.Controls.AddRange(new Control[] { btnStepUpdate, btnSelectUpdate, btnMSWarStart, btnRefresh, btnInstantWar, btnReset });

            // Layout
            _contentPanel.Controls.Add(dgv);
            _contentPanel.Controls.Add(forbidCard);
            _contentPanel.Controls.Add(actionPanel);
            _contentPanel.Controls.Add(statusLabel);

            // ── State ──
            List<DeclarationOfWarInfo> warList = new();
            DeclarationOfWarForbidTime? forbidTime = null;
            Dictionary<int, string> monsterNames = new();
            Dictionary<int, string> mapNames = new();

            // ── Load data ──
            async Task LoadData()
            {
                statusLabel.Text = "Loading war data...";
                statusLabel.ForeColor = Theme.TextSecondary;

                var (list, info) = await _db.LoadDeclarationOfWarAsync();
                var (ft, ftInfo) = await _db.LoadDeclarationOfWarForbidTimeAsync();
                monsterNames = await _db.LoadMonsterNamesAsync();
                mapNames = await _db.LoadMapNamesAsync();
                warList = list;
                forbidTime = ft;

                statusLabel.Text = info;
                dgv.Rows.Clear();
                foreach (var w in list.OrderBy(w => w.Influence).ThenBy(w => w.MSWarStep))
                {
                    string bossName = w.MSNum == 0 ? "-" : (monsterNames.TryGetValue(w.MSNum, out var mn) ? mn : $"#{w.MSNum}");
                    string mapName = w.MSAppearanceMap == 0 ? "-" : (mapNames.TryGetValue(w.MSAppearanceMap, out var mp) ? mp : $"#{w.MSAppearanceMap}");

                    dgv.Rows.Add(
                        w.InfluenceName,
                        w.MSWarStep == 99 ? "Next" : w.MSWarStep.ToString(),
                        w.NCP,
                        bossName,
                        mapName,
                        w.MSWarStepStartTime?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                        w.MSWarStepEndTime?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                        w.MSWarStartTime?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                        w.MSWarEndTime?.ToString("yyyy-MM-dd HH:mm") ?? "-",
                        w.SelectCount,
                        w.GiveUp ? "Yes" : "No",
                        w.ResultString);
                }

                // Update forbid time display
                if (ft != null)
                    lblForbid.Text = $"Forbid Time: {ft.DayName} {ft.ForbidStartTime?.ToString("HH:mm") ?? "?"} - {ft.ForbidEndTime?.ToString("HH:mm") ?? "?"}";
                else
                    lblForbid.Text = "Forbid Time: Not configured";
            }

            // ── Get selected war entry ──
            DeclarationOfWarInfo? GetSelected()
            {
                if (dgv.SelectedRows.Count == 0) return null;
                var row = dgv.SelectedRows[0];
                var inflStr = row.Cells["Influence"].Value?.ToString() ?? "";
                int infl = inflStr == "BCU" ? 2 : inflStr == "ANI" ? 4 : 0;
                var stepStr = row.Cells["Step"].Value?.ToString() ?? "";
                int step = stepStr == "Next" ? 99 : (int.TryParse(stepStr, out var s) ? s : 0);
                return warList.FirstOrDefault(w => w.Influence == infl && w.MSWarStep == step);
            }

            // ── Step Start Update ──
            btnStepUpdate.Click += async (s, e) =>
            {
                var sel = GetSelected();
                if (sel == null) { statusLabel.Text = "Select a war entry first."; statusLabel.ForeColor = Theme.Warning; return; }

                using var dlg = new Form
                {
                    Text = "Update Step Start Time",
                    Size = new Size(440, 220),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Theme.Background
                };

                var lbl = new Label { Text = $"Set start time for Step {sel.MSWarStep} ({sel.InfluenceName}):\nThis will cascade-update all subsequent steps (+7 days each).", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(20, 16), Size = new Size(400, 50) };
                var dtpDate = new DateTimePicker { Location = new Point(20, 70), Width = 180, Format = DateTimePickerFormat.Short, Value = sel.MSWarStepStartTime ?? DateTime.Now };
                var dtpTime = new DateTimePicker { Location = new Point(210, 70), Width = 140, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = sel.MSWarStepStartTime ?? DateTime.Now };
                var btnOk = Theme.CreateButton("Update", Theme.Primary, 110, 36); btnOk.Location = new Point(20, 120);
                var btnCancel = Theme.CreateButton("Cancel", Theme.Danger, 100, 36); btnCancel.Location = new Point(140, 120);
                dlg.Controls.AddRange(new Control[] { lbl, dtpDate, dtpTime, btnOk, btnCancel });

                bool confirmed = false;
                btnOk.Click += (_, __) => { confirmed = true; dlg.Close(); };
                btnCancel.Click += (_, __) => dlg.Close();
                dlg.ShowDialog();

                if (confirmed)
                {
                    var dt = dtpDate.Value.Date + dtpTime.Value.TimeOfDay;
                    var (ok, info) = await _db.UpdateWarStepStartAsync(sel.MSWarStep, dt);
                    statusLabel.Text = info;
                    statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                    if (ok) { await SendWarReloadToServer(statusLabel); await LoadData(); }
                }
            };

            // ── Select Count Update ──
            btnSelectUpdate.Click += async (s, e) =>
            {
                var sel = GetSelected();
                if (sel == null) { statusLabel.Text = "Select a war entry first."; statusLabel.ForeColor = Theme.Warning; return; }

                using var dlg = new Form
                {
                    Text = "Update Select Count",
                    Size = new Size(380, 180),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Theme.Background
                };

                var lbl = new Label { Text = $"Select count for {sel.InfluenceName} Step {sel.MSWarStep}:", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(20, 16), AutoSize = true };
                var nud = new NumericUpDown { Location = new Point(20, 50), Width = 100, Minimum = 0, Maximum = 5, Value = sel.SelectCount, BackColor = Theme.Surface, ForeColor = Theme.TextPrimary, Font = Theme.BodyFont };
                var btnOk = Theme.CreateButton("Update", Theme.Primary, 110, 36); btnOk.Location = new Point(20, 90);
                var btnCancel = Theme.CreateButton("Cancel", Theme.Danger, 100, 36); btnCancel.Location = new Point(140, 90);
                dlg.Controls.AddRange(new Control[] { lbl, nud, btnOk, btnCancel });

                bool confirmed = false;
                btnOk.Click += (_, __) => { confirmed = true; dlg.Close(); };
                btnCancel.Click += (_, __) => dlg.Close();
                dlg.ShowDialog();

                if (confirmed)
                {
                    var (ok, info) = await _db.UpdateWarSelectCountAsync(sel.Influence, sel.MSWarStep, (int)nud.Value);
                    statusLabel.Text = info;
                    statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                    if (ok) await LoadData();
                }
            };

            // ── Set War Time (MS Start) ──
            btnMSWarStart.Click += async (s, e) =>
            {
                var sel = GetSelected();
                if (sel == null) { statusLabel.Text = "Select a war entry first."; statusLabel.ForeColor = Theme.Warning; return; }

                using var dlg = new Form
                {
                    Text = "Set Mothership War Start Time",
                    Size = new Size(440, 220),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Theme.Background
                };

                var lbl = new Label { Text = $"Set war start time for {sel.InfluenceName} Step {sel.MSWarStep}:\nMust be within the step's period. Requires FieldServer restart.", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(20, 16), Size = new Size(400, 50) };
                var dtpDate = new DateTimePicker { Location = new Point(20, 70), Width = 180, Format = DateTimePickerFormat.Short, Value = sel.MSWarStartTime ?? DateTime.Now };
                var dtpTime = new DateTimePicker { Location = new Point(210, 70), Width = 140, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = sel.MSWarStartTime ?? DateTime.Now };
                var btnOk = Theme.CreateButton("Set Time", Theme.Warning, 110, 36); btnOk.Location = new Point(20, 120);
                var btnCancel = Theme.CreateButton("Cancel", Theme.Danger, 100, 36); btnCancel.Location = new Point(140, 120);
                dlg.Controls.AddRange(new Control[] { lbl, dtpDate, dtpTime, btnOk, btnCancel });

                bool confirmed = false;
                btnOk.Click += (_, __) => { confirmed = true; dlg.Close(); };
                btnCancel.Click += (_, __) => dlg.Close();
                dlg.ShowDialog();

                if (confirmed)
                {
                    var dt = dtpDate.Value.Date + dtpTime.Value.TimeOfDay;
                    // Validate within step period
                    if (sel.MSWarStepStartTime.HasValue && dt < sel.MSWarStepStartTime.Value)
                    {
                        statusLabel.Text = "War time must be after step start time!"; statusLabel.ForeColor = Theme.Danger; return;
                    }
                    if (sel.MSWarStepEndTime.HasValue && dt > sel.MSWarStepEndTime.Value)
                    {
                        statusLabel.Text = "War time must be before step end time!"; statusLabel.ForeColor = Theme.Danger; return;
                    }
                    var (ok, info) = await _db.UpdateMSWarStartTimeAsync(sel.Influence, sel.MSWarStep, dt);
                    statusLabel.Text = info;
                    statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                    if (ok) { await SendWarReloadToServer(statusLabel); await LoadData(); }
                }
            };

            // ── Edit Forbid Time ──
            btnEditForbid.Click += async (s, e) =>
            {
                using var dlg = new Form
                {
                    Text = "Edit Forbid Time",
                    Size = new Size(420, 250),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Theme.Background
                };

                var lblDay = new Label { Text = "Day of Week:", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(20, 20), AutoSize = true };
                var cmbDay = new ComboBox { Location = new Point(160, 16), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.Surface, ForeColor = Theme.TextPrimary, Font = Theme.BodyFont };
                cmbDay.Items.AddRange(new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });
                cmbDay.SelectedIndex = forbidTime?.DayOfWeek ?? 5;

                var lblStart = new Label { Text = "Start Time:", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(20, 60), AutoSize = true };
                var dtpStart = new DateTimePicker { Location = new Point(160, 56), Width = 200, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = forbidTime?.ForbidStartTime ?? DateTime.Today.AddHours(6) };

                var lblEnd = new Label { Text = "End Time:", Font = Theme.BodyFont, ForeColor = Theme.TextSecondary, Location = new Point(20, 100), AutoSize = true };
                var dtpEnd = new DateTimePicker { Location = new Point(160, 96), Width = 200, Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = forbidTime?.ForbidEndTime ?? DateTime.Today.AddHours(12) };

                var btnOk = Theme.CreateButton("Update", Theme.Primary, 110, 36); btnOk.Location = new Point(20, 150);
                var btnCancel = Theme.CreateButton("Cancel", Theme.Danger, 100, 36); btnCancel.Location = new Point(140, 150);
                dlg.Controls.AddRange(new Control[] { lblDay, cmbDay, lblStart, dtpStart, lblEnd, dtpEnd, btnOk, btnCancel });

                bool confirmed = false;
                btnOk.Click += (_, __) => { confirmed = true; dlg.Close(); };
                btnCancel.Click += (_, __) => dlg.Close();
                dlg.ShowDialog();

                if (confirmed)
                {
                    var (ok, info) = await _db.UpdateWarForbidTimeAsync(cmbDay.SelectedIndex, dtpStart.Value, dtpEnd.Value);
                    statusLabel.Text = info;
                    statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                    if (ok) await LoadData();
                }
            };

            // ── Instant War ──
            btnInstantWar.Click += async (s, e) =>
            {
                var sel = GetSelected();
                if (sel == null) { statusLabel.Text = "Select a war entry first."; statusLabel.ForeColor = Theme.Warning; return; }

                using var dlg = new Form
                {
                    Text = "Instant Mothership War",
                    Size = new Size(460, 240),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Theme.Background
                };

                var lbl = new Label
                {
                    Text = $"\u26A1 Start instant war for {sel.InfluenceName} Step {sel.MSWarStep}?\n\n" +
                           "This will set the war start time to NOW + 1 minute.\n" +
                           "The war state will be reset to allow triggering.\n\n" +
                           "A reload command will be sent to the FieldServer\n" +
                           "automatically via PreServer (no restart needed).",
                    Font = Theme.BodyFont,
                    ForeColor = Theme.TextPrimary,
                    Location = new Point(20, 16),
                    Size = new Size(420, 120)
                };

                var cmbInfl = new ComboBox { Location = new Point(20, 140), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Theme.Surface, ForeColor = Theme.TextPrimary, Font = Theme.BodyFont };
                cmbInfl.Items.AddRange(new[] { "BCU", "ANI" });
                cmbInfl.SelectedIndex = sel.Influence == 2 ? 0 : 1;

                var btnStart = Theme.CreateButton("\u26A1 START WAR", Color.FromArgb(255, 60, 60), 140, 38); btnStart.Location = new Point(160, 138);
                var btnCancel = Theme.CreateButton("Cancel", Theme.TextMuted, 100, 38); btnCancel.Location = new Point(310, 138);
                dlg.Controls.AddRange(new Control[] { lbl, cmbInfl, btnStart, btnCancel });

                bool confirmed = false;
                btnStart.Click += (_, __) => { confirmed = true; dlg.Close(); };
                btnCancel.Click += (_, __) => dlg.Close();
                dlg.ShowDialog();

                if (confirmed)
                {
                    int infl = cmbInfl.SelectedIndex == 0 ? 2 : 4;

                    // Pre-check: read current DB state before update
                    string preCheck = "";
                    try
                    {
                        using var preConn = new System.Data.SqlClient.SqlConnection(_db.GetConnectionStringForDiag());
                        await preConn.OpenAsync();
                        using var preCmd = new System.Data.SqlClient.SqlCommand(
                            $"SELECT Influence, MSWarStep, MSNum, MSWarEndState, MSWarStepStartTime, MSWarStartTime FROM td_DeclarationOfWar WHERE MSWarStep = {sel.MSWarStep}", preConn);
                        using var preR = await preCmd.ExecuteReaderAsync();
                        while (await preR.ReadAsync())
                        {
                            preCheck += $"Infl={preR["Influence"]} Step={preR["MSWarStep"]} MSNum={preR["MSNum"]} EndState={preR["MSWarEndState"]} StepStart={preR["MSWarStepStartTime"]} WarStart={preR["MSWarStartTime"]}\n";
                        }
                    }
                    catch (Exception ex) { preCheck = $"PreCheck error: {ex.Message}"; }

                    var (ok, info) = await _db.StartInstantWarAsync(infl, sel.MSWarStep);

                    // Show diagnostic MessageBox so it doesn't disappear
                    MessageBox.Show(
                        $"=== BEFORE UPDATE ===\n{preCheck}\n" +
                        $"=== RESULT ===\n{info}\n\n" +
                        $"Parameters: Influence={infl}, Step={sel.MSWarStep}",
                        ok ? "Instant War - OK" : "Instant War - FAILED",
                        MessageBoxButtons.OK,
                        ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);

                    statusLabel.Text = info;
                    statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                    if (ok)
                    {
                        await SendWarReloadToServer(statusLabel);
                        await LoadData();
                    }
                }
            };

            // ── Reset All ──
            btnReset.Click += async (s, e) =>
            {
                var result = MessageBox.Show(
                    "This will RESET all Declaration of War data!\n\n" +
                    "- Steps 1-5 will be deleted and recreated\n" +
                    "- Step 99 (Next Leader) will become Step 1\n" +
                    "- All war times will be recalculated\n\n" +
                    "FieldServer restart will be required.\n\nAre you sure?",
                    "Reset Declaration of War", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result != DialogResult.Yes) return;

                statusLabel.Text = "Resetting...";
                var (ok, info) = await _db.ResetDeclarationOfWarAsync();
                statusLabel.Text = info;
                statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                if (ok) { await SendWarReloadToServer(statusLabel); await LoadData(); }
            };

            // ── Refresh ──
            btnRefresh.Click += async (s, e) => await LoadData();

            await LoadData();
        }

        // ═══════════════════════════════════════════════
        // SETTINGS
        // ═══════════════════════════════════════════════
        private void LoadSettingsPage()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var card = CreateCard(250);
            card.Size = new Size(550, 250);
            card.Dock = DockStyle.None;
            card.Location = new Point(0, 0);

            int y = 20;
            foreach (var line in new[]
            {
                $"Account DB: {_config.AccountDbServer}:{_config.AccountDbPort}/{_config.AccountDbName}",
                $"Game DB: {_config.GameDbServer}:{_config.GameDbPort}/{_config.GameDbName}",
                $"Logged in as: {_admin.AccountName}",
                $"Role: {_admin.GetRoleString()}",
                $"Account Type: 0x{_admin.AccountType:X4}",
                $"Application: Atum Control Panel v2.0"
            })
            {
                card.Controls.Add(new Label
                {
                    Text = line,
                    Font = Theme.BodyFont,
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(20, y),
                    AutoSize = true
                });
                y += 32;
            }

            panel.Controls.Add(card);
            _contentPanel.Controls.Add(panel);
        }

        // ═══════════════════════════════════════════════
        // UI HELPERS
        // ═══════════════════════════════════════════════
        private static (Panel panel, TextBox txt, Button btn) CreateSearchBar(string placeholder)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 54 };
            var txt = new TextBox
            {
                Width = 320,
                Height = 34,
                Location = new Point(0, 10),
                PlaceholderText = placeholder
            };
            Theme.StyleTextBox(txt);
            var btn = Theme.CreateButton("\u2315  Search", Theme.Primary, 110, 34);
            btn.Location = new Point(330, 10);
            panel.Controls.AddRange(new Control[] { txt, btn });
            return (panel, txt, btn);
        }

        private static Label CreateStatusLabel() => new()
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0)
        };

        private static DataGridView CreateGrid(params string[] columns)
        {
            var dgv = new DataGridView { Dock = DockStyle.Fill };
            Theme.StyleDataGridView(dgv);
            foreach (var col in columns)
            {
                var parts = col.Split(':');
                dgv.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = parts[0],
                    HeaderText = parts[1],
                    FillWeight = parts.Length > 2 ? int.Parse(parts[2]) : 10
                });
            }
            return dgv;
        }

        private static GlassCard CreateCard(int height, Color? accent = null)
        {
            var card = new GlassCard(accent) { Dock = DockStyle.Top, Height = height };
            return card;
        }

        private static ContextMenuStrip CreateContextMenu(DataGridView dgv, params (string text, Func<Task> action)[] items)
        {
            var ctx = new ContextMenuStrip
            {
                BackColor = Theme.CardBg,
                ForeColor = Theme.TextPrimary,
                Font = Theme.BodyFont,
                RenderMode = ToolStripRenderMode.Professional
            };
            ctx.Renderer = new ModernMenuRenderer();
            foreach (var (text, action) in items)
            {
                var item = new ToolStripMenuItem(text) { ForeColor = Theme.TextPrimary, Padding = new Padding(8, 4, 8, 4) };
                item.Click += async (s, e) => await action();
                ctx.Items.Add(item);
            }
            dgv.ContextMenuStrip = ctx;
            return ctx;
        }

        private static string? GetSelectedCell(DataGridView dgv, string colName)
        {
            if (dgv.SelectedRows.Count == 0) return null;
            return dgv.SelectedRows[0].Cells[colName].Value?.ToString();
        }

        private static string? Prompt(string message, string title)
        {
            return Microsoft.VisualBasic.Interaction.InputBox(message, title, "");
        }

        private static void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ═══════════════════════════════════════════════════════════
        //  ANTI-CHEAT & SECURITY PAGE
        // ═══════════════════════════════════════════════════════════
        private async void LoadAntiCheatPage()
        {
            // --- Stat Cards Row ---
            var statsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                Padding = new Padding(10, 10, 10, 0),
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent
            };
            _contentPanel.Controls.Add(statsPanel);

            var cardTotal = new StatCard("Total Blocked", "\u26D4", Theme.Danger) { Value = "...", Width = 200, Height = 90, Margin = new Padding(0, 0, 14, 0) };
            var cardAuto = new StatCard("AutoBlock", "\u2699", Color.FromArgb(255, 100, 50)) { Value = "...", Width = 200, Height = 90, Margin = new Padding(0, 0, 14, 0) };
            var cardSpeed = new StatCard("Speed Hack", "\u26A1", Theme.Warning) { Value = "...", Width = 200, Height = 90, Margin = new Padding(0, 0, 14, 0) };
            var cardMem = new StatCard("Memory Hack", "\u2623", Theme.Accent) { Value = "...", Width = 200, Height = 90, Margin = new Padding(0, 0, 14, 0) };
            var cardManual = new StatCard("Manual Block", "\u270B", Theme.TextMuted) { Value = "...", Width = 200, Height = 90, Margin = new Padding(0, 0, 14, 0) };
            statsPanel.Controls.AddRange(new Control[] { cardTotal, cardAuto, cardSpeed, cardMem, cardManual });

            // --- Action Bar ---
            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(10, 6, 10, 6),
                WrapContents = false,
                BackColor = Color.Transparent
            };
            _contentPanel.Controls.Add(actionPanel);

            var btnBlock = new ModernButton("\u26D4  Block Account", Theme.Danger, 160, 38) { Margin = new Padding(0, 0, 8, 0) };
            var btnUnblock = new ModernButton("\u2714  Unblock", Theme.Success, 130, 38) { Margin = new Padding(0, 0, 8, 0) };
            var btnRefresh = new ModernButton("\u21BB  Refresh", Theme.Primary, 120, 38) { Margin = new Padding(0, 0, 20, 0) };

            var txtSearch = new ModernTextBox("Search account name...") { Width = 250, Height = 38, Margin = new Padding(0, 0, 8, 0) };
            var cmbFilter = new ComboBox
            {
                Width = 180,
                Height = 38,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.CardBg,
                ForeColor = Theme.TextPrimary,
                Font = Theme.BodyFont,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 4, 0, 0)
            };
            cmbFilter.Items.AddRange(new object[] { "All Types", "AutoBlock: SpdHack", "AutoBlock: MemHack",
                "Manual: SpeedHack", "Manual: Normal", "Money Related", "Item Related", "Chat Related", "Game Bug" });
            cmbFilter.SelectedIndex = 0;

            actionPanel.Controls.AddRange(new Control[] { btnBlock, btnUnblock, btnRefresh, txtSearch, cmbFilter });

            // Force correct dock order: stats on top, action below stats, grid fills rest
            statsPanel.BringToFront();
            actionPanel.BringToFront();

            // --- Status Bar (add first, docks at bottom) ---
            var statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                ForeColor = Theme.TextMuted,
                Font = Theme.SmallFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                BackColor = Color.FromArgb(12, 14, 26)
            };
            _contentPanel.Controls.Add(statusLabel);

            // --- Grid (Fill - add before Top panels) ---
            var grid = CreateGrid(
                "AccountName:Account:15",
                "BlockedType:Type:12",
                "StartDate:Blocked Since:12",
                "EndDate:Until:12",
                "Status:Status:8",
                "Admin:Blocked By:10",
                "Reason:Reason (User):15",
                "AdminReason:Reason (Admin):16"
            );
            grid.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(grid);
            grid.BringToFront();

            // --- Context Menu ---
            CreateContextMenu(grid,
                ("Unblock Account", async () =>
                {
                    var acc = GetSelectedCell(grid, "AccountName");
                    if (acc == null) return;
                    if (MessageBox.Show($"Unblock '{acc}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                    var (ok, msg) = await _db.UnblockAccountAsync(acc);
                    statusLabel.Text = msg;
                    statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                    if (ok) await RefreshBlockedGrid();
                }
            ),
                ("Copy Account Name", () =>
                {
                    var acc = GetSelectedCell(grid, "AccountName");
                    if (acc != null) Clipboard.SetText(acc);
                    return Task.CompletedTask;
                }
            )
            );

            // Local data holder
            List<BlockedAccount> allBlocked = new();

            // --- Load & Populate ---
            async Task RefreshBlockedGrid()
            {
                statusLabel.Text = "Loading blocked accounts...";
                statusLabel.ForeColor = Theme.TextMuted;

                var statsTask = _db.GetBlockedAccountStatsAsync();
                var dataTask = _db.LoadBlockedAccountsAsync();
                await Task.WhenAll(statsTask, dataTask);

                var (total, auto, manual, spd, mem) = statsTask.Result;
                cardTotal.Value = total.ToString();
                cardAuto.Value = auto.ToString();
                cardSpeed.Value = spd.ToString();
                cardMem.Value = mem.ToString();
                cardManual.Value = manual.ToString();

                allBlocked = dataTask.Result;
                ApplyFilter();
                statusLabel.Text = $"Loaded {allBlocked.Count} blocked accounts.";
            }

            void ApplyFilter()
            {
                var search = txtSearch.Text.Trim().ToLower();
                var filterIdx = cmbFilter.SelectedIndex;

                var filtered = allBlocked.Where(b =>
                {
                    if (!string.IsNullOrEmpty(search) && !b.AccountName.ToLower().Contains(search))
                        return false;
                    return filterIdx switch
                    {
                        1 => b.BlockedType == 8,   // AutoBlock: SpdHack
                        2 => b.BlockedType == 7,   // AutoBlock: MemHack
                        3 => b.BlockedType == 4,   // Manual: SpeedHack
                        4 => b.BlockedType == 1,   // Manual: Normal
                        5 => b.BlockedType == 2,   // Money
                        6 => b.BlockedType == 3,   // Item
                        7 => b.BlockedType == 5,   // Chat
                        8 => b.BlockedType == 6,   // GameBug
                        _ => true
                    };
                }).ToList();

                grid.Rows.Clear();
                foreach (var b in filtered)
                {
                    int rowIdx = grid.Rows.Add(
                        b.AccountName,
                        b.BlockedTypeName,
                        b.StartDate.ToString("yyyy-MM-dd HH:mm"),
                        b.EndDate.Year >= 2100 ? "Permanent" : b.EndDate.ToString("yyyy-MM-dd HH:mm"),
                        b.StatusText,
                        b.AdminAccountName,
                        b.BlockedReason,
                        b.BlockedReasonForOnlyAdmin
                    );

                    var row = grid.Rows[rowIdx];
                    // Color coding by type
                    Color typeColor = b.BlockedType switch
                    {
                        7 => Color.FromArgb(255, 100, 50),  // MemHack - orange
                        8 => Theme.Warning,                   // SpdHack - yellow
                        _ => Theme.TextSecondary
                    };
                    row.Cells["BlockedType"].Style.ForeColor = typeColor;

                    // Status color
                    if (b.IsExpired)
                        row.Cells["Status"].Style.ForeColor = Theme.TextMuted;
                    else if (b.IsPermanent)
                        row.Cells["Status"].Style.ForeColor = Theme.Danger;
                    else
                        row.Cells["Status"].Style.ForeColor = Theme.Warning;

                    // AutoBlock rows get subtle highlight
                    if (b.IsAutoBlock)
                        row.DefaultCellStyle.BackColor = Color.FromArgb(30, 15, 15);
                }
            }

            // --- Search & Filter Events ---
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            cmbFilter.SelectedIndexChanged += (s, e) => ApplyFilter();

            // --- Button Events ---
            btnRefresh.Click += async (s, e) => await RefreshBlockedGrid();

            btnUnblock.Click += async (s, e) =>
            {
                var acc = GetSelectedCell(grid, "AccountName");
                if (acc == null) { statusLabel.Text = "Select an account to unblock."; return; }
                if (MessageBox.Show($"Unblock '{acc}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                var (ok, msg) = await _db.UnblockAccountAsync(acc);
                statusLabel.Text = msg;
                statusLabel.ForeColor = ok ? Theme.Success : Theme.Danger;
                if (ok) await RefreshBlockedGrid();
            };

            btnBlock.Click += async (s, e) =>
            {
                // --- Block Account Dialog ---
                using var dlg = new Form
                {
                    Text = "Block Account",
                    Size = new Size(480, 520),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Theme.Background,
                    ForeColor = Theme.TextPrimary
                };

                var lbl = (string text, int top) => new Label
                {
                    Text = text,
                    Left = 20,
                    Top = top,
                    Width = 140,
                    Height = 24,
                    ForeColor = Theme.TextSecondary,
                    Font = Theme.SmallFont,
                    TextAlign = ContentAlignment.MiddleRight
                };

                var txtAccount = new ModernTextBox("Account name") { Left = 170, Top = 20, Width = 270, Height = 38 };
                var cmbType = new ComboBox
                {
                    Left = 170,
                    Top = 68,
                    Width = 270,
                    Height = 30,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Theme.CardBg,
                    ForeColor = Theme.TextPrimary,
                    Font = Theme.BodyFont
                };
                cmbType.Items.AddRange(new object[] {
                    "1 - Normal", "2 - Money Related", "3 - Item Related",
                    "4 - SpeedHack", "5 - Chat Related", "6 - Game Bug",
                    "7 - AutoBlock: MemHack", "8 - AutoBlock: SpdHack"
                });
                cmbType.SelectedIndex = 0;

                var chkPermanent = new CheckBox
                {
                    Text = "Permanent Ban",
                    Left = 170,
                    Top = 108,
                    Width = 200,
                    Height = 24,
                    ForeColor = Theme.Danger,
                    Font = Theme.BodyFont,
                    Checked = true
                };

                var dtpEnd = new DateTimePicker
                {
                    Left = 170,
                    Top = 140,
                    Width = 270,
                    Height = 30,
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "yyyy-MM-dd HH:mm",
                    Value = DateTime.Now.AddDays(30),
                    Enabled = false,
                    CalendarForeColor = Theme.TextPrimary
                };
                chkPermanent.CheckedChanged += (cs, ce) => dtpEnd.Enabled = !chkPermanent.Checked;

                var txtReasonUser = new ModernTextBox("Reason shown to player") { Left = 170, Top = 180, Width = 270, Height = 38 };
                var txtReasonAdmin = new ModernTextBox("Admin-only reason (internal)") { Left = 170, Top = 228, Width = 270, Height = 38 };

                var lblWarn = new Label
                {
                    Left = 20,
                    Top = 290,
                    Width = 430,
                    Height = 60,
                    ForeColor = Theme.Warning,
                    Font = Theme.SmallFont,
                    Text = "\u26A0 Warning: Blocking an account will prevent the player from logging in.\nMake sure you have the correct account name before proceeding."
                };

                var btnOk = new ModernButton("Block Account", Theme.Danger, 160, 42) { Left = 170, Top = 370 };
                var btnCancel = new ModernButton("Cancel", Theme.TextMuted, 100, 42) { Left = 340, Top = 370 };

                dlg.Controls.AddRange(new Control[] {
                    lbl("Account:", 28), txtAccount,
                    lbl("Block Type:", 72), cmbType,
                    chkPermanent, lbl("End Date:", 144), dtpEnd,
                    lbl("Player Reason:", 188), txtReasonUser,
                    lbl("Admin Reason:", 236), txtReasonAdmin,
                    lblWarn, btnOk, btnCancel
                });

                btnCancel.Click += (cs, ce) => dlg.DialogResult = DialogResult.Cancel;
                btnOk.Click += async (cs, ce) =>
                {
                    var accName = txtAccount.Text.Trim();
                    if (string.IsNullOrEmpty(accName))
                    {
                        MessageBox.Show("Enter an account name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Verify account exists
                    bool exists = await _db.IsAccountExistsAsync(accName);
                    if (!exists)
                    {
                        if (MessageBox.Show($"Account '{accName}' not found in td_Account.\nBlock anyway?",
                            "Account Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                            return;
                    }

                    int blockType = cmbType.SelectedIndex + 1;
                    DateTime endDate = chkPermanent.Checked ? new DateTime(2200, 1, 1, 1, 1, 1) : dtpEnd.Value;
                    string reasonU = string.IsNullOrEmpty(txtReasonUser.Text.Trim()) ? "Blocked" : txtReasonUser.Text.Trim();
                    string reasonA = txtReasonAdmin.Text.Trim();
                    string admin = "Admin";

                    var (ok2, msg2) = await _db.BlockAccountAsync(accName, blockType, DateTime.Now, endDate, admin, reasonU, reasonA);
                    if (ok2)
                    {
                        dlg.DialogResult = DialogResult.OK;
                    }
                    else
                    {
                        MessageBox.Show(msg2, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                    await RefreshBlockedGrid();
            };

            // --- Initial Load ---
            await RefreshBlockedGrid();
        }

        // ═══════════════════════════════════════════════════════════
        //  AUTO UPDATE PAGE
        // ═══════════════════════════════════════════════════════════
        private void LoadAutoUpdatePage()
        {
            var clientPath = _config.UpdateClientPath;
            var toolExe = _config.UpdateToolExe;
            var versionsCfg = _config.VersionsCfgPath;

            // --- Info Cards ---
            var infoPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                Padding = new Padding(10, 10, 10, 0),
                WrapContents = false,
                AutoScroll = false,
                BackColor = Color.Transparent
            };
            _contentPanel.Controls.Add(infoPanel);

            var cardCurrent = new StatCard("Current Version", "\u2B06", Theme.Primary) { Value = "...", Width = 250, Height = 110, Margin = new Padding(0, 0, 14, 0) };
            var cardFiles = new StatCard("Total Files", "\u25A3", Theme.Accent) { Value = "...", Width = 250, Height = 110, Margin = new Padding(0, 0, 14, 0) };
            var cardSize = new StatCard("Total Size", "\u25CB", Theme.Success) { Value = "...", Width = 350, Height = 110, Margin = new Padding(0, 0, 14, 0) };
            var cardCfgVer = new StatCard("versions.cfg", "\u2699", Theme.Warning) { Value = "...", Width = 450, Height = 110, Margin = new Padding(0, 0, 14, 0) };
            infoPanel.Controls.AddRange(new Control[] { cardCurrent, cardFiles, cardSize, cardCfgVer });

            // --- New Version Input Row ---
            var inputPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(10, 8, 10, 8),
                WrapContents = false,
                BackColor = Color.Transparent
            };
            _contentPanel.Controls.Add(inputPanel);

            var lblNewVer = new Label
            {
                Text = "New Version:",
                Font = Theme.BodyFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = false,
                Size = new Size(100, 36),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 4, 0)
            };
            var txtNewVersion = new ModernTextBox("e.g. 1.0.0.8")
            {
                Width = 180,
                Height = 36,
                Margin = new Padding(0, 0, 12, 0)
            };
            var btnGenerate = new ModernButton("\u2B06  Generate File List", Theme.Primary, 200, 38) { Margin = new Padding(0, 0, 8, 0) };
            var btnApply = new ModernButton("\u2714  Apply versions.cfg", Theme.Success, 200, 38) { Margin = new Padding(0, 0, 8, 0) };
            var btnRefreshInfo = new ModernButton("\u21BB  Refresh", Theme.TextMuted, 110, 38) { Margin = new Padding(0, 0, 8, 0) };
            inputPanel.Controls.AddRange(new Control[] { lblNewVer, txtNewVersion, btnGenerate, btnApply, btnRefreshInfo });

            // --- Log Output ---
            var logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 16, 28),
                ForeColor = Theme.TextSecondary,
                Font = new Font("Consolas", 10f),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10)
            };
            _contentPanel.Controls.Add(logBox);

            // --- Status Bar ---
            var statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                ForeColor = Theme.TextMuted,
                Font = Theme.SmallFont,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                BackColor = Color.FromArgb(14, 16, 28)
            };
            _contentPanel.Controls.Add(statusLabel);

            // ── Helper: append log ──
            void AppendLog(string text, Color? color = null)
            {
                if (logBox.InvokeRequired) { logBox.Invoke(() => AppendLog(text, color)); return; }
                logBox.SelectionStart = logBox.TextLength;
                logBox.SelectionColor = color ?? Theme.TextSecondary;
                logBox.AppendText(text + "\n");
                logBox.ScrollToCaret();
            }

            // ── Helper: read current filelist.dat info ──
            void RefreshInfo()
            {
                var filelistPath = Path.Combine(clientPath, "filelist.dat");
                if (File.Exists(filelistPath))
                {
                    var lines = File.ReadLines(filelistPath).Take(3).ToArray();
                    if (lines.Length >= 3 && lines[0] == "DSUP1")
                    {
                        cardCurrent.Value = lines[1].Replace("Version: ", "");
                        cardFiles.Value = lines[2];
                        txtNewVersion.Text = cardCurrent.Value;
                    }

                    var fi = new FileInfo(filelistPath);
                    long totalBytes = 0;
                    int count = 0;
                    foreach (var line in File.ReadLines(filelistPath).Skip(3))
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 3 && long.TryParse(parts[2], out long sz))
                            totalBytes += sz;
                        count++;
                    }
                    double totalMB = totalBytes / (1024.0 * 1024.0);
                    cardSize.Value = $"{totalMB:F1} MB";
                }
                else
                {
                    cardCurrent.Value = "N/A";
                    cardFiles.Value = "0";
                    cardSize.Value = "0 MB";
                }

                // Read versions.cfg
                if (File.Exists(versionsCfg))
                {
                    foreach (var line in File.ReadAllLines(versionsCfg))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("ClientVersion"))
                        {
                            var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3)
                                cardCfgVer.Value = $"{parts[1]} \u2192 {parts[2]}";
                            break;
                        }
                    }
                }
                else
                {
                    cardCfgVer.Value = "Not found";
                }
            }

            // ── Generate File List ──
            btnGenerate.Click += async (s, e) =>
            {
                var newVer = txtNewVersion.Text.Trim();
                if (string.IsNullOrEmpty(newVer) || newVer.Split('.').Length != 4)
                {
                    statusLabel.Text = "Invalid version format. Use x.x.x.x";
                    statusLabel.ForeColor = Theme.Danger;
                    return;
                }

                if (!File.Exists(toolExe))
                {
                    AppendLog($"[ERROR] DarksideUpdateTool not found: {toolExe}", Theme.Danger);
                    return;
                }

                btnGenerate.Enabled = false;
                statusLabel.Text = "Generating file list...";
                statusLabel.ForeColor = Theme.Accent;
                AppendLog($"[{DateTime.Now:HH:mm:ss}] Generating filelist.dat (v{newVer})...", Theme.Accent);

                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = toolExe,
                        Arguments = $". {newVer}",
                        WorkingDirectory = clientPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var proc = System.Diagnostics.Process.Start(psi)!;
                    var output = await proc.StandardOutput.ReadToEndAsync();
                    var error = await proc.StandardError.ReadToEndAsync();
                    await proc.WaitForExitAsync();

                    if (!string.IsNullOrEmpty(output))
                    {
                        foreach (var line in output.Split('\n').TakeLast(8))
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                AppendLog("  " + line.TrimEnd(), Theme.TextSecondary);
                        }
                    }
                    if (!string.IsNullOrEmpty(error))
                        AppendLog($"  [STDERR] {error.TrimEnd()}", Theme.Warning);

                    if (proc.ExitCode == 0)
                    {
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] filelist.dat generated successfully!", Theme.Success);
                        statusLabel.Text = $"filelist.dat generated (v{newVer}). Don't forget to Apply versions.cfg!";
                        statusLabel.ForeColor = Theme.Success;
                    }
                    else
                    {
                        AppendLog($"[{DateTime.Now:HH:mm:ss}] DarksideUpdateTool exited with code {proc.ExitCode}", Theme.Danger);
                        statusLabel.Text = "Error generating file list.";
                        statusLabel.ForeColor = Theme.Danger;
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"[ERROR] {ex.Message}", Theme.Danger);
                    statusLabel.ForeColor = Theme.Danger;
                }
                finally
                {
                    btnGenerate.Enabled = true;
                    RefreshInfo();
                }
            };

            // ── Apply versions.cfg ──
            btnApply.Click += (s, e) =>
            {
                var newVer = txtNewVersion.Text.Trim();
                if (string.IsNullOrEmpty(newVer) || newVer.Split('.').Length != 4)
                {
                    statusLabel.Text = "Invalid version format.";
                    statusLabel.ForeColor = Theme.Danger;
                    return;
                }

                try
                {
                    // Read existing versions.cfg, update ClientVersion line
                    var lines = File.Exists(versionsCfg) ? File.ReadAllLines(versionsCfg).ToList() : new List<string>();
                    bool found = false;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].TrimStart().StartsWith("ClientVersion"))
                        {
                            lines[i] = $"ClientVersion\t\t\t1.0.0.0\t{newVer}";
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        lines.Insert(0, $"ClientVersion\t\t\t1.0.0.0\t{newVer}");

                    File.WriteAllLines(versionsCfg, lines);

                    AppendLog($"[{DateTime.Now:HH:mm:ss}] versions.cfg updated: 1.0.0.0 -> {newVer}", Theme.Success);
                    AppendLog($"  \u26A0 PreServer restart required for changes to take effect!", Theme.Warning);
                    statusLabel.Text = $"versions.cfg updated. Restart PreServer!";
                    statusLabel.ForeColor = Theme.Warning;
                    RefreshInfo();
                }
                catch (Exception ex)
                {
                    AppendLog($"[ERROR] {ex.Message}", Theme.Danger);
                    statusLabel.ForeColor = Theme.Danger;
                }
            };

            // ── Refresh ──
            btnRefreshInfo.Click += (s, e) =>
            {
                RefreshInfo();
                statusLabel.Text = "Info refreshed.";
                statusLabel.ForeColor = Theme.TextMuted;
            };

            // ── Initial Load ──
            RefreshInfo();
            AppendLog($"[{DateTime.Now:HH:mm:ss}] Auto Update page loaded.", Theme.TextMuted);
            AppendLog($"  Client path: {clientPath}", Theme.TextMuted);
            AppendLog($"  Update tool: {toolExe}", Theme.TextMuted);
            AppendLog($"  versions.cfg: {versionsCfg}", Theme.TextMuted);
            AppendLog("", Theme.TextMuted);
            AppendLog("Workflow:", Theme.TextPrimary);
            AppendLog("  1. Copy updated files to client folder", Theme.TextSecondary);
            AppendLog("  2. Enter new version number", Theme.TextSecondary);
            AppendLog("  3. Click 'Generate File List'", Theme.TextSecondary);
            AppendLog("  4. Click 'Apply versions.cfg'", Theme.TextSecondary);
            AppendLog("  5. Restart PreServer", Theme.TextSecondary);
        }
    }
}
