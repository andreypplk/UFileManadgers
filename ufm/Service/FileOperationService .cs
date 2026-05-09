//08 05 2026

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Core_FileManagement;
using Microsoft.UI.Dispatching;

namespace ufm
{
    public class FileOperationService : IFileOperationService
    {
        // ---------- P/Invoke для SHFileOperation ----------
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        private const uint FO_COPY = 0x0002;
        private const uint FO_DELETE = 0x0003;
        private const uint FO_MOVE = 0x0001;

        private const ushort FOF_ALLOWUNDO = 0x0040;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_SIMPLEPROGRESS = 0x0100;
        private const ushort FOF_MULTIDESTFILES = 0x0200;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);
        // ------------------------------------------------

        private List<string> _sourcePaths = new();
        private bool _isCut;
        private IntPtr _parentHwnd = IntPtr.Zero;
        private DispatcherQueue _dispatcherQueue;

        public bool CanPaste => _sourcePaths.Count > 0;
        public event EventHandler ClipboardChanged;

        // ===================== Инициализация =====================
        public void SetParentWindow(IntPtr hwnd)
        {
            Debug.WriteLine($"[{nameof(SetParentWindow)}] Old hwnd: 0x{_parentHwnd:X}, new hwnd: 0x{hwnd:X}");
            _parentHwnd = hwnd;
        }

        public void Initialize(IntPtr parentHwnd, DispatcherQueue dispatcher)
        {
            Debug.WriteLine($"[{nameof(Initialize)}] hwnd=0x{parentHwnd:X}, dispatcher={(dispatcher != null ? "present" : "null")}");
            _parentHwnd = parentHwnd;
            _dispatcherQueue = dispatcher;
        }

        // ===================== Copy / Cut =====================
        public void Copy(IEnumerable<ExplorerItemViewModel> items)
        {
            Debug.WriteLine($"[{nameof(Copy)}] ENTER");
            Debug.WriteLine($"[{nameof(Copy)}] items null? {items == null}");
            if (items == null)
            {
                Debug.WriteLine($"[{nameof(Copy)}] items is null -> return");
                return;
            }
            var itemsList = items.ToList();
            Debug.WriteLine($"[{nameof(Copy)}] items.Count: {itemsList.Count}");

            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
            Debug.WriteLine($"[{nameof(Copy)}] extracted paths count: {paths.Count}");
            if (paths.Count > 0)
            {
                for (int i = 0; i < Math.Min(paths.Count, 3); i++)
                    Debug.WriteLine($"[{nameof(Copy)}] path[{i}]: '{paths[i]}'");
            }
            if (paths.Count == 0)
            {
                Debug.WriteLine($"[{nameof(Copy)}] no valid paths -> return");
                return;
            }

            _sourcePaths = paths;
            _isCut = false;
            Debug.WriteLine($"[{nameof(Copy)}] _sourcePaths set ({_sourcePaths.Count} items), _isCut=false");
            Debug.WriteLine($"[{nameof(Copy)}] Firing ClipboardChanged...");
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine($"[{nameof(Copy)}] EXIT");
        }

        public void Cut(IEnumerable<ExplorerItemViewModel> items)
        {
            Debug.WriteLine($"[{nameof(Cut)}] ENTER");
            Debug.WriteLine($"[{nameof(Cut)}] items null? {items == null}");
            if (items == null)
            {
                Debug.WriteLine($"[{nameof(Cut)}] items is null -> return");
                return;
            }
            var itemsList = items.ToList();
            Debug.WriteLine($"[{nameof(Cut)}] items.Count: {itemsList.Count}");

            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
            Debug.WriteLine($"[{nameof(Cut)}] extracted paths count: {paths.Count}");
            if (paths.Count > 0)
            {
                for (int i = 0; i < Math.Min(paths.Count, 3); i++)
                    Debug.WriteLine($"[{nameof(Cut)}] path[{i}]: '{paths[i]}'");
            }
            if (paths.Count == 0)
            {
                Debug.WriteLine($"[{nameof(Cut)}] no valid paths -> return");
                return;
            }

            _sourcePaths = paths;
            _isCut = true;
            Debug.WriteLine($"[{nameof(Cut)}] _sourcePaths set ({_sourcePaths.Count} items), _isCut=true");
            Debug.WriteLine($"[{nameof(Cut)}] Firing ClipboardChanged...");
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
            Debug.WriteLine($"[{nameof(Cut)}] EXIT");
        }

        // ===================== PASTE =====================
        public async Task PasteAsync(string destinationFolder)
        {
            Debug.WriteLine($"[{nameof(PasteAsync)}] ENTER");
            Debug.WriteLine($"[{nameof(PasteAsync)}] destinationFolder: '{destinationFolder}'");
            Debug.WriteLine($"[{nameof(PasteAsync)}] _sourcePaths.Count: {_sourcePaths.Count}");
            Debug.WriteLine($"[{nameof(PasteAsync)}] Directory.Exists(destinationFolder): {Directory.Exists(destinationFolder)}");

            if (_sourcePaths.Count == 0 || string.IsNullOrEmpty(destinationFolder) || !Directory.Exists(destinationFolder))
            {
                Debug.WriteLine($"[{nameof(PasteAsync)}] Invalid preconditions -> return");
                return;
            }

            var sources = _sourcePaths.ToList();
            bool isCut = _isCut;
            Debug.WriteLine($"[{nameof(PasteAsync)}] sources count: {sources.Count}, isCut: {isCut}");

            // Очищаем буфер обмена перед операцией
            _sourcePaths.Clear();
            _isCut = false;
            Debug.WriteLine($"[{nameof(PasteAsync)}] Cleared _sourcePaths, _isCut=false");
            Debug.WriteLine($"[{nameof(PasteAsync)}] Firing ClipboardChanged...");
            ClipboardChanged?.Invoke(this, EventArgs.Empty);

            string title = isCut ? "Перемещение..." : "Копирование...";
            uint operation = isCut ? FO_MOVE : FO_COPY;
            Debug.WriteLine($"[{nameof(PasteAsync)}] title: '{title}', operation: {operation}");

            Debug.WriteLine($"[{nameof(PasteAsync)}] Calling RunOperationWithProgressAsync...");
            bool ok = await RunOperationWithProgressAsync(operation, sources, destinationFolder, title);
            Debug.WriteLine($"[{nameof(PasteAsync)}] RunOperationWithProgressAsync returned: {ok}");

            if (!ok)
            {
                Debug.WriteLine($"[{nameof(PasteAsync)}] Operation failed or aborted, starting manual fallback...");
                ManualPasteFallback(sources, destinationFolder, isCut);
            }
            else
            {
                Debug.WriteLine($"[{nameof(PasteAsync)}] Operation succeeded.");
            }
            Debug.WriteLine($"[{nameof(PasteAsync)}] EXIT");
        }

        // ===================== DELETE =====================
        public async Task DeleteAsync(IEnumerable<ExplorerItemViewModel> items)
        {
            Debug.WriteLine($"[{nameof(DeleteAsync)}] ENTER");
            Debug.WriteLine($"[{nameof(DeleteAsync)}] items null? {items == null}");
            if (items == null)
            {
                Debug.WriteLine($"[{nameof(DeleteAsync)}] items is null -> return");
                return;
            }
            var itemsList = items.ToList();
            Debug.WriteLine($"[{nameof(DeleteAsync)}] items.Count: {itemsList.Count}");

            var paths = itemsList
                .Select(i => i.FilePath)
                .Where(p => !string.IsNullOrEmpty(p) && (File.Exists(p) || Directory.Exists(p)))
                .ToList();
            Debug.WriteLine($"[{nameof(DeleteAsync)}] valid paths count: {paths.Count}");
            if (paths.Count > 0)
            {
                for (int i = 0; i < Math.Min(paths.Count, 3); i++)
                    Debug.WriteLine($"[{nameof(DeleteAsync)}] path[{i}]: '{paths[i]}'");
            }
            if (paths.Count == 0)
            {
                Debug.WriteLine($"[{nameof(DeleteAsync)}] no valid paths -> return");
                return;
            }

            Debug.WriteLine($"[{nameof(DeleteAsync)}] Calling RunOperationWithProgressAsync (FO_DELETE) with FOF_ALLOWUNDO...");
            bool ok = await RunOperationWithProgressAsync(FO_DELETE, paths, null, "Удаление...",
                additionalFlags: FOF_ALLOWUNDO);
            Debug.WriteLine($"[{nameof(DeleteAsync)}] RunOperationWithProgressAsync returned: {ok}");

            if (!ok)
            {
                Debug.WriteLine($"[{nameof(DeleteAsync)}] Operation failed/aborted, performing permanent delete fallback...");
                foreach (var path in paths)
                {
                    try
                    {
                        Debug.WriteLine($"[{nameof(DeleteAsync)}] Fallback deleting: '{path}'");
                        if (File.Exists(path))
                        {
                            Debug.WriteLine($"[{nameof(DeleteAsync)}]   is file -> File.Delete");
                            File.Delete(path);
                        }
                        else if (Directory.Exists(path))
                        {
                            Debug.WriteLine($"[{nameof(DeleteAsync)}]   is directory -> Directory.Delete(true)");
                            Directory.Delete(path, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[{nameof(DeleteAsync)}] Fallback error for '{path}': {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.WriteLine($"[{nameof(DeleteAsync)}] Operation succeeded.");
            }
            Debug.WriteLine($"[{nameof(DeleteAsync)}] EXIT");
        }

        // ===================== ЕДИНЫЙ МЕТОД ПРОГРЕССА =====================
        /// <summary>
        /// Запускает SHFileOperation с системным окном прогресса (и подтверждением для удаления, если не указан FOF_NOCONFIRMATION).
        /// </summary>
        /// <param name="wFunc">FO_COPY, FO_MOVE, FO_DELETE</param>
        /// <param name="sourcePaths">Список исходных путей</param>
        /// <param name="destinationFolder">Папка назначения (null для удаления)</param>
        /// <param name="progressTitle">Заголовок окна прогресса</param>
        /// <param name="additionalFlags">Дополнительные флаги (например, FOF_ALLOWUNDO для корзины)</param>
        private async Task<bool> RunOperationWithProgressAsync(
            uint wFunc,
            List<string> sourcePaths,
            string destinationFolder,   // теперь без ?, может принимать null
            string progressTitle,
            ushort additionalFlags = 0)
        {
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] ENTER");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] wFunc: 0x{wFunc:X}");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] sourcePaths.Count: {sourcePaths.Count}");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] destinationFolder: '{destinationFolder ?? "null"}'");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] progressTitle: '{progressTitle}'");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] additionalFlags: 0x{additionalFlags:X}");

            if (sourcePaths.Count > 0)
            {
                for (int i = 0; i < Math.Min(sourcePaths.Count, 3); i++)
                    Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] source[{i}]: '{sourcePaths[i]}'");
            }

            string fromList = string.Join("\0", sourcePaths) + "\0\0";
            string toPath = (destinationFolder != null) ? destinationFolder + "\0\0" : null;
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] fromList length: {fromList.Length} chars");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] toPath: '{toPath ?? "null"}'");

            ushort flags = FOF_SIMPLEPROGRESS; // всегда показываем прогресс
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] base flags (FOF_SIMPLEPROGRESS): 0x{flags:X}");
            if (wFunc != FO_DELETE)
            {
                flags |= FOF_MULTIDESTFILES;
                Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] Added FOF_MULTIDESTFILES, flags now: 0x{flags:X}");
            }
            flags |= additionalFlags;
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] After additionalFlags, flags: 0x{flags:X} (binary: {Convert.ToString(flags, 2).PadLeft(16, '0')})");

            var op = new SHFILEOPSTRUCT
            {
                hwnd = _parentHwnd,
                wFunc = wFunc,
                pFrom = fromList,
                pTo = toPath,
                fFlags = flags,
                fAnyOperationsAborted = false,
                hNameMappings = IntPtr.Zero,
                lpszProgressTitle = progressTitle
            };

            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] SHFILEOPSTRUCT prepared:");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}]   hwnd=0x{op.hwnd:X}");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}]   wFunc=0x{op.wFunc:X}");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}]   fFlags=0x{op.fFlags:X}");
            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}]   lpszProgressTitle='{op.lpszProgressTitle}'");

            bool success;
            if (_dispatcherQueue != null)
            {
                Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] DispatcherQueue available, enqueuing to UI thread...");
                var tcs = new TaskCompletionSource<bool>();
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] [UI-thread] SHFileOperation called");
                    try
                    {
                        int result = SHFileOperation(ref op);
                        Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] [UI-thread] SHFileOperation result={result}, fAnyOperationsAborted={op.fAnyOperationsAborted}");
                        success = (result == 0 && !op.fAnyOperationsAborted);
                        Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] [UI-thread] Derived success={success}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] [UI-thread] Exception: {ex.GetType().Name} - {ex.Message}");
                        success = false;
                    }
                    tcs.SetResult(success);
                });
                Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] Awaiting UI thread completion...");
                success = await tcs.Task;
                Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] UI thread task completed, success={success}");
            }
            else
            {
                Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] DispatcherQueue is null, using Task.Run (STA thread)");
                success = await Task.Run(() =>
                {
                    Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] [STA-thread] SHFileOperation called");
                    try
                    {
                        int result = SHFileOperation(ref op);
                        Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] [STA-thread] SHFileOperation result={result}, fAnyOperationsAborted={op.fAnyOperationsAborted}");
                        bool localSuccess = (result == 0 && !op.fAnyOperationsAborted);
                        Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] [STA-thread] Derived success={localSuccess}");
                        return localSuccess;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] [STA-thread] Exception: {ex.GetType().Name} - {ex.Message}");
                        return false;
                    }
                });
                Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] Task.Run completed, success={success}");
            }

            Debug.WriteLine($"[{nameof(RunOperationWithProgressAsync)}] EXIT, returning {success}");
            return success;
        }

        // ===================== Ручной fallback (без интерфейса) =====================
        private static void ManualPasteFallback(List<string> sources, string dest, bool isCut)
        {
            Debug.WriteLine($"[{nameof(ManualPasteFallback)}] ENTER");
            Debug.WriteLine($"[{nameof(ManualPasteFallback)}] sources.Count: {sources.Count}, dest: '{dest}', isCut: {isCut}");
            foreach (var src in sources)
            {
                try
                {
                    string dst = Path.Combine(dest, Path.GetFileName(src));
                    Debug.WriteLine($"[{nameof(ManualPasteFallback)}] Processing source '{src}' -> '{dst}'");
                    if (isCut)
                    {
                        if (File.Exists(src))
                        {
                            Debug.WriteLine($"[{nameof(ManualPasteFallback)}]   Moving file");
                            File.Move(src, dst, true);
                        }
                        else if (Directory.Exists(src))
                        {
                            Debug.WriteLine($"[{nameof(ManualPasteFallback)}]   Moving directory");
                            Directory.Move(src, dst);
                        }
                        else
                        {
                            Debug.WriteLine($"[{nameof(ManualPasteFallback)}]   Source not found");
                        }
                    }
                    else
                    {
                        if (File.Exists(src))
                        {
                            Debug.WriteLine($"[{nameof(ManualPasteFallback)}]   Copying file");
                            File.Copy(src, dst, true);
                        }
                        else if (Directory.Exists(src))
                        {
                            Debug.WriteLine($"[{nameof(ManualPasteFallback)}]   Copying directory recursively");
                            CopyDirRecursive(src, dst);
                        }
                        else
                        {
                            Debug.WriteLine($"[{nameof(ManualPasteFallback)}]   Source not found");
                        }
                    }
                    Debug.WriteLine($"[{nameof(ManualPasteFallback)}] Successfully processed '{src}'");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{nameof(ManualPasteFallback)}] Error processing '{src}': {ex.Message}");
                }
            }
            Debug.WriteLine($"[{nameof(ManualPasteFallback)}] EXIT");
        }

        private static void CopyDirRecursive(string sourceDir, string destDir)
        {
            Debug.WriteLine($"[{nameof(CopyDirRecursive)}] ENTER: source='{sourceDir}', dest='{destDir}'");
            try
            {
                Directory.CreateDirectory(destDir);
                Debug.WriteLine($"[{nameof(CopyDirRecursive)}] Created directory '{destDir}'");
                var files = Directory.GetFiles(sourceDir);
                Debug.WriteLine($"[{nameof(CopyDirRecursive)}] Files in source: {files.Length}");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var destFile = Path.Combine(destDir, fileName);
                    Debug.WriteLine($"[{nameof(CopyDirRecursive)}] Copying file '{file}' to '{destFile}'");
                    File.Copy(file, destFile, true);
                }
                var dirs = Directory.GetDirectories(sourceDir);
                Debug.WriteLine($"[{nameof(CopyDirRecursive)}] Subdirectories in source: {dirs.Length}");
                foreach (var subDir in dirs)
                {
                    var dirName = Path.GetFileName(subDir);
                    var destSubDir = Path.Combine(destDir, dirName);
                    Debug.WriteLine($"[{nameof(CopyDirRecursive)}] Recursing into '{subDir}' -> '{destSubDir}'");
                    CopyDirRecursive(subDir, destSubDir);
                }
                Debug.WriteLine($"[{nameof(CopyDirRecursive)}] Finished copying '{sourceDir}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{nameof(CopyDirRecursive)}] Exception: {ex.Message}");
            }
            Debug.WriteLine($"[{nameof(CopyDirRecursive)}] EXIT");
        }
    }
}

