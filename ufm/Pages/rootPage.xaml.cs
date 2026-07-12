//using System;
//using Windows.Storage;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using ufm.Pages;

//namespace ufm
//{
//    public sealed partial class rootPage : Page
//    {
//        private double savedWidth = -1;
//        private double savedWidthViewDataPanel = -1;
//        private double savedMinWidth = -1;
//        private double savedMaxWidth = -1;                // больше не используется для восстановления
//        private double savedMinWidthViewDataPanel = -1;
//        private double savedMaxWidthViewDataPanel = -1;   // больше не используется для восстановления
//        private bool isCollapsedrootPageTreePanelandViewDataPanel = false;
//        private bool isInitialized = false;
//        private bool isResizing = false;

//        private DispatcherTimer saveTimer = new DispatcherTimer();
//        public TabView ParentTabView { get; private set; }

//        public rootPage()
//        {
//            this.InitializeComponent();

//            FrameTreePanel.Content = new TreePanelPage();
//            FrameViewDataPanel.Content = new ViewPage();

//            this.SizeChanged += RootPage_SizeChanged;
//            this.PanelGrodSplitter.PointerReleased += PanelGrodSplitter_PointerReleased1;
//            this.PanelGrodSplitter.PointerPressed += PanelGrodSplitter_PointerPressed;
//            this.GridWorkAreaLeftToolBar.PointerReleased += GridWorkAreaLeftToolBar_PointerReleased;
//            this.Loaded += RootPage_OnLoaded;
//            this.Unloaded += RootPage_Unloaded;

//            saveTimer.Interval = TimeSpan.FromSeconds(1);
//            saveTimer.Tick += SaveTimer_Tick;
//        }

//        private void GridWorkAreaLeftToolBar_PointerReleased(object sender, PointerRoutedEventArgs e)
//        {
//            if (isResizing)
//            {
//                saveTimer.Start();
//            }
//        }

//        private void PanelGrodSplitter_PointerReleased1(object sender, PointerRoutedEventArgs e)
//        {
//            isResizing = false;
//            saveTimer.Start();
//        }

//        private void PanelGrodSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            isResizing = true;
//            saveTimer.Stop();
//        }

//        private void SaveTimer_Tick(object sender, object e)
//        {
//            saveTimer.Stop();
//            SaveSizes();
//            SaveSettings(savedWidth, savedWidthViewDataPanel);
//        }

//        private void RootPage_Unloaded(object sender, RoutedEventArgs e)
//        {
//            SaveSettings(savedWidth, savedWidthViewDataPanel);
//        }

//        public void SetParentTabView(TabView parentTabView)
//        {
//            ParentTabView = parentTabView;
//        }

//        public void AddTabToTabs(TabViewItem tab)
//        {
//            ParentTabView?.TabItems.Add(tab);
//        }

//        private void RootPage_SizeChanged(object sender, SizeChangedEventArgs e)
//        {
//            if (!isInitialized)
//                return;

//            if (isCollapsedrootPageTreePanelandViewDataPanel)
//                return;

//            // Обновляем динамический предел MaxWidth = 50% ширины окна
//            UpdateTreeMaxWidth();

//            if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) < 2)
//                return;

//            double previousWidth = e.PreviousSize.Width;
//            double newWidth = e.NewSize.Width;

//            double fixedWidth = 52;

//            double previousWorkWidth = Math.Max(previousWidth - fixedWidth, 0);
//            double newWorkWidth = Math.Max(newWidth - fixedWidth, 0);

//            double widthDiff = Math.Abs(newWorkWidth - previousWorkWidth);

//            if (widthDiff < 1) return;

//            double treePanelProportion = 0.2;

//            if (newWorkWidth > previousWorkWidth)
//            {
//                double addTreePanelWidth = widthDiff * treePanelProportion;
//                double newTreePanelWidth = Math.Min(ColumnTreeView.ActualWidth + addTreePanelWidth, ColumnTreeView.MaxWidth);

//                newTreePanelWidth = Math.Round(newTreePanelWidth);

//                ColumnTreeView.Width = new GridLength(newTreePanelWidth, GridUnitType.Pixel);
//            }
//            else if (newWorkWidth < previousWorkWidth)
//            {
//                double subtractTreePanelWidth = widthDiff * treePanelProportion;
//                double newTreePanelWidth = Math.Max(ColumnTreeView.ActualWidth - subtractTreePanelWidth, ColumnTreeView.MinWidth);

//                newTreePanelWidth = Math.Round(newTreePanelWidth);

//                ColumnTreeView.Width = new GridLength(newTreePanelWidth, GridUnitType.Pixel);
//            }

//            SaveSizes();
//            SaveSettings(Math.Round(ColumnTreeView.ActualWidth), Math.Round(FrameViewDataPanel.ActualWidth));
//        }

//        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
//        {
//            if (ColumnTreeView.Width.IsStar || !isCollapsedrootPageTreePanelandViewDataPanel)
//            {
//                SaveSizes();
//                ColumnTreeView.Width = new GridLength(0, GridUnitType.Auto);
//                ColumnTreeView.ClearValue(ColumnDefinition.MinWidthProperty);
//                ColumnTreeView.ClearValue(ColumnDefinition.MaxWidthProperty);

//                FrameTreePanel.Visibility = Visibility.Collapsed;
//                PanelGrodSplitter.Visibility = Visibility.Collapsed;
//                isCollapsedrootPageTreePanelandViewDataPanel = true;
//            }
//            else
//            {
//                RestoreSizes();
//            }

//            // Принудительный пересчёт макета и сброс кэша сплиттеров во ViewPage
//            FrameViewDataPanel.UpdateLayout();
//            if (FrameViewDataPanel.Content is ViewPage viewPage)
//            {
//                viewPage.ResetPanelLayout();
//            }
//        }

//        private void SaveSettings(double sw, double sw2)
//        {
//            try
//            {
//                // Убраны проверки на savedMaxWidth и savedMaxWidthViewDataPanel
//                if (sw <= 0 || sw2 <= 0 || savedMinWidth <= 0)
//                    return;

//                App.SettingsManager.SaveSetting("savedWidth", sw);
//                App.SettingsManager.SaveSetting("savedWidthViewDataPanel", sw2);
//                App.SettingsManager.SaveSetting("savedMinWidth", savedMinWidth);
//                // Сохранение savedMaxWidth и savedMaxWidthViewDataPanel исключено
//                App.SettingsManager.SaveSetting("savedMinWidthViewDataPanel", savedMinWidthViewDataPanel);
//                App.SettingsManager.SaveSetting("isCollapsedrootPageTreePanelandViewDataPanel", isCollapsedrootPageTreePanelandViewDataPanel);
//                App.SettingsManager.SaveSetting("FrameTreePanel.Visibility", (Visibility)FrameTreePanel.Visibility);
//                App.SettingsManager.SaveSetting("PanelGrodSplitter.Visibility", (Visibility)PanelGrodSplitter.Visibility);
//            }
//            catch
//            {
//            }
//        }

//        private void LoadSettings()
//        {
//            const double defaultWidth = 350;
//            const double defaultMinWidth = 100;
//            const double defaultMaxWidth = 500;          // больше не используется
//            const double defaultWidthViewDataPanel = 400;
//            const double defaultMinWidthViewDataPanel = 400;
//            const double defaultMaxWidthViewDataPanel = 7680;

//            const bool defaultCollapsedState = false;
//            const Visibility defaultVisibility = Visibility.Visible;

//            try
//            {
//                if (ColumnTreeView == null || FrameTreePanel == null || PanelGrodSplitter == null || ColumViewDataPanel == null)
//                    return;

//                savedWidth = App.SettingsManager.GetSetting<double>("savedWidth");
//                if (savedWidth <= 0)
//                    savedWidth = defaultWidth;

//                savedMinWidth = App.SettingsManager.GetSetting<double>("savedMinWidth");
//                if (savedMinWidth <= 0)
//                    savedMinWidth = defaultMinWidth;

//                // Больше не загружаем savedMaxWidth и savedMaxWidthViewDataPanel
//                savedWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedWidthViewDataPanel");
//                if (savedWidthViewDataPanel <= 0)
//                    savedWidthViewDataPanel = defaultWidthViewDataPanel;

//                savedMinWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedMinWidthViewDataPanel");
//                if (savedMinWidthViewDataPanel <= 0)
//                    savedMinWidthViewDataPanel = defaultMinWidthViewDataPanel;

//                isCollapsedrootPageTreePanelandViewDataPanel =
//                    App.SettingsManager.GetSetting<bool>("isCollapsedrootPageTreePanelandViewDataPanel", defaultCollapsedState);

//                FrameTreePanel.Visibility = App.SettingsManager.GetSetting<Visibility>("FrameTreePanel.Visibility", defaultVisibility);
//                PanelGrodSplitter.Visibility =
//                    App.SettingsManager.GetSetting<Visibility>("PanelGrodSplitter.Visibility", defaultVisibility);

//                if (FrameTreePanel.Visibility != Visibility.Visible && FrameTreePanel.Visibility != Visibility.Collapsed)
//                    FrameTreePanel.Visibility = defaultVisibility;

//                if (PanelGrodSplitter.Visibility != Visibility.Visible && PanelGrodSplitter.Visibility != Visibility.Collapsed)
//                    PanelGrodSplitter.Visibility = defaultVisibility;
//            }
//            catch
//            {
//                savedWidth = defaultWidth;
//                savedMinWidth = defaultMinWidth;
//                // savedMaxWidth больше не используется

//                savedWidthViewDataPanel = defaultWidthViewDataPanel;
//                savedMinWidthViewDataPanel = defaultMinWidthViewDataPanel;

//                isCollapsedrootPageTreePanelandViewDataPanel = defaultCollapsedState;
//                FrameTreePanel.Visibility = defaultVisibility;
//                PanelGrodSplitter.Visibility = defaultVisibility;
//            }
//        }

//        private void SaveSizes()
//        {
//            savedWidth = FrameTreePanel.ActualWidth;
//            savedMinWidth = ColumnTreeView.MinWidth;
//            savedMaxWidth = ColumnTreeView.MaxWidth;               // записываем, но не сохраняем и не восстанавливаем
//            savedWidthViewDataPanel = FrameViewDataPanel.ActualWidth;
//            savedMinWidthViewDataPanel = ColumViewDataPanel.MinWidth;
//            savedMaxWidthViewDataPanel = ColumViewDataPanel.MaxWidth; // аналогично
//        }

//        private void RestoreSizes()
//        {
//            if (savedWidth != -1)
//                ColumnTreeView.Width = new GridLength(savedWidth, GridUnitType.Pixel);

//            if (savedMinWidth != -1)
//                ColumnTreeView.MinWidth = savedMinWidth;

//            // MaxWidth не восстанавливаем из сохранённого значения

//            if (savedWidthViewDataPanel != -1)
//                ColumViewDataPanel.Width = new GridLength(savedWidthViewDataPanel, GridUnitType.Star);

//            if (savedMinWidthViewDataPanel != -1)
//                ColumViewDataPanel.MinWidth = savedMinWidthViewDataPanel;

//            // MaxWidthViewDataPanel тоже не восстанавливаем

//            FrameTreePanel.Visibility = Visibility.Visible;
//            PanelGrodSplitter.Visibility = Visibility.Visible;
//            isCollapsedrootPageTreePanelandViewDataPanel = false;

//            // Сразу задаём актуальный лимит после разворачивания
//            UpdateTreeMaxWidth();
//        }

//        private void ApplyLoadedSettings()
//        {
//            if (savedWidth != -1)
//                ColumnTreeView.Width = new GridLength(savedWidth, GridUnitType.Pixel);

//            if (savedMinWidth != -1)
//                ColumnTreeView.MinWidth = savedMinWidth;

//            // MaxWidth не применяется из сохранённых данных

//            if (savedWidthViewDataPanel != -1)
//                ColumViewDataPanel.Width = new GridLength(savedWidthViewDataPanel, GridUnitType.Star);

//            if (savedMinWidthViewDataPanel != -1)
//                ColumViewDataPanel.MinWidth = savedMinWidthViewDataPanel;

//            FrameTreePanel.Visibility = isCollapsedrootPageTreePanelandViewDataPanel ? Visibility.Collapsed : Visibility.Visible;
//            PanelGrodSplitter.Visibility = isCollapsedrootPageTreePanelandViewDataPanel ? Visibility.Collapsed : Visibility.Visible;

//            // Устанавливаем динамический лимит после применения всех настроек
//            UpdateTreeMaxWidth();
//        }

//        /// <summary>
//        /// Обновляет MaxWidth дерева: ровно половина текущей ширины страницы, но не меньше MinWidth.
//        /// </summary>
//        private void UpdateTreeMaxWidth()
//        {
//            double pageWidth = this.ActualWidth;
//            if (pageWidth <= 0)
//                return;

//            double max = pageWidth / 2.0;
//            if (max < ColumnTreeView.MinWidth)
//                max = ColumnTreeView.MinWidth;

//            ColumnTreeView.MaxWidth = max;
//        }

//        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
//        {
//            AnimatedIcon.SetState(this.SearchAnimatedIcon, "PointerOver");
//        }

//        private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
//        {
//            AnimatedIcon.SetState(this.SearchAnimatedIcon, "Normal");
//        }

//        private void RootPage_OnLoaded(object sender, RoutedEventArgs e)
//        {
//            LoadSettings();
//            ApplyLoadedSettings();
//            isInitialized = true;
//        }
//    }
//}


using System;
using System.Diagnostics;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ufm.Pages;

namespace ufm
{
    public sealed partial class rootPage : Page
    {
        private double savedWidth = -1;
        private double savedWidthViewDataPanel = -1;
        private double savedMinWidth = -1;
        private double savedMinWidthViewDataPanel = -1;
        private bool isCollapsedrootPageTreePanelandViewDataPanel = false;
        private bool isInitialized = false;
        private bool isResizing = false;

        private DispatcherTimer saveTimer = new DispatcherTimer();
        public TabView ParentTabView { get; private set; }

        // Защита от повторного создания страниц
        private bool _pagesCreated = false;

        public rootPage()
        {
            var sw = Stopwatch.StartNew();
            this.InitializeComponent();

            // Тяжёлые страницы НЕ создаём здесь – они будут созданы отложенно в Loaded

            this.SizeChanged += RootPage_SizeChanged;
            this.PanelGrodSplitter.PointerReleased += PanelGrodSplitter_PointerReleased1;
            this.PanelGrodSplitter.PointerPressed += PanelGrodSplitter_PointerPressed;
            this.GridWorkAreaLeftToolBar.PointerReleased += GridWorkAreaLeftToolBar_PointerReleased;
            this.Loaded += RootPage_OnLoaded;
            this.Unloaded += RootPage_Unloaded;

            saveTimer.Interval = TimeSpan.FromSeconds(1);
            saveTimer.Tick += SaveTimer_Tick;
            sw.Stop();
            Debug.WriteLine($"[rootPage] Constructor: {sw.ElapsedMilliseconds} ms");
        }

        private void GridWorkAreaLeftToolBar_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isResizing) saveTimer.Start();
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
            var sw = Stopwatch.StartNew();
            saveTimer.Stop();
            SaveSizes();
            SaveSettings(savedWidth, savedWidthViewDataPanel);
            sw.Stop();
            Debug.WriteLine($"[rootPage] SaveTimer_Tick: {sw.ElapsedMilliseconds} ms");
        }

        private void RootPage_Unloaded(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            SaveSettings(savedWidth, savedWidthViewDataPanel);
            sw.Stop();
            Debug.WriteLine($"[rootPage] RootPage_Unloaded: {sw.ElapsedMilliseconds} ms");
        }

        public void SetParentTabView(TabView parentTabView) => ParentTabView = parentTabView;

        public void AddTabToTabs(TabViewItem tab) => ParentTabView?.TabItems.Add(tab);

        private void RootPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!isInitialized || isCollapsedrootPageTreePanelandViewDataPanel) return;

            UpdateTreeMaxWidth();

            if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) < 2) return;

            double previousWidth = e.PreviousSize.Width;
            double newWidth = e.NewSize.Width;
            double fixedWidth = 52;
            double previousWorkWidth = Math.Max(previousWidth - fixedWidth, 0);
            double newWorkWidth = Math.Max(newWidth - fixedWidth, 0);
            double widthDiff = Math.Abs(newWorkWidth - previousWorkWidth);
            if (widthDiff < 1) return;

            double treePanelProportion = 0.2;
            if (newWorkWidth > previousWorkWidth)
            {
                double addTreePanelWidth = widthDiff * treePanelProportion;
                double newTreePanelWidth = Math.Min(ColumnTreeView.ActualWidth + addTreePanelWidth, ColumnTreeView.MaxWidth);
                ColumnTreeView.Width = new GridLength(Math.Round(newTreePanelWidth), GridUnitType.Pixel);
            }
            else
            {
                double subtractTreePanelWidth = widthDiff * treePanelProportion;
                double newTreePanelWidth = Math.Max(ColumnTreeView.ActualWidth - subtractTreePanelWidth, ColumnTreeView.MinWidth);
                ColumnTreeView.Width = new GridLength(Math.Round(newTreePanelWidth), GridUnitType.Pixel);
            }

            SaveSizes();
            SaveSettings(Math.Round(ColumnTreeView.ActualWidth), Math.Round(FrameViewDataPanel.ActualWidth));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            if (ColumnTreeView.Width.IsStar || !isCollapsedrootPageTreePanelandViewDataPanel)
            {
                SaveSizes();
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
            }

            FrameViewDataPanel.UpdateLayout();
            if (FrameViewDataPanel.Content is ViewPage viewPage)
                viewPage.ResetPanelLayout();
            sw.Stop();
            Debug.WriteLine($"[rootPage] ButtonBase_OnClick: {sw.ElapsedMilliseconds} ms");
        }

        private void SaveSettings(double sw, double sw2)
        {
            try
            {
                if (sw <= 0 || sw2 <= 0 || savedMinWidth <= 0) return;
                App.SettingsManager.SaveSetting("savedWidth", sw);
                App.SettingsManager.SaveSetting("savedWidthViewDataPanel", sw2);
                App.SettingsManager.SaveSetting("savedMinWidth", savedMinWidth);
                App.SettingsManager.SaveSetting("savedMinWidthViewDataPanel", savedMinWidthViewDataPanel);
                App.SettingsManager.SaveSetting("isCollapsedrootPageTreePanelandViewDataPanel", isCollapsedrootPageTreePanelandViewDataPanel);
                App.SettingsManager.SaveSetting("FrameTreePanel.Visibility", (Visibility)FrameTreePanel.Visibility);
                App.SettingsManager.SaveSetting("PanelGrodSplitter.Visibility", (Visibility)PanelGrodSplitter.Visibility);
            }
            catch { }
        }

        private void LoadSettings()
        {
            var sw = Stopwatch.StartNew();
            const double defaultWidth = 350;
            const double defaultMinWidth = 100;
            const double defaultWidthViewDataPanel = 400;
            const double defaultMinWidthViewDataPanel = 400;
            const bool defaultCollapsedState = false;
            const Visibility defaultVisibility = Visibility.Visible;

            try
            {
                if (ColumnTreeView == null || FrameTreePanel == null || PanelGrodSplitter == null || ColumViewDataPanel == null)
                    return;

                savedWidth = App.SettingsManager.GetSetting<double>("savedWidth");
                if (savedWidth <= 0) savedWidth = defaultWidth;

                savedMinWidth = App.SettingsManager.GetSetting<double>("savedMinWidth");
                if (savedMinWidth <= 0) savedMinWidth = defaultMinWidth;

                savedWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedWidthViewDataPanel");
                if (savedWidthViewDataPanel <= 0) savedWidthViewDataPanel = defaultWidthViewDataPanel;

                savedMinWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedMinWidthViewDataPanel");
                if (savedMinWidthViewDataPanel <= 0) savedMinWidthViewDataPanel = defaultMinWidthViewDataPanel;

                isCollapsedrootPageTreePanelandViewDataPanel =
                    App.SettingsManager.GetSetting<bool>("isCollapsedrootPageTreePanelandViewDataPanel", defaultCollapsedState);

                FrameTreePanel.Visibility = App.SettingsManager.GetSetting<Visibility>("FrameTreePanel.Visibility", defaultVisibility);
                PanelGrodSplitter.Visibility = App.SettingsManager.GetSetting<Visibility>("PanelGrodSplitter.Visibility", defaultVisibility);

                if (FrameTreePanel.Visibility != Visibility.Visible && FrameTreePanel.Visibility != Visibility.Collapsed)
                    FrameTreePanel.Visibility = defaultVisibility;
                if (PanelGrodSplitter.Visibility != Visibility.Visible && PanelGrodSplitter.Visibility != Visibility.Collapsed)
                    PanelGrodSplitter.Visibility = defaultVisibility;
            }
            catch
            {
                savedWidth = defaultWidth;
                savedMinWidth = defaultMinWidth;
                savedWidthViewDataPanel = defaultWidthViewDataPanel;
                savedMinWidthViewDataPanel = defaultMinWidthViewDataPanel;
                isCollapsedrootPageTreePanelandViewDataPanel = defaultCollapsedState;
                FrameTreePanel.Visibility = defaultVisibility;
                PanelGrodSplitter.Visibility = defaultVisibility;
            }
            sw.Stop();
            Debug.WriteLine($"[rootPage] LoadSettings: {sw.ElapsedMilliseconds} ms");
        }

        private void SaveSizes()
        {
            savedWidth = FrameTreePanel.ActualWidth;
            savedMinWidth = ColumnTreeView.MinWidth;
            savedWidthViewDataPanel = FrameViewDataPanel.ActualWidth;
            savedMinWidthViewDataPanel = ColumViewDataPanel.MinWidth;
        }

        private void RestoreSizes()
        {
            if (savedWidth != -1) ColumnTreeView.Width = new GridLength(savedWidth, GridUnitType.Pixel);
            if (savedMinWidth != -1) ColumnTreeView.MinWidth = savedMinWidth;
            if (savedWidthViewDataPanel != -1) ColumViewDataPanel.Width = new GridLength(savedWidthViewDataPanel, GridUnitType.Star);
            if (savedMinWidthViewDataPanel != -1) ColumViewDataPanel.MinWidth = savedMinWidthViewDataPanel;

            FrameTreePanel.Visibility = Visibility.Visible;
            PanelGrodSplitter.Visibility = Visibility.Visible;
            isCollapsedrootPageTreePanelandViewDataPanel = false;
            UpdateTreeMaxWidth();
        }

        private void ApplyLoadedSettings()
        {
            if (savedWidth != -1) ColumnTreeView.Width = new GridLength(savedWidth, GridUnitType.Pixel);
            if (savedMinWidth != -1) ColumnTreeView.MinWidth = savedMinWidth;
            if (savedWidthViewDataPanel != -1) ColumViewDataPanel.Width = new GridLength(savedWidthViewDataPanel, GridUnitType.Star);
            if (savedMinWidthViewDataPanel != -1) ColumViewDataPanel.MinWidth = savedMinWidthViewDataPanel;

            FrameTreePanel.Visibility = isCollapsedrootPageTreePanelandViewDataPanel ? Visibility.Collapsed : Visibility.Visible;
            PanelGrodSplitter.Visibility = isCollapsedrootPageTreePanelandViewDataPanel ? Visibility.Collapsed : Visibility.Visible;
            UpdateTreeMaxWidth();
        }

        private void UpdateTreeMaxWidth()
        {
            double pageWidth = this.ActualWidth;
            if (pageWidth <= 0) return;
            double max = pageWidth / 2.0;
            if (max < ColumnTreeView.MinWidth) max = ColumnTreeView.MinWidth;
            ColumnTreeView.MaxWidth = max;
        }

        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e) =>
            AnimatedIcon.SetState(this.SearchAnimatedIcon, "PointerOver");

        private void Button_PointerExited(object sender, PointerRoutedEventArgs e) =>
            AnimatedIcon.SetState(this.SearchAnimatedIcon, "Normal");

        private void RootPage_OnLoaded(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            LoadSettings();
            ApplyLoadedSettings();
            isInitialized = true;
            sw.Stop();
            Debug.WriteLine($"[rootPage] RootPage_OnLoaded (before late create): {sw.ElapsedMilliseconds} ms");

            if (!_pagesCreated)
            {
                _pagesCreated = true;
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
                {
                    var sw2 = Stopwatch.StartNew();
                    FrameTreePanel.Content = new TreePanelPage();
                    FrameViewDataPanel.Content = new ViewPage();
                    sw2.Stop();
                    Debug.WriteLine($"[rootPage] Late creation of TreePanelPage & ViewPage: {sw2.ElapsedMilliseconds} ms");
                });
            }
        }
    }
}