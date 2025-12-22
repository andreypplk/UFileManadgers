//namespace Core_FileManagement
//{
//    public class DirectoryNode
//    {
//        public DirectoryNode PreviousNode { get; set; }
//        public DirectoryNode NextNode { get; set; }
//        public string DirectoryPath { get; }
//        public string DirectoryPathName { get; }

//        public DirectoryNode(string directoryPath, string directoryPathName)
//        {
//            DirectoryPath = directoryPath;
//            DirectoryPathName = directoryPathName;
//        }
//    }
//}

using System;

namespace Core_FileManagement
{
    public class DirectoryNode : IDisposable
    {
        private bool _disposed = false;

        public DirectoryNode PreviousNode { get; set; }
        public DirectoryNode NextNode { get; set; }
        public string DirectoryPath { get; }
        public string DirectoryPathName { get; }

        public DirectoryNode(string directoryPath, string directoryPathName)
        {
            DirectoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
            DirectoryPathName = directoryPathName ?? throw new ArgumentNullException(nameof(directoryPathName));
        }

        // Метод для проверки, является ли узел корневым
        public bool IsRoot => PreviousNode == null;

        // Метод для проверки, является ли узел конечным
        public bool IsTail => NextNode == null;

        // Метод для отцепления узла от списка
        public void Detach()
        {
            if (PreviousNode != null)
            {
                PreviousNode.NextNode = NextNode;
            }

            if (NextNode != null)
            {
                NextNode.PreviousNode = PreviousNode;
            }

            PreviousNode = null;
            NextNode = null;
        }

        // Реализация IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Очищаем связи с другими узлами
                    Detach();
                }

                // Здесь можно освободить неуправляемые ресурсы, если они есть

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DirectoryNode()
        {
            Dispose(false);
        }

        // Переопределение ToString для удобства отладки
        public override string ToString()
        {
            return $"{DirectoryPathName} ({DirectoryPath})";
        }
    }
}