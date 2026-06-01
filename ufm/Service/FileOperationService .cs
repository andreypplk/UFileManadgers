using System;
using System.Collections.Generic;
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

        // Защита от одновременного выполнения PasteAsync
        private volatile bool _isPasting;

        public bool CanPaste => _sourcePaths.Count > 0;
        public event EventHandler ClipboardChanged;

        // ===================== Инициализация =====================
        public void SetParentWindow(IntPtr hwnd)
        {
            _parentHwnd = hwnd;
        }

        public void Initialize(IntPtr parentHwnd, DispatcherQueue dispatcher)
        {
            _parentHwnd = parentHwnd;
            _dispatcherQueue = dispatcher;
        }

        // ===================== Copy / Cut =====================
        public void Copy(IEnumerable<ExplorerItemViewModel> items)
        {
            if (items == null)
                return;

            var itemsList = items.ToList();
            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();

            if (paths.Count == 0)
                return;

            _sourcePaths = paths;
            _isCut = false;
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Cut(IEnumerable<ExplorerItemViewModel> items)
        {
            if (items == null)
                return;

            var itemsList = items.ToList();
            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();

            if (paths.Count == 0)
                return;

            _sourcePaths = paths;
            _isCut = true;
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }

        // ===================== PASTE =====================
        public async Task PasteAsync(string destinationFolder)
        {
            // Предотвращаем повторный вход
            if (_isPasting)
                return;

            if (_sourcePaths.Count == 0 || string.IsNullOrEmpty(destinationFolder) || !Directory.Exists(destinationFolder))
                return;

            _isPasting = true;
            try
            {
                // Фиксируем текущее состояние и сразу очищаем, чтобы избежать гонки
                var sources = _sourcePaths.ToList();
                bool isCut = _isCut;

                _sourcePaths.Clear();
                _isCut = false;
                ClipboardChanged?.Invoke(this, EventArgs.Empty);

                string title = isCut ? "Перемещение..." : "Копирование...";
                uint operation = isCut ? FO_MOVE : FO_COPY;

                bool ok = await RunOperationWithProgressAsync(operation, sources, destinationFolder, title);

                if (!ok)
                {
                    // Ручной fallback тоже выполняем асинхронно, чтобы не блокировать UI
                    await Task.Run(() => ManualPasteFallback(sources, destinationFolder, isCut));
                }
            }
            finally
            {
                _isPasting = false;
            }
        }

        // ===================== DELETE =====================
        public async Task DeleteAsync(IEnumerable<ExplorerItemViewModel> items)
        {
            if (items == null)
                return;

            var itemsList = items.ToList();
            var paths = itemsList
                .Select(i => i.FilePath)
                .Where(p => !string.IsNullOrEmpty(p) && (File.Exists(p) || Directory.Exists(p)))
                .ToList();

            if (paths.Count == 0)
                return;

            bool ok = await RunOperationWithProgressAsync(FO_DELETE, paths, null, "Удаление...",
                additionalFlags: FOF_ALLOWUNDO);

            if (!ok)
            {
                // Ручной fallback также в фоновом потоке
                await Task.Run(() =>
                {
                    foreach (var path in paths)
                    {
                        try
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }
                            else if (Directory.Exists(path))
                            {
                                Directory.Delete(path, true);
                            }
                        }
                        catch
                        {
                        }
                    }
                });
            }
        }

        // ===================== ЕДИНЫЙ МЕТОД ПРОГРЕССА =====================
        /// <summary>
        /// Запускает SHFileOperation с системным окном прогресса.
        /// Всегда выполняется в фоновом потоке, чтобы не блокировать UI.
        /// </summary>
        private async Task<bool> RunOperationWithProgressAsync(
            uint wFunc,
            List<string> sourcePaths,
            string destinationFolder,
            string progressTitle,
            ushort additionalFlags = 0)
        {
            string fromList = string.Join("\0", sourcePaths) + "\0\0";
            string toPath = (destinationFolder != null) ? destinationFolder + "\0\0" : null;

            ushort flags = FOF_SIMPLEPROGRESS;
            if (wFunc != FO_DELETE)
            {
                flags |= FOF_MULTIDESTFILES;
            }
            flags |= additionalFlags;

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

            // Всегда выполняем в фоновом потоке
            return await Task.Run(() =>
            {
                try
                {
                    int result = SHFileOperation(ref op);
                    return result == 0 && !op.fAnyOperationsAborted;
                }
                catch
                {
                    return false;
                }
            });
        }

        // ===================== Ручной fallback =====================
        private static void ManualPasteFallback(List<string> sources, string dest, bool isCut)
        {
            foreach (var src in sources)
            {
                try
                {
                    string dst = Path.Combine(dest, Path.GetFileName(src));
                    if (isCut)
                    {
                        if (File.Exists(src))
                        {
                            File.Move(src, dst, true);
                        }
                        else if (Directory.Exists(src))
                        {
                            Directory.Move(src, dst);
                        }
                    }
                    else
                    {
                        if (File.Exists(src))
                        {
                            File.Copy(src, dst, true);
                        }
                        else if (Directory.Exists(src))
                        {
                            CopyDirRecursive(src, dst);
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private static void CopyDirRecursive(string sourceDir, string destDir)
        {
            try
            {
                Directory.CreateDirectory(destDir);
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var fileName = Path.GetFileName(file);
                    var destFile = Path.Combine(destDir, fileName);
                    File.Copy(file, destFile, true);
                }
                foreach (var subDir in Directory.GetDirectories(sourceDir))
                {
                    var dirName = Path.GetFileName(subDir);
                    var destSubDir = Path.Combine(destDir, dirName);
                    CopyDirRecursive(subDir, destSubDir);
                }
            }
            catch
            {
            }
        }
    }
}