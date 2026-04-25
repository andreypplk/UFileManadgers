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
            CalculateSize();
        }

        public DirectoryViewModel(DirectoryInfo directoryInfo, EntityFlags flags) : base(directoryInfo.Name, flags)
        {
            FullName = directoryInfo.FullName;
            CalculateSize();
        }

        private void CalculateSize()
        {
            try
            {
                var dirInfo = new DirectoryInfo(FullName);
                // Ограничиваем сканирование первым уровнем для производительности
                Size = dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Sum(f => f.Length);
            }
            catch
            {
                Size = 0;
            }
        }
    }
}