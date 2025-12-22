using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI;
using Windows.Storage;
using Microsoft.UI.Windowing;
using WinRT.Interop;

namespace ufm
{
    public class WindowHelper
    {
        // Создание нового окна с фоном Mica
        static public Window CreateWindow()
        {
            Window newWindow = new MainWindow()
            {
                SystemBackdrop = new MicaBackdrop() // Установка фона Mica для окна
            };
            TrackWindow(newWindow); // Отслеживание нового окна
            return newWindow; // Возвращение нового окна
        }

        // Отслеживание окна и удаление его из списка активных окон при закрытии
        static public void TrackWindow(Window window)
        {
            window.Closed += (sender, args) => {
                _activeWindows.Remove(window); // Удаление окна из списка активных окон при закрытии
            };
            _activeWindows.Add(window); // Добавление окна в список активных окон
        }

        // Получение AppWindow из Window
        static public AppWindow GetAppWindow(Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window); // Получение дескриптора окна
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd); // Получение идентификатора окна
            return AppWindow.GetFromWindowId(wndId); // Возвращение AppWindow по идентификатору окна
        }

        // Получение окна для элемента UI
        static public Window GetWindowForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return window; // Возвращение окна, если XamlRoot элемента совпадает с XamlRoot окна
                    }
                }
            }
            return null; // Возвращение null, если окно не найдено
        }

        // Получение масштаба растеризации для элемента UI
        static public double GetRasterizationScaleForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return element.XamlRoot.RasterizationScale; // Возвращение масштаба растеризации для элемента
                    }
                }
            }
            return 0.0; // Возвращение 0.0, если окно не найдено
        }

        // Список активных окон
        static public List<Window> ActiveWindows { get { return _activeWindows; } }

        // Приватное поле для хранения списка активных окон
        static private List<Window> _activeWindows = new List<Window>();

        // Получение локальной папки приложения
        static public StorageFolder GetAppLocalFolder()
        {
            StorageFolder localFolder;
            if (!NativeHelper.IsAppPackaged)
            {
                // Получение папки из пути, если приложение не упаковано
                localFolder = Task.Run(async () => await StorageFolder.GetFolderFromPathAsync(System.AppContext.BaseDirectory)).Result;
            }
            else
            {
                // Получение локальной папки приложения, если приложение упаковано
                localFolder = ApplicationData.Current.LocalFolder;
            }
            return localFolder; // Возвращение локальной папки
        }

        internal static object GetWindowForElement(TabViewManager tabViewManager)
        {
            // Получаем родительский объект Window для указанного TabViewManager
            var window = Window.Current; // Получаем текущее окно

            if (window != null)
            {
                return window;
            }
            else
            {
                throw new InvalidOperationException("Unable to find the parent window for the provided TabViewManager.");
            }
        }

    }
}

