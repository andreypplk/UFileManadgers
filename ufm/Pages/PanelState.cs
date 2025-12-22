using System.Collections.Generic;

namespace ufm
{
    public class PanelState
    {
        // Основные настройки отображения
        public ViewPage.ViewMode ViewMode { get; set; } = ViewPage.ViewMode.Icons;
        public string IconSize { get; set; } = "Icons Medium";

        // Состояние представления
        public SortDescription CurrentSort { get; set; }
        public bool IsAscendingSort { get; set; } = true;
        public List<string> VisibleColumns { get; set; } = new List<string>();
        public double ScrollPosition { get; set; } = 0;

        // Выделение элементов
        public List<string> SelectedItems { get; set; } = new List<string>();
        public string FocusedItem { get; set; } = string.Empty;

        // Фильтры и поиск
        public string SearchFilter { get; set; } = string.Empty;
        public string FileTypeFilter { get; set; } = "All";

        // Внешний вид
        public bool ShowHiddenFiles { get; set; } = false;
        public bool ShowFileExtensions { get; set; } = true;
        public double ColumnWidth { get; set; } = 200;

        // Размеры для сплиттеров
        public Dictionary<string, double> SplitterSizes { get; set; } = new Dictionary<string, double>();

        // Данные
        public object DataContext { get; set; }

        // Навигационная история
        public List<string> NavigationHistory { get; set; } = new List<string>();
        public int HistoryIndex { get; set; } = -1;
        public string CurrentPath { get; set; } = "MyComputer";

        // Флаги состояния
        public bool IsActive { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool SynchronizeWithOtherPanels { get; set; } = true;

        public PanelState Clone()
        {
            return new PanelState
            {
                ViewMode = this.ViewMode,
                IconSize = this.IconSize,
                CurrentSort = this.CurrentSort,
                IsAscendingSort = this.IsAscendingSort,
                VisibleColumns = new List<string>(this.VisibleColumns),
                ScrollPosition = this.ScrollPosition,
                SelectedItems = new List<string>(this.SelectedItems),
                FocusedItem = this.FocusedItem,
                SearchFilter = this.SearchFilter,
                FileTypeFilter = this.FileTypeFilter,
                ShowHiddenFiles = this.ShowHiddenFiles,
                ShowFileExtensions = this.ShowFileExtensions,
                ColumnWidth = this.ColumnWidth,
                SplitterSizes = new Dictionary<string, double>(this.SplitterSizes),
                NavigationHistory = new List<string>(this.NavigationHistory),
                HistoryIndex = this.HistoryIndex,
                CurrentPath = this.CurrentPath,
                IsActive = this.IsActive,
                IsVisible = this.IsVisible,
                SynchronizeWithOtherPanels = this.SynchronizeWithOtherPanels
            };
        }
    }
}