using System.Collections.Generic;

namespace ufm
{
    /// <summary>
    /// Универсальный класс для сохранения/восстановления состояния узлов дерева,
    /// содержимого папок и отдельных файлов/папок.
    /// </summary>
    public class SnapshotNode
    {
        // Обязательные общие поля
        public string Path { get; set; }
        public string Name { get; set; }

        // Поля для узлов дерева
        public bool IsExpanded { get; set; }
        public bool HasUnrealizedChildren { get; set; }
        public string TreeId { get; set; }   // "MainTree" или "SpFTree", для папок – null

        // Поля для элементов папки
        public bool IsDirectory { get; set; }
        public long Length { get; set; }

        // Вложенные элементы (дети дерева или содержимое папки)
        public List<SnapshotNode> Children { get; set; } = new List<SnapshotNode>();
    }
}