using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BijoyTrainer.Core;

/// <summary>
/// Loads the bundled SutonnyMJ font directly from the assembly without
/// installing it into the Windows font folder.
/// </summary>
/// <remarks>
/// The font bytes are copied into unmanaged memory because
/// <see cref="PrivateFontCollection.AddMemoryFont"/> keeps a pointer into that
/// buffer for the lifetime of the collection. All <see cref="Font"/> objects
/// created by this loader are tracked so they can be disposed before the
/// unmanaged buffer is released.
/// </remarks>
public sealed class FontLoader : IDisposable
{
    /// <summary>The manifest resource name of the embedded SutonnyMJ font.</summary>
    public const string FontResourceName = "BijoyTrainer.Resources.SutonnyMJ.ttf";

    private readonly PrivateFontCollection _collection = new();
    private readonly List<Font> _createdFonts = new();

    private IntPtr _fontMemory = IntPtr.Zero;
    private IntPtr _gdiFontHandle = IntPtr.Zero;
    private FontFamily? _family;
    private string _familyName = "Segoe UI";
    private bool _disposed;

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr AddFontMemResourceEx(
        IntPtr fileBuffer,
        uint size,
        IntPtr reserved,
        out uint fontCount);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveFontMemResourceEx(IntPtr handle);

    private FontLoader()
    {
    }

    /// <summary>Gets the family name actually available to the application.</summary>
    public string FamilyName => _familyName;

    /// <summary>
    /// Gets a value indicating whether the embedded SutonnyMJ font was loaded.
    /// When <c>false</c>, <see cref="CreateFont"/> falls back to a system font.
    /// </summary>
    public bool IsEmbeddedFontLoaded => _family is not null;

    /// <summary>
    /// Creates a loader, reading the font from the executing assembly.
    /// </summary>
    /// <param name="error">
    /// A user-friendly error message when the embedded font could not be loaded;
    /// otherwise <c>null</c>. The returned loader is always usable.
    /// </param>
    /// <returns>A usable <see cref="FontLoader"/>.</returns>
    public static FontLoader Create(out string? error)
    {
        var loader = new FontLoader();
        error = null;

        try
        {
            using Stream? stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(FontResourceName);

            if (stream is null)
            {
                error = "The embedded SutonnyMJ font (BijoyTrainer.Resources.SutonnyMJ.ttf) " +
                        "was not found. The application will use a default font, so Bengali " +
                        "text may not display correctly.";
                return loader;
            }

            byte[] bytes;
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                bytes = buffer.ToArray();
            }

            if (bytes.Length == 0)
            {
                error = "The embedded SutonnyMJ font is empty. A default font will be used.";
                return loader;
            }

            loader._fontMemory = Marshal.AllocCoTaskMem(bytes.Length);
            Marshal.Copy(bytes, 0, loader._fontMemory, bytes.Length);
            loader._collection.AddMemoryFont(loader._fontMemory, bytes.Length);

            // PrivateFontCollection is enough for GDI+ drawing, but the Win32
            // edit controls (TextBox/RichTextBox) resolve their font through
            // GDI. Registering the same bytes process-privately makes the
            // family selectable there too, without touching the system fonts
            // folder or requiring administrator rights.
            loader._gdiFontHandle = AddFontMemResourceEx(
                loader._fontMemory,
                (uint)bytes.Length,
                IntPtr.Zero,
                out _);

            if (loader._collection.Families.Length == 0)
            {
                throw new InvalidOperationException("The font contains no usable font families.");
            }

            loader._family = loader._collection.Families[0];
            loader._familyName = loader._family.Name;
        }
        catch (Exception ex)
        {
            loader.ReleaseGdiRegistration();
            loader.ReleaseUnmanagedMemory();
            loader._family = null;
            loader._familyName = "Segoe UI";
            error = "The embedded SutonnyMJ font could not be loaded. " +
                    $"A default font will be used instead.{Environment.NewLine}{Environment.NewLine}{ex.Message}";
        }

        return loader;
    }

    /// <summary>
    /// Creates a tracked <see cref="Font"/> using the embedded font family.
    /// </summary>
    /// <param name="size">Font size in points.</param>
    /// <param name="style">Optional font style.</param>
    /// <returns>A font owned by this loader.</returns>
    public Font CreateFont(float size, FontStyle style = FontStyle.Regular)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(FontLoader));
        }

        Font font;
        try
        {
            if (_family is not null)
            {
                font = new Font(_family, size, style, GraphicsUnit.Point);
            }
            else
            {
                font = new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
            }
        }
        catch (ArgumentException)
        {
            font = new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
        }

        _createdFonts.Add(font);
        return font;
    }

    /// <summary>
    /// Disposes every font created by this loader and then frees the unmanaged
    /// font buffer. No font returned by <see cref="CreateFont"/> may be used
    /// after this call.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (Font font in _createdFonts)
        {
            font.Dispose();
        }

        _createdFonts.Clear();
        _collection.Dispose();
        ReleaseGdiRegistration();
        ReleaseUnmanagedMemory();
        _family = null;
    }

    private void ReleaseGdiRegistration()
    {
        if (_gdiFontHandle == IntPtr.Zero)
        {
            return;
        }

        RemoveFontMemResourceEx(_gdiFontHandle);
        _gdiFontHandle = IntPtr.Zero;
    }

    private void ReleaseUnmanagedMemory()
    {
        if (_fontMemory == IntPtr.Zero)
        {
            return;
        }

        Marshal.FreeCoTaskMem(_fontMemory);
        _fontMemory = IntPtr.Zero;
    }
}
