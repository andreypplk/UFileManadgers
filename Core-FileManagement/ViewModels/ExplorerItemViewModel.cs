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
//        private readonly SemaphoreSlim _loadingSemaphore = new SemaphoreSlim(1, 1);
//        private bool _isLoadingInProgress = false;

//        // Поля для редактирования (ДОБАВЛЕНО)
//        private bool _isEditing = false;
//        private string _originalName = "";
//        private bool _editRequested = false;
//        private string _filePath;
//        private string _name;
//        private string _newNameForEdit = "";

//        //04 11 2025
//        public bool HasChildren { get; set; }
//        //25 11 2025
//        public bool IsTreeViewNode { get; set; }
//        // 29 11 2025
//        public bool IsSpecialFolderNode { get; set; }
//        #endregion

//        #region Свойства навигации

//        public string FilePath
//        {
//            get => _filePath;
//            set
//            {
//                if (_filePath != value)
//                {
//                    _filePath = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public string Name
//        {
//            get => _name;
//            set
//            {
//                if (_name != value)
//                {
//                    _name = value;
//                    OnPropertyChanged();
//                    // Обновляем команду сохранения при изменении имени
//                    SaveEditCommand?.RaiseCanExecuteChanged();
//                }
//            }
//        }

//        public ObservableCollection<FileEntityViewModel> DirectoriesAndFiles => _directoriesAndFiles;
//        public FileEntityViewModel SelectedFileEntity { get; set; }
//        public BitmapImage ImageSource { get; set; }
//        public bool IsMyComputer => FilePath == "Мой Компьютер" || Name == "Мой Компьютер";

//        #endregion

//        #region Свойства редактирования (ДОБАВЛЕНО)

//        public bool IsEditing
//        {
//            get => _isEditing;
//            set
//            {
//                if (_isEditing != value)
//                {
//                    _isEditing = value;
//                    OnPropertyChanged();

//                    // При начале редактирования сохраняем оригинальное имя и сбрасываем временное
//                    if (value)
//                    {
//                        _originalName = Name;
//                        NewNameForEdit = Name; // Инициализируем временное свойство
//                        Debug.WriteLine($"[ExplorerItemViewModel] Начато редактирование: {_originalName}, FullPath: {FilePath}");
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[ExplorerItemViewModel] Редактирование завершено");
//                    }
//                }
//            }
//        }
//        public string NewNameForEdit
//        {
//            get => _newNameForEdit;
//            set
//            {
//                if (_newNameForEdit != value)
//                {
//                    _newNameForEdit = value;
//                    OnPropertyChanged();
//                    // Обновляем команду сохранения при изменении временного имени
//                    SaveEditCommand?.RaiseCanExecuteChanged();
//                }
//            }
//        }
//        public bool EditRequested
//        {
//            get => _editRequested;
//            set
//            {
//                if (_editRequested != value)
//                {
//                    _editRequested = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public EntityFlags Flags { get; set; } = EntityFlags.None;

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

//        // Команды для редактирования (ДОБАВЛЕНО)
//        public DelegateCommand StartEditCommand { get; private set; }
//        public DelegateCommand SaveEditCommand { get; private set; }
//        public DelegateCommand CancelEditCommand { get; private set; }

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

//            // Команды для редактирования (ДОБАВЛЕНО)
//            StartEditCommand = new DelegateCommand(OnStartEdit, CanStartEdit);
//            SaveEditCommand = new DelegateCommand(OnSaveEdit, CanSaveEdit);
//            CancelEditCommand = new DelegateCommand(OnCancelEdit);
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
//            _history.Add(path, name);
//        }

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

//        #region Методы редактирования (ИСПРАВЛЕНО)

//        private bool CanStartEdit(object parameter)
//        {
//            // Проверяем, можно ли редактировать этот элемент
//            return !IsEditing &&
//                   !string.IsNullOrEmpty(FilePath) &&
//                   !IsMyComputer &&
//                   !IsTreeViewNode &&
//                   !IsSpecialFolderNode &&
//                   (File.Exists(FilePath) || Directory.Exists(FilePath));
//        }

//        private void OnStartEdit(object parameter)
//        {
//            Debug.WriteLine($"[ExplorerItemViewModel] Запрос на редактирование: {Name}");
//            IsEditing = true;
//            EditRequested = true;
//            _originalName = Name;
//            NewNameForEdit = Name; // Инициализируем временное свойство
//        }

//        private bool CanSaveEdit(object parameter)
//        {
//            // Можно сохранять если идет редактирование и временное имя не пустое и изменилось
//            bool canSave = IsEditing &&
//                           !string.IsNullOrEmpty(NewNameForEdit?.Trim()) &&
//                           NewNameForEdit.Trim() != _originalName;

//            Debug.WriteLine($"[ExplorerItemViewModel] CanSaveEdit: IsEditing={IsEditing}, " +
//                           $"NewNameForEdit='{NewNameForEdit}', Original='{_originalName}', Result={canSave}");

//            return canSave;
//        }


//        private async void OnSaveEdit(object parameter)
//        {
//            try
//            {
//                string newName = NewNameForEdit?.Trim() ?? "";

//                Debug.WriteLine($"[ExplorerItemViewModel] OnSaveEdit called:");
//                Debug.WriteLine($"  Original name: {_originalName}");
//                Debug.WriteLine($"  New name from NewNameForEdit: {newName}");
//                Debug.WriteLine($"  Full path: {FilePath}");

//                if (string.IsNullOrEmpty(newName))
//                {
//                    Debug.WriteLine("[ExplorerItemViewModel] Имя не может быть пустым");
//                    CancelEdit();
//                    return;
//                }

//                if (newName == _originalName)
//                {
//                    Debug.WriteLine("[ExplorerItemViewModel] Имя не изменилось");
//                    CancelEdit();
//                    return;
//                }

//                bool success = false;
//                string newPath = "";
//                string directory = Path.GetDirectoryName(FilePath);

//                // Проверяем существование файла или папки
//                if (File.Exists(FilePath))
//                {
//                    // Переименование файла
//                    newPath = Path.Combine(directory, newName);
//                    Debug.WriteLine($"[ExplorerItemViewModel] Новый путь для файла: {newPath}");
//                    success = await RenameFileAsync(FilePath, newPath);
//                }
//                else if (Directory.Exists(FilePath))
//                {
//                    // Переименование папки
//                    newPath = Path.Combine(directory, newName);
//                    Debug.WriteLine($"[ExplorerItemViewModel] Новый путь для папки: {newPath}");
//                    success = await RenameDirectoryAsync(FilePath, newPath);
//                }
//                else
//                {
//                    Debug.WriteLine($"[ExplorerItemViewModel] Элемент не найден: {FilePath}");
//                    CancelEdit();
//                    return;
//                }

//                if (success)
//                {
//                    Debug.WriteLine($"[ExplorerItemViewModel] Успешно переименовано!");

//                    // Обновляем основное свойство Name
//                    Name = newName;

//                    // ВАЖНО: Обновляем путь после переименования
//                    FilePath = newPath;

//                    IsEditing = false;
//                    EditRequested = false;

//                    // Сбрасываем временное свойство
//                    NewNameForEdit = "";

//                    // Инвалидируем кэш родительской директории для обновления отображения
//                    if (!string.IsNullOrEmpty(directory))
//                    {
//                        Debug.WriteLine($"[ExplorerItemViewModel] Инвалидируем кэш для: {directory}");
//                        DirectoryCacheService.InvalidateCache(directory);

//                        // Перезагружаем содержимое текущей директории
//                        await LoadDirectoryContentAsync();
//                    }

//                    Debug.WriteLine($"[ExplorerItemViewModel] Переименование завершено успешно");
//                }
//                else
//                {
//                    Debug.WriteLine("[ExplorerItemViewModel] Ошибка при переименовании");
//                    // Восстанавливаем временное имя
//                    NewNameForEdit = _originalName;
//                    CancelEdit();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[ExplorerItemViewModel] Ошибка при сохранении редактирования: {ex.Message}");
//                CancelEdit();
//            }
//        }

//        private void OnCancelEdit(object parameter)
//        {
//            CancelEdit();
//        }

//        public void CancelEdit()
//        {
//            // Восстанавливаем оригинальное имя во временном свойстве
//            if (!string.IsNullOrEmpty(_originalName))
//            {
//                NewNameForEdit = _originalName;
//            }

//            // Если нужно, также восстанавливаем основное свойство
//            if (!string.IsNullOrEmpty(_originalName) && Name != _originalName)
//            {
//                Name = _originalName;
//            }

//            IsEditing = false;
//            EditRequested = false;
//            NewNameForEdit = ""; // Сбрасываем временное свойство

//            Debug.WriteLine("[ExplorerItemViewModel] Редактирование отменено");
//        }

//        private async Task<bool> RenameFileAsync(string oldPath, string newPath)
//        {
//            try
//            {
//                Debug.WriteLine($"[ExplorerItemViewModel] RenameFileAsync:");
//                Debug.WriteLine($"  Старый путь: {oldPath}");
//                Debug.WriteLine($"  Новый путь: {newPath}");

//                var fileInfo = new FileInfo(oldPath);
//                if (!fileInfo.Exists)
//                {
//                    Debug.WriteLine($"  ERROR: Исходный файл не существует: {oldPath}");
//                    return false;
//                }

//                // Проверяем, существует ли файл с таким именем
//                if (File.Exists(newPath))
//                {
//                    Debug.WriteLine($"  ERROR: Файл '{Path.GetFileName(newPath)}' уже существует");
//                    return false;
//                }

//                // Выполняем переименование
//                await Task.Run(() =>
//                {
//                    fileInfo.MoveTo(newPath);
//                });

//                bool fileRenamed = File.Exists(newPath) && !File.Exists(oldPath);
//                Debug.WriteLine($"  Файл переименован успешно: {fileRenamed}");

//                return fileRenamed;
//            }
//            catch (UnauthorizedAccessException ex)
//            {
//                Debug.WriteLine($"  Нет прав для переименования файла: {ex.Message}");
//                return false;
//            }
//            catch (IOException ex)
//            {
//                Debug.WriteLine($"  Ошибка ввода-вывода при переименовании файла: {ex.Message}");
//                return false;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"  Ошибка при переименовании файла: {ex.Message}");
//                return false;
//            }
//        }

//        private async Task<bool> RenameDirectoryAsync(string oldPath, string newPath)
//        {
//            try
//            {
//                Debug.WriteLine($"[ExplorerItemViewModel] RenameDirectoryAsync:");
//                Debug.WriteLine($"  Старый путь: {oldPath}");
//                Debug.WriteLine($"  Новый путь: {newPath}");

//                // Проверяем, существует ли папка с таким именем
//                if (Directory.Exists(newPath))
//                {
//                    Debug.WriteLine($"  Папка '{Path.GetFileName(newPath)}' уже существует");
//                    return false;
//                }

//                await Task.Run(() => Directory.Move(oldPath, newPath));

//                bool directoryRenamed = Directory.Exists(newPath) && !Directory.Exists(oldPath);
//                Debug.WriteLine($"  Папка переименована успешно: {directoryRenamed}");

//                return directoryRenamed;
//            }
//            catch (UnauthorizedAccessException ex)
//            {
//                Debug.WriteLine($"  Нет прав для переименования папки: {ex.Message}");
//                return false;
//            }
//            catch (IOException ex)
//            {
//                Debug.WriteLine($"  Ошибка ввода-вывода при переименовании папки: {ex.Message}");
//                return false;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"  Ошибка при переименовании папки: {ex.Message}");
//                return false;
//            }
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

//using System;
//using System.Collections.ObjectModel;
//using System.Collections.Specialized;
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
//        private readonly SemaphoreSlim _loadingSemaphore = new SemaphoreSlim(1, 1);
//        private bool _isLoadingInProgress = false;

//        // Словарь для быстрого доступа к файлам
//        private readonly Dictionary<string, FileViewModel> _fileMap = new(StringComparer.OrdinalIgnoreCase);

//        // Дебаунсинг для производительности
//        private DispatcherQueueTimer _metricsUpdateTimer;
//        private PerformanceMetrics _pendingMetrics;
//        private DispatcherQueueTimer _historyUpdateTimer;
//        private bool _historyUpdatePending;

//        // Кэшированные агрегаты дисков
//        private int _cachedUsedPercent;
//        private string _cachedUsedSpace;
//        private string _cachedFreeSpace;
//        private string _cachedTotalSize;

//        // Поля для редактирования
//        private bool _isEditing = false;
//        private string _originalName = "";
//        private bool _editRequested = false;
//        private string _filePath;
//        private string _name;
//        private string _newNameForEdit = "";

//        public bool HasChildren { get; set; }
//        public bool IsTreeViewNode { get; set; }
//        public bool IsSpecialFolderNode { get; set; }
//        #endregion

//        #region Свойства навигации

//        public string FilePath
//        {
//            get => _filePath;
//            set
//            {
//                if (_filePath != value)
//                {
//                    _filePath = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public string Name
//        {
//            get => _name;
//            set
//            {
//                if (_name != value)
//                {
//                    _name = value;
//                    OnPropertyChanged();
//                    SaveEditCommand?.RaiseCanExecuteChanged();
//                }
//            }
//        }

//        public ObservableCollection<FileEntityViewModel> DirectoriesAndFiles => _directoriesAndFiles;
//        public FileEntityViewModel SelectedFileEntity { get; set; }
//        public BitmapImage ImageSource { get; set; }
//        public bool IsMyComputer => FilePath == "Мой Компьютер" || Name == "Мой Компьютер";

//        #endregion

//        #region Свойства редактирования

//        public bool IsEditing
//        {
//            get => _isEditing;
//            set
//            {
//                if (_isEditing != value)
//                {
//                    _isEditing = value;
//                    OnPropertyChanged();
//                    if (value)
//                    {
//                        _originalName = Name;
//                        NewNameForEdit = Name;
//                        Debug.WriteLine($"[ExplorerItemViewModel] Начато редактирование: {_originalName}, FullPath: {FilePath}");
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[ExplorerItemViewModel] Редактирование завершено");
//                    }
//                }
//            }
//        }
//        public string NewNameForEdit
//        {
//            get => _newNameForEdit;
//            set
//            {
//                if (_newNameForEdit != value)
//                {
//                    _newNameForEdit = value;
//                    OnPropertyChanged();
//                    SaveEditCommand?.RaiseCanExecuteChanged();
//                }
//            }
//        }
//        public bool EditRequested
//        {
//            get => _editRequested;
//            set
//            {
//                if (_editRequested != value)
//                {
//                    _editRequested = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public EntityFlags Flags { get; set; } = EntityFlags.None;

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
//        public DelegateCommand StartEditCommand { get; private set; }
//        public DelegateCommand SaveEditCommand { get; private set; }
//        public DelegateCommand CancelEditCommand { get; private set; }

//        #endregion

//        #region Конструктор и инициализация

//        public ExplorerItemViewModel(IDirectoryHistory history)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                _history = history ?? throw new ArgumentNullException(nameof(history));

//                _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
//                if (_dispatcherQueue == null)
//                {
//                    _dispatcherQueueController = DispatcherQueueController.CreateOnCurrentThread();
//                    _dispatcherQueue = _dispatcherQueueController.DispatcherQueue;
//                    Debug.WriteLine("Создан новый DispatcherQueueController");
//                }

//                _directoriesAndFiles = new BatchObservableCollection<FileEntityViewModel>();
//                _directoriesAndFiles.CollectionChanged += OnDirectoriesAndFilesCollectionChanged;

//                InitializeCommands();
//                InitializeMetrics();
//                UpdateFromHistory();
//                SubscribeToEvents();
//                _ = InitializePerformanceManagerAsync();
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.ctor] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void InitializeCommands()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                OpenCommand = new DelegateCommand(Open);
//                MoveBackCommand = new DelegateCommand(OnMoveBack, OnCanMoveBack);
//                MoveForwardCommand = new DelegateCommand(OnMoveForward, OnCanMoveForward);
//                RefreshCommand = new DelegateCommand(OnRefresh);
//                CancelLoadingCommand = new DelegateCommand(OnCancelLoading, CanCancelLoading);
//                StartEditCommand = new DelegateCommand(OnStartEdit, CanStartEdit);
//                SaveEditCommand = new DelegateCommand(OnSaveEdit, CanSaveEdit);
//                CancelEditCommand = new DelegateCommand(OnCancelEdit);
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.InitializeCommands] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void InitializeMetrics()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                CurrentMetrics = new PerformanceMetrics
//                {
//                    CpuUsage = 0,
//                    IoUsage = 0,
//                    MemoryUsage = 0,
//                    MemoryUsagePercent = 0,
//                    TotalMemory = 16L * 1024 * 1024 * 1024
//                };
//                UpdateStatusBar();
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.InitializeMetrics] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void SubscribeToEvents()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                _history.HistoryChanged += OnHistoryChanged;
//                DriveService.DrivesUpdated += OnDrivesUpdated;
//                FileCacheService.CacheUpdated += OnFileCacheUpdated;
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.SubscribeToEvents] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        #endregion

//        #region Методы работы с производительностью

//        private async Task InitializePerformanceManagerAsync()
//        {
//            var sw = Stopwatch.StartNew();
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
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.InitializePerformanceManagerAsync] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        // Дебаунсинг обновления метрик
//        private void OnPerformanceMetricsUpdated(object sender, PerformanceMetrics metrics)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                _pendingMetrics = metrics;
//                if (_metricsUpdateTimer == null)
//                {
//                    _metricsUpdateTimer = _dispatcherQueue.CreateTimer();
//                    _metricsUpdateTimer.Interval = TimeSpan.FromMilliseconds(500);
//                    _metricsUpdateTimer.Tick += (s, e) =>
//                    {
//                        _metricsUpdateTimer.Stop();
//                        _dispatcherQueue.TryEnqueue(() =>
//                        {
//                            if (_pendingMetrics != null && !_disposed)
//                            {
//                                CurrentMetrics = _pendingMetrics;
//                                UpdateStatusBar();
//                            }
//                        });
//                    };
//                }
//                _metricsUpdateTimer.Start();
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnPerformanceMetricsUpdated] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void UpdateStatusBar()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                if (CurrentMetrics == null) return;
//                CpuUsageText = $"{CurrentMetrics.CpuUsage:0}%";
//                IoUsageText = $"{CurrentMetrics.IoUsage:0.0} MB/s";
//                MemoryUsageText = $"{CurrentMetrics.MemoryUsage / (1024 * 1024):0} MB / {CurrentMetrics.TotalMemory / (1024 * 1024):0} MB";
//                CpuUsagePercent = CurrentMetrics.CpuUsage;
//                MemoryUsagePercent = CurrentMetrics.MemoryUsagePercent;
//                OnPropertiesChanged(nameof(CpuUsageText), nameof(IoUsageText), nameof(MemoryUsageText), nameof(CpuUsagePercent), nameof(MemoryUsagePercent));
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.UpdateStatusBar] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        #endregion

//        #region Методы работы с файловой системой

//        public async Task OpenDirectoryAsync()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                await _loadingSemaphore.WaitAsync();
//                try
//                {
//                    if (_isLoadingInProgress)
//                    {
//                        Debug.WriteLine("OpenDirectoryAsync already in progress, skipping");
//                        return;
//                    }
//                    _isLoadingInProgress = true;
//                }
//                finally
//                {
//                    _loadingSemaphore.Release();
//                }

//                try
//                {
//                    IsLoading = true;
//                    _directoriesAndFiles.Clear();
//                    _fileMap.Clear(); // Очищаем словарь при смене папки

//                    if (IsMyComputer)
//                    {
//                        LoadCachedDrives();
//                    }
//                    else
//                    {
//                        await LoadDirectoryContentAsync();
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Ошибка открытия директории: {ex.Message}");
//                }
//                finally
//                {
//                    IsLoading = false;

//                    await _loadingSemaphore.WaitAsync();
//                    try
//                    {
//                        _isLoadingInProgress = false;
//                    }
//                    finally
//                    {
//                        _loadingSemaphore.Release();
//                    }
//                }
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OpenDirectoryAsync] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void UpdateFromHistory()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                var current = _history.Current;
//                if (!IsTreeViewNode && !IsSpecialFolderNode)
//                {
//                    FilePath = current.DirectoryPath;
//                    Name = current.DirectoryPathName;
//                }

//                if (IsMyComputer)
//                {
//                    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Computer.png"));
//                    LoadCachedDrives();
//                }
//                else if (!IsTreeViewNode && !IsSpecialFolderNode)
//                {
//                    ImageSource = null;
//                    _ = LoadDirectoryContentAsync();
//                }

//                OnPropertiesChanged(nameof(FilePath), nameof(Name), nameof(ImageSource));
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.UpdateFromHistory] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private static ObservableCollection<FileEntityViewModel> LoadDrives()
//        {
//            var sw = Stopwatch.StartNew();
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
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.LoadDrives] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void LoadCachedDrives()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                _directoriesAndFiles.SuspendNotifications();
//                try
//                {
//                    _directoriesAndFiles.Clear();
//                    _directoriesAndFiles.AddRange(_cachedDrives.Value);
//                }
//                finally
//                {
//                    _directoriesAndFiles.ResumeNotifications();
//                }
//                UpdateDriveUsageStats();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка загрузки кэшированных дисков: {ex.Message}");
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.LoadCachedDrives] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void UpdateDriveUsageStats()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                if (!IsMyComputer || !_cachedDrives.Value.Any()) return;

//                // Используем кэшированные значения
//                var drives = _cachedDrives.Value.OfType<DriveViewModel>().Where(d => !d.IsUnavailable).ToList();
//                if (drives.Any())
//                {
//                    _cachedUsedPercent = (int)drives.Average(d => d.UsedProcentValue);
//                    var rep = drives.FirstOrDefault();
//                    if (rep != null)
//                    {
//                        _cachedUsedSpace = rep.UsedSpaceString;
//                        _cachedFreeSpace = rep.FreeSpaceString;
//                        _cachedTotalSize = rep.TotalSizeString;
//                    }
//                }
//                else
//                {
//                    _cachedUsedPercent = 0;
//                    _cachedUsedSpace = _cachedFreeSpace = _cachedTotalSize = "";
//                }

//                UsedProcentValue = _cachedUsedPercent;
//                UsedSpaceString = _cachedUsedSpace;
//                FreeSpaceString = _cachedFreeSpace;
//                TotalSizeString = _cachedTotalSize;
//                OnPropertiesChanged(nameof(UsedSpaceString), nameof(FreeSpaceString), nameof(TotalSizeString));
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка в UpdateDriveUsageStats: {ex.Message}");
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.UpdateDriveUsageStats] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private async Task LoadDirectoryContentAsync()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                _loadingCancellation?.Cancel();
//                _loadingCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

//                IsLoading = true;
//                _directoriesAndFiles.Clear();
//                _fileMap.Clear();

//                var token = _loadingCancellation.Token;
//                var directoryItems = await DirectoryCacheService.GetDirectoryContentAsync(FilePath, token);

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
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.LoadDirectoryContentAsync] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private async Task AddItemsToUiAsync(List<FileEntityViewModel> directoryItems, CancellationToken token)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                // Сортировка и добавление
//                await _dispatcherQueue.EnqueueAsync(() =>
//                {
//                    _directoriesAndFiles.SuspendNotifications();
//                    try
//                    {
//                        var sorted = directoryItems.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList();
//                        _directoriesAndFiles.AddRange(sorted);
//                    }
//                    finally
//                    {
//                        _directoriesAndFiles.ResumeNotifications();
//                    }
//                });

//                // Загрузка иконок с группировкой
//                await LoadIconsGroupedAsync(directoryItems.OfType<FileViewModel>().ToList(), token);
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.AddItemsToUiAsync] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        // Группированная загрузка иконок – одно обновление UI
//        private async Task LoadIconsGroupedAsync(List<FileViewModel> files, CancellationToken token)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                var takeCount = Math.Min(20, files.Count);
//                var tasks = files.Take(takeCount).Select(async file =>
//                {
//                    if (token.IsCancellationRequested) return (file: (FileViewModel)null, icon: (BitmapImage)null);
//                    try
//                    {
//                        var icon = await FileCacheService.GetFileIconAsync(file.FullName);
//                        return (file, icon);
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"Ошибка загрузки иконки: {ex.Message}");
//                        return (file: null, icon: null);
//                    }
//                }).ToList();

//                var results = await Task.WhenAll(tasks);
//                var updates = results.Where(r => r.file != null && r.icon != null).ToList();
//                if (updates.Count == 0) return;

//                await _dispatcherQueue.EnqueueAsync(() =>
//                {
//                    foreach (var (file, icon) in updates)
//                    {
//                        if (!token.IsCancellationRequested && file != null)
//                            file.ImageSource = icon;
//                    }
//                });
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.LoadIconsGroupedAsync] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void Open(object parameter)
//        {
//            var sw = Stopwatch.StartNew();
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
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.Open] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void NavigateTo(string path, string name)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                _history.Add(path, name);
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.NavigateTo] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private async void OnRefresh(object parameter)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                // Инвалидируем кэш структуры папок
//                DirectoryCacheService.InvalidateCache(FilePath);

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
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnRefresh] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void OnCancelLoading(object parameter)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                _loadingCancellation?.Cancel();
//                IsLoading = false;
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnCancelLoading] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private bool CanCancelLoading(object parameter)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                return _loadingCancellation != null &&
//                       !_loadingCancellation.IsCancellationRequested &&
//                       IsLoading;
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.CanCancelLoading] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        #endregion

//        #region Методы редактирования

//        private bool CanStartEdit(object parameter)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                return !IsEditing &&
//                       !string.IsNullOrEmpty(FilePath) &&
//                       !IsMyComputer &&
//                       !IsTreeViewNode &&
//                       !IsSpecialFolderNode &&
//                       (File.Exists(FilePath) || Directory.Exists(FilePath));
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.CanStartEdit] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void OnStartEdit(object parameter)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                Debug.WriteLine($"[ExplorerItemViewModel] Запрос на редактирование: {Name}");
//                IsEditing = true;
//                EditRequested = true;
//                _originalName = Name;
//                NewNameForEdit = Name;
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnStartEdit] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private bool CanSaveEdit(object parameter)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                bool canSave = IsEditing &&
//                               !string.IsNullOrEmpty(NewNameForEdit?.Trim()) &&
//                               NewNameForEdit.Trim() != _originalName;
//                Debug.WriteLine($"[ExplorerItemViewModel] CanSaveEdit: {canSave}");
//                return canSave;
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.CanSaveEdit] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private async void OnSaveEdit(object parameter)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                string newName = NewNameForEdit?.Trim() ?? "";
//                if (string.IsNullOrEmpty(newName) || newName == _originalName)
//                {
//                    CancelEdit();
//                    return;
//                }

//                bool success = false;
//                string newPath = "";
//                string directory = Path.GetDirectoryName(FilePath);

//                if (File.Exists(FilePath))
//                {
//                    newPath = Path.Combine(directory, newName);
//                    success = await RenameFileAsync(FilePath, newPath);
//                }
//                else if (Directory.Exists(FilePath))
//                {
//                    newPath = Path.Combine(directory, newName);
//                    success = await RenameDirectoryAsync(FilePath, newPath);
//                }
//                else
//                {
//                    CancelEdit();
//                    return;
//                }

//                if (success)
//                {
//                    Name = newName;
//                    FilePath = newPath;
//                    IsEditing = false;
//                    EditRequested = false;
//                    NewNameForEdit = "";

//                    if (!string.IsNullOrEmpty(directory))
//                    {
//                        DirectoryCacheService.InvalidateCache(directory);
//                        await LoadDirectoryContentAsync();
//                    }
//                }
//                else
//                {
//                    NewNameForEdit = _originalName;
//                    CancelEdit();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[ExplorerItemViewModel] Ошибка при сохранении редактирования: {ex.Message}");
//                CancelEdit();
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnSaveEdit] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void OnCancelEdit(object parameter) => CancelEdit();

//        public void CancelEdit()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                if (!string.IsNullOrEmpty(_originalName))
//                {
//                    NewNameForEdit = _originalName;
//                    if (Name != _originalName) Name = _originalName;
//                }
//                IsEditing = false;
//                EditRequested = false;
//                NewNameForEdit = "";
//                Debug.WriteLine("[ExplorerItemViewModel] Редактирование отменено");
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.CancelEdit] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private async Task<bool> RenameFileAsync(string oldPath, string newPath)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                var fileInfo = new FileInfo(oldPath);
//                if (!fileInfo.Exists) return false;
//                if (File.Exists(newPath)) return false;

//                await Task.Run(() => fileInfo.MoveTo(newPath));
//                return File.Exists(newPath) && !File.Exists(oldPath);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка переименования файла: {ex.Message}");
//                return false;
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.RenameFileAsync] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private async Task<bool> RenameDirectoryAsync(string oldPath, string newPath)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                if (Directory.Exists(newPath)) return false;
//                await Task.Run(() => Directory.Move(oldPath, newPath));
//                return Directory.Exists(newPath) && !Directory.Exists(oldPath);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка переименования папки: {ex.Message}");
//                return false;
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.RenameDirectoryAsync] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        #endregion

//        #region Обработчики событий

//        private void OnHistoryChanged(object sender, EventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                // Дебаунсинг
//                if (_historyUpdateTimer == null)
//                {
//                    _historyUpdateTimer = _dispatcherQueue.CreateTimer();
//                    _historyUpdateTimer.Interval = TimeSpan.FromMilliseconds(200);
//                    _historyUpdateTimer.Tick += (s, ev) =>
//                    {
//                        _historyUpdateTimer.Stop();
//                        _dispatcherQueue.TryEnqueue(() =>
//                        {
//                            MoveBackCommand?.RaiseCanExecuteChanged();
//                            MoveForwardCommand?.RaiseCanExecuteChanged();
//                            UpdateFromHistory();
//                        });
//                    };
//                }
//                _historyUpdateTimer.Start();
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnHistoryChanged] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void OnDrivesUpdated(object sender, EventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                _dispatcherQueue?.TryEnqueue(() =>
//                {
//                    if (IsMyComputer && !_disposed)
//                    {
//                        var newDrives = new ObservableCollection<FileEntityViewModel>();
//                        foreach (var drive in DriveService.GetDrives())
//                            newDrives.Add(drive);
//                        _cachedDrives.Value.Clear();
//                        foreach (var drive in newDrives)
//                            _cachedDrives.Value.Add(drive);
//                        UpdateDriveUsageStats();
//                        LoadCachedDrives();
//                    }
//                });
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnDrivesUpdated] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void OnFileCacheUpdated(object sender, FileCacheEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                if (string.IsNullOrEmpty(e?.FilePath) || _disposed) return;
//                if (!_fileMap.TryGetValue(e.FilePath, out var fileToUpdate)) return;

//                _dispatcherQueue?.TryEnqueue(() =>
//                {
//                    try
//                    {
//                        if (_disposed) return;
//                        var fileInfo = new FileInfo(e.FilePath);
//                        if (fileInfo.Exists)
//                        {
//                            var updatedFile = FileCacheService.GetFileMetadata(fileInfo);
//                            var index = _directoriesAndFiles.IndexOf(fileToUpdate);
//                            if (index >= 0 && index < _directoriesAndFiles.Count)
//                                _directoriesAndFiles[index] = updatedFile;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"Ошибка обновления кэша файлов: {ex.Message}");
//                    }
//                });
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnFileCacheUpdated] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private void OnDirectoriesAndFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
//                {
//                    foreach (FileViewModel item in e.NewItems.OfType<FileViewModel>())
//                        _fileMap[item.FullName] = item;
//                }
//                else if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
//                {
//                    foreach (FileViewModel item in e.OldItems.OfType<FileViewModel>())
//                        _fileMap.Remove(item.FullName);
//                }
//                else if (e.Action == NotifyCollectionChangedAction.Replace && e.NewItems != null && e.OldItems != null)
//                {
//                    foreach (FileViewModel oldItem in e.OldItems.OfType<FileViewModel>())
//                        _fileMap.Remove(oldItem.FullName);
//                    foreach (FileViewModel newItem in e.NewItems.OfType<FileViewModel>())
//                        _fileMap[newItem.FullName] = newItem;
//                }
//                else if (e.Action == NotifyCollectionChangedAction.Reset)
//                {
//                    _fileMap.Clear();
//                    foreach (FileViewModel item in _directoriesAndFiles.OfType<FileViewModel>())
//                        _fileMap[item.FullName] = item;
//                }
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnDirectoriesAndFilesCollectionChanged] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        private bool OnCanMoveBack(object obj) => _history?.CanMoveBack ?? false;
//        private void OnMoveBack(object obj) => _history?.MoveBack();
//        private bool OnCanMoveForward(object obj) => _history?.CanMoveForward ?? false;
//        private void OnMoveForward(object obj) => _history?.MoveForward();

//        #endregion

//        #region Вспомогательные методы

//        private void OnPropertiesChanged(params string[] propertyNames)
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                foreach (var name in propertyNames)
//                    OnPropertyChanged(name);
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.OnPropertiesChanged] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
//        }

//        #endregion

//        #region IDisposable

//        public void Dispose()
//        {
//            var sw = Stopwatch.StartNew();
//            try
//            {
//                if (_disposed) return;
//                _loadingCancellation?.Cancel();
//                _loadingCancellation?.Dispose();
//                _metricsUpdateTimer?.Stop();
//                _historyUpdateTimer?.Stop();
//                _history.HistoryChanged -= OnHistoryChanged;
//                DriveService.DrivesUpdated -= OnDrivesUpdated;
//                PerformanceManager.MetricsUpdated -= OnPerformanceMetricsUpdated;
//                FileCacheService.CacheUpdated -= OnFileCacheUpdated;
//                _directoriesAndFiles.CollectionChanged -= OnDirectoriesAndFilesCollectionChanged;
//                PerformanceManager.Dispose();
//                _disposed = true;
//            }
//            finally
//            {
//                sw.Stop();
//                Debug.WriteLine($"[ExplorerItemViewModel.Dispose] elapsed: {sw.Elapsed.TotalMilliseconds:F2} ms");
//            }
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
//            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
//        }

//        public void AddRange(IEnumerable<T> items)
//        {
//            SuspendNotifications();
//            try
//            {
//                foreach (var item in items)
//                    Add(item);
//            }
//            finally
//            {
//                ResumeNotifications();
//            }
//        }

//        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
//        {
//            if (!_notificationsSuspended)
//                base.OnCollectionChanged(e);
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
        public BitmapImage ImageSource { get; set; }
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации PerformanceManager: {ex}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка открытия директории: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки дисков: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки кэшированных дисков: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в UpdateDriveUsageStats: {ex.Message}");
            }
        }

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
                var directoryItems = await DirectoryCacheService.GetDirectoryContentAsync(FilePath, token);

                await AddItemsToUiAsync(directoryItems, token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки директории: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddItemsToUiAsync(List<FileEntityViewModel> directoryItems, CancellationToken token)
        {
            try
            {
                await _dispatcherQueue.EnqueueAsync(() =>
                {
                    _directoriesAndFiles.SuspendNotifications();
                    try
                    {
                        var sorted = directoryItems.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToList();
                        _directoriesAndFiles.AddRange(sorted);
                    }
                    finally
                    {
                        _directoriesAndFiles.ResumeNotifications();
                    }
                });

                await LoadIconsGroupedAsync(directoryItems.OfType<FileViewModel>().ToList(), token);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка добавления элементов в UI: {ex.Message}");
            }
        }

        private async Task LoadIconsGroupedAsync(List<FileViewModel> files, CancellationToken token)
        {
            try
            {
                var takeCount = Math.Min(20, files.Count);
                var tasks = files.Take(takeCount).Select(async file =>
                {
                    if (token.IsCancellationRequested) return (file: (FileViewModel)null, icon: (BitmapImage)null);
                    try
                    {
                        var icon = await FileCacheService.GetFileIconAsync(file.FullName);
                        return (file, icon);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки иконки: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка группированной загрузки иконок: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка открытия элемента: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении редактирования: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка переименования файла: {ex.Message}");
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка переименования папки: {ex.Message}");
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
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка обновления кэша файлов: {ex.Message}");
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