using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace ReadyTypingSetup;

/// <summary>A single progress report from the install engine.</summary>
internal sealed class InstallProgress
{
    public InstallProgress(double fraction, string message)
    {
        Fraction = fraction;
        Message = message;
    }

    /// <summary>Gets the overall completion, from 0 to 1.</summary>
    public double Fraction { get; }

    /// <summary>Gets the human-readable status line.</summary>
    public string Message { get; }
}

/// <summary>The outcome of an installation attempt.</summary>
internal sealed class InstallResult
{
    private InstallResult(bool success, string executablePath, string? error)
    {
        Success = success;
        ExecutablePath = executablePath;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the app was installed.</summary>
    public bool Success { get; }

    /// <summary>Gets the full path of the installed executable.</summary>
    public string ExecutablePath { get; }

    /// <summary>Gets the user-facing error text, or <c>null</c> on success.</summary>
    public string? Error { get; }

    public static InstallResult Ok(string executablePath) =>
        new(true, executablePath, null);

    public static InstallResult Fail(string error) =>
        new(false, string.Empty, error);
}

/// <summary>
/// Installs the embedded, gzip-compressed Ready Typing executable and creates
/// the requested shortcuts. The engine never requires administrator rights:
/// everything is written below the current user's profile.
/// </summary>
internal static class InstallEngine
{
    /// <summary>The logical name of the embedded application payload.</summary>
    public const string PayloadResourceName = "ReadyTypingSetup.Payload.ReadyTyping.exe.gz";

    /// <summary>The logical name of the embedded shortcut icon.</summary>
    public const string IconResourceName = "ReadyTypingSetup.Resources.app.ico";

    /// <summary>The default per-user installation folder.</summary>
    public static string DefaultInstallDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Ready Typing");

    /// <summary>The executable file name inside the installation folder.</summary>
    public const string ExecutableName = "ReadyTyping.exe";

    /// <summary>
    /// Installs the application, reporting progress as it decompresses the
    /// payload and creates shortcuts.
    /// </summary>
    /// <param name="targetDirectory">Folder that receives the executable.</param>
    /// <param name="desktopShortcut">Whether to create a Desktop shortcut.</param>
    /// <param name="startMenuShortcut">Whether to create a Start menu shortcut.</param>
    /// <param name="progress">Optional progress sink; must be thread-safe.</param>
    /// <param name="cancellation">Token used to abort a long copy.</param>
    /// <returns>The installation result.</returns>
    public static InstallResult Install(
        string targetDirectory,
        bool desktopShortcut,
        bool startMenuShortcut,
        IProgress<InstallProgress>? progress,
        CancellationToken cancellation)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            return InstallResult.Fail("The install folder is empty.");
        }

        string fullTarget = Path.GetFullPath(targetDirectory);
        string executable = Path.Combine(fullTarget, ExecutableName);
        string temporary = Path.Combine(fullTarget, ExecutableName + ".new");

        try
        {
            cancellation.ThrowIfCancellationRequested();

            progress?.Report(new InstallProgress(0.02, "Creating the install folder..."));
            Directory.CreateDirectory(fullTarget);

            TryDelete(temporary);
            ExtractPayload(temporary, progress, cancellation);

            cancellation.ThrowIfCancellationRequested();
            progress?.Report(new InstallProgress(0.88, "Finishing the installation..."));

            if (File.Exists(executable))
            {
                File.SetAttributes(executable, FileAttributes.Normal);
                File.Delete(executable);
            }

            File.Move(temporary, executable);

            progress?.Report(new InstallProgress(0.9, "Writing the shortcut icon..."));
            string shortcutIcon = InstallShortcutIcon(fullTarget);

            if (desktopShortcut)
            {
                progress?.Report(new InstallProgress(0.93, "Adding a Desktop shortcut..."));
                CreateShortcut(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    executable,
                    fullTarget,
                    shortcutIcon);
            }

            if (startMenuShortcut)
            {
                progress?.Report(new InstallProgress(0.97, "Adding a Start menu shortcut..."));
                CreateShortcut(
                    Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                    executable,
                    fullTarget,
                    shortcutIcon);
            }

            NotifyShell();
            progress?.Report(new InstallProgress(1d, "Ready Typing is installed."));
            return InstallResult.Ok(executable);
        }
        catch (OperationCanceledException)
        {
            TryDelete(temporary);
            return InstallResult.Fail("The installation was cancelled.");
        }
        catch (UnauthorizedAccessException)
        {
            TryDelete(temporary);
            return InstallResult.Fail(
                "Windows did not allow writing to this folder. Choose another install " +
                "location, or close Ready Typing if it is already running.");
        }
        catch (IOException error)
        {
            TryDelete(temporary);
            return InstallResult.Fail(
                "The files could not be written. Close Ready Typing if it is running, " +
                "then try again.\n\n" + error.Message);
        }
        catch (Exception error)
        {
            TryDelete(temporary);
            return InstallResult.Fail("The installation could not finish.\n\n" + error.Message);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }
        catch
        {
            // A leftover .new file is harmless; never hide the real failure.
        }
    }

    /// <summary>Starts the installed executable.</summary>
    /// <param name="executablePath">Full path of the installed app.</param>
    /// <returns><c>true</c> when the process started.</returns>
    public static bool Launch(string executablePath)
    {
        try
        {
            var start = new ProcessStartInfo(executablePath)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty
            };

            Process.Start(start);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ExtractPayload(
        string destination,
        IProgress<InstallProgress>? progress,
        CancellationToken cancellation)
    {
        using Stream? payload = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(PayloadResourceName);

        if (payload is null)
        {
            throw new InvalidOperationException(
                "The installer payload is missing. Rebuild the setup executable.");
        }

        long total = ReadUncompressedLength(payload);

        using var compressed = new GZipStream(payload, CompressionMode.Decompress, leaveOpen: false);
        using var output = new FileStream(
            destination,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            128 * 1024,
            FileOptions.SequentialScan);

        var buffer = new byte[128 * 1024];
        long written = 0;
        int lastPercent = -1;

        while (true)
        {
            cancellation.ThrowIfCancellationRequested();
            int read = compressed.Read(buffer, 0, buffer.Length);
            if (read <= 0)
            {
                break;
            }

            output.Write(buffer, 0, read);
            written += read;

            if (total > 0)
            {
                int percent = (int)Math.Min(85d, 5d + written * 80d / total);
                if (percent != lastPercent)
                {
                    lastPercent = percent;
                    progress?.Report(new InstallProgress(
                        percent / 100d,
                        $"Unpacking Ready Typing... {percent}%"));
                }
            }
        }

        output.Flush(true);
    }

    /// <summary>
    /// Reads the gzip ISIZE trailer so the progress bar can show a real
    /// percentage; returns 0 when the stream cannot be inspected.
    /// </summary>
    private static long ReadUncompressedLength(Stream payload)
    {
        try
        {
            if (!payload.CanSeek || payload.Length < 18)
            {
                return 0;
            }

            long position = payload.Position;
            payload.Seek(-4, SeekOrigin.End);
            var size = new byte[4];
            int read = payload.Read(size, 0, 4);
            payload.Seek(position, SeekOrigin.Begin);

            if (read != 4)
            {
                return 0;
            }

            return (uint)(size[0] | (size[1] << 8) | (size[2] << 16) | (size[3] << 24));
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Writes the neon shortcut icon next to the executable. The file name
    /// carries the version so that Windows refreshes its icon cache after an
    /// update: the shell keys cached icons by path, and a replaced executable
    /// at the same path can keep showing the previous artwork.
    /// </summary>
    /// <param name="folder">The installation folder.</param>
    /// <returns>The icon path, or an empty string when the resource is missing.</returns>
    private static string InstallShortcutIcon(string folder)
    {
        string version = typeof(InstallEngine).Assembly.GetName().Version?.ToString(2) ?? "1.0";
        string iconPath = Path.Combine(folder, "ReadyTyping-" + version + ".ico");

        foreach (string stale in Directory.GetFiles(folder, "ReadyTyping*.ico"))
        {
            if (!string.Equals(stale, iconPath, StringComparison.OrdinalIgnoreCase))
            {
                TryDelete(stale);
            }
        }

        using Stream? source = typeof(InstallEngine).Assembly
            .GetManifestResourceStream(IconResourceName);
        if (source is null)
        {
            return string.Empty;
        }

        using FileStream target = File.Create(iconPath);
        source.CopyTo(target);
        return iconPath;
    }

    /// <summary>Tells the shell to rebuild its icon cache after an update.</summary>
    private static void NotifyShell()
    {
        try
        {
            SHChangeNotify(
                0x08000000, // SHCNE_ASSOCCHANGED
                0x3000,     // SHCNF_IDLIST | SHCNF_FLUSHNOWAIT
                IntPtr.Zero,
                IntPtr.Zero);
        }
        catch
        {
            // A stale icon is cosmetic; never fail the installation for it.
        }
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

    private static void CreateShortcut(
        string folder,
        string executable,
        string workingDirectory,
        string iconPath)
    {
        Directory.CreateDirectory(folder);

        string shortcutPath = Path.Combine(folder, "Ready Typing.lnk");
        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
        {
            throw new InvalidOperationException("Windows Script Host is not available.");
        }

        object shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows Script Host could not start.");

        try
        {
            dynamic wsh = shell;
            dynamic shortcut = wsh.CreateShortcut(shortcutPath);
            shortcut.TargetPath = executable;
            shortcut.WorkingDirectory = workingDirectory;
            shortcut.IconLocation = string.IsNullOrEmpty(iconPath)
                ? executable + ",0"
                : iconPath + ",0";
            shortcut.Description = "Ready Typing - Bengali Bijoy typing trainer";
            shortcut.Save();
        }
        finally
        {
            if (shell is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
