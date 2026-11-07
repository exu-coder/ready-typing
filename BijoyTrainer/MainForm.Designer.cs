#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using BijoyTrainer.Controls;

namespace BijoyTrainer;

partial class MainForm
{
    private IContainer? components;

    private Panel headerPanel = null!;
    private Label lblTitle = null!;
    private Label lblSubtitle = null!;
    private Label lblLessonCaption = null!;
    private Label lblTimerCaption = null!;
    private Label lblTimer = null!;

    private Panel contentPanel = null!;
    private TableLayoutPanel layoutRoot = null!;

    private RoundedPanel targetCard = null!;
    private Label lblTargetCaption = null!;
    private Label lblProgress = null!;
    private RichTextBox targetBox = null!;
    private ProgressStrip targetProgress = null!;

    private RoundedPanel inputCard = null!;
    private Label lblInputCaption = null!;
    private TextBox txtInput = null!;
    private Panel inputUnderline = null!;

    private Panel actionRow = null!;
    private AccentButton btnStart = null!;
    private AccentButton btnReset = null!;
    private AccentButton btnNext = null!;
    private AccentButton btnLessonOne = null!;
    private AccentButton btnHome = null!;
    private Label lblHint = null!;
    private Label lblStatus = null!;

    private TableLayoutPanel statsRow = null!;
    private StatCard statCpm = null!;
    private StatCard statWpm = null!;
    private StatCard statAccuracy = null!;
    private StatCard statProgress = null!;

    private RoundedPanel keyboardCard = null!;
    private Label lblKeyboardCaption = null!;
    private Label lblKeyboardNote = null!;
    private KeyboardPanel keyboardPanel = null!;

    private Panel footerPanel = null!;
    private TableLayoutPanel footerLayout = null!;
    private Label lblFooter = null!;
    private Label lblLessonCounter = null!;

    private System.Windows.Forms.Timer uiTimer = null!;

    private void InitializeComponent()
    {
        components = new Container();

        Color pageColor = Color.FromArgb(238, 240, 249);
        Color ink = Color.FromArgb(30, 27, 75);
        Color muted = Color.FromArgb(99, 96, 133);
        Color headerStart = Color.FromArgb(79, 70, 229);

        SuspendLayout();

        // ---------------------------------------------------------------- form
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = pageColor;
        ClientSize = new Size(1240, 900);
        MinimumSize = new Size(1024, 780);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Ready Typing - Bijoy Typing Trainer";
        DoubleBuffered = true;

        // -------------------------------------------------------------- header
        headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 96,
            BackColor = headerStart,
            TabStop = false
        };
        headerPanel.Paint += HeaderPanel_Paint;

        lblTitle = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 20f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(92, 16),
            Text = "Ready Typing"
        };

        lblSubtitle = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 9.5f),
            ForeColor = Color.FromArgb(224, 231, 255),
            Location = new Point(94, 55),
            Text = "Bijoy Typing Trainer  ·  SutonnyMJ legacy layout  ·  timed lessons  ·  v2.0"
        };

        lblLessonCaption = new Label
        {
            AutoEllipsis = true,
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(224, 231, 255),
            Location = new Point(640, 36),
            Size = new Size(360, 28),
            Text = "Lesson 1 / 1",
            TextAlign = ContentAlignment.MiddleRight,
            UseMnemonic = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        lblTimerCaption = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(207, 250, 254),
            Location = new Point(1128, 14),
            Text = "TIME LEFT",
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        lblTimer = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 18f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(1124, 33),
            Size = new Size(100, 34),
            Text = "00:00",
            TextAlign = ContentAlignment.MiddleRight
        };

        headerPanel.Controls.Add(lblTitle);
        headerPanel.Controls.Add(lblSubtitle);
        headerPanel.Controls.Add(lblLessonCaption);
        headerPanel.Controls.Add(lblTimerCaption);
        headerPanel.Controls.Add(lblTimer);

        // -------------------------------------------------------------- footer
        footerPanel = new Panel
        {
            BackColor = Color.FromArgb(228, 230, 243),
            Dock = DockStyle.Bottom,
            Height = 42,
            Padding = new Padding(18, 0, 18, 0)
        };

        footerLayout = new TableLayoutPanel
        {
            BackColor = Color.Transparent,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            RowCount = 1
        };
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190f));

        lblFooter = new Label
        {
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Font = new Font("Times New Roman", 8.5f),
            ForeColor = Color.FromArgb(70, 68, 96),
            Text = "Follow the pointing hand  ·  Hold Shift for the upper Bengali legend  ·  G joins letters  ·  Finish before the timer ends",
            TextAlign = ContentAlignment.MiddleLeft
        };

        lblLessonCounter = new Label
        {
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Font = new Font("Times New Roman", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(67, 56, 202),
            Text = "Lesson 1 / 1",
            TextAlign = ContentAlignment.MiddleRight
        };

        footerLayout.Controls.Add(lblFooter, 0, 0);
        footerLayout.Controls.Add(lblLessonCounter, 1, 0);
        footerPanel.Controls.Add(footerLayout);

        // ------------------------------------------------------------- content
        contentPanel = new Panel
        {
            BackColor = pageColor,
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 12)
        };

        layoutRoot = new TableLayoutPanel
        {
            BackColor = pageColor,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(0),
            RowCount = 5
        };
        layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 190f));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 108f));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 88f));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // --------------------------------------------------------- target card
        targetCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(20, 12, 20, 12)
        };

        lblTargetCaption = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 8f, FontStyle.Bold),
            ForeColor = muted,
            Location = new Point(22, 16),
            Text = "TARGET TEXT"
        };

        lblProgress = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 9f, FontStyle.Bold),
            ForeColor = muted,
            Location = new Point(1050, 13),
            Size = new Size(132, 20),
            Text = "0 / 0",
            TextAlign = ContentAlignment.MiddleRight
        };

        targetBox = new RichTextBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            DetectUrls = false,
            Font = new Font("Times New Roman", 30f),
            ForeColor = ink,
            Location = new Point(22, 54),
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.None,
            ShortcutsEnabled = false,
            TabStop = false,
            Text = string.Empty,
            WordWrap = true
        };

        targetProgress = new ProgressStrip
        {
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
            Location = new Point(22, 162),
            Size = new Size(1160, 6),
            TabStop = false
        };

        targetCard.Controls.Add(lblTargetCaption);
        targetCard.Controls.Add(lblProgress);
        targetCard.Controls.Add(targetBox);
        targetCard.Controls.Add(targetProgress);

        // ---------------------------------------------------------- input card
        inputCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(20, 10, 20, 10)
        };

        lblInputCaption = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 8f, FontStyle.Bold),
            ForeColor = muted,
            Location = new Point(22, 11),
            Text = "YOUR INPUT"
        };

        txtInput = new TextBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Times New Roman", 25f),
            ForeColor = ink,
            Location = new Point(20, 34),
            ShortcutsEnabled = false,
            Size = new Size(1164, 44),
            TabIndex = 0,
            ImeMode = ImeMode.Disable
        };
        txtInput.TextChanged += TxtInput_TextChanged;
        txtInput.KeyPress += TxtInput_KeyPress;
        txtInput.KeyDown += TxtInput_KeyDown;
        txtInput.KeyUp += TxtInput_KeyUp;
        txtInput.MouseDown += TxtInput_MouseDown;
        txtInput.Enter += TxtInput_Enter;
        txtInput.Leave += TxtInput_Leave;

        inputUnderline = new Panel
        {
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(199, 210, 254),
            Location = new Point(22, 82),
            Size = new Size(1160, 3)
        };

        inputCard.Controls.Add(lblInputCaption);
        inputCard.Controls.Add(txtInput);
        inputCard.Controls.Add(inputUnderline);

        // --------------------------------------------------------- action row
        actionRow = new Panel
        {
            BackColor = pageColor,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8)
        };

        btnStart = new AccentButton
        {
            Location = new Point(0, 6),
            Size = new Size(108, 40),
            Text = "Start",
            TabIndex = 2
        };
        btnStart.Click += BtnStart_Click;

        btnReset = new AccentButton
        {
            BaseColor = Color.FromArgb(228, 230, 243),
            HoverColor = Color.FromArgb(214, 216, 234),
            PressedColor = Color.FromArgb(186, 192, 220),
            Location = new Point(116, 6),
            Size = new Size(100, 40),
            Text = "Reset",
            TextColor = Color.FromArgb(48, 46, 84),
            TabIndex = 3
        };
        btnReset.Click += BtnReset_Click;

        btnNext = new AccentButton
        {
            BaseColor = Color.FromArgb(6, 182, 212),
            HoverColor = Color.FromArgb(8, 145, 178),
            PressedColor = Color.FromArgb(14, 116, 144),
            Location = new Point(224, 6),
            Size = new Size(140, 40),
            Text = "Next lesson",
            TabIndex = 4
        };
        btnNext.Click += BtnNext_Click;

        btnLessonOne = new AccentButton
        {
            BaseColor = Color.FromArgb(228, 230, 243),
            HoverColor = Color.FromArgb(214, 216, 234),
            PressedColor = Color.FromArgb(186, 192, 220),
            Location = new Point(372, 6),
            Size = new Size(132, 40),
            Text = "Lesson 1",
            TextColor = Color.FromArgb(48, 46, 84),
            TabIndex = 5
        };
        btnLessonOne.Click += BtnLessonOne_Click;

        btnHome = new AccentButton
        {
            BaseColor = Color.FromArgb(228, 230, 243),
            HoverColor = Color.FromArgb(214, 216, 234),
            PressedColor = Color.FromArgb(186, 192, 220),
            Location = new Point(512, 6),
            Size = new Size(96, 40),
            Text = "⌂ Home",
            TextColor = Color.FromArgb(48, 46, 84),
            TabIndex = 6
        };
        btnHome.Click += BtnHome_Click;

        lblHint = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            AutoSize = false,
            AutoEllipsis = true,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(67, 56, 202),
            Location = new Point(520, 8),
            Size = new Size(354, 36),
            Text = "Next key: -",
            TextAlign = ContentAlignment.MiddleLeft,
            UseMnemonic = false
        };

        lblStatus = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            AutoEllipsis = true,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 9f),
            ForeColor = Color.FromArgb(99, 96, 133),
            Location = new Point(882, 8),
            Size = new Size(322, 36),
            Text = "Press Start, or simply begin typing.",
            TextAlign = ContentAlignment.MiddleRight,
            UseMnemonic = false
        };

        actionRow.Controls.Add(btnStart);
        actionRow.Controls.Add(btnReset);
        actionRow.Controls.Add(btnNext);
        actionRow.Controls.Add(btnLessonOne);
        actionRow.Controls.Add(btnHome);
        actionRow.Controls.Add(lblHint);
        actionRow.Controls.Add(lblStatus);

        // ----------------------------------------------------------- stat grid
        statsRow = new TableLayoutPanel
        {
            BackColor = pageColor,
            ColumnCount = 4,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(0),
            RowCount = 1
        };
        for (int i = 0; i < 4; i++)
        {
            statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        }

        statCpm = new StatCard
        {
            AccentColor = Color.FromArgb(79, 70, 229),
            Caption = "CHARACTERS / MIN",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            Suffix = "CPM",
            Value = "0"
        };

        statWpm = new StatCard
        {
            AccentColor = Color.FromArgb(56, 189, 248),
            Caption = "WORDS / MIN",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            Suffix = "WPM",
            Value = "0"
        };

        statAccuracy = new StatCard
        {
            AccentColor = Color.FromArgb(16, 185, 129),
            Caption = "ACCURACY",
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            Suffix = "%",
            Value = "100"
        };

        statProgress = new StatCard
        {
            AccentColor = Color.FromArgb(245, 158, 11),
            Caption = "PROGRESS",
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Suffix = "%",
            Value = "0"
        };

        statsRow.Controls.Add(statCpm, 0, 0);
        statsRow.Controls.Add(statWpm, 1, 0);
        statsRow.Controls.Add(statAccuracy, 2, 0);
        statsRow.Controls.Add(statProgress, 3, 0);

        // ------------------------------------------------------- keyboard card
        keyboardCard = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(16, 10, 16, 12)
        };

        lblKeyboardCaption = new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 8f, FontStyle.Bold),
            ForeColor = muted,
            Location = new Point(20, 12),
            Text = "BIJOY KEYBOARD"
        };

        lblKeyboardNote = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.Transparent,
            Font = new Font("Times New Roman", 8f),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(780, 12),
            Size = new Size(480, 16),
            Text = "The pointing hand shows the next key  ·  it points at Shift first when the upper legend is needed",
            TextAlign = ContentAlignment.MiddleRight
        };

        keyboardPanel = new KeyboardPanel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.White,
            Location = new Point(14, 34),
            Size = new Size(1176, 150),
            TabStop = false
        };

        keyboardCard.Controls.Add(lblKeyboardCaption);
        keyboardCard.Controls.Add(lblKeyboardNote);
        keyboardCard.Controls.Add(keyboardPanel);

        layoutRoot.Controls.Add(targetCard, 0, 0);
        layoutRoot.Controls.Add(inputCard, 0, 1);
        layoutRoot.Controls.Add(actionRow, 0, 2);
        layoutRoot.Controls.Add(statsRow, 0, 3);
        layoutRoot.Controls.Add(keyboardCard, 0, 4);

        contentPanel.Controls.Add(layoutRoot);

        Controls.Add(contentPanel);
        Controls.Add(footerPanel);
        Controls.Add(headerPanel);

        // ------------------------------------------------------------- timer
        uiTimer = new System.Windows.Forms.Timer(components)
        {
            Interval = 200
        };
        uiTimer.Tick += UiTimer_Tick;

        ResumeLayout(false);
    }
}
