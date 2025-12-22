using System;
using System.Collections.Generic;

namespace Core_FileManagement
{
    public interface IDirectoryHistory : IEnumerable<DirectoryNode>, IDisposable
    {
        bool CanMoveBack { get; }
        bool CanMoveForward { get; }

        DirectoryNode Current { get; }
        event EventHandler HistoryChanged;

        void Add(string filePath, string name);
        void MoveBack();
        void MoveForward();
    }
}
