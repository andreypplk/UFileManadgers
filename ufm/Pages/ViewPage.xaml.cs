using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Storage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Windows.UI;
using Microsoft.UI.Xaml.Input;
using CommunityToolkit.WinUI.Controls;
using Core_FileManagement;
using System.Threading.Tasks;

namespace ufm
{
    public sealed partial class ViewPage : Page
    {
        #region Enums
        public enum PanelIndex { Panel0 = 0, Panel1 = 1, Panel2 = 2, Panel3 = 3 }

        public enum DisplayMode
        {
            Single,              // Только Panel0
            Vertical,            // Panel0 | Panel1  
            Horizontal,          // Panel0 | Panel1
            TripleVertical,      // Panel0 | Panel1 | Panel2 (вертикальное 3-х панельное)
            TripleHorizontal,    // Panel0 | Panel1 | Panel2 (горизонтальное 3-х панельное)
            TripleTopBottom,     // 2 сверху, 1 снизу: Panel0(сверху-слева) | Panel1(сверху-справа) | Panel2(снизу)
            TripleBottomTop,     // 1 сверху, 2 снизу: Panel0(сверху) | Panel1(снизу-слева) | Panel2(снизу-справа)
            TripleLeftRight,     // 1 слева, 2 справа: Panel0(слева) | Panel1(справа-сверху) | Panel2(справа-снизу)
            TripleRightLeft,     // 2 слева, 1 справа: Panel0(слева-сверху) | Panel1(слева-снизу) | Panel2(справа)
            Quad                 // Panel0 | Panel1 | Panel2 | Panel3 (2x2)
        }

        public enum PreviewPanelMode { None, Left, Right }

        public enum ViewMode
        {
            Icons,
            List,
            Table,
            Tiles,
            CompactList
        }

        public class BreadcrumbItem
        {
            public string Text { get; set; }
            public Symbol Icon { get; set; }
            public override string ToString() => Text;
        }
        #endregion

        #region Поля
        private double _leftPreviewWidth = 300;
        private double _rightPreviewWidth = 300;
        private DisplayMode _currentDisplayMode = DisplayMode.Single;
        private string _prefsize = "Icons";

        private SplitterManager _splitterManager;
        private readonly INavigationManager _navigationManager;
        private bool _isUpdatingViewMode = false;
        private PreviewPanelMode _previewPanelMode = PreviewPanelMode.None;

        private PanelManagerRegistry _panelRegistry;
        private PanelSynchronizationService _syncService;
        private PanelManager _activePanelManager;

        // Упрощенная система из 4 панелей
        private readonly PanelManager[] _panels = new PanelManager[4];
        private PanelIndex _activePanelIndex = PanelIndex.Panel0;

        // Сопоставление PanelIndex с PanelManager ID
        private readonly Dictionary<PanelIndex, string> _panelIdMap = new Dictionary<PanelIndex, string>
        {
            [PanelIndex.Panel0] = "SinglePanel",
            [PanelIndex.Panel1] = "RightPanel",
            [PanelIndex.Panel2] = "TripleVerticalCenterPanel",
            [PanelIndex.Panel3] = "QuadBottomRightPanel"
        };

        // Сопоставление PanelIndex с ContentControl для каждого режима отображения
        private readonly Dictionary<DisplayMode, Dictionary<PanelIndex, ContentControl>> _displayPanelControls = new Dictionary<DisplayMode, Dictionary<PanelIndex, ContentControl>>();
        #endregion

        #region Конструктор и инициализация
        public ViewPage()
        {
            this.InitializeComponent();

            _navigationManager = new NavigationManager();
            _panelRegistry = new PanelManagerRegistry(_navigationManager);
            _syncService = new PanelSynchronizationService(_panelRegistry);

            InitializePanelMappings();
            InitializePanels();
            InitializePanelEventHandlers();
            InitializeBreadcrumbBar();
            InitializeNavigationButtons();

            _splitterManager = new SplitterManager(App.SettingsManager);

            // Подписка на события сплиттеров
            VerticalSplitter.PointerReleased += Splitter_PointerReleased;
            HorizontalSplitter.PointerReleased += Splitter_PointerReleased;
            TripleVerticalSplitter1.PointerReleased += Splitter_PointerReleased;
            TripleVerticalSplitter2.PointerReleased += Splitter_PointerReleased;
            TripleHorizontalSplitter1.PointerReleased += Splitter_PointerReleased;
            TripleHorizontalSplitter2.PointerReleased += Splitter_PointerReleased;

            // Подписка на события предпросмотра
            PreviewLeftRadio.Click += PreviewLeft_Click;
            PreviewRightRadio.Click += PreviewRight_Click;
            PreviewNoneRadio.Click += PreviewNone_Click;

            this.Loaded += ViewPage_Loaded;

            // Подписка на события сплиттеров предпросмотра
            LeftPreviewSplitter.PointerReleased += PreviewSplitter_PointerReleased;
            LeftPreviewSplitter.PointerCaptureLost += PreviewSplitter_PointerCaptureLost;
            RightPreviewSplitter.PointerReleased += PreviewSplitter_PointerReleased;
            RightPreviewSplitter.PointerCaptureLost += PreviewSplitter_PointerCaptureLost;

            ColumPreviewLeft.MinWidth = 0;
            ColumPreviewRigth.MinWidth = 0;

            CleanInvalidSplitterSettings();
        }

        private void InitializePanelMappings()
        {
            // Инициализируем сопоставления для каждого режима отображения
            _displayPanelControls[DisplayMode.Single] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = ViewContainer
            };

            _displayPanelControls[DisplayMode.Vertical] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = VerticalLeftContent,
                [PanelIndex.Panel1] = VerticalRightContent
            };

            _displayPanelControls[DisplayMode.Horizontal] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = HorizontalTopContent,
                [PanelIndex.Panel1] = HorizontalBottomContent
            };

            _displayPanelControls[DisplayMode.TripleVertical] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = TripleVerticalLeftContent,
                [PanelIndex.Panel1] = TripleVerticalCenterContent,
                [PanelIndex.Panel2] = TripleVerticalRightContent
            };

            _displayPanelControls[DisplayMode.TripleHorizontal] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = TripleHorizontalTopContent,
                [PanelIndex.Panel1] = TripleHorizontalCenterContent,
                [PanelIndex.Panel2] = TripleHorizontalBottomContent
            };

            _displayPanelControls[DisplayMode.TripleTopBottom] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = TripleTopLeftContent,
                [PanelIndex.Panel1] = TripleTopRightContent,
                [PanelIndex.Panel2] = TripleBottomContent
            };

            _displayPanelControls[DisplayMode.TripleBottomTop] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = TripleTopContent,
                [PanelIndex.Panel1] = TripleBottomLeftContent,
                [PanelIndex.Panel2] = TripleBottomRightContent
            };

            _displayPanelControls[DisplayMode.TripleLeftRight] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = TripleLeftContent,
                [PanelIndex.Panel1] = TripleRightTopContent,
                [PanelIndex.Panel2] = TripleRightBottomContent
            };

            _displayPanelControls[DisplayMode.TripleRightLeft] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = TripleLeftTopContent,
                [PanelIndex.Panel1] = TripleLeftBottomContent,
                [PanelIndex.Panel2] = TripleRightContent
            };

            _displayPanelControls[DisplayMode.Quad] = new Dictionary<PanelIndex, ContentControl>
            {
                [PanelIndex.Panel0] = QuadTopLeftContent,
                [PanelIndex.Panel1] = QuadTopRightContent,
                [PanelIndex.Panel2] = QuadBottomLeftContent,
                [PanelIndex.Panel3] = QuadBottomRightContent
            };
        }

        private void InitializePanels()
        {
            // Инициализируем 4 панели, но используем старые ID для совместимости
            foreach (var kvp in _panelIdMap)
            {
                var panelIndex = kvp.Key;
                var panelId = kvp.Value;

                _panels[(int)panelIndex] = _panelRegistry.GetOrCreatePanel(panelId, "MyComputer");
                _panels[(int)panelIndex].StateChanged += OnPanelStateChanged;
            }

            _activePanelManager = _panels[0]; // Panel0 активна по умолчанию
            _panelRegistry.ActivePanelChanged += OnActivePanelChanged;
        }

        private void OnPanelStateChanged(object sender, EventArgs e)
        {
            // Обработка изменения состояния панели
            Debug.WriteLine("Panel state changed");
        }

        private void InitializePanelEventHandlers()
        {
            // Подписываемся на события всех ContentControl'ов
            var allControls = new ContentControl[]
            {
                ViewContainer, VerticalLeftContent, VerticalRightContent,
                HorizontalTopContent, HorizontalBottomContent, TripleVerticalLeftContent,
                TripleVerticalCenterContent, TripleVerticalRightContent, TripleHorizontalTopContent,
                TripleHorizontalCenterContent, TripleHorizontalBottomContent, TripleTopLeftContent,
                TripleTopRightContent, TripleBottomContent, TripleTopContent, TripleBottomLeftContent,
                TripleBottomRightContent, TripleLeftContent, TripleRightTopContent, TripleRightBottomContent,
                TripleLeftTopContent, TripleLeftBottomContent, TripleRightContent, QuadTopLeftContent,
                QuadTopRightContent, QuadBottomLeftContent, QuadBottomRightContent
            };

            foreach (var control in allControls)
            {
                control.GotFocus += (s, e) => SetActivePanel(control);
                AddClickHandlersToPanel(control);
                AddHoverHandlersToPanel(control);
            }
        }

        private void AddClickHandlersToPanel(ContentControl panel)
        {
            panel.PointerPressed += (s, e) =>
            {
                panel.Focus(FocusState.Programmatic);
                e.Handled = true;
            };
        }

        private void AddHoverHandlersToPanel(ContentControl panel)
        {
            panel.PointerEntered += (s, e) => ShowHoverEffect(panel);
            panel.PointerExited += (s, e) => HideHoverEffect(panel);
        }

        private void InitializeBreadcrumbBar()
        {
            var items = new List<BreadcrumbItem>
            {
                new BreadcrumbItem { Text = "This PC", Icon = Symbol.OutlineStar },
                new BreadcrumbItem { Text = "Documents", Icon = Symbol.Document },
                new BreadcrumbItem { Text = "Projects", Icon = Symbol.Folder }
            };
            BreadcrumbBar.ItemsSource = items;
        }

        private void InitializeNavigationButtons()
        {
            BackButton.IsEnabled = _activePanelManager?.CanGoBack ?? false;
            ForwardButton.IsEnabled = _activePanelManager?.CanGoForward ?? false;
            UpButton.IsEnabled = true;
            RefreshButton.IsEnabled = true;
        }
        #endregion

        #region Управление отображением
        private void SetDisplayMode(DisplayMode mode)
        {
            SaveSplitterSizes();
            _currentDisplayMode = mode;

            // Обновляем видимость Grid контейнеров
            UpdateLayoutVisibility(mode);

            // Восстанавливаем представления для всех видимых панелей
            InitializePanelViewsForCurrentMode();

            ApplyIconSizeToVisiblePanels();
            UpdateActivePanelVisualState();
            SaveAllPanelStates();
        }

        private void UpdateLayoutVisibility(DisplayMode mode)
        {
            // Скрываем все контейнеры
            SingleViewGrid.Visibility = Visibility.Collapsed;
            VerticalViewGrid.Visibility = Visibility.Collapsed;
            HorizontalViewGrid.Visibility = Visibility.Collapsed;
            TripleVerticalViewGrid.Visibility = Visibility.Collapsed;
            TripleHorizontalViewGrid.Visibility = Visibility.Collapsed;
            TripleTopBottomViewGrid.Visibility = Visibility.Collapsed;
            TripleBottomTopViewGrid.Visibility = Visibility.Collapsed;
            TripleLeftRightViewGrid.Visibility = Visibility.Collapsed;
            TripleRightLeftViewGrid.Visibility = Visibility.Collapsed;
            QuadViewGrid.Visibility = Visibility.Collapsed;

            // Показываем только нужный контейнер
            switch (mode)
            {
                case DisplayMode.Single:
                    SingleViewGrid.Visibility = Visibility.Visible;
                    break;
                case DisplayMode.Vertical:
                    VerticalViewGrid.Visibility = Visibility.Visible;
                    break;
                case DisplayMode.Horizontal:
                    HorizontalViewGrid.Visibility = Visibility.Visible;
                    break;
                case DisplayMode.TripleVertical:
                    TripleVerticalViewGrid.Visibility = Visibility.Visible;
                    break;
                case DisplayMode.TripleHorizontal:
                    TripleHorizontalViewGrid.Visibility = Visibility.Visible;
                    break;
                case DisplayMode.TripleTopBottom:
                    TripleTopBottomViewGrid.Visibility = Visibility.Visible;
                    break;
                case DisplayMode.TripleBottomTop:
                    TripleBottomTopViewGrid.Visibility = Visibility.Visible;
                    break;
                case DisplayMode.TripleLeftRight:
                    TripleLeftRightViewGrid.Visibility = Visibility.Visible;
                    break;
                case DisplayMode.TripleRightLeft:
                    TripleRightLeftViewGrid.Visibility = Visibility.Visible;
                    break;
                case DisplayMode.Quad:
                    QuadViewGrid.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void InitializePanelViewsForCurrentMode()
        {
            if (!_displayPanelControls.ContainsKey(_currentDisplayMode))
                return;

            var panelControls = _displayPanelControls[_currentDisplayMode];

            foreach (var kvp in panelControls)
            {
                var panelIndex = kvp.Key;
                var control = kvp.Value;
                var panel = _panels[(int)panelIndex];

                if (panel != null && control != null)
                {
                    // Устанавливаем представление для панели
                    SetViewMode(panel.State.ViewMode, control, panel);
                }
            }

            // Убедимся, что активная панель видна
            if (!IsPanelVisibleInCurrentMode(_activePanelIndex))
            {
                // Если активная панель не видна, активируем первую видимую
                _activePanelIndex = panelControls.Keys.First();
                SetActivePanel(_activePanelIndex);
            }
        }

        private bool IsPanelVisibleInCurrentMode(PanelIndex panelIndex)
        {
            return _displayPanelControls[_currentDisplayMode].ContainsKey(panelIndex);
        }
        #endregion

        #region Управление активной панелью
        private void SetActivePanel(PanelIndex panelIndex)
        {
            if (!IsPanelVisibleInCurrentMode(panelIndex)) return;

            _activePanelIndex = panelIndex;
            _activePanelManager = _panels[(int)panelIndex];

            // Активируем через реестр для корректной работы существующей логики
            _panelRegistry.SetActivePanel(_panelIdMap[panelIndex]);

            SaveActivePanelPreference();
            UpdateActivePanelVisualState();
            UpdateRadioButtonsForActivePanel();
            UpdateNavigationButtons();
        }

        private void SetActivePanel(ContentControl panel)
        {
            // Находим PanelIndex по ContentControl в текущем режиме отображения
            var panelControls = _displayPanelControls[_currentDisplayMode];
            var panelPair = panelControls.FirstOrDefault(x => x.Value == panel);

            if (panelPair.Value != null)
            {
                SetActivePanel(panelPair.Key);
            }
        }

        private void SaveActivePanelPreference()
        {
            App.SettingsManager?.SaveSetting("ActivePanelIndex", (int)_activePanelIndex);
        }

        private ContentControl GetActivePanelControl()
        {
            var panelControls = _displayPanelControls[_currentDisplayMode];
            return panelControls.ContainsKey(_activePanelIndex) ? panelControls[_activePanelIndex] : null;
        }

        private PanelManager GetActivePanelManager()
        {
            return _panels[(int)_activePanelIndex];
        }
        #endregion

        #region Обработчики отображения
        private void SingleView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.Single);
            UpdateViewMenuSelection();
        }

        private void VerticalView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.Vertical);
            UpdateViewMenuSelection();
        }

        private void HorizontalView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.Horizontal);
            UpdateViewMenuSelection();
        }

        private void TripleVerticalView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.TripleVertical);
            UpdateViewMenuSelection();
        }

        private void TripleHorizontalView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.TripleHorizontal);
            UpdateViewMenuSelection();
        }

        private void TripleTopBottomView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.TripleTopBottom);
            UpdateViewMenuSelection();
        }

        private void TripleBottomTopView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.TripleBottomTop);
            UpdateViewMenuSelection();
        }

        private void TripleLeftRightView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.TripleLeftRight);
            UpdateViewMenuSelection();
        }

        private void TripleRightLeftView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.TripleRightLeft);
            UpdateViewMenuSelection();
        }

        private void QuadView_Click(object sender, RoutedEventArgs e)
        {
            SetDisplayMode(DisplayMode.Quad);
            UpdateViewMenuSelection();
        }

        private void UpdateViewMenuSelection()
        {
            SingleViewRadio.IsChecked = _currentDisplayMode == DisplayMode.Single;
            VerticalViewRadio.IsChecked = _currentDisplayMode == DisplayMode.Vertical;
            HorizontalViewRadio.IsChecked = _currentDisplayMode == DisplayMode.Horizontal;
            TripleVerticalViewRadio.IsChecked = _currentDisplayMode == DisplayMode.TripleVertical;
            TripleHorizontalViewRadio.IsChecked = _currentDisplayMode == DisplayMode.TripleHorizontal;
            TripleTopBottomViewRadio.IsChecked = _currentDisplayMode == DisplayMode.TripleTopBottom;
            TripleBottomTopViewRadio.IsChecked = _currentDisplayMode == DisplayMode.TripleBottomTop;
            TripleLeftRightViewRadio.IsChecked = _currentDisplayMode == DisplayMode.TripleLeftRight;
            TripleRightLeftViewRadio.IsChecked = _currentDisplayMode == DisplayMode.TripleRightLeft;
            QuadViewRadio.IsChecked = _currentDisplayMode == DisplayMode.Quad;
        }
        #endregion

        #region Методы применения настроек
        private void ApplyIconSizeToVisiblePanels()
        {
            var panelControls = _displayPanelControls[_currentDisplayMode];

            foreach (var kvp in panelControls)
            {
                var panelIndex = kvp.Key;
                var control = kvp.Value;
                var panel = _panels[(int)panelIndex];

                if (control != null)
                {
                    ApplyIconSizeToPanel(control, panel.State.IconSize);
                }
            }
        }

        private void ApplyIconSizeToPanel(ContentControl panel, string iconSize)
        {
            if (panel.Content is ISupportsIconSize sizeSupport)
            {
                Debug.WriteLine($"Setting icon size for panel: {iconSize}");
                sizeSupport.SetIconSize(iconSize);
            }
        }

        private void ApplyIconSizeToActivePanelOnly()
        {
            if (_activePanelManager == null) return;
            Debug.WriteLine($"Applying icon size to active panel only: {_activePanelManager.State.IconSize}");
            ContentControl activeControl = GetActivePanelControl();
            if (activeControl != null && activeControl.Content is ISupportsIconSize sizeSupport)
                sizeSupport.SetIconSize(_activePanelManager.State.IconSize);
        }
        #endregion

        #region Загрузка состояний
        private void LoadAllPanelStates()
        {
            try
            {
                // Загружаем настройки
                var currentDisplayModeStr = App.SettingsManager?.GetSetting<string>("CurrentDisplayMode") ?? "Single";
                _currentDisplayMode = Enum.TryParse<DisplayMode>(currentDisplayModeStr, out var mode) ? mode : DisplayMode.Single;

                var previewPanelModeStr = App.SettingsManager?.GetSetting<string>("PreviewPanelMode") ?? "None";
                _previewPanelMode = Enum.TryParse<PreviewPanelMode>(previewPanelModeStr, out var previewMode) ? previewMode : PreviewPanelMode.None;

                _activePanelIndex = App.SettingsManager?.GetSetting<PanelIndex>("ActivePanelIndex") ?? PanelIndex.Panel0;

                // Загружаем состояния для всех 4 панелей
                for (int i = 0; i < 4; i++)
                {
                    _panels[i]?.LoadState();
                }

                // Восстанавливаем активную панель
                SetActivePanel(_activePanelIndex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки состояний панелей: {ex.Message}");
                ResetAllPanelStatesToDefault();
            }
        }

        private void LoadPreviewPanelSizes()
        {
            _leftPreviewWidth = App.SettingsManager?.GetSetting<double>("LeftPreviewWidth", 300) ?? 300;
            _rightPreviewWidth = App.SettingsManager?.GetSetting<double>("RightPreviewWidth", 300) ?? 300;

            ColumPreviewLeft.MinWidth = 0;
            ColumPreviewRigth.MinWidth = 0;
        }

        private void ResetAllPanelStatesToDefault()
        {
            _currentDisplayMode = DisplayMode.Single;
            _previewPanelMode = PreviewPanelMode.None;

            for (int i = 0; i < 4; i++)
            {
                _panels[i]?.LoadState(new PanelState
                {
                    IconSize = "Icons Medium",
                    ViewMode = ViewMode.Icons,
                    CurrentPath = "MyComputer"
                });
            }

            SetActivePanel(PanelIndex.Panel0);
            _prefsize = "Icons";
        }
        #endregion

        #region Сохранение состояний
        private void SaveAllPanelStates()
        {
            App.SettingsManager?.SaveSetting("CurrentDisplayMode", _currentDisplayMode.ToString());
            App.SettingsManager?.SaveSetting("PreviewPanelMode", _previewPanelMode.ToString());
            App.SettingsManager?.SaveSetting("ActivePanelIndex", _activePanelIndex);

            // Состояния панелей автоматически сохраняются через AutoSavePanelState в PanelManager
        }

        private void SavePreviewPanelSizes()
        {
            if (ColumPreviewLeft.ActualWidth > 0)
                _leftPreviewWidth = ColumPreviewLeft.ActualWidth;
            if (ColumPreviewRigth.ActualWidth > 0)
                _rightPreviewWidth = ColumPreviewRigth.ActualWidth;

            App.SettingsManager?.SaveSetting("LeftPreviewWidth", _leftPreviewWidth);
            App.SettingsManager?.SaveSetting("RightPreviewWidth", _rightPreviewWidth);

            Debug.WriteLine($"Saved preview sizes: Left={_leftPreviewWidth}, Right={_rightPreviewWidth}");
        }

        private void SaveSplitterSizes()
        {
            _splitterManager.SaveAllSplitterSizes(this);
        }
        #endregion

        #region Методы управления сплиттерами
        private void CleanInvalidSplitterSettings()
        {
            try
            {
                var keys = new[]
                {
                    "Splitter_Vertical_Left", "Splitter_Vertical_Right",
                    "Splitter_Horizontal_Top", "Splitter_Horizontal_Bottom",
                    "Splitter_TripleVertical_Left", "Splitter_TripleVertical_Center", "Splitter_TripleVertical_Right",
                    "Splitter_TripleHorizontal_Top", "Splitter_TripleHorizontal_Center", "Splitter_TripleHorizontal_Bottom",
                    "Splitter_TripleTopBottom_Left", "Splitter_TripleTopBottom_Right", "Splitter_TripleTopBottom_Top", "Splitter_TripleTopBottom_Bottom",
                    "Splitter_TripleBottomTop_Left", "Splitter_TripleBottomTop_Right", "Splitter_TripleBottomTop_Top", "Splitter_TripleBottomTop_Bottom",
                    "Splitter_TripleLeftRight_Left", "Splitter_TripleLeftRight_Top", "Splitter_TripleLeftRight_Bottom",
                    "Splitter_TripleRightLeft_Right", "Splitter_TripleRightLeft_Top", "Splitter_TripleRightLeft_Bottom",
                    "Splitter_Quad_Left", "Splitter_Quad_Right", "Splitter_Quad_Top", "Splitter_Quad_Bottom"
                };

                foreach (var key in keys)
                {
                    var value = App.SettingsManager.GetSetting<double>(key, -1);
                    if (value <= 0)
                    {
                        App.SettingsManager.SaveSetting(key, 0);
                        Debug.WriteLine($"Cleaned invalid setting: {key}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning splitter settings: {ex.Message}");
            }
        }

        private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            SaveSplitterSizes();
        }
        #endregion

        #region Методы управления предпросмотром
        private void PreviewSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            SavePreviewPanelSizes();
        }

        private void PreviewSplitter_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            SavePreviewPanelSizes();
        }

        private void UpdatePreviewPanelVisibility()
        {
            if (PreviewPanelLeft == null || PreviewPanelRight == null ||
                ColumPreviewLeft == null || ColumPreviewRigth == null ||
                LeftPreviewSplitter == null || RightPreviewSplitter == null)
                return;

            switch (_previewPanelMode)
            {
                case PreviewPanelMode.Left:
                    PreviewPanelLeft.Visibility = Visibility.Visible;
                    LeftPreviewSplitter.Visibility = Visibility.Visible;
                    PreviewPanelRight.Visibility = Visibility.Collapsed;
                    RightPreviewSplitter.Visibility = Visibility.Collapsed;
                    ColumPreviewLeft.Width = new GridLength(_leftPreviewWidth, GridUnitType.Pixel);
                    ColumPreviewRigth.Width = new GridLength(0, GridUnitType.Pixel);
                    break;
                case PreviewPanelMode.Right:
                    PreviewPanelLeft.Visibility = Visibility.Collapsed;
                    LeftPreviewSplitter.Visibility = Visibility.Collapsed;
                    PreviewPanelRight.Visibility = Visibility.Visible;
                    RightPreviewSplitter.Visibility = Visibility.Visible;
                    ColumPreviewLeft.Width = new GridLength(0, GridUnitType.Pixel);
                    ColumPreviewRigth.Width = new GridLength(_rightPreviewWidth, GridUnitType.Pixel);
                    break;
                case PreviewPanelMode.None:
                    PreviewPanelLeft.Visibility = Visibility.Collapsed;
                    LeftPreviewSplitter.Visibility = Visibility.Collapsed;
                    PreviewPanelRight.Visibility = Visibility.Collapsed;
                    RightPreviewSplitter.Visibility = Visibility.Collapsed;
                    ColumPreviewLeft.Width = new GridLength(0, GridUnitType.Pixel);
                    ColumPreviewRigth.Width = new GridLength(0, GridUnitType.Pixel);
                    break;
            }

            InvalidateArrange();
            UpdateLayout();
        }

        private void UpdatePreviewRadioButtons()
        {
            PreviewLeftRadio.Click -= PreviewLeft_Click;
            PreviewRightRadio.Click -= PreviewRight_Click;
            PreviewNoneRadio.Click -= PreviewNone_Click;

            try
            {
                PreviewLeftRadio.IsChecked = false;
                PreviewRightRadio.IsChecked = false;
                PreviewNoneRadio.IsChecked = false;

                switch (_previewPanelMode)
                {
                    case PreviewPanelMode.Left:
                        PreviewLeftRadio.IsChecked = true;
                        break;
                    case PreviewPanelMode.Right:
                        PreviewRightRadio.IsChecked = true;
                        break;
                    case PreviewPanelMode.None:
                    default:
                        PreviewNoneRadio.IsChecked = true;
                        break;
                }

                Debug.WriteLine($"Preview radio buttons updated. Mode: {_previewPanelMode}");
            }
            finally
            {
                PreviewLeftRadio.Click += PreviewLeft_Click;
                PreviewRightRadio.Click += PreviewRight_Click;
                PreviewNoneRadio.Click += PreviewNone_Click;
            }
        }

        private void PreviewLeft_Click(object sender, RoutedEventArgs e)
        {
            SavePreviewPanelSizes();
            _previewPanelMode = PreviewPanelMode.Left;
            UpdatePreviewPanelVisibility();
            SaveAllPanelStates();
        }

        private void PreviewRight_Click(object sender, RoutedEventArgs e)
        {
            SavePreviewPanelSizes();
            _previewPanelMode = PreviewPanelMode.Right;
            UpdatePreviewPanelVisibility();
            SaveAllPanelStates();
        }

        private void PreviewNone_Click(object sender, RoutedEventArgs e)
        {
            SavePreviewPanelSizes();
            _previewPanelMode = PreviewPanelMode.None;
            UpdatePreviewPanelVisibility();
            SaveAllPanelStates();
        }
        #endregion

        #region Методы управления радио-кнопками
        private void UpdateRadioButtonsForActivePanel()
        {
            if (_activePanelManager == null) return;

            UnsubscribeFromRadioButtonEvents();
            try
            {
                UpdateViewModeRadioButtons(_activePanelManager.State.ViewMode);
                SetSelectedRadioButton(_activePanelManager.State.IconSize);
            }
            finally
            {
                SubscribeToRadioButtonEvents();
            }
        }

        private void UnsubscribeFromRadioButtonEvents()
        {
            IconsModeRadioButton.Checked -= ViewModeRadioButton_Checked;
            ListModeRadioButton.Checked -= ViewModeRadioButton_Checked;
            CompListModeRadioButton.Checked -= ViewModeRadioButton_Checked;
            TilesModeRadioButton.Checked -= ViewModeRadioButton_Checked;
            TableModeRadioButton.Checked -= ViewModeRadioButton_Checked;
            TinySizeRadioButton.Checked -= SizeRadioButton_Checked;
            ExtraSmallSizeRadioButton.Checked -= SizeRadioButton_Checked;
            SmallSizeRadioButton.Checked -= SizeRadioButton_Checked;
            BelowMediumSizeRadioButton.Checked -= SizeRadioButton_Checked;
            MediumSizeRadioButton.Checked -= SizeRadioButton_Checked;
            AboveMediumSizeRadioButton.Checked -= SizeRadioButton_Checked;
            LargeSizeRadioButton.Checked -= SizeRadioButton_Checked;
            ExtraLargeSizeRadioButton.Checked -= SizeRadioButton_Checked;
            HugeSizeRadioButton.Checked -= SizeRadioButton_Checked;
        }

        private void SubscribeToRadioButtonEvents()
        {
            IconsModeRadioButton.Checked += ViewModeRadioButton_Checked;
            ListModeRadioButton.Checked += ViewModeRadioButton_Checked;
            CompListModeRadioButton.Checked += ViewModeRadioButton_Checked;
            TilesModeRadioButton.Checked += ViewModeRadioButton_Checked;
            TableModeRadioButton.Checked += ViewModeRadioButton_Checked;
            TinySizeRadioButton.Checked += SizeRadioButton_Checked;
            ExtraSmallSizeRadioButton.Checked += SizeRadioButton_Checked;
            SmallSizeRadioButton.Checked += SizeRadioButton_Checked;
            BelowMediumSizeRadioButton.Checked += SizeRadioButton_Checked;
            MediumSizeRadioButton.Checked += SizeRadioButton_Checked;
            AboveMediumSizeRadioButton.Checked += SizeRadioButton_Checked;
            LargeSizeRadioButton.Checked += SizeRadioButton_Checked;
            ExtraLargeSizeRadioButton.Checked += SizeRadioButton_Checked;
            HugeSizeRadioButton.Checked += SizeRadioButton_Checked;
        }

        private void UpdateViewModeRadioButtons(ViewMode mode)
        {
            IconsModeRadioButton.IsChecked = false;
            ListModeRadioButton.IsChecked = false;
            CompListModeRadioButton.IsChecked = false;
            TilesModeRadioButton.IsChecked = false;
            TableModeRadioButton.IsChecked = false;

            switch (mode)
            {
                case ViewMode.Icons:
                    IconsModeRadioButton.IsChecked = true;
                    break;
                case ViewMode.List:
                    ListModeRadioButton.IsChecked = true;
                    break;
                case ViewMode.CompactList:
                    CompListModeRadioButton.IsChecked = true;
                    break;
                case ViewMode.Tiles:
                    TilesModeRadioButton.IsChecked = true;
                    break;
                case ViewMode.Table:
                    TableModeRadioButton.IsChecked = true;
                    break;
            }
        }

        private void SetSelectedRadioButton(string fullSizeKey)
        {
            if (string.IsNullOrEmpty(fullSizeKey))
            {
                MediumSizeRadioButton.IsChecked = true;
                return;
            }

            string sizePart = ExtractSizePartFromFullKey(fullSizeKey);

            TinySizeRadioButton.IsChecked = false;
            ExtraSmallSizeRadioButton.IsChecked = false;
            SmallSizeRadioButton.IsChecked = false;
            BelowMediumSizeRadioButton.IsChecked = false;
            MediumSizeRadioButton.IsChecked = false;
            AboveMediumSizeRadioButton.IsChecked = false;
            LargeSizeRadioButton.IsChecked = false;
            ExtraLargeSizeRadioButton.IsChecked = false;
            HugeSizeRadioButton.IsChecked = false;

            switch (sizePart)
            {
                case "Tiny":
                    TinySizeRadioButton.IsChecked = true;
                    break;
                case "Extra Small":
                    ExtraSmallSizeRadioButton.IsChecked = true;
                    break;
                case "Small":
                    SmallSizeRadioButton.IsChecked = true;
                    break;
                case "Below Medium":
                    BelowMediumSizeRadioButton.IsChecked = true;
                    break;
                case "Medium":
                    MediumSizeRadioButton.IsChecked = true;
                    break;
                case "Above Medium":
                    AboveMediumSizeRadioButton.IsChecked = true;
                    break;
                case "Large":
                    LargeSizeRadioButton.IsChecked = true;
                    break;
                case "Extra Large":
                    ExtraLargeSizeRadioButton.IsChecked = true;
                    break;
                case "Huge":
                    HugeSizeRadioButton.IsChecked = true;
                    break;
                default:
                    MediumSizeRadioButton.IsChecked = true;
                    break;
            }
        }

        private string ExtractSizePartFromFullKey(string fullSizeKey)
        {
            if (string.IsNullOrEmpty(fullSizeKey))
                return "Medium";

            var parts = fullSizeKey.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "Medium";

            if (parts.Length >= 3)
            {
                string potentialThreeWordSize = $"{parts[parts.Length - 3]} {parts[parts.Length - 2]} {parts[parts.Length - 1]}";
                if (potentialThreeWordSize == "Below Medium" || potentialThreeWordSize == "Above Medium")
                    return potentialThreeWordSize;
            }

            if (parts.Length >= 2)
            {
                string potentialMultiWordSize = $"{parts[parts.Length - 2]} {parts[parts.Length - 1]}";
                if (potentialMultiWordSize == "Extra Small" || potentialMultiWordSize == "Extra Large")
                    return potentialMultiWordSize;
            }

            return parts[parts.Length - 1];
        }

        private void SizeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingViewMode) return;

            if (sender is RadioButton radioButton && radioButton.Tag is string sizeTag && _activePanelManager != null)
            {
                if (radioButton.IsChecked == true)
                {
                    string fullSizeKey = $"{_prefsize} {sizeTag}";
                    _activePanelManager.UpdateState(state => state.IconSize = fullSizeKey);
                    ApplyIconSizeToActivePanelOnly();

                    // Сохранение настроек размера иконок
                    bool saved = false;
                    if (App.SettingsManager != null)
                    {
                        try
                        {
                            App.SettingsManager.SaveSetting("SelectedSizeIconViewPage", fullSizeKey);
                            saved = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка SettingsManager при сохранении размера иконок: {ex.Message}");
                        }
                    }

                    if (!saved)
                    {
                        try
                        {
                            var localSettings = ApplicationData.Current.LocalSettings.Values;
                            localSettings["SelectedSizeIconViewPage"] = fullSizeKey;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Ошибка LocalSettings при сохранении размера иконок: {ex.Message}");
                        }
                    }

                    SaveAllPanelStates();
                }
            }
        }

        private void ViewModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingViewMode) return;

            try
            {
                _isUpdatingViewMode = true;

                if (sender is RadioButton radioButton && radioButton.Tag is string modeTag && _activePanelManager != null)
                {
                    ViewMode mode = (ViewMode)Enum.Parse(typeof(ViewMode), modeTag);
                    _activePanelManager.UpdateState(state =>
                    {
                        state.ViewMode = mode;
                        _prefsize = GetViewModePrefix(mode);
                        string currentSize = ExtractSizePartFromFullKey(state.IconSize);
                        state.IconSize = $"{_prefsize} {currentSize}";
                    });

                    SetViewMode(mode);
                    ApplyIconSizeToActivePanelOnly();
                    SaveAllPanelStates();
                }
            }
            finally
            {
                _isUpdatingViewMode = false;
            }
        }

        private string GetViewModePrefix(ViewMode viewMode)
        {
            return viewMode switch
            {
                ViewMode.Icons => "Icons",
                ViewMode.List => "List",
                ViewMode.CompactList => "CompactList",
                ViewMode.Tiles => "Tiles",
                ViewMode.Table => "Table",
                _ => "Icons"
            };
        }
        #endregion

        #region Визуальные методы
        private void ShowHoverEffect(ContentControl panel)
        {
            var accentBrush = Application.Current.Resources["AccentBorderBrush"] as SolidColorBrush;
            if (panel.BorderBrush != accentBrush)
                panel.BorderBrush = new SolidColorBrush(Colors.LightGray);
        }

        private void HideHoverEffect(ContentControl panel)
        {
            var accentBrush = Application.Current.Resources["AccentBorderBrush"] as SolidColorBrush;
            if (panel.BorderBrush != accentBrush)
                panel.BorderBrush = Application.Current.Resources["TransparentBrush"] as SolidColorBrush;
        }

        private void UpdateActivePanelVisualState()
        {
            ResetAllPanelBorders();
            var accentBrush = (SolidColorBrush)Application.Current.Resources["AccentBorderBrush"];
            ContentControl activeControl = GetActivePanelControl();
            if (activeControl != null)
                activeControl.BorderBrush = accentBrush;
        }

        private void ResetAllPanelBorders()
        {
            var transparentBrush = (SolidColorBrush)Application.Current.Resources["TransparentBrush"];

            var allPanels = new ContentControl[]
            {
                ViewContainer, VerticalLeftContent, VerticalRightContent,
                HorizontalTopContent, HorizontalBottomContent, TripleVerticalLeftContent,
                TripleVerticalCenterContent, TripleVerticalRightContent, TripleHorizontalTopContent,
                TripleHorizontalCenterContent, TripleHorizontalBottomContent, TripleTopLeftContent,
                TripleTopRightContent, TripleBottomContent, TripleTopContent, TripleBottomLeftContent,
                TripleBottomRightContent, TripleLeftContent, TripleRightTopContent, TripleRightBottomContent,
                TripleLeftTopContent, TripleLeftBottomContent, TripleRightContent, QuadTopLeftContent,
                QuadTopRightContent, QuadBottomLeftContent, QuadBottomRightContent
            };

            foreach (var panel in allPanels)
            {
                panel.BorderBrush = transparentBrush;
            }
        }

        private void ResetVisualPanels()
        {
            try
            {
                Debug.WriteLine($"[ViewPage] Resetting visual panels");

                var allVisualPanels = new ContentControl[]
                {
                    ViewContainer, VerticalLeftContent, VerticalRightContent,
                    HorizontalTopContent, HorizontalBottomContent, TripleVerticalLeftContent,
                    TripleVerticalCenterContent, TripleVerticalRightContent, TripleHorizontalTopContent,
                    TripleHorizontalCenterContent, TripleHorizontalBottomContent, TripleTopLeftContent,
                    TripleTopRightContent, TripleBottomContent, TripleTopContent, TripleBottomLeftContent,
                    TripleBottomRightContent, TripleLeftContent, TripleRightTopContent, TripleRightBottomContent,
                    TripleLeftTopContent, TripleLeftBottomContent, TripleRightContent, QuadTopLeftContent,
                    QuadTopRightContent, QuadBottomLeftContent, QuadBottomRightContent
                };

                foreach (var panel in allVisualPanels)
                {
                    if (panel?.Content is IRefreshablePanel refreshablePanel)
                    {
                        refreshablePanel.RefreshNavigation();
                    }
                }

                Debug.WriteLine($"[ViewPage] Visual panels reset completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ViewPage] Error resetting visual panels: {ex}");
            }
        }
        #endregion

        #region Обработчики событий
        private async void ViewPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Сначала загружаем состояния панелей
            LoadAllPanelStates();

            // ЗАТЕМ загружаем размер иконок и применяем его к активной панели
            string savedIconSize = null;
            if (App.SettingsManager != null)
            {
                savedIconSize = App.SettingsManager.GetSetting<string>("SelectedSizeIconViewPage");
            }

            if (string.IsNullOrEmpty(savedIconSize))
            {
                var localSettings = ApplicationData.Current.LocalSettings.Values;
                if (localSettings?.ContainsKey("SelectedSizeIconViewPage") == true)
                {
                    savedIconSize = localSettings["SelectedSizeIconViewPage"]?.ToString();
                }
            }

            if (!string.IsNullOrEmpty(savedIconSize) && _activePanelManager != null)
            {
                _activePanelManager.UpdateState(state => state.IconSize = savedIconSize);
                Debug.WriteLine($"Загружен размер иконок: {savedIconSize}");

                // Также обновляем _prefsize на основе загруженного размера
                if (savedIconSize.StartsWith("Icons "))
                    _prefsize = "Icons";
                else if (savedIconSize.StartsWith("List "))
                    _prefsize = "List";
                else if (savedIconSize.StartsWith("CompactList "))
                    _prefsize = "CompactList";
                else if (savedIconSize.StartsWith("Tiles "))
                    _prefsize = "Tiles";
                else if (savedIconSize.StartsWith("Table "))
                    _prefsize = "Table";
            }
            else
            {
                Debug.WriteLine("Используются настройки размера иконок по умолчанию.");
            }

            // Восстанавливаем режим отображения
            switch (_currentDisplayMode)
            {
                case DisplayMode.Single:
                    SingleView_Click(null, null);
                    break;
                case DisplayMode.Vertical:
                    VerticalView_Click(null, null);
                    break;
                case DisplayMode.Horizontal:
                    HorizontalView_Click(null, null);
                    break;
                case DisplayMode.TripleVertical:
                    TripleVerticalView_Click(null, null);
                    break;
                case DisplayMode.TripleHorizontal:
                    TripleHorizontalView_Click(null, null);
                    break;
                case DisplayMode.TripleTopBottom:
                    TripleTopBottomView_Click(null, null);
                    break;
                case DisplayMode.TripleBottomTop:
                    TripleBottomTopView_Click(null, null);
                    break;
                case DisplayMode.TripleLeftRight:
                    TripleLeftRightView_Click(null, null);
                    break;
                case DisplayMode.TripleRightLeft:
                    TripleRightLeftView_Click(null, null);
                    break;
                case DisplayMode.Quad:
                    QuadView_Click(null, null);
                    break;
            }

            await Task.Delay(5);

            var splitterSizes = _splitterManager.LoadAllSplitterSizes();
            _splitterManager.ApplySplitterSizes(this, splitterSizes);

            LoadPreviewPanelSizes();
            UpdatePreviewPanelVisibility();
            UpdatePreviewRadioButtons();

            ApplyIconSizeToVisiblePanels();
            UpdateRadioButtonsForActivePanel();
            UpdateActivePanelVisualState();
            SetFocusToActivePanel();
            UpdateNavigationButtons();

            _panelRegistry.ActivePanelChanged += OnActivePanelChanged;
        }

        private void OnActivePanelChanged(object sender, PanelManager panel)
        {
            // Обновляем активную панель на основе PanelId
            if (panel?.PanelId != null)
            {
                var panelPair = _panelIdMap.FirstOrDefault(x => x.Value == panel.PanelId);
                if (panelPair.Value != null)
                {
                    _activePanelIndex = panelPair.Key;
                    _activePanelManager = panel;
                }
            }

            UpdateNavigationButtons();
            UpdateRadioButtonsForActivePanel();
            UpdateActivePanelVisualState();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activePanelManager != null && _activePanelManager.CanGoBack)
            {
                _activePanelManager.GoBack();
                UpdateNavigationButtons();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activePanelManager != null && _activePanelManager.CanGoForward)
            {
                _activePanelManager.GoForward();
                UpdateNavigationButtons();
            }
        }

        private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            if (_activePanelManager != null && args.Item is BreadcrumbItem item)
            {
                _activePanelManager.NavigateTo(item.Text);
                UpdateNavigationButtons();
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Up clicked");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Refresh clicked");
            RefreshActivePanel();
        }

        private void RefreshActivePanel()
        {
            Debug.WriteLine("Refreshing active panel");

            if (_activePanelManager != null)
            {
                var activeControl = GetActivePanelControl();
                if (activeControl?.Content is IRefreshablePanel refreshablePanel)
                {
                    refreshablePanel.RefreshNavigation();
                }
            }
        }

        private void RefreshAllPanels()
        {
            Debug.WriteLine("Refreshing all panels");

            var allControls = new ContentControl[]
            {
                ViewContainer, VerticalLeftContent, VerticalRightContent,
                HorizontalTopContent, HorizontalBottomContent, TripleVerticalLeftContent,
                TripleVerticalCenterContent, TripleVerticalRightContent, TripleHorizontalTopContent,
                TripleHorizontalCenterContent, TripleHorizontalBottomContent, TripleTopLeftContent,
                TripleTopRightContent, TripleBottomContent, TripleTopContent, TripleBottomLeftContent,
                TripleBottomRightContent, TripleLeftContent, TripleRightTopContent, TripleRightBottomContent,
                TripleLeftTopContent, TripleLeftBottomContent, TripleRightContent, QuadTopLeftContent,
                QuadTopRightContent, QuadBottomLeftContent, QuadBottomRightContent
            };

            foreach (var control in allControls)
            {
                if (control?.Content is IRefreshablePanel refreshablePanel)
                {
                    refreshablePanel.RefreshNavigation();
                }
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (_activePanelManager != null)
                _activePanelManager.UpdateState(state => state.SearchFilter = args.QueryText);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Copy clicked");
        }

        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Paste clicked");
        }

        private void CutButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Cut clicked");
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Delete clicked");
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Rename clicked");
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Move clicked");
        }

        private void SetViewMode(ViewMode mode, ContentControl targetControl = null, PanelManager targetManager = null)
        {
            PanelManager managerToUse = targetManager ?? _activePanelManager;
            ContentControl controlToUse = targetControl ?? GetActivePanelControl();

            if (managerToUse == null || controlToUse == null) return;

            managerToUse.UpdateState(state => state.ViewMode = mode);
            _prefsize = GetViewModePrefix(mode);

            // Сначала отписываемся от старого содержимого
            UnsubscribeFromTileViewEvents(controlToUse);

            UserControl newView = null;

            switch (mode)
            {
                case ViewMode.Icons:
                    {
                        var tileViewer = new TileViewerContent();
                        tileViewer.PanelId = managerToUse.PanelId;
                        tileViewer.SetPanelManager(managerToUse);
                        tileViewer.DisplayMode = "Vertical"; // Для иконок вертикальный режим
                        tileViewer.NavigationChanged += TileView_NavigationChanged;
                        newView = tileViewer;
                    }
                    break;

                case ViewMode.List:
                    {
                        var tileViewer = new TileViewerContent();
                        tileViewer.PanelId = managerToUse.PanelId;
                        tileViewer.SetPanelManager(managerToUse);
                        tileViewer.DisplayMode = "Horizontal"; // Для списка горизонтальный режим
                        tileViewer.NavigationChanged += TileView_NavigationChanged;
                        newView = tileViewer;
                    }
                    break;

                case ViewMode.Table:
                    // newView = new TileTableView();
                    break;

                case ViewMode.Tiles:
                    // newView = new TileTilesView();
                    break;

                case ViewMode.CompactList:
                    // newView = new TileCompactListView();
                    break;
            }

            if (newView != null)
            {
                newView.DataContext = managerToUse.State.DataContext ?? this.DataContext;
                controlToUse.Content = newView;
            }

            ApplyIconSizeToVisiblePanels();
            SaveAllPanelStates();
        }

        private void TileView_NavigationChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("NavigationChanged event received from TileView");
            UpdateNavigationButtons();
            SaveAllPanelStates();
        }

        private void UnsubscribeFromTileViewEvents(ContentControl control)
        {
            if (control?.Content is TileViewerContent tileViewerContent)
            {
                tileViewerContent.NavigationChanged -= TileView_NavigationChanged;
            }
            //else if (control?.Content is TileTableView tileTableView)
            //{
            //    tileTableView.NavigationChanged -= TileView_NavigationChanged;
            //}
            //else if (control?.Content is TileTilesView tileTilesView)
            //{
            //    tileTilesView.NavigationChanged -= TileView_NavigationChanged;
            //}
            //else if (control?.Content is TileCompactListView tileCompactListView)
            //{
            //    tileCompactListView.NavigationChanged -= TileView_NavigationChanged;
            //}
        }

        private void SetFocusToActivePanel()
        {
            ContentControl activeControl = GetActivePanelControl();
            activeControl?.Focus(FocusState.Programmatic);
        }

        internal void UpdateNavigationButtons()
        {
            bool canGoBack = _activePanelManager?.CanGoBack ?? false;
            bool canGoForward = _activePanelManager?.CanGoForward ?? false;

            BackButton.IsEnabled = canGoBack;
            ForwardButton.IsEnabled = canGoForward;

            BackButton.InvalidateArrange();
            ForwardButton.InvalidateArrange();
        }
        #endregion
    }
}