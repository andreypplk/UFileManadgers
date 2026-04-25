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