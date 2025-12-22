//using System;
//using System.Diagnostics;
//using System.Management;
//using System.Threading.Tasks;
//using System.Runtime.Versioning;
//using System.Security.Principal;
//using System.Collections.Generic;
//using SettingManager;
//using System.Runtime.InteropServices;

//namespace Core_FileManagement
//{
//    [SupportedOSPlatform("windows")]
//    public static class PerformanceManager
//    {
//        #region Constants and Fields
//        private const long DEFAULT_MEMORY_BYTES = 16L * 1024 * 1024 * 1024;

//        private static PerformanceSettings _settings;
//        private static PerformanceMonitor _monitor;
//        private static bool _isInitialized;
//        private static bool _isAdmin;
//        private static long _totalSystemMemory = -1;

//        private static int _cachedPhysicalCores = -1;
//        private static int _cachedLogicalProcessors = -1;
//        private static bool _cachedIsServer = false;
//        private static int _cachedSmtRatio = -1;

//        private static readonly object _memoryLock = new();

//        #region Cache Fields
//        private static readonly object _cacheLock = new();
//        private static int _cachedOptimalThreadCount = -1;
//        private static StorageType? _cachedStorageType;
//        private static float _cachedCpuUsage;
//        private static float _cachedIoUsage;
//        private static bool _cacheInvalidated = true;
//        #endregion
//        #endregion

//        #region Public Interface
//        public static event EventHandler<PerformanceMetrics> MetricsUpdated
//        {
//            add
//            {
//                if (_monitor != null)
//                    _monitor.MetricsUpdated += value;
//            }
//            remove
//            {
//                if (_monitor != null)
//                    _monitor.MetricsUpdated -= value;
//            }
//        }

//        public static PerformanceSettings Settings
//        {
//            get => _settings ?? throw new InvalidOperationException("PerformanceManager not initialized");
//            set
//            {
//                _settings = value ?? throw new ArgumentNullException(nameof(value));
//                ApplyPerformanceProfile();
//                SaveSettings();
//            }
//        }

//        public static PerformanceMonitor Monitor => _monitor ??
//            throw new InvalidOperationException("PerformanceManager not initialized");

//        public static async Task<bool> InitializeAsync()
//        {
//            if (_isInitialized) return true;

//            try
//            {
//                // Синхронные операции
//                _isAdmin = CheckAdminRights();

//                // Асинхронная загрузка тяжелых операций
//                await Task.Run(() =>
//                {
//                    LoadPerformanceSettings();
//                    ApplyPerformanceProfile();
//                    _ = GetTotalSystemMemory();
//                });

//                // Инициализация монитора после загрузки настроек
//                _monitor = new PerformanceMonitor(_isAdmin);

//                _isInitialized = true;
//                Debug.WriteLine($"[Perf] Initialized successfully with profile: {_settings.Profile}");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Perf] Init error: {ex}");
//                return false;
//            }
//        }

//        public static void StartMonitoring()
//        {
//            if (!_isInitialized) return;
//            _monitor.StartMonitoring();
//        }

//        public static void StopMonitoring()
//        {
//            if (!_isInitialized) return;
//            _monitor.StopMonitoring();
//        }

//        public static void ForceUpdate()
//        {
//            if (!_isInitialized) return;
//            _monitor.ForceUpdate();
//        }

//        public static float CurrentCpuUsage => _isInitialized ? _monitor.CurrentCpuUsage : 0;
//        public static float CurrentIoUsage => _isInitialized ? _monitor.CurrentIoUsage : 0;
//        public static bool IsMonitoring => _isInitialized && _monitor.IsMonitoring;

//        #region Hardware Detection Methods
//        public static int GetPhysicalCoreCount()
//        {
//            if (_cachedPhysicalCores != -1)
//                return _cachedPhysicalCores;

//            try
//            {
//                // Приоритетный метод - через WMI
//                int wmiCores = GetPhysicalCoreCountViaWMI();
//                if (wmiCores > 0)
//                {
//                    _cachedPhysicalCores = wmiCores;
//                    Debug.WriteLine($"[CoreDetection] WMI physical cores: {_cachedPhysicalCores}");
//                    return _cachedPhysicalCores;
//                }

//                // Резервный метод
//                int logicalProcessors = Environment.ProcessorCount;
//                _cachedPhysicalCores = Math.Max(1, logicalProcessors / 2);
//                Debug.WriteLine($"[CoreDetection] Fallback physical cores: {_cachedPhysicalCores}");
//                return _cachedPhysicalCores;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[CoreDetection] Error: {ex.Message}");
//                _cachedPhysicalCores = Math.Max(1, Environment.ProcessorCount / 2);
//                return _cachedPhysicalCores;
//            }
//        }

//        public static int GetLogicalProcessorCount()
//        {
//            if (_cachedLogicalProcessors != -1)
//                return _cachedLogicalProcessors;

//            _cachedLogicalProcessors = Environment.ProcessorCount;
//            return _cachedLogicalProcessors;
//        }

//        public static bool IsServerSystem()
//        {
//            if (_cachedIsServer)
//                return _cachedIsServer;

//            try
//            {
//                int physicalCores = GetPhysicalCoreCount();
//                int logicalProcessors = GetLogicalProcessorCount();

//                // Более точное определение серверной системы
//                bool hasManyCores = physicalCores > 16;
//                bool hasManyThreads = logicalProcessors > 32;
//                bool highCoreToThreadRatio = logicalProcessors > physicalCores * 2;

//                _cachedIsServer = hasManyCores || hasManyThreads || highCoreToThreadRatio;
//                return _cachedIsServer;
//            }
//            catch
//            {
//                _cachedIsServer = false;
//                return false;
//            }
//        }

//        public static int GetLogicalToPhysicalCoreRatio()
//        {
//            if (_cachedSmtRatio != -1)
//                return _cachedSmtRatio;

//            try
//            {
//                int logical = GetLogicalProcessorCount();
//                int physical = GetPhysicalCoreCount();

//                if (physical > 0 && logical > 0 && logical >= physical)
//                {
//                    _cachedSmtRatio = Math.Max(1, Math.Min(4, logical / physical));
//                    Debug.WriteLine($"[SMT] Ratio: {_cachedSmtRatio}");
//                    return _cachedSmtRatio;
//                }

//                _cachedSmtRatio = 2;
//                return _cachedSmtRatio;
//            }
//            catch
//            {
//                _cachedSmtRatio = 2;
//                return _cachedSmtRatio;
//            }
//        }

//        public static void ClearHardwareCache()
//        {
//            _cachedPhysicalCores = -1;
//            _cachedLogicalProcessors = -1;
//            _cachedIsServer = false;
//            _cachedSmtRatio = -1;
//        }
//        #endregion

//        public static long GetTotalSystemMemory()
//        {
//            lock (_memoryLock)
//            {
//                if (_totalSystemMemory != -1)
//                    return _totalSystemMemory;

//                try
//                {
//                    using var searcher = new ManagementObjectSearcher(
//                        "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");

//                    foreach (ManagementObject mo in searcher.Get())
//                    {
//                        _totalSystemMemory = Convert.ToInt64(mo["TotalPhysicalMemory"]);
//                        Debug.WriteLine($"[Perf] Detected memory: {_totalSystemMemory / (1024 * 1024)} MB");
//                        return _totalSystemMemory;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[Perf] Memory detection failed: {ex.Message}");
//                    _totalSystemMemory = DEFAULT_MEMORY_BYTES;
//                }

//                return _totalSystemMemory;
//            }
//        }

//        [DllImport("kernel32.dll")]
//        private static extern void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);

//        [StructLayout(LayoutKind.Sequential)]
//        private struct SYSTEM_INFO
//        {
//            public ushort wProcessorArchitecture;
//            public ushort wReserved;
//            public uint dwPageSize;
//            public IntPtr lpMinimumApplicationAddress;
//            public IntPtr lpMaximumApplicationAddress;
//            public IntPtr dwActiveProcessorMask;
//            public uint dwNumberOfProcessors;
//            public uint dwProcessorType;
//            public uint dwAllocationGranularity;
//            public ushort wProcessorLevel;
//            public ushort wProcessorRevision;
//        }

//        public static int GetNativeProcessorCount()
//        {
//            GetNativeSystemInfo(out SYSTEM_INFO info);
//            return (int)info.dwNumberOfProcessors;
//        }

//        #endregion

//        #region Settings Management
//        public static void SaveSettings()
//        {
//            try
//            {
//                if (_settings == null) return;

//                SettingsManager.Instance.SaveSetting("PerformanceProfile", (int)_settings.Profile);
//                SettingsManager.Instance.SaveSetting("MaxCpuUsage", _settings.MaxCpuUsage);
//                SettingsManager.Instance.SaveSetting("MaxIoUsage", _settings.MaxIoUsage);
//                SettingsManager.Instance.SaveSetting("CpuPriority", _settings.CpuPriority);
//                SettingsManager.Instance.SaveSetting("IoPriority", _settings.IoPriority);
//                SettingsManager.Instance.SaveSetting("MaxThreads", _settings.MaxThreads);
//                SettingsManager.Instance.SaveSetting("MaxCores", _settings.MaxCores);

//                // Инвалидируем кэш при сохранении настроек
//                InvalidateCache();

//                Debug.WriteLine($"[Perf] Saved custom settings and invalidated cache");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Perf] Error saving settings: {ex}");
//            }
//        }

//        public static void LoadPerformanceSettings()
//        {
//            try
//            {
//                // Кэшируем значения один раз
//                int physicalCores = GetPhysicalCoreCount();
//                int logicalProcessors = GetLogicalProcessorCount();
//                bool isServer = IsServerSystem();

//                int profileValue = SettingsManager.Instance.GetSetting<int>(
//                    "PerformanceProfile",
//                    (int)PerformanceProfile.Balanced);

//                _settings = new PerformanceSettings
//                {
//                    Profile = (PerformanceProfile)profileValue,
//                    MaxCpuUsage = SettingsManager.Instance.GetSetting<int>("MaxCpuUsage", 80),
//                    MaxIoUsage = SettingsManager.Instance.GetSetting<int>("MaxIoUsage", 80),
//                    CpuPriority = SettingsManager.Instance.GetSetting<int>("CpuPriority", 50),
//                    IoPriority = SettingsManager.Instance.GetSetting<int>("IoPriority", 50),
//                    MaxThreads = SettingsManager.Instance.GetSetting<int>("MaxThreads",
//                        isServer ? Math.Min(logicalProcessors, 64) : logicalProcessors),
//                    MaxCores = SettingsManager.Instance.GetSetting<int>("MaxCores",
//                        isServer ? Math.Min(physicalCores, 32) : physicalCores)
//                };

//                // Валидация значений
//                ValidateSettings();

//                Debug.WriteLine($"[Perf] Loaded settings: Cores={_settings.MaxCores}, Threads={_settings.MaxThreads}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Perf] Error loading settings: {ex}");
//                _settings = new PerformanceSettings { Profile = PerformanceProfile.Balanced };
//            }
//        }

//        public static void ResetToDefaultSettings()
//        {
//            _settings = new PerformanceSettings { Profile = PerformanceProfile.Balanced };
//            ApplyPerformanceProfile();
//            SaveSettings();
//            Debug.WriteLine("[Perf] Settings reset to default");
//        }
//        #endregion

//        #region Cache Management
//        public static int CalculateOptimalThreadCount(StorageType? storageType = null)
//        {
//            if (!_isInitialized)
//                return Math.Max(1, Environment.ProcessorCount);

//            try
//            {
//                // Получаем текущие метрики
//                float currentCpuUsage = CurrentCpuUsage;
//                float currentIoUsage = CurrentIoUsage;

//                lock (_cacheLock)
//                {
//                    // Проверяем валидность кэша
//                    if (!_cacheInvalidated &&
//                        _cachedOptimalThreadCount != -1 &&
//                        _cachedStorageType == storageType &&
//                        Math.Abs(_cachedCpuUsage - currentCpuUsage) < 0.1f &&
//                        Math.Abs(_cachedIoUsage - currentIoUsage) < 0.1f)
//                    {
//                        Debug.WriteLine($"[Perf] Using cached thread count: {_cachedOptimalThreadCount}");
//                        return _cachedOptimalThreadCount;
//                    }

//                    // Вычисляем новое значение
//                    int threadCount = CalculateOptimalThreadCountInternal(storageType, currentCpuUsage, currentIoUsage);

//                    // Обновляем кэш
//                    _cachedOptimalThreadCount = threadCount;
//                    _cachedStorageType = storageType;
//                    _cachedCpuUsage = currentCpuUsage;
//                    _cachedIoUsage = currentIoUsage;
//                    _cacheInvalidated = false;

//                    Debug.WriteLine($"[Perf] Cache updated: {threadCount} threads");
//                    return threadCount;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Perf] Error calculating thread count: {ex}");
//                return Math.Max(1, Environment.ProcessorCount);
//            }
//        }

//        private static int CalculateOptimalThreadCountInternal(StorageType? storageType, float cpuUsage, float ioUsage)
//        {
//            // Учитываем пользовательские настройки
//            int maxThreads = _settings?.MaxThreads ?? Environment.ProcessorCount;
//            int maxCores = _settings?.MaxCores ?? GetPhysicalCoreCount();
//            float cpuPriority = (_settings?.CpuPriority ?? 50) / 100f;
//            float ioPriority = (_settings?.IoPriority ?? 50) / 100f;

//            float cpuUsageSafe = Math.Max(0.1f, cpuUsage);
//            float maxCpuSafe = Math.Max(0.1f, _settings?.MaxCpuUsage ?? 70);
//            float maxIoSafe = Math.Max(0.1f, _settings?.MaxIoUsage ?? 80);

//            float totalPriority = cpuPriority + ioPriority;
//            float totalPrioritySafe = totalPriority > 0 ? totalPriority : 1.0f;

//            float cpuWeight = cpuPriority / totalPrioritySafe;
//            float ioWeight = ioPriority / totalPrioritySafe;

//            float storageFactor = storageType.HasValue
//                ? StorageCharacteristics.GetProfile(storageType.Value)?.ParallelismFactor ?? 1.0f
//                : 1.0f;

//            float cpuFactor = 1 - (cpuUsageSafe / maxCpuSafe);
//            float ioFactor = 1 - (ioUsage / maxIoSafe);

//            // Базовое значение с учетом ограничений
//            int baseThreads = Math.Min(
//                maxThreads,
//                maxCores * GetLogicalToPhysicalCoreRatio()
//            );

//            int threads = (int)(baseThreads * (cpuWeight * cpuFactor + ioWeight * ioFactor * storageFactor));
//            return Math.Clamp(threads, 1, maxThreads);
//        }

//        public static void InvalidateCache()
//        {
//            lock (_cacheLock)
//            {
//                _cacheInvalidated = true;
//                _cachedOptimalThreadCount = -1;
//                Debug.WriteLine("[Perf] Cache invalidated");
//            }
//        }
//        #endregion

//        #region Helper Methods
//        private static bool CheckAdminRights()
//        {
//            try
//            {
//                using var identity = WindowsIdentity.GetCurrent();
//                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public static void ApplyPerformanceProfile()
//        {
//            try
//            {
//                if (_settings == null)
//                {
//                    _settings = new PerformanceSettings();
//                    Debug.WriteLine("[Perf] Created new settings with default profile");
//                }

//                if (!Enum.IsDefined(typeof(PerformanceProfile), _settings.Profile))
//                {
//                    _settings.Profile = PerformanceProfile.Balanced;
//                    Debug.WriteLine("[Perf] Invalid profile, defaulting to Balanced");
//                }

//                Debug.WriteLine($"[Perf] Applying {_settings.Profile} profile...");

//                if (_settings.Profile != PerformanceProfile.Custom)
//                {
//                    switch (_settings.Profile)
//                    {
//                        case PerformanceProfile.Low:
//                            SetProfileValues(20, 30, 30, 40);
//                            break;
//                        case PerformanceProfile.PowerSaver:
//                            SetProfileValues(30, 40, 50, 60);
//                            break;
//                        case PerformanceProfile.Balanced:
//                            SetProfileValues(50, 50, 70, 80);
//                            break;
//                        case PerformanceProfile.HighPerformance:
//                            SetProfileValues(70, 80, 90, 95);
//                            break;
//                        case PerformanceProfile.Realtime:
//                            SetProfileValues(100, 100, 100, 100);
//                            break;
//                        default:
//                            _settings.Profile = PerformanceProfile.Balanced;
//                            SetProfileValues(50, 50, 70, 80);
//                            Debug.WriteLine("[Perf] Unknown profile, using Balanced");
//                            break;
//                    }
//                }

//                if (_settings.MaxCores <= 0)
//                    _settings.MaxCores = Environment.ProcessorCount;

//                if (_settings.MaxThreads <= 0)
//                    _settings.MaxThreads = Environment.ProcessorCount;

//                // Инвалидируем кэш после применения профиля
//                InvalidateCache();

//                // Принудительно обновляем кэши
//                DirectoryCacheService.InvalidateCache();
//                FileCacheService.ClearCache();

//                Debug.WriteLine($"[Perf] Applied: Cores={_settings.MaxCores}, " +
//                              $"Threads={_settings.MaxThreads}, " +
//                              $"CPU={_settings.CpuPriority}%, " +
//                              $"IO={_settings.IoPriority}%");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Perf] Error applying profile: {ex}");
//            }
//            finally
//            {
//                SaveSettings();
//            }
//        }

//        private static void SetProfileValues(int cpuPriority, int ioPriority, int maxCpu, int maxIo)
//        {
//            _settings.CpuPriority = Math.Clamp(cpuPriority, 0, 100);
//            _settings.IoPriority = Math.Clamp(ioPriority, 0, 100);
//            _settings.MaxCpuUsage = Math.Clamp(maxCpu, 1, 100);
//            _settings.MaxIoUsage = Math.Clamp(maxIo, 1, 100);
//        }

//        private static void ValidateSettings()
//        {
//            try
//            {
//                int physicalCores = GetPhysicalCoreCount();
//                int logicalProcessors = GetLogicalProcessorCount();
//                bool isServer = IsServerSystem();

//                // Универсальные ограничения
//                int maxAllowedCores = isServer ? Math.Min(physicalCores, 128) : physicalCores;
//                int maxAllowedThreads = isServer ?
//                    Math.Min(logicalProcessors * 2, 512) :
//                    Math.Min(logicalProcessors, 128);

//                // Валидация ядер
//                if (_settings.MaxCores <= 0 || _settings.MaxCores > maxAllowedCores)
//                {
//                    Debug.WriteLine($"Invalid cores: {_settings.MaxCores}, resetting to {physicalCores}");
//                    _settings.MaxCores = physicalCores;
//                }

//                // Валидация потоков
//                if (_settings.MaxThreads <= 0 || _settings.MaxThreads > maxAllowedThreads)
//                {
//                    Debug.WriteLine($"Invalid threads: {_settings.MaxThreads}, resetting to {logicalProcessors}");
//                    _settings.MaxThreads = logicalProcessors;
//                }

//                // Синхронизация значений: потоки не могут быть меньше ядер
//                if (_settings.MaxThreads < _settings.MaxCores)
//                {
//                    Debug.WriteLine($"Threads ({_settings.MaxThreads}) < Cores ({_settings.MaxCores}), adjusting threads");
//                    _settings.MaxThreads = _settings.MaxCores;
//                }

//                Debug.WriteLine($"[Perf] Validated: Cores={_settings.MaxCores}, Threads={_settings.MaxThreads}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Perf] Error validating settings: {ex}");
//                // Резервная валидация
//                int processors = Math.Max(1, Environment.ProcessorCount);
//                _settings.MaxCores = Math.Max(1, Math.Min(_settings.MaxCores, processors));
//                _settings.MaxThreads = Math.Max(1, Math.Min(_settings.MaxThreads, processors));
//            }
//        }

//        private static int GetPhysicalCoreCountViaWMI()
//        {
//            try
//            {
//                int totalCores = 0;
//                using var searcher = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor");

//                foreach (ManagementObject obj in searcher.Get())
//                {
//                    if (obj["NumberOfCores"] != null)
//                    {
//                        totalCores += Convert.ToInt32(obj["NumberOfCores"]);
//                    }
//                }

//                return totalCores > 0 ? totalCores : 0;
//            }
//            catch
//            {
//                return 0; // Вернем 0 для использования резервного метода
//            }
//        }

//        public static void Cleanup()
//        {
//            if (_isInitialized)
//            {
//                StopMonitoring();
//                _monitor?.Dispose();
//                _monitor = null;
//                _isInitialized = false;
//                ClearHardwareCache();
//                _totalSystemMemory = -1;
//                InvalidateCache(); // Очищаем кэш при очистке
//            }
//        }
//        #endregion
//    }
//}

using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Core_FileManagement
{
    [SupportedOSPlatform("windows")]
    public static class PerformanceManager
    {
        #region Constants and Fields
        private const long DEFAULT_MEMORY_BYTES = 16L * 1024 * 1024 * 1024;

        private static PerformanceMonitor _monitor;
        private static bool _isInitialized;
        private static bool _isAdmin;
        private static long _totalSystemMemory = -1;
        private static readonly object _memoryLock = new();
        #endregion

        #region Public Interface
        public static event EventHandler<PerformanceMetrics> MetricsUpdated
        {
            add
            {
                if (_monitor != null)
                    _monitor.MetricsUpdated += value;
            }
            remove
            {
                if (_monitor != null)
                    _monitor.MetricsUpdated -= value;
            }
        }

        public static PerformanceMonitor Monitor => _monitor ??
            throw new InvalidOperationException("PerformanceManager not initialized");

        public static async Task<bool> InitializeAsync()
        {
            if (_isInitialized) return true;

            try
            {
                // Синхронные операции
                _isAdmin = CheckAdminRights();

                // Асинхронная загрузка тяжелых операций
                await Task.Run(() =>
                {
                    // Предварительная инициализация памяти
                    _ = GetTotalSystemMemory();
                });

                // Инициализация монитора
                _monitor = new PerformanceMonitor(_isAdmin);

                _isInitialized = true;
                Debug.WriteLine("[Perf] Initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Perf] Init error: {ex}");
                return false;
            }
        }

        public static void StartMonitoring()
        {
            if (!_isInitialized) return;
            _monitor.StartMonitoring();
        }

        public static void StopMonitoring()
        {
            if (!_isInitialized) return;
            _monitor.StopMonitoring();
        }

        public static void ForceUpdate()
        {
            if (!_isInitialized) return;
            _monitor.ForceUpdate();
        }

        public static float CurrentCpuUsage => _isInitialized ? _monitor.CurrentCpuUsage : 0;
        public static float CurrentIoUsage => _isInitialized ? _monitor.CurrentIoUsage : 0;
        public static bool IsMonitoring => _isInitialized && _monitor.IsMonitoring;

        public static void Dispose()
        {
            if (_isInitialized)
            {
                StopMonitoring();
                _monitor?.Dispose();
                _monitor = null;
                _isInitialized = false;
                _totalSystemMemory = -1; // Сбрасываем кэш памяти
                Debug.WriteLine("[Perf] Disposed");
            }
        }
        #endregion

        #region Memory Management
        public static long GetTotalSystemMemory()
        {
            lock (_memoryLock)
            {
                if (_totalSystemMemory != -1)
                    return _totalSystemMemory;

                try
                {
                    using var searcher = new ManagementObjectSearcher(
                        "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");

                    foreach (ManagementObject mo in searcher.Get())
                    {
                        _totalSystemMemory = Convert.ToInt64(mo["TotalPhysicalMemory"]);
                        Debug.WriteLine($"[Perf] Detected memory: {_totalSystemMemory / (1024 * 1024)} MB");
                        return _totalSystemMemory;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Perf] Memory detection failed: {ex.Message}");
                    _totalSystemMemory = DEFAULT_MEMORY_BYTES;
                }

                return _totalSystemMemory;
            }
        }
        #endregion

        #region Helper Methods
        private static bool CheckAdminRights()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}