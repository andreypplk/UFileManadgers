using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Core_FileManagement;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ufm
{
    public class DragDropService
    {
        private readonly IFileOperationService _fileOperationService;
        private readonly IModifierKeyService _modifierKeyService;
        private readonly DispatcherQueue _dispatcherQueue;

        public DragDropService(IFileOperationService fileOperationService,
                               IModifierKeyService modifierKeyService)
        {
            _fileOperationService = fileOperationService ?? throw new ArgumentNullException(nameof(fileOperationService));
            _modifierKeyService = modifierKeyService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            Debug.WriteLine("[DragDropService] Initialized");
        }

        // ----------------------------------------------------------------
        // Заполнение DataPackage для исходящего перетаскивания
        // ----------------------------------------------------------------
        public void OnDragItemsStarting(IReadOnlyList<string> paths, DragItemsStartingEventArgs e)
        {
            Debug.WriteLine($"[DragDropService] OnDragItemsStarting (ListView/GridView). Paths: {paths.Count}");
            FillDataPackage(paths, e.Data);
        }

        public void OnDragItemsStarting(IReadOnlyList<string> paths, TreeViewDragItemsStartingEventArgs e)
        {
            Debug.WriteLine($"[DragDropService] OnDragItemsStarting (TreeView). Paths: {paths.Count}");
            FillDataPackage(paths, e.Data);
        }

        private async void FillDataPackage(IReadOnlyList<string> paths, DataPackage dataPackage)
        {
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
                catch (Exception ex)
                {
                    Debug.WriteLine($"  Cannot get StorageItem for {path}: {ex.Message}");
                }
            }

            if (storageItems.Count > 0)
            {
                dataPackage.SetStorageItems(storageItems);
                dataPackage.RequestedOperation = DataPackageOperation.Copy | DataPackageOperation.Move;
                Debug.WriteLine($"[DragDropService] DataPackage ready, {storageItems.Count} items");
            }
            else
            {
                Debug.WriteLine("[DragDropService] WARNING: No StorageItems created!");
            }
        }

        // ----------------------------------------------------------------
        // DragOver – установка AcceptedOperation
        // ----------------------------------------------------------------
        public void OnDragOver(DragEventArgs e, string targetFolder, bool isFromExternal)
        {
            Debug.WriteLine($"[DragDropService] OnDragOver. target='{targetFolder}', external={isFromExternal}");

            if (!e.DataView.Contains(StandardDataFormats.StorageItems) || string.IsNullOrEmpty(targetFolder))
            {
                e.AcceptedOperation = DataPackageOperation.None;
                Debug.WriteLine("  -> AcceptedOperation = None");
                return;
            }

            var (ctrl, shift, _) = _modifierKeyService.GetCurrentState();
            Debug.WriteLine($"  Modifiers: Ctrl={ctrl}, Shift={shift}");

            DataPackageOperation proposed;
            if (isFromExternal)
            {
                proposed = DataPackageOperation.Copy;
                if (shift) proposed = DataPackageOperation.Move;
            }
            else
            {
                proposed = DataPackageOperation.Move;
                if (ctrl) proposed = DataPackageOperation.Copy;
                else if (shift) proposed = DataPackageOperation.Move;
            }

            e.AcceptedOperation = proposed;
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
            Debug.WriteLine($"  -> AcceptedOperation = {proposed}");
        }

        public async Task OnDropAsync(DragEventArgs e, string targetFolder)
        {
            _modifierKeyService.UpdateKeyStateFromCore();

            if (!e.DataView.Contains(StandardDataFormats.StorageItems) || string.IsNullOrEmpty(targetFolder))
                return;

            var operation = e.AcceptedOperation;
            var (ctrl, shift, _) = _modifierKeyService.GetCurrentState();
            if (ctrl) operation = DataPackageOperation.Copy;
            else if (shift) operation = DataPackageOperation.Move;

            var items = await e.DataView.GetStorageItemsAsync();
            var paths = items.Select(i => i.Path).ToList();

            bool isMove = operation == DataPackageOperation.Move;
            var fop = _fileOperationService as FileOperationService;
            if (fop != null)
            {
                uint wFunc = isMove ? 0x0001u /* FO_MOVE */ : 0x0002u /* FO_COPY */;
                string title = isMove ? "Перемещение..." : "Копирование...";
                bool ok = await fop.RunOperationWithProgressAsync(wFunc, paths, targetFolder, title);
                if (!ok)
                    await Task.Run(() => FileOperationService.ManualPasteFallback(paths, targetFolder, isMove));
            }
        }
    }
}