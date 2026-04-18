using System;
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

            saveTimer.Interval = TimeSpan.FromSeconds(1);
            saveTimer.Tick += SaveTimer_Tick;
        }

        private void GridWorkAreaLeftToolBar_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isResizing)
            {
                saveTimer.Start();
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
            saveTimer.Stop();
            SaveSizes();
            SaveSettings(savedWidth, savedWidthViewDataPanel);
        }

        private void RootPage_Unloaded(object sender, RoutedEventArgs e)
        {
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

            if (Math.Abs(e.NewSize.Width - e.PreviousSize.Width) < 2)
                return;

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

                newTreePanelWidth = Math.Round(newTreePanelWidth);

                ColumnTreeView.Width = new GridLength(newTreePanelWidth, GridUnitType.Pixel);
            }
            else if (newWorkWidth < previousWorkWidth)
            {
                double subtractTreePanelWidth = widthDiff * treePanelProportion;
                double newTreePanelWidth = Math.Max(ColumnTreeView.ActualWidth - subtractTreePanelWidth, ColumnTreeView.MinWidth);

                newTreePanelWidth = Math.Round(newTreePanelWidth);

                ColumnTreeView.Width = new GridLength(newTreePanelWidth, GridUnitType.Pixel);
            }

            SaveSizes();
            SaveSettings(Math.Round(ColumnTreeView.ActualWidth), Math.Round(FrameViewDataPanel.ActualWidth));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
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
        }

        private void SaveSettings(double sw, double sw2)
        {
            try
            {
                if (sw <= 0 || sw2 <= 0 || savedMinWidth <= 0 || savedMaxWidth <= 0)
                {
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
            }
            catch
            {
            }
        }

        private void LoadSettings()
        {
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
                    return;
                }

                savedWidth = App.SettingsManager.GetSetting<double>("savedWidth");
                if (savedWidth <= 0)
                {
                    savedWidth = defaultWidth;
                }

                savedMinWidth = App.SettingsManager.GetSetting<double>("savedMinWidth");
                if (savedMinWidth <= 0)
                {
                    savedMinWidth = defaultMinWidth;
                }

                savedMaxWidth = App.SettingsManager.GetSetting<double>("savedMaxWidth");
                if (savedMaxWidth <= 0)
                {
                    savedMaxWidth = defaultMaxWidth;
                }

                savedWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedWidthViewDataPanel");
                if (savedWidthViewDataPanel <= 0)
                {
                    savedWidthViewDataPanel = defaultWidthViewDataPanel;
                }

                savedMinWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedMinWidthViewDataPanel");
                if (savedMinWidthViewDataPanel <= 0)
                {
                    savedMinWidthViewDataPanel = defaultMinWidthViewDataPanel;
                }

                savedMaxWidthViewDataPanel = App.SettingsManager.GetSetting<double>("savedMaxWidthViewDataPanel");
                if (savedMaxWidthViewDataPanel <= 0)
                {
                    savedMaxWidthViewDataPanel = defaultMaxWidthViewDataPanel;
                }

                isCollapsedrootPageTreePanelandViewDataPanel =
                    App.SettingsManager.GetSetting<bool>("isCollapsedrootPageTreePanelandViewDataPanel", defaultCollapsedState);

                FrameTreePanel.Visibility = App.SettingsManager.GetSetting<Visibility>("FrameTreePanel.Visibility", defaultVisibility);
                PanelGrodSplitter.Visibility =
                    App.SettingsManager.GetSetting<Visibility>("PanelGrodSplitter.Visibility", defaultVisibility);

                if (FrameTreePanel.Visibility != Visibility.Visible && FrameTreePanel.Visibility != Visibility.Collapsed)
                {
                    FrameTreePanel.Visibility = defaultVisibility;
                }

                if (PanelGrodSplitter.Visibility != Visibility.Visible && PanelGrodSplitter.Visibility != Visibility.Collapsed)
                {
                    PanelGrodSplitter.Visibility = defaultVisibility;
                }
            }
            catch
            {
                savedWidth = defaultWidth;
                savedMinWidth = defaultMinWidth;
                savedMaxWidth = defaultMaxWidth;

                savedWidthViewDataPanel = defaultWidthViewDataPanel;
                savedMinWidthViewDataPanel = defaultMinWidthViewDataPanel;
                savedMaxWidthViewDataPanel = defaultMaxWidthViewDataPanel;

                isCollapsedrootPageTreePanelandViewDataPanel = defaultCollapsedState;
                FrameTreePanel.Visibility = defaultVisibility;
                PanelGrodSplitter.Visibility = defaultVisibility;
            }
        }

        private void SaveSizes()
        {
            savedWidth = FrameTreePanel.ActualWidth;
            savedMinWidth = ColumnTreeView.MinWidth;
            savedMaxWidth = ColumnTreeView.MaxWidth;
            savedWidthViewDataPanel = FrameViewDataPanel.ActualWidth;
            savedMinWidthViewDataPanel = ColumViewDataPanel.MinWidth;
            savedMaxWidthViewDataPanel = ColumViewDataPanel.MaxWidth;
        }

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
        }

        private void ApplyLoadedSettings()
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

            FrameTreePanel.Visibility = isCollapsedrootPageTreePanelandViewDataPanel ? Visibility.Collapsed : Visibility.Visible;
            PanelGrodSplitter.Visibility = isCollapsedrootPageTreePanelandViewDataPanel ? Visibility.Collapsed : Visibility.Visible;
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
            LoadSettings();
            ApplyLoadedSettings();
            isInitialized = true;
        }
    }
}