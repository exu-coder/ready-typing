using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReadyTypingSetup;

/// <summary>
/// The borderless Ready Typing setup window. It walks the user through a
/// welcome page, an install progress page and a success page, all drawn with
/// the same visual language as the trainer itself.
/// </summary>
internal sealed class InstallerForm : Form
{
    private static readonly Color Ink = Color.FromArgb(30, 27, 75);
    private static readonly Color Muted = Color.FromArgb(99, 96, 133);
    private static readonly Color Canvas = Color.FromArgb(238, 240, 249);
    private static readonly Color Teal = Color.FromArgb(79, 70, 229);
    private static readonly Color TealDark = Color.FromArgb(67, 56, 202);
    private static readonly Color Line = Color.FromArgb(226, 228, 240);

    private readonly Panel _titleBar;
    private readonly Panel _content;
    private readonly Panel _welcomePage;
    private readonly Panel _progressPage;
    private readonly Panel _donePage;

    private readonly TextBox _txtPath;
    private readonly CheckBox _chkDesktop;
    private readonly CheckBox _chkStartMenu;
    private readonly CheckBox _chkLaunch;
    private readonly FlatButton _btnInstall;
    private readonly FlatButton _btnExit;
    private readonly FlatButton _btnBrowse;
    private readonly FlatButton _btnCancel;
    private readonly FlatButton _btnOpen;
    private readonly FlatButton _btnClose;

    private readonly ProgressBarEx _progress;
    private readonly Label _lblStatus;
    private readonly Label _lblPercent;
    private readonly Label _lblDonePath;

    private Image? _logo;
    private Icon? _windowIcon;
    private CancellationTokenSource? _cancellation;
    private string? _installedPath;
    private bool _installing;
    private bool _dragging;
    private Point _dragOffset;

    public InstallerForm()
    {
        Text = "Ready Typing Setup";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(780, 590);
        MinimumSize = new Size(780, 590);
        BackColor = Canvas;
        Font = new Font("Times New Roman", 9.5f);
        DoubleBuffered = true;
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96f, 96f);
        MaximizeBox = false;
        MinimizeBox = false;

        LoadBrandAssets();

        _titleBar = BuildTitleBar(out _btnClose);
        _content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Canvas
        };

        _welcomePage = BuildWelcomePage(
            out _txtPath,
            out _chkDesktop,
            out _chkStartMenu,
            out _chkLaunch,
            out _btnInstall,
            out _btnExit,
            out _btnBrowse);

        _progressPage = BuildProgressPage(
            out _progress,
            out _lblStatus,
            out _lblPercent,
            out _btnCancel);

        _donePage = BuildDonePage(out _lblDonePath, out _btnOpen);

        _content.Controls.Add(_welcomePage);
        _content.Controls.Add(_progressPage);
        _content.Controls.Add(_donePage);

        Controls.Add(_content);
        Controls.Add(_titleBar);

        _btnBrowse.Click += BtnBrowse_Click;
        _btnInstall.Click += BtnInstall_Click;
        _btnExit.Click += (_, _) => Close();
        _btnClose.Click += (_, _) => Close();
        _btnCancel.Click += BtnCancel_Click;
        _btnOpen.Click += BtnOpen_Click;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape && !_installing)
            {
                Close();
            }
        };

        FormClosing += InstallerForm_FormClosing;

        Region = new Region(RoundPanel.RoundedPath(
            new Rectangle(0, 0, Width, Height),
            14));

        ShowPage(_welcomePage);
        ActiveControl = _btnInstall;
    }

    /// <summary>Switches the visible page and resets keyboard focus.</summary>
    private void ShowPage(Panel page)
    {
        foreach (Control child in _content.Controls)
        {
            child.Visible = ReferenceEquals(child, page);
        }

        page.Visible = true;
        page.BringToFront();
        ActiveControl = null;
    }

    private Panel BuildTitleBar(out FlatButton closeButton)
    {
        var bar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = Teal
        };

        bar.Paint += (_, e) =>
        {
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var gradient = new LinearGradientBrush(
                bar.ClientRectangle,
                Color.FromArgb(79, 70, 229),
                Color.FromArgb(6, 182, 212),
                0f);
            graphics.FillRectangle(gradient, bar.ClientRectangle);

            if (_logo is not null)
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(
                    _logo,
                    new Rectangle(16, 11, 24, 24),
                    new Rectangle(0, 0, _logo.Width, _logo.Height),
                    GraphicsUnit.Pixel);
            }
        };

        var brand = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Color.White,
            Font = new Font("Times New Roman", 10.5f, FontStyle.Bold),
            Location = new Point(50, 13),
            Text = "Ready Typing Setup"
        };
        brand.MouseDown += TitleBar_MouseDown;
        brand.MouseMove += TitleBar_MouseMove;
        brand.MouseUp += TitleBar_MouseUp;

        closeButton = new FlatButton
        {
            BaseColor = Color.FromArgb(0, Color.White),
            HoverColor = Color.FromArgb(46, 255, 255, 255),
            PressedColor = Color.FromArgb(72, 255, 255, 255),
            TextColor = Color.White,
            Radius = 8,
            Font = new Font("Times New Roman", 9f),
            Text = "✕",
            Location = new Point(732, 8),
            Size = new Size(34, 30),
            TabStop = false
        };

        bar.Controls.Add(brand);
        bar.Controls.Add(closeButton);
        bar.MouseDown += TitleBar_MouseDown;
        bar.MouseMove += TitleBar_MouseMove;
        bar.MouseUp += TitleBar_MouseUp;

        return bar;
    }

    private Panel BuildWelcomePage(
        out TextBox pathBox,
        out CheckBox desktop,
        out CheckBox startMenu,
        out CheckBox launch,
        out FlatButton install,
        out FlatButton exit,
        out FlatButton browse)
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Canvas,
            Visible = false
        };

        if (_logo is not null)
        {
            var logo = new PictureBox
            {
                Image = _logo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(44, 26),
                Size = new Size(64, 64),
                BackColor = Color.Transparent
            };
            page.Controls.Add(logo);
        }

        var title = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Ink,
            Font = new Font("Times New Roman", 26f, FontStyle.Bold),
            Location = new Point(120, 22),
            Text = "Ready Typing"
        };

        var subtitle = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Muted,
            Font = new Font("Times New Roman", 10.5f),
            Location = new Point(124, 72),
            Text = "Bengali Bijoy typing trainer  ·  Setup"
        };

        var card = new RoundPanel
        {
            Location = new Point(44, 124),
            Size = new Size(692, 300),
            FillColor = Color.White,
            BorderColor = Line,
            Radius = 16
        };

        var locationCaption = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Muted,
            Font = new Font("Times New Roman", 8.5f, FontStyle.Bold),
            Location = new Point(28, 22),
            Text = "INSTALL LOCATION"
        };

        var pathCard = new RoundPanel
        {
            Location = new Point(28, 46),
            Size = new Size(492, 36),
            FillColor = Color.FromArgb(248, 250, 252),
            BorderColor = Line,
            Radius = 9
        };

        pathBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(248, 250, 252),
            ForeColor = Ink,
            Font = new Font("Times New Roman", 9.5f),
            Location = new Point(12, 9),
            Width = 466,
            Text = InstallEngine.DefaultInstallDirectory,
            ReadOnly = false
        };

        browse = new FlatButton
        {
            BaseColor = Color.White,
            HoverColor = Color.FromArgb(241, 245, 249),
            PressedColor = Color.FromArgb(228, 230, 243),
            TextColor = Ink,
            BorderColor = Line,
            Radius = 9,
            Font = new Font("Times New Roman", 9f, FontStyle.Bold),
            Text = "Browse...",
            Location = new Point(532, 46),
            Size = new Size(132, 36)
        };

        desktop = new CheckBox
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Ink,
            Font = new Font("Times New Roman", 10f),
            Location = new Point(28, 110),
            Text = "Create a Desktop shortcut",
            Checked = true
        };

        startMenu = new CheckBox
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Ink,
            Font = new Font("Times New Roman", 10f),
            Location = new Point(28, 144),
            Text = "Add Ready Typing to the Start menu",
            Checked = true
        };

        launch = new CheckBox
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Ink,
            Font = new Font("Times New Roman", 10f),
            Location = new Point(28, 178),
            Text = "Open Ready Typing when setup finishes",
            Checked = true
        };

        var note = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Muted,
            Font = new Font("Times New Roman", 9f),
            Location = new Point(28, 222),
            Size = new Size(620, 46),
            Text = "Installs for this Windows user only. No administrator rights are needed.\r\n" +
                   "About 130 MB of disk space is used; the trainer works completely offline."
        };

        var divider = new Panel
        {
            BackColor = Line,
            Location = new Point(28, 206),
            Size = new Size(636, 1)
        };

        card.Controls.Add(locationCaption);
        card.Controls.Add(pathCard);
        pathCard.Controls.Add(pathBox);
        card.Controls.Add(browse);
        card.Controls.Add(desktop);
        card.Controls.Add(startMenu);
        card.Controls.Add(launch);
        card.Controls.Add(divider);
        card.Controls.Add(note);

        exit = new FlatButton
        {
            BaseColor = Color.White,
            HoverColor = Color.FromArgb(241, 245, 249),
            PressedColor = Color.FromArgb(228, 230, 243),
            TextColor = Ink,
            BorderColor = Line,
            Radius = 10,
            Text = "Exit",
            Location = new Point(466, 452),
            Size = new Size(108, 42)
        };

        install = new FlatButton
        {
            BaseColor = Teal,
            HoverColor = TealDark,
            PressedColor = Color.FromArgb(55, 48, 163),
            TextColor = Color.White,
            Radius = 10,
            Font = new Font("Times New Roman", 9.5f, FontStyle.Bold),
            Text = "Install now",
            Location = new Point(586, 452),
            Size = new Size(150, 42)
        };

        var footer = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Times New Roman", 8.5f),
            Location = new Point(44, 510),
            Text = "Version 2.0  ·  118 lessons  ·  works offline"
        };

        page.Controls.Add(title);
        page.Controls.Add(subtitle);
        page.Controls.Add(card);
        page.Controls.Add(exit);
        page.Controls.Add(install);
        page.Controls.Add(footer);

        return page;
    }

    private Panel BuildProgressPage(
        out ProgressBarEx progress,
        out Label status,
        out Label percent,
        out FlatButton cancel)
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Canvas,
            Visible = false
        };

        var title = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Ink,
            Font = new Font("Times New Roman", 24f, FontStyle.Bold),
            Location = new Point(44, 52),
            Text = "Installing Ready Typing"
        };

        var subtitle = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            ForeColor = Muted,
            Font = new Font("Times New Roman", 10.5f),
            Location = new Point(46, 96),
            Text = "Unpacking the trainer and creating shortcuts. This only takes a moment."
        };

        var card = new RoundPanel
        {
            Location = new Point(44, 150),
            Size = new Size(692, 216),
            FillColor = Color.White,
            BorderColor = Line,
            Radius = 16
        };

        status = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Ink,
            Font = new Font("Times New Roman", 10.5f, FontStyle.Bold),
            Location = new Point(28, 30),
            Size = new Size(636, 24),
            Text = "Preparing..."
        };

        progress = new ProgressBarEx
        {
            Location = new Point(28, 74),
            Size = new Size(636, 12),
            Value = 0d
        };

        percent = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Muted,
            Font = new Font("Times New Roman", 9f, FontStyle.Bold),
            Location = new Point(28, 96),
            Size = new Size(636, 20),
            Text = "0%",
            TextAlign = ContentAlignment.MiddleRight
        };

        var note = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Muted,
            Font = new Font("Times New Roman", 9f),
            Location = new Point(28, 142),
            Size = new Size(636, 40),
            Text = "Keep this window open while the files are unpacked."
        };

        card.Controls.Add(status);
        card.Controls.Add(progress);
        card.Controls.Add(percent);
        card.Controls.Add(note);

        cancel = new FlatButton
        {
            BaseColor = Color.White,
            HoverColor = Color.FromArgb(241, 245, 249),
            PressedColor = Color.FromArgb(228, 230, 243),
            TextColor = Muted,
            BorderColor = Line,
            Radius = 9,
            Font = new Font("Times New Roman", 9f, FontStyle.Bold),
            Text = "Cancel",
            Location = new Point(44, 398),
            Size = new Size(100, 36)
        };

        page.Controls.Add(title);
        page.Controls.Add(subtitle);
        page.Controls.Add(card);
        page.Controls.Add(cancel);
        return page;
    }

    private Panel BuildDonePage(out Label donePath, out FlatButton open)
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Canvas,
            Visible = false
        };

        var badge = new SuccessBadge
        {
            Location = new Point(358, 66),
            Size = new Size(64, 64)
        };

        var title = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Ink,
            Font = new Font("Times New Roman", 24f, FontStyle.Bold),
            Location = new Point(44, 146),
            Size = new Size(692, 42),
            Text = "You're ready to type!",
            TextAlign = ContentAlignment.MiddleCenter
        };

        var text = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Muted,
            Font = new Font("Times New Roman", 10.5f),
            Location = new Point(94, 196),
            Size = new Size(592, 56),
            Text = "Ready Typing is installed and ready to use.\r\n" +
                   "Open it from the Desktop or the Start menu any time.",
            TextAlign = ContentAlignment.MiddleCenter
        };

        open = new FlatButton
        {
            BaseColor = Teal,
            HoverColor = TealDark,
            PressedColor = Color.FromArgb(55, 48, 163),
            TextColor = Color.White,
            Radius = 10,
            Font = new Font("Times New Roman", 9.5f, FontStyle.Bold),
            Text = "Open Ready Typing",
            Location = new Point(244, 300),
            Size = new Size(190, 44)
        };

        var close = new FlatButton
        {
            BaseColor = Color.White,
            HoverColor = Color.FromArgb(241, 245, 249),
            PressedColor = Color.FromArgb(228, 230, 243),
            TextColor = Ink,
            BorderColor = Line,
            Radius = 10,
            Text = "Close",
            Location = new Point(446, 300),
            Size = new Size(90, 44)
        };
        close.Click += (_, _) => Close();

        donePath = new Label
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Times New Roman", 8.5f),
            Location = new Point(44, 364),
            Size = new Size(692, 20),
            Text = string.Empty,
            TextAlign = ContentAlignment.MiddleCenter
        };

        page.Controls.Add(badge);
        page.Controls.Add(title);
        page.Controls.Add(text);
        page.Controls.Add(open);
        page.Controls.Add(close);
        page.Controls.Add(donePath);
        return page;
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose where Ready Typing should be installed",
            ShowNewFolderButton = true,
            SelectedPath = _txtPath.Text
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _txtPath.Text = dialog.SelectedPath;
        }
    }

    private async void BtnInstall_Click(object? sender, EventArgs e)
    {
        if (_installing)
        {
            return;
        }

        string destination = _txtPath.Text.Trim();
        if (destination.Length == 0)
        {
            MessageBox.Show(
                this,
                "Choose an install folder first.",
                "Ready Typing Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            destination = Path.GetFullPath(destination);
        }
        catch
        {
            MessageBox.Show(
                this,
                "That install folder is not a valid path.",
                "Ready Typing Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _installing = true;
        _btnInstall.Enabled = false;
        _btnBrowse.Enabled = false;
        _btnCancel.Enabled = true;
        _progress.Value = 0d;
        _lblPercent.Text = "0%";
        _lblStatus.Text = "Preparing...";
        ShowPage(_progressPage);
        _btnCancel.Focus();

        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();

        var reporter = new Progress<InstallProgress>(report =>
        {
            _progress.Value = report.Fraction;
            _lblStatus.Text = report.Message;
            _lblPercent.Text = ((int)Math.Round(report.Fraction * 100d)) + "%";
        });

        bool desktop = _chkDesktop.Checked;
        bool startMenu = _chkStartMenu.Checked;
        CancellationToken token = _cancellation.Token;

        InstallResult result = await Task.Run(
            () => InstallEngine.Install(destination, desktop, startMenu, reporter, token));

        _installing = false;
        _btnInstall.Enabled = true;
        _btnBrowse.Enabled = true;

        if (result.Success)
        {
            _installedPath = result.ExecutablePath;
            _lblDonePath.Text = result.ExecutablePath;
            _progress.Value = 1d;
            _lblPercent.Text = "100%";
            ShowPage(_donePage);

            if (_chkLaunch.Checked)
            {
                InstallEngine.Launch(result.ExecutablePath);
            }

            _btnOpen.Focus();
            return;
        }

        ShowPage(_welcomePage);
        MessageBox.Show(
            this,
            result.Error,
            "Ready Typing Setup",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        if (!_installing)
        {
            return;
        }

        _btnCancel.Enabled = false;
        _lblStatus.Text = "Cancelling...";
        _cancellation?.Cancel();
    }

    private void BtnOpen_Click(object? sender, EventArgs e)
    {
        if (_installedPath is null)
        {
            Close();
            return;
        }

        if (!InstallEngine.Launch(_installedPath))
        {
            MessageBox.Show(
                this,
                "Ready Typing could not be started. Open it from the Desktop shortcut instead.",
                "Ready Typing Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Close();
    }

    private void InstallerForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_installing)
        {
            return;
        }

        e.Cancel = true;
        MessageBox.Show(
            this,
            "Setup is still finishing. Wait a moment, or press Cancel first.",
            "Ready Typing Setup",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragging = true;
        Point screen = _titleBar.PointToScreen(e.Location);
        _dragOffset = new Point(screen.X - Left, screen.Y - Top);
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging || e.Button != MouseButtons.Left)
        {
            return;
        }

        Point screen = _titleBar.PointToScreen(e.Location);
        Location = new Point(screen.X - _dragOffset.X, screen.Y - _dragOffset.Y);
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
    {
        _dragging = false;
    }

    private void LoadBrandAssets()
    {
        try
        {
            using Stream? logoStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("ReadyTypingSetup.Resources.logo.png");

            if (logoStream is not null)
            {
                using Image source = Image.FromStream(logoStream);
                _logo = new Bitmap(source);
            }
        }
        catch
        {
            _logo = null;
        }

        try
        {
            using Stream? iconStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("ReadyTypingSetup.Resources.app.ico");

            if (iconStream is not null)
            {
                _windowIcon = new Icon(iconStream);
                Icon = _windowIcon;
            }
        }
        catch
        {
            _windowIcon = null;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var border = new Pen(Color.FromArgb(214, 216, 234), 1f);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (Width > 0 && Height > 0)
        {
            Region?.Dispose();
            Region = new Region(RoundPanel.RoundedPath(
                new Rectangle(0, 0, Width, Height),
                14));
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;

            base.Dispose(disposing);

            _logo?.Dispose();
            _logo = null;

            _windowIcon?.Dispose();
            _windowIcon = null;
            return;
        }

        base.Dispose(disposing);
    }
}
