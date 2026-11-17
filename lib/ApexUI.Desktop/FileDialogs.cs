using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ApexUI.Desktop;

/// Native open/save dialogs without extra packages: the Win32 common dialogs on Windows,
/// AppleScript on macOS, and zenity or kdialog on Linux. Results complete off the UI thread;
/// DesktopPlatform's synchronization context brings awaiting code back to it.
internal static partial class FileDialogs
{
    public static Task<string?> OpenAsync(FileDialogOptions options, nint owner)
    {
        if (OperatingSystem.IsWindows()) return ShowOnStaThread(options, owner, save: false);
        if (OperatingSystem.IsMacOS())   return RunProcessAsync("osascript", ["-e", AppleScript(options, save: false)], options, save: false);
        return LinuxAsync(options, save: false);
    }

    public static Task<string?> SaveAsync(FileDialogOptions options, nint owner)
    {
        if (OperatingSystem.IsWindows()) return ShowOnStaThread(options, owner, save: true);
        if (OperatingSystem.IsMacOS())   return RunProcessAsync("osascript", ["-e", AppleScript(options, save: true)], options, save: true);
        return LinuxAsync(options, save: true);
    }

    // The shell parts of the common dialogs need a single-threaded apartment, which the UI thread is not.
    // A separate thread also keeps the window drawing while the dialog is open; the dialog disables it for input.
    [SupportedOSPlatform("windows")]
    private static Task<string?> ShowOnStaThread(FileDialogOptions options, nint owner, bool save)
    {
        var result = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try   { result.SetResult(Win32FileDialog.Show(options, owner, save)); }
            catch (Exception e) { result.SetException(e); }
        }) { IsBackground = true, Name = "File dialog" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return result.Task;
    }

    private static async Task<string?> LinuxAsync(FileDialogOptions options, bool save)
    {
        if (await HasCommandAsync("zenity")) return await RunProcessAsync("zenity", ZenityArgs(options, save), options, save);
        if (await HasCommandAsync("kdialog")) return await RunProcessAsync("kdialog", KDialogArgs(options, save), options, save);
        throw new NotSupportedException("File dialogs need zenity or kdialog on Linux.");
    }

    private static async Task<bool> HasCommandAsync(string name)
    {
        try
        {
            using var which = Process.Start(new ProcessStartInfo("which", name) { RedirectStandardOutput = true, UseShellExecute = false });
            if (which is null) return false;
            await which.WaitForExitAsync();
            return which.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    // Exit code 0 with a path is a choice; anything else (1 for cancel in all three tools) is no choice.
    private static async Task<string?> RunProcessAsync(string fileName, List<string> args, FileDialogOptions options, bool save)
    {
        var info = new ProcessStartInfo(fileName) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var arg in args) info.ArgumentList.Add(arg);

        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        string path = output.Trim();
        if (process.ExitCode != 0 || path.Length == 0) return null;
        return save ? WithDefaultExtension(path, options) : path;
    }

    private static string WithDefaultExtension(string path, FileDialogOptions options)
        => Path.HasExtension(path) || DefaultExtension(options) is not { } ext ? path : $"{path}.{ext}";

    private static string? DefaultExtension(FileDialogOptions options)
        => options.Filters.Count > 0 && options.Filters[0].Extensions.Count > 0 ? options.Filters[0].Extensions[0] : null;

    private static List<string> ZenityArgs(FileDialogOptions options, bool save)
    {
        var args = new List<string> { "--file-selection", $"--title={options.Title}" };
        if (save)
        {
            args.Add("--save");
            args.Add("--confirm-overwrite");
            if (options.SuggestedFileName is { } name) args.Add($"--filename={name}");
        }
        foreach (var filter in options.Filters)
            args.Add($"--file-filter={filter.Name} | {Patterns(filter, " ")}");
        return args;
    }

    private static List<string> KDialogArgs(FileDialogOptions options, bool save)
    {
        string filters = string.Join("\n", options.Filters.Select(f => $"{Patterns(f, " ")}|{f.Name}"));
        return
        [
            "--title", options.Title,
            save ? "--getsavefilename" : "--getopenfilename",
            save && options.SuggestedFileName is { } name ? name : ".",
            filters,
        ];
    }

    private static string AppleScript(FileDialogOptions options, bool save)
    {
        string prompt = Quote(options.Title);
        if (save)
        {
            string name = options.SuggestedFileName is { } n ? $" default name {Quote(n)}" : "";
            return $"POSIX path of (choose file name with prompt {prompt}{name})";
        }

        var types = options.Filters.SelectMany(f => f.Extensions).Where(e => e != "*").Select(Quote).ToList();
        string ofType = types.Count > 0 ? $" of type {{{string.Join(", ", types)}}}" : "";
        return $"POSIX path of (choose file with prompt {prompt}{ofType})";
    }

    private static string Quote(string text) => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static string Patterns(FileFilter filter, string separator)
        => string.Join(separator, filter.Extensions.Select(e => e == "*" ? "*" : $"*.{e}"));

    [SupportedOSPlatform("windows")]
    private static partial class Win32FileDialog
    {
        private const int MaxPath = 32_768;
        private const int OfnOverwritePrompt = 0x2;
        private const int OfnHideReadOnly    = 0x4;
        private const int OfnNoChangeDir     = 0x8;
        private const int OfnPathMustExist   = 0x800;
        private const int OfnFileMustExist   = 0x1000;
        private const int OfnExplorer        = 0x80000;

        public static string? Show(FileDialogOptions options, nint owner, bool save)
        {
            nint filter = Marshal.StringToHGlobalUni(FilterString(options));
            nint title  = Marshal.StringToHGlobalUni(options.Title);
            nint defExt = DefaultExtension(options) is { } ext ? Marshal.StringToHGlobalUni(ext) : 0;
            nint file   = Marshal.AllocHGlobal(MaxPath * sizeof(char));
            try
            {
                string initial = save ? options.SuggestedFileName ?? "" : "";
                var buffer = new char[MaxPath];
                initial.AsSpan(0, Math.Min(initial.Length, MaxPath - 1)).CopyTo(buffer);
                Marshal.Copy(buffer, 0, file, MaxPath);

                var ofn = new OpenFileName
                {
                    StructSize  = Marshal.SizeOf<OpenFileName>(),
                    Owner       = owner,
                    Filter      = filter,
                    FilterIndex = 1,
                    File        = file,
                    MaxFile     = MaxPath,
                    Title       = title,
                    DefExt      = defExt,
                    Flags       = OfnExplorer | OfnNoChangeDir | OfnPathMustExist | OfnHideReadOnly
                                | (save ? OfnOverwritePrompt : OfnFileMustExist),
                };

                bool chosen = save ? GetSaveFileName(ref ofn) : GetOpenFileName(ref ofn);
                return chosen ? Marshal.PtrToStringUni(file) : null;
            }
            finally
            {
                Marshal.FreeHGlobal(filter);
                Marshal.FreeHGlobal(title);
                if (defExt != 0) Marshal.FreeHGlobal(defExt);
                Marshal.FreeHGlobal(file);
            }
        }

        // "Name\0*.a;*.b\0Name2\0*.c\0\0": StringToHGlobalUni adds the last terminator.
        private static string FilterString(FileDialogOptions options)
        {
            string text = "";
            foreach (var filter in options.Filters)
                text += $"{filter.Name}\0{Patterns(filter, ";")}\0";
            return text.Length == 0 ? "All files\0*.*\0" : text;
        }

        [LibraryImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetOpenFileName(ref OpenFileName ofn);

        [LibraryImport("comdlg32.dll", EntryPoint = "GetSaveFileNameW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetSaveFileName(ref OpenFileName ofn);

        [StructLayout(LayoutKind.Sequential)]
        private struct OpenFileName
        {
            public int   StructSize;
            public nint  Owner;
            public nint  Instance;
            public nint  Filter;
            public nint  CustomFilter;
            public int   MaxCustomFilter;
            public int   FilterIndex;
            public nint  File;
            public int   MaxFile;
            public nint  FileTitle;
            public int   MaxFileTitle;
            public nint  InitialDir;
            public nint  Title;
            public int   Flags;
            public short FileOffset;
            public short FileExtension;
            public nint  DefExt;
            public nint  CustomData;
            public nint  Hook;
            public nint  TemplateName;
            public nint  Reserved;
            public int   ReservedInt;
            public int   FlagsEx;
        }
    }
}
