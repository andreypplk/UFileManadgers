using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Core_FileManagement
{
    public class DirectoryHistory : IDirectoryHistory, IImageSourceProvider, IDisposable
    {
        private DirectoryNode _head;
        private bool _disposed = false;

        public bool CanMoveBack => Current.PreviousNode != null;
        public bool CanMoveForward => Current.NextNode != null;
        public DirectoryNode Current { get; private set; }

        #region Events
        public event EventHandler HistoryChanged;
        #endregion

        #region Constructor
        public DirectoryHistory(string directoryPath, string directoryPathName)
        {
            _head = new DirectoryNode(directoryPath, directoryPathName);
            Current = _head;
        }
        #endregion

        public void Add(string filePath, string name)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DirectoryHistory));

            var node = new DirectoryNode(filePath, name);
            Current.NextNode = node;
            node.PreviousNode = Current;
            Current = node;

            RaiseHistoryChanged();
        }

        public void MoveBack()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DirectoryHistory));

            var prev = Current.PreviousNode;
            Current = prev;
            RaiseHistoryChanged();
        }

        public void MoveForward()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DirectoryHistory));

            var next = Current.NextNode;
            Current = next;
            RaiseHistoryChanged();
        }

        #region Private Method
        private void RaiseHistoryChanged()
        {
            if (_disposed) return;
            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Enumerator
        public IEnumerator<DirectoryNode> GetEnumerator()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DirectoryHistory));
            yield return Current;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        public BitmapImage ImageSource { get; set; }

        #region IDisposable Implementation
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы
                    ImageSource = null;
                    HistoryChanged = null;

                    // Очищаем цепочку узлов
                    var current = _head;
                    while (current != null)
                    {
                        var next = current.NextNode;
                        current.NextNode = null;
                        current.PreviousNode = null;
                        current = next;
                    }

                    _head = null;
                    Current = null;
                }

                // Освобождаем неуправляемые ресурсы (если есть)

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DirectoryHistory()
        {
            Dispose(false);
        }
        #endregion
    }
}