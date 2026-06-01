using System;
using System.Collections.Generic;
using System.Text;

namespace ufm
{
    /// <summary>
    /// Приёмник перетаскивания – сообщает целевую папку и визуально подсвечивает элемент.
    /// </summary>
    public interface IDropTarget
    {
        /// <summary>Целевая директория, в которую будет выполнена вставка, или null.</summary>
        string GetTargetFolder();
    }
}
