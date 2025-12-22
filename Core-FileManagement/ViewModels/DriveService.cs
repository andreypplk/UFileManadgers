//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Management;
//using System.Threading;

//namespace Core_FileManagement
//{
//    public static class DriveService
//    {
//        private static List<DriveViewModel> _cachedDrives = new();
//        private static DateTime _lastRefreshTime = DateTime.MinValue;
//        private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
//        private static readonly object _syncLock = new();
//        private static ManagementEventWatcher _driveWatcher;
//        private static bool _isDisposed = false;

//        public static event EventHandler DrivesUpdated;

//        static DriveService()
//        {
//            InitializeWatcher();
//            RefreshDrives();
//        }

//        public static IReadOnlyList<DriveViewModel> GetDrives()
//        {
//            lock (_syncLock)
//            {
//                if (_isDisposed)
//                    throw new ObjectDisposedException(nameof(DriveService));

//                if (ShouldRefresh())
//                {
//                    RefreshDrives();
//                }
//                return _cachedDrives.ToList().AsReadOnly();
//            }
//        }

//        private static bool ShouldRefresh()
//        {
//            return DateTime.Now - _lastRefreshTime > _cacheExpiry ||
//                   !_cachedDrives.Any();
//        }

//        public static void RefreshDrives()
//        {
//            lock (_syncLock)
//            {
//                if (_isDisposed) return;

//                var newDrives = new List<DriveViewModel>();
//                var drives = DriveInfo.GetDrives();
//                bool hasChanges = false;

//                foreach (var drive in drives.OrderBy(d => d.Name))
//                {
//                    try
//                    {
//                        var flags = EntityFlags.IsDrive;

//                        if (drive.IsReady)
//                        {
//                            if (drive.DriveType == DriveType.Fixed)
//                                flags |= EntityFlags.IsSystem;
//                            if (drive.DriveType == DriveType.Network)
//                                flags |= EntityFlags.IsHidden;

//                            var newDrive = new DriveViewModel(drive, flags);
//                            newDrives.Add(newDrive);

//                            var existingDrive = _cachedDrives.FirstOrDefault(d => d.FullName == drive.Name);
//                            if (existingDrive == null ||
//                                Math.Abs(existingDrive.UsedProcentValue - newDrive.UsedProcentValue) >= 1.0 ||
//                                existingDrive.FreeSpace != newDrive.FreeSpace)
//                            {
//                                hasChanges = true;
//                            }
//                        }
//                        else
//                        {
//                            flags |= EntityFlags.IsUnavailable;
//                            newDrives.Add(new DriveViewModel(drive, flags));
//                            hasChanges = true;
//                        }
//                    }
//                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                    {
//                        Debug.WriteLine($"[DriveService] Error reading {drive.Name}: {ex.Message}");
//                    }
//                }

//                if (hasChanges || !_cachedDrives.Any())
//                {
//                    _cachedDrives = newDrives;
//                    _lastRefreshTime = DateTime.Now;
//                    DrivesUpdated?.Invoke(null, EventArgs.Empty);
//                }
//            }
//        }

//        private static void InitializeWatcher()
//        {
//            try
//            {
//                _driveWatcher = new ManagementEventWatcher(
//                    new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent"));

//                _driveWatcher.EventArrived += (sender, e) =>
//                {
//                    ThreadPool.QueueUserWorkItem(_ =>
//                    {
//                        try
//                        {
//                            lock (_syncLock)
//                            {
//                                if (!_isDisposed)
//                                {
//                                    RefreshDrives();
//                                }
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            Debug.WriteLine($"[DriveService] Watcher callback error: {ex.Message}");
//                        }
//                    });
//                };

//                _driveWatcher.Start();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DriveService] Watcher init failed: {ex.Message}");
//            }
//        }

//        public static void Dispose()
//        {
//            lock (_syncLock)
//            {
//                if (_isDisposed) return;

//                try
//                {
//                    _driveWatcher?.Stop();
//                    _driveWatcher?.Dispose();
//                    _cachedDrives.Clear();
//                    _isDisposed = true;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[DriveService] Dispose error: {ex.Message}");
//                }
//            }
//        }
//    }
//}


//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Management;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Core_FileManagement
//{
//    public static class DriveService
//    {
//        private static List<DriveViewModel> _cachedDrives = new();
//        private static DateTime _lastRefreshTime = DateTime.MinValue;
//        private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
//        private static readonly object _syncLock = new();
//        private static ManagementEventWatcher _driveWatcher;
//        private static bool _isDisposed = false;
//        private static int _refreshInProgress = 0;

//        public static event EventHandler DrivesUpdated;
//        public static Microsoft.UI.Dispatching.DispatcherQueue HostDispatcher { get; set; }


//        static DriveService()
//        {
//            InitializeWatcher();
//            _ = RefreshDrivesAsync(); // Start initial refresh asynchronously
//        }

//        public static IReadOnlyList<DriveViewModel> GetDrives()
//        {
//            lock (_syncLock)
//            {
//                if (_isDisposed)
//                    throw new ObjectDisposedException(nameof(DriveService));

//                if (ShouldRefresh())
//                {
//                    _ = RefreshDrivesAsync(); // Fire-and-forget async refresh
//                }
//                return _cachedDrives.ToList().AsReadOnly();
//            }
//        }

//        private static bool ShouldRefresh()
//        {
//            return DateTime.Now - _lastRefreshTime > _cacheExpiry ||
//                   !_cachedDrives.Any();
//        }

//        public static async Task RefreshDrivesAsync()
//        {
//            // Prevent multiple concurrent refreshes
//            if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0)
//                return;

//            try
//            {
//                await Task.Run(() =>
//                {
//                    lock (_syncLock)
//                    {
//                        if (_isDisposed) return;

//                        var newDrives = new List<DriveViewModel>();
//                        var drives = DriveInfo.GetDrives();
//                        bool hasChanges = false;

//                        // Determine optimal parallelization based on current load
//                        var parallelOptions = new ParallelOptions
//                        {
//                            MaxDegreeOfParallelism = PerformanceManager.CalculateOptimalThreadCount()
//                        };

//                        Parallel.ForEach(drives.OrderBy(d => d.Name), parallelOptions, drive =>
//                        {
//                            try
//                            {
//                                var flags = EntityFlags.IsDrive;
//                                var driveType = StorageDetector.DetectStorageType(drive.Name);

//                                if (drive.IsReady)
//                                {
//                                    if (drive.DriveType == DriveType.Fixed)
//                                        flags |= EntityFlags.IsSystem;
//                                    if (drive.DriveType == DriveType.Network)
//                                        flags |= EntityFlags.IsHidden;

//                                    var newDrive = new DriveViewModel(drive, flags, driveType);
//                                    lock (newDrives)
//                                    {
//                                        newDrives.Add(newDrive);
//                                    }

//                                    // Check for changes without lock for performance
//                                    var existingDrive = _cachedDrives.FirstOrDefault(d => d.FullName == drive.Name);
//                                    if (existingDrive == null ||
//                                        Math.Abs(existingDrive.UsedProcentValue - newDrive.UsedProcentValue) >= 1.0 ||
//                                        existingDrive.FreeSpace != newDrive.FreeSpace)
//                                    {
//                                        hasChanges = true;
//                                    }
//                                }
//                                else
//                                {
//                                    flags |= EntityFlags.IsUnavailable;
//                                    lock (newDrives)
//                                    {
//                                        newDrives.Add(new DriveViewModel(drive, flags, driveType));
//                                    }
//                                    hasChanges = true;
//                                }
//                            }
//                            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                            {
//                                Debug.WriteLine($"[DriveService] Error reading {drive.Name}: {ex.Message}");
//                            }
//                        });

//                        if (hasChanges || !_cachedDrives.Any())
//                        {
//                            _cachedDrives = newDrives.OrderBy(d => d.Name).ToList();
//                            _lastRefreshTime = DateTime.Now;
//                            RaiseDrivesUpdated();
//                        }
//                    }
//                }).ConfigureAwait(false);
//            }
//            finally
//            {
//                Interlocked.Exchange(ref _refreshInProgress, 0);
//            }
//        }

//        private static void InitializeWatcher()
//        {
//            try
//            {
//                _driveWatcher = new ManagementEventWatcher(
//                    new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent"));

//                _driveWatcher.EventArrived += async (sender, e) =>
//                {
//                    // Add delay to avoid multiple rapid refreshes
//                    await Task.Delay(1000);
//                    await RefreshDrivesAsync();
//                };

//                _driveWatcher.Start();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DriveService] Watcher init failed: {ex.Message}");
//            }
//        }
//        private static void RaiseDrivesUpdated()
//        {
//            // У вас должна быть HostDispatcher или аналогичный метод
//            var handler = DrivesUpdated;
//            if (handler == null) return;

//            if (HostDispatcher != null)
//            {
//                if (HostDispatcher.HasThreadAccess)
//                    handler.Invoke(null, EventArgs.Empty);
//                else
//                    HostDispatcher.TryEnqueue(() => handler.Invoke(null, EventArgs.Empty));
//            }
//            else
//            {
//                Debug.WriteLine("[Warning] DrivesUpdated called without dispatcher");
//                handler.Invoke(null, EventArgs.Empty);
//            }
//        }
//        public static async Task DisposeAsync()
//        {
//            lock (_syncLock)
//            {
//                if (_isDisposed) return;

//                try
//                {
//                    _driveWatcher?.Stop();
//                    _driveWatcher?.Dispose();
//                    _cachedDrives.Clear();
//                    _isDisposed = true;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[DriveService] Dispose error: {ex.Message}");
//                    throw;
//                }
//            }

//            // Wait for any pending refresh to complete
//            while (Interlocked.CompareExchange(ref _refreshInProgress, 0, 0) == 1)
//            {
//                await Task.Delay(50);
//            }
//        }

//        public static void Dispose()
//        {
//            DisposeAsync().GetAwaiter().GetResult();
//        }
//    }
//}


//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Management;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Core_FileManagement
//{
//    public static class DriveService
//    {
//        private static List<DriveViewModel> _cachedDrives = new();
//        private static DateTime _lastRefreshTime = DateTime.MinValue;
//        private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
//        private static readonly object _syncLock = new();
//        private static ManagementEventWatcher _driveWatcher;
//        private static bool _isDisposed = false;
//        private static int _refreshInProgress = 0;
//        private static volatile bool _drivesChanged = false;

//        public static event EventHandler DrivesUpdated;
//        public static Microsoft.UI.Dispatching.DispatcherQueue HostDispatcher { get; set; }

//        static DriveService()
//        {
//            InitializeWatcher();
//            _ = RefreshDrivesAsync(); // Start initial refresh asynchronously
//        }

//        public static IReadOnlyList<DriveViewModel> GetDrives()
//        {
//            lock (_syncLock)
//            {
//                if (_isDisposed)
//                    throw new ObjectDisposedException(nameof(DriveService));

//                if (ShouldRefresh())
//                {
//                    _ = RefreshDrivesAsync(); // Fire-and-forget async refresh
//                }
//                return _cachedDrives.ToList().AsReadOnly();
//            }
//        }

//        private static bool ShouldRefresh()
//        {
//            return DateTime.Now - _lastRefreshTime > _cacheExpiry ||
//                   !_cachedDrives.Any() ||
//                   _drivesChanged;
//        }

//        public static async Task RefreshDrivesAsync()
//        {
//            // Предотвращаем множественные одновременные обновления
//            if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0)
//                return;

//            _drivesChanged = false; // Сбрасываем флаг изменений

//            try
//            {
//                await Task.Run(() =>
//                {
//                    lock (_syncLock)
//                    {
//                        if (_isDisposed) return;

//                        var newDrives = new List<DriveViewModel>();
//                        var drives = DriveInfo.GetDrives();
//                        bool hasChanges = false;

//                        // Определяем оптимальное количество потоков один раз
//                        int optimalThreads = PerformanceManager.CalculateOptimalThreadCount();

//                        // Для обновления дисков используем последовательную обработку, 
//                        // так как операции с дисками не масштабируются хорошо
//                        var parallelOptions = new ParallelOptions
//                        {
//                            MaxDegreeOfParallelism = Math.Min(optimalThreads, 4) // Ограничиваем для дисков
//                        };

//                        // Сначала обрабатываем доступные диски
//                        var availableDrives = drives.Where(d => d.IsReady).ToList();
//                        var unavailableDrives = drives.Where(d => !d.IsReady).ToList();

//                        // Обработка доступных дисков
//                        foreach (var drive in availableDrives.OrderBy(d => d.Name))
//                        {
//                            try
//                            {
//                                var flags = EntityFlags.IsDrive;
//                                var driveType = StorageDetector.DetectStorageType(drive.Name);

//                                if (drive.DriveType == DriveType.Fixed)
//                                    flags |= EntityFlags.IsSystem;
//                                if (drive.DriveType == DriveType.Network)
//                                    flags |= EntityFlags.IsHidden;

//                                var newDrive = new DriveViewModel(drive, flags, driveType);
//                                newDrives.Add(newDrive);

//                                // Проверяем изменения без блокировки для производительности
//                                var existingDrive = _cachedDrives.FirstOrDefault(d => d.FullName == drive.Name);
//                                if (existingDrive == null ||
//                                    Math.Abs(existingDrive.UsedProcentValue - newDrive.UsedProcentValue) >= 1.0 ||
//                                    existingDrive.FreeSpace != newDrive.FreeSpace)
//                                {
//                                    hasChanges = true;
//                                }
//                            }
//                            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                            {
//                                Debug.WriteLine($"[DriveService] Error reading {drive.Name}: {ex.Message}");
//                                hasChanges = true; // Считаем ошибку как изменение
//                            }
//                        }

//                        // Обработка недоступных дисков
//                        foreach (var drive in unavailableDrives.OrderBy(d => d.Name))
//                        {
//                            try
//                            {
//                                var flags = EntityFlags.IsDrive | EntityFlags.IsUnavailable;
//                                var driveType = StorageDetector.DetectStorageType(drive.Name);

//                                newDrives.Add(new DriveViewModel(drive, flags, driveType));
//                                hasChanges = true; // Недоступный диск всегда считается изменением
//                            }
//                            catch (Exception ex)
//                            {
//                                Debug.WriteLine($"[DriveService] Error processing unavailable drive {drive.Name}: {ex.Message}");
//                            }
//                        }

//                        if (hasChanges || !_cachedDrives.Any())
//                        {
//                            _cachedDrives = newDrives.OrderBy(d => d.Name).ToList();
//                            _lastRefreshTime = DateTime.Now;
//                            RaiseDrivesUpdated();
//                        }
//                    }
//                }).ConfigureAwait(false);
//            }
//            finally
//            {
//                Interlocked.Exchange(ref _refreshInProgress, 0);
//            }
//        }

//        private static void InitializeWatcher()
//        {
//            try
//            {
//                _driveWatcher = new ManagementEventWatcher(
//                    new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3"));

//                _driveWatcher.EventArrived += async (sender, e) =>
//                {
//                    _drivesChanged = true; // Помечаем, что диски изменились
//                    // Добавляем задержку для избежания множественных обновлений
//                    await Task.Delay(1000);
//                    await RefreshDrivesAsync();
//                };

//                _driveWatcher.Start();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DriveService] Watcher init failed: {ex.Message}");
//            }
//        }

//        private static void RaiseDrivesUpdated()
//        {
//            var handler = DrivesUpdated;
//            if (handler == null) return;

//            if (HostDispatcher != null)
//            {
//                if (HostDispatcher.HasThreadAccess)
//                    handler.Invoke(null, EventArgs.Empty);
//                else
//                    HostDispatcher.TryEnqueue(() => handler.Invoke(null, EventArgs.Empty));
//            }
//            else
//            {
//                Task.Run(() => handler.Invoke(null, EventArgs.Empty));
//            }
//        }

//        public static async Task DisposeAsync()
//        {
//            lock (_syncLock)
//            {
//                if (_isDisposed) return;

//                try
//                {
//                    _driveWatcher?.Stop();
//                    _driveWatcher?.Dispose();
//                    _cachedDrives.Clear();
//                    _isDisposed = true;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[DriveService] Dispose error: {ex.Message}");
//                }
//            }

//            // Ждем завершения любых текущих обновлений
//            while (Interlocked.CompareExchange(ref _refreshInProgress, 0, 0) == 1)
//            {
//                await Task.Delay(50);
//            }
//        }

//        public static void Dispose()
//        {
//            DisposeAsync().GetAwaiter().GetResult();
//        }
//    }
//}


//29 09 2025


//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Management;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Core_FileManagement
//{
//    public static class DriveService
//    {
//        private static List<DriveViewModel> _cachedDrives = new();
//        private static DateTime _lastRefreshTime = DateTime.MinValue;
//        private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
//        private static readonly object _syncLock = new();
//        private static ManagementEventWatcher _driveWatcher;
//        private static bool _isDisposed = false;
//        private static int _refreshInProgress = 0;
//        private static volatile bool _drivesChanged = false;

//        public static event EventHandler DrivesUpdated;
//        public static Microsoft.UI.Dispatching.DispatcherQueue HostDispatcher { get; set; }

//        static DriveService()
//        {
//            InitializeWatcher();
//            _ = RefreshDrivesAsync();
//        }

//        public static IReadOnlyList<DriveViewModel> GetDrives()
//        {
//            lock (_syncLock)
//            {
//                if (_isDisposed)
//                    throw new ObjectDisposedException(nameof(DriveService));

//                if (ShouldRefresh())
//                {
//                    _ = RefreshDrivesAsync();
//                }
//                return _cachedDrives.ToList().AsReadOnly();
//            }
//        }

//        private static bool ShouldRefresh()
//        {
//            return DateTime.Now - _lastRefreshTime > _cacheExpiry ||
//                   !_cachedDrives.Any() ||
//                   _drivesChanged;
//        }

//        public static async Task RefreshDrivesAsync()
//        {
//            if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0)
//                return;

//            _drivesChanged = false;

//            try
//            {
//                await Task.Run(() =>
//                {
//                    lock (_syncLock)
//                    {
//                        if (_isDisposed) return;

//                        var newDrives = new List<DriveViewModel>();
//                        var drives = DriveInfo.GetDrives();
//                        bool hasChanges = false;

//                        // Используем PerformanceManager для оптимального количества потоков
//                        int optimalThreads = PerformanceManager.CalculateOptimalThreadCount();
//                        var parallelOptions = new ParallelOptions
//                        {
//                            MaxDegreeOfParallelism = Math.Min(optimalThreads, 4) // Ограничиваем для операций с дисками
//                        };

//                        // Разделяем доступные и недоступные диски
//                        var availableDrives = drives.Where(d => d.IsReady).ToList();
//                        var unavailableDrives = drives.Where(d => !d.IsReady).ToList();

//                        // Обработка доступных дисков
//                        foreach (var drive in availableDrives.OrderBy(d => d.Name))
//                        {
//                            try
//                            {
//                                var flags = EntityFlags.IsDrive;
//                                var driveType = StorageDetector.DetectStorageType(drive.Name);

//                                if (drive.DriveType == DriveType.Fixed)
//                                    flags |= EntityFlags.IsSystem;
//                                if (drive.DriveType == DriveType.Network)
//                                    flags |= EntityFlags.IsHidden;

//                                var newDrive = new DriveViewModel(drive, flags, driveType);
//                                newDrives.Add(newDrive);

//                                // Проверяем изменения
//                                var existingDrive = _cachedDrives.FirstOrDefault(d => d.FullName == drive.Name);
//                                if (existingDrive == null ||
//                                    Math.Abs(existingDrive.UsedProcentValue - newDrive.UsedProcentValue) >= 1.0 ||
//                                    existingDrive.FreeSpace != newDrive.FreeSpace)
//                                {
//                                    hasChanges = true;
//                                }
//                            }
//                            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                            {
//                                Debug.WriteLine($"[DriveService] Error reading {drive.Name}: {ex.Message}");
//                                hasChanges = true;
//                            }
//                        }

//                        // Обработка недоступных дисков
//                        foreach (var drive in unavailableDrives.OrderBy(d => d.Name))
//                        {
//                            try
//                            {
//                                var flags = EntityFlags.IsDrive | EntityFlags.IsUnavailable;
//                                var driveType = StorageDetector.DetectStorageType(drive.Name);

//                                newDrives.Add(new DriveViewModel(drive, flags, driveType));
//                                hasChanges = true;
//                            }
//                            catch (Exception ex)
//                            {
//                                Debug.WriteLine($"[DriveService] Error processing unavailable drive {drive.Name}: {ex.Message}");
//                            }
//                        }

//                        if (hasChanges || !_cachedDrives.Any())
//                        {
//                            _cachedDrives = newDrives.OrderBy(d => d.Name).ToList();
//                            _lastRefreshTime = DateTime.Now;
//                            RaiseDrivesUpdated();
//                        }
//                    }
//                }).ConfigureAwait(false);
//            }
//            finally
//            {
//                Interlocked.Exchange(ref _refreshInProgress, 0);
//            }
//        }

//        private static void InitializeWatcher()
//        {
//            try
//            {
//                _driveWatcher = new ManagementEventWatcher(
//                    new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3"));

//                _driveWatcher.EventArrived += async (sender, e) =>
//                {
//                    _drivesChanged = true;
//                    await Task.Delay(1000); // Анти-дребезг
//                    await RefreshDrivesAsync();
//                };

//                _driveWatcher.Start();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DriveService] Watcher init failed: {ex.Message}");
//            }
//        }

//        private static void RaiseDrivesUpdated()
//        {
//            var handler = DrivesUpdated;
//            if (handler == null) return;

//            if (HostDispatcher != null)
//            {
//                if (HostDispatcher.HasThreadAccess)
//                    handler.Invoke(null, EventArgs.Empty);
//                else
//                    HostDispatcher.TryEnqueue(() => handler.Invoke(null, EventArgs.Empty));
//            }
//            else
//            {
//                Task.Run(() => handler.Invoke(null, EventArgs.Empty));
//            }
//        }

//        public static async Task DisposeAsync()
//        {
//            lock (_syncLock)
//            {
//                if (_isDisposed) return;

//                try
//                {
//                    _driveWatcher?.Stop();
//                    _driveWatcher?.Dispose();
//                    _cachedDrives.Clear();
//                    _isDisposed = true;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[DriveService] Dispose error: {ex.Message}");
//                }
//            }

//            while (Interlocked.CompareExchange(ref _refreshInProgress, 0, 0) == 1)
//            {
//                await Task.Delay(50);
//            }
//        }

//        public static void Dispose()
//        {
//            DisposeAsync().GetAwaiter().GetResult();
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Core_FileManagement
{
    public static class DriveService
    {
        private static List<DriveViewModel> _cachedDrives = new();
        private static DateTime _lastRefreshTime = DateTime.MinValue;
        private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);
        private static readonly object _syncLock = new();
        private static ManagementEventWatcher _driveWatcher;
        private static bool _isDisposed = false;
        private static int _refreshInProgress = 0;
        private static volatile bool _drivesChanged = false;

        public static event EventHandler DrivesUpdated;
        public static Microsoft.UI.Dispatching.DispatcherQueue HostDispatcher { get; set; }

        static DriveService()
        {
            InitializeWatcher();
            _ = RefreshDrivesAsync();
        }

        public static IReadOnlyList<DriveViewModel> GetDrives()
        {
            lock (_syncLock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(DriveService));

                if (ShouldRefresh())
                {
                    _ = RefreshDrivesAsync();
                }
                return _cachedDrives.ToList().AsReadOnly();
            }
        }

        private static bool ShouldRefresh()
        {
            return DateTime.Now - _lastRefreshTime > _cacheExpiry ||
                   !_cachedDrives.Any() ||
                   _drivesChanged;
        }

        public static async Task RefreshDrivesAsync()
        {
            if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0)
                return;

            _drivesChanged = false;

            try
            {
                await Task.Run(() =>
                {
                    lock (_syncLock)
                    {
                        if (_isDisposed) return;

                        var newDrives = new List<DriveViewModel>();
                        var drives = DriveInfo.GetDrives();
                        bool hasChanges = false;

                        foreach (var drive in drives.OrderBy(d => d.Name))
                        {
                            try
                            {
                                var flags = EntityFlags.IsDrive;

                                if (drive.IsReady)
                                {
                                    if (drive.DriveType == DriveType.Fixed)
                                        flags |= EntityFlags.IsSystem;
                                    if (drive.DriveType == DriveType.Network)
                                        flags |= EntityFlags.IsHidden;
                                }
                                else
                                {
                                    flags |= EntityFlags.IsUnavailable;
                                }

                                var newDrive = new DriveViewModel(drive, flags);
                                newDrives.Add(newDrive);

                                // Проверяем изменения
                                var existingDrive = _cachedDrives.FirstOrDefault(d => d.FullName == drive.Name);
                                if (existingDrive == null || HasDriveChanged(existingDrive, newDrive))
                                {
                                    hasChanges = true;
                                }
                            }
                            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                            {
                                Debug.WriteLine($"[DriveService] Error reading {drive.Name}: {ex.Message}");
                                hasChanges = true;
                            }
                        }

                        if (hasChanges || !_cachedDrives.Any())
                        {
                            _cachedDrives = newDrives;
                            _lastRefreshTime = DateTime.Now;
                            RaiseDrivesUpdated();
                        }
                    }
                }).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Exchange(ref _refreshInProgress, 0);
            }
        }

        private static bool HasDriveChanged(DriveViewModel oldDrive, DriveViewModel newDrive)
        {
            return oldDrive.TotalSize != newDrive.TotalSize ||
                   oldDrive.FreeSpace != newDrive.FreeSpace ||
                   oldDrive.UsedProcentValue != newDrive.UsedProcentValue;
        }

        private static void InitializeWatcher()
        {
            try
            {
                _driveWatcher = new ManagementEventWatcher(
                    new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3"));

                _driveWatcher.EventArrived += async (sender, e) =>
                {
                    _drivesChanged = true;
                    await Task.Delay(1000); // Анти-дребезг
                    await RefreshDrivesAsync();
                };

                _driveWatcher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DriveService] Watcher init failed: {ex.Message}");
            }
        }

        private static void RaiseDrivesUpdated()
        {
            var handler = DrivesUpdated;
            if (handler == null) return;

            if (HostDispatcher != null)
            {
                if (HostDispatcher.HasThreadAccess)
                    handler.Invoke(null, EventArgs.Empty);
                else
                    HostDispatcher.TryEnqueue(() => handler.Invoke(null, EventArgs.Empty));
            }
            else
            {
                Task.Run(() => handler.Invoke(null, EventArgs.Empty));
            }
        }

        public static async Task DisposeAsync()
        {
            lock (_syncLock)
            {
                if (_isDisposed) return;

                try
                {
                    _driveWatcher?.Stop();
                    _driveWatcher?.Dispose();
                    _cachedDrives.Clear();
                    _isDisposed = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DriveService] Dispose error: {ex.Message}");
                }
            }

            while (Interlocked.CompareExchange(ref _refreshInProgress, 0, 0) == 1)
            {
                await Task.Delay(50);
            }
        }

        public static void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }
    }
}