using System;
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
                _isAdmin = CheckAdminRights();

                await Task.Run(() =>
                {
                    _ = GetTotalSystemMemory();
                });

                _monitor = new PerformanceMonitor(_isAdmin);

                _isInitialized = true;
                return true;
            }
            catch
            {
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
                _totalSystemMemory = -1;
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
                        return _totalSystemMemory;
                    }
                }
                catch
                {
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