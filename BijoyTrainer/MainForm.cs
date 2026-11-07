using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Forms;
using BijoyTrainer.Controls;
using BijoyTrainer.Core;
using BijoyTrainer.Views;

namespace BijoyTrainer;

/// <summary>
/// The single-window Ready Typing trainer. This file contains the UI behaviour;
/// all Bengali input logic lives in <see cref="Core.BijoyInputHandler"/> and the
/// Core folder stays free of Windows Forms references.
/// </summary>
public partial class MainForm : Form
{
    private static readonly Color CorrectColor = Color.FromArgb(4, 120, 87);
    private static readonly Color WrongColor = Color.FromArgb(225, 29, 72);
    private static readonly Color PendingColor = Color.FromArgb(30, 27, 75);
    private static readonly Color FutureColor = Color.FromArgb(70, 68, 96);
    private static readonly Color CurrentBackColor = Color.FromArgb(254, 243, 199);
    private static readonly Color StatusColor = Color.FromArgb(99, 96, 133);
    private static readonly Color SuccessColor = Color.FromArgb(16, 185, 129);

    private readonly ComparisonEngine _comparison = new();
    private readonly StatsTracker _stats = new();
    private readonly Stopwatch _lessonClock = new();
    private readonly List<Keystroke> _keystrokes = new();

    private BijoyInputHandler? _inputHandler;
    private FontLoader? _fontLoader;
    private LessonManager? _lessonManager;

    private Font? _targetFont;
    private Font? _inputFont;
    private Font? _keyboardFont;
    private Image? _logoImage;

    private int _lessonIndex;
    private int _lessonLimitSeconds;
    private bool _countdownStarted;
    private bool _timedOut;
    private bool _lessonPassed;
    private bool _loading;
    private bool _completed;
    private bool _disposed;
    private bool _chromeReady;
    private bool _inputFocused;
    private bool _resultsShown;
    private ResultsView? _resultsView;
    private HomeView? _homeView;
    private int _practiceSessions;
    private double _pulse;

    /// <summary>Initializes the trainer window.</summary>
    public MainForm()
    {
        InitializeComponent();
        _inputHandler = new BijoyInputHandler();
        _chromeReady = true;

        Load += MainForm_Load;
        FormClosed += MainForm_FormClosed;
        Resize += MainForm_Resize;
        Shown += MainForm_Shown;
        LayoutChrome();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the lesson results screen opens
    /// automatically after a completion or timeout. Tests disable it when they
    /// only want to exercise the typing flow.
    /// </summary>
    public bool ResultsScreenEnabled { get; set; } = true;

    /// <summary>
    /// Routes Enter to the in-window results screen. The results view lives
    /// inside this form, so the key handling belongs here rather than in a
    /// dialog.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_resultsView is { Visible: true })
        {
            if ((keyData & Keys.KeyCode) == Keys.Enter)
            {
                _resultsView.Confirm();
                return true;
            }

            if ((keyData & Keys.KeyCode) == Keys.Escape)
            {
                // The results screen has exactly one way forward: Enter (or
                // the Go on button).
                return true;
            }
        }

        if (_homeView is { Visible: false } && (keyData & Keys.KeyCode) == Keys.Escape)
        {
            ShowHome();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        if (_homeView is { Visible: true })
        {
            _homeView.Focus();
            return;
        }

        txtInput.Focus();
    }

    private void MainForm_Resize(object? sender, EventArgs e) => LayoutChrome();

    /// <summary>
    /// Positions the anchored chrome and the contents of the rounded cards.
    /// The WinForms designer records anchor offsets against its design-time
    /// parent size, which makes anchored children drift when a card is created
    /// at a different size. Laying them out explicitly keeps the layout exact
    /// at any window size and DPI.
    /// </summary>
    private void LayoutChrome()
    {
        if (!_chromeReady)
        {
            return;
        }

        // Header: brand on the left, current lesson and countdown on the right.
        int headerWidth = headerPanel.ClientSize.Width;
        const int headerPad = 24;
        const int timerWidth = 112;

        int timerX = Math.Max(420, headerWidth - headerPad - timerWidth);
        lblTimer.SetBounds(timerX, 30, timerWidth, 40);
        lblTimerCaption.Location = new Point(timerX + timerWidth - lblTimerCaption.Width, 13);

        // The lesson caption is a transparent label, so its rectangle must not
        // overlap the subtitle: a transparent sibling repaints the gradient and
        // would clip the words underneath it.
        int lessonLeft = Math.Max(360, lblSubtitle.Right + 20);
        int lessonRight = timerX - 28;
        lblLessonCaption.SetBounds(
            lessonLeft,
            36,
            Math.Max(0, lessonRight - lessonLeft),
            28);

        // Target card.
        int cardWidth = targetCard.ClientSize.Width;
        int cardHeight = targetCard.ClientSize.Height;
        if (cardWidth > 60 && cardHeight > 90)
        {
            int innerWidth = cardWidth - 44;
            lblProgress.SetBounds(cardWidth - 22 - 132, 13, 132, 20);
            targetProgress.SetBounds(22, cardHeight - 18, innerWidth, 6);
            LayoutTargetBox(cardWidth, cardHeight);
        }

        // Input card.
        int inputWidth = inputCard.ClientSize.Width;
        int inputHeight = inputCard.ClientSize.Height;
        if (inputWidth > 60 && inputHeight > 60)
        {
            txtInput.SetBounds(20, 32, inputWidth - 40, 42);
            inputUnderline.SetBounds(22, inputHeight - 12, inputWidth - 44, 3);
        }

        // Action row.
        int rowWidth = actionRow.ClientSize.Width;
        int hintX = 616;
        int statusWidth = Math.Min(322, Math.Max(140, rowWidth / 4));
        int hintWidth = Math.Max(120, rowWidth - hintX - statusWidth - 12);
        lblHint.SetBounds(hintX, 4, hintWidth, 40);
        lblStatus.SetBounds(rowWidth - statusWidth, 4, statusWidth, 40);

        // Keyboard card.
        int keyboardWidth = keyboardCard.ClientSize.Width;
        int keyboardHeight = keyboardCard.ClientSize.Height;
        if (keyboardWidth > 240 && keyboardHeight > 70)
        {
            lblKeyboardNote.SetBounds(keyboardWidth - 20 - 480, 12, 480, 16);
            keyboardPanel.SetBounds(14, 32, keyboardWidth - 28, keyboardHeight - 32 - 10);
        }
    }

    /// <summary>
    /// Sizes and vertically centres the target text inside its card. Short
    /// lessons sit in the middle of the card instead of hugging the top edge,
    /// and long captions keep a full block of available height.
    /// </summary>
    private void LayoutTargetBox(int cardWidth, int cardHeight)
    {
        if (cardWidth <= 60 || cardHeight <= 90)
        {
            return;
        }

        int innerWidth = cardWidth - 44;
        int availableTop = 48;
        int availableBottom = cardHeight - 28;
        int available = Math.Max(38, availableBottom - availableTop);

        // Measure wrapping at the final width first.
        targetBox.SetBounds(22, availableTop, innerWidth, available);

        int lines = 1;
        if (targetBox.TextLength > 0)
        {
            lines = Math.Max(1, targetBox.GetLineFromCharIndex(targetBox.TextLength - 1) + 1);
        }

        float lineHeight = _targetFont?.GetHeight() ?? 36f;
        int textHeight = (int)Math.Ceiling(lineHeight * lines) + 14;
        int height = Math.Min(available, Math.Max(42, textHeight));
        int top = availableTop + Math.Max(0, (available - height) / 2);

        targetBox.SetBounds(22, top, innerWidth, height);
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        TryLoadWindowIcon();
        _logoImage = TryLoadLogo();
        LayoutChrome();

        _fontLoader = FontLoader.Create(out string? fontError);
        _targetFont = _fontLoader.CreateFont(30f);
        _inputFont = _fontLoader.CreateFont(25f);
        _keyboardFont = _fontLoader.CreateFont(13f);

        targetBox.Font = _targetFont;
        txtInput.Font = _inputFont;
        keyboardPanel.BengaliFont = _keyboardFont;
        txtInput.HideSelection = false;

        _lessonManager = LessonManager.Load(out string? lessonWarning);
        int resumeIndex = LessonProgressStore.LoadResumeIndex(_lessonManager.Lessons.Count);
        LoadLesson(resumeIndex);

        uiTimer.Start();

        if (fontError is not null)
        {
            MessageBox.Show(
                this,
                fontError + Environment.NewLine + Environment.NewLine +
                "Place SutonnyMJ.ttf in the Resources folder with Build Action = Embedded Resource, " +
                "then rebuild the project.",
                "Ready Typing - font could not be loaded",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        if (lessonWarning is not null)
        {
            lblStatus.Text = lessonWarning;
            lblStatus.ForeColor = Color.FromArgb(180, 83, 9);
        }
        else
        {
            lblStatus.Text = resumeIndex > 0
                ? $"Resuming at lesson {resumeIndex + 1} – beat the timer."
                : "Press Start or begin typing – beat the timer.";
            lblStatus.ForeColor = StatusColor;
        }

        CreateHomeView();
    }

    /// <summary>
    /// Creates the dashboard home page and makes it the first screen the
    /// learner sees. The trainer layout stays loaded underneath so switching
    /// between the two is instant.
    /// </summary>
    private void CreateHomeView()
    {
        _homeView = new HomeView
        {
            Dock = DockStyle.Fill,
            Logo = _logoImage
        };

        _homeView.LessonRequested += HomeView_LessonRequested;
        _homeView.NavigationRequested += HomeView_NavigationRequested;

        contentPanel.Controls.Add(_homeView);
        _homeView.BringToFront();

        ShowHome();
    }

    /// <summary>
    /// Shows the dashboard and stops any lesson clock that was running.
    /// </summary>
    private void ShowHome()
    {
        if (_homeView is null)
        {
            return;
        }

        _lessonClock.Reset();
        _countdownStarted = false;
        _timedOut = false;
        _completed = false;
        _stats.StopTimer();
        _stats.Reset();

        layoutRoot.Visible = false;
        _homeView.Visible = true;
        _homeView.BringToFront();
        _homeView.ShowSection("dashboard");
        _homeView.SetLessons(BuildHomeLessons());
        _homeView.SetProgress(LessonProgressStore.LoadLastCompleted(), _practiceSessions);

        lblTimer.Text = "--:--";
        lblTimer.ForeColor = Color.White;
        lblLessonCaption.Text = "Dashboard";
        lblLessonCounter.Text = "Home";
        lblStatus.ForeColor = StatusColor;
        lblStatus.Text = "Choose a lesson to begin.";
    }

    /// <summary>
    /// Shows the typing trainer and hides the dashboard.
    /// </summary>
    private void ShowTrainer()
    {
        if (_homeView is not null)
        {
            _homeView.Visible = false;
        }

        layoutRoot.Visible = true;
        layoutRoot.BringToFront();
        txtInput.Focus();
    }

    private void HomeView_LessonRequested(object? sender, int lessonNumber)
    {
        if (_lessonManager is null)
        {
            return;
        }

        LoadLesson(lessonNumber - 1);
        ShowTrainer();
    }

    private void HomeView_NavigationRequested(object? sender, string page)
    {
        if (string.Equals(page, "practice", StringComparison.OrdinalIgnoreCase))
        {
            LoadLesson(_lessonIndex);
            ShowTrainer();
        }
    }

    private void BtnHome_Click(object? sender, EventArgs e) => ShowHome();

    /// <summary>
    /// Projects the lesson bank into the cards shown on the dashboard. The
    /// completion flag comes from the persisted progress file.
    /// </summary>
    private List<LessonCardModel> BuildHomeLessons()
    {
        var models = new List<LessonCardModel>();
        if (_lessonManager is null)
        {
            return models;
        }

        int lastCompleted = LessonProgressStore.LoadLastCompleted();
        for (int i = 0; i < _lessonManager.Lessons.Count; i++)
        {
            Lesson lesson = _lessonManager.Lessons[i];
            int number = i + 1;

            models.Add(new LessonCardModel(
                number,
                lesson.Title,
                $"Practice {lesson.Group.ToLowerInvariant()} with the Bijoy keyboard.",
                lesson.Group,
                FormatLessonLength(lesson.Text.Length),
                IconForGroup(lesson.Group),
                number <= lastCompleted));
        }

        return models;
    }

    private static string FormatLessonLength(int characterCount)
    {
        int seconds = CalculateLessonLimit(characterCount);
        int minutes = Math.Max(1, (int)Math.Round(seconds / 60d));
        return $"{minutes} min";
    }

    private static string IconForGroup(string group)
    {
        string value = group.ToLowerInvariant();
        if (value.Contains("warm") || value.Contains("reach"))
        {
            return "⌨";
        }

        if (value.Contains("everyday"))
        {
            return "✎";
        }

        if (value.Contains("conjunct"))
        {
            return "⌘";
        }

        if (value.Contains("digit"))
        {
            return "123";
        }

        if (value.Contains("pride"))
        {
            return "♛";
        }

        if (value.Contains("nature"))
        {
            return "⚡";
        }

        if (value.Contains("sentence") || value.Contains("longer"))
        {
            return "▤";
        }

        return "অ";
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        uiTimer.Stop();
        _stats.StopTimer();
        _inputHandler?.Reset();
        _inputHandler = null;
    }

    private void TryLoadWindowIcon()
    {
        try
        {
            using Stream? stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("BijoyTrainer.Resources.app.ico");

            if (stream is not null)
            {
                Icon = new Icon(stream);
            }
        }
        catch
        {
            // A missing icon must never stop the trainer from opening.
        }
    }

    private static Image? TryLoadLogo()
    {
        try
        {
            using Stream? stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("BijoyTrainer.Resources.logo.png");

            if (stream is null)
            {
                return null;
            }

            using Image source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch
        {
            return null;
        }
    }

    private void LoadLesson(int index)
    {
        if (_lessonManager is null || _lessonManager.Lessons.Count == 0)
        {
            return;
        }

        int count = _lessonManager.Lessons.Count;
        index = ((index % count) + count) % count;
        _lessonIndex = index;

        Lesson lesson = _lessonManager.Lessons[index];

        _loading = true;
        try
        {
            _stats.Reset();
            _comparison.SetTarget(lesson.Text);
            _inputHandler?.Reset();
            _keystrokes.Clear();
            _resultsShown = false;

            txtInput.Clear();
            targetBox.Clear();
            targetBox.Text = lesson.Text;

            // A small paragraph indent stops the left edge of wide legacy
            // glyphs (for example আ) from being clipped by the control border.
            targetBox.SelectAll();
            targetBox.SelectionIndent = 18;
            targetBox.Select(0, 0);

            lblProgress.Text = $"0 / {lesson.Text.Length}";
            lblLessonCounter.Text = $"Lesson {index + 1} / {count}";
            lblLessonCaption.Text = $"Lesson {index + 1} / {count}  ·  {lesson.Title}";

            _lessonLimitSeconds = CalculateLessonLimit(lesson.Text.Length);
            _lessonClock.Reset();
            _countdownStarted = false;
            _timedOut = false;
            _lessonPassed = false;
            txtInput.ReadOnly = false;
            btnStart.Enabled = true;
            btnNext.Enabled = false;
            lblTimer.ForeColor = Color.White;
            lblTimer.Text = FormatSeconds(_lessonLimitSeconds);

            targetProgress.Value = 0d;
            _completed = false;

            ApplyTargetColors();
            LayoutChrome();
            UpdateStats();
            UpdateNextKey();

            inputUnderline.BackColor = Color.FromArgb(199, 210, 254);
            lblStatus.Text = "Press Start or begin typing – beat the timer.";
            lblStatus.ForeColor = StatusColor;
        }
        finally
        {
            _loading = false;
        }

        txtInput.Focus();
    }

    private void TxtInput_TextChanged(object? sender, EventArgs e)
    {
        if (_loading || _timedOut)
        {
            return;
        }

        _comparison.Update(txtInput.Text);
        _stats.Update(_comparison.CorrectCount, _comparison.TotalTyped);

        if (!_completed && txtInput.TextLength > 0)
        {
            StartCountdown();

            if (!_stats.IsRunning)
            {
                _stats.StartTimer();
            }
        }

        if (_completed && txtInput.TextLength < _comparison.Target.Length)
        {
            _completed = false;
            _stats.StartTimer();
        }

        ApplyTargetColors();
        UpdateStats();
        UpdateNextKey();
        UpdateInputFeedback();

        bool waitingForConjunct =
            _inputHandler is not null &&
            (_inputHandler.Buffer.IsPending ||
             _inputHandler.Buffer.HasOpenConsonant);

        // A consonant alone reaches the target length before the G + consonant
        // pair can create a conjunct. Do not lock the input until the cluster
        // has resolved to an exact match, or there is no base left to resolve.
        bool canStillResolve =
            _comparison.ErrorCount > 0 &&
            waitingForConjunct;

        if (!_completed && _comparison.IsComplete && !canStillResolve)
        {
            if (_countdownStarted &&
                !_timedOut &&
                _lessonClock.Elapsed.TotalSeconds >= _lessonLimitSeconds)
            {
                HandleTimeout();
            }
            else
            {
                CompleteLesson();
            }
        }
    }

    /// <summary>
    /// Highlights typing mistakes immediately: the input underline turns red on
    /// the last wrong character and the status line explains how to fix it.
    /// This makes wrong input obvious even though a plain <see cref="TextBox"/>
    /// cannot colour individual characters.
    /// </summary>
    private void UpdateInputFeedback()
    {
        IReadOnlyList<bool> results = _comparison.PerCharacterResult;
        bool lastWrong = results.Count > 0 && !results[^1];

        if (_completed)
        {
            inputUnderline.BackColor = _lessonPassed ? SuccessColor : WrongColor;
            return;
        }

        if (_timedOut)
        {
            inputUnderline.BackColor = WrongColor;
            return;
        }

        inputUnderline.BackColor = lastWrong
            ? WrongColor
            : _comparison.ErrorCount > 0
                ? Color.FromArgb(245, 158, 11)
                : _inputFocused
                    ? Color.FromArgb(79, 70, 229)
                    : Color.FromArgb(199, 210, 254);

        if (lastWrong)
        {
            lblStatus.ForeColor = WrongColor;
            lblStatus.Text =
                $"Incorrect character – press Backspace to fix it ({_comparison.ErrorCount} wrong)";
        }
        else if (_stats.IsRunning)
        {
            lblStatus.ForeColor = StatusColor;
            lblStatus.Text = "Timer running – finish the line before it reaches zero.";
        }
    }

    private void TxtInput_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (_inputHandler is null || _loading || _timedOut || _completed)
        {
            return;
        }

        if (e.KeyChar == '\b')
        {
            return; // Handled by KeyDown so a pending hasanta can be cancelled.
        }

        if ((Control.ModifierKeys & (Keys.Control | Keys.Alt)) != 0)
        {
            return;
        }

        bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

        char? previous = null;
        int caret = txtInput.SelectionStart;
        if (caret > 0 && caret <= txtInput.TextLength)
        {
            previous = txtInput.Text[caret - 1];
        }

        string textBefore = txtInput.Text;

        if (_inputHandler.TryProcessKey(e.KeyChar, shift, previous, out LigatureAction action))
        {
            e.Handled = true;
            ApplyLigatureAction(action);
            RecordKeystroke(e.KeyChar, textBefore);
        }
        else if (!char.IsControl(e.KeyChar))
        {
            // Unknown printable keys are swallowed: the legacy font would
            // otherwise paint a random Latin-looking glyph.
            e.Handled = true;
        }
    }

    /// <summary>
    /// Stores one processed key press for the results charts. The comparison
    /// engine has already seen the new text, so the character at the caret is
    /// classified as correct or wrong exactly as the lesson colours it.
    /// </summary>
    private void RecordKeystroke(char key, string textBefore)
    {
        if (_comparison.Target.Length == 0 ||
            string.Equals(textBefore, txtInput.Text, StringComparison.Ordinal))
        {
            return;
        }

        int index = Math.Max(0, txtInput.SelectionStart - 1);
        bool correct =
            index < _comparison.PerCharacterResult.Count &&
            _comparison.PerCharacterResult[index];

        _keystrokes.Add(new Keystroke(
            char.ToLowerInvariant(key),
            correct,
            _lessonClock.Elapsed,
            index));
    }

    private void TxtInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_inputHandler is null)
        {
            return;
        }

        keyboardPanel.ShiftHeld = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

        if (e.KeyCode is Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey)
        {
            UpdateNextKey();
        }

        if (_timedOut)
        {
            return;
        }

        if (e.KeyCode == Keys.Back)
        {
            if (_inputHandler.ProcessBackspace())
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }

            return;
        }

        if (e.KeyCode is Keys.Left or Keys.Right or Keys.Home or Keys.End or Keys.Delete)
        {
            _inputHandler.ForgetBase();
        }

        if (e.KeyCode == Keys.Escape)
        {
            _inputHandler.Reset();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void TxtInput_KeyUp(object? sender, KeyEventArgs e)
    {
        keyboardPanel.ShiftHeld = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

        if (!_completed && !_timedOut)
        {
            UpdateNextKey();
        }
    }

    private void TxtInput_MouseDown(object? sender, MouseEventArgs e) => _inputHandler?.ForgetBase();

    private void TxtInput_Enter(object? sender, EventArgs e)
    {
        _inputFocused = true;
        keyboardPanel.ShiftHeld = false;
        UpdateInputFeedback();
        UpdateNextKey();
    }

    private void TxtInput_Leave(object? sender, EventArgs e)
    {
        _inputFocused = false;
        keyboardPanel.ShiftHeld = false;
        keyboardPanel.PointToShift = false;
        UpdateInputFeedback();
    }

    private void ApplyLigatureAction(LigatureAction action)
    {
        switch (action.Kind)
        {
            case LigatureActionKind.None:
                return;

            case LigatureActionKind.Insert:
                InsertText(action.Text);
                break;

            case LigatureActionKind.ReplacePrevious:
            {
                int caret = txtInput.SelectionStart;
                bool canReplace =
                    txtInput.SelectionLength == 0 &&
                    caret > 0 &&
                    action.ExpectedPrevious is char expected &&
                    txtInput.Text[caret - 1] == expected;

                if (canReplace)
                {
                    txtInput.Select(caret - 1, 1);
                    txtInput.SelectedText = action.Text;
                    txtInput.SelectionStart = caret - 1 + action.Text.Length;
                    txtInput.SelectionLength = 0;
                }
                else
                {
                    InsertText(action.Fallback);
                }

                break;
            }
        }
    }

    private void InsertText(string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        int caret = txtInput.SelectionStart + text.Length;
        txtInput.SelectedText = text;
        txtInput.SelectionStart = caret;
        txtInput.SelectionLength = 0;
    }

    private void ApplyTargetColors()
    {
        string target = _comparison.Target;
        if (targetBox.TextLength != target.Length)
        {
            return;
        }

        int typedLength = Math.Min(txtInput.TextLength, target.Length);

        // Nothing typed yet: paint everything as "future" in one run.
        if (target.Length == 0)
        {
            return;
        }

        targetBox.SuspendLayout();
        try
        {
            int runStart = 0;
            (Color Fore, Color Back) runStyle = StyleFor(0, typedLength);

            for (int i = 1; i <= target.Length; i++)
            {
                (Color Fore, Color Back) style = i < target.Length
                    ? StyleFor(i, typedLength)
                    : default;

                if (i == target.Length || style != runStyle)
                {
                    ApplyRun(runStart, i - runStart, runStyle.Fore, runStyle.Back);
                    runStart = i;
                    runStyle = style;
                }
            }
        }
        finally
        {
            // Re-apply the paragraph margin last: applying character colours
            // resets the paragraph format on some RichEdit versions, which
            // clipped the left edge of the first legacy glyph.
            targetBox.SelectAll();
            targetBox.SelectionIndent = 18;
            targetBox.SelectionRightIndent = 10;
            targetBox.Select(0, 0);
            targetBox.ResumeLayout();
        }
    }

    private static (Color Fore, Color Back) StyleFor(int index, int typedLength)
    {
        if (index < typedLength)
        {
            // Colour is repaired below by ApplyRun because correctness is
            // evaluated per index by the caller.
            return (FutureColor, Color.White);
        }

        if (index == typedLength)
        {
            return (PendingColor, CurrentBackColor);
        }

        return (FutureColor, Color.White);
    }

    private void ApplyRun(int start, int length, Color foreColor, Color backColor)
    {
        if (length <= 0)
        {
            return;
        }

        // Correctness is stored on the comparison engine; recompute the colour
        // here so the run helper stays independent of the caller's loop.
        bool[]? results = null;
        if (_comparison.PerCharacterResult.Count > 0)
        {
            results = new bool[_comparison.PerCharacterResult.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _comparison.PerCharacterResult[i];
            }
        }

        for (int i = start; i < start + length; i++)
        {
            Color fore = foreColor;
            if (results is not null && i < results.Length)
            {
                fore = results[i] ? CorrectColor : WrongColor;
            }
            else if (i == _comparison.CurrentIndex)
            {
                fore = PendingColor;
            }

            targetBox.Select(i, 1);
            targetBox.SelectionColor = fore;
            targetBox.SelectionBackColor = backColor;
        }
    }

    private void UpdateStats()
    {
        int cpm = (int)Math.Round(_stats.CharactersPerMinute);
        int wpm = (int)Math.Round(_stats.WordsPerMinute);
        int accuracy = (int)Math.Round(_stats.Accuracy);

        int typed = Math.Min(txtInput.TextLength, _comparison.Target.Length);
        int total = _comparison.Target.Length;
        double progress = total > 0 ? (double)typed / total : 0d;

        statCpm.Value = cpm.ToString();
        statWpm.Value = wpm.ToString();
        statAccuracy.Value = Math.Clamp(accuracy, 0, 100).ToString();
        statProgress.Value = ((int)Math.Round(progress * 100d)).ToString();

        targetProgress.Value = progress;
        lblProgress.Text = $"{typed} / {total}";
    }

    private void UpdateNextKey()
    {
        int index = _comparison.CurrentIndex;

        if (_completed || index >= _comparison.Target.Length)
        {
            keyboardPanel.PointToShift = false;
            keyboardPanel.ClearHighlight();
            lblHint.Text = _completed ? "Lesson complete!" : "Next key: -";
            return;
        }

        char targetChar = _comparison.Target[index];
        char second = default;

        bool pendingJoin =
            _inputHandler?.Buffer.IsPending == true &&
            _inputHandler.Buffer.PendingBase is char pendingBase &&
            ((BijoyMappingTable.TryGetConjunctParts(targetChar.ToString(), out char first, out second) &&
              first == pendingBase) ||
             BijoyMappingTable.TryGetConjunctContinuation(
                 _comparison.Target, index, pendingBase, out second));

        if (pendingJoin &&
            BijoyMappingTable.TryGetPhysicalKey(second, out char joinKey, out bool joinShift))
        {
            // G is already down; the next key is the second consonant.
            keyboardPanel.NextKey = joinKey;
            keyboardPanel.NextShift = joinShift;
            keyboardPanel.PointToShift = joinShift && !keyboardPanel.ShiftHeld;
            lblHint.Text =
                $"Next key: {BijoyMappingTable.FormatKey(joinKey, joinShift)}  ·  " +
                $"{KeyboardPanel.DescribeFinger(joinKey)} (joins with G)";
            return;
        }

        if (BijoyMappingTable.TryGetNextKey(targetChar, out char key, out bool shift))
        {
            keyboardPanel.NextKey = key;
            keyboardPanel.NextShift = shift;
            keyboardPanel.PointToShift = shift && !keyboardPanel.ShiftHeld;
            lblHint.Text = keyboardPanel.PointToShift
                ? $"Hold Shift, then press {BijoyMappingTable.FormatKey(key, false)}  ·  " +
                  $"{KeyboardPanel.DescribeFinger(key)}"
                : $"Next key: {BijoyMappingTable.GetKeyHint(targetChar)}  ·  " +
                  $"{KeyboardPanel.DescribeFinger(key)}";
        }
        else
        {
            keyboardPanel.PointToShift = false;
            keyboardPanel.ClearHighlight();
            lblHint.Text = "Next key: ?";
        }
    }

    private void CompleteLesson()
    {
        if (_completed || _timedOut)
        {
            return;
        }

        _completed = true;
        bool passed = _comparison.ErrorCount == 0;
        _lessonPassed = passed;
        _lessonClock.Stop();

        int secondsLeft = Math.Max(
            0,
            (int)Math.Ceiling(_lessonLimitSeconds - _lessonClock.Elapsed.TotalSeconds));

        _stats.Update(_comparison.CorrectCount, _comparison.TotalTyped);
        _stats.StopTimer();
        UpdateStats();
        UpdateNextKey();

        btnNext.Enabled = passed;
        btnStart.Enabled = false;
        txtInput.ReadOnly = true;
        lblTimer.Text = FormatSeconds(secondsLeft);

        if (passed)
        {
            LessonProgressStore.SaveLastCompleted(_lessonIndex + 1);
            _practiceSessions++;
            _homeView?.SetProgress(LessonProgressStore.LoadLastCompleted(), _practiceSessions);
            lblTimer.ForeColor = Color.FromArgb(207, 250, 254);
            lblHint.Text = "Lesson complete! Press Next lesson.";
            lblStatus.ForeColor = SuccessColor;
            lblStatus.Text =
                $"Passed with {secondsLeft}s left  ·  {Math.Round(_stats.Accuracy)}% accuracy  ·  " +
                $"{Math.Round(_stats.WordsPerMinute)} WPM";
        }
        else
        {
            lblTimer.ForeColor = Color.FromArgb(254, 205, 211);
            lblHint.Text = "Mistakes found. Choose Try again on the results screen.";
            lblStatus.ForeColor = WrongColor;
            lblStatus.Text =
                $"{_comparison.ErrorCount} wrong character(s). " +
                "Choose Try again on the results screen.";
        }

        try
        {
            if (passed)
            {
                SystemSounds.Asterisk.Play();
            }
            else
            {
                SystemSounds.Hand.Play();
            }
        }
        catch
        {
            // Sound is a nicety; never let it break completion.
        }

        QueueResults();
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_completed || _timedOut)
        {
            txtInput.Focus();
            return;
        }

        StartCountdown();
        _stats.StartTimer();
        txtInput.Focus();
        lblStatus.ForeColor = StatusColor;
        lblStatus.Text = "Timer started – type the line above.";
    }

    private void BtnReset_Click(object? sender, EventArgs e) => LoadLesson(_lessonIndex);

    /// <summary>
    /// Returns to the first lesson. Saved progress is left intact until the
    /// learner passes a lesson, so this is a navigation shortcut, not a reset.
    /// </summary>
    private void BtnLessonOne_Click(object? sender, EventArgs e)
    {
        LoadLesson(0);
        txtInput.Focus();
    }

    private void BtnNext_Click(object? sender, EventArgs e)
    {
        if (!_lessonPassed)
        {
            return;
        }

        LoadLesson(_lessonIndex + 1);
    }

    /// <summary>
    /// Starts the per-lesson countdown once. The clock keeps running from the
    /// first keystroke, so the learner cannot stop it by clicking elsewhere.
    /// </summary>
    private void StartCountdown()
    {
        if (_countdownStarted || _completed || _timedOut)
        {
            return;
        }

        _countdownStarted = true;
        btnStart.Enabled = false;
        _lessonClock.Start();
    }

    /// <summary>
    /// Gives each lesson a readable time budget: a short fixed buffer plus one
    /// second per character, clamped so early drills stay quick and the long
    /// finale still has room to breathe.
    /// </summary>
    private static int CalculateLessonLimit(int length)
    {
        // Every lesson budget is 2.9x the original estimate.
        return Math.Clamp((int)Math.Round((8 + length) * 2.9), 58, 696);
    }

    private void HandleTimeout()
    {
        if (_timedOut)
        {
            return;
        }

        _timedOut = true;
        _lessonClock.Stop();
        _stats.StopTimer();
        _inputHandler?.Reset();

        txtInput.ReadOnly = true;
        btnNext.Enabled = false;
        btnStart.Enabled = false;

        keyboardPanel.PointToShift = false;
        keyboardPanel.ClearHighlight();
        lblTimer.Text = "00:00";
        lblTimer.ForeColor = Color.FromArgb(254, 205, 211);
        inputUnderline.BackColor = WrongColor;

        lblHint.Text = "Time is up – press Reset to try again.";
        lblStatus.ForeColor = WrongColor;
        lblStatus.Text = "Time is up. The line must be finished before the timer reaches zero.";

        try
        {
            SystemSounds.Hand.Play();
        }
        catch
        {
            // Sound is a nicety; never let it break the timeout.
        }

        QueueResults();
    }

    /// <summary>
    /// Queues the results screen once the current keystroke or timer tick has
    /// finished. Deferring with <see cref="BeginInvoke(Delegate)"/> matters:
    /// the final keystroke is recorded immediately after the text comparison,
    /// so showing the report too early would lose the last key.
    /// </summary>
    private void QueueResults()
    {
        if (!ResultsScreenEnabled ||
            _resultsShown ||
            !IsHandleCreated ||
            IsDisposed ||
            Disposing)
        {
            return;
        }

        _resultsShown = true;
        BeginInvoke(new Action(ShowResults));
    }

    /// <summary>
    /// Swaps the typing layout for the in-window results screen. The report is
    /// shown inside the main window so the app never opens a second window.
    /// </summary>
    private void ShowResults()
    {
        if (!ResultsScreenEnabled ||
            IsDisposed ||
            Disposing ||
            _lessonManager is null ||
            _lessonManager.Lessons.Count == 0)
        {
            return;
        }

        Lesson lesson = _lessonManager.Lessons[_lessonIndex];
        LessonReport report = LessonReport.Build(
            lesson.Title,
            _lessonIndex + 1,
            _lessonManager.Lessons.Count,
            lesson.Text,
            _lessonPassed,
            _stats.Elapsed,
            _comparison.CorrectCount,
            _comparison.TotalTyped,
            _keystrokes);

        _resultsView?.Dispose();
        _resultsView = new ResultsView(report)
        {
            Dock = DockStyle.Fill
        };
        _resultsView.Confirmed += ResultsView_Confirmed;

        layoutRoot.Visible = false;
        contentPanel.Controls.Add(_resultsView);
        _resultsView.BringToFront();
        _resultsView.FocusChoice();
    }

    private void ResultsView_Confirmed(object? sender, ResultsChoice choice)
    {
        // The view raises the event from its own button or key handler, so the
        // swap happens after that call stack unwinds.
        BeginInvoke(new Action(() => FinishResults(choice)));
    }

    private void FinishResults(ResultsChoice choice)
    {
        if (_resultsView is null)
        {
            return;
        }

        _resultsView.Confirmed -= ResultsView_Confirmed;
        contentPanel.Controls.Remove(_resultsView);
        _resultsView.Dispose();
        _resultsView = null;
        layoutRoot.Visible = true;

        bool advance = choice == ResultsChoice.NextLesson && _lessonPassed;
        LoadLesson(advance ? _lessonIndex + 1 : _lessonIndex);
        txtInput.Focus();
    }

    private void UiTimer_Tick(object? sender, EventArgs e)
    {
        if (_stats.IsRunning)
        {
            _stats.Update(_comparison.CorrectCount, _comparison.TotalTyped);
            UpdateStats();
        }

        if (_countdownStarted && !_completed && !_timedOut)
        {
            double remaining = _lessonLimitSeconds - _lessonClock.Elapsed.TotalSeconds;
            if (remaining <= 0d)
            {
                HandleTimeout();
            }
            else
            {
                lblTimer.Text = FormatSeconds((int)Math.Ceiling(remaining));
                lblTimer.ForeColor = remaining <= 10d
                    ? Color.FromArgb(254, 205, 211)
                    : Color.White;
            }
        }

        _pulse += 0.16;
        if (_pulse > Math.PI * 2)
        {
            _pulse -= Math.PI * 2;
        }

        keyboardPanel.Pulse = (Math.Sin(_pulse) + 1d) / 2d;
    }

    private static string FormatSeconds(int seconds)
    {
        seconds = Math.Max(0, seconds);
        return TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
    }

    private void HeaderPanel_Paint(object? sender, PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        Rectangle header = headerPanel.ClientRectangle;
        if (header.Width <= 0 || header.Height <= 0)
        {
            return;
        }

        using (var gradient = new LinearGradientBrush(
                   header,
                   Color.FromArgb(79, 70, 229),
                   Color.FromArgb(6, 182, 212),
                   12f))
        {
            graphics.FillRectangle(gradient, header);
        }

        using (var decoration = new SolidBrush(Color.FromArgb(18, 255, 255, 255)))
        {
            graphics.FillEllipse(decoration, header.Width - 270, -120, 260, 260);
            graphics.FillEllipse(decoration, header.Width - 150, 26, 180, 180);
            graphics.FillEllipse(decoration, header.Width - 520, 60, 120, 120);
        }

        Rectangle badge = new(20, 16, 64, 64);
        using (GraphicsPath badgePath = RoundedPanel.CreateRoundedPath(badge, 16))
        using (var badgeFill = new SolidBrush(Color.FromArgb(42, 255, 255, 255)))
        using (var badgeBorder = new Pen(Color.FromArgb(90, 255, 255, 255), 1.2f))
        {
            graphics.FillPath(badgeFill, badgePath);
            graphics.DrawPath(badgeBorder, badgePath);
        }

        if (_logoImage is not null)
        {
            GraphicsState state = graphics.Save();
            try
            {
                using GraphicsPath clip = RoundedPanel.CreateRoundedPath(badge, 16);
                graphics.SetClip(clip);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(
                    _logoImage,
                    new Rectangle(badge.X, badge.Y, badge.Width, badge.Height),
                    new Rectangle(0, 0, _logoImage.Width, _logoImage.Height),
                    GraphicsUnit.Pixel);
            }
            finally
            {
                graphics.Restore(state);
            }
        }
        else
        {
            using var brandFont = new Font("Times New Roman", 16f, FontStyle.Bold);
            TextRenderer.DrawText(
                graphics,
                "RT",
                brandFont,
                badge,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }

        using (var divider = new Pen(Color.FromArgb(34, 255, 255, 255), 1f))
        {
            graphics.DrawLine(divider, 0, header.Height - 1, header.Width, header.Height - 1);
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            uiTimer.Stop();
            uiTimer.Dispose();

            components?.Dispose();

            _inputHandler?.Reset();
            _inputHandler = null;

            _stats.StopTimer();

            // Dispose child controls first, then the fonts they were using.
            base.Dispose(disposing);

            _fontLoader?.Dispose();
            _fontLoader = null;

            _logoImage?.Dispose();
            _logoImage = null;

            _targetFont = null;
            _inputFont = null;
            _keyboardFont = null;
            return;
        }

        base.Dispose(disposing);
    }
}
