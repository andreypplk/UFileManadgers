using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core_FileManagement;   // Обязательно для ExplorerItemViewModel

namespace ufm
{
    public interface IFileOperationService
    {
        bool CanPaste { get; }
        void Copy(IEnumerable<ExplorerItemViewModel> items);
        void Cut(IEnumerable<ExplorerItemViewModel> items);
        Task PasteAsync(string destinationFolder);
        Task DeleteAsync(IEnumerable<ExplorerItemViewModel> items);
        event EventHandler ClipboardChanged;
    }
}