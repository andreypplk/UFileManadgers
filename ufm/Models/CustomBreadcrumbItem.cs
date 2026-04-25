using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace ufm.Models
{
    public class CustomBreadcrumbItem
    {
        /// <summary>
        /// Отображаемое имя
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Полный путь (или специальный ключ типа "MyComputer")
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// Иконка элемента (BitmapImage / ImageSource)
        /// </summary>
        public ImageSource Icon { get; set; }

        /// <summary>
        /// Дочерние элементы для выпадающего списка
        /// </summary>
        public ObservableCollection<CustomBreadcrumbItem> Children { get; set; } = new ObservableCollection<CustomBreadcrumbItem>();
    }
}