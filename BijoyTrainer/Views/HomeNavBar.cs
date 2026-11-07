using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BijoyTrainer.Controls;

namespace BijoyTrainer.Views;

/// <summary>
/// A horizontal navigation strip with one rounded button per dashboard page.
/// </summary>
public sealed class HomeNavBar : Panel
{
    private static readonly Color InactiveBase = Color.FromArgb(228, 230, 243);
    private static readonly Color InactiveHover = Color.FromArgb(214, 216, 234);
    private static readonly Color InactivePressed = Color.FromArgb(186, 192, 220);
    private static readonly Color InactiveText = Color.FromArgb(48, 46, 84);

    private readonly List<AccentButton> _ordered = new();
    private readonly Dictionary<string, AccentButton> _byPage = new(StringComparer.OrdinalIgnoreCase);
    private readonly Font _navFont;

    /// <summary>Initializes a new instance of the <see cref="HomeNavBar"/> class.</summary>
    public HomeNavBar()
    {
        Height = 46;
        BackColor = Theme.Page;
        Margin = Padding.Empty;
        Padding = Padding.Empty;

        _navFont = Theme.CreateFont(9f, FontStyle.Bold);

        AddButton("dashboard", "⌂ Dashboard", 128);
        AddButton("lessons", "▤ Lessons", 112);
        AddButton("achievements", "♛ Achievements", 136);
        AddButton("practice", "⌨ Quick Practice", 148);

        SetActive(string.Empty);
        LayoutButtons();
    }

    /// <summary>
    /// Raised when the user selects a page. The argument is the page key
    /// (<c>dashboard</c>, <c>lessons</c>, <c>achievements</c> or <c>practice</c>).
    /// </summary>
    public event EventHandler<string>? PageRequested;

    /// <summary>
    /// Highlights the button that belongs to <paramref name="page"/> and resets
    /// every other button to the inactive style.
    /// </summary>
    /// <param name="page">Page key to activate; an unknown key deactivates all buttons.</param>
    public void SetActive(string page)
    {
        foreach (KeyValuePair<string, AccentButton> pair in _byPage)
        {
            bool active = string.Equals(pair.Key, page, StringComparison.OrdinalIgnoreCase);
            AccentButton button = pair.Value;

            if (active)
            {
                button.BaseColor = Theme.Accent;
                button.HoverColor = Theme.AccentDark;
                button.PressedColor = Theme.AccentDark;
                button.TextColor = Color.White;
            }
            else
            {
                button.BaseColor = InactiveBase;
                button.HoverColor = InactiveHover;
                button.PressedColor = InactivePressed;
                button.TextColor = InactiveText;
            }

            button.Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);

        if (!IsDisposed && _ordered is not null)
        {
            LayoutButtons();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _navFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void AddButton(string page, string text, int width)
    {
        var button = new AccentButton
        {
            Text = text,
            Width = width,
            Height = 34,
            Font = _navFont,
            CornerRadius = 10,
            TabStop = false,
        };

        button.Click += (_, _) => PageRequested?.Invoke(this, page);

        _ordered.Add(button);
        _byPage[page] = button;
        Controls.Add(button);
    }

    private void LayoutButtons()
    {
        int x = 0;

        foreach (AccentButton button in _ordered)
        {
            button.Location = new Point(x, 6);
            button.Size = new Size(button.Width, 34);
            x += button.Width + 8;
        }
    }
}
