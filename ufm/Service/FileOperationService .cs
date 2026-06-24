//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks;
//using Core_FileManagement;
//using Microsoft.UI.Dispatching;

//namespace ufm
//{
//    public class FileOperationService : IFileOperationService
//    {
//        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
//        private struct SHFILEOPSTRUCT
//        {
//            public IntPtr hwnd;
//            public uint wFunc;
//            [MarshalAs(UnmanagedType.LPWStr)]
//            public string pFrom;
//            [MarshalAs(UnmanagedType.LPWStr)]
//            public string pTo;
//            public ushort fFlags;
//            public bool fAnyOperationsAborted;
//            public IntPtr hNameMappings;
//            public string lpszProgressTitle;
//        }

//        private const uint FO_COPY = 0x0002;
//        private const uint FO_DELETE = 0x0003;
//        private const uint FO_MOVE = 0x0001;

//        private const ushort FOF_ALLOWUNDO = 0x0040;
//        private const ushort FOF_NOCONFIRMATION = 0x0010;
//        private const ushort FOF_SIMPLEPROGRESS = 0x0100;
//        private const ushort FOF_MULTIDESTFILES = 0x0200;

//        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
//        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

//        private List<string> _sourcePaths = new();
//        private bool _isCut;
//        private IntPtr _parentHwnd = IntPtr.Zero;
//        private DispatcherQueue _dispatcherQueue;
//        private volatile bool _isPasting;

//        public bool CanPaste => _sourcePaths.Count > 0;
//        public event EventHandler ClipboardChanged;

//        public void SetParentWindow(IntPtr hwnd)
//        {
//            Debug.WriteLine($"[FileOpService] SetParentWindow: hwnd = {hwnd}");
//            _parentHwnd = hwnd;
//        }

//        public void Initialize(IntPtr parentHwnd, DispatcherQueue dispatcher)
//        {
//            Debug.WriteLine($"[FileOpService] Initialize: hwnd = {parentHwnd}");
//            _parentHwnd = parentHwnd;
//            _dispatcherQueue = dispatcher;
//        }

//        public void Copy(IEnumerable<ExplorerItemViewModel> items)
//        {
//            if (items == null)
//            {
//                Debug.WriteLine("[FileOpService] Copy: items is null, returning.");
//                return;
//            }

//            var itemsList = items.ToList();
//            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
//            Debug.WriteLine($"[FileOpService] Copy: {paths.Count} items. Paths: {string.Join(", ", paths)}");

//            if (paths.Count == 0)
//                return;

//            _sourcePaths = paths;
//            _isCut = false;
//            Debug.WriteLine("[FileOpService] Copy: clipboard updated (Copy mode).");
//            ClipboardChanged?.Invoke(this, EventArgs.Empty);
//        }

//        public void Cut(IEnumerable<ExplorerItemViewModel> items)
//        {
//            if (items == null)
//            {
//                Debug.WriteLine("[FileOpService] Cut: items is null, returning.");
//                return;
//            }

//            var itemsList = items.ToList();
//            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
//            Debug.WriteLine($"[FileOpService] Cut: {paths.Count} items. Paths: {string.Join(", ", paths)}");

//            if (paths.Count == 0)
//                return;

//            _sourcePaths = paths;
//            _isCut = true;
//            Debug.WriteLine("[FileOpService] Cut: clipboard updated (Cut mode).");
//            ClipboardChanged?.Invoke(this, EventArgs.Empty);
//        }

//        public async Task PasteAsync(string destinationFolder)
//        {
//            Debug.WriteLine($"[FileOpService] PasteAsync: destination='{destinationFolder}', isPasting={_isPasting}");
//            if (_isPasting)
//            {
//                Debug.WriteLine("[FileOpService] PasteAsync: already pasting, exiting.");
//                return;
//            }

//            if (_sourcePaths.Count == 0 || string.IsNullOrEmpty(destinationFolder) || !Directory.Exists(destinationFolder))
//            {
//                Debug.WriteLine("[FileOpService] PasteAsync: precondition failed (empty sources or invalid destination).");
//                return;
//            }

//            _isPasting = true;
//            try
//            {
//                var sources = _sourcePaths.ToList();
//                bool isCut = _isCut;
//                Debug.WriteLine($"[FileOpService] PasteAsync: sources count={sources.Count}, isCut={isCut}. Clearing clipboard.");
//                _sourcePaths.Clear();
//                _isCut = false;
//                ClipboardChanged?.Invoke(this, EventArgs.Empty);

//                string title = isCut ? "Перемещение..." : "Копирование...";
//                uint operation = isCut ? FO_MOVE : FO_COPY;

//                Debug.WriteLine($"[FileOpService] PasteAsync: starting SHFileOperation (title='{title}')");
//                bool ok = await RunOperationWithProgressAsync(operation, sources, destinationFolder, title);
//                Debug.WriteLine($"[FileOpService] PasteAsync: SHFileOperation result = {ok}");

//                if (!ok)
//                {
//                    Debug.WriteLine("[FileOpService] PasteAsync: falling back to manual paste.");
//                    await Task.Run(() => ManualPasteFallback(sources, destinationFolder, isCut));
//                }
//            }
//            finally
//            {
//                _isPasting = false;
//                Debug.WriteLine("[FileOpService] PasteAsync: finished.");
//            }
//        }

//        public async Task DeleteAsync(IEnumerable<ExplorerItemViewModel> items)
//        {
//            if (items == null)
//            {
//                Debug.WriteLine("[FileOpService] DeleteAsync: items is null, returning.");
//                return;
//            }

//            var itemsList = items.ToList();
//            var paths = itemsList
//                .Select(i => i.FilePath)
//                .Where(p => !string.IsNullOrEmpty(p) && (File.Exists(p) || Directory.Exists(p)))
//                .ToList();

//            Debug.WriteLine($"[FileOpService] DeleteAsync: {paths.Count} items. Paths: {string.Join(", ", paths)}");

//            if (paths.Count == 0)
//                return;

//            Debug.WriteLine("[FileOpService] DeleteAsync: starting SHFileOperation (FO_DELETE).");
//            bool ok = await RunOperationWithProgressAsync(FO_DELETE, paths, null, "Удаление...", additionalFlags: FOF_ALLOWUNDO);
//            Debug.WriteLine($"[FileOpService] DeleteAsync: SHFileOperation result = {ok}");

//            if (!ok)
//            {
//                Debug.WriteLine("[FileOpService] DeleteAsync: falling back to manual deletion.");
//                await Task.Run(() =>
//                {
//                    foreach (var path in paths)
//                    {
//                        try
//                        {
//                            if (File.Exists(path)) File.Delete(path);
//                            else if (Directory.Exists(path)) Directory.Delete(path, true);
//                        }
//                        catch (Exception ex)
//                        {
//                            Debug.WriteLine($"[FileOpService] DeleteAsync: manual delete failed for '{path}': {ex.Message}");
//                        }
//                    }
//                });
//            }
//        }

//        private async Task<bool> RunOperationWithProgressAsync(
//            uint wFunc,
//            List<string> sourcePaths,
//            string destinationFolder,
//            string progressTitle,
//            ushort additionalFlags = 0)
//        {
//            string fromList = string.Join("\0", sourcePaths) + "\0\0";
//            string toPath = (destinationFolder != null) ? destinationFolder + "\0\0" : null;

//            ushort flags = FOF_SIMPLEPROGRESS;
//            if (wFunc != FO_DELETE) flags |= FOF_MULTIDESTFILES;
//            flags |= additionalFlags;

//            var op = new SHFILEOPSTRUCT
//            {
//                hwnd = _parentHwnd,
//                wFunc = wFunc,
//                pFrom = fromList,
//                pTo = toPath,
//                fFlags = flags,
//                fAnyOperationsAborted = false,
//                hNameMappings = IntPtr.Zero,
//                lpszProgressTitle = progressTitle
//            };

//            Debug.WriteLine($"[FileOpService] SHFileOperation: wFunc={wFunc}, from='{fromList.Replace("\0", "|")}', to='{toPath?.Replace("\0", "|")}', flags=0x{flags:X}");

//            return await Task.Run(() =>
//            {
//                try
//                {
//                    int result = SHFileOperation(ref op);
//                    Debug.WriteLine($"[FileOpService] SHFileOperation returned: result={result}, aborted={op.fAnyOperationsAborted}");
//                    return result == 0 && !op.fAnyOperationsAborted;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] SHFileOperation exception: {ex}");
//                    return false;
//                }
//            });
//        }

//        private static void ManualPasteFallback(List<string> sources, string dest, bool isCut)
//        {
//            Debug.WriteLine($"[FileOpService] ManualPasteFallback: isCut={isCut}, dest='{dest}', sources={sources.Count}");
//            foreach (var src in sources)
//            {
//                try
//                {
//                    string dst = Path.Combine(dest, Path.GetFileName(src));
//                    Debug.WriteLine($"[FileOpService] ManualPaste: processing '{src}' -> '{dst}'");
//                    if (isCut)
//                    {
//                        if (File.Exists(src)) File.Move(src, dst, true);
//                        else if (Directory.Exists(src)) Directory.Move(src, dst);
//                    }
//                    else
//                    {
//                        if (File.Exists(src)) File.Copy(src, dst, true);
//                        else if (Directory.Exists(src)) CopyDirRecursive(src, dst);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] ManualPaste: failed for '{src}': {ex.Message}");
//                }
//            }
//        }

//        private static void CopyDirRecursive(string sourceDir, string destDir)
//        {
//            try
//            {
//                Directory.CreateDirectory(destDir);
//                foreach (var file in Directory.GetFiles(sourceDir))
//                {
//                    string fileName = Path.GetFileName(file);
//                    File.Copy(file, Path.Combine(destDir, fileName), true);
//                }
//                foreach (var subDir in Directory.GetDirectories(sourceDir))
//                {
//                    string dirName = Path.GetFileName(subDir);
//                    CopyDirRecursive(subDir, Path.Combine(destDir, dirName));
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileOpService] CopyDirRecursive: error from '{sourceDir}' to '{destDir}': {ex.Message}");
//            }
//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks;
//using Core_FileManagement;
//using Microsoft.UI.Dispatching;
//using Windows.ApplicationModel.DataTransfer;  // Добавлено
//using Windows.Storage;                       // Добавлено

//namespace ufm
//{
//    public class FileOperationService : IFileOperationService, IDisposable
//    {
//        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
//        private struct SHFILEOPSTRUCT
//        {
//            public IntPtr hwnd;
//            public uint wFunc;
//            [MarshalAs(UnmanagedType.LPWStr)]
//            public string pFrom;
//            [MarshalAs(UnmanagedType.LPWStr)]
//            public string pTo;
//            public ushort fFlags;
//            public bool fAnyOperationsAborted;
//            public IntPtr hNameMappings;
//            public string lpszProgressTitle;
//        }

//        private const uint FO_COPY = 0x0002;
//        private const uint FO_DELETE = 0x0003;
//        private const uint FO_MOVE = 0x0001;

//        private const ushort FOF_ALLOWUNDO = 0x0040;
//        private const ushort FOF_NOCONFIRMATION = 0x0010;
//        private const ushort FOF_SIMPLEPROGRESS = 0x0100;
//        private const ushort FOF_MULTIDESTFILES = 0x0200;

//        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
//        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

//        private List<string> _sourcePaths = new();
//        private bool _isCut;
//        private IntPtr _parentHwnd = IntPtr.Zero;
//        private DispatcherQueue _dispatcherQueue;
//        private volatile bool _isPasting;

//        public bool CanPaste
//        {
//            get
//            {
//                // Можем вставлять, если есть внутренний буфер или в системном буфере есть файлы
//                if (_sourcePaths.Count > 0)
//                    return true;
//                try
//                {
//                    var data = Clipboard.GetContent();
//                    return data.Contains(StandardDataFormats.StorageItems);
//                }
//                catch
//                {
//                    return false;
//                }
//            }
//        }

//        public event EventHandler ClipboardChanged;

//        public void SetParentWindow(IntPtr hwnd)
//        {
//            Debug.WriteLine($"[FileOpService] SetParentWindow: hwnd = {hwnd}");
//            _parentHwnd = hwnd;
//        }

//        public void Initialize(IntPtr parentHwnd, DispatcherQueue dispatcher)
//        {
//            Debug.WriteLine($"[FileOpService] Initialize: hwnd = {parentHwnd}");
//            _parentHwnd = parentHwnd;
//            _dispatcherQueue = dispatcher;
//            Clipboard.ContentChanged += OnClipboardContentChanged;   // Добавлено
//        }

//        public void Copy(IEnumerable<ExplorerItemViewModel> items)
//        {
//            if (items == null)
//            {
//                Debug.WriteLine("[FileOpService] Copy: items is null, returning.");
//                return;
//            }

//            var itemsList = items.ToList();
//            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
//            Debug.WriteLine($"[FileOpService] Copy: {paths.Count} items. Paths: {string.Join(", ", paths)}");

//            if (paths.Count == 0)
//                return;

//            _sourcePaths = paths;
//            _isCut = false;
//            SetClipboardContent(paths, DataPackageOperation.Copy);   // Добавлено
//            ClipboardChanged?.Invoke(this, EventArgs.Empty);
//        }

//        public void Cut(IEnumerable<ExplorerItemViewModel> items)
//        {
//            if (items == null)
//            {
//                Debug.WriteLine("[FileOpService] Cut: items is null, returning.");
//                return;
//            }

//            var itemsList = items.ToList();
//            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
//            Debug.WriteLine($"[FileOpService] Cut: {paths.Count} items. Paths: {string.Join(", ", paths)}");

//            if (paths.Count == 0)
//                return;

//            _sourcePaths = paths;
//            _isCut = true;
//            SetClipboardContent(paths, DataPackageOperation.Move);   // Добавлено
//            ClipboardChanged?.Invoke(this, EventArgs.Empty);
//        }

//        public async Task PasteAsync(string destinationFolder)
//        {
//            Debug.WriteLine($"[FileOpService] PasteAsync: destination='{destinationFolder}', isPasting={_isPasting}");
//            if (_isPasting)
//            {
//                Debug.WriteLine("[FileOpService] PasteAsync: already pasting, exiting.");
//                return;
//            }

//            if (string.IsNullOrEmpty(destinationFolder) || !Directory.Exists(destinationFolder))
//            {
//                Debug.WriteLine("[FileOpService] PasteAsync: invalid destination.");
//                return;
//            }

//            List<string> sources;
//            bool isCut;

//            // Сначала пытаемся использовать внутренний буфер (из нашего приложения)
//            if (_sourcePaths.Count > 0)
//            {
//                sources = _sourcePaths.ToList();
//                isCut = _isCut;
//                Debug.WriteLine($"[FileOpService] PasteAsync: using internal clipboard. sources={sources.Count}, isCut={isCut}");
//                _sourcePaths.Clear();
//                _isCut = false;
//                ClipboardChanged?.Invoke(this, EventArgs.Empty);
//            }
//            else
//            {
//                // Пытаемся получить файлы из системного буфера обмена
//                Debug.WriteLine("[FileOpService] PasteAsync: internal empty, trying system clipboard.");
//                var dataPackageView = Clipboard.GetContent();
//                if (dataPackageView.Contains(StandardDataFormats.StorageItems))
//                {
//                    var storageItems = await dataPackageView.GetStorageItemsAsync();
//                    sources = storageItems.Select(i => i.Path).ToList();
//                    isCut = dataPackageView.RequestedOperation == DataPackageOperation.Move;
//                    Debug.WriteLine($"[FileOpService] PasteAsync: got from system clipboard. sources={sources.Count}, isCut={isCut}");
//                }
//                else
//                {
//                    Debug.WriteLine("[FileOpService] PasteAsync: system clipboard has no storage items.");
//                    return;
//                }
//            }

//            if (sources.Count == 0) return;

//            _isPasting = true;
//            try
//            {
//                string title = isCut ? "Перемещение..." : "Копирование...";
//                uint operation = isCut ? FO_MOVE : FO_COPY;

//                Debug.WriteLine($"[FileOpService] PasteAsync: starting SHFileOperation (title='{title}')");
//                bool ok = await RunOperationWithProgressAsync(operation, sources, destinationFolder, title);
//                Debug.WriteLine($"[FileOpService] PasteAsync: SHFileOperation result = {ok}");

//                if (!ok)
//                {
//                    Debug.WriteLine("[FileOpService] PasteAsync: falling back to manual paste.");
//                    await Task.Run(() => ManualPasteFallback(sources, destinationFolder, isCut));
//                }

//                // Если операция была перемещением, очищаем системный буфер
//                if (isCut)
//                {
//                    try
//                    {
//                        Clipboard.Clear();
//                        Debug.WriteLine("[FileOpService] PasteAsync: system clipboard cleared after move.");
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"[FileOpService] PasteAsync: error clearing clipboard: {ex.Message}");
//                    }
//                }
//            }
//            finally
//            {
//                _isPasting = false;
//                Debug.WriteLine("[FileOpService] PasteAsync: finished.");
//            }
//        }

//        public async Task DeleteAsync(IEnumerable<ExplorerItemViewModel> items)
//        {
//            // ... без изменений ...
//            if (items == null)
//            {
//                Debug.WriteLine("[FileOpService] DeleteAsync: items is null, returning.");
//                return;
//            }

//            var itemsList = items.ToList();
//            var paths = itemsList
//                .Select(i => i.FilePath)
//                .Where(p => !string.IsNullOrEmpty(p) && (File.Exists(p) || Directory.Exists(p)))
//                .ToList();

//            Debug.WriteLine($"[FileOpService] DeleteAsync: {paths.Count} items. Paths: {string.Join(", ", paths)}");

//            if (paths.Count == 0)
//                return;

//            Debug.WriteLine("[FileOpService] DeleteAsync: starting SHFileOperation (FO_DELETE).");
//            bool ok = await RunOperationWithProgressAsync(FO_DELETE, paths, null, "Удаление...", additionalFlags: FOF_ALLOWUNDO);
//            Debug.WriteLine($"[FileOpService] DeleteAsync: SHFileOperation result = {ok}");

//            if (!ok)
//            {
//                Debug.WriteLine("[FileOpService] DeleteAsync: falling back to manual deletion.");
//                await Task.Run(() =>
//                {
//                    foreach (var path in paths)
//                    {
//                        try
//                        {
//                            if (File.Exists(path)) File.Delete(path);
//                            else if (Directory.Exists(path)) Directory.Delete(path, true);
//                        }
//                        catch (Exception ex)
//                        {
//                            Debug.WriteLine($"[FileOpService] DeleteAsync: manual delete failed for '{path}': {ex.Message}");
//                        }
//                    }
//                });
//            }
//        }

//        // ===================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====================

//        private async Task<bool> RunOperationWithProgressAsync(
//            uint wFunc,
//            List<string> sourcePaths,
//            string destinationFolder,
//            string progressTitle,
//            ushort additionalFlags = 0)
//        {
//            // ... без изменений ...
//            string fromList = string.Join("\0", sourcePaths) + "\0\0";
//            string toPath = (destinationFolder != null) ? destinationFolder + "\0\0" : null;

//            ushort flags = FOF_SIMPLEPROGRESS;
//            if (wFunc != FO_DELETE) flags |= FOF_MULTIDESTFILES;
//            flags |= additionalFlags;

//            var op = new SHFILEOPSTRUCT
//            {
//                hwnd = _parentHwnd,
//                wFunc = wFunc,
//                pFrom = fromList,
//                pTo = toPath,
//                fFlags = flags,
//                fAnyOperationsAborted = false,
//                hNameMappings = IntPtr.Zero,
//                lpszProgressTitle = progressTitle
//            };

//            Debug.WriteLine($"[FileOpService] SHFileOperation: wFunc={wFunc}, from='{fromList.Replace("\0", "|")}', to='{toPath?.Replace("\0", "|")}', flags=0x{flags:X}");

//            return await Task.Run(() =>
//            {
//                try
//                {
//                    int result = SHFileOperation(ref op);
//                    Debug.WriteLine($"[FileOpService] SHFileOperation returned: result={result}, aborted={op.fAnyOperationsAborted}");
//                    return result == 0 && !op.fAnyOperationsAborted;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] SHFileOperation exception: {ex}");
//                    return false;
//                }
//            });
//        }

//        private static void ManualPasteFallback(List<string> sources, string dest, bool isCut)
//        {
//            // ... без изменений ...
//            Debug.WriteLine($"[FileOpService] ManualPasteFallback: isCut={isCut}, dest='{dest}', sources={sources.Count}");
//            foreach (var src in sources)
//            {
//                try
//                {
//                    string dst = Path.Combine(dest, Path.GetFileName(src));
//                    Debug.WriteLine($"[FileOpService] ManualPaste: processing '{src}' -> '{dst}'");
//                    if (isCut)
//                    {
//                        if (File.Exists(src)) File.Move(src, dst, true);
//                        else if (Directory.Exists(src)) Directory.Move(src, dst);
//                    }
//                    else
//                    {
//                        if (File.Exists(src)) File.Copy(src, dst, true);
//                        else if (Directory.Exists(src)) CopyDirRecursive(src, dst);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] ManualPaste: failed for '{src}': {ex.Message}");
//                }
//            }
//        }

//        private static void CopyDirRecursive(string sourceDir, string destDir)
//        {
//            // ... без изменений ...
//            try
//            {
//                Directory.CreateDirectory(destDir);
//                foreach (var file in Directory.GetFiles(sourceDir))
//                {
//                    string fileName = Path.GetFileName(file);
//                    File.Copy(file, Path.Combine(destDir, fileName), true);
//                }
//                foreach (var subDir in Directory.GetDirectories(sourceDir))
//                {
//                    string dirName = Path.GetFileName(subDir);
//                    CopyDirRecursive(subDir, Path.Combine(destDir, dirName));
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileOpService] CopyDirRecursive: error from '{sourceDir}' to '{destDir}': {ex.Message}");
//            }
//        }

//        // ===================== НОВЫЕ МЕТОДЫ =====================

//        private void OnClipboardContentChanged(object sender, object e)
//        {
//            // Вызываем событие в UI-потоке, чтобы обновить кнопки «Вставить»
//            _dispatcherQueue?.TryEnqueue(() => ClipboardChanged?.Invoke(this, EventArgs.Empty));
//        }

//        private async void SetClipboardContent(IReadOnlyList<string> paths, DataPackageOperation operation)
//        {
//            var dataPackage = new DataPackage
//            {
//                RequestedOperation = operation
//            };
//            var storageItems = new List<IStorageItem>();
//            foreach (var path in paths)
//            {
//                try
//                {
//                    if (Directory.Exists(path))
//                        storageItems.Add(await StorageFolder.GetFolderFromPathAsync(path));
//                    else if (File.Exists(path))
//                        storageItems.Add(await StorageFile.GetFileFromPathAsync(path));
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] SetClipboardContent: failed to get storage item for '{path}': {ex.Message}");
//                }
//            }
//            if (storageItems.Count > 0)
//            {
//                dataPackage.SetStorageItems(storageItems);
//                Clipboard.SetContent(dataPackage);
//                Debug.WriteLine($"[FileOpService] SetClipboardContent: {storageItems.Count} items placed into system clipboard.");
//            }
//            else
//            {
//                Debug.WriteLine("[FileOpService] SetClipboardContent: no valid storage items to set.");
//            }
//        }

//        // Реализация IDisposable (добавлена)
//        public void Dispose()
//        {
//            Clipboard.ContentChanged -= OnClipboardContentChanged;
//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks;
//using Core_FileManagement;
//using Microsoft.UI.Dispatching;
//using Windows.ApplicationModel.DataTransfer;
//using Windows.Storage;

//namespace ufm
//{
//    public class FileOperationService : IFileOperationService, IDisposable
//    {
//        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
//        private struct SHFILEOPSTRUCT
//        {
//            public IntPtr hwnd;
//            public uint wFunc;
//            [MarshalAs(UnmanagedType.LPWStr)]
//            public string pFrom;
//            [MarshalAs(UnmanagedType.LPWStr)]
//            public string pTo;
//            public ushort fFlags;
//            public bool fAnyOperationsAborted;
//            public IntPtr hNameMappings;
//            public string lpszProgressTitle;
//        }

//        private const uint FO_COPY = 0x0002;
//        private const uint FO_DELETE = 0x0003;
//        private const uint FO_MOVE = 0x0001;

//        private const ushort FOF_ALLOWUNDO = 0x0040;
//        private const ushort FOF_NOCONFIRMATION = 0x0010;
//        private const ushort FOF_SIMPLEPROGRESS = 0x0100;
//        private const ushort FOF_MULTIDESTFILES = 0x0200;

//        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
//        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

//        private List<string> _sourcePaths = new();
//        private bool _isCut;
//        private IntPtr _parentHwnd = IntPtr.Zero;
//        private DispatcherQueue _dispatcherQueue;
//        private volatile bool _isPasting;

//        public bool CanPaste
//        {
//            get
//            {
//                Debug.WriteLine($"[FileOpService] CanPaste: checking internal _sourcePaths.Count={_sourcePaths.Count}");
//                if (_sourcePaths.Count > 0)
//                    return true;
//                try
//                {
//                    var data = Clipboard.GetContent();
//                    bool hasStorageItems = data.Contains(StandardDataFormats.StorageItems);
//                    Debug.WriteLine($"[FileOpService] CanPaste: system clipboard has StorageItems={hasStorageItems}");
//                    return hasStorageItems;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] CanPaste: error reading clipboard: {ex.Message}");
//                    return false;
//                }
//            }
//        }

//        public event EventHandler ClipboardChanged;

//        public void SetParentWindow(IntPtr hwnd)
//        {
//            Debug.WriteLine($"[FileOpService] SetParentWindow: hwnd = {hwnd}");
//            _parentHwnd = hwnd;
//        }

//        public void Initialize(IntPtr parentHwnd, DispatcherQueue dispatcher)
//        {
//            Debug.WriteLine($"[FileOpService] Initialize: parentHwnd={parentHwnd}, dispatcher={dispatcher != null}");
//            _parentHwnd = parentHwnd;
//            _dispatcherQueue = dispatcher;
//            Clipboard.ContentChanged += OnClipboardContentChanged;
//            Debug.WriteLine("[FileOpService] Initialize: subscribed to Clipboard.ContentChanged");
//        }

//        public void Copy(IEnumerable<ExplorerItemViewModel> items)
//        {
//            Debug.WriteLine($"[FileOpService] Copy: called with items={(items != null ? "not null" : "null")}");
//            if (items == null)
//            {
//                Debug.WriteLine("[FileOpService] Copy: items is null, returning.");
//                return;
//            }

//            var itemsList = items.ToList();
//            Debug.WriteLine($"[FileOpService] Copy: itemsList.Count={itemsList.Count}");
//            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
//            Debug.WriteLine($"[FileOpService] Copy: collected {paths.Count} paths: {string.Join(", ", paths)}");

//            if (paths.Count == 0)
//                return;

//            _sourcePaths = paths;
//            _isCut = false;
//            Debug.WriteLine($"[FileOpService] Copy: _sourcePaths set, _isCut={_isCut}");
//            SetClipboardContent(paths, DataPackageOperation.Copy);
//            ClipboardChanged?.Invoke(this, EventArgs.Empty);
//            Debug.WriteLine("[FileOpService] Copy: finished.");
//        }

//        public void Cut(IEnumerable<ExplorerItemViewModel> items)
//        {
//            Debug.WriteLine($"[FileOpService] Cut: called with items={(items != null ? "not null" : "null")}");
//            if (items == null)
//            {
//                Debug.WriteLine("[FileOpService] Cut: items is null, returning.");
//                return;
//            }

//            var itemsList = items.ToList();
//            Debug.WriteLine($"[FileOpService] Cut: itemsList.Count={itemsList.Count}");
//            var paths = itemsList.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
//            Debug.WriteLine($"[FileOpService] Cut: collected {paths.Count} paths: {string.Join(", ", paths)}");

//            if (paths.Count == 0)
//                return;

//            _sourcePaths = paths;
//            _isCut = true;
//            Debug.WriteLine($"[FileOpService] Cut: _sourcePaths set, _isCut={_isCut}");
//            SetClipboardContent(paths, DataPackageOperation.Move);
//            ClipboardChanged?.Invoke(this, EventArgs.Empty);
//            Debug.WriteLine("[FileOpService] Cut: finished.");
//        }

//        public async Task PasteAsync(string destinationFolder)
//        {
//            Debug.WriteLine($"[FileOpService] PasteAsync: START destinationFolder='{destinationFolder}', _isPasting={_isPasting}");
//            if (_isPasting)
//            {
//                Debug.WriteLine("[FileOpService] PasteAsync: already pasting, exiting.");
//                return;
//            }

//            if (string.IsNullOrEmpty(destinationFolder) || !Directory.Exists(destinationFolder))
//            {
//                Debug.WriteLine($"[FileOpService] PasteAsync: invalid destination. exists={Directory.Exists(destinationFolder)}");
//                return;
//            }

//            List<string> sources;
//            bool isCut;

//            if (_sourcePaths.Count > 0)
//            {
//                sources = _sourcePaths.ToList();
//                isCut = _isCut;
//                Debug.WriteLine($"[FileOpService] PasteAsync: using internal clipboard. sources count={sources.Count}, isCut={isCut}");
//                _sourcePaths.Clear();
//                _isCut = false;
//                ClipboardChanged?.Invoke(this, EventArgs.Empty);
//            }
//            else
//            {
//                Debug.WriteLine("[FileOpService] PasteAsync: internal clipboard empty, trying system clipboard.");
//                try
//                {
//                    var dataPackageView = Clipboard.GetContent();
//                    Debug.WriteLine($"[FileOpService] PasteAsync: Clipboard.GetContent() returned dataPackageView of type {dataPackageView.GetType().FullName}");
//                    bool containsStorage = dataPackageView.Contains(StandardDataFormats.StorageItems);
//                    Debug.WriteLine($"[FileOpService] PasteAsync: contains StorageItems={containsStorage}");

//                    if (containsStorage)
//                    {
//                        IReadOnlyList<IStorageItem> storageItems = await dataPackageView.GetStorageItemsAsync();
//                        Debug.WriteLine($"[FileOpService] PasteAsync: received {storageItems.Count} StorageItems from system clipboard.");
//                        foreach (var item in storageItems)
//                        {
//                            Debug.WriteLine($"  -> StorageItem: Path={item.Path}, IsDirectory={item.IsOfType(StorageItemTypes.Folder)}, Name={System.IO.Path.GetFileName(item.Path)}");
//                        }
//                        sources = storageItems.Select(i => i.Path).ToList();
//                        var requestedOp = dataPackageView.RequestedOperation;
//                        isCut = (requestedOp == DataPackageOperation.Move);
//                        Debug.WriteLine($"[FileOpService] PasteAsync: RequestedOperation={requestedOp}, isCut={isCut}");
//                    }
//                    else
//                    {
//                        Debug.WriteLine("[FileOpService] PasteAsync: system clipboard does NOT contain StorageItems. Exiting.");
//                        return;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] PasteAsync: FAILED to read system clipboard: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
//                    return;
//                }
//            }

//            if (sources.Count == 0)
//            {
//                Debug.WriteLine("[FileOpService] PasteAsync: no sources to paste. Returning.");
//                return;
//            }

//            _isPasting = true;
//            try
//            {
//                string title = isCut ? "Перемещение..." : "Копирование...";
//                uint operation = isCut ? FO_MOVE : FO_COPY;
//                Debug.WriteLine($"[FileOpService] PasteAsync: executing SHFileOperation with op={operation}, sources={sources.Count}, dest='{destinationFolder}', title='{title}'");

//                bool ok = await RunOperationWithProgressAsync(operation, sources, destinationFolder, title);
//                Debug.WriteLine($"[FileOpService] PasteAsync: SHFileOperation result={ok}");

//                if (!ok)
//                {
//                    Debug.WriteLine("[FileOpService] PasteAsync: SHFileOperation failed, using manual fallback.");
//                    await Task.Run(() => ManualPasteFallback(sources, destinationFolder, isCut));
//                }

//                if (isCut)
//                {
//                    try
//                    {
//                        Clipboard.Clear();
//                        Debug.WriteLine("[FileOpService] PasteAsync: system clipboard cleared after move.");
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"[FileOpService] PasteAsync: error clearing clipboard: {ex.Message}");
//                    }
//                }
//            }
//            finally
//            {
//                _isPasting = false;
//                Debug.WriteLine("[FileOpService] PasteAsync: finished.");
//            }
//        }

//        public async Task DeleteAsync(IEnumerable<ExplorerItemViewModel> items)
//        {
//            if (items == null)
//            {
//                Debug.WriteLine("[FileOpService] DeleteAsync: items is null, returning.");
//                return;
//            }

//            var itemsList = items.ToList();
//            var paths = itemsList
//                .Select(i => i.FilePath)
//                .Where(p => !string.IsNullOrEmpty(p) && (File.Exists(p) || Directory.Exists(p)))
//                .ToList();

//            Debug.WriteLine($"[FileOpService] DeleteAsync: {paths.Count} items. Paths: {string.Join(", ", paths)}");

//            if (paths.Count == 0)
//                return;

//            bool ok = await RunOperationWithProgressAsync(FO_DELETE, paths, null, "Удаление...", additionalFlags: FOF_ALLOWUNDO);
//            Debug.WriteLine($"[FileOpService] DeleteAsync: SHFileOperation result = {ok}");

//            if (!ok)
//            {
//                await Task.Run(() =>
//                {
//                    foreach (var path in paths)
//                    {
//                        try
//                        {
//                            if (File.Exists(path)) File.Delete(path);
//                            else if (Directory.Exists(path)) Directory.Delete(path, true);
//                        }
//                        catch (Exception ex)
//                        {
//                            Debug.WriteLine($"[FileOpService] DeleteAsync: manual delete failed for '{path}': {ex.Message}");
//                        }
//                    }
//                });
//            }
//        }

//        private async Task<bool> RunOperationWithProgressAsync(
//            uint wFunc,
//            List<string> sourcePaths,
//            string destinationFolder,
//            string progressTitle,
//            ushort additionalFlags = 0)
//        {
//            string fromList = string.Join("\0", sourcePaths) + "\0\0";
//            string toPath = (destinationFolder != null) ? destinationFolder + "\0\0" : null;

//            ushort flags = FOF_SIMPLEPROGRESS;
//            if (wFunc != FO_DELETE) flags |= FOF_MULTIDESTFILES;
//            flags |= additionalFlags;

//            var op = new SHFILEOPSTRUCT
//            {
//                hwnd = _parentHwnd,
//                wFunc = wFunc,
//                pFrom = fromList,
//                pTo = toPath,
//                fFlags = flags,
//                fAnyOperationsAborted = false,
//                hNameMappings = IntPtr.Zero,
//                lpszProgressTitle = progressTitle
//            };

//            Debug.WriteLine($"[FileOpService] SHFileOperation: wFunc={wFunc}, from='{fromList.Replace("\0", "|")}', to='{toPath?.Replace("\0", "|")}', flags=0x{flags:X}");

//            return await Task.Run(() =>
//            {
//                try
//                {
//                    int result = SHFileOperation(ref op);
//                    Debug.WriteLine($"[FileOpService] SHFileOperation returned: result={result}, aborted={op.fAnyOperationsAborted}");
//                    return result == 0 && !op.fAnyOperationsAborted;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] SHFileOperation exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
//                    return false;
//                }
//            });
//        }

//        private static void ManualPasteFallback(List<string> sources, string dest, bool isCut)
//        {
//            Debug.WriteLine($"[FileOpService] ManualPasteFallback: isCut={isCut}, dest='{dest}', sources={sources.Count}");
//            foreach (var src in sources)
//            {
//                try
//                {
//                    string dst = System.IO.Path.Combine(dest, System.IO.Path.GetFileName(src));
//                    Debug.WriteLine($"[FileOpService] ManualPaste: processing '{src}' -> '{dst}'");
//                    if (isCut)
//                    {
//                        if (File.Exists(src)) File.Move(src, dst, true);
//                        else if (Directory.Exists(src)) Directory.Move(src, dst);
//                    }
//                    else
//                    {
//                        if (File.Exists(src)) File.Copy(src, dst, true);
//                        else if (Directory.Exists(src)) CopyDirRecursive(src, dst);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] ManualPaste: failed for '{src}': {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
//                }
//            }
//        }

//        private static void CopyDirRecursive(string sourceDir, string destDir)
//        {
//            try
//            {
//                Directory.CreateDirectory(destDir);
//                foreach (var file in Directory.GetFiles(sourceDir))
//                {
//                    string fileName = System.IO.Path.GetFileName(file);
//                    File.Copy(file, System.IO.Path.Combine(destDir, fileName), true);
//                }
//                foreach (var subDir in Directory.GetDirectories(sourceDir))
//                {
//                    string dirName = System.IO.Path.GetFileName(subDir);
//                    CopyDirRecursive(subDir, System.IO.Path.Combine(destDir, dirName));
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileOpService] CopyDirRecursive: error from '{sourceDir}' to '{destDir}': {ex.Message}");
//            }
//        }

//        private void OnClipboardContentChanged(object sender, object e)
//        {
//            Debug.WriteLine("[FileOpService] OnClipboardContentChanged: clipboard content changed.");
//            _dispatcherQueue?.TryEnqueue(() =>
//            {
//                Debug.WriteLine("[FileOpService] OnClipboardContentChanged: invoking ClipboardChanged on UI thread.");
//                ClipboardChanged?.Invoke(this, EventArgs.Empty);
//            });
//        }

//        private async void SetClipboardContent(IReadOnlyList<string> paths, DataPackageOperation operation)
//        {
//            var dataPackage = new DataPackage
//            {
//                RequestedOperation = operation
//            };
//            var storageItems = new List<IStorageItem>();
//            foreach (var path in paths)
//            {
//                try
//                {
//                    if (Directory.Exists(path))
//                        storageItems.Add(await StorageFolder.GetFolderFromPathAsync(path));
//                    else if (File.Exists(path))
//                        storageItems.Add(await StorageFile.GetFileFromPathAsync(path));
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[FileOpService] SetClipboardContent: failed to get storage item for '{path}': {ex.Message}");
//                }
//            }
//            if (storageItems.Count > 0)
//            {
//                dataPackage.SetStorageItems(storageItems);
//                // Вызываем SetContent в UI‑потоке, чтобы избежать InvalidOperationException
//                _dispatcherQueue.TryEnqueue(() =>
//                {
//                    Clipboard.SetContent(dataPackage);
//                    Debug.WriteLine($"[FileOpService] SetClipboardContent: {storageItems.Count} items placed into system clipboard.");
//                });
//            }
//            else
//            {
//                Debug.WriteLine("[FileOpService] SetClipboardContent: no valid storage items to set.");
//            }
//        }

//        public void Dispose()
//        {
//            Debug.WriteLine("[FileOpService] Dispose: unsubscribing from Clipboard.ContentChanged");
//            Clipboard.ContentChanged -= OnClipboardContentChanged;
//        }
//    }
//}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Core_FileManagement;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace ufm
{
    public class FileOperationService : IFileOperationService, IDisposable
    {
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

        private List<string> _sourcePaths = new();
        private bool _isCut;
        private IntPtr _parentHwnd = IntPtr.Zero;
        private DispatcherQueue _dispatcherQueue;
        private volatile bool _isPasting;

        public bool CanPaste
        {
            get
            {
                if (_sourcePaths.Count > 0) return true;
                try
                {
                    var data = Clipboard.GetContent();
                    return data.Contains(StandardDataFormats.StorageItems);
                }
                catch { return false; }
            }
        }

        public event EventHandler ClipboardChanged;

        public void SetParentWindow(IntPtr hwnd)
        {
            _parentHwnd = hwnd;
        }

        public void Initialize(IntPtr parentHwnd, DispatcherQueue dispatcher)
        {
            _parentHwnd = parentHwnd;
            _dispatcherQueue = dispatcher;
            Clipboard.ContentChanged += OnClipboardContentChanged;
        }

        public void Copy(IEnumerable<ExplorerItemViewModel> items)
        {
            if (items == null) return;
            var paths = items.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
            if (paths.Count == 0) return;
            _sourcePaths = paths;
            _isCut = false;
            SetClipboardContent(paths, DataPackageOperation.Copy);
            RaiseClipboardChanged();
        }

        public void Cut(IEnumerable<ExplorerItemViewModel> items)
        {
            if (items == null) return;
            var paths = items.Select(i => i.FilePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
            if (paths.Count == 0) return;
            _sourcePaths = paths;
            _isCut = true;
            SetClipboardContent(paths, DataPackageOperation.Move);
            RaiseClipboardChanged();
        }

        public async Task PasteAsync(string destinationFolder)
        {
            if (_isPasting) return;
            if (string.IsNullOrEmpty(destinationFolder) || !Directory.Exists(destinationFolder)) return;

            List<string> sources;
            bool isCut;

            if (_sourcePaths.Count > 0)
            {
                sources = _sourcePaths.ToList();
                isCut = _isCut;
                _sourcePaths.Clear();
                _isCut = false;
                RaiseClipboardChanged();
            }
            else
            {
                var dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.StorageItems))
                {
                    var storageItems = await dataPackageView.GetStorageItemsAsync();
                    sources = storageItems.Select(i => i.Path).ToList();
                    isCut = dataPackageView.RequestedOperation == DataPackageOperation.Move;
                }
                else return;
            }

            if (sources.Count == 0) return;

            _isPasting = true;
            try
            {
                string title = isCut ? "Перемещение..." : "Копирование...";
                uint operation = isCut ? FO_MOVE : FO_COPY;

                bool ok = await RunOperationWithProgressAsync(operation, sources, destinationFolder, title);
                if (!ok)
                    await Task.Run(() => ManualPasteFallback(sources, destinationFolder, isCut));

                if (isCut)
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        Clipboard.Clear();
                        Debug.WriteLine("[FileOpService] PasteAsync: system clipboard cleared after move.");
                    });
                }
            }
            finally
            {
                _isPasting = false;
            }
        }

        public async Task DeleteAsync(IEnumerable<ExplorerItemViewModel> items)
        {
            if (items == null) return;
            var paths = items.Select(i => i.FilePath)
                             .Where(p => !string.IsNullOrEmpty(p) && (File.Exists(p) || Directory.Exists(p)))
                             .ToList();
            if (paths.Count == 0) return;

            bool ok = await RunOperationWithProgressAsync(FO_DELETE, paths, null, "Удаление...", additionalFlags: FOF_ALLOWUNDO);
            if (!ok)
            {
                await Task.Run(() =>
                {
                    foreach (var path in paths)
                    {
                        try
                        {
                            if (File.Exists(path)) File.Delete(path);
                            else if (Directory.Exists(path)) Directory.Delete(path, true);
                        }
                        catch { }
                    }
                });
            }
        }

        public async Task<bool> RunOperationWithProgressAsync(
            uint wFunc, List<string> sourcePaths, string destinationFolder,
            string progressTitle, ushort additionalFlags = 0)
        {
            string fromList = string.Join("\0", sourcePaths) + "\0\0";
            string toPath = (destinationFolder != null) ? destinationFolder + "\0\0" : null;

            ushort flags = FOF_SIMPLEPROGRESS;
            if (wFunc != FO_DELETE) flags |= FOF_MULTIDESTFILES;
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

            return await Task.Run(() =>
            {
                try
                {
                    int result = SHFileOperation(ref op);
                    return result == 0 && !op.fAnyOperationsAborted;
                }
                catch { return false; }
            });
        }

        public static void ManualPasteFallback(List<string> sources, string dest, bool isCut)
        {
            foreach (var src in sources)
            {
                try
                {
                    string dst = System.IO.Path.Combine(dest, System.IO.Path.GetFileName(src));
                    if (isCut)
                    {
                        if (File.Exists(src)) File.Move(src, dst, true);
                        else if (Directory.Exists(src)) Directory.Move(src, dst);
                    }
                    else
                    {
                        if (File.Exists(src)) File.Copy(src, dst, true);
                        else if (Directory.Exists(src)) CopyDirRecursive(src, dst);
                    }
                }
                catch { }
            }
        }

        private static void CopyDirRecursive(string sourceDir, string destDir)
        {
            try
            {
                Directory.CreateDirectory(destDir);
                foreach (var file in Directory.GetFiles(sourceDir))
                    File.Copy(file, System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(file)), true);
                foreach (var subDir in Directory.GetDirectories(sourceDir))
                    CopyDirRecursive(subDir, System.IO.Path.Combine(destDir, System.IO.Path.GetFileName(subDir)));
            }
            catch { }
        }

        private void RaiseClipboardChanged()
        {
            _dispatcherQueue.TryEnqueue(() => ClipboardChanged?.Invoke(this, EventArgs.Empty));
        }

        private void OnClipboardContentChanged(object sender, object e)
        {
            RaiseClipboardChanged();
        }

        private async void SetClipboardContent(IReadOnlyList<string> paths, DataPackageOperation operation)
        {
            var dataPackage = new DataPackage { RequestedOperation = operation };
            var storageItems = new List<IStorageItem>();
            foreach (var path in paths)
            {
                try
                {
                    if (Directory.Exists(path))
                        storageItems.Add(await StorageFolder.GetFolderFromPathAsync(path));
                    else if (File.Exists(path))
                        storageItems.Add(await StorageFile.GetFileFromPathAsync(path));
                }
                catch { }
            }
            if (storageItems.Count > 0)
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    dataPackage.SetStorageItems(storageItems);
                    Clipboard.SetContent(dataPackage);
                });
            }
        }

        public void Dispose()
        {
            Clipboard.ContentChanged -= OnClipboardContentChanged;
        }
    }
}