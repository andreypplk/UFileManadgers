using System.Collections.Generic;

namespace ufm
{
    /// <summary>
    /// Источник перетаскивания – предоставляет пути файлов/папок,
    /// которые нужно включить в DataPackage.
    /// </summary>
    public interface IDragSource
    {
        IReadOnlyList<string> GetDraggedPaths();
    }
}