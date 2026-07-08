using Core_FileManagement;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ufm.Controls;
using ufm.Models;
using WinRT.Interop;

namespace ufm
{
    public sealed partial class ViewPage : Page
    {
        #region Enums
        public enum PanelIndex { Panel0 = 0, Panel1 = 1, Panel2 = 2, Panel3 = 3 }
        public enum DisplayMode { Single, Vertical, Horizontal, TripleVertical, TripleHorizontal, TripleTopBottom, TripleBottomTop, TripleLeftRight, TripleRightLeft, Quad }
        public enum PreviewPanelMode { None, Left, Right }
        public enum ViewMode { Icons, List, Table, Tiles, CompactList }
        #endregion

        #region Fields
        private double _leftPreviewWidth = 300;
        private double _rightPreviewWidth = 300;
        private DisplayMode _currentDisplayMode = DisplayMode.Single;
        private string _prefsize = "Icons";
        private bool _isUpdatingViewMode = false;
        private PreviewPanelMode _previewPanelMode = PreviewPanelMode.None;
        private PanelIndex _activePanelIndex = PanelIndex.Panel0;

        private SplitterManager _splitterManager;
        private readonly INavigationManager _navigationManager;
        private PanelManagerRegistry _panelRegistry;
        private PanelSynchronizationService _syncService;
        private PanelManager _activePanelManager;
        private readonly PanelManager[] _panels = new PanelManager[4];
        private TreePanelPg01 _treePanel;
        private readonly FileSystemService _fileSystemService = new FileSystemService();

        private TileViewerContent _activeTileViewer;
        private IFileOperationService _fileOperationService;

        private ContentDialog _deleteConfirmationDialog;
        private bool _isDeleteDialogOpen = false;

        private readonly Dictionary<PanelIndex, string> _panelIdMap = new Dictionary<PanelIndex, string>
        {
            [PanelIndex.Panel0] = "SinglePanel",
            [PanelIndex.Panel1] = "RightPanel",
            [PanelIndex.Panel2] = "TripleVerticalCenterPanel",
            [PanelIndex.Panel3] = "QuadBottomRightPanel"
        };

        private readonly Dictionary<DisplayMode, Dictionary<PanelIndex, ContentControl>> _displayPanelControls =
            new Dictionary<DisplayMode, Dictionary<PanelIndex, ContentControl>>();
        #endregion

        #region Constructor & Initialization
        public ViewPage()
        {
            this.InitializeComponent();

            _navigationManager = new NavigationManager();
            _panelRegistry = new PanelManagerRegistry(_navigationManager);
            _syncService = new PanelSynchronizationService(_panelRegistry);

            InitializePanelMappings();
            InitializePanels();
            InitializePanelEventHandlers();
            InitializeNavigationButtons();

            _splitterManager = new SplitterManager(App.SettingsManager);

            VerticalSplitter.PointerReleased += Splitter_PointerReleased;
            HorizontalSplitter.PointerReleased += Splitter_PointerReleased;
            TripleVerticalSplitter1.PointerReleased += Splitter_PointerReleased;
            TripleVerticalSplitter2.PointerReleased += Splitter_PointerReleased;
            TripleHorizontalSplitter1.PointerReleased += Splitter_PointerReleased;
            TripleHorizontalSplitter2.PointerReleased += Splitter_PointerReleased;

            PreviewLeftRadio.Click += PreviewLeft_Click;
            PreviewRightRadio.Click += PreviewRight_Click;
            PreviewNoneRadio.Click += PreviewNone_Click;

            this.Loaded += ViewPage_Loaded;
            this.Unloaded += ViewPage_Unloaded;

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
            foreach (var kvp in _panelIdMap)
            {
                var panelIndex = kvp.Key;
                var panelId = kvp.Value;

                _panels[(int)panelIndex] = _panelRegistry.GetOrCreatePanel(panelId, "MyComputer");
                _panels[(int)panelIndex].StateChanged += OnPanelStateChanged;
            }

            _activePanelManager = _panels[0];
            _panelRegistry.ActivePanelChanged += OnActivePanelChanged;
        }

        private void OnPanelStateChanged(object sender, EventArgs e)
        {
        }

        private void InitializePanelEventHandlers()
        {
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

        private void InitializeNavigationButtons()
        {
            BackButton.IsEnabled = _activePanelManager?.CanGoBack ?? false;
            ForwardButton.IsEnabled = _activePanelManager?.CanGoForward ?? false;
            UpButton.IsEnabled = true;
            RefreshButton.IsEnabled = true;

            UpdateFileOperationButtonsState();
        }
        #endregion

        #region Display Mode Management
        private void SetDisplayMode(DisplayMode mode)
        {
            SaveSplitterSizes();
            _currentDisplayMode = mode;

            UpdateLayoutVisibility(mode);
            InitializePanelViewsForCurrentMode();

            ApplyIconSizeToVisiblePanels();
            UpdateActivePanelVisualState();
            SaveAllPanelStates();

            RefreshActiveTileViewer();
        }

        private void UpdateLayoutVisibility(DisplayMode mode)
        {
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

            switch (mode)
            {
                case DisplayMode.Single: SingleViewGrid.Visibility = Visibility.Visible; break;
                case DisplayMode.Vertical: VerticalViewGrid.Visibility = Visibility.Visible; break;
                case DisplayMode.Horizontal: HorizontalViewGrid.Visibility = Visibility.Visible; break;
                case DisplayMode.TripleVertical: TripleVerticalViewGrid.Visibility = Visibility.Visible; break;
                case DisplayMode.TripleHorizontal: TripleHorizontalViewGrid.Visibility = Visibility.Visible; break;
                case DisplayMode.TripleTopBottom: TripleTopBottomViewGrid.Visibility = Visibility.Visible; break;
                case DisplayMode.TripleBottomTop: TripleBottomTopViewGrid.Visibility = Visibility.Visible; break;
                case DisplayMode.TripleLeftRight: TripleLeftRightViewGrid.Visibility = Visibility.Visible; break;
                case DisplayMode.TripleRightLeft: TripleRightLeftViewGrid.Visibility = Visibility.Visible; break;
                case DisplayMode.Quad: QuadViewGrid.Visibility = Visibility.Visible; break;
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
                    SetViewMode(panel.State.ViewMode, control, panel);
                }
            }

            if (!IsPanelVisibleInCurrentMode(_activePanelIndex))
            {
                _activePanelIndex = panelControls.Keys.First();
                SetActivePanel(_activePanelIndex);
            }

            RefreshActiveTileViewer();
        }

        private bool IsPanelVisibleInCurrentMode(PanelIndex panelIndex)
        {
            return _displayPanelControls[_currentDisplayMode].ContainsKey(panelIndex);
        }
        #endregion

        #region Active Panel Management
        private void SetActivePanel(PanelIndex panelIndex)
        {
            if (!IsPanelVisibleInCurrentMode(panelIndex)) return;

            _activePanelIndex = panelIndex;
            _activePanelManager = _panels[(int)panelIndex];

            _panelRegistry.SetActivePanel(_panelIdMap[panelIndex]);

            SaveActivePanelPreference();
            UpdateActivePanelVisualState();
            UpdateRadioButtonsForActivePanel();
            UpdateNavigationButtons();

            UpdateBreadcrumbBar(_activePanelManager.CurrentPath);
            _treePanel?.SelectPath(_activePanelManager.CurrentPath);

            RefreshActiveTileViewer();
        }

        private void SetActivePanel(ContentControl panel)
        {
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

        #region Display Mode Handlers
        private void SingleView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.Single); UpdateViewMenuSelection(); }
        private void VerticalView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.Vertical); UpdateViewMenuSelection(); }
        private void HorizontalView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.Horizontal); UpdateViewMenuSelection(); }
        private void TripleVerticalView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.TripleVertical); UpdateViewMenuSelection(); }
        private void TripleHorizontalView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.TripleHorizontal); UpdateViewMenuSelection(); }
        private void TripleTopBottomView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.TripleTopBottom); UpdateViewMenuSelection(); }
        private void TripleBottomTopView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.TripleBottomTop); UpdateViewMenuSelection(); }
        private void TripleLeftRightView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.TripleLeftRight); UpdateViewMenuSelection(); }
        private void TripleRightLeftView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.TripleRightLeft); UpdateViewMenuSelection(); }
        private void QuadView_Click(object sender, RoutedEventArgs e) { SetDisplayMode(DisplayMode.Quad); UpdateViewMenuSelection(); }

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

        #region Settings Application
        private void ApplyIconSizeToVisiblePanels()
        {
            var panelControls = _displayPanelControls[_currentDisplayMode];
            foreach (var kvp in panelControls)
            {
                var control = kvp.Value;
                var panel = _panels[(int)kvp.Key];
                if (control != null) ApplyIconSizeToPanel(control, panel.State.IconSize);
            }
        }

        private void ApplyIconSizeToPanel(ContentControl panel, string iconSize)
        {
            if (panel.Content is ISupportsIconSize sizeSupport)
            {
                // Если размер иконок по какой-то причине null, используем значение по умолчанию
                sizeSupport.SetIconSize(iconSize ?? "Medium");
            }
        }

        private void ApplyIconSizeToActivePanelOnly()
        {
            if (_activePanelManager == null) return;
            var activeControl = GetActivePanelControl();
            if (activeControl?.Content is ISupportsIconSize sizeSupport)
                sizeSupport.SetIconSize(_activePanelManager.State.IconSize);
        }
        #endregion

        #region State Loading
        private void LoadAllPanelStates()
        {
            try
            {
                _currentDisplayMode = Enum.TryParse<DisplayMode>(App.SettingsManager?.GetSetting<string>("CurrentDisplayMode") ?? "Single", out var m) ? m : DisplayMode.Single;
                _previewPanelMode = Enum.TryParse<PreviewPanelMode>(App.SettingsManager?.GetSetting<string>("PreviewPanelMode") ?? "None", out var pm) ? pm : PreviewPanelMode.None;
                _activePanelIndex = App.SettingsManager?.GetSetting<PanelIndex>("ActivePanelIndex") ?? PanelIndex.Panel0;
                for (int i = 0; i < 4; i++) _panels[i]?.LoadState();
                SetActivePanel(_activePanelIndex);
            }
            catch { ResetAllPanelStatesToDefault(); }
        }

        private void LoadPreviewPanelSizes()
        {
            _leftPreviewWidth = App.SettingsManager?.GetSetting<double>("LeftPreviewWidth", 300) ?? 300;
            _rightPreviewWidth = App.SettingsManager?.GetSetting<double>("RightPreviewWidth", 300) ?? 300;
            ColumPreviewLeft.MinWidth = 0; ColumPreviewRigth.MinWidth = 0;
        }

        private void ResetAllPanelStatesToDefault()
        {
            _currentDisplayMode = DisplayMode.Single; _previewPanelMode = PreviewPanelMode.None;
            for (int i = 0; i < 4; i++)
                _panels[i]?.LoadState(new PanelState { IconSize = "Icons Medium", ViewMode = ViewMode.Icons, CurrentPath = "MyComputer" });
            SetActivePanel(PanelIndex.Panel0); _prefsize = "Icons";
        }
        #endregion

        #region State Saving
        private void SaveAllPanelStates()
        {
            App.SettingsManager?.SaveSetting("CurrentDisplayMode", _currentDisplayMode.ToString());
            App.SettingsManager?.SaveSetting("PreviewPanelMode", _previewPanelMode.ToString());
            App.SettingsManager?.SaveSetting("ActivePanelIndex", _activePanelIndex);
        }

        private void SavePreviewPanelSizes()
        {
            if (ColumPreviewLeft.ActualWidth > 0) _leftPreviewWidth = ColumPreviewLeft.ActualWidth;
            if (ColumPreviewRigth.ActualWidth > 0) _rightPreviewWidth = ColumPreviewRigth.ActualWidth;
            App.SettingsManager?.SaveSetting("LeftPreviewWidth", _leftPreviewWidth);
            App.SettingsManager?.SaveSetting("RightPreviewWidth", _rightPreviewWidth);
        }

        private void SaveSplitterSizes() => _splitterManager.SaveAllSplitterSizes(this);
        #endregion

        #region Splitter Management
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
                    if (value <= 0) App.SettingsManager.SaveSetting(key, 0);
                }
            }
            catch { }
        }

        private void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e) => SaveSplitterSizes();
        #endregion

        #region Preview Panel Management
        private void PreviewSplitter_PointerReleased(object sender, PointerRoutedEventArgs e) => SavePreviewPanelSizes();
        private void PreviewSplitter_PointerCaptureLost(object sender, PointerRoutedEventArgs e) => SavePreviewPanelSizes();

        private void UpdatePreviewPanelVisibility()
        {
            if (PreviewPanelLeft == null || PreviewPanelRight == null || ColumPreviewLeft == null || ColumPreviewRigth == null || LeftPreviewSplitter == null || RightPreviewSplitter == null) return;

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
            InvalidateArrange(); UpdateLayout();
        }

        private void UpdatePreviewRadioButtons()
        {
            PreviewLeftRadio.Click -= PreviewLeft_Click;
            PreviewRightRadio.Click -= PreviewRight_Click;
            PreviewNoneRadio.Click -= PreviewNone_Click;
            try
            {
                PreviewLeftRadio.IsChecked = false; PreviewRightRadio.IsChecked = false; PreviewNoneRadio.IsChecked = false;
                switch (_previewPanelMode)
                {
                    case PreviewPanelMode.Left: PreviewLeftRadio.IsChecked = true; break;
                    case PreviewPanelMode.Right: PreviewRightRadio.IsChecked = true; break;
                    default: PreviewNoneRadio.IsChecked = true; break;
                }
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
            ResetPanelLayout();                     // ← добавлено
            SaveAllPanelStates();
        }

        private void PreviewRight_Click(object sender, RoutedEventArgs e)
        {
            SavePreviewPanelSizes();
            _previewPanelMode = PreviewPanelMode.Right;
            UpdatePreviewPanelVisibility();
            ResetPanelLayout();                     // ← добавлено
            SaveAllPanelStates();
        }
        private void PreviewNone_Click(object sender, RoutedEventArgs e)
        {
            SavePreviewPanelSizes();
            _previewPanelMode = PreviewPanelMode.None;
            UpdatePreviewPanelVisibility();
            ResetPanelLayout();                     // ← добавлено
            SaveAllPanelStates();
        }
        #endregion

        #region Radio Button Management
        private void UpdateRadioButtonsForActivePanel()
        {
            if (_activePanelManager == null) return;
            UnsubscribeFromRadioButtonEvents();
            try
            {
                UpdateViewModeRadioButtons(_activePanelManager.State.ViewMode);
                SetSelectedRadioButton(_activePanelManager.State.IconSize);
            }
            finally { SubscribeToRadioButtonEvents(); }
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
                case ViewMode.Icons: IconsModeRadioButton.IsChecked = true; break;
                case ViewMode.List: ListModeRadioButton.IsChecked = true; break;
                case ViewMode.CompactList: CompListModeRadioButton.IsChecked = true; break;
                case ViewMode.Tiles: TilesModeRadioButton.IsChecked = true; break;
                case ViewMode.Table: TableModeRadioButton.IsChecked = true; break;
            }
        }

        private void SetSelectedRadioButton(string fullSizeKey)
        {
            if (string.IsNullOrEmpty(fullSizeKey)) { MediumSizeRadioButton.IsChecked = true; return; }
            string sizePart = SizeHelper.ExtractSizePartFromFullKey(fullSizeKey).ToLower();
            TinySizeRadioButton.IsChecked = false; ExtraSmallSizeRadioButton.IsChecked = false;
            SmallSizeRadioButton.IsChecked = false; BelowMediumSizeRadioButton.IsChecked = false;
            MediumSizeRadioButton.IsChecked = false; AboveMediumSizeRadioButton.IsChecked = false;
            LargeSizeRadioButton.IsChecked = false; ExtraLargeSizeRadioButton.IsChecked = false;
            HugeSizeRadioButton.IsChecked = false;
            switch (sizePart)
            {
                case "Tiny": TinySizeRadioButton.IsChecked = true; break;
                case "Extra Small": ExtraSmallSizeRadioButton.IsChecked = true; break;
                case "Small": SmallSizeRadioButton.IsChecked = true; break;
                case "Below Medium": BelowMediumSizeRadioButton.IsChecked = true; break;
                case "Medium": MediumSizeRadioButton.IsChecked = true; break;
                case "Above Medium": AboveMediumSizeRadioButton.IsChecked = true; break;
                case "Large": LargeSizeRadioButton.IsChecked = true; break;
                case "Extra Large": ExtraLargeSizeRadioButton.IsChecked = true; break;
                case "Huge": HugeSizeRadioButton.IsChecked = true; break;
                default: MediumSizeRadioButton.IsChecked = true; break;
            }
        }

        private void SizeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingViewMode) return;
            if (sender is RadioButton rb && rb.Tag is string sizeTag && _activePanelManager != null && rb.IsChecked == true)
            {
                string fullSizeKey = $"{_prefsize} {sizeTag}";
                _activePanelManager.UpdateState(state => state.IconSize = fullSizeKey);
                ApplyIconSizeToActivePanelOnly();
                App.SettingsManager?.SaveSetting("SelectedSizeIconViewPage", fullSizeKey);
                SaveAllPanelStates();
            }
        }

        private void ViewModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingViewMode) return;
            try
            {
                _isUpdatingViewMode = true;
                if (sender is RadioButton rb && rb.Tag is string modeTag && _activePanelManager != null)
                {
                    ViewMode mode = (ViewMode)Enum.Parse(typeof(ViewMode), modeTag);
                    _activePanelManager.UpdateState(state =>
                    {
                        state.ViewMode = mode;
                        _prefsize = GetViewModePrefix(mode);
                        state.IconSize = $"{_prefsize} {SizeHelper.ExtractSizePartFromFullKey((state.IconSize))}";
                    });
                    SetViewMode(mode);
                    ApplyIconSizeToActivePanelOnly();
                    SaveAllPanelStates();
                }
            }
            finally { _isUpdatingViewMode = false; }
        }

        private string GetViewModePrefix(ViewMode viewMode) => viewMode switch
        {
            ViewMode.Icons => "Icons",
            ViewMode.List => "List",
            ViewMode.CompactList => "CompactList",
            ViewMode.Tiles => "Tiles",
            ViewMode.Table => "Table",
            _ => "Icons"
        };
        #endregion

        #region Visual Methods
        private void ShowHoverEffect(ContentControl panel)
        {
            var accentBrush = Application.Current.Resources["AccentBorderBrush"] as SolidColorBrush;
            if (panel.BorderBrush != accentBrush) panel.BorderBrush = new SolidColorBrush(Colors.LightGray);
        }

        private void HideHoverEffect(ContentControl panel)
        {
            var accentBrush = Application.Current.Resources["AccentBorderBrush"] as SolidColorBrush;
            if (panel.BorderBrush != accentBrush) panel.BorderBrush = Application.Current.Resources["TransparentBrush"] as SolidColorBrush;
        }

        private void UpdateActivePanelVisualState()
        {
            ResetAllPanelBorders();
            var accentBrush = (SolidColorBrush)Application.Current.Resources["AccentBorderBrush"];
            var activeControl = GetActivePanelControl();
            if (activeControl != null) activeControl.BorderBrush = accentBrush;
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
            foreach (var panel in allPanels) panel.BorderBrush = transparentBrush;
        }
        #endregion

        #region Event Handlers
        private async void ViewPage_Loaded(object sender, RoutedEventArgs e)
        {
            //Файловые операции
            _fileOperationService = App.FileOperationService;

            LoadAllPanelStates();

            string savedIconSize = App.SettingsManager?.GetSetting<string>("SelectedSizeIconViewPage");
            if (!string.IsNullOrEmpty(savedIconSize) && _activePanelManager != null)
            {
                _prefsize = GetViewModePrefix(_activePanelManager.State.ViewMode);
                _activePanelManager.UpdateState(state => state.IconSize = savedIconSize);
            }

            switch (_currentDisplayMode)
            {
                case DisplayMode.Single: SingleView_Click(null, null); break;
                case DisplayMode.Vertical: VerticalView_Click(null, null); break;
                case DisplayMode.Horizontal: HorizontalView_Click(null, null); break;
                case DisplayMode.TripleVertical: TripleVerticalView_Click(null, null); break;
                case DisplayMode.TripleHorizontal: TripleHorizontalView_Click(null, null); break;
                case DisplayMode.TripleTopBottom: TripleTopBottomView_Click(null, null); break;
                case DisplayMode.TripleBottomTop: TripleBottomTopView_Click(null, null); break;
                case DisplayMode.TripleLeftRight: TripleLeftRightView_Click(null, null); break;
                case DisplayMode.TripleRightLeft: TripleRightLeftView_Click(null, null); break;
                case DisplayMode.Quad: QuadView_Click(null, null); break;
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
            _navigationManager.NavigationChanged += OnLocalNavigationChanged;
            _treePanel = TreePanelPg01.Instance;
            if (_treePanel != null) _treePanel.NavigateRequested += OnTreeNavigateRequested;

            UpdateBreadcrumbBar(_activePanelManager?.CurrentPath ?? "MyComputer");

            RefreshActiveTileViewer();
            EnsureDeleteConfirmationDialog();
        }

        private void ViewPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_treePanel != null) _treePanel.NavigateRequested -= OnTreeNavigateRequested;
            _panelRegistry.ActivePanelChanged -= OnActivePanelChanged;
            _navigationManager.NavigationChanged -= OnLocalNavigationChanged;

            if (_activeTileViewer != null)
            {
                _activeTileViewer.SelectionStateChanged -= OnTileViewerSelectionChanged;
                _activeTileViewer.ClipboardChanged -= OnTileViewerClipboardChanged;
                _activeTileViewer.DeleteRequested -= OnDeleteRequested;
            }
        }

        private void OnTreeNavigateRequested(object sender, string path)
        {
            if (path == "MyComputer")
            {
                _activePanelManager?.NavigateTo("Drives");
                return;
            }
            if (path == "Drives")
            {
                UpdateBreadcrumbBar(path);
                return;
            }
            if (path == "SpecialFolders")
            {
                _activePanelManager?.NavigateTo("SpecialFolders");
                return;
            }
            _activePanelManager?.NavigateTo(path);
        }

        private void OnLocalNavigationChanged(object sender, NavigationEventArgs e)
        {
            if (e.PanelId == _activePanelManager?.PanelId)
            {
                UpdateBreadcrumbBar(e.Path);
                _treePanel?.SelectPath(e.Path);
            }
        }

        private void OnActivePanelChanged(object sender, PanelManager panel)
        {
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
            if (panel != null)
            {
                UpdateBreadcrumbBar(panel.CurrentPath);
                _treePanel?.SelectPath(panel.CurrentPath);
            }

            RefreshActiveTileViewer();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activePanelManager != null && _activePanelManager.CanGoBack)
            {
                _activePanelManager.GoBack();
                UpdateNavigationButtons();
                UpdateBreadcrumbBar(_activePanelManager.CurrentPath);
                _treePanel?.NavigateToPath(_activePanelManager.CurrentPath);
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activePanelManager != null && _activePanelManager.CanGoForward)
            {
                _activePanelManager.GoForward();
                UpdateNavigationButtons();
                UpdateBreadcrumbBar(_activePanelManager.CurrentPath);
                _treePanel?.NavigateToPath(_activePanelManager.CurrentPath);
            }
        }

        private void CustomBreadcrumb_ItemClicked(object sender, CustomBreadcrumbItem item)
        {
            if (item?.FullPath != null)
            {
                _activePanelManager?.NavigateTo(item.FullPath);
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string currentPath = _activePanelManager?.CurrentPath;
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    var parentDir = Directory.GetParent(currentPath);
                    if (parentDir != null) _activePanelManager?.NavigateTo(parentDir.FullName);
                }
                else if (currentPath == "Drives") _activePanelManager?.NavigateTo("MyComputer");
            }
            catch { }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshActivePanel();

        private void RefreshActivePanel()
        {
            if (_activePanelManager != null && GetActivePanelControl()?.Content is IRefreshablePanel refreshablePanel)
                refreshablePanel.RefreshNavigation();
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (_activePanelManager != null) _activePanelManager.UpdateState(state => state.SearchFilter = args.QueryText);
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e) => _activeTileViewer?.CopySelectedItems();
        private void CutButton_Click(object sender, RoutedEventArgs e) => _activeTileViewer?.CutSelectedItems();

        private async void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTileViewer != null)
                await _activeTileViewer.PasteItemsAsync();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e) => OnDeleteRequested(this, EventArgs.Empty);

        private async void OnDeleteRequested(object sender, EventArgs e)
        {
            if (_isDeleteDialogOpen) return;
            _isDeleteDialogOpen = true;
            try
            {
                if (_activeTileViewer != null)
                    await _activeTileViewer.DeleteSelectedItemsAsync();
            }
            finally
            {
                _isDeleteDialogOpen = false;
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e) => _activeTileViewer?.RenameSelectedItem();

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Move clicked – not implemented");
        }
        #endregion

        #region View Mode Management
        private void SetViewMode(ViewMode mode, ContentControl targetControl = null, PanelManager targetManager = null)
        {
            PanelManager managerToUse = targetManager ?? _activePanelManager;
            ContentControl controlToUse = targetControl ?? GetActivePanelControl();
            if (managerToUse == null || controlToUse == null) return;

            managerToUse.UpdateState(state => state.ViewMode = mode);
            _prefsize = GetViewModePrefix(mode);
            UnsubscribeFromTileViewEvents(controlToUse);

            UserControl newView = null;
            switch (mode)
            {
                case ViewMode.Icons:
                    newView = new TileViewerContent
                    {
                        PanelId = managerToUse.PanelId,
                        DisplayMode = "Vertical"
                    };
                    break;
                case ViewMode.List:
                    newView = new TileViewerContent
                    {
                        PanelId = managerToUse.PanelId,
                        DisplayMode = "Horizontal"
                    };
                    break;
            }

            if (newView != null)
            {
                newView.DataContext = managerToUse.State.DataContext ?? this.DataContext;

                if (newView is TileViewerContent tileViewer)
                {
                    tileViewer.NavigationChanged += TileView_NavigationChanged;
                    tileViewer.SetFileOperationService(_fileOperationService);
                    controlToUse.Content = newView;
                    tileViewer.SetPanelManager(managerToUse);
                }
                else
                {
                    controlToUse.Content = newView;
                }
            }

            ApplyIconSizeToVisiblePanels();
            SaveAllPanelStates();

            RefreshActiveTileViewer();
        }

        public void ResetPanelLayout()
        {
            // Очищаем все сохранённые размеры сплиттеров
            _splitterManager.ClearAllSavedSizes();
            // Перезапускаем текущий режим, чтобы панели перераспределились равномерно
            SetDisplayMode(_currentDisplayMode);
        }

        private void TileView_NavigationChanged(object sender, EventArgs e)
        {
            UpdateNavigationButtons();
            SaveAllPanelStates();
            if (_activePanelManager != null)
            {
                UpdateBreadcrumbBar(_activePanelManager.CurrentPath);
                _treePanel?.NavigateToPath(_activePanelManager.CurrentPath);
            }
        }

        private void UnsubscribeFromTileViewEvents(ContentControl control)
        {
            if (control?.Content is TileViewerContent tileViewerContent)
                tileViewerContent.NavigationChanged -= TileView_NavigationChanged;
        }
        #endregion

        #region Navigation
        private void SetFocusToActivePanel() => GetActivePanelControl()?.Focus(FocusState.Programmatic);

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

        #region Breadcrumbs
        private async void UpdateBreadcrumbBar(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var items = new ObservableCollection<CustomBreadcrumbItem>();
            BuildBreadcrumbItems(path, items);
            await LoadIconsForBreadcrumbAsync(items);

            CustomBreadcrumb.ItemsSource = items;

            _ = LoadBreadcrumbChildrenAsync(items);
        }

        private void BuildBreadcrumbItems(string path, ObservableCollection<CustomBreadcrumbItem> items)
        {
            if (path == "MyComputer" || path == "SpecialFolders" || path == "Drives")
            {
                if (path == "Drives")
                {
                    items.Add(new CustomBreadcrumbItem
                    {
                        Text = _fileSystemService.GetDisplayName("MyComputer"),
                        FullPath = "MyComputer"
                    });
                    items.Add(new CustomBreadcrumbItem
                    {
                        Text = _fileSystemService.GetDisplayName("Drives"),
                        FullPath = "Drives"
                    });
                }
                else
                {
                    items.Add(new CustomBreadcrumbItem
                    {
                        Text = _fileSystemService.GetDisplayName(path),
                        FullPath = path
                    });
                }
            }
            else if (Directory.Exists(path) || (path.Length == 3 && path.EndsWith(":\\")))
            {
                items.Add(new CustomBreadcrumbItem
                {
                    Text = _fileSystemService.GetDisplayName("MyComputer"),
                    FullPath = "MyComputer"
                });
                items.Add(new CustomBreadcrumbItem
                {
                    Text = _fileSystemService.GetDisplayName("Drives"),
                    FullPath = "Drives"
                });

                var parts = new List<string>();
                string currentPath = path;
                while (!string.IsNullOrEmpty(currentPath))
                {
                    parts.Insert(0, currentPath);
                    var parent = Path.GetDirectoryName(currentPath);
                    if (string.IsNullOrEmpty(parent) || parent == currentPath)
                        break;
                    currentPath = parent;
                }

                foreach (var part in parts)
                {
                    string displayName = _fileSystemService.GetDisplayName(part);
                    if (items.All(i => i.FullPath != part))
                    {
                        items.Add(new CustomBreadcrumbItem
                        {
                            Text = displayName,
                            FullPath = part
                        });
                    }
                }
            }
            else
            {
                items.Add(new CustomBreadcrumbItem
                {
                    Text = _fileSystemService.GetDisplayName(path),
                    FullPath = path
                });
            }
        }

        private async Task LoadIconsForBreadcrumbAsync(ObservableCollection<CustomBreadcrumbItem> items)
        {
            foreach (var item in items)
            {
                try
                {
                    if (item.FullPath == "MyComputer")
                    {
                        item.Icon = new BitmapImage(new Uri("ms-appx:///Assets/home.png"));
                    }
                    else if (item.FullPath == "Drives")
                    {
                        item.Icon = new BitmapImage(new Uri("ms-appx:///Assets/computer.png"));
                    }
                    else if (item.FullPath == "SpecialFolders")
                    {
                        item.Icon = new BitmapImage(new Uri("ms-appx:///Assets/home.png"));
                    }
                    else if (item.FullPath.Length == 3 && item.FullPath.EndsWith(":\\"))
                    {
                        var icon = await _fileSystemService.GetDriveIconAsync(item.FullPath);
                        item.Icon = icon ?? new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"));
                    }
                    else if (Directory.Exists(item.FullPath))
                    {
                        var icon = await _fileSystemService.GetFolderIconAsync(item.FullPath);
                        item.Icon = icon ?? new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));
                    }
                    else
                    {
                        item.Icon = new BitmapImage(new Uri("ms-appx:///Assets/file.png"));
                    }
                }
                catch
                {
                    item.Icon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));
                }
            }
        }

        private async Task LoadBreadcrumbChildrenAsync(ObservableCollection<CustomBreadcrumbItem> items)
        {
            for (int i = 0; i < items.Count - 1; i++)
            {
                var item = items[i];
                string targetPath = item.FullPath;
                if (string.IsNullOrEmpty(targetPath)) continue;

                try
                {
                    List<ExplorerItemViewModel> children = null;
                    if (targetPath == "MyComputer")
                    {
                        children = await _fileSystemService.LoadHomePageAsync();
                    }
                    else if (targetPath == "Drives")
                    {
                        var history = new DirectoryHistory("Drives", "Мой компьютер");
                        children = await _fileSystemService.LoadDrivesAsync(history);
                    }
                    else if (Directory.Exists(targetPath))
                    {
                        var history = new DirectoryHistory(targetPath, targetPath);
                        children = await _fileSystemService.LoadFoldersOnlyAsync(targetPath, history);
                    }

                    if (children != null)
                    {
                        var childItems = new ObservableCollection<CustomBreadcrumbItem>();
                        foreach (var child in children)
                        {
                            childItems.Add(new CustomBreadcrumbItem
                            {
                                Text = child.Name,
                                FullPath = child.FilePath,
                                Icon = child.ImageSource
                            });
                        }
                        item.Children = childItems;
                    }
                }
                catch { }
            }
        }
        #endregion

        #region TileViewer selection and file operation buttons state
        private void RefreshActiveTileViewer()
        {
            if (_activeTileViewer != null)
            {
                _activeTileViewer.SelectionStateChanged -= OnTileViewerSelectionChanged;
                _activeTileViewer.ClipboardChanged -= OnTileViewerClipboardChanged;
                _activeTileViewer.DeleteRequested -= OnDeleteRequested;
            }

            var activeControl = GetActivePanelControl();
            _activeTileViewer = activeControl?.Content as TileViewerContent;

            if (_activeTileViewer != null)
            {
                _activeTileViewer.SelectionStateChanged += OnTileViewerSelectionChanged;
                _activeTileViewer.ClipboardChanged += OnTileViewerClipboardChanged;
                _activeTileViewer.DeleteRequested += OnDeleteRequested;
            }

            UpdateFileOperationButtonsState();
        }

        private void OnTileViewerSelectionChanged(object sender, bool hasSelection)
        {
            RenameButton.IsEnabled = hasSelection;
            UpdateFileOperationButtonsState();
        }

        private void OnTileViewerClipboardChanged(object sender, EventArgs e)
        {
            UpdateFileOperationButtonsState();
        }

        private void UpdateFileOperationButtonsState()
        {
            bool hasSelection = _activeTileViewer?.HasSelection ?? false;
            CopyButton.IsEnabled = hasSelection;
            CutButton.IsEnabled = hasSelection;
            DeleteButton.IsEnabled = hasSelection;
            MoveButton.IsEnabled = hasSelection;

            PasteButton.IsEnabled = _activeTileViewer?.CanPaste ?? false;
        }
        #endregion

        #region Delete confirmation dialog
        private void EnsureDeleteConfirmationDialog()
        {
            if (_deleteConfirmationDialog == null)
            {
                if (this.Resources.TryGetValue("DeleteConfirmationDialog", out var res) && res is ContentDialog dlg)
                    _deleteConfirmationDialog = dlg;
                else
                    _deleteConfirmationDialog = new ContentDialog
                    {
                        Title = "Подтверждение удаления",
                        Content = "Вы уверены, что хотите удалить выбранные элементы?",
                        PrimaryButtonText = "Удалить",
                        SecondaryButtonText = "Отмена",
                        DefaultButton = ContentDialogButton.Secondary
                    };
            }
        }
        #endregion
    }
}