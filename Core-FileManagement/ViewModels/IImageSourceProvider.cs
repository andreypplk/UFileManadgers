using Microsoft.UI.Xaml.Media.Imaging;

namespace Core_FileManagement
{
    public interface IImageSourceProvider
    {
        BitmapImage ImageSource { get; set; }
    }
}
