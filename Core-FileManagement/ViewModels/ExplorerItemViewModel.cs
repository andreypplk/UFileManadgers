//using System;
//using System.Collections.ObjectModel;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.UI.Dispatching;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System.Collections.Generic;
//using PropertyChanged;

//namespace Core_FileManagement
//{
//    public class ExplorerItemViewModel : BaseViewModel, IDisposable
//    {
//        #region Поля и константы

//        private readonly IDirectoryHistory _history;
//        private static readonly Lazy<ObservableCollection<FileEntityViewModel>> _cachedDrives =
//            new Lazy<ObservableCollection<FileEntityViewModel>>(LoadDrives);
//        private bool _disposed = false;
//        private CancellationTokenSource _loadingCancellation;
//        private DispatcherQueue _dispatcherQueue;
//        private DispatcherQueueController _dispatcherQueueController;

//        // Оптимизации для производительности
//        private readonly BatchObservableCollection<FileEntityViewModel> _directoriesAndFiles;
//        //01 12 2025
//        //private readonly object _loadingLock = new object();
//        private readonly SemaphoreSlim _loadingSemaphore = new SemaphoreSlim(1, 1);
//        private bool _isLoadingInProgress = false;

//        //04 11 2025
//        public bool HasChildren { get; set; }
//        //25 11 2025
//        public bool IsTreeViewNode { get; set; }
//        // 29 11 2025
//        public bool IsSpecialFolderNode { get; set; }
//        #endregion

//        #region Свойства навигации

//        public string FilePath { get; set; }
//        public string Name { get; set; }
//        public ObservableCollection<FileEntityViewModel> DirectoriesAndFiles => _directoriesAndFiles;
//        public FileEntityViewModel SelectedFileEntity { get; set; }
//        public BitmapImage ImageSource { get; set; }
//        public bool IsMyComputer => FilePath == "Мой Компьютер" || Name == "Мой Компьютер";

//        #endregion

//        #region Свойства дискового пространства

//        public string UsedSpaceString { get; set; }
//        public string FreeSpaceString { get; set; }
//        public string TotalSizeString { get; set; }

//        private int _usedProcentValue;
//        public int UsedProcentValue
//        {
//            get => _usedProcentValue;
//            set
//            {
//                if (_usedProcentValue != value)
//                {
//                    _usedProcentValue = value;
//                    OnPropertyChanged();
//                    OnPropertyChanged(nameof(IsCritical));
//                }
//            }
//        }
//        public bool IsCritical => UsedProcentValue >= 80;

//        #endregion

//        #region Свойства производительности

//        public PerformanceMetrics CurrentMetrics { get; private set; } = new();

//        public float CpuUsagePercent { get; private set; }
//        public double MemoryUsagePercent { get; private set; }

//        private string _cpuUsageText = "0%";
//        public string CpuUsageText
//        {
//            get => _cpuUsageText;
//            set { if (_cpuUsageText != value) { _cpuUsageText = value; OnPropertyChanged(); } }
//        }

//        private string _ioUsageText = "0 MB/s";
//        public string IoUsageText
//        {
//            get => _ioUsageText;
//            set { if (_ioUsageText != value) { _ioUsageText = value; OnPropertyChanged(); } }
//        }

//        private string _memoryUsageText = "0 MB";
//        public string MemoryUsageText
//        {
//            get => _memoryUsageText;
//            set { if (_memoryUsageText != value) { _memoryUsageText = value; OnPropertyChanged(); } }
//        }

//        #endregion

//        #region Состояние UI

//        private bool _isProgressBarVisible;
//        public bool IsProgressBarVisible
//        {
//            get => _isProgressBarVisible;
//            set { if (_isProgressBarVisible != value) { _isProgressBarVisible = value; OnPropertyChanged(); } }
//        }

//        private bool _isLoading;
//        public bool IsLoading
//        {
//            get => _isLoading;
//            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); } }
//        }

//        #endregion

//        #region Команды

//        public DelegateCommand OpenCommand { get; private set; }
//        public DelegateCommand MoveBackCommand { get; private set; }
//        public DelegateCommand MoveForwardCommand { get; private set; }
//        public DelegateCommand RefreshCommand { get; private set; }
//        public DelegateCommand CancelLoadingCommand { get; private set; }

//        #endregion

//        #region Конструктор и инициализация

//        public ExplorerItemViewModel(IDirectoryHistory history)
//        {
//            _history = history ?? throw new ArgumentNullException(nameof(history));

//            // Инициализация DispatcherQueue
//            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
//            if (_dispatcherQueue == null)
//            {
//                _dispatcherQueueController = DispatcherQueueController.CreateOnCurrentThread();
//                _dispatcherQueue = _dispatcherQueueController.DispatcherQueue;
//                Debug.WriteLine("Создан новый DispatcherQueueController");
//            }

//            // Инициализация оптимизированных коллекций
//            _directoriesAndFiles = new BatchObservableCollection<FileEntityViewModel>();

//            InitializeCommands();
//            InitializeMetrics();
//            UpdateFromHistory();

//            SubscribeToEvents();
//            _ = InitializePerformanceManagerAsync();
//        }

//        private void InitializeCommands()
//        {
//            OpenCommand = new DelegateCommand(Open);
//            MoveBackCommand = new DelegateCommand(OnMoveBack, OnCanMoveBack);
//            MoveForwardCommand = new DelegateCommand(OnMoveForward, OnCanMoveForward);
//            RefreshCommand = new DelegateCommand(OnRefresh);
//            CancelLoadingCommand = new DelegateCommand(OnCancelLoading, CanCancelLoading);
//        }

//        private void InitializeMetrics()
//        {
//            CurrentMetrics = new PerformanceMetrics
//            {
//                CpuUsage = 0,
//                IoUsage = 0,
//                MemoryUsage = 0,
//                MemoryUsagePercent = 0,
//                TotalMemory = 16L * 1024 * 1024 * 1024
//            };
//            UpdateStatusBar();
//        }

//        private void SubscribeToEvents()
//        {
//            _history.HistoryChanged += OnHistoryChanged;
//            DriveService.DrivesUpdated += OnDrivesUpdated;
//            FileCacheService.CacheUpdated += OnFileCacheUpdated;
//        }

//        #endregion

//        #region Методы работы с производительностью

//        private async Task InitializePerformanceManagerAsync()
//        {
//            try
//            {
//                bool initialized = await PerformanceManager.InitializeAsync();
//                if (initialized)
//                {
//                    CurrentMetrics.TotalMemory = PerformanceManager.GetTotalSystemMemory();
//                    PerformanceManager.MetricsUpdated += OnPerformanceMetricsUpdated;
//                    PerformanceManager.StartMonitoring();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка инициализации PerformanceManager: {ex}");
//            }
//        }

//        private void OnPerformanceMetricsUpdated(object sender, PerformanceMetrics metrics)
//        {
//            _dispatcherQueue?.TryEnqueue(() =>
//            {
//                if (_disposed) return;

//                CurrentMetrics = metrics ?? new PerformanceMetrics();
//                UpdateStatusBar();
//            });
//        }

//        private void UpdateStatusBar()
//        {
//            if (CurrentMetrics == null) return;

//            CpuUsageText = $"{CurrentMetrics.CpuUsage:0}%";
//            IoUsageText = $"{CurrentMetrics.IoUsage:0.0} MB/s";
//            MemoryUsageText = $"{CurrentMetrics.MemoryUsage / (1024 * 1024):0} MB / {CurrentMetrics.TotalMemory / (1024 * 1024):0} MB";

//            CpuUsagePercent = CurrentMetrics.CpuUsage;
//            MemoryUsagePercent = CurrentMetrics.MemoryUsagePercent;

//            OnPropertiesChanged(nameof(CpuUsageText), nameof(IoUsageText), nameof(MemoryUsageText), nameof(CpuUsagePercent), nameof(MemoryUsagePercent));
//        }

//        #endregion

//        #region Методы работы с файловой системой

//        public async Task OpenDirectoryAsync()
//        {
//            //// Защита от множественных одновременных вызовов
//            //lock (_loadingLock)
//            //{
//            //    if (_isLoadingInProgress)
//            //    {
//            //        Debug.WriteLine("OpenDirectoryAsync already in progress, skipping");
//            //        return;
//            //    }
//            //    _isLoadingInProgress = true;
//            //}

//            //try
//            //{
//            //    IsLoading = true;
//            //    _directoriesAndFiles.Clear();

//            //    if (IsMyComputer)
//            //    {
//            //        LoadCachedDrives();
//            //    }
//            //    else
//            //    {
//            //        await LoadDirectoryContentAsync();
//            //    }
//            //}
//            //catch (Exception ex)
//            //{
//            //    Debug.WriteLine($"Ошибка открытия директории: {ex.Message}");
//            //}
//            //finally
//            //{
//            //    IsLoading = false;
//            //    lock (_loadingLock)
//            //    {
//            //        _isLoadingInProgress = false;
//            //    }
//            //}
//            await _loadingSemaphore.WaitAsync();
//            try
//            {
//                if (_isLoadingInProgress)
//                {
//                    Debug.WriteLine("OpenDirectoryAsync already in progress, skipping");
//                    return;
//                }
//                _isLoadingInProgress = true;
//            }
//            finally
//            {
//                _loadingSemaphore.Release();
//            }

//            try
//            {
//                IsLoading = true;
//                _directoriesAndFiles.Clear();

//                if (IsMyComputer)
//                {
//                    LoadCachedDrives();
//                }
//                else
//                {
//                    await LoadDirectoryContentAsync();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка открытия директории: {ex.Message}");
//            }
//            finally
//            {
//                IsLoading = false;

//                await _loadingSemaphore.WaitAsync();
//                try
//                {
//                    _isLoadingInProgress = false;
//                }
//                finally
//                {
//                    _loadingSemaphore.Release();
//                }
//            }
//        }

//        //private void UpdateFromHistory()
//        //{
//        //    var current = _history.Current;
//        //    FilePath = current.DirectoryPath;
//        //    Name = current.DirectoryPathName;

//        //    if (IsMyComputer)
//        //    {
//        //        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Computer.png"));
//        //        LoadCachedDrives();
//        //    }
//        //    else if(!IsTreeViewNode ) //25 11 2025
//        //    {
//        //        ImageSource = null;
//        //        _ = LoadDirectoryContentAsync();
//        //    }

//        //    OnPropertiesChanged(nameof(FilePath), nameof(Name), nameof(ImageSource));
//        //}
//        //private void UpdateFromHistory()
//        //{
//        //    var current = _history.Current;

//        //    // ДЛЯ TREEVIEW УЗЛОВ: НЕ ОБНОВЛЯЕМ ИМЯ И ПУТЬ
//        //    if (!IsTreeViewNode)
//        //    {
//        //        FilePath = current.DirectoryPath;
//        //        Name = current.DirectoryPathName;
//        //    }

//        //    if (IsMyComputer)
//        //    {
//        //        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Computer.png"));
//        //        LoadCachedDrives();
//        //    }
//        //    else if (!IsTreeViewNode) // И не загружаем содержимое
//        //    {
//        //        ImageSource = null;
//        //        _ = LoadDirectoryContentAsync();
//        //    }

//        //    OnPropertiesChanged(nameof(FilePath), nameof(Name), nameof(ImageSource));
//        //}
//        private void UpdateFromHistory()
//        {
//            var current = _history.Current;

//            // ДЛЯ TREEVIEW УЗЛОВ И СПЕЦИАЛЬНЫХ ПАПОК: НЕ ОБНОВЛЯЕМ ИМЯ И ПУТЬ
//            if (!IsTreeViewNode && !IsSpecialFolderNode)
//            {
//                FilePath = current.DirectoryPath;
//                Name = current.DirectoryPathName;
//            }

//            if (IsMyComputer)
//            {
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Computer.png"));
//                LoadCachedDrives();
//            }
//            else if (!IsTreeViewNode && !IsSpecialFolderNode) // И не загружаем содержимое для узлов TreeView и спецпапок
//            {
//                ImageSource = null;
//                _ = LoadDirectoryContentAsync();
//            }

//            OnPropertiesChanged(nameof(FilePath), nameof(Name), nameof(ImageSource));
//        }
//        private static ObservableCollection<FileEntityViewModel> LoadDrives()
//        {
//            try
//            {
//                var drives = DriveService.GetDrives();
//                var collection = new ObservableCollection<FileEntityViewModel>();
//                foreach (var drive in drives)
//                {
//                    collection.Add(drive);
//                }
//                return collection;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка загрузки дисков: {ex.Message}");
//                return new ObservableCollection<FileEntityViewModel>();
//            }
//        }

//        private void LoadCachedDrives()
//        {
//            try
//            {
//                _directoriesAndFiles.Clear();
//                foreach (var drive in _cachedDrives.Value)
//                {
//                    _directoriesAndFiles.Add(drive);
//                }
//                UpdateDriveUsageStats();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка загрузки кэшированных дисков: {ex.Message}");
//            }
//        }

//        private void UpdateDriveUsageStats()
//        {
//            try
//            {
//                if (!IsMyComputer || !_cachedDrives.Value.Any()) return;

//                var drives = _cachedDrives.Value.OfType<DriveViewModel>().Where(d => !d.IsUnavailable).ToList();
//                if (drives.Any())
//                {
//                    UsedProcentValue = (int)drives.Average(d => d.UsedProcentValue);

//                    var representativeDrive = drives.FirstOrDefault();
//                    if (representativeDrive != null)
//                    {
//                        UsedSpaceString = representativeDrive.UsedSpaceString;
//                        FreeSpaceString = representativeDrive.FreeSpaceString;
//                        TotalSizeString = representativeDrive.TotalSizeString;

//                        OnPropertiesChanged(nameof(UsedSpaceString), nameof(FreeSpaceString), nameof(TotalSizeString));
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка в UpdateDriveUsageStats: {ex.Message}");
//            }
//        }

//        private async Task LoadDirectoryContentAsync()
//        {
//            _loadingCancellation?.Cancel();
//            _loadingCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Было 2 минуты

//            try
//            {
//                IsLoading = true;
//                _directoriesAndFiles.Clear();

//                var token = _loadingCancellation.Token;

//                // Простая асинхронная загрузка без параллелизма
//                var directoryItems = await DirectoryCacheService.GetDirectoryContentAsync(FilePath, token);

//                // Добавление элементов в UI
//                await AddItemsToUiAsync(directoryItems, token);
//            }
//            catch (OperationCanceledException)
//            {
//                Debug.WriteLine("Загрузка директории отменена");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка загрузки директории: {ex.Message}");
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }

//        private async Task AddItemsToUiAsync(List<FileEntityViewModel> directoryItems, CancellationToken token)
//        {
//            // Добавляем все элементы
//            await _dispatcherQueue.EnqueueAsync(() =>
//            {
//                _directoriesAndFiles.SuspendNotifications();
//                try
//                {
//                    foreach (var item in directoryItems.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
//                    {
//                        if (token.IsCancellationRequested) break;
//                        _directoriesAndFiles.Add(item);
//                    }
//                }
//                finally
//                {
//                    _directoriesAndFiles.ResumeNotifications();
//                }
//            });

//            // Загружаем иконки простым и эффективным способом
//            await LoadIconsDirectlyAsync(directoryItems.OfType<FileViewModel>().ToList(), token);
//        }
//        private async Task LoadIconsDirectlyAsync(List<FileViewModel> files, CancellationToken token)
//        {
//            // Загружаем иконки последовательно для первых 50 файлов
//            //foreach (var file in files.Take(50))
//            //{
//            //    if (token.IsCancellationRequested) break;

//            //    try
//            //    {
//            //        var icon = await FileCacheService.GetFileIconAsync(file.FullName);
//            //        if (icon != null)
//            //        {
//            //            await _dispatcherQueue.EnqueueAsync(() =>
//            //            {
//            //                file.ImageSource = icon;
//            //            });
//            //        }
//            //    }
//            //    catch (Exception ex)
//            //    {
//            //        Debug.WriteLine($"Ошибка загрузки иконки для {file.FullName}: {ex.Message}");
//            //    }

//            //    await Task.Delay(10, token); // Небольшая пауза
//            //}
//            var iconTasks = files.Take(20).Select(async file =>
//            {
//                if (token.IsCancellationRequested) return;

//                try
//                {
//                    var icon = await FileCacheService.GetFileIconAsync(file.FullName);
//                    if (icon != null && !token.IsCancellationRequested)
//                    {
//                        await _dispatcherQueue.EnqueueAsync(() =>
//                        {
//                            if (!token.IsCancellationRequested)
//                                file.ImageSource = icon;
//                        });
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Ошибка загрузки иконки: {ex.Message}");
//                }
//            }).ToList();

//            await Task.WhenAll(iconTasks);
//        }

//        private void Open(object parameter)
//        {
//            try
//            {
//                if (parameter is DirectoryViewModel drVM)
//                    NavigateTo(drVM.FullName, drVM.Name);
//                else if (parameter is DriveViewModel drive)
//                    NavigateTo(drive.FullName, drive.Name);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка открытия элемента: {ex.Message}");
//            }
//        }

//        private void NavigateTo(string path, string name)
//        {
//            //ХЗ работает или нет
//            _history.Add(path, name);
//        }
//        //private async Task NavigateTo(string path, string name)
//        //{
//        //    _history.Add(path, name);

//        //    // Предзагрузка содержимого в фоне
//        //    await DirectoryCacheService.PreloadDirectoryAsync(path);

//        //    // Предзагрузка родительской директории
//        //    var parentPath = Path.GetDirectoryName(path);
//        //    if (!string.IsNullOrEmpty(parentPath))
//        //    {
//        //        await DirectoryCacheService.PreloadDirectoryAsync(parentPath);
//        //    }
//        //}
//        //private async Task NavigateTo(string path, string name)
//        //{
//        //    try
//        //    {
//        //        _history.Add(path, name);

//        //        // Фоновая предзагрузка без блокировки основного потока
//        //        _ = Task.Run(async () =>
//        //        {
//        //            try
//        //            {
//        //                await DirectoryCacheService.PreloadDirectoryAsync(path);

//        //                // Предзагрузка родительской директории для быстрого возврата
//        //                var parentPath = Path.GetDirectoryName(path);
//        //                if (!string.IsNullOrEmpty(parentPath) && Directory.Exists(parentPath))
//        //                {
//        //                    await DirectoryCacheService.PreloadDirectoryAsync(parentPath);
//        //                }

//        //                Debug.WriteLine($"[NavigateTo] Фоновая предзагрузка завершена для {path}");
//        //            }
//        //            catch (Exception ex)
//        //            {
//        //                Debug.WriteLine($"[NavigateTo] Ошибка предзагрузки: {ex.Message}");
//        //            }
//        //        });
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Debug.WriteLine($"[NavigateTo] Критическая ошибка: {ex.Message}");
//        //    }
//        //}
//        private async void OnRefresh(object parameter)
//        {
//            try
//            {
//                // Инвалидируем кэш структуры папок
//                DirectoryCacheService.InvalidateCache(FilePath);

//                // Очищаем ВЕСЬ кэш файлов (так как при обновлении директории меняется состав файлов)
//                FileCacheService.ClearCache();

//                if (IsMyComputer)
//                {
//                    await DriveService.RefreshDrivesAsync();
//                    LoadCachedDrives();
//                }
//                else
//                {
//                    await OpenDirectoryAsync();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка обновления: {ex.Message}");
//            }
//        }

//        private void OnCancelLoading(object parameter)
//        {
//            _loadingCancellation?.Cancel();
//            IsLoading = false;
//        }

//        private bool CanCancelLoading(object parameter)
//        {
//            return _loadingCancellation != null &&
//                   !_loadingCancellation.IsCancellationRequested &&
//                   IsLoading;
//        }

//        #endregion

//        #region Обработчики событий

//        [SuppressPropertyChangedWarnings]
//        private void OnHistoryChanged(object sender, EventArgs e)
//        {
//            _dispatcherQueue?.TryEnqueue(() =>
//            {
//                MoveBackCommand?.RaiseCanExecuteChanged();
//                MoveForwardCommand?.RaiseCanExecuteChanged();
//                UpdateFromHistory();
//            });
//        }

//        private void OnDrivesUpdated(object sender, EventArgs e)
//        {
//            _dispatcherQueue?.TryEnqueue(() =>
//            {
//                if (IsMyComputer && !_disposed)
//                {
//                    _cachedDrives.Value.Clear();
//                    var drives = DriveService.GetDrives();
//                    foreach (var drive in drives)
//                    {
//                        _cachedDrives.Value.Add(drive);
//                    }
//                    UpdateDriveUsageStats();
//                }
//            });
//        }

//        private void OnFileCacheUpdated(object sender, FileCacheEventArgs e)
//        {
//            if (string.IsNullOrEmpty(e?.FilePath) || _disposed) return;

//            _dispatcherQueue?.TryEnqueue(() =>
//            {
//                try
//                {
//                    if (_disposed) return;

//                    var fileToUpdate = _directoriesAndFiles.OfType<FileViewModel>()
//                        .FirstOrDefault(f => f.FullName.Equals(e.FilePath, StringComparison.OrdinalIgnoreCase));

//                    if (fileToUpdate != null)
//                    {
//                        // Обновляем метаданные файла
//                        var fileInfo = new FileInfo(e.FilePath);
//                        if (fileInfo.Exists)
//                        {
//                            var updatedFile = FileCacheService.GetFileMetadata(fileInfo);
//                            var index = _directoriesAndFiles.IndexOf(fileToUpdate);
//                            if (index >= 0 && index < _directoriesAndFiles.Count)
//                            {
//                                _directoriesAndFiles[index] = updatedFile;
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Ошибка обновления кэша файлов: {ex.Message}");
//                }
//            });
//        }

//        private bool OnCanMoveBack(object obj) => _history?.CanMoveBack ?? false;
//        private void OnMoveBack(object obj) => _history?.MoveBack();
//        private bool OnCanMoveForward(object obj) => _history?.CanMoveForward ?? false;
//        private void OnMoveForward(object obj) => _history?.MoveForward();

//        #endregion

//        #region Вспомогательные методы

//        private void OnPropertiesChanged(params string[] propertyNames)
//        {
//            foreach (var propertyName in propertyNames)
//            {
//                OnPropertyChanged(propertyName);
//            }
//        }

//        #endregion

//        #region IDisposable

//        public void Dispose()
//        {
//            if (_disposed) return;

//            _loadingCancellation?.Cancel();
//            _loadingCancellation?.Dispose();

//            // Отписываемся от событий
//            _history.HistoryChanged -= OnHistoryChanged;
//            DriveService.DrivesUpdated -= OnDrivesUpdated;
//            PerformanceManager.MetricsUpdated -= OnPerformanceMetricsUpdated;
//            FileCacheService.CacheUpdated -= OnFileCacheUpdated;

//            PerformanceManager.Dispose();
//            _disposed = true;
//        }

//        #endregion
//    }

//    // Вспомогательные классы
//    public class BatchObservableCollection<T> : ObservableCollection<T>
//    {
//        private bool _notificationsSuspended;

//        public void SuspendNotifications() => _notificationsSuspended = true;

//        public void ResumeNotifications()
//        {
//            _notificationsSuspended = false;
//            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
//                System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
//        }

//        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
//        {
//            if (!_notificationsSuspended) base.OnCollectionChanged(e);
//        }
//    }

//    public static class DispatcherQueueExtensions
//    {
//        public static async Task EnqueueAsync(this DispatcherQueue dispatcherQueue, Action action)
//        {
//            var tcs = new TaskCompletionSource<bool>();

//            if (dispatcherQueue.HasThreadAccess)
//            {
//                action();
//                tcs.SetResult(true);
//            }
//            else
//            {
//                dispatcherQueue.TryEnqueue(() =>
//                {
//                    action();
//                    tcs.SetResult(true);
//                });
//            }

//            await tcs.Task;
//        }
//    }
//}


using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;
using PropertyChanged;

namespace Core_FileManagement
{
    public class ExplorerItemViewModel : BaseViewModel, IDisposable
    {
        #region Поля и константы

        private readonly IDirectoryHistory _history;
        private static readonly Lazy<ObservableCollection<FileEntityViewModel>> _cachedDrives =
            new Lazy<ObservableCollection<FileEntityViewModel>>(LoadDrives);
        private bool _disposed = false;
        private CancellationTokenSource _loadingCancellation;
        private DispatcherQueue _dispatcherQueue;
        private DispatcherQueueController _dispatcherQueueController;

        // Оптимизации для производительности
        private readonly BatchObservableCollection<FileEntityViewModel> _directoriesAndFiles;
        private readonly SemaphoreSlim _loadingSemaphore = new SemaphoreSlim(1, 1);
        private bool _isLoadingInProgress = false;

        // Поля для редактирования (ДОБАВЛЕНО)
        private bool _isEditing = false;
        private string _originalName = "";
        private bool _editRequested = false;
        private EntityFlags _flags = EntityFlags.None; // ДОБАВЛЕНО: Используем EntityFlags вместо FileAttributes

        //04 11 2025
        public bool HasChildren { get; set; }
        //25 11 2025
        public bool IsTreeViewNode { get; set; }
        // 29 11 2025
        public bool IsSpecialFolderNode { get; set; }
        #endregion

        #region Свойства навигации

        public string FilePath { get; set; }
        public string Name { get; set; }
        public ObservableCollection<FileEntityViewModel> DirectoriesAndFiles => _directoriesAndFiles;
        public FileEntityViewModel SelectedFileEntity { get; set; }
        public BitmapImage ImageSource { get; set; }
        public bool IsMyComputer => FilePath == "Мой Компьютер" || Name == "Мой Компьютер";

        #endregion

        #region Свойства редактирования (ДОБАВЛЕНО)

        // В ExplorerItemViewModel.cs
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged();

                    // При начале редактирования сохраняем оригинальное имя
                    if (value)
                    {
                        _originalName = Name;
                        Debug.WriteLine($"Начато редактирование: {_originalName}");
                    }
                    else
                    {
                        Debug.WriteLine($"Редактирование завершено");
                    }
                }
            }
        }

        public bool EditRequested
        {
            get => _editRequested;
            set
            {
                if (_editRequested != value)
                {
                    _editRequested = value;
                    OnPropertyChanged();
                }
            }
        }

        public EntityFlags Flags
        {
            get => _flags;
            set
            {
                if (_flags != value)
                {
                    _flags = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Свойства дискового пространства

        public string UsedSpaceString { get; set; }
        public string FreeSpaceString { get; set; }
        public string TotalSizeString { get; set; }

        private int _usedProcentValue;
        public int UsedProcentValue
        {
            get => _usedProcentValue;
            set
            {
                if (_usedProcentValue != value)
                {
                    _usedProcentValue = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsCritical));
                }
            }
        }
        public bool IsCritical => UsedProcentValue >= 80;

        #endregion

        #region Свойства производительности

        public PerformanceMetrics CurrentMetrics { get; private set; } = new();

        public float CpuUsagePercent { get; private set; }
        public double MemoryUsagePercent { get; private set; }

        private string _cpuUsageText = "0%";
        public string CpuUsageText
        {
            get => _cpuUsageText;
            set { if (_cpuUsageText != value) { _cpuUsageText = value; OnPropertyChanged(); } }
        }

        private string _ioUsageText = "0 MB/s";
        public string IoUsageText
        {
            get => _ioUsageText;
            set { if (_ioUsageText != value) { _ioUsageText = value; OnPropertyChanged(); } }
        }

        private string _memoryUsageText = "0 MB";
        public string MemoryUsageText
        {
            get => _memoryUsageText;
            set { if (_memoryUsageText != value) { _memoryUsageText = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Состояние UI

        private bool _isProgressBarVisible;
        public bool IsProgressBarVisible
        {
            get => _isProgressBarVisible;
            set { if (_isProgressBarVisible != value) { _isProgressBarVisible = value; OnPropertyChanged(); } }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); } }
        }

        #endregion

        #region Команды

        public DelegateCommand OpenCommand { get; private set; }
        public DelegateCommand MoveBackCommand { get; private set; }
        public DelegateCommand MoveForwardCommand { get; private set; }
        public DelegateCommand RefreshCommand { get; private set; }
        public DelegateCommand CancelLoadingCommand { get; private set; }

        // Команды для редактирования (ДОБАВЛЕНО)
        public DelegateCommand StartEditCommand { get; private set; }
        public DelegateCommand SaveEditCommand { get; private set; }
        public DelegateCommand CancelEditCommand { get; private set; }

        #endregion

        #region Конструктор и инициализация

        public ExplorerItemViewModel(IDirectoryHistory history)
        {
            _history = history ?? throw new ArgumentNullException(nameof(history));

            // Инициализация DispatcherQueue
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            if (_dispatcherQueue == null)
            {
                _dispatcherQueueController = DispatcherQueueController.CreateOnCurrentThread();
                _dispatcherQueue = _dispatcherQueueController.DispatcherQueue;
                Debug.WriteLine("Создан новый DispatcherQueueController");
            }

            // Инициализация оптимизированных коллекций
            _directoriesAndFiles = new BatchObservableCollection<FileEntityViewModel>();

            InitializeCommands();
            InitializeMetrics();
            UpdateFromHistory();

            SubscribeToEvents();
            _ = InitializePerformanceManagerAsync();
        }

        private void InitializeCommands()
        {
            OpenCommand = new DelegateCommand(Open);
            MoveBackCommand = new DelegateCommand(OnMoveBack, OnCanMoveBack);
            MoveForwardCommand = new DelegateCommand(OnMoveForward, OnCanMoveForward);
            RefreshCommand = new DelegateCommand(OnRefresh);
            CancelLoadingCommand = new DelegateCommand(OnCancelLoading, CanCancelLoading);

            // Команды для редактирования (ДОБАВЛЕНО)
            StartEditCommand = new DelegateCommand(OnStartEdit, CanStartEdit);
            SaveEditCommand = new DelegateCommand(OnSaveEdit, CanSaveEdit);
            CancelEditCommand = new DelegateCommand(OnCancelEdit);
        }

        private void InitializeMetrics()
        {
            CurrentMetrics = new PerformanceMetrics
            {
                CpuUsage = 0,
                IoUsage = 0,
                MemoryUsage = 0,
                MemoryUsagePercent = 0,
                TotalMemory = 16L * 1024 * 1024 * 1024
            };
            UpdateStatusBar();
        }

        private void SubscribeToEvents()
        {
            _history.HistoryChanged += OnHistoryChanged;
            DriveService.DrivesUpdated += OnDrivesUpdated;
            FileCacheService.CacheUpdated += OnFileCacheUpdated;
        }

        #endregion

        #region Методы работы с производительностью

        private async Task InitializePerformanceManagerAsync()
        {
            try
            {
                bool initialized = await PerformanceManager.InitializeAsync();
                if (initialized)
                {
                    CurrentMetrics.TotalMemory = PerformanceManager.GetTotalSystemMemory();
                    PerformanceManager.MetricsUpdated += OnPerformanceMetricsUpdated;
                    PerformanceManager.StartMonitoring();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка инициализации PerformanceManager: {ex}");
            }
        }

        private void OnPerformanceMetricsUpdated(object sender, PerformanceMetrics metrics)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (_disposed) return;

                CurrentMetrics = metrics ?? new PerformanceMetrics();
                UpdateStatusBar();
            });
        }

        private void UpdateStatusBar()
        {
            if (CurrentMetrics == null) return;

            CpuUsageText = $"{CurrentMetrics.CpuUsage:0}%";
            IoUsageText = $"{CurrentMetrics.IoUsage:0.0} MB/s";
            MemoryUsageText = $"{CurrentMetrics.MemoryUsage / (1024 * 1024):0} MB / {CurrentMetrics.TotalMemory / (1024 * 1024):0} MB";

            CpuUsagePercent = CurrentMetrics.CpuUsage;
            MemoryUsagePercent = CurrentMetrics.MemoryUsagePercent;

            OnPropertiesChanged(nameof(CpuUsageText), nameof(IoUsageText), nameof(MemoryUsageText), nameof(CpuUsagePercent), nameof(MemoryUsagePercent));
        }

        #endregion

        #region Методы работы с файловой системой

        public async Task OpenDirectoryAsync()
        {
            await _loadingSemaphore.WaitAsync();
            try
            {
                if (_isLoadingInProgress)
                {
                    Debug.WriteLine("OpenDirectoryAsync already in progress, skipping");
                    return;
                }
                _isLoadingInProgress = true;
            }
            finally
            {
                _loadingSemaphore.Release();
            }

            try
            {
                IsLoading = true;
                _directoriesAndFiles.Clear();

                if (IsMyComputer)
                {
                    LoadCachedDrives();
                }
                else
                {
                    await LoadDirectoryContentAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка открытия директории: {ex.Message}");
            }
            finally
            {
                IsLoading = false;

                await _loadingSemaphore.WaitAsync();
                try
                {
                    _isLoadingInProgress = false;
                }
                finally
                {
                    _loadingSemaphore.Release();
                }
            }
        }

        private void UpdateFromHistory()
        {
            var current = _history.Current;

            // ДЛЯ TREEVIEW УЗЛОВ И СПЕЦИАЛЬНЫХ ПАПОК: НЕ ОБНОВЛЯЕМ ИМЯ И ПУТЬ
            if (!IsTreeViewNode && !IsSpecialFolderNode)
            {
                FilePath = current.DirectoryPath;
                Name = current.DirectoryPathName;
            }

            if (IsMyComputer)
            {
                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Computer.png"));
                LoadCachedDrives();
            }
            else if (!IsTreeViewNode && !IsSpecialFolderNode) // И не загружаем содержимое для узлов TreeView и спецпапок
            {
                ImageSource = null;
                _ = LoadDirectoryContentAsync();
            }

            OnPropertiesChanged(nameof(FilePath), nameof(Name), nameof(ImageSource));
        }

        private static ObservableCollection<FileEntityViewModel> LoadDrives()
        {
            try
            {
                var drives = DriveService.GetDrives();
                var collection = new ObservableCollection<FileEntityViewModel>();
                foreach (var drive in drives)
                {
                    collection.Add(drive);
                }
                return collection;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки дисков: {ex.Message}");
                return new ObservableCollection<FileEntityViewModel>();
            }
        }

        private void LoadCachedDrives()
        {
            try
            {
                _directoriesAndFiles.Clear();
                foreach (var drive in _cachedDrives.Value)
                {
                    _directoriesAndFiles.Add(drive);
                }
                UpdateDriveUsageStats();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки кэшированных дисков: {ex.Message}");
            }
        }

        private void UpdateDriveUsageStats()
        {
            try
            {
                if (!IsMyComputer || !_cachedDrives.Value.Any()) return;

                var drives = _cachedDrives.Value.OfType<DriveViewModel>().Where(d => !d.IsUnavailable).ToList();
                if (drives.Any())
                {
                    UsedProcentValue = (int)drives.Average(d => d.UsedProcentValue);

                    var representativeDrive = drives.FirstOrDefault();
                    if (representativeDrive != null)
                    {
                        UsedSpaceString = representativeDrive.UsedSpaceString;
                        FreeSpaceString = representativeDrive.FreeSpaceString;
                        TotalSizeString = representativeDrive.TotalSizeString;

                        OnPropertiesChanged(nameof(UsedSpaceString), nameof(FreeSpaceString), nameof(TotalSizeString));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в UpdateDriveUsageStats: {ex.Message}");
            }
        }

        private async Task LoadDirectoryContentAsync()
        {
            _loadingCancellation?.Cancel();
            _loadingCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // Было 2 минуты

            try
            {
                IsLoading = true;
                _directoriesAndFiles.Clear();

                var token = _loadingCancellation.Token;

                // Простая асинхронная загрузка без параллелизма
                var directoryItems = await DirectoryCacheService.GetDirectoryContentAsync(FilePath, token);

                // Добавление элементов в UI
                await AddItemsToUiAsync(directoryItems, token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Загрузка директории отменена");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки директории: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddItemsToUiAsync(List<FileEntityViewModel> directoryItems, CancellationToken token)
        {
            // Добавляем все элементы
            await _dispatcherQueue.EnqueueAsync(() =>
            {
                _directoriesAndFiles.SuspendNotifications();
                try
                {
                    foreach (var item in directoryItems.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        if (token.IsCancellationRequested) break;
                        _directoriesAndFiles.Add(item);
                    }
                }
                finally
                {
                    _directoriesAndFiles.ResumeNotifications();
                }
            });

            // Загружаем иконки простым и эффективным способом
            await LoadIconsDirectlyAsync(directoryItems.OfType<FileViewModel>().ToList(), token);
        }

        private async Task LoadIconsDirectlyAsync(List<FileViewModel> files, CancellationToken token)
        {
            var iconTasks = files.Take(20).Select(async file =>
            {
                if (token.IsCancellationRequested) return;

                try
                {
                    var icon = await FileCacheService.GetFileIconAsync(file.FullName);
                    if (icon != null && !token.IsCancellationRequested)
                    {
                        await _dispatcherQueue.EnqueueAsync(() =>
                        {
                            if (!token.IsCancellationRequested)
                                file.ImageSource = icon;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка загрузки иконки: {ex.Message}");
                }
            }).ToList();

            await Task.WhenAll(iconTasks);
        }

        private void Open(object parameter)
        {
            try
            {
                if (parameter is DirectoryViewModel drVM)
                    NavigateTo(drVM.FullName, drVM.Name);
                else if (parameter is DriveViewModel drive)
                    NavigateTo(drive.FullName, drive.Name);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка открытия элемента: {ex.Message}");
            }
        }

        private void NavigateTo(string path, string name)
        {
            _history.Add(path, name);
        }

        private async void OnRefresh(object parameter)
        {
            try
            {
                // Инвалидируем кэш структуры папок
                DirectoryCacheService.InvalidateCache(FilePath);

                // Очищаем ВЕСЬ кэш файлов (так как при обновлении директории меняется состав файлов)
                FileCacheService.ClearCache();

                if (IsMyComputer)
                {
                    await DriveService.RefreshDrivesAsync();
                    LoadCachedDrives();
                }
                else
                {
                    await OpenDirectoryAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обновления: {ex.Message}");
            }
        }

        private void OnCancelLoading(object parameter)
        {
            _loadingCancellation?.Cancel();
            IsLoading = false;
        }

        private bool CanCancelLoading(object parameter)
        {
            return _loadingCancellation != null &&
                   !_loadingCancellation.IsCancellationRequested &&
                   IsLoading;
        }

        #endregion

        #region Методы редактирования (ДОБАВЛЕНО)

        private bool CanStartEdit(object parameter)
        {
            // Проверяем, можно ли редактировать этот элемент
            // Нельзя редактировать: MyComputer, Drives, системные элементы, уже редактируемый элемент
            return !IsEditing &&
                   !string.IsNullOrEmpty(FilePath) &&
                   !IsMyComputer &&
                   !FilePath.Equals("Drives", StringComparison.OrdinalIgnoreCase) &&
                   !IsTreeViewNode &&
                   !IsSpecialFolderNode &&
                   (File.Exists(FilePath) || Directory.Exists(FilePath));
        }

        private void OnStartEdit(object parameter)
        {
            Debug.WriteLine($"Запрос на редактирование: {Name}");
            IsEditing = true;
            EditRequested = true;
        }

        private bool CanSaveEdit(object parameter)
        {
            // Можно сохранять если идет редактирование и имя не пустое
            return IsEditing &&
                   !string.IsNullOrEmpty(Name?.Trim()) &&
                   Name.Trim() != _originalName;
        }

        private async void OnSaveEdit(object parameter)
        {
            try
            {
                string newName = Name?.Trim() ?? "";

                if (string.IsNullOrEmpty(newName))
                {
                    Debug.WriteLine("Имя не может быть пустым");
                    CancelEdit();
                    return;
                }

                if (newName == _originalName)
                {
                    Debug.WriteLine("Имя не изменилось");
                    CancelEdit();
                    return;
                }

                Debug.WriteLine($"Попытка переименования: {_originalName} -> {newName}");

                bool success = false;

                // Простая проверка по существованию файла/папки
                if (File.Exists(FilePath))
                {
                    // Переименование файла
                    string newPath = Path.Combine(Path.GetDirectoryName(FilePath), newName);
                    success = await RenameFileAsync(FilePath, newPath);
                }
                else if (Directory.Exists(FilePath))
                {
                    // Переименование папки
                    string newPath = Path.Combine(Path.GetDirectoryName(FilePath), newName);
                    success = await RenameDirectoryAsync(FilePath, newPath);
                }
                else
                {
                    Debug.WriteLine($"Элемент не найден: {FilePath}");
                    CancelEdit();
                    return;
                }

                if (success)
                {
                    Debug.WriteLine($"Успешно переименовано: {_originalName} -> {newName}");
                    IsEditing = false;
                    EditRequested = false;

                    // Инвалидируем кэш родительской директории
                    if (!string.IsNullOrEmpty(FilePath))
                    {
                        string parentPath = Path.GetDirectoryName(FilePath);
                        if (!string.IsNullOrEmpty(parentPath))
                        {
                            DirectoryCacheService.InvalidateCache(parentPath);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Ошибка при переименовании");
                    CancelEdit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при сохранении редактирования: {ex.Message}");
                CancelEdit();
            }
        }

        private void OnCancelEdit(object parameter)
        {
            CancelEdit();
        }

        public void CancelEdit()
        {
            // Восстанавливаем оригинальное имя
            if (!string.IsNullOrEmpty(_originalName))
            {
                Name = _originalName;
                OnPropertyChanged(nameof(Name));
            }

            IsEditing = false;
            EditRequested = false;

            Debug.WriteLine("Редактирование отменено");
        }

        private async Task<bool> RenameFileAsync(string oldPath, string newPath)
        {
            try
            {
                // Проверяем, существует ли файл с таким именем
                if (File.Exists(newPath))
                {
                    Debug.WriteLine($"Файл {newPath} уже существует");
                    return false;
                }

                await Task.Run(() => File.Move(oldPath, newPath));
                FilePath = newPath;

                Debug.WriteLine($"Файл переименован: {oldPath} -> {newPath}");
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Нет прав для переименования файла: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Ошибка ввода-вывода при переименовании файла: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переименовании файла: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RenameDirectoryAsync(string oldPath, string newPath)
        {
            try
            {
                // Проверяем, существует ли папка с таким именем
                if (Directory.Exists(newPath))
                {
                    Debug.WriteLine($"Папка {newPath} уже существует");
                    return false;
                }

                await Task.Run(() => Directory.Move(oldPath, newPath));
                FilePath = newPath;

                Debug.WriteLine($"Папка переименована: {oldPath} -> {newPath}");
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Нет прав для переименования папки: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Ошибка ввода-вывода при переименовании папки: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при переименовании папки: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Обработчики событий

        [SuppressPropertyChangedWarnings]
        private void OnHistoryChanged(object sender, EventArgs e)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                MoveBackCommand?.RaiseCanExecuteChanged();
                MoveForwardCommand?.RaiseCanExecuteChanged();
                UpdateFromHistory();
            });
        }

        private void OnDrivesUpdated(object sender, EventArgs e)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (IsMyComputer && !_disposed)
                {
                    _cachedDrives.Value.Clear();
                    var drives = DriveService.GetDrives();
                    foreach (var drive in drives)
                    {
                        _cachedDrives.Value.Add(drive);
                    }
                    UpdateDriveUsageStats();
                }
            });
        }

        private void OnFileCacheUpdated(object sender, FileCacheEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.FilePath) || _disposed) return;

            _dispatcherQueue?.TryEnqueue(() =>
            {
                try
                {
                    if (_disposed) return;

                    var fileToUpdate = _directoriesAndFiles.OfType<FileViewModel>()
                        .FirstOrDefault(f => f.FullName.Equals(e.FilePath, StringComparison.OrdinalIgnoreCase));

                    if (fileToUpdate != null)
                    {
                        // Обновляем метаданные файла
                        var fileInfo = new FileInfo(e.FilePath);
                        if (fileInfo.Exists)
                        {
                            var updatedFile = FileCacheService.GetFileMetadata(fileInfo);
                            var index = _directoriesAndFiles.IndexOf(fileToUpdate);
                            if (index >= 0 && index < _directoriesAndFiles.Count)
                            {
                                _directoriesAndFiles[index] = updatedFile;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка обновления кэша файлов: {ex.Message}");
                }
            });
        }

        private bool OnCanMoveBack(object obj) => _history?.CanMoveBack ?? false;
        private void OnMoveBack(object obj) => _history?.MoveBack();
        private bool OnCanMoveForward(object obj) => _history?.CanMoveForward ?? false;
        private void OnMoveForward(object obj) => _history?.MoveForward();

        #endregion

        #region Вспомогательные методы

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _loadingCancellation?.Cancel();
            _loadingCancellation?.Dispose();

            // Отписываемся от событий
            _history.HistoryChanged -= OnHistoryChanged;
            DriveService.DrivesUpdated -= OnDrivesUpdated;
            PerformanceManager.MetricsUpdated -= OnPerformanceMetricsUpdated;
            FileCacheService.CacheUpdated -= OnFileCacheUpdated;

            PerformanceManager.Dispose();
            _disposed = true;
        }

        #endregion
    }

    // Вспомогательные классы
    public class BatchObservableCollection<T> : ObservableCollection<T>
    {
        private bool _notificationsSuspended;

        public void SuspendNotifications() => _notificationsSuspended = true;

        public void ResumeNotifications()
        {
            _notificationsSuspended = false;
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!_notificationsSuspended) base.OnCollectionChanged(e);
        }
    }

    public static class DispatcherQueueExtensions
    {
        public static async Task EnqueueAsync(this DispatcherQueue dispatcherQueue, Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (dispatcherQueue.HasThreadAccess)
            {
                action();
                tcs.SetResult(true);
            }
            else
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    action();
                    tcs.SetResult(true);
                });
            }

            await tcs.Task;
        }
    }
}