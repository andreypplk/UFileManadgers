using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

        private readonly BatchObservableCollection<FileEntityViewModel> _directoriesAndFiles;
        private readonly SemaphoreSlim _loadingSemaphore = new SemaphoreSlim(1, 1);
        private bool _isLoadingInProgress = false;

        private readonly Dictionary<string, FileViewModel> _fileMap = new(StringComparer.OrdinalIgnoreCase);

        private DispatcherQueueTimer _metricsUpdateTimer;
        private PerformanceMetrics _pendingMetrics;
        private DispatcherQueueTimer _historyUpdateTimer;

        private int _cachedUsedPercent;
        private string _cachedUsedSpace;
        private string _cachedFreeSpace;
        private string _cachedTotalSize;

        private bool _isEditing = false;
        private string _originalName = "";
        private bool _editRequested = false;
        private string _filePath;
        private string _name;
        private string _newNameForEdit = "";

        // Поддержка уведомлений для ImageSource
        private BitmapImage _imageSource;
        public BitmapImage ImageSource
        {
            get => _imageSource;
            set
            {
                if (_imageSource != value)
                {
                    _imageSource = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasChildren { get; set; }
        public bool IsTreeViewNode { get; set; }
        public bool IsSpecialFolderNode { get; set; }
        #endregion

        #region Свойства навигации

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    SaveEditCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<FileEntityViewModel> DirectoriesAndFiles => _directoriesAndFiles;
        public FileEntityViewModel SelectedFileEntity { get; set; }
        public bool IsMyComputer => FilePath == "Мой Компьютер" || Name == "Мой Компьютер";

        #endregion

        #region Свойства редактирования

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged();
                }
            }
        }
        public string NewNameForEdit
        {
            get => _newNameForEdit;
            set
            {
                if (_newNameForEdit != value)
                {
                    _newNameForEdit = value;
                    OnPropertyChanged();
                    SaveEditCommand?.RaiseCanExecuteChanged();
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

        public EntityFlags Flags { get; set; } = EntityFlags.None;

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

        // Для поддержки дополнительных привязок (могут заполняться позже)
        public string ItemCountString { get; set; } = "";
        public string LastModified { get; set; } = "";
        public string AttributesText { get; set; } = "";
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
        public DelegateCommand StartEditCommand { get; private set; }
        public DelegateCommand SaveEditCommand { get; private set; }
        public DelegateCommand CancelEditCommand { get; private set; }

        #endregion

        #region Конструктор и инициализация

        public ExplorerItemViewModel(IDirectoryHistory history)
        {
            _history = history ?? throw new ArgumentNullException(nameof(history));

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            if (_dispatcherQueue == null)
            {
                _dispatcherQueueController = DispatcherQueueController.CreateOnCurrentThread();
                _dispatcherQueue = _dispatcherQueueController.DispatcherQueue;
            }

            _directoriesAndFiles = new BatchObservableCollection<FileEntityViewModel>();
            _directoriesAndFiles.CollectionChanged += OnDirectoriesAndFilesCollectionChanged;

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
            catch
            {
            }
        }

        private void OnPerformanceMetricsUpdated(object sender, PerformanceMetrics metrics)
        {
            _pendingMetrics = metrics;
            if (_metricsUpdateTimer == null)
            {
                _metricsUpdateTimer = _dispatcherQueue.CreateTimer();
                _metricsUpdateTimer.Interval = TimeSpan.FromMilliseconds(500);
                _metricsUpdateTimer.Tick += (s, e) =>
                {
                    _metricsUpdateTimer.Stop();
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        if (_pendingMetrics != null && !_disposed)
                        {
                            CurrentMetrics = _pendingMetrics;
                            UpdateStatusBar();
                        }
                    });
                };
            }
            _metricsUpdateTimer.Start();
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
                _fileMap.Clear();

                if (IsMyComputer)
                {
                    LoadCachedDrives();
                }
                else
                {
                    await LoadDirectoryContentAsync();
                }
            }
            catch
            {
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
            else if (!IsTreeViewNode && !IsSpecialFolderNode)
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
            catch
            {
                return new ObservableCollection<FileEntityViewModel>();
            }
        }

        private void LoadCachedDrives()
        {
            try
            {
                _directoriesAndFiles.SuspendNotifications();
                try
                {
                    _directoriesAndFiles.Clear();
                    _directoriesAndFiles.AddRange(_cachedDrives.Value);
                }
                finally
                {
                    _directoriesAndFiles.ResumeNotifications();
                }
                UpdateDriveUsageStats();
            }
            catch
            {
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
                    _cachedUsedPercent = (int)drives.Average(d => d.UsedProcentValue);
                    var rep = drives.FirstOrDefault();
                    if (rep != null)
                    {
                        _cachedUsedSpace = rep.UsedSpaceString;
                        _cachedFreeSpace = rep.FreeSpaceString;
                        _cachedTotalSize = rep.TotalSizeString;
                    }
                }
                else
                {
                    _cachedUsedPercent = 0;
                    _cachedUsedSpace = _cachedFreeSpace = _cachedTotalSize = "";
                }

                UsedProcentValue = _cachedUsedPercent;
                UsedSpaceString = _cachedUsedSpace;
                FreeSpaceString = _cachedFreeSpace;
                TotalSizeString = _cachedTotalSize;
                OnPropertiesChanged(nameof(UsedSpaceString), nameof(FreeSpaceString), nameof(TotalSizeString));
            }
            catch
            {
            }
        }

        // ==================== ПОТОКОВАЯ БЫСТРАЯ ЗАГРУЗКА ====================
        private async Task LoadDirectoryContentAsync()
        {
            try
            {
                _loadingCancellation?.Cancel();
                _loadingCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                IsLoading = true;
                _directoriesAndFiles.Clear();
                _fileMap.Clear();

                var token = _loadingCancellation.Token;
                string path = FilePath;

                // Фоновая задача потокового перечисления файлов и папок
                await Task.Run(async () =>
                {
                    var dirInfo = new DirectoryInfo(path);
                    if (!dirInfo.Exists) return;

                    var batch = new List<FileEntityViewModel>(100);
                    foreach (var fsi in dirInfo.EnumerateFileSystemInfos())
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            var item = CreateFileSystemItem(fsi);
                            if (item != null)
                            {
                                batch.Add(item);
                                if (batch.Count >= 100)
                                {
                                    var batchCopy = batch.ToList();
                                    batch.Clear();
                                    // Добавляем партию в UI через диспетчер
                                    await _dispatcherQueue.EnqueueAsync(() =>
                                    {
                                        if (token.IsCancellationRequested) return;
                                        _directoriesAndFiles.SuspendNotifications();
                                        try
                                        {
                                            foreach (var it in batchCopy)
                                                _directoriesAndFiles.Add(it);
                                        }
                                        finally { _directoriesAndFiles.ResumeNotifications(); }
                                    });
                                    await Task.Delay(1, token); // даём UI "продышаться"
                                }
                            }
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                        {
                            // Игнорируем недоступные элементы
                        }
                    }

                    // Добавляем оставшиеся элементы
                    if (batch.Count > 0)
                    {
                        await _dispatcherQueue.EnqueueAsync(() =>
                        {
                            if (token.IsCancellationRequested) return;
                            _directoriesAndFiles.SuspendNotifications();
                            try
                            {
                                foreach (var it in batch)
                                    _directoriesAndFiles.Add(it);
                            }
                            finally { _directoriesAndFiles.ResumeNotifications(); }
                        });
                    }
                }, token);

                // После завершения загрузки можно дозагрузить иконки для первых файлов
                var files = _directoriesAndFiles.OfType<FileViewModel>().Take(20).ToList();
                await LoadIconsGroupedAsync(files, token);
            }
            catch (OperationCanceledException)
            {
                // Загрузка отменена – ничего не делаем
            }
            catch
            {
                // Игнорируем остальные ошибки
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Вспомогательный метод создания ViewModel
        private static FileEntityViewModel CreateFileSystemItem(FileSystemInfo fsi)
        {
            if (fsi is DirectoryInfo dir)
            {
                var flags = EntityFlags.IsDirectory;
                if (dir.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
                if (dir.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;
                return new DirectoryViewModel(dir, flags);
            }
            else if (fsi is FileInfo file)
            {
                var flags = EntityFlags.IsFile;
                if (file.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
                if (file.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;
                if (file.Attributes.HasFlag(FileAttributes.ReadOnly)) flags |= EntityFlags.IsReadOnly;
                return new FileViewModel(file, flags);
            }
            return null;
        }

        private async Task LoadIconsGroupedAsync(List<FileViewModel> files, CancellationToken token)
        {
            try
            {
                var tasks = files.Select(async file =>
                {
                    if (token.IsCancellationRequested) return (file: (FileViewModel)null, icon: (BitmapImage)null);
                    try
                    {
                        var icon = await FileCacheService.GetFileIconAsync(file.FullName);
                        return (file, icon);
                    }
                    catch
                    {
                        return (file: null, icon: null);
                    }
                }).ToList();

                var results = await Task.WhenAll(tasks);
                var updates = results.Where(r => r.file != null && r.icon != null).ToList();
                if (updates.Count == 0) return;

                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    foreach (var (file, icon) in updates)
                    {
                        if (!token.IsCancellationRequested && file != null)
                            file.ImageSource = icon;
                    }
                });
            }
            catch
            {
            }
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
            catch
            {
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
                DirectoryCacheService.InvalidateCache(FilePath);

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
            catch
            {
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

        #region Методы редактирования

        private bool CanStartEdit(object parameter)
        {
            return !IsEditing &&
                   !string.IsNullOrEmpty(FilePath) &&
                   !IsMyComputer &&
                   !IsTreeViewNode &&
                   !IsSpecialFolderNode &&
                   (File.Exists(FilePath) || Directory.Exists(FilePath));
        }

        private void OnStartEdit(object parameter)
        {
            IsEditing = true;
            EditRequested = true;
            _originalName = Name;
            NewNameForEdit = Name;
        }

        private bool CanSaveEdit(object parameter)
        {
            return IsEditing &&
                   !string.IsNullOrEmpty(NewNameForEdit?.Trim()) &&
                   NewNameForEdit.Trim() != _originalName;
        }

        private async void OnSaveEdit(object parameter)
        {
            try
            {
                string newName = NewNameForEdit?.Trim() ?? "";
                if (string.IsNullOrEmpty(newName) || newName == _originalName)
                {
                    CancelEdit();
                    return;
                }

                bool success = false;
                string newPath = "";
                string directory = Path.GetDirectoryName(FilePath);

                if (File.Exists(FilePath))
                {
                    newPath = Path.Combine(directory, newName);
                    success = await RenameFileAsync(FilePath, newPath);
                }
                else if (Directory.Exists(FilePath))
                {
                    newPath = Path.Combine(directory, newName);
                    success = await RenameDirectoryAsync(FilePath, newPath);
                }
                else
                {
                    CancelEdit();
                    return;
                }

                if (success)
                {
                    Name = newName;
                    FilePath = newPath;
                    IsEditing = false;
                    EditRequested = false;
                    NewNameForEdit = "";

                    if (!string.IsNullOrEmpty(directory))
                    {
                        DirectoryCacheService.InvalidateCache(directory);
                        await LoadDirectoryContentAsync();
                    }
                }
                else
                {
                    NewNameForEdit = _originalName;
                    CancelEdit();
                }
            }
            catch
            {
                CancelEdit();
            }
        }

        private void OnCancelEdit(object parameter) => CancelEdit();

        public void CancelEdit()
        {
            if (!string.IsNullOrEmpty(_originalName))
            {
                NewNameForEdit = _originalName;
                if (Name != _originalName) Name = _originalName;
            }
            IsEditing = false;
            EditRequested = false;
            NewNameForEdit = "";
        }

        private async Task<bool> RenameFileAsync(string oldPath, string newPath)
        {
            try
            {
                var fileInfo = new FileInfo(oldPath);
                if (!fileInfo.Exists) return false;
                if (File.Exists(newPath)) return false;

                await Task.Run(() => fileInfo.MoveTo(newPath));
                return File.Exists(newPath) && !File.Exists(oldPath);
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> RenameDirectoryAsync(string oldPath, string newPath)
        {
            try
            {
                if (Directory.Exists(newPath)) return false;
                await Task.Run(() => Directory.Move(oldPath, newPath));
                return Directory.Exists(newPath) && !Directory.Exists(oldPath);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Обработчики событий

        private void OnHistoryChanged(object sender, EventArgs e)
        {
            if (_historyUpdateTimer == null)
            {
                _historyUpdateTimer = _dispatcherQueue.CreateTimer();
                _historyUpdateTimer.Interval = TimeSpan.FromMilliseconds(200);
                _historyUpdateTimer.Tick += (s, ev) =>
                {
                    _historyUpdateTimer.Stop();
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        MoveBackCommand?.RaiseCanExecuteChanged();
                        MoveForwardCommand?.RaiseCanExecuteChanged();
                        UpdateFromHistory();
                    });
                };
            }
            _historyUpdateTimer.Start();
        }

        private void OnDrivesUpdated(object sender, EventArgs e)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                if (IsMyComputer && !_disposed)
                {
                    var newDrives = new ObservableCollection<FileEntityViewModel>();
                    foreach (var drive in DriveService.GetDrives())
                        newDrives.Add(drive);
                    _cachedDrives.Value.Clear();
                    foreach (var drive in newDrives)
                        _cachedDrives.Value.Add(drive);
                    UpdateDriveUsageStats();
                    LoadCachedDrives();
                }
            });
        }

        private void OnFileCacheUpdated(object sender, FileCacheEventArgs e)
        {
            if (string.IsNullOrEmpty(e?.FilePath) || _disposed) return;
            if (!_fileMap.TryGetValue(e.FilePath, out var fileToUpdate)) return;

            _dispatcherQueue?.TryEnqueue(() =>
            {
                try
                {
                    if (_disposed) return;
                    var fileInfo = new FileInfo(e.FilePath);
                    if (fileInfo.Exists)
                    {
                        var updatedFile = FileCacheService.GetFileMetadata(fileInfo);
                        var index = _directoriesAndFiles.IndexOf(fileToUpdate);
                        if (index >= 0 && index < _directoriesAndFiles.Count)
                            _directoriesAndFiles[index] = updatedFile;
                    }
                }
                catch
                {
                }
            });
        }

        private void OnDirectoriesAndFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (FileViewModel item in e.NewItems.OfType<FileViewModel>())
                    _fileMap[item.FullName] = item;
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (FileViewModel item in e.OldItems.OfType<FileViewModel>())
                    _fileMap.Remove(item.FullName);
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace && e.NewItems != null && e.OldItems != null)
            {
                foreach (FileViewModel oldItem in e.OldItems.OfType<FileViewModel>())
                    _fileMap.Remove(oldItem.FullName);
                foreach (FileViewModel newItem in e.NewItems.OfType<FileViewModel>())
                    _fileMap[newItem.FullName] = newItem;
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _fileMap.Clear();
                foreach (FileViewModel item in _directoriesAndFiles.OfType<FileViewModel>())
                    _fileMap[item.FullName] = item;
            }
        }

        private bool OnCanMoveBack(object obj) => _history?.CanMoveBack ?? false;
        private void OnMoveBack(object obj) => _history?.MoveBack();
        private bool OnCanMoveForward(object obj) => _history?.CanMoveForward ?? false;
        private void OnMoveForward(object obj) => _history?.MoveForward();

        #endregion

        #region Вспомогательные методы

        private void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
                OnPropertyChanged(name);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _loadingCancellation?.Cancel();
            _loadingCancellation?.Dispose();
            _metricsUpdateTimer?.Stop();
            _historyUpdateTimer?.Stop();
            _history.HistoryChanged -= OnHistoryChanged;
            DriveService.DrivesUpdated -= OnDrivesUpdated;
            PerformanceManager.MetricsUpdated -= OnPerformanceMetricsUpdated;
            FileCacheService.CacheUpdated -= OnFileCacheUpdated;
            _directoriesAndFiles.CollectionChanged -= OnDirectoriesAndFilesCollectionChanged;
            PerformanceManager.Dispose();
            _disposed = true;
        }

        #endregion
    }

    public class BatchObservableCollection<T> : ObservableCollection<T>
    {
        private bool _notificationsSuspended;

        public void SuspendNotifications() => _notificationsSuspended = true;

        public void ResumeNotifications()
        {
            _notificationsSuspended = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddRange(IEnumerable<T> items)
        {
            SuspendNotifications();
            try
            {
                foreach (var item in items)
                    Add(item);
            }
            finally
            {
                ResumeNotifications();
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_notificationsSuspended)
                base.OnCollectionChanged(e);
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