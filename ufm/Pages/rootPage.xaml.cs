using System;
using System.Diagnostics;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ufm.Pages;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ufm
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class rootPage : Page
    {
        private double savedWidth = -1;
        private double savedWidthViewDataPanel = -1;
        private double savedMinWidth = -1;
        private double savedMaxWidth = -1;
        private double savedMinWidthViewDataPanel = -1;
        private double savedMaxWidthViewDataPanel = -1;
        private bool isCollapsedrootPageTreePanelandViewDataPanel = false;
        private bool isInitialized = false;
        private bool isResizing = false;


        private DispatcherTimer saveTimer = new DispatcherTimer();
        public TabView ParentTabView { get; private set; }
        public rootPage()
        {
            this.InitializeComponent();

            FrameTreePanel.Content = new TreePanelPage();

            FrameViewDataPanel.Content = new ViewPage();

            this.SizeChanged += RootPage_SizeChanged;
            this.PanelGrodSplitter.PointerReleased += PanelGrodSplitter_PointerReleased1;
            this.PanelGrodSplitter.PointerPressed += PanelGrodSplitter_PointerPressed;
            this.GridWorkAreaLeftToolBar.PointerReleased += GridWorkAreaLeftToolBar_PointerReleased;
            this.Loaded += RootPage_OnLoaded;
            this.Unloaded += RootPage_Unloaded;


            // Настройка таймера
            saveTimer.Interval = TimeSpan.FromSeconds(1); // Задержка в 1 секунду
            saveTimer.Tick += SaveTimer_Tick; // Обработчик события Tick
        }

        private void GridWorkAreaLeftToolBar_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isResizing) // Проверяем, было ли начато перемещение
            {
                Debug.WriteLine("Кнопка мыши отпущена на родительском элементе. Сохраняем данные.");
                saveTimer.Start(); // Запускаем таймер для сохранения данных
            }
        }

        private void PanelGrodSplitter_PointerReleased1(object sender, PointerRoutedEventArgs e)
        {
            isResizing = false;
            saveTimer.Start();
        }

        private void PanelGrodSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isResizing = true;
            saveTimer.Stop();
        }


        private void SaveTimer_Tick(object sender, object e)
        {
            saveTimer.Stop(); // Останавливаем таймер
            SaveSizes(); // Сохраняем данные
            SaveSettings(savedWidth, savedWidthViewDataPanel); // Сохраняем настройки
            Debug.WriteLine("Данные сохранены после завершения перемещения.");
        }

        private void RootPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"текущий размер = {savedWidth}");
            SaveSettings(savedWidth, savedWidthViewDataPanel);
        }


        public void SetParentTabView(TabView parentTabView)
        {
            ParentTabView = parentTabView;
        }

        public void AddTabToTabs(TabViewItem tab)
        {
            ParentTabView?.TabItems.Add(tab);
        }
        
        private void RootPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!isInitialized)
            {
                return;
            }

            // Игнорируем микроколебания размеров
            if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) < 2)
                return;

            double previousWidth = e.PreviousSize.Width;
            double newWidth = e.NewSize.Width;

            // Ширина элемента, который не изменяется
            double fixedWidth = 52;

            // Вычисляем рабочую ширину (общая ширина минус фиксированная часть)
            double previousWorkWidth = Math.Max(previousWidth - fixedWidth, 0);
            double newWorkWidth = Math.Max(newWidth - fixedWidth, 0);

            // Вычисляем абсолютную разницу
            double widthDiff = Math.Abs(newWorkWidth - previousWorkWidth);

            // Если изменение слишком маленькое - игнорируем
            if (widthDiff < 1) return;

            // Пропорции для разделения доступного пространства
            double treePanelProportion = 0.2;

            if (newWorkWidth > previousWorkWidth)
            {
                // Увеличилось
                double addTreePanelWidth = widthDiff * treePanelProportion;
                double newTreePanelWidth = Math.Min(ColumnTreeView.ActualWidth + addTreePanelWidth, ColumnTreeView.MaxWidth);

                // Округляем до целого числа для избежания ошибок сериализации
                newTreePanelWidth = Math.Round(newTreePanelWidth);

                ColumnTreeView.Width = new GridLength(newTreePanelWidth, GridUnitType.Pixel);
                Debug.WriteLine($"Ширина увеличилась: +{addTreePanelWidth:F1} -> {newTreePanelWidth}");
            }
            else if (newWorkWidth < previousWorkWidth)
            {
                // Уменьшилось
                double subtractTreePanelWidth = widthDiff * treePanelProportion;
                double newTreePanelWidth = Math.Max(ColumnTreeView.ActualWidth - subtractTreePanelWidth, ColumnTreeView.MinWidth);

                // Округляем до целого числа для избежания ошибок сериализации
                newTreePanelWidth = Math.Round(newTreePanelWidth);

                ColumnTreeView.Width = new GridLength(newTreePanelWidth, GridUnitType.Pixel);
                Debug.WriteLine($"Ширина уменьшилась: -{subtractTreePanelWidth:F1} -> {newTreePanelWidth}");
            }

            // Сохраняем только целочисленные значения
            SaveSizes();
            SaveSettings(Math.Round(ColumnTreeView.ActualWidth), Math.Round(FrameViewDataPanel.ActualWidth));
        }
        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            // Переключение ширины столбца и обнуление MinWidth перед скрытием панели
            if (ColumnTreeView.Width.IsStar || !isCollapsedrootPageTreePanelandViewDataPanel)
            {
                SaveSizes();
                // Меняем ширину на Auto и обнуляем MinWidth и MaxWidth
                ColumnTreeView.Width = new GridLength(0, GridUnitType.Auto);
                ColumnTreeView.ClearValue(ColumnDefinition.MinWidthProperty);
                ColumnTreeView.ClearValue(ColumnDefinition.MaxWidthProperty);

                FrameTreePanel.Visibility = Visibility.Collapsed;
                PanelGrodSplitter.Visibility = Visibility.Collapsed;
                isCollapsedrootPageTreePanelandViewDataPanel = true;
            }
            else
            {
                RestoreSizes();
                // Меняем ширину на 3* и восстанавливаем MinWidth и MaxWidth
            }
        }
        private void SaveSettings(double sw, double sw2)
        {
            try
            {
                if (sw <= 0 || sw2 <= 0 || savedMinWidth <= 0 || savedMaxWidth <= 0)
                {
                    Debug.WriteLine("Invalid values detected. Aborting save.");
                    return;
                }

                App.SettingsManager.SaveSetting("savedWidth", sw);
                App.SettingsManager.SaveSetting("savedWidthViewDataPanel", sw2);
                App.SettingsManager.SaveSetting("savedMinWidth", savedMinWidth);
                App.SettingsManager.SaveSetting("savedMaxWidth", savedMaxWidth);
                App.SettingsManager.SaveSetting("savedMinWidthViewDataPanel", savedMinWidthViewDataPanel);
                App.SettingsManager.SaveSetting("savedMaxWidthViewDataPanel", savedMaxWidthViewDataPanel);
                App.SettingsManager.SaveSetting("isCollapsedrootPageTreePanelandViewDataPanel", isCollapsedrootPageTreePanelandViewDataPanel);
                App.SettingsManager.SaveSetting("FrameTreePanel.Visibility", (Visibility)FrameTreePanel.Visibility);
                App.SettingsManager.SaveSetting("PanelGrodSplitter.Visibility", (Visibility)PanelGrodSplitter.Visibility);

                Debug.WriteLine("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        private void LoadSettings()
        {
            // Дефолтные значения
            const double defaultWidth = 350;
            const double defaultMinWidth = 100;
            const double defaultMaxWidth = 500;
            const double defaultWidthViewDataPanel = 400;
            const double defaultMinWidthViewDataPanel = 400;
            const double defaultMaxWidthViewDataPanel = 7680;
            
            const bool defaultCollapsedState = false;
            const Visibility defaultVisibility = Visibility.Visible;

            try
            {
                if (ColumnTreeView == null || FrameTreePanel == null || PanelGrodSplitter == null || ColumViewDataPanel == null)
                {
                    Debug.WriteLine("Элементы интерфейса не инициализированы.");
                    return;
                }

                // Загрузка с проверкой на 0 или отрицательные значения
                savedWidth = App.SettingsManager.GetSetting<double>("savedWidth");
                if (savedWidth <= 0)
                {
                    savedWidth = defaultWidth;
                    Debug.WriteLine("savedWidth <= 0, using default: " + defaultWidth);
                }

                savedMinWidth = App.SettingsManager.GetSetting<double>("savedMinWidth");
                if (savedMinWidth <= 0)
                {
                    savedMinWidth = defaultMinWidth;
                    Debug.WriteLine("savedMinWidth <= 0, using default: " + defaultMinWidth);
                }

                savedMaxWidth = App.SettingsManager.GetSetting<double>("savedMaxWidth");
                if (savedMaxWidth <= 0)
                {
                    savedMaxWidth = defaultMaxWidth;
                    Debug.WriteLine("savedMaxWidth <= 0, using default: " + defaultMaxWidth);
                }

                savedWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedWidthViewDataPanel");
                if (savedWidthViewDataPanel <= 0)
                {
                    savedWidthViewDataPanel = defaultWidthViewDataPanel;
                    Debug.WriteLine("savedWidth <= 0, using default: " + defaultWidthViewDataPanel);
                }

                savedMinWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedMinWidthViewDataPanel");
                if (savedMinWidthViewDataPanel <= 0)
                {
                    savedMinWidthViewDataPanel = defaultMinWidthViewDataPanel;
                    Debug.WriteLine("savedMinWidth <= 0, using default: " + defaultMinWidthViewDataPanel);
                }

                savedMaxWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedMaxWidthViewDataPanel");
                if (savedMaxWidthViewDataPanel <= 0)
                {
                    savedMaxWidthViewDataPanel = defaultMaxWidthViewDataPanel;
                    Debug.WriteLine("savedMaxWidth <= 0, using default: " + defaultMaxWidthViewDataPanel);
                }
                
                // Загрузка и проверка для bool
                isCollapsedrootPageTreePanelandViewDataPanel =
                    App.SettingsManager.GetSetting<bool>("isCollapsedrootPageTreePanelandViewDataPanel", defaultCollapsedState);

                // Загрузка и проверка для Visibility
                FrameTreePanel.Visibility = App.SettingsManager.GetSetting<Visibility>("FrameTreePanel.Visibility", defaultVisibility);
                PanelGrodSplitter.Visibility =
                    App.SettingsManager.GetSetting<Visibility>("PanelGrodSplitter.Visibility", defaultVisibility);

                // Дополнительная проверка для Visibility
                if (FrameTreePanel.Visibility != Visibility.Visible && FrameTreePanel.Visibility != Visibility.Collapsed)
                {
                    FrameTreePanel.Visibility = defaultVisibility;
                    Debug.WriteLine("Invalid FrameTreePanel.Visibility, using default: Visible");
                }

                if (PanelGrodSplitter.Visibility != Visibility.Visible && PanelGrodSplitter.Visibility != Visibility.Collapsed)
                {
                    PanelGrodSplitter.Visibility = defaultVisibility;
                    Debug.WriteLine("Invalid PanelGrodSplitter.Visibility, using default: Visible");
                }

                Debug.WriteLine("Settings loaded and applied successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading settings: {ex.Message}");

                // Установка дефолтных значений при ошибке
                savedWidth = defaultWidth;
                savedMinWidth = defaultMinWidth;
                savedMaxWidth = defaultMaxWidth;

                savedWidthViewDataPanel = defaultWidthViewDataPanel;
                savedMinWidthViewDataPanel = defaultMinWidthViewDataPanel;
                savedMaxWidthViewDataPanel = defaultMaxWidthViewDataPanel;

                isCollapsedrootPageTreePanelandViewDataPanel = defaultCollapsedState;
                FrameTreePanel.Visibility = defaultVisibility;
                PanelGrodSplitter.Visibility = defaultVisibility;

                Debug.WriteLine("Loaded default settings due to error");
            }
        }
        // Метод для сохранения размеров
        private void SaveSizes()
        {
            savedWidth = FrameTreePanel.ActualWidth;
            savedMinWidth = ColumnTreeView.MinWidth;
            savedMaxWidth = ColumnTreeView.MaxWidth;
            savedWidthViewDataPanel = FrameViewDataPanel.ActualWidth;
            savedMinWidthViewDataPanel = ColumViewDataPanel.MinWidth;
            savedMaxWidthViewDataPanel = ColumViewDataPanel.MaxWidth;
        }

        // Метод для восстановления размеров
        private void RestoreSizes()
        {
            if (savedWidth != -1)
            {
                ColumnTreeView.Width = new GridLength(savedWidth, GridUnitType.Pixel);
            }

            if (savedMinWidth != -1)
            {
                ColumnTreeView.MinWidth = savedMinWidth;
            }

            if (savedMaxWidth != -1)
            {
                ColumnTreeView.MaxWidth = savedMaxWidth;
            }

            if (savedWidthViewDataPanel != -1)
            {
                ColumViewDataPanel.Width = new GridLength(savedWidthViewDataPanel, GridUnitType.Star);
            }

            if (savedMinWidthViewDataPanel != -1)
            {
                ColumViewDataPanel.MinWidth = savedMinWidthViewDataPanel;
            }

            if (savedMaxWidthViewDataPanel != -1)
            {
                ColumViewDataPanel.MaxWidth = savedMaxWidthViewDataPanel;
            }
            FrameTreePanel.Visibility = Visibility.Visible;
            PanelGrodSplitter.Visibility = Visibility.Visible;
            isCollapsedrootPageTreePanelandViewDataPanel = false;
            Debug.WriteLine($"Restored Width: {ColumnTreeView.Width.Value}, MinWidth: {ColumnTreeView.MinWidth}, MaxWidth: {ColumnTreeView.MaxWidth}");
        }
        private void ApplyLoadedSettings()
        {
            // Применение загруженных размеров
            if (savedWidth != -1)
            {
                ColumnTreeView.Width = new GridLength(savedWidth, GridUnitType.Pixel);
            }

            if (savedMinWidth != -1)
            {
                ColumnTreeView.MinWidth = savedMinWidth;
            }

            if (savedMaxWidth != -1)
            {
                ColumnTreeView.MaxWidth = savedMaxWidth;
            }
            if (savedWidthViewDataPanel != -1)
            {
                ColumViewDataPanel.Width = new GridLength(savedWidthViewDataPanel, GridUnitType.Star);
            }

            if (savedMinWidthViewDataPanel != -1)
            {
                ColumViewDataPanel.MinWidth = savedMinWidthViewDataPanel;
            }

            if (savedMaxWidthViewDataPanel != -1)
            {
                ColumViewDataPanel.MaxWidth = savedMaxWidthViewDataPanel;
            }
            // Применение состояния видимости
            FrameTreePanel.Visibility = isCollapsedrootPageTreePanelandViewDataPanel ? Visibility.Collapsed : Visibility.Visible;
            PanelGrodSplitter.Visibility = isCollapsedrootPageTreePanelandViewDataPanel ? Visibility.Collapsed : Visibility.Visible;

            Debug.WriteLine($"Applied settings: Width={savedWidth}, MinWidth={savedMinWidth}, MaxWidth={savedMaxWidth}, IsCollapsed={isCollapsedrootPageTreePanelandViewDataPanel}");
        }

        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            AnimatedIcon.SetState(this.SearchAnimatedIcon, "PointerOver");
        }

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            AnimatedIcon.SetState(this.SearchAnimatedIcon, "Normal");
        }

        private void RootPage_OnLoaded(object sender, RoutedEventArgs e)
        {

            // Установка начальной ширины для ResizableColumn
            //ResizableColumn.Width = new GridLength(350, GridUnitType.Pixel);
            LoadSettings();
            ApplyLoadedSettings();
            isInitialized = true;

        }
    }
}

