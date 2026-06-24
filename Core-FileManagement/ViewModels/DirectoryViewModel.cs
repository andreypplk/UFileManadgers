using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using System.Linq;

namespace Core_FileManagement
{
    public class DirectoryViewModel : FileEntityViewModel
    {
        public long Size { get; private set; }
        public new BitmapImage ImageSource { get; set; }

        public DirectoryViewModel(string directoryName, EntityFlags flags) : base(directoryName, flags)
        {
            FullName = directoryName;
            // Размер больше не вычисляется немедленно – это главная причина зависаний
            Size = -1;  // -1 означает «ещё не вычислено»
        }

        public DirectoryViewModel(DirectoryInfo directoryInfo, EntityFlags flags) : base(directoryInfo.Name, flags)
        {
            FullName = directoryInfo.FullName;
            // Размер папки не вычисляем, чтобы открытие было мгновенным
            Size = -1;
        }

        // При необходимости размер можно запросить позже (например, для строки статуса)
        public void RequestSizeCalculationAsync()
        {
            if (Size < 0)
                CalculateSize();
        }

        private void CalculateSize()
        {
            try
            {
                var dirInfo = new DirectoryInfo(FullName);
                Size = dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Sum(f => f.Length);
            }
            catch
            {
                Size = 0;
            }
        }
    }
}