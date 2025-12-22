using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Core_FileManagement
{
    [SupportedOSPlatform("windows")]
    public class PerformanceMonitor : IDisposable
    {
        #region Constants and Fields
        private const int MONITORING_INTERVAL_MS = 1000;

        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ioReadCounter;
        private readonly PerformanceCounter _ioWriteCounter;
        private readonly PerformanceCounter _memoryCounter;

        private float _currentCpuUsage;
        private float _currentIoUsage;

        private bool _isMonitoring;
        private bool _isDisposed;
        private CancellationTokenSource _monitoringCts;
        private readonly object _monitoringLock = new();

        public event EventHandler<PerformanceMetrics> MetricsUpdated;
        #endregion

        #region Properties
        public float CurrentCpuUsage => _currentCpuUsage;
        public float CurrentIoUsage => _currentIoUsage;
        public bool IsMonitoring => _isMonitoring;
        #endregion

        #region Constructor
        public PerformanceMonitor(bool isAdmin)
        {
            try
            {
                string processorCategory = isAdmin ? "Processor Information" : "Processor";
                string diskCategory = isAdmin ? "LogicalDisk" : "PhysicalDisk";

                _cpuCounter = new PerformanceCounter(
                    processorCategory,
                    isAdmin ? "% Processor Utility" : "% Processor Time",
                    "_Total");

                _ioReadCounter = new PerformanceCounter(
                    diskCategory,
                    "Disk Read Bytes/sec",
                    "_Total");

                _ioWriteCounter = new PerformanceCounter(
                    diskCategory,
                    "Disk Write Bytes/sec",
                    "_Total");

                _memoryCounter = new PerformanceCounter(
                    "Memory",
                    "Available MBytes");

                // Инициализация счетчиков
                _ = _cpuCounter.NextValue();

                Debug.WriteLine("[PerfMonitor] Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PerfMonitor] Initialization failed: {ex.Message}");
                throw new InvalidOperationException("Failed to initialize performance counters", ex);
            }
        }
        #endregion

        #region Public Interface
        public void StartMonitoring()
        {
            lock (_monitoringLock)
            {
                if (_isMonitoring || _isDisposed) return;

                _monitoringCts = new CancellationTokenSource();
                _isMonitoring = true;

                Task.Run(() => MonitorPerformanceLoopAsync(_monitoringCts.Token));
                Debug.WriteLine("[PerfMonitor] Monitoring started");
            }
        }

        public void StopMonitoring()
        {
            lock (_monitoringLock)
            {
                if (!_isMonitoring) return;

                _monitoringCts?.Cancel();
                _monitoringCts?.Dispose();
                _monitoringCts = null;
                _isMonitoring = false;

                Debug.WriteLine("[PerfMonitor] Monitoring stopped");
            }
        }

        public PerformanceMetrics GetCurrentMetrics()
        {
            return new PerformanceMetrics
            {
                CpuUsage = GetCpuUsage(),
                IoUsage = GetDiskActivity(),
                MemoryUsage = GetUsedMemory(),
                MemoryUsagePercent = GetMemoryUsagePercent(),
                TotalMemory = PerformanceManager.GetTotalSystemMemory()
            };
        }

        public void ForceUpdate()
        {
            if (!_isMonitoring) return;

            var metrics = GetCurrentMetrics();
            MetricsUpdated?.Invoke(this, metrics);
        }
        #endregion

        #region Monitoring Core
        private async Task MonitorPerformanceLoopAsync(CancellationToken cancellationToken)
        {
            while (_isMonitoring && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var metrics = GetCurrentMetrics();
                    MetricsUpdated?.Invoke(this, metrics);

                    await Task.Delay(MONITORING_INTERVAL_MS, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Ожидаемое исключение при остановке
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PerfMonitor] Error in monitoring loop: {ex.Message}");
                    await Task.Delay(MONITORING_INTERVAL_MS, cancellationToken);
                }
            }

            Debug.WriteLine("[PerfMonitor] Monitoring loop exited");
        }
        #endregion

        #region Performance Counters
        private float GetCpuUsage()
        {
            try
            {
                _currentCpuUsage = _cpuCounter.NextValue();
                return _currentCpuUsage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PerfMonitor] CPU reading error: {ex.Message}");
                return 0;
            }
        }

        private float GetDiskActivity()
        {
            try
            {
                float read = _ioReadCounter.NextValue();
                float write = _ioWriteCounter.NextValue();
                _currentIoUsage = (read + write) / (1024 * 1024); // Convert to MB/s
                return _currentIoUsage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PerfMonitor] IO reading error: {ex.Message}");
                return 0;
            }
        }

        private long GetUsedMemory()
        {
            try
            {
                float availableMB = _memoryCounter.NextValue();
                long totalMemory = PerformanceManager.GetTotalSystemMemory();
                return totalMemory - (long)(availableMB * 1024 * 1024);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PerfMonitor] Memory reading error: {ex.Message}");
                return 0;
            }
        }

        private double GetMemoryUsagePercent()
        {
            long total = PerformanceManager.GetTotalSystemMemory();
            long used = GetUsedMemory();
            return total > 0 ? (double)used / total * 100 : 0;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_isDisposed) return;

            lock (_monitoringLock)
            {
                StopMonitoring();

                _cpuCounter?.Dispose();
                _ioReadCounter?.Dispose();
                _ioWriteCounter?.Dispose();
                _memoryCounter?.Dispose();

                _isDisposed = true;
            }

            Debug.WriteLine("[PerfMonitor] Disposed");
        }
        #endregion
    }
}