using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ufm
{
    internal class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController(
            [In] DispatcherQueueOptions options,
            out IntPtr dispatcherQueueControllerPtr);

        private object _dispatcherQueueController; // Убрана аннотация nullable

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (global::Windows.System.DispatcherQueue.GetForCurrentThread() != null)
                return;

            if (_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options = new()
                {
                    dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions)), // Вернул обратно для совместимости
                    threadType = 2,
                    apartmentType = 2
                };

                IntPtr controllerPtr = IntPtr.Zero;
                int hr = CreateDispatcherQueueController(options, out controllerPtr);

                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                try
                {
                    _dispatcherQueueController = Marshal.GetObjectForIUnknown(controllerPtr);
                }
                finally
                {
                    if (controllerPtr != IntPtr.Zero)
                        Marshal.Release(controllerPtr);
                }
            }
        }
    }
}