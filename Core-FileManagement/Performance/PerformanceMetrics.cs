using System;

namespace Core_FileManagement
{
    public class PerformanceMetrics
    {
        public float CpuUsage { get; set; }
        public float IoUsage { get; set; } // MB/s
        public long MemoryUsage { get; set; } // bytes
        public double MemoryUsagePercent { get; set; }
        public long TotalMemory { get; set; } // bytes
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"CPU: {CpuUsage:F1}%, IO: {IoUsage:F1} MB/s, " +
                   $"Memory: {MemoryUsagePercent:F1}% ({MemoryUsage / (1024 * 1024):N0} MB)";
        }
    }
}