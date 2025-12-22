using System.Collections.ObjectModel;
using System.Linq;

namespace Core_FileManagement
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<ExplorerItemViewModel> ExplorerItems { get; } = new ObservableCollection<ExplorerItemViewModel>();
        private ExplorerItemViewModel _currentExplorerItem;

        public ExplorerItemViewModel CurrentExplorerItem
        {
            get => _currentExplorerItem;
            set
            {
                if (_currentExplorerItem != value)
                {
                    _currentExplorerItem = value;
                    OnPropertyChanged();
                }
            }
        }

        public DelegateCommand AddTabItemCommand { get; }
        public DelegateCommand CloseCommand { get; }

        public MainViewModel()
        {
            AddTabItemCommand = new DelegateCommand(OnAddTabItem);
            CloseCommand = new DelegateCommand(OnClose);
            AddTabItemViewModel();
        }

        private void OnAddTabItem(object obj)
        {
            AddTabItemViewModel();
        }

        private void OnClose(object obj)
        {
            if (obj is ExplorerItemViewModel explorerItemViewModel)
            {
                CloseTab(explorerItemViewModel);
            }
        }

        private void AddTabItemViewModel()
        {
            var history = new DirectoryHistory("Мой Компьютер", "Мой Компьютер");
            var vm = new ExplorerItemViewModel(history);
            ExplorerItems.Add(vm);
            CurrentExplorerItem = vm;
        }

        private void CloseTab(ExplorerItemViewModel explorerItemViewModel)
        {
            explorerItemViewModel.Dispose();
            ExplorerItems.Remove(explorerItemViewModel);
            CurrentExplorerItem = ExplorerItems.LastOrDefault();
        }
    }
}