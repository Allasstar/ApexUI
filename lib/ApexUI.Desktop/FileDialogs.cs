using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;

namespace ApexUI.Desktop;

/// Native open/save dialogs without extra packages: the Win32 common dialogs on Windows,
/// AppleScript on macOS, and zenity or kdialog on Linux. Results complete off the UI thread;
/// DesktopPlatform's synchronization context brings awaiting code back to it.
internal static partial class FileDialogs
{
    public static Task<string?> OpenAsync(FileDialogOptions options, nint owner)
    {
        if (OperatingSystem.IsWindows()) return FileOnStaThread(options, owner, save: false);
        if (OperatingSystem.IsMacOS())   return RunProcessAsync("osascript", ["-e", AppleScript(options, save: false)]);
        return LinuxAsync(ZenityArgs(options, save: false), KDialogArgs(options, save: false));
    }

    public static async Task<string?> SaveAsync(FileDialogOptions options, nint owner)
    {
        if (OperatingSystem.IsWindows()) return await FileOnStaThread(options, owner, save: true);
        string? path = OperatingSystem.IsMacOS()
            ? await RunProcessAsync("osascript", ["-e", AppleScript(options, save: true)])
            : await LinuxAsync(ZenityArgs(options, save: true), KDialogArgs(options, save: true));
        return path is null ? null : WithDefaultExtension(path, options);
    }

    public static Task<string?> PickFolderAsync(string title, nint owner)
    {
        if (OperatingSystem.IsWindows()) return FolderOnStaThread(title, owner);
        if (OperatingSystem.IsMacOS())   return RunProcessAsync("osascript", ["-e", $"POSIX path of (choose folder with prompt {Quote(title)})"]);
        return LinuxAsync(["--file-selection", "--directory", $"--title={title}"], ["--title", title, "--getexistingdirectory", "."]);
    }

    [SupportedOSPlatform("windows")]
    private static Task<string?> FileOnStaThread(FileDialogOptions options, nint owner, bool save) =>
        OnStaThread(() => Win32FileDialog.Show(options, owner, save));

    [SupportedOSPlatform("windows")]
    private static Task<string?> FolderOnStaThread(string title, nint owner) => OnStaThread(() => Win32FolderDialog.Show(title, owner));

    // The shell parts of the common dialogs need a single-threaded apartment, which the UI thread is not.
    // A separate thread also keeps the window drawing while the dialog is open; the dialog disables it for input.
    [SupportedOSPlatform("windows")]
    private static Task<string?> OnStaThread(Func<string?> show)
    {
        var result = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try   { result.SetResult(show()); }
            catch (Exception e) { result.SetException(e); }
        }) { IsBackground = true, Name = "File dialog" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return result.Task;
    }

    private static async Task<string?> LinuxAsync(List<string> zenityArgs, List<string> kdialogArgs)
    {
        if (await HasCommandAsync("zenity")) return await RunProcessAsync("zenity", zenityArgs);
        if (await HasCommandAsync("kdialog")) return await RunProcessAsync("kdialog", kdialogArgs);
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
    private static async Task<string?> RunProcessAsync(string fileName, List<string> args)
    {
        var info = new ProcessStartInfo(fileName) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var arg in args) info.ArgumentList.Add(arg);

        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {fileName}.");
        string output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        string path = output.Trim();
        return process.ExitCode != 0 || path.Length == 0 ? null : path;
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
    // The folder picker of Vista and later: the common file open dialog in folder mode, through COM.
    [SupportedOSPlatform("windows")]
    internal static partial class Win32FolderDialog
    {
        private const int  ClsctxInprocServer = 1;
        private const uint FosNoChangeDir     = 0x8;
        private const uint FosPickFolders     = 0x20;
        private const uint FosForceFileSystem = 0x40;
        private const uint SigdnFileSysPath   = 0x80058000;
        private const int  ErrorCancelled     = unchecked((int)0x800704C7);

        private static readonly Guid FileOpenDialog = new("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
        private static readonly Guid FileDialogIid  = new("42F85136-DB7E-439C-85F1-E4075D135FC8");

        public static string? Show(string title, nint owner)
        {
            Marshal.ThrowExceptionForHR(CoCreateInstance(in FileOpenDialog, 0, ClsctxInprocServer, in FileDialogIid, out nint unknown));
            IFileDialog dialog;
            try     { dialog = (IFileDialog)new StrategyBasedComWrappers().GetOrCreateObjectForComInstance(unknown, CreateObjectFlags.None); }
            finally { Marshal.Release(unknown); }

            dialog.GetOptions(out uint options);
            dialog.SetOptions(options | FosPickFolders | FosForceFileSystem | FosNoChangeDir);
            dialog.SetTitle(title);
            int shown = dialog.Show(owner);
            if (shown == ErrorCancelled) return null;
            Marshal.ThrowExceptionForHR(shown);

            dialog.GetResult(out IShellItem item);
            item.GetDisplayName(SigdnFileSysPath, out nint name);
            try     { return Marshal.PtrToStringUni(name); }
            finally { Marshal.FreeCoTaskMem(name); }
        }

        [LibraryImport("ole32.dll")]
        private static partial int CoCreateInstance(in Guid clsid, nint outer, int context, in Guid iid, out nint instance);

        // Methods up to the last one used, in vtable order; Show comes from IModalWindow.
        [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        internal partial interface IFileDialog
        {
            [PreserveSig] int Show(nint owner);
            void SetFileTypes(uint count, nint filterSpec);
            void SetFileTypeIndex(uint index);
            void GetFileTypeIndex(out uint index);
            void Advise(nint events, out uint cookie);
            void Unadvise(uint cookie);
            void SetOptions(uint options);
            void GetOptions(out uint options);
            void SetDefaultFolder(nint item);
            void SetFolder(nint item);
            void GetFolder(out nint item);
            void GetCurrentSelection(out nint item);
            void SetFileName(string name);
            void GetFileName(out nint name);
            void SetTitle(string title);
            void SetOkButtonLabel(string text);
            void SetFileNameLabel(string label);
            void GetResult(out IShellItem item);
        }

        [GeneratedComInterface]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        internal partial interface IShellItem
        {
            void BindToHandler(nint context, in Guid handler, in Guid iid, out nint result);
            void GetParent(out nint parent);
            void GetDisplayName(uint form, out nint name);
        }
    }
}
