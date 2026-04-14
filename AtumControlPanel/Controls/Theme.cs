using System.Drawing.Drawing2D;

namespace AtumControlPanel.Controls
{
    public static class Theme
    {
        // ── Color Palette ─────────────────────────────
        // Deep space dark with electric accents
        public static readonly Color Background = Color.FromArgb(10, 12, 22);
        public static readonly Color SidebarBg = Color.FromArgb(14, 17, 30);
        public static readonly Color CardBg = Color.FromArgb(18, 22, 40);
        public static readonly Color CardBgHover = Color.FromArgb(24, 30, 52);
        public static readonly Color Surface = Color.FromArgb(22, 27, 48);
        public static readonly Color SurfaceLight = Color.FromArgb(28, 34, 58);
        public static readonly Color Border = Color.FromArgb(40, 50, 80);
        public static readonly Color BorderLight = Color.FromArgb(55, 65, 100);

        public static readonly Color Primary = Color.FromArgb(88, 115, 254);
        public static readonly Color PrimaryDark = Color.FromArgb(65, 88, 220);
        public static readonly Color PrimaryGlow = Color.FromArgb(30, 88, 115, 254);
        public static readonly Color Accent = Color.FromArgb(0, 200, 255);
        public static readonly Color AccentGlow = Color.FromArgb(25, 0, 200, 255);
        public static readonly Color Success = Color.FromArgb(0, 220, 130);
        public static readonly Color SuccessGlow = Color.FromArgb(25, 0, 220, 130);
        public static readonly Color Warning = Color.FromArgb(255, 180, 0);
        public static readonly Color Danger = Color.FromArgb(255, 65, 80);

        public static readonly Color TextPrimary = Color.FromArgb(230, 235, 255);
        public static readonly Color TextSecondary = Color.FromArgb(130, 148, 190);
        public static readonly Color TextMuted = Color.FromArgb(70, 85, 120);

        // Gradient pairs
        public static readonly Color GradientStart = Color.FromArgb(88, 115, 254);
        public static readonly Color GradientEnd = Color.FromArgb(0, 200, 255);

        // ── Typography ────────────────────────────────
        public static readonly Font TitleFont = new("Segoe UI", 18f, FontStyle.Bold);
        public static readonly Font SubtitleFont = new("Segoe UI", 13f, FontStyle.Bold);
        public static readonly Font BodyFont = new("Segoe UI", 10f);
        public static readonly Font BodyFontBold = new("Segoe UI", 10f, FontStyle.Bold);
        public static readonly Font SmallFont = new("Segoe UI", 9f);
        public static readonly Font SmallFontBold = new("Segoe UI", 9f, FontStyle.Bold);
        public static readonly Font StatFont = new("Segoe UI", 28f, FontStyle.Bold);
        public static readonly Font MenuFont = new("Segoe UI", 10.5f);
        public static readonly Font MenuFontBold = new("Segoe UI", 10.5f, FontStyle.Bold);

        // ── Dimensions ────────────────────────────────
        public const int BorderRadius = 8;
        public const int BorderRadiusSmall = 5;

        // ── Drawing Helpers ───────────────────────────
        public static GraphicsPath RoundedRect(RectangleF rect, int radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            if (d > rect.Height) d = rect.Height;
            if (d > rect.Width) d = rect.Width;
            var arc = new RectangleF(rect.Location, new SizeF(d, d));
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - d;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - d;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawGlowShadow(Graphics g, Rectangle rect, Color color, int radius, int spread = 6)
        {
            for (int i = spread; i > 0; i--)
            {
                int alpha = (int)(((float)(spread - i) / spread) * 30);
                using var pen = new Pen(Color.FromArgb(alpha, color), 1);
                var r = new Rectangle(rect.X - i, rect.Y - i, rect.Width + i * 2, rect.Height + i * 2);
                using var path = RoundedRect(r, radius + i);
                g.DrawPath(pen, path);
            }
        }

        public static LinearGradientBrush CreateGradient(Rectangle rect, Color c1, Color c2, float angle = 135f)
        {
            if (rect.Width <= 0) rect.Width = 1;
            if (rect.Height <= 0) rect.Height = 1;
            return new LinearGradientBrush(rect, c1, c2, angle);
        }

        // ── Control Styling ───────────────────────────
        public static void StyleDataGridView(DataGridView dgv)
        {
            dgv.BackgroundColor = CardBg;
            dgv.GridColor = Color.FromArgb(30, 38, 65);
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.EnableHeadersVisualStyles = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.RowHeadersVisible = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.ReadOnly = true;
            dgv.DoubleBuffered(true);

            dgv.DefaultCellStyle.BackColor = CardBg;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(40, Primary.R, Primary.G, Primary.B);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.DefaultCellStyle.Font = BodyFont;
            dgv.DefaultCellStyle.Padding = new Padding(10, 5, 10, 5);

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(21, 26, 46);

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(14, 17, 32);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Accent;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 8, 10, 8);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersHeight = 42;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            dgv.RowTemplate.Height = 38;

            // Row hover effect
            dgv.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(28, 35, 60);
            };
            dgv.CellMouseLeave += (s, e) =>
            {
                if (e.RowIndex >= 0)
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Empty;
            };
        }

        public static void StyleTextBox(TextBox tb)
        {
            tb.BackColor = Surface;
            tb.ForeColor = TextPrimary;
            tb.BorderStyle = BorderStyle.FixedSingle;
            tb.Font = BodyFont;
        }

        public static Button CreateButton(string text, Color bgColor, int width = 120, int height = 36)
        {
            var btn = new ModernButton(text, bgColor, width, height);
            return btn;
        }
    }

    // ══════════════════════════════════════════════
    // MODERN BUTTON - Rounded, gradient, glow
    // ══════════════════════════════════════════════
    public class ModernButton : Button
    {
        private Color _baseColor;
        private bool _hovering;
        private bool _pressing;
        private float _hoverAnim = 0f;
        private readonly System.Windows.Forms.Timer _animTimer;

        public ModernButton(string text, Color bgColor, int width, int height)
        {
            _baseColor = bgColor;
            Text = text;
            Size = new Size(width, height);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (s, e) =>
            {
                float target = _hovering ? 1f : 0f;
                _hoverAnim += (target - _hoverAnim) * 0.2f;
                if (Math.Abs(_hoverAnim - target) < 0.01f)
                {
                    _hoverAnim = target;
                    _animTimer.Stop();
                }
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e) { _hovering = true; _animTimer.Start(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovering = false; _pressing = false; _animTimer.Start(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs mevent) { _pressing = true; Invalidate(); base.OnMouseDown(mevent); }
        protected override void OnMouseUp(MouseEventArgs mevent) { _pressing = false; Invalidate(); base.OnMouseUp(mevent); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent?.BackColor ?? Theme.Background);

            var rect = new Rectangle(1, 1, Width - 3, Height - 3);
            int radius = Theme.BorderRadiusSmall;

            // Glow on hover
            if (_hoverAnim > 0.05f)
            {
                int glowSpread = (int)(4 * _hoverAnim);
                Theme.DrawGlowShadow(g, rect, _baseColor, radius, glowSpread);
            }

            // Button fill - slightly lighter on hover
            var fillColor = _pressing
                ? ControlPaint.Dark(_baseColor, 0.1f)
                : Color.FromArgb(
                    Math.Min(255, _baseColor.R + (int)(20 * _hoverAnim)),
                    Math.Min(255, _baseColor.G + (int)(20 * _hoverAnim)),
                    Math.Min(255, _baseColor.B + (int)(20 * _hoverAnim)));

            using var path = Theme.RoundedRect(rect, radius);
            using (var brush = new SolidBrush(fillColor))
                g.FillPath(brush, path);

            // Subtle top highlight
            if (_hoverAnim > 0.1f)
            {
                var highlightRect = new Rectangle(rect.X + 2, rect.Y, rect.Width - 4, rect.Height / 2);
                using var highlightPath = Theme.RoundedRect(highlightRect, radius);
                using var highlightBrush = new LinearGradientBrush(highlightRect,
                    Color.FromArgb((int)(25 * _hoverAnim), 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255), 90f);
                g.FillPath(highlightBrush, highlightPath);
            }

            // Text
            var textRect = new Rectangle(0, _pressing ? 1 : 0, Width, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, Enabled ? ForeColor : Theme.TextMuted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ══════════════════════════════════════════════
    // MODERN TEXT INPUT - Underline style with focus animation
    // ══════════════════════════════════════════════
    public class ModernTextBox : Panel
    {
        private readonly TextBox _innerBox;
        private readonly Label _placeholderLabel;
        private bool _focused;
        private float _focusAnim = 0f;
        private readonly System.Windows.Forms.Timer _animTimer;

        public new string Text
        {
            get => _innerBox.Text;
            set => _innerBox.Text = value;
        }

        public bool UseSystemPasswordChar
        {
            get => _innerBox.UseSystemPasswordChar;
            set => _innerBox.UseSystemPasswordChar = value;
        }

        public new event EventHandler? TextChanged
        {
            add => _innerBox.TextChanged += value;
            remove => _innerBox.TextChanged -= value;
        }

        public ModernTextBox(string placeholder = "", string defaultValue = "")
        {
            Height = 44;
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _innerBox = new TextBox
            {
                Text = defaultValue,
                Font = Theme.BodyFont,
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                BorderStyle = BorderStyle.None,
                Location = new Point(12, 14),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            _innerBox.GotFocus += (s, e) => { _focused = true; _animTimer.Start(); };
            _innerBox.LostFocus += (s, e) => { _focused = false; _animTimer.Start(); };
            _innerBox.TextChanged += (s, e) => { _placeholderLabel.Visible = string.IsNullOrEmpty(_innerBox.Text); };

            _placeholderLabel = new Label
            {
                Text = placeholder,
                Font = Theme.BodyFont,
                ForeColor = Theme.TextMuted,
                BackColor = Color.Transparent,
                AutoSize = false,
                Location = new Point(12, 14),
                Size = new Size(200, 20),
                Visible = string.IsNullOrEmpty(defaultValue),
                Cursor = Cursors.IBeam
            };
            _placeholderLabel.Click += (s, e) => _innerBox.Focus();

            Controls.Add(_innerBox);
            Controls.Add(_placeholderLabel);

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (s, e) =>
            {
                float target = _focused ? 1f : 0f;
                _focusAnim += (target - _focusAnim) * 0.25f;
                if (Math.Abs(_focusAnim - target) < 0.01f) { _focusAnim = target; _animTimer.Stop(); }
                Invalidate();
            };
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_innerBox != null)
                _innerBox.Width = Width - 24;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.Background);

            var rect = new RectangleF(0, 0, Width - 1, Height - 1);

            // Background
            using var bgPath = Theme.RoundedRect(rect, Theme.BorderRadiusSmall);
            using (var bgBrush = new SolidBrush(Theme.Surface))
                g.FillPath(bgBrush, bgPath);

            // Border - animate color on focus
            var borderColor = _focusAnim > 0.05f
                ? Color.FromArgb(
                    (int)(Theme.Border.R + (Theme.Primary.R - Theme.Border.R) * _focusAnim),
                    (int)(Theme.Border.G + (Theme.Primary.G - Theme.Border.G) * _focusAnim),
                    (int)(Theme.Border.B + (Theme.Primary.B - Theme.Border.B) * _focusAnim))
                : Theme.Border;
            using (var pen = new Pen(borderColor, _focused ? 1.5f : 1f))
                g.DrawPath(pen, bgPath);

            // Bottom accent line on focus
            if (_focusAnim > 0.05f)
            {
                float lineWidth = (Width - 4) * _focusAnim;
                float lineX = (Width - lineWidth) / 2f;
                using var lineBrush = Theme.CreateGradient(
                    new Rectangle(0, 0, Width, 3), Theme.Primary, Theme.Accent);
                using var linePen = new Pen(lineBrush, 2f);
                g.DrawLine(linePen, lineX, Height - 2, lineX + lineWidth, Height - 2);
            }
        }
    }

    // ══════════════════════════════════════════════
    // SIDEBAR BUTTON - Premium hover/active effects
    // ══════════════════════════════════════════════
    public class SidebarButton : Panel
    {
        private bool _isActive;
        private bool _hovering;
        private float _hoverAnim = 0f;
        private float _activeAnim = 0f;
        private readonly Label _iconLabel;
        private readonly Label _textLabel;
        private readonly System.Windows.Forms.Timer _animTimer;

        public string MenuText { get => _textLabel.Text; set => _textLabel.Text = value; }
        public string Icon { get => _iconLabel.Text; set => _iconLabel.Text = value; }
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; _animTimer.Start(); UpdateVisual(); }
        }

        public SidebarButton(string icon, string text)
        {
            Height = 44;
            Dock = DockStyle.Top;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 13f),
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(8, 0),
                BackColor = Color.Transparent
            };

            _textLabel = new Label
            {
                Text = text,
                Font = Theme.MenuFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = false,
                Size = new Size(180, 44),
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(52, 0),
                BackColor = Color.Transparent
            };

            Controls.Add(_iconLabel);
            Controls.Add(_textLabel);

            foreach (Control c in Controls)
            {
                c.Click += (s, e) => OnClick(e);
                c.MouseEnter += (s, e) => OnMouseEnter(e);
                c.MouseLeave += (s, e) => OnMouseLeave(e);
            }

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (s, e) =>
            {
                float hoverTarget = _hovering || _isActive ? 1f : 0f;
                float activeTarget = _isActive ? 1f : 0f;
                _hoverAnim += (hoverTarget - _hoverAnim) * 0.2f;
                _activeAnim += (activeTarget - _activeAnim) * 0.15f;
                if (Math.Abs(_hoverAnim - hoverTarget) < 0.01f && Math.Abs(_activeAnim - activeTarget) < 0.01f)
                {
                    _hoverAnim = hoverTarget;
                    _activeAnim = activeTarget;
                    _animTimer.Stop();
                }
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e) { _hovering = true; _animTimer.Start(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovering = false; _animTimer.Start(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.SidebarBg);

            // Hover/active background
            if (_hoverAnim > 0.02f)
            {
                int alpha = (int)((_isActive ? 20 : 12) * _hoverAnim);
                var rect = new RectangleF(4, 2, Width - 8, Height - 4);
                using var path = Theme.RoundedRect(rect, 6);
                using var brush = new SolidBrush(Color.FromArgb(alpha, Theme.Primary));
                g.FillPath(brush, path);
            }

            // Active indicator - gradient line on left
            if (_activeAnim > 0.02f)
            {
                float indicatorHeight = (Height - 16) * _activeAnim;
                float indicatorY = (Height - indicatorHeight) / 2f;
                var indicatorRect = new RectangleF(0, indicatorY, 3, indicatorHeight);
                if (indicatorRect.Height > 0)
                {
                    using var brush = Theme.CreateGradient(
                        Rectangle.Round(indicatorRect), Theme.Primary, Theme.Accent, 90f);
                    using var path = Theme.RoundedRect(indicatorRect, 2);
                    g.FillPath(brush, path);
                }
            }
        }

        private void UpdateVisual()
        {
            if (_isActive)
            {
                _iconLabel.ForeColor = Theme.Primary;
                _textLabel.ForeColor = Theme.TextPrimary;
                _textLabel.Font = Theme.MenuFontBold;
            }
            else
            {
                _iconLabel.ForeColor = Theme.TextMuted;
                _textLabel.ForeColor = Theme.TextSecondary;
                _textLabel.Font = Theme.MenuFont;
            }
        }
    }

    // ══════════════════════════════════════════════
    // STAT CARD - Glassmorphism with gradient accent
    // ══════════════════════════════════════════════
    public class StatCard : Panel
    {
        private readonly Label _valueLabel;
        private readonly Label _titleLabel;
        private readonly Label _iconLabel;
        private readonly Color _accentColor;
        private bool _hovering;
        private float _hoverAnim = 0f;
        private readonly System.Windows.Forms.Timer _animTimer;

        public string Title { get => _titleLabel.Text; set => _titleLabel.Text = value; }
        public string Value { get => _valueLabel.Text; set => _valueLabel.Text = value; }

        public StatCard(string title, string icon, Color accentColor)
        {
            _accentColor = accentColor;
            Size = new Size(220, 110);
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _iconLabel = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 22f),
                ForeColor = accentColor,
                Location = new Point(18, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _valueLabel = new Label
            {
                Text = "0",
                Font = Theme.StatFont,
                ForeColor = Theme.TextPrimary,
                Location = new Point(62, 14),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _titleLabel = new Label
            {
                Text = title,
                Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(18, 78),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            Controls.Add(_iconLabel);
            Controls.Add(_valueLabel);
            Controls.Add(_titleLabel);

            foreach (Control c in Controls)
            {
                c.MouseEnter += (s, e) => OnMouseEnter(e);
                c.MouseLeave += (s, e) => OnMouseLeave(e);
            }

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (s, e) =>
            {
                float target = _hovering ? 1f : 0f;
                _hoverAnim += (target - _hoverAnim) * 0.2f;
                if (Math.Abs(_hoverAnim - target) < 0.01f) { _hoverAnim = target; _animTimer.Stop(); }
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e) { _hovering = true; _animTimer.Start(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovering = false; _animTimer.Start(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.Background);

            var rect = new RectangleF(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundedRect(rect, Theme.BorderRadius);

            // Hover glow
            if (_hoverAnim > 0.05f)
                Theme.DrawGlowShadow(g, Rectangle.Round(rect), _accentColor, Theme.BorderRadius, (int)(5 * _hoverAnim));

            // Card background
            using (var bgBrush = new SolidBrush(Theme.CardBg))
                g.FillPath(bgBrush, path);

            // Border
            using (var borderPen = new Pen(Color.FromArgb(30 + (int)(20 * _hoverAnim), Theme.Border), 1f))
                g.DrawPath(borderPen, path);

            // Top gradient accent line
            var lineRect = new RectangleF(1, 1, Width - 3, 3);
            using var linePath = Theme.RoundedRect(new RectangleF(8, 0, Width - 16, 3), 2);
            var lighterAccent = Color.FromArgb(
                Math.Min(255, _accentColor.R + 60),
                Math.Min(255, _accentColor.G + 60),
                Math.Min(255, _accentColor.B + 60));
            using var lineBrush = Theme.CreateGradient(
                new Rectangle(0, 0, Width, 3), _accentColor, lighterAccent, 0f);
            g.FillPath(lineBrush, linePath);
        }
    }

    // ══════════════════════════════════════════════
    // GLASS CARD - Semi-transparent card with glow border
    // ══════════════════════════════════════════════
    public class GlassCard : Panel
    {
        private Color _accentColor;

        public Color AccentColor
        {
            get => _accentColor;
            set { _accentColor = value; Invalidate(); }
        }

        public GlassCard(Color? accent = null)
        {
            _accentColor = accent ?? Theme.Primary;
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            Padding = new Padding(16, 20, 16, 16);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.Background);

            var rect = new RectangleF(1, 1, Width - 3, Height - 3);
            using var path = Theme.RoundedRect(rect, Theme.BorderRadius);

            // Card background
            using (var bgBrush = new SolidBrush(Theme.CardBg))
                g.FillPath(bgBrush, path);

            // Border
            using (var borderPen = new Pen(Color.FromArgb(40, Theme.BorderLight), 1f))
                g.DrawPath(borderPen, path);

            // Top gradient accent
            var lineRect = new RectangleF(12, 1, Width - 24, 2.5f);
            using var linePath = Theme.RoundedRect(lineRect, 2);
            var lighterAccent = Color.FromArgb(
                Math.Min(255, _accentColor.R + 80),
                Math.Min(255, _accentColor.G + 80),
                Math.Min(255, _accentColor.B + 80));
            using var lineBrush = Theme.CreateGradient(
                Rectangle.Round(lineRect), _accentColor, lighterAccent, 0f);
            g.FillPath(lineBrush, linePath);
        }
    }

    // ══════════════════════════════════════════════
    // GRADIENT HEADER BAR
    // ══════════════════════════════════════════════
    public class GradientHeader : Panel
    {
        public GradientHeader()
        {
            Height = 56;
            Dock = DockStyle.Top;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Very subtle gradient background
            using var bgBrush = new LinearGradientBrush(
                ClientRectangle.Width > 0 && ClientRectangle.Height > 0 ? ClientRectangle : new Rectangle(0, 0, 1, 1),
                Theme.Background,
                Color.FromArgb(14, 17, 28), 0f);
            g.FillRectangle(bgBrush, ClientRectangle);

            // Bottom line with gradient
            var lineRect = new Rectangle(0, Height - 1, Width, 1);
            using var lineBrush = Theme.CreateGradient(lineRect, Theme.Primary, Color.FromArgb(10, Theme.Primary), 0f);
            g.FillRectangle(lineBrush, lineRect);
        }
    }

    // ══════════════════════════════════════════════
    // MODERN BADGE LABEL
    // ══════════════════════════════════════════════
    public class BadgeLabel : Label
    {
        private Color _badgeColor;
        public Color BadgeColor { get => _badgeColor; set { _badgeColor = value; Invalidate(); } }

        public BadgeLabel(string text, Color color)
        {
            _badgeColor = color;
            Text = text;
            Font = Theme.SmallFontBold;
            ForeColor = color;
            AutoSize = false;
            TextAlign = ContentAlignment.MiddleCenter;
            Size = new Size(70, 24);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent?.BackColor ?? Theme.CardBg);

            var rect = new RectangleF(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundedRect(rect, Height / 2);
            using (var bgBrush = new SolidBrush(Color.FromArgb(25, _badgeColor)))
                g.FillPath(bgBrush, path);
            using (var borderPen = new Pen(Color.FromArgb(60, _badgeColor), 1f))
                g.DrawPath(borderPen, path);

            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ══════════════════════════════════════════════
    // DOUBLE BUFFERED PANEL
    // ══════════════════════════════════════════════
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Just fill with back color - no custom painting needed for content panel
            e.Graphics.Clear(BackColor);
            base.OnPaint(e);
        }
    }

    // ══════════════════════════════════════════════
    // MODERN CONTEXT MENU RENDERER
    // ══════════════════════════════════════════════
    public class ModernMenuRenderer : ToolStripProfessionalRenderer
    {
        public ModernMenuRenderer() : base(new ModernMenuColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (e.Item.Selected)
            {
                var rect = new Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
                using var path = Theme.RoundedRect(rect, 4);
                using var brush = new SolidBrush(Color.FromArgb(25, Theme.Primary));
                g.FillPath(brush, path);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Theme.CardBg);
            g.FillRectangle(brush, e.AffectedBounds);
            using var pen = new Pen(Color.FromArgb(40, Theme.Primary), 1);
            g.DrawRectangle(pen, 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { /* no border */ }
    }

    public class ModernMenuColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => Color.FromArgb(40, Theme.Primary);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.FromArgb(25, Theme.Primary);
        public override Color MenuStripGradientBegin => Theme.CardBg;
        public override Color MenuStripGradientEnd => Theme.CardBg;
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(25, Theme.Primary);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(25, Theme.Primary);
        public override Color ImageMarginGradientBegin => Theme.CardBg;
        public override Color ImageMarginGradientEnd => Theme.CardBg;
        public override Color ImageMarginGradientMiddle => Theme.CardBg;
        public override Color SeparatorDark => Theme.Border;
        public override Color SeparatorLight => Theme.Border;
    }

    // ══════════════════════════════════════════════
    // EXTENSION: Enable DoubleBuffered on DataGridView
    // ══════════════════════════════════════════════
    public static class ControlExtensions
    {
        public static void DoubleBuffered(this DataGridView dgv, bool setting)
        {
            var type = dgv.GetType();
            var prop = type.GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(dgv, setting, null);
        }
    }
}
